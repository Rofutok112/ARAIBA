using System;
using Projects.Scripts.Audio;
using Projects.Scripts.Control;
using UnityEngine;

namespace Projects.Scripts.Sorting
{
    /// <summary>
    /// 選別画面でD&Dできる皿。
    /// </summary>
    public class SortingDish : MonoBehaviour, IInputHandler
    {
        private const int DragFrontSortingOrderStart = 1000;
        private static int s_nextDragSortingOrder = DragFrontSortingOrderStart;

        private string _shapeKey;
        private int _scorePoints;
        private Vector2 _dragOffset;
        private Vector2 _spawnPosition;
        private Action<SortingDish, int> _onSortedCallback;
        private SortingDropResolver _dropResolver;
        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;
        private bool _isSorted;

        public string ShapeKey => _shapeKey;

        public void Initialize(
            string shapeKey,
            int scorePoints,
            Sprite sprite,
            int shapeWidth,
            int shapeHeight,
            Vector2 cellSize,
            SortingDropResolver dropResolver,
            Action<SortingDish, int> onSorted)
        {
            _shapeKey = shapeKey;
            _scorePoints = scorePoints;
            _dropResolver = dropResolver;
            _onSortedCallback = onSorted;

            _spriteRenderer = SortingDishVisualBuilder.Build(transform, sprite, shapeWidth, shapeHeight, cellSize);
            _collider = GetComponent<Collider2D>();
            _spawnPosition = transform.position;
        }

        public void OnInputBegin(Vector2 pos)
        {
            if (_isSorted) return;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = s_nextDragSortingOrder++;
            }

            _dragOffset = (Vector2)transform.position - pos;
            AudioManager.PlayOneShot("PieceClick");
        }

        public void OnInputDrag(Vector2 pos)
        {
            if (_isSorted) return;
            transform.position = pos + _dragOffset;
        }

        public void OnInputEnd(Vector2 pos)
        {
            if (_isSorted) return;

            var dropPos = pos + _dragOffset;
            var target = _dropResolver != null ? _dropResolver.FindClosestTarget(dropPos) : null;

            if (target != null && target.ShapeKey == _shapeKey)
            {
                _isSorted = true;
                if (_collider != null)
                {
                    _collider.enabled = false;
                }

                target.StackDish(transform, _spriteRenderer);
                _onSortedCallback?.Invoke(this, _scorePoints);
                AudioManager.PlayOneShot("PiecePlace");
            }
            else
            {
                transform.position = _spawnPosition;
                AudioManager.PlayOneShot("PieceCancel");
            }
        }
    }
}
