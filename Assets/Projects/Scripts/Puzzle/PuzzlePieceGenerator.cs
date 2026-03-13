using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Projects.Scripts.Puzzle
{
    /// <summary>
    /// パズルピースを生成し、スロットに保持するシステム。
    /// 指定されたスロット位置にピースを生成し、
    /// すべてのピースが配置されたら新しいピースを生成する。
    /// </summary>
    public class PuzzlePieceGenerator : MonoBehaviour
    {
        [Header("Piece Settings")]
        [Tooltip("生成するピースの形状候補リスト")]
        [SerializeField] private PuzzlePieceShape[] shapePool;

        [Tooltip("ピースのプレハブ（PuzzlePieceコンポーネント付き）")]
        [SerializeField] private PuzzlePiece piecePrefab;

        [Tooltip("配置先のグリッドビュー")]
        [SerializeField] private PuzzleGridView gridView;

        [Header("Slot Settings")]
        [Tooltip("ピースのスロット数")]
        [SerializeField] private int slotCount = 3;

        [Tooltip("スロットの配置間隔")]
        [SerializeField] private float slotSpacing = 3f;

        [Tooltip("スロットを配置する基準位置からのオフセット（ローカル座標）")]
        [SerializeField] private Vector3 slotAreaOffset = new(0f, -5f, 0f);

        [Header("Generation Options")]
        [Tooltip("trueの場合、全スロットが空になったら一括生成。falseの場合、配置されたスロットを即座に補充")]
        [SerializeField] private bool batchGeneration = true;

        [Tooltip("同じ形状の重複を許可するかどうか")]
        [SerializeField] private bool allowDuplicateShapes = true;

        /// <summary>
        /// 現在スロットに保持されているピースの配列
        /// </summary>
        private PuzzlePiece[] _slots;

        /// <summary>
        /// 各スロットのワールド座標
        /// </summary>
        private Vector3[] _slotPositions;

        /// <summary>
        /// すべてのピースが配置されたときに発火するイベント
        /// </summary>
        public event Action OnAllPiecesPlaced;

        /// <summary>
        /// 新しいピースが生成されたときに発火するイベント
        /// </summary>
        public event Action OnPiecesGenerated;

        /// <summary>
        /// ピースが1つ配置されるたびに発火するイベント（占有率更新等に使用）
        /// </summary>
        public event Action<PuzzlePiece> OnPiecePlacedOnGrid;

        /// <summary>
        /// トレイが確定されたときに発火するイベント（占有率を引数に渡す）
        /// </summary>
        public event Action<float> OnTraySubmitted;

        /// <summary>
        /// 現在保持しているピースの読み取り専用リスト
        /// </summary>
        public IReadOnlyList<PuzzlePiece> Slots => _slots;

        /// <summary>
        /// 残っている（未配置の）ピースの数
        /// </summary>
        public int RemainingPieceCount
        {
            get
            {
                var count = 0;
                if (_slots == null) return count;
                foreach (var slot in _slots)
                {
                    if (slot != null && !slot.IsPlaced) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 現在のグリッドの占有率（0.0～1.0）
        /// </summary>
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
            InitializeSlots();
            GenerateAllPieces();
        }

        /// <summary>
        /// スロット位置を初期化する
        /// </summary>
        private void InitializeSlots()
        {
            _slots = new PuzzlePiece[slotCount];
            _slotPositions = new Vector3[slotCount];

            // スロットを中央揃えで配置
            var totalWidth = (slotCount - 1) * slotSpacing;
            var startX = -totalWidth / 2f;

            for (var i = 0; i < slotCount; i++)
            {
                _slotPositions[i] = transform.TransformPoint(
                    slotAreaOffset + new Vector3(startX + i * slotSpacing, 0f, 0f)
                );
            }
        }

        /// <summary>
        /// すべてのスロットにピースを生成する
        /// </summary>
        public void GenerateAllPieces()
        {
            if (shapePool == null || shapePool.Length == 0)
            {
                Debug.LogWarning("[PuzzlePieceGenerator] 形状プールが空です。インスペクターで形状を設定してください。");
                return;
            }

            if (piecePrefab == null)
            {
                Debug.LogWarning("[PuzzlePieceGenerator] ピースプレハブが設定されていません。");
                return;
            }

            var usedShapes = new HashSet<int>();

            for (var i = 0; i < slotCount; i++)
            {
                // 既にピースがあり未配置なら再生成しない
                if (_slots[i] != null && !_slots[i].IsPlaced)
                    continue;

                // 以前のピースがあれば破棄
                if (_slots[i] != null)
                {
                    Destroy(_slots[i].gameObject);
                    _slots[i] = null;
                }

                var shape = SelectShape(usedShapes);
                if (shape == null)
                {
                    Debug.LogWarning($"[PuzzlePieceGenerator] スロット{i}に適切な形状が見つかりませんでした。");
                    continue;
                }

                _slots[i] = SpawnPiece(shape, _slotPositions[i]);
            }

            OnPiecesGenerated?.Invoke();
        }

        /// <summary>
        /// 形状プールからランダムに形状を選択する
        /// </summary>
        private PuzzlePieceShape SelectShape(HashSet<int> usedIndices)
        {
            // 候補リストを構築
            var candidates = new List<int>();

            for (var i = 0; i < shapePool.Length; i++)
            {
                if (shapePool[i] == null) continue;

                // 重複チェック
                if (!allowDuplicateShapes && usedIndices.Contains(i)) continue;

                candidates.Add(i);
            }

            if (candidates.Count == 0) return null;

            // ランダム選択
            var selectedIndex = candidates[Random.Range(0, candidates.Count)];
            usedIndices.Add(selectedIndex);
            return shapePool[selectedIndex];
        }

        /// <summary>
        /// ピースのインスタンスを生成する
        /// </summary>
        private PuzzlePiece SpawnPiece(PuzzlePieceShape shape, Vector3 position)
        {
            var piece = Instantiate(piecePrefab, position, Quaternion.identity, transform);
            piece.Initialize(shape, gridView, HandlePiecePlaced);
            return piece;
        }

        /// <summary>
        /// ピースが配置されたときのコールバック
        /// </summary>
        private void HandlePiecePlaced(PuzzlePiece piece)
        {
            // ピース配置通知を発火
            OnPiecePlacedOnGrid?.Invoke(piece);

            if (batchGeneration)
            {
                // 一括生成モード: すべてのスロットが空（配置済み）かチェック
                if (RemainingPieceCount == 0)
                {
                    OnAllPiecesPlaced?.Invoke();
                    GenerateAllPieces();
                }
            }
            else
            {
                // 即時補充モード: 配置されたスロットを見つけて補充
                for (var i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] == piece)
                    {
                        Destroy(_slots[i].gameObject);
                        var usedShapes = new HashSet<int>();
                        var shape = SelectShape(usedShapes);
                        if (shape != null)
                        {
                            _slots[i] = SpawnPiece(shape, _slotPositions[i]);
                        }
                        break;
                    }
                }

                OnPiecesGenerated?.Invoke();
            }
        }

        /// <summary>
        /// ユーザーがトレイを確定する。
        /// グリッドをクリアし、占有率を返してイベントを発火する。
        /// 残っているピースも破棄して新しいピースを生成する。
        /// </summary>
        /// <returns>確定時のグリッド占有率（0.0～1.0）</returns>
        public float SubmitTray()
        {
            if (gridView == null || gridView.Grid == null) return 0f;

            // グリッドをクリアして占有率を取得
            var occupancy = gridView.Grid.Clear();

            // 配置済みのピースGameObjectを削除（グリッド上のビジュアル）
            CleanupPlacedPieces();

            // イベント発火
            OnTraySubmitted?.Invoke(occupancy);

            // 残りのピースも破棄して新しいセットを生成
            ClearAllPieces();
            GenerateAllPieces();

            return occupancy;
        }

        /// <summary>
        /// グリッド上に配置済みのピースGameObjectを削除する
        /// </summary>
        private void CleanupPlacedPieces()
        {
            // スロット内の配置済みピースを削除
            if (_slots == null) return;

            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].IsPlaced)
                {
                    Destroy(_slots[i].gameObject);
                    _slots[i] = null;
                }
            }
        }

        /// <summary>
        /// すべてのスロットのピースを破棄してリセットする
        /// </summary>
        public void ClearAllPieces()
        {
            if (_slots == null) return;

            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    Destroy(_slots[i].gameObject);
                    _slots[i] = null;
                }
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
        /// <summary>
        /// エディター上でスロット位置をギズモ表示する
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (slotCount <= 0) return;

            var totalWidth = (slotCount - 1) * slotSpacing;
            var startX = -totalWidth / 2f;

            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.5f);

            for (var i = 0; i < slotCount; i++)
            {
                var pos = transform.TransformPoint(
                    slotAreaOffset + new Vector3(startX + i * slotSpacing, 0f, 0f)
                );
                Gizmos.DrawWireCube(pos, Vector3.one * 2f);
                Gizmos.DrawIcon(pos, "d_Prefab Icon", true);
            }
        }
#endif
    }
}