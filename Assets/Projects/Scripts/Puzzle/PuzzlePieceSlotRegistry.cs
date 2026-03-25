using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    internal sealed class PuzzlePieceSlotRegistry
    {
        private PuzzlePiece[] _slots = System.Array.Empty<PuzzlePiece>();
        private PuzzlePieceSlotState[] _slotStates = System.Array.Empty<PuzzlePieceSlotState>();
        private readonly Dictionary<PuzzlePiece, int> _pieceToSlotIndex = new();

        public IReadOnlyList<PuzzlePiece> Slots => _slots;
        public int SlotCount => _slotStates.Length;

        public int RemainingPieceCount
        {
            get
            {
                var count = 0;
                foreach (var state in _slotStates)
                {
                    count += state?.Pieces.Count ?? 0;
                }

                return count;
            }
        }

        public bool MatchesSlotCount(int slotCount)
        {
            return _slotStates != null && _slotStates.Length == slotCount;
        }

        public void Initialize(int slotCount)
        {
            _slots = new PuzzlePiece[slotCount];
            _slotStates = new PuzzlePieceSlotState[slotCount];

            for (var i = 0; i < slotCount; i++)
            {
                _slotStates[i] = new PuzzlePieceSlotState();
            }
        }

        public bool TryGetState(int slotIndex, out PuzzlePieceSlotState state)
        {
            state = null;
            if (_slotStates == null || slotIndex < 0 || slotIndex >= _slotStates.Length) return false;

            state = _slotStates[slotIndex];
            return state != null;
        }

        public void AssignShape(int slotIndex, PuzzlePieceShape shape)
        {
            if (!TryGetState(slotIndex, out var state)) return;

            state.shape = shape;
            PuzzlePieceRefillScheduler.Reset(ref state.refillState);
        }

        public void RegisterPiece(int slotIndex, PuzzlePiece piece)
        {
            if (piece == null || !TryGetState(slotIndex, out var state)) return;

            state.Pieces.Add(piece);
            _pieceToSlotIndex[piece] = slotIndex;
        }

        public bool TryTakePiece(PuzzlePiece piece, out int slotIndex, out PuzzlePieceSlotState state)
        {
            slotIndex = -1;
            state = null;

            if (piece == null || !_pieceToSlotIndex.TryGetValue(piece, out slotIndex)) return false;
            if (!TryGetState(slotIndex, out state)) return false;

            _pieceToSlotIndex.Remove(piece);
            state.Pieces.Remove(piece);
            return true;
        }

        public void SetTopPiece(int slotIndex, PuzzlePiece piece)
        {
            if (_slots == null || slotIndex < 0 || slotIndex >= _slots.Length) return;
            _slots[slotIndex] = piece;
        }

        public void ClearTrackedPieces()
        {
            foreach (var state in _slotStates)
            {
                if (state == null) continue;

                foreach (var piece in state.Pieces)
                {
                    if (piece != null)
                    {
                        Object.Destroy(piece.gameObject);
                    }
                }

                state.Pieces.Clear();
                state.shape = null;
                PuzzlePieceRefillScheduler.Reset(ref state.refillState);
            }

            _pieceToSlotIndex.Clear();

            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i] = null;
            }
        }
    }
}
