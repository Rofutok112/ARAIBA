using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    internal sealed class PuzzlePieceSlotPresenter
    {
        private readonly PuzzlePieceSlotLayout _layout;
        private readonly Transform _root;
        private readonly Vector3 _stackPieceOffset;

        public PuzzlePieceSlotPresenter(
            Transform root,
            Vector3 slotAreaOffset,
            float slotSpacing,
            int slotsPerLine,
            float lineSpacing,
            SlotLayoutDirection slotDirection,
            Vector3 stackPieceOffset)
        {
            _root = root;
            _layout = new PuzzlePieceSlotLayout(root, slotAreaOffset, slotSpacing, slotsPerLine, lineSpacing, slotDirection);
            _stackPieceOffset = stackPieceOffset;
        }

        public Vector3 GetSlotLocalPosition(int slotIndex, int slotCount)
        {
            return _layout.GetSlotLocalPosition(slotIndex, slotCount);
        }

        public Vector3 GetSlotPreviewWorldPosition(int slotIndex, int slotCount)
        {
            return _root.TransformPoint(GetSlotLocalPosition(slotIndex, slotCount));
        }

        public void RefreshSlotVisuals(PuzzlePieceSlotRegistry registry, int slotIndex)
        {
            if (registry == null || !registry.TryGetState(slotIndex, out var state)) return;

            var slotLocalPosition = GetSlotLocalPosition(slotIndex, registry.SlotCount);
            for (var i = 0; i < state.Pieces.Count; i++)
            {
                var piece = state.Pieces[i];
                if (piece == null) continue;

                var localPosition = slotLocalPosition + _stackPieceOffset * i;
                var isTopPiece = i == state.Pieces.Count - 1;
                piece.ConfigureStackPresentationLocal(localPosition, i, isTopPiece);
            }

            registry.SetTopPiece(slotIndex, state.Pieces.Count > 0 ? state.Pieces[^1] : null);
        }
    }
}
