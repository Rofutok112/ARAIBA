using Projects.Scripts.Common;
using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    /// <summary>
    /// PuzzleGridの表示を管理するMonoBehaviour。
    /// グリッドのセルをSpriteRendererで表示する。
    /// </summary>
    public class PuzzleGridView : BaseGridView
    {
        /// <summary>
        /// パズルグリッドのデータへのアクセス
        /// </summary>
        public PuzzleGrid Grid { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Grid = new PuzzleGrid(GridSize);
        }

        /// <summary>
        /// グリッドの表示をリセットする（全セルを通常の色に戻す）
        /// </summary>
        public void RefreshView()
        {
            RefreshGridView();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawGridBoundsGizmo();
        }
#endif
    }
}
