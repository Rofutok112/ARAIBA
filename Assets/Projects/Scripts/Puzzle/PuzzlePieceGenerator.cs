using System;
using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    public enum SlotLayoutDirection
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// パズルピースを生成し、各スロットを同一形状のスタックとして保持する。
    /// ピースは時間経過で1枚ずつ補充され、最大枚数まで積み上がる。
    /// </summary>
    public class PuzzlePieceGenerator : MonoBehaviour
    {
        [Header("Piece Settings")]
        [Tooltip("形状プール（ScriptableObject）")]
        [SerializeField] private ShapePool shapePool;

        [Tooltip("ピースのプレハブ（PuzzlePieceコンポーネント付き）")]
        [SerializeField] private PuzzlePiece piecePrefab;

        [Tooltip("配置先のグリッドビュー")]
        [SerializeField] private PuzzleGridView gridView;

        [Header("Slot Settings")]
        [Tooltip("スロットの配置間隔")]
        [SerializeField] private float slotSpacing = 3f;

        [Tooltip("1段あたりに並べるスロット数。超えた分は次の段に折り返す")]
        [SerializeField, Min(1)] private int slotsPerLine = 4;

        [Tooltip("段間の配置間隔")]
        [SerializeField] private float lineSpacing = 3f;

        [Tooltip("スロットを配置する基準位置からのオフセット（ローカル座標）")]
        [SerializeField] private Vector3 slotAreaOffset = new(0f, -5f, 0f);

        [Tooltip("スロットの並び方向")]
        [SerializeField] private SlotLayoutDirection slotDirection = SlotLayoutDirection.Horizontal;

        [Header("Refill Settings")]
        [SerializeField] private PuzzlePieceRefillSettings refillSettings;

        [Tooltip("同じスロット内で重ね表示するときの1枚ごとのオフセット")]
        [SerializeField] private Vector3 stackPieceOffset = new(0.04f, 0.04f, 0f);

        private readonly PuzzlePieceSlotRegistry _slotRegistry = new();

        public event Action OnAllPiecesPlaced;
        public event Action OnPiecesGenerated;
        public event Action<PuzzlePiece> OnPiecePlacedOnGrid;
        public event Action<float> OnTraySubmitted;

        public IReadOnlyList<PuzzlePiece> Slots => _slotRegistry.Slots;

        public int RemainingPieceCount
        {
            get
            {
                return _slotRegistry.RemainingPieceCount;
            }
        }

        public float CurrentOccupancy
        {
            get
            {
                if (gridView == null || gridView.Grid == null) return 0f;
                return gridView.Grid.GetOccupancy();
            }
        }

        private PuzzlePieceSlotPresenter SlotPresenter => new(
            transform,
            slotAreaOffset,
            slotSpacing,
            slotsPerLine,
            lineSpacing,
            slotDirection,
            stackPieceOffset
        );

        private PuzzlePieceFactory PieceFactory => new(transform, piecePrefab, gridView, HandlePiecePlaced);

        private void Start()
        {
            GenerateAllPieces();
        }

        private void Update()
        {
            ProcessTimedRefill();
        }

        private void InitializeSlots(int slotCount)
        {
            _slotRegistry.Initialize(slotCount);
        }

        public void GenerateAllPieces()
        {
            var availableShapes = GetAvailableShapes();
            if (availableShapes.Count == 0)
            {
                Debug.LogWarning("[PuzzlePieceGenerator] 形状プールが空です。インスペクターで形状を設定してください。");
                return;
            }

            if (piecePrefab == null)
            {
                Debug.LogWarning("[PuzzlePieceGenerator] ピースプレハブが設定されていません。");
                return;
            }

            if (refillSettings == null)
            {
                Debug.LogWarning("[PuzzlePieceGenerator] Refill Settings が設定されていません。");
                return;
            }

            if (!_slotRegistry.MatchesSlotCount(availableShapes.Count))
            {
                InitializeSlots(availableShapes.Count);
            }

            ClearAllPieces();
            var initialCount = refillSettings.InitialPiecesPerSlot;

            for (var i = 0; i < availableShapes.Count; i++)
            {
                _slotRegistry.AssignShape(i, availableShapes[i]);

                if (availableShapes[i] == null)
                {
                    Debug.LogWarning($"[PuzzlePieceGenerator] スロット{i}に適切な形状が見つかりませんでした。");
                    continue;
                }

                RefillSlot(i, initialCount, false);
            }

            OnPiecesGenerated?.Invoke();
        }

        private void ProcessTimedRefill()
        {
            if (_slotRegistry.SlotCount == 0) return;

            var generatedAny = false;
            for (var i = 0; i < _slotRegistry.SlotCount; i++)
            {
                if (!_slotRegistry.TryGetState(i, out var state) || state.shape == null) continue;

                if (state.Pieces.Count >= refillSettings.MaxPiecesPerSlot)
                {
                    PuzzlePieceRefillScheduler.MarkFull(ref state.refillState);
                    continue;
                }

                PuzzlePieceRefillScheduler.EnsureScheduled(ref state.refillState, state.shape, Time.time);

                while (PuzzlePieceRefillScheduler.ShouldRefill(state.refillState, state.Pieces.Count, refillSettings.MaxPiecesPerSlot, Time.time))
                {
                    RefillSlot(i, 1, true);
                    PuzzlePieceRefillScheduler.Advance(ref state.refillState, state.shape);
                    generatedAny = true;
                }

                if (state.Pieces.Count >= refillSettings.MaxPiecesPerSlot)
                {
                    PuzzlePieceRefillScheduler.MarkFull(ref state.refillState);
                }
            }

            if (generatedAny)
            {
                OnPiecesGenerated?.Invoke();
            }
        }

        private List<PuzzlePieceShape> GetAvailableShapes()
        {
            var uniqueShapes = new List<PuzzlePieceShape>();
            if (shapePool == null || shapePool.Shapes == null) return uniqueShapes;

            foreach (var shape in shapePool.Shapes)
            {
                if (shape == null || uniqueShapes.Contains(shape)) continue;
                uniqueShapes.Add(shape);
            }

            return uniqueShapes;
        }

        private void RefillSlot(int slotIndex, int amount, bool playSpawnFade)
        {
            if (!_slotRegistry.TryGetState(slotIndex, out var state) || state.shape == null) return;

            var spawnCount = Mathf.Min(amount, refillSettings.MaxPiecesPerSlot - state.Pieces.Count);
            var slotLocalPosition = SlotPresenter.GetSlotLocalPosition(slotIndex, _slotRegistry.SlotCount);
            for (var i = 0; i < spawnCount; i++)
            {
                var piece = PieceFactory.Create(state.shape, slotLocalPosition);
                if (playSpawnFade)
                {
                    piece.PlaySpawnFade();
                }
                _slotRegistry.RegisterPiece(slotIndex, piece);
            }

            SlotPresenter.RefreshSlotVisuals(_slotRegistry, slotIndex);
        }

        private void HandlePiecePlaced(PuzzlePiece piece)
        {
            OnPiecePlacedOnGrid?.Invoke(piece);

            if (!_slotRegistry.TryTakePiece(piece, out var slotIndex, out var state))
            {
                return;
            }

            if (state.Pieces.Count < refillSettings.MaxPiecesPerSlot)
            {
                PuzzlePieceRefillScheduler.EnsureScheduled(ref state.refillState, state.shape, Time.time);
            }

            SlotPresenter.RefreshSlotVisuals(_slotRegistry, slotIndex);

            if (RemainingPieceCount == 0)
            {
                OnAllPiecesPlaced?.Invoke();
            }
        }

        private void CleanupPlacedPieces()
        {
            var pieces = GetComponentsInChildren<PuzzlePiece>();
            foreach (var piece in pieces)
            {
                if (piece != null && piece.IsPlaced)
                {
                    Destroy(piece.gameObject);
                }
            }
        }

        public void ClearAllPieces()
        {
            _slotRegistry.ClearTrackedPieces();
        }

        public void ResetAndRegenerate()
        {
            ResetPuzzle();
        }

        public void ResetPuzzle()
        {
            if (gridView != null && gridView.Grid != null)
            {
                gridView.Grid.Clear();
            }

            CleanupPlacedPieces();
            ClearAllPieces();
            GenerateAllPieces();
        }

        public void ResetGridOnly()
        {
            if (gridView != null && gridView.Grid != null)
            {
                gridView.Grid.Clear();
            }

            CleanupPlacedPieces();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            slotsPerLine = Mathf.Max(1, slotsPerLine);
        }

        private void OnDrawGizmosSelected()
        {
            var slotCount = GetAvailableShapes().Count;
            if (slotCount <= 0) return;

            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.5f);

            for (var i = 0; i < slotCount; i++)
            {
                var pos = SlotPresenter.GetSlotPreviewWorldPosition(i, slotCount);
                Gizmos.DrawSphere(pos, 0.1f);

                var topPos = pos + stackPieceOffset * Mathf.Max((refillSettings != null ? refillSettings.InitialPiecesPerSlot : 1) - 1, 0);
                Gizmos.DrawLine(pos, topPos);
            }
        }
#endif
    }
}
