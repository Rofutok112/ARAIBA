using System;
using System.Collections.Generic;
using Projects.Scripts.Audio;
using Projects.Scripts.Control;
using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    /// <summary>
    /// パズルピースのMonoBehaviour。
    /// ドラッグ＆ドロップでグリッドに配置する。
    /// ピースの形状はPuzzlePieceShapeで定義する。
    /// </summary>
    public class PuzzlePiece : MonoBehaviour, IInputHandler
    {
        [Header("Piece Settings")]
        [Tooltip("このピースの形状データ")]
        [SerializeField] private PuzzlePieceShape shape;

        [Tooltip("配置先のグリッド")]
        [SerializeField] private PuzzleGridView gridView;

        [Header("Visual Settings")]
        [Tooltip("ドラッグ中のスケール倍率")]
        [SerializeField] private float dragScale = 1.0f;

        [Tooltip("ドラッグ中の透明度（0.0～1.0）")]
        [SerializeField] private float dragAlpha = 0.5f;

        [Tooltip("プレビュー（ゴースト）の不透明度（0.0～1.0）")]
        [SerializeField, Range(0f, 1f)] private float previewAlpha = 0.4f;

        [Tooltip("ストック状態でのスケール倍率")]
        [SerializeField, Range(0.1f, 1f)] private float stockScaleMultiplier = 0.8f;

        private Vector2 _dragOffset;
        private Vector3 _originalScale;
        private Vector2 _spawnPosition;
        private bool _isPlaced;
        private bool _returnToSpawnOnFailedPlacement = true;
        private bool _isInitialized;
        private Action<PuzzlePiece> _onPlacedCallback;
        private Action<PuzzlePiece> _onFailedPlacementCallback;
        private readonly List<SpriteRenderer> _spriteRenderers = new();
        private SpriteRenderer _dishRenderer;
        private GameObject _ghostObject;
        private int _orderInLayer;
        private Sprite _selectedDishSprite;

        /// <summary>
        /// このピースの形状データ
        /// </summary>
        public PuzzlePieceShape Shape => shape;

        /// <summary>
        /// グリッド上に配置済みかどうか
        /// </summary>
        public bool IsPlaced => _isPlaced;

        /// <summary>
        /// スタック内での表示順
        /// </summary>
        public int OrderInLayer => _orderInLayer;

        /// <summary>
        /// PuzzlePieceGeneratorから動的に初期化する
        /// </summary>
        public void Initialize(
            PuzzlePieceShape pieceShape,
            PuzzleGridView targetGridView,
            Sprite selectedDishSprite = null,
            Action<PuzzlePiece> onPlaced = null,
            bool returnToSpawnOnFailedPlacement = true,
            Action<PuzzlePiece> onFailedPlacement = null)
        {
            shape = pieceShape;
            gridView = targetGridView;
            _selectedDishSprite = selectedDishSprite;
            _onPlacedCallback = onPlaced;
            _returnToSpawnOnFailedPlacement = returnToSpawnOnFailedPlacement;
            _onFailedPlacementCallback = onFailedPlacement;
            InitializeVisualState();
        }

        private void Start()
        {
            InitializeVisualState();
        }

        private void InitializeVisualState()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            _spawnPosition = transform.position;
            _originalScale = transform.localScale;
            CreatePieceVisuals();
        }

        /// <summary>
        /// ピースの形状に基づいてビジュアルを生成する
        /// </summary>
        private void CreatePieceVisuals()
        {
            if (shape == null) return;

            var filledCells = shape.GetFilledCells();
            var cellSize = gridView != null ? gridView.CellSize : 1f;

            // ピースの中心を計算
            var center = CalculatePieceCenter(filledCells, cellSize);

            // ドラッグ検出用のColliderを追加
            EnsureCollider(filledCells, cellSize, center);

            // 食器スプライトの表示
            CreateDishOverlay(cellSize, center);
        }

        /// <summary>
        /// 食器スプライトをピースの中心にオーバーレイ表示する
        /// </summary>
        private void CreateDishOverlay(float cellSize, Vector2 center)
        {
            var sprite = GetSelectedSprite();
            if (sprite == null) return;

            var dishObj = new GameObject("DishOverlay");
            dishObj.transform.SetParent(transform, false);

            // 形状のバウンディングボックスの中心に配置
            var bboxCenter = new Vector3(
                (shape.Width - 1) * cellSize / 2f - center.x,
                (shape.Height - 1) * cellSize / 2f - center.y,
                0f
            );
            dishObj.transform.localPosition = bboxCenter;

            var sr = dishObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 10 + _orderInLayer;
            _dishRenderer = sr;
            _spriteRenderers.Add(sr);

            // スプライトのピクセルサイズから自動スケーリング
            // 形状のバウンディングボックス（width * cellSize × height * cellSize）に収まるようにする
            var spriteSize = sprite.bounds.size;
            var targetWidth = shape.Width * cellSize;
            var targetHeight = shape.Height * cellSize;
            var scaleX = targetWidth / spriteSize.x;
            var scaleY = targetHeight / spriteSize.y;
            dishObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        /// <summary>
        /// ピースの全セルの中心座標を計算する
        /// </summary>
        private static Vector2 CalculatePieceCenter(Vector2Int[] filledCells, float cellSize)
        {
            if (filledCells.Length == 0) return Vector2.zero;

            var sum = Vector2.zero;
            foreach (var cell in filledCells)
            {
                sum += new Vector2(cell.x * cellSize, cell.y * cellSize);
            }
            return sum / filledCells.Length;
        }

        /// <summary>
        /// ドラッグ可能にするためのColliderを設定する
        /// </summary>
        private void EnsureCollider(Vector2Int[] filledCells, float cellSize, Vector2 center)
        {
            // 既存のColliderがあれば削除
            var existingCollider = GetComponent<Collider2D>();
            if (existingCollider != null)
                Destroy(existingCollider);

            // ピースの全セルを包むBoxColliderを作成
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var cell in filledCells)
            {
                var x = cell.x * cellSize - center.x;
                var y = cell.y * cellSize - center.y;
                minX = Mathf.Min(minX, x - cellSize / 2f);
                minY = Mathf.Min(minY, y - cellSize / 2f);
                maxX = Mathf.Max(maxX, x + cellSize / 2f);
                maxY = Mathf.Max(maxY, y + cellSize / 2f);
            }

            var boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.offset = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            boxCollider.size = new Vector2(maxX - minX, maxY - minY);
        }

        public void ConfigureStackPresentation(Vector3 worldPosition, int orderInLayer, bool isInteractable)
        {
            if (_isPlaced) return;

            _orderInLayer = Mathf.Clamp(orderInLayer, 0, 19);
            transform.position = worldPosition;
            _spawnPosition = worldPosition;

            if (_dishRenderer != null)
            {
                _dishRenderer.sortingOrder = 10 + _orderInLayer;
            }

            ApplyStockScale();
            SetInteractable(isInteractable);
        }

        public void SetInteractable(bool isInteractable)
        {
            if (_isPlaced) return;

            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = isInteractable;
            }
        }

        public void OnInputBegin(Vector2 pos)
        {
            if (_isPlaced) return;
            
            AudioManager.PlayOneShot("PieceClick");
            
            _dragOffset = (Vector2)transform.position - pos;
            ApplyFullScale(dragScale);
            SetAlpha(dragAlpha);
        }

        public void OnInputDrag(Vector2 pos)
        {
            if (_isPlaced) return;
            
            transform.position = pos + _dragOffset;

            // グリッド上にゴーストプレビューを表示
            if (gridView != null)
            {
                var gridPos = GetGridOrigin(pos + _dragOffset);
                UpdateGhostPreview(gridPos);
            }
        }

        public void OnInputEnd(Vector2 pos)
        {
            if (_isPlaced) return;

            ApplyFullScale();
            SetAlpha(1f);
            DestroyGhost();

            if (gridView == null) return;

            var gridPos = GetGridOrigin(pos + _dragOffset);

            if (gridView.Grid.TryPlace(shape, gridPos))
            {
                // 配置成功: ピースをグリッドのセル位置にスナップ
                AudioManager.PlayOneShot("PiecePlace");
                SnapToGrid(gridPos);
                _isPlaced = true;

                // Colliderを無効化（配置済みのピースはドラッグ不可）
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                // 配置完了コールバックを発火
                _onPlacedCallback?.Invoke(this);
            }
            else
            {
                if (_returnToSpawnOnFailedPlacement)
                {
                    transform.position = _spawnPosition;
                    AudioManager.PlayOneShot("PieceCancel");
                    ApplyStockScale();
                }
                else
                {
                    _onFailedPlacementCallback?.Invoke(this);
                }
            }
        }

        private void ApplyStockScale()
        {
            transform.localScale = _originalScale * stockScaleMultiplier;
        }

        private void ApplyFullScale(float multiplier = 1f)
        {
            transform.localScale = _originalScale * multiplier;
        }

        /// <summary>
        /// 全SpriteRendererのアルファ値を設定する
        /// </summary>
        private void SetAlpha(float alpha)
        {
            foreach (var sr in _spriteRenderers)
            {
                if (sr == null) continue;
                var c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }

        /// <summary>
        /// ゴーストプレビューの位置を更新する。未作成なら作成する。
        /// </summary>
        private void UpdateGhostPreview(Vector2Int gridOrigin)
        {
            if (_ghostObject == null)
                CreateGhost();

            // ゴーストをスナップ位置に移動
            var filledCells = shape.GetFilledCells();
            var cellSize = gridView.CellSize;
            var center = CalculatePieceCenter(filledCells, cellSize);

            var gridWorldPos = gridView.GridToWorldPosition(gridOrigin);
            var snappedPos = new Vector2(
                gridWorldPos.x + center.x,
                gridWorldPos.y + center.y
            );

            _ghostObject.transform.position = new Vector3(snappedPos.x, snappedPos.y, transform.position.z);
        }

        /// <summary>
        /// ピースの半透明コピー（ゴースト）を作成する
        /// </summary>
        private void CreateGhost()
        {
            if (shape == null) return;

            _ghostObject = new GameObject("GhostPreview");

            var filledCells = shape.GetFilledCells();
            var cellSize = gridView != null ? gridView.CellSize : 1f;
            var center = CalculatePieceCenter(filledCells, cellSize);

            // 食器スプライトのゴーストを生成
            var sprite = GetSelectedSprite();
            if (sprite != null)
            {
                var dishObj = new GameObject("GhostDishOverlay");
                dishObj.transform.SetParent(_ghostObject.transform, false);

                var bboxCenter = new Vector3(
                    (shape.Width - 1) * cellSize / 2f - center.x,
                    (shape.Height - 1) * cellSize / 2f - center.y,
                    0f
                );
                dishObj.transform.localPosition = bboxCenter;

                var sr = dishObj.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(1f, 1f, 1f, previewAlpha);
                sr.sortingOrder = 5;

                var spriteSize = sprite.bounds.size;
                var targetWidth = shape.Width * cellSize;
                var targetHeight = shape.Height * cellSize;
                var scaleX = targetWidth / spriteSize.x;
                var scaleY = targetHeight / spriteSize.y;
                dishObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }

        private Sprite GetSelectedSprite()
        {
            return _selectedDishSprite != null ? _selectedDishSprite : shape.GetEffectiveSprite();
        }

        /// <summary>
        /// ゴーストプレビューを破棄する
        /// </summary>
        private void DestroyGhost()
        {
            if (_ghostObject != null)
            {
                Destroy(_ghostObject);
                _ghostObject = null;
            }
        }

        /// <summary>
        /// ピースのワールド座標から、ピース形状の原点(0,0)が対応するグリッド座標を計算する
        /// </summary>
        private Vector2Int GetGridOrigin(Vector2 pieceWorldPos)
        {
            if (shape == null) return Vector2Int.zero;

            var filledCells = shape.GetFilledCells();
            var cellSize = gridView.CellSize;
            var center = CalculatePieceCenter(filledCells, cellSize);

            // ピースの原点(0,0)のワールド座標（セル中心）
            var originWorldPos = new Vector2(
                pieceWorldPos.x - center.x,
                pieceWorldPos.y - center.y
            );

            // セルのワールド中心座標をグリッド座標に変換して返す
            return gridView.WorldToGridPosition(originWorldPos);
        }

        /// <summary>
        /// ピースをグリッドの配置位置にスナップする
        /// </summary>
        private void SnapToGrid(Vector2Int gridOrigin)
        {
            var filledCells = shape.GetFilledCells();
            var cellSize = gridView.CellSize;
            var center = CalculatePieceCenter(filledCells, cellSize);

            // ピースの中心オフセットをそのまま加算する
            var gridWorldPos = gridView.GridToWorldPosition(gridOrigin);
            var snappedPos = new Vector2(
                gridWorldPos.x + center.x,
                gridWorldPos.y + center.y
            );

            transform.position = new Vector3(snappedPos.x, snappedPos.y, transform.position.z);
        }
    }
}
