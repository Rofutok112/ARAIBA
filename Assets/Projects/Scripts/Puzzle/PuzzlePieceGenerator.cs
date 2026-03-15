using System;
using System.Collections.Generic;
using Projects.Scripts.Audio;
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
        private const int MaxSupportedOrderInLayer = 20;

        [Serializable]
        private sealed class SlotState
        {
            public PuzzlePieceShape Shape;
            public readonly List<PuzzlePiece> Pieces = new();
            public float NextRefillTime = -1f;
        }

        [Header("Piece Settings")]
        [Tooltip("生成するピースの形状候補リスト")]
        [SerializeField] private PuzzlePieceShape[] shapePool;

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
        [Tooltip("各スロットの初期枚数")]
        [SerializeField, Range(1, MaxSupportedOrderInLayer)] private int initialPiecesPerSlot = 5;

        [Tooltip("各スロットの最大枚数")]
        [SerializeField, Range(1, MaxSupportedOrderInLayer)] private int maxPiecesPerSlot = MaxSupportedOrderInLayer;

        [Tooltip("同じスロット内で重ね表示するときの1枚ごとのオフセット")]
        [SerializeField] private Vector3 stackPieceOffset = new(0.04f, 0.04f, 0f);

        /// <summary>
        /// 現在各スロットで一番上にあるピースの配列
        /// </summary>
        private PuzzlePiece[] _slots;

        /// <summary>
        /// 各スロットのワールド座標
        /// </summary>
        private Vector3[] _slotPositions;

        private SlotState[] _slotStates;
        private readonly Dictionary<PuzzlePiece, int> _pieceToSlotIndex = new();

        public event Action OnAllPiecesPlaced;
        public event Action OnPiecesGenerated;
        public event Action<PuzzlePiece> OnPiecePlacedOnGrid;
        public event Action<float> OnTraySubmitted;

        public IReadOnlyList<PuzzlePiece> Slots => _slots;

        public int RemainingPieceCount
        {
            get
            {
                if (_slotStates == null) return 0;

                var count = 0;
                foreach (var state in _slotStates)
                {
                    count += state?.Pieces.Count ?? 0;
                }

                return count;
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

        private void Start()
        {
            GenerateAllPieces();
        }

        private void Update()
        {
            ProcessTimedRefill();
        }

        /// <summary>
        /// スロット位置を初期化する
        /// </summary>
        private void InitializeSlots()
        {
            var slotCount = GetAvailableShapes().Count;
            _slots = new PuzzlePiece[slotCount];
            _slotPositions = new Vector3[slotCount];
            _slotStates = new SlotState[slotCount];

            for (var i = 0; i < slotCount; i++)
            {
                _slotPositions[i] = transform.TransformPoint(slotAreaOffset + CalculateSlotOffset(i, slotCount));
                _slotStates[i] = new SlotState();
            }
        }

        /// <summary>
        /// すべてのスロットを新しいスタックで初期化する
        /// </summary>
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

            if (_slotStates == null || _slotStates.Length != availableShapes.Count)
            {
                InitializeSlots();
            }

            ClearAllPieces();
            var initialCount = Mathf.Clamp(initialPiecesPerSlot, 1, Mathf.Min(maxPiecesPerSlot, MaxSupportedOrderInLayer));

            for (var i = 0; i < availableShapes.Count; i++)
            {
                var state = _slotStates[i];
                state.Shape = availableShapes[i];
                state.NextRefillTime = -1f;

                if (state.Shape == null)
                {
                    Debug.LogWarning($"[PuzzlePieceGenerator] スロット{i}に適切な形状が見つかりませんでした。");
                    continue;
                }

                RefillSlot(i, initialCount);
            }

            OnPiecesGenerated?.Invoke();
        }

        private void ProcessTimedRefill()
        {
            if (_slotStates == null) return;

            var generatedAny = false;
            for (var i = 0; i < _slotStates.Length; i++)
            {
                var state = _slotStates[i];
                if (state == null || state.Shape == null) continue;

                if (state.Pieces.Count >= maxPiecesPerSlot)
                {
                    state.NextRefillTime = -1f;
                    continue;
                }

                if (state.NextRefillTime < 0f)
                {
                    state.NextRefillTime = Time.time + GetRefillInterval(state.Shape);
                }

                while (state.Pieces.Count < maxPiecesPerSlot && Time.time >= state.NextRefillTime)
                {
                    RefillSlot(i, 1);
                    state.NextRefillTime += GetRefillInterval(state.Shape);
                    generatedAny = true;
                }

                if (state.Pieces.Count >= maxPiecesPerSlot)
                {
                    state.NextRefillTime = -1f;
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
            if (shapePool == null) return uniqueShapes;

            foreach (var shape in shapePool)
            {
                if (shape == null || uniqueShapes.Contains(shape)) continue;
                uniqueShapes.Add(shape);
            }

            return uniqueShapes;
        }

        private static float GetRefillInterval(PuzzlePieceShape shape)
        {
            return shape != null ? shape.RefillIntervalSeconds : 0.1f;
        }

        private Vector3 CalculateSlotOffset(int slotIndex, int slotCount)
        {
            if (slotCount <= 0) return Vector3.zero;

            var effectiveSlotsPerLine = Mathf.Max(1, slotsPerLine);
            var lineIndex = slotIndex / effectiveSlotsPerLine;
            var indexInLine = slotIndex % effectiveSlotsPerLine;
            var lineWidth = (effectiveSlotsPerLine - 1) * slotSpacing;
            var lineStart = -lineWidth / 2f;

            if (slotDirection == SlotLayoutDirection.Horizontal)
            {
                return new Vector3(
                    lineStart + indexInLine * slotSpacing,
                    -lineIndex * lineSpacing,
                    0f
                );
            }

            return new Vector3(
                lineIndex * lineSpacing,
                -lineStart - indexInLine * slotSpacing,
                0f
            );
        }

        private void RefillSlot(int slotIndex, int amount)
        {
            var state = _slotStates[slotIndex];
            if (state?.Shape == null) return;

            var spawnCount = Mathf.Min(amount, maxPiecesPerSlot - state.Pieces.Count);
            for (var i = 0; i < spawnCount; i++)
            {
                var piece = SpawnPiece(state.Shape, _slotPositions[slotIndex]);
                state.Pieces.Add(piece);
                _pieceToSlotIndex[piece] = slotIndex;
            }

            RefreshSlotVisuals(slotIndex);
        }

        /// <summary>
        /// ピースのインスタンスを生成する
        /// </summary>
        private PuzzlePiece SpawnPiece(PuzzlePieceShape shape, Vector3 position)
        {
            var piece = Instantiate(piecePrefab, position, Quaternion.identity, transform);
            piece.Initialize(shape, gridView, ChooseRandomSprite(shape), HandlePiecePlaced);
            return piece;
        }

        private static Sprite ChooseRandomSprite(PuzzlePieceShape shape)
        {
            if (shape == null) return null;

            var assignedSpriteCount = shape.GetAssignedDishSpriteCount();
            if (assignedSpriteCount <= 0)
            {
                return null;
            }

            var targetIndex = UnityEngine.Random.Range(0, assignedSpriteCount);
            var currentIndex = 0;
            for (var i = 0; i < shape.DishSprites.Count; i++)
            {
                var sprite = shape.GetSpriteAt(i);
                if (sprite == null) continue;

                if (currentIndex == targetIndex)
                {
                    return sprite;
                }

                currentIndex++;
            }

            return null;
        }

        /// <summary>
        /// スロット内のピース位置と操作可否を更新する
        /// </summary>
        private void RefreshSlotVisuals(int slotIndex)
        {
            var state = _slotStates[slotIndex];
            if (state == null) return;

            for (var i = 0; i < state.Pieces.Count; i++)
            {
                var piece = state.Pieces[i];
                if (piece == null) continue;

                var position = _slotPositions[slotIndex] + stackPieceOffset * i;
                var isTopPiece = i == state.Pieces.Count - 1;
                piece.ConfigureStackPresentation(position, i, isTopPiece);
            }

            _slots[slotIndex] = state.Pieces.Count > 0 ? state.Pieces[^1] : null;
        }

        /// <summary>
        /// ピースが配置されたときのコールバック
        /// </summary>
        private void HandlePiecePlaced(PuzzlePiece piece)
        {
            OnPiecePlacedOnGrid?.Invoke(piece);

            if (!_pieceToSlotIndex.TryGetValue(piece, out var slotIndex))
            {
                return;
            }

            var state = _slotStates[slotIndex];
            _pieceToSlotIndex.Remove(piece);
            state.Pieces.Remove(piece);

            if (state.Pieces.Count < maxPiecesPerSlot && state.NextRefillTime < 0f)
            {
                state.NextRefillTime = Time.time + GetRefillInterval(state.Shape);
            }

            RefreshSlotVisuals(slotIndex);

            if (RemainingPieceCount == 0)
            {
                OnAllPiecesPlaced?.Invoke();
            }
        }

        /// <summary>
        /// ユーザーがトレイを確定する。
        /// グリッドをクリアし、占有率を返してイベントを発火する。
        /// 残っているピースも破棄して新しいスタックを生成する。
        /// </summary>
        public float SubmitTray()
        {
            if (gridView == null || gridView.Grid == null) return 0f;

            var occupancy = gridView.Grid.Clear();
            CleanupPlacedPieces();
            OnTraySubmitted?.Invoke(occupancy);

            ClearAllPieces();
            GenerateAllPieces();

            return occupancy;
        }

        /// <summary>
        /// グリッド上に配置済みのピースGameObjectを削除する
        /// </summary>
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

        /// <summary>
        /// すべてのスロットの未配置ピースを破棄してリセットする
        /// </summary>
        public void ClearAllPieces()
        {
            if (_slotStates == null) return;

            foreach (var state in _slotStates)
            {
                if (state == null) continue;

                foreach (var piece in state.Pieces)
                {
                    if (piece != null)
                    {
                        Destroy(piece.gameObject);
                    }
                }

                state.Pieces.Clear();
                state.NextRefillTime = -1f;
            }

            _pieceToSlotIndex.Clear();

            if (_slots == null) return;
            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i] = null;
            }
        }

        /// <summary>
        /// すべてのスロットを破棄して新しいピースを生成する
        /// </summary>
        public void ResetAndRegenerate()
        {
            ClearAllPieces();
            GenerateAllPieces();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxPiecesPerSlot = Mathf.Clamp(maxPiecesPerSlot, 1, MaxSupportedOrderInLayer);
            initialPiecesPerSlot = Mathf.Clamp(initialPiecesPerSlot, 1, maxPiecesPerSlot);
            slotsPerLine = Mathf.Max(1, slotsPerLine);
        }

        /// <summary>
        /// エディター上でスロット位置をギズモ表示する
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            var slotCount = GetAvailableShapes().Count;
            if (slotCount <= 0) return;

            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.5f);

            for (var i = 0; i < slotCount; i++)
            {
                var pos = transform.TransformPoint(slotAreaOffset + CalculateSlotOffset(i, slotCount));
                Gizmos.DrawSphere(pos, 0.1f);

                var topPos = pos + stackPieceOffset * Mathf.Max(initialPiecesPerSlot - 1, 0);
                Gizmos.DrawLine(pos, topPos);
            }
        }
#endif
    }
}
