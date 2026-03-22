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

        public string ShapeKey => _shapeKey;

        public void Initialize(string shapeKey, string dishTypeName, Sprite sprite, float alpha, int shapeWidth, int shapeHeight, float cellSize)
        {
            _shapeKey = shapeKey;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = new Color(1f, 1f, 1f, alpha);

                if (sprite != null)
                {
                    var spriteSize = sprite.bounds.size;
                    var targetWidth = shapeWidth * cellSize;
                    var targetHeight = shapeHeight * cellSize;
                    spriteRenderer.transform.localScale = new Vector3(
                        targetWidth / spriteSize.x,
                        targetHeight / spriteSize.y,
                        1f
                    );
                }
            }

            if (label != null)
            {
                label.text = dishTypeName ?? shapeKey;
            }
        }
    }
}
