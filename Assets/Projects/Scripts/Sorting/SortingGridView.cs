using Projects.Scripts.Common;
using UnityEngine;

namespace Projects.Scripts.Sorting
{
    /// <summary>
    /// 選別画面用のグリッド表示。
    /// グリッドセルの描画を担当する。
    /// </summary>
    public class SortingGridView : BaseGridView
    {
        protected override string GetCellObjectPrefix()
        {
            return "SortCell";
        }

#if UNITY_EDITOR
        internal void DrawGridBoundsPreview()
        {
            DrawGridBoundsGizmo();
        }
#endif
    }
}
