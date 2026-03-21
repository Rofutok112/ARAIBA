using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    /// <summary>
    /// PuzzleGridの表示を管理するMonoBehaviour。
    /// グリッドのセルをSpriteRendererで表示する。
    /// </summary>
    public class PuzzleGridView : MonoBehaviour
    {
        [Header("Grid Settings")] [Tooltip("グリッドの一辺のセル数")] [SerializeField]
        private int gridSize = 8;

        [Tooltip("1セルのワールド空間サイズ")] [SerializeField]
        private float cellSize = 1f;

        [Header("Cell Visuals")] [Tooltip("セルに使用するスプライト（正方形推奨）")] [SerializeField]
        private Sprite cellSprite;

        [Tooltip("セルの色")] [SerializeField] private Color cellColor = new(0.9f, 0.9f, 0.9f, 1f);

        private SpriteRenderer[,] _cellRenderers;

        /// <summary>
        /// パズルグリッドのデータへのアクセス
        /// </summary>
        public PuzzleGrid Grid { get; private set; }

        /// <summary>
        /// 1セルのワールド空間サイズ
        /// </summary>
        public float CellSize => cellSize;

        public int GridSize => gridSize;
        public Sprite CellSprite => cellSprite;
        public Color CellColor => cellColor;

        /// <summary>
        /// グリッド全体のワールド空間サイズ
        /// </summary>
        public float GridWorldSize => gridSize * cellSize;

        private void Awake()
        {
            Grid = new PuzzleGrid(gridSize);
            CreateGridVisuals();
        }

        /// <summary>
        /// グリッドのセルSpriteRendererを生成する
        /// </summary>
        private void CreateGridVisuals()
        {
            _cellRenderers = new SpriteRenderer[gridSize, gridSize];

            // グリッドの左下を基準とするオフセット
            var gridOffset = new Vector2(
                -(gridSize * cellSize) / 2f + cellSize / 2f,
                -(gridSize * cellSize) / 2f + cellSize / 2f
            );

            for (var y = 0; y < gridSize; y++)
            {
                for (var x = 0; x < gridSize; x++)
                {
                    var cellObj = new GameObject($"Cell_{x}_{y}");
                    cellObj.transform.SetParent(transform, false);
                    cellObj.transform.localPosition = new Vector3(
                        gridOffset.x + x * cellSize,
                        gridOffset.y + y * cellSize,
                        0
                    );

                    // スプライトのサイズをcellSizeに合わせる
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
        /// グリッドの表示をリセットする（全セルを通常の色に戻す）
        /// </summary>
        public void RefreshView()
        {
            for (var y = 0; y < gridSize; y++)
            {
                for (var x = 0; x < gridSize; x++)
                {
                    _cellRenderers[x, y].color = cellColor;
                }
            }
        }

        /// <summary>
        /// ワールド座標をグリッド座標に変換する
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector2 worldPos)
        {
            // グリッドのワールド空間でのオフセットを考慮
            var localPos = (Vector2)transform.InverseTransformPoint(worldPos);
            var gridOffset = new Vector2(
                -(gridSize * cellSize) / 2f,
                -(gridSize * cellSize) / 2f
            );

            var gx = Mathf.FloorToInt((localPos.x - gridOffset.x) / cellSize);
            var gy = Mathf.FloorToInt((localPos.y - gridOffset.y) / cellSize);

            return new Vector2Int(gx, gy);
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

        public void PlayOpeningAnimation()
        {
            
        }

        public void PlayClosingAnimation()
        {
            
        }
    }
}
