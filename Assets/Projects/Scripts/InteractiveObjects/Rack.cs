using System;
using Projects.Scripts.Control;
using UnityEngine;

namespace Projects.Scripts.InteractiveObjects
{
    public enum RackState
    {
        Empty,
        Packing,
        Packed,
        Washing,
        Washed,
        Sorting,
    }

    public class Rack : MonoBehaviour, IInputHandler
    {
        [Header("Sprites")]
        [Tooltip("空のラックのスプライト")]
        [SerializeField] private Sprite emptySprite;

        [Tooltip("皿が入ったラックのスプライト")]
        [SerializeField] private Sprite filledSprite;

        private SpriteRenderer _spriteRenderer;
        private RackState _state = RackState.Empty;
        private RackPlacementData _placementData;

        public RackState State => _state;
        public RackPlacementData PlacementData => _placementData;

        public event Action<Rack> OnClicked;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            ApplySprite();
        }

        public void SetState(RackState newState)
        {
            _state = newState;
            ApplySprite();
        }

        public void SavePlacementData(RackPlacementData data)
        {
            _placementData = data;
        }

        public void ClearPlacementData()
        {
            _placementData = null;
        }

        private void ApplySprite()
        {
            if (_spriteRenderer == null) return;

            _spriteRenderer.sprite = _state == RackState.Empty ? emptySprite : filledSprite;
        }

        public void OnInputBegin(Vector2 pos)
        {
            OnClicked?.Invoke(this);
        }

        public void OnInputDrag(Vector2 pos) { }
        public void OnInputEnd(Vector2 pos) { }
    }
}
