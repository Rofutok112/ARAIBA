using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    internal sealed class PuzzlePieceVisualBuilder
    {
        private readonly PuzzlePieceShape _shape;
        private readonly Sprite _sprite;
        private readonly Transform _root;

        public PuzzlePieceVisualBuilder(PuzzlePieceShape shape, Sprite sprite, Transform root)
        {
            _shape = shape;
            _sprite = sprite;
            _root = root;
        }

        public PuzzlePieceVisualResult Build(float previewAlpha, int sortingOrder, Vector2 cellWorldSize)
        {
            var filledCells = _shape != null ? _shape.GetFilledCells() : null;
            if (_shape == null || filledCells == null) return default;

            var center = CalculateCenter(filledCells);
            var spriteRenderers = new List<SpriteRenderer>();
            var dishRenderer = CreateDishOverlay(center, sortingOrder, spriteRenderers);
            var collider = EnsureCollider(filledCells, center);
            var ghostFactory = new PuzzlePieceGhostFactory(_shape, _sprite, cellWorldSize);

            return new PuzzlePieceVisualResult(center, collider, dishRenderer, spriteRenderers, ghostFactory, previewAlpha);
        }

        private SpriteRenderer CreateDishOverlay(Vector2 center, int sortingOrder, List<SpriteRenderer> spriteRenderers)
        {
            if (_sprite == null) return null;

            var dishObj = new GameObject("DishOverlay");
            var dishTransform = dishObj.transform;
            dishTransform.SetParent(_root, false);
            var sr = dishObj.AddComponent<SpriteRenderer>();
            
            var bboxCenter = new Vector3(
                (_shape.Width - 1) / 2f - center.x,
                (_shape.Height - 1) / 2f - center.y,
                0f
            );
            dishTransform.localPosition = bboxCenter;
            
            sr.sortingLayerName = "Dish";
            sr.sprite = _sprite;
            sr.color = Color.white;
            sr.sortingOrder = sortingOrder;
            spriteRenderers.Add(sr);

            var spriteSize = _sprite.bounds.size;
            dishTransform.localScale = new Vector3(
                _shape.Width / spriteSize.x,
                _shape.Height / spriteSize.y,
                1f
            );

            return sr;
        }

        private BoxCollider2D EnsureCollider(Vector2Int[] filledCells, Vector2 center)
        {
            var existingCollider = _root.GetComponent<Collider2D>();
            if (existingCollider != null)
            {
                Object.Destroy(existingCollider);
            }

            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var cell in filledCells)
            {
                var x = cell.x - center.x;
                var y = cell.y - center.y;
                minX = Mathf.Min(minX, x - 0.5f);
                minY = Mathf.Min(minY, y - 0.5f);
                maxX = Mathf.Max(maxX, x + 0.5f);
                maxY = Mathf.Max(maxY, y + 0.5f);
            }

            var boxCollider = _root.gameObject.AddComponent<BoxCollider2D>();
            boxCollider.offset = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            boxCollider.size = new Vector2(maxX - minX, maxY - minY);
            return boxCollider;
        }

        private static Vector2 CalculateCenter(Vector2Int[] filledCells)
        {
            if (filledCells.Length == 0) return Vector2.zero;

            var sum = Vector2.zero;
            foreach (var cell in filledCells)
            {
                sum += new Vector2(cell.x, cell.y);
            }

            return sum / filledCells.Length;
        }
    }

    internal readonly struct PuzzlePieceVisualResult
    {
        public PuzzlePieceVisualResult(
            Vector2 localCenter,
            BoxCollider2D collider,
            SpriteRenderer dishRenderer,
            List<SpriteRenderer> spriteRenderers,
            PuzzlePieceGhostFactory ghostFactory,
            float previewAlpha)
        {
            LocalCenter = localCenter;
            Collider = collider;
            DishRenderer = dishRenderer;
            SpriteRenderers = spriteRenderers;
            GhostFactory = ghostFactory;
            PreviewAlpha = previewAlpha;
        }

        public Vector2 LocalCenter { get; }
        public BoxCollider2D Collider { get; }
        public SpriteRenderer DishRenderer { get; }
        public List<SpriteRenderer> SpriteRenderers { get; }
        public PuzzlePieceGhostFactory GhostFactory { get; }
        public float PreviewAlpha { get; }
    }

    internal sealed class PuzzlePieceGhostFactory
    {
        private readonly PuzzlePieceShape _shape;
        private readonly Sprite _sprite;
        private readonly Vector2 _cellWorldSize;

        public PuzzlePieceGhostFactory(PuzzlePieceShape shape, Sprite sprite, Vector2 cellWorldSize)
        {
            _shape = shape;
            _sprite = sprite;
            _cellWorldSize = cellWorldSize;
        }

        public GameObject Create(Vector2 localCenter, float previewAlpha)
        {
            var ghostRoot = new GameObject("GhostPreview");
            ghostRoot.transform.localScale = new Vector3(_cellWorldSize.x, _cellWorldSize.y, 1f);
            if (_shape == null || _sprite == null) return ghostRoot;

            var dishObj = new GameObject("GhostDishOverlay");
            dishObj.transform.SetParent(ghostRoot.transform, false);
            dishObj.transform.localPosition = new Vector3(
                (_shape.Width - 1) / 2f - localCenter.x,
                (_shape.Height - 1) / 2f - localCenter.y,
                0f
            );

            var sr = dishObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Dish";
            sr.sprite = _sprite;
            sr.color = new Color(1f, 1f, 1f, previewAlpha);
            sr.sortingOrder = 5;

            var spriteSize = _sprite.bounds.size;
            dishObj.transform.localScale = new Vector3(
                _shape.Width / spriteSize.x,
                _shape.Height / spriteSize.y,
                1f
            );

            return ghostRoot;
        }
    }
}
