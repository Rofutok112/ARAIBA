using System;
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
        [Tooltip("ピースのセルに使用するスプライト")]
        [SerializeField] private Sprite cellSprite;

        [Tooltip("ピースの色")]
        [SerializeField] private Color pieceColor = new(0.2f, 0.5f, 0.9f, 1f);

        [Tooltip("ドラッグ中のスケール倍率")]
        [SerializeField] private float dragScale = 1.0f;

        private Vector2 _dragOffset;
        private Vector3 _originalScale;
        private Vector2 _spawnPosition;
        private bool _isPlaced;
        private Action<PuzzlePiece> _onPlacedCallback;

        /// <summary>
        /// このピースの形状データ
        /// </summary>
        public PuzzlePieceShape Shape => shape;

        /// <summary>
        /// グリッド上に配置済みかどうか
        /// </summary>
        public bool IsPlaced => _isPlaced;

        /// <summary>
        /// PuzzlePieceGeneratorから動的に初期化する
        /// </summary>
        /// <param name="pieceShape">ピースの形状データ</param>
        /// <param name="targetGridView">配置先のグリッドビュー</param>
        /// <param name="onPlaced">配置完了時のコールバック</param>
        public void Initialize(PuzzlePieceShape pieceShape, PuzzleGridView targetGridView, Action<PuzzlePiece> onPlaced = null)
        {
            shape = pieceShape;
            gridView = targetGridView;
            _onPlacedCallback = onPlaced;
        }

        private void Start()
        {
            _spawnPosition = transform.position;
            _originalScale = transform.localScale;
            CreatePieceVisuals();
        }

        /// <summary>
        /// ピースの形状に基づいてセルのSpriteRendererを生成する
        /// </summary>
        private void CreatePieceVisuals()
        {
            if (shape == null || cellSprite == null) return;

            var filledCells = shape.GetFilledCells();
            var cellSize = gridView != null ? gridView.CellSize : 1f;

            // ピースの中心を計算
            var center = CalculatePieceCenter(filledCells, cellSize);

            foreach (var cell in filledCells)
            {
                var cellObj = new GameObject($"PieceCell_{cell.x}_{cell.y}");
                cellObj.transform.SetParent(transform, false);
                cellObj.transform.localPosition = new Vector3(
                    cell.x * cellSize - center.x,
                    cell.y * cellSize - center.y,
                    0
                );

                var sr = cellObj.AddComponent<SpriteRenderer>();
                sr.sprite = cellSprite;
                sr.color = pieceColor;
                sr.sortingOrder = 10;

                // スプライトサイズをcellSizeに合わせる
                var spriteSize = cellSprite.bounds.size;
                var scale = cellSize / Mathf.Max(spriteSize.x, spriteSize.y);
                cellObj.transform.localScale = Vector3.one * (scale * 0.9f);
            }

            // ドラッグ検出用のColliderを追加
            EnsureCollider(filledCells, cellSize, center);
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

        public void OnInputBegin(Vector2 pos)
        {
            if (_isPlaced) return;

            _dragOffset = (Vector2)transform.position - pos;
            transform.localScale = _originalScale * dragScale;
        }

        public void OnInputDrag(Vector2 pos)
        {
            if (_isPlaced) return;

            transform.position = pos + _dragOffset;

            // グリッド上でのプレビュー表示
            if (gridView != null)
            {
                var gridPos = GetGridOrigin(pos + _dragOffset);
                gridView.ShowPreview(shape, gridPos);
            }
        }

        public void OnInputEnd(Vector2 pos)
        {
            if (_isPlaced) return;

            transform.localScale = _originalScale;

            if (gridView == null) return;

            var gridPos = GetGridOrigin(pos + _dragOffset);

            if (gridView.Grid.TryPlace(shape, gridPos))
            {
                // 配置成功: ピースをグリッドのセル位置にスナップ
                SnapToGrid(gridPos);
                _isPlaced = true;

                // Colliderを無効化（配置済みのピースはドラッグ不可）
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                // 配置完了コールバックを発火
                _onPlacedCallback?.Invoke(this);

                // ライン消去チェック TODO: 終了の自由化
                //gridView.Grid.ClearCompletedLines();
            }
            else
            {
                // 配置失敗: 元の位置に戻す
                transform.position = _spawnPosition;
                gridView.ClearPreview();
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