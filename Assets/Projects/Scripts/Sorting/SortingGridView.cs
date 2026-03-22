using System.Collections.Generic;
using Projects.Scripts.Puzzle;
using UnityEngine;

// SlotLayoutDirectionはPuzzlePieceGenerator内で定義済み

namespace Projects.Scripts.Sorting
{
    /// <summary>
    /// 選別画面用のグリッド表示。
    /// グリッドセルの描画と、Shape Poolに基づくドロップターゲットの配置を担当する。
    /// </summary>
    public class SortingGridView : MonoBehaviour
    {
        [Header("Grid Settings")]
        [Tooltip("グリッドの一辺のセル数")]
        [SerializeField] private int gridSize = 8;

        [Tooltip("1セルのワールド空間サイズ")]
        [SerializeField] private float cellSize = 1f;

        [Header("Cell Visuals")]
        [Tooltip("セルに使用するスプライト（正方形推奨）")]
        [SerializeField] private Sprite cellSprite;

        [Tooltip("セルの色")]
        [SerializeField] private Color cellColor = new(0.9f, 0.9f, 0.9f, 1f);

        [Header("Target Settings")]
        [Tooltip("ターゲットのプレハブ")]
        [SerializeField] private SortingTarget targetPrefab;

        [Tooltip("形状プール（パズルと共有）")]
        [SerializeField] private ShapePool shapePool;

        [Tooltip("ターゲットを配置する基準位置からのオフセット（ローカル座標）")]
        [SerializeField] private Vector3 targetAreaOffset = new(0f, -5f, 0f);

        [Tooltip("ターゲット間の間隔")]
        [SerializeField] private float targetSpacing = 2f;

        [Tooltip("1段あたりに並べるターゲット数。超えた分は次の段に折り返す")]
        [SerializeField, Min(1)] private int targetsPerLine = 4;

        [Tooltip("段間の配置間隔")]
        [SerializeField] private float lineSpacing = 3f;

        [Tooltip("ターゲットの並び方向")]
        [SerializeField] private SlotLayoutDirection targetDirection = SlotLayoutDirection.Horizontal;

        [Tooltip("ターゲットの半透明度")]
        [SerializeField, Range(0f, 1f)] private float targetAlpha = 0.3f;

        [Tooltip("ターゲットの判定半径")]
        [SerializeField, Min(0.1f)] private float targetRadius = 1.5f;

        private SpriteRenderer[,] _cellRenderers;
        private readonly List<SortingTarget> _activeTargets = new();

        public int GridSize => gridSize;
        public float CellSize => cellSize;
        public IReadOnlyList<SortingTarget> ActiveTargets => _activeTargets;
        public float TargetRadius => targetRadius;

        private void Awake()
        {
            CreateGrid();
            CreateTargets();
        }

        private void CreateGrid()
        {
            _cellRenderers = new SpriteRenderer[gridSize, gridSize];

            var gridOffset = new Vector2(
                -(gridSize * cellSize) / 2f + cellSize / 2f,
                -(gridSize * cellSize) / 2f + cellSize / 2f
            );

            for (var y = 0; y < gridSize; y++)
            {
                for (var x = 0; x < gridSize; x++)
                {
                    var cellObj = new GameObject($"SortCell_{x}_{y}");
                    cellObj.transform.SetParent(transform, false);
                    cellObj.transform.localPosition = new Vector3(
                        gridOffset.x + x * cellSize,
                        gridOffset.y + y * cellSize,
                        0
                    );

                    var sr = cellObj.AddComponent<SpriteRenderer>();
                    sr.sprite = cellSprite;
                    sr.color = cellColor;

                    if (cellSprite != null)
                    {
                        var spriteSize = cellSprite.bounds.size;
                        var scale = cellSize / Mathf.Max(spriteSize.x, spriteSize.y);
                        cellObj.transform.localScale = Vector3.one * scale;
                    }

                    _cellRenderers[x, y] = sr;
                }
            }
        }

        /// <summary>
        /// Shape Poolからターゲットを事前生成する
        /// </summary>
        private void CreateTargets()
        {
            if (shapePool == null || shapePool.Shapes == null || shapePool.Shapes.Count == 0 || targetPrefab == null) return;

            var count = shapePool.Shapes.Count;

            for (var i = 0; i < count; i++)
            {
                var shape = shapePool.Shapes[i];
                if (shape == null) continue;

                var localOffset = targetAreaOffset + CalculateTargetOffset(i, count);
                var worldPos = transform.TransformPoint(localOffset);

                var target = Instantiate(targetPrefab, worldPos, Quaternion.identity, transform);
                target.name = $"SortingTarget_{shape.name}";
                target.Initialize(
                    shape.name,
                    shape.DishTypeName,
                    shape.GetEffectiveSprite(),
                    targetAlpha,
                    shape.Width,
                    shape.Height,
                    cellSize
                );
                _activeTargets.Add(target);
            }
        }

        private Vector3 CalculateTargetOffset(int index, int totalCount)
        {
            if (totalCount <= 0) return Vector3.zero;

            var effectivePerLine = Mathf.Max(1, targetsPerLine);
            var lineIndex = index / effectivePerLine;
            var indexInLine = index % effectivePerLine;
            var lineWidth = (effectivePerLine - 1) * targetSpacing;
            var lineStart = -lineWidth / 2f;

            if (targetDirection == SlotLayoutDirection.Horizontal)
            {
                return new Vector3(
                    lineStart + indexInLine * targetSpacing,
                    -lineIndex * lineSpacing,
                    0f
                );
            }

            return new Vector3(
                lineIndex * lineSpacing,
                -lineStart - indexInLine * targetSpacing,
                0f
            );
        }

        /// <summary>
        /// グリッド座標をワールド座標（セル中心）に変換する
        /// </summary>
        public Vector2 GridToWorldPosition(Vector2Int gridPos)
        {
            var gridOffset = new Vector2(
                -(gridSize * cellSize) / 2f + cellSize / 2f,
                -(gridSize * cellSize) / 2f + cellSize / 2f
            );

            var localPos = new Vector3(
                gridOffset.x + gridPos.x * cellSize,
                gridOffset.y + gridPos.y * cellSize,
                0
            );

            return transform.TransformPoint(localPos);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // ランタイム中：実際のターゲット位置を表示
            if (_activeTargets is { Count: > 0 })
            {
                Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.5f);
                foreach (var target in _activeTargets)
                {
                    if (target == null) continue;
                    Gizmos.DrawWireSphere(target.transform.position, targetRadius);
                    Gizmos.DrawSphere(target.transform.position, 0.1f);
                }
                return;
            }

            // エディタ上：Shape Pool数分のプレビュー位置を表示
            var count = shapePool != null && shapePool.Shapes != null ? shapePool.Shapes.Count : 1;
            Gizmos.color = new Color(0.8f, 0.6f, 0.2f, 0.5f);
            for (var i = 0; i < count; i++)
            {
                var localOffset = targetAreaOffset + CalculateTargetOffset(i, count);
                var pos = transform.TransformPoint(localOffset);
                Gizmos.DrawWireSphere(pos, targetRadius);
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }
#endif
    }
}
