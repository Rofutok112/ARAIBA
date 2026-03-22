using System;
using System.Collections.Generic;
using Projects.Scripts.Control;
using UnityEngine;

namespace Projects.Scripts.Sorting
{
    /// <summary>
    /// 選別画面でD&Dできる皿。
    /// </summary>
    public class SortingDish : MonoBehaviour, IInputHandler
    {
        private string _shapeKey;
        private SpriteRenderer _spriteRenderer;
        private Vector2 _dragOffset;
        private Vector2 _spawnPosition;
        private Action<SortingDish> _onSortedCallback;
        private IReadOnlyList<SortingTarget> _targets;
        private float _targetRadius;

        public string ShapeKey => _shapeKey;

        public void Initialize(
            string shapeKey,
            Sprite sprite,
            int shapeWidth,
            int shapeHeight,
            float cellSize,
            IReadOnlyList<SortingTarget> targets,
            float targetRadius,
            Action<SortingDish> onSorted)
        {
            _shapeKey = shapeKey;
            _targets = targets;
            _targetRadius = targetRadius;
            _onSortedCallback = onSorted;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            _spriteRenderer.sprite = sprite;
            _spriteRenderer.sortingOrder = 10;

            // スプライトをセルサイズに合わせてスケーリング
            if (sprite != null)
            {
                var spriteSize = sprite.bounds.size;
                var targetWidth = shapeWidth * cellSize;
                var targetHeight = shapeHeight * cellSize;
                transform.localScale = new Vector3(
                    targetWidth / spriteSize.x,
                    targetHeight / spriteSize.y,
                    1f
                );
            }

            _spawnPosition = transform.position;

            if (GetComponent<Collider2D>() == null)
            {
                gameObject.AddComponent<BoxCollider2D>();
            }
        }

        public void OnInputBegin(Vector2 pos)
        {
            _dragOffset = (Vector2)transform.position - pos;
        }

        public void OnInputDrag(Vector2 pos)
        {
            transform.position = pos + _dragOffset;
        }

        public void OnInputEnd(Vector2 pos)
        {
            var dropPos = pos + _dragOffset;
            var target = FindClosestTarget(dropPos);

            if (target != null && target.ShapeKey == _shapeKey)
            {
                _onSortedCallback?.Invoke(this);
                Destroy(gameObject);
            }
            else
            {
                transform.position = _spawnPosition;
            }
        }

        private SortingTarget FindClosestTarget(Vector2 worldPos)
        {
            if (_targets == null) return null;

            SortingTarget closest = null;
            var closestDist = float.MaxValue;

            foreach (var target in _targets)
            {
                if (target == null) continue;

                var dist = Vector2.Distance(worldPos, target.transform.position);
                if (dist < closestDist && dist <= _targetRadius)
                {
                    closestDist = dist;
                    closest = target;
                }
            }

            return closest;
        }
    }
}
