using System;
using System.Collections.Generic;
using DG.Tweening;
using Projects.Scripts.Audio;
using Projects.Scripts.Common;
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
        private const int DragFrontSortingOrderStart = 1000;
        private static int s_nextDragSortingOrder = DragFrontSortingOrderStart;

        private PuzzlePieceShape _shape;
        private PuzzleGridView _gridView;

        [Header("Visual Settings")]
        [Tooltip("ドラッグ中のスケール倍率")]
        [SerializeField] private float dragScale = 1.0f;

        [Tooltip("ドラッグ中の透明度（0.0～1.0）")]
        [SerializeField] private float dragAlpha = 0.5f;

        [Tooltip("プレビュー（ゴースト）の不透明度（0.0～1.0）")]
        [SerializeField, Range(0f, 1f)] private float previewAlpha = 0.4f;

        [Tooltip("ストック状態でのスケール倍率")]
        [SerializeField, Range(0.1f, 1f)] private float stockScaleMultiplier = 0.8f;
        [SerializeField, Min(0f)] private float spawnFadeDuration = 0.18f;

        [Header("Dirty Visuals")]
        [SerializeField] private Sprite[] dirtyOverlaySprites;
        [SerializeField, Min(0f)] private float dirtyCountPerFilledCell = 0.35f;
        [SerializeField, Min(0)] private int minimumDirtyOverlayCount = 1;
        [SerializeField, Min(0)] private int maximumDirtyOverlayCount = 6;
        [SerializeField, Range(0f, 1f)] private float dirtyAlphaMin = 0.7f;
        [SerializeField, Range(0f, 1f)] private float dirtyAlphaMax = 1f;
        [SerializeField] private Vector2 dirtyScaleRange = new(0.45f, 0.8f);
        [SerializeField, Min(0)] private int dirtySortingOrderOffset = 1;

        private Vector2 _dragOffset;
        private Vector3 _originalScale;
        private Vector3 _spawnLocalPosition;
        private bool _isPlaced;
        private bool _returnToSpawnOnFailedPlacement = true;
        private bool _isInitialized;
        private Action<PuzzlePiece> _onPlacedCallback;
        private Action<PuzzlePiece> _onFailedPlacementCallback;
        private readonly List<SpriteRenderer> _spriteRenderers = new();
        private readonly Dictionary<SpriteRenderer, float> _baseAlphaByRenderer = new();
        private SpriteRenderer _dishRenderer;
        private GameObject _ghostObject;
        private PuzzlePieceGhostFactory _ghostFactory;
        private int _orderInLayer;
        private Sprite _selectedDishSprite;
        private Vector2Int _placedGridOrigin;
        private Vector2 _localCenter;
        private Tween _spawnFadeTween;

        private GridGeometry? CurrentGeometry => _gridView != null ? _gridView.Geometry : null;
        private PuzzlePiecePlacement? CurrentPlacement => _shape != null && CurrentGeometry != null
            ? new PuzzlePiecePlacement(_shape, CurrentGeometry.Value)
            : null;

        /// <summary>
        /// このピースの形状データ
        /// </summary>
        public PuzzlePieceShape Shape => _shape;

        /// <summary>
        /// グリッド上に配置済みかどうか
        /// </summary>
        public bool IsPlaced => _isPlaced;

        /// <summary>
        /// スタック内での表示順
        /// </summary>
        public int OrderInLayer => _orderInLayer;
        public string DishTypeKey => _selectedDishSprite != null
            ? _selectedDishSprite.name
            : _shape != null
                ? _shape.name
                : "Unknown";
        public Sprite SelectedDishSprite => _selectedDishSprite != null ? _selectedDishSprite : _shape?.GetEffectiveSprite();
        public Vector2Int PlacedGridOrigin => _placedGridOrigin;

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
            _shape = pieceShape;
            _gridView = targetGridView;
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

        private void OnDisable()
        {
            _spawnFadeTween?.Kill();
            _spawnFadeTween = null;
        }

        private void InitializeVisualState()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            ApplyBaseCellScale();
            _spawnLocalPosition = transform.localPosition;
            _originalScale = transform.localScale;
            CreatePieceVisuals();
        }

        /// <summary>
        /// ピースの形状に基づいてビジュアルを生成する
        /// </summary>
        private void CreatePieceVisuals()
        {
            if (_shape == null) return;

            var dirtSettings = new DishDirtVisualSettings(
                dirtyOverlaySprites,
                dirtyCountPerFilledCell,
                minimumDirtyOverlayCount,
                maximumDirtyOverlayCount,
                dirtyAlphaMin,
                dirtyAlphaMax,
                dirtyScaleRange,
                dirtySortingOrderOffset);
            var visualBuilder = new PuzzlePieceVisualBuilder(_shape, GetSelectedSprite(), transform, dirtSettings);
            var visualResult = visualBuilder.Build(previewAlpha, 10 + _orderInLayer, CurrentGeometry?.CellWorldSize ?? Vector2.one);
            _localCenter = visualResult.LocalCenter;
            _dishRenderer = visualResult.DishRenderer;
            _ghostFactory = visualResult.GhostFactory;
            _spriteRenderers.Clear();
            _baseAlphaByRenderer.Clear();
            if (visualResult.SpriteRenderers != null)
            {
                _spriteRenderers.AddRange(visualResult.SpriteRenderers);
                foreach (var spriteRenderer in _spriteRenderers)
                {
                    if (spriteRenderer != null)
                    {
                        _baseAlphaByRenderer[spriteRenderer] = spriteRenderer.color.a;
                    }
                }
            }

            UpdateSortingOrders(10 + _orderInLayer);
        }

        public void ConfigureStackPresentationLocal(Vector3 localPosition, int orderInLayer, bool isInteractable)
        {
            if (_isPlaced) return;

            _orderInLayer = Mathf.Clamp(orderInLayer, 0, 19);
            transform.localPosition = localPosition;
            _spawnLocalPosition = localPosition;

            UpdateSortingOrders(10 + _orderInLayer);

            ApplyStockScale();
            SetInteractable(isInteractable);
        }

        public void PlaySpawnFade()
        {
            if (spawnFadeDuration <= 0f || _spriteRenderers.Count == 0)
            {
                SetAlpha(1f);
                return;
            }

            _spawnFadeTween?.Kill();
            SetAlpha(0f);
            _spawnFadeTween = DOVirtual.Float(0f, 1f, spawnFadeDuration, SetAlpha)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => _spawnFadeTween = null)
                .OnKill(() => _spawnFadeTween = null);
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

            AudioManager.PlayOneShot("PieceClick", 0.5f);

            UpdateSortingOrders(s_nextDragSortingOrder++);

            _dragOffset = (Vector2)transform.position - pos;
            ApplyFullScale(dragScale);
            SetAlpha(dragAlpha);
        }

        public void OnInputDrag(Vector2 pos)
        {
            if (_isPlaced) return;
            
            transform.position = pos + _dragOffset;

            // グリッド上にゴーストプレビューを表示
            if (_gridView != null)
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

            if (_gridView == null) return;

            var gridPos = GetGridOrigin(pos + _dragOffset);

            if (_gridView.Grid.TryPlace(_shape, gridPos))
            {
                // 配置成功: ピースをグリッドのセル位置にスナップ
                AudioManager.PlayOneShot("PiecePlace", 0.5f);
                SnapToGrid(gridPos);
                _isPlaced = true;
                _placedGridOrigin = gridPos;

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
                    transform.localPosition = _spawnLocalPosition;
                    AudioManager.PlayOneShot("PieceCancel", 0.25f);
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
                c.a = _baseAlphaByRenderer.TryGetValue(sr, out var baseAlpha)
                    ? baseAlpha * alpha
                    : alpha;
                sr.color = c;
            }
        }

        private void UpdateSortingOrders(int baseSortingOrder)
        {
            foreach (var spriteRenderer in _spriteRenderers)
            {
                if (spriteRenderer == null)
                {
                    continue;
                }

                spriteRenderer.sortingOrder = spriteRenderer == _dishRenderer
                    ? baseSortingOrder
                    : baseSortingOrder + dirtySortingOrderOffset;
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
            var snappedPos = GetPieceWorldPosition(gridOrigin);

            _ghostObject.transform.position = new Vector3(snappedPos.x, snappedPos.y, transform.position.z);
        }

        /// <summary>
        /// ピースの半透明コピー（ゴースト）を作成する
        /// </summary>
        private void CreateGhost()
        {
            if (_shape == null) return;

            _ghostObject = _ghostFactory != null
                ? _ghostFactory.Create(_localCenter, previewAlpha)
                : new GameObject("GhostPreview");
        }

        private void ApplyBaseCellScale()
        {
            var geometry = CurrentGeometry;
            if (geometry == null) return;

            transform.localScale = new Vector3(
                geometry.Value.CellWorldSize.x,
                geometry.Value.CellWorldSize.y,
                1f
            );
        }

        private Sprite GetSelectedSprite()
        {
            return _selectedDishSprite != null ? _selectedDishSprite : _shape.GetEffectiveSprite();
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
            if (_shape == null) return Vector2Int.zero;

            var geometry = CurrentGeometry;
            if (geometry == null) return Vector2Int.zero;
            return new PuzzlePiecePlacement(_shape, geometry.Value).GetGridOrigin(pieceWorldPos);
        }

        /// <summary>
        /// ピースをグリッドの配置位置にスナップする
        /// </summary>
        private void SnapToGrid(Vector2Int gridOrigin)
        {
            var snappedPos = GetPieceWorldPosition(gridOrigin);
            transform.position = new Vector3(snappedPos.x, snappedPos.y, transform.position.z);
        }

        private Vector2 GetPieceWorldPosition(Vector2Int gridOrigin)
        {
            var placement = CurrentPlacement;
            return placement != null ? placement.Value.GetPieceWorldPosition(gridOrigin) : (Vector2)transform.position;
        }
    }
}
