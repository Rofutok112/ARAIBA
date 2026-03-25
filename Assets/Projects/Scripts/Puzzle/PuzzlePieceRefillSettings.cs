using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    [CreateAssetMenu(fileName = "PuzzlePieceRefillSettings", menuName = "Puzzle/Refill Settings")]
    public class PuzzlePieceRefillSettings : ScriptableObject
    {
        private const int MaxSupportedOrderInLayer = 20;

        [SerializeField, Range(1, MaxSupportedOrderInLayer)] private int initialPiecesPerSlot = 5;
        [SerializeField, Range(1, MaxSupportedOrderInLayer)] private int maxPiecesPerSlot = MaxSupportedOrderInLayer;

        public int InitialPiecesPerSlot => Mathf.Clamp(initialPiecesPerSlot, 1, MaxPiecesPerSlot);
        public int MaxPiecesPerSlot => Mathf.Clamp(maxPiecesPerSlot, 1, MaxSupportedOrderInLayer);

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxPiecesPerSlot = Mathf.Clamp(maxPiecesPerSlot, 1, MaxSupportedOrderInLayer);
            initialPiecesPerSlot = Mathf.Clamp(initialPiecesPerSlot, 1, maxPiecesPerSlot);
        }
#endif
    }
}
