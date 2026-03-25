using UnityEngine;

namespace Projects.Scripts.Sorting
{
    internal static class SortingDishVisualBuilder
    {
        public static SpriteRenderer Build(Transform root, Sprite sprite, int shapeWidth, int shapeHeight, Vector2 cellSize)
        {
            var spriteRenderer = root.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = root.gameObject.AddComponent<SpriteRenderer>();
            }
            
            spriteRenderer.sortingLayerName = "Dish";
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 0;

            if (sprite != null)
            {
                var spriteSize = sprite.bounds.size;
                var targetWidth = shapeWidth * cellSize.x;
                var targetHeight = shapeHeight * cellSize.y;
                root.localScale = new Vector3(
                    targetWidth / spriteSize.x,
                    targetHeight / spriteSize.y,
                    1f
                );
            }

            var boxCollider = root.GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                boxCollider = root.gameObject.AddComponent<BoxCollider2D>();
            }

            if (sprite != null)
            {
                boxCollider.offset = sprite.bounds.center;
                boxCollider.size = sprite.bounds.size;
            }
            else
            {
                boxCollider.offset = Vector2.zero;
                boxCollider.size = new Vector2(shapeWidth * cellSize.x, shapeHeight * cellSize.y);
            }

            return spriteRenderer;
        }
    }
}
