using TMPro;
using UnityEngine;

namespace Projects.Scripts.Sorting
{
    /// <summary>
    /// 選別画面のドロップ先。Shape（形状）ごとに1つ配置される。
    /// </summary>
    public class SortingTarget : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TextMeshPro label;

        private string _shapeKey;
        private Vector3 _stackPieceOffset;
        private int _stackBaseSortingOrder;
        private int _stackedDishCount;

        public string ShapeKey => _shapeKey;

        public void Initialize(
            string shapeKey,
            string dishTypeName,
            Sprite sprite,
            float alpha,
            int shapeWidth,
            int shapeHeight,
            Vector2 cellSize,
            Vector3 stackPieceOffset,
            int stackBaseSortingOrder)
        {
            _shapeKey = shapeKey;
            _stackPieceOffset = stackPieceOffset;
            _stackBaseSortingOrder = stackBaseSortingOrder;
            _stackedDishCount = 0;
            SortingTargetVisualBuilder.Build(
                spriteRenderer,
                label,
                shapeKey,
                dishTypeName,
                sprite,
                alpha,
                shapeWidth,
                shapeHeight,
                cellSize
            );
        }

        public void StackDish(Transform dishTransform, SpriteRenderer dishRenderer)
        {
            if (dishTransform == null) return;

            dishTransform.position = transform.TransformPoint(_stackPieceOffset * _stackedDishCount);

            if (dishRenderer != null)
            {
                dishRenderer.sortingOrder = _stackBaseSortingOrder + _stackedDishCount;
            }

            _stackedDishCount++;
        }
    }
}
