using System;
using System.Collections.Generic;
using Projects.Scripts.Control;
using Projects.Scripts.Puzzle;
using Projects.Scripts.Sorting;
using UnityEngine;

namespace Projects.Scripts.InteractiveObjects
{
    /// <summary>
    /// ラックとパズルウィンドウの管理を行うクラス。
    /// </summary>
    public class RackManager : MonoBehaviour
    {
        [Header("Puzzle")]
        [Tooltip("パズルウィンドウのGameObject（使い回す）")]
        [SerializeField] private GameObject puzzleWindow;

        [Tooltip("パズルピース生成器")]
        [SerializeField] private PuzzlePieceGenerator puzzlePieceGenerator;

        [Tooltip("パズルグリッドビュー")]
        [SerializeField] private PuzzleGridView puzzleGridView;

        [Tooltip("パズルを開くたびに PuzzlePieceGenerator のストックも含めてリセットする")]
        [SerializeField] private bool resetPuzzleOnOpen = true;

        [Header("Input")]
        [SerializeField] private InputStateRouter inputStateRouter;

        [Header("Sorting")]
        [SerializeField] private SortingManager sortingManager;

        [Header("Racks")]
        [Tooltip("管理対象のラック一覧")]
        [SerializeField] private List<Rack> racks;

        /// <summary>
        /// 現在パズル中のラック（null = パズル画面を開いていない）
        /// </summary>
        private Rack _activeRack;
        private bool _isPuzzleTransitioning;
        public event Action<Rack> PuzzleOpened;
        public event Action PuzzleConfirmed;
        public event Action PuzzleWindowActivated;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            if (racks == null) return;
            foreach (var rack in racks)
            {
                if (rack != null) rack.OnClicked += OnRackClicked;
            }
        }

        private void OnDisable()
        {
            if (racks == null) return;
            foreach (var rack in racks)
            {
                if (rack != null) rack.OnClicked -= OnRackClicked;
            }
        }

        /// <summary>
        /// ラックがクリックされたときに呼ばれる
        /// </summary>
        private void OnRackClicked(Rack rack)
        {
            switch (rack.State)
            {
                case RackState.Empty:
                    OpenPuzzle(rack);
                    break;
                case RackState.Washed:
                    StartSorting(rack);
                    break;
            }
        }

        /// <summary>
        /// パズル画面を開く
        /// </summary>
        private void OpenPuzzle(Rack rack)
        {
            if (_activeRack != null || _isPuzzleTransitioning || !ValidateReferences()) return;

            _isPuzzleTransitioning = true;
            _activeRack = rack;
            rack.SetState(RackState.Packing);

            if (resetPuzzleOnOpen)
            {
                puzzlePieceGenerator.ResetPuzzle();
            }
            else
            {
                puzzlePieceGenerator.ResetGridOnly();
            }
            puzzleWindow.SetActive(true);
            PuzzleOpened?.Invoke(rack);
            PuzzleWindowActivated?.Invoke();
            puzzleGridView.PlayOpeningAnimation(() =>
            {
                inputStateRouter?.SetOperationState(InputOperationState.Puzzle);
                _isPuzzleTransitioning = false;
            });
        }

        private void StartSorting(Rack rack)
        {
            if (rack == null || !ValidateReferences()) return;
            sortingManager.StartSorting(rack);
        }

        /// <summary>
        /// パズルの確定ボタンから呼ばれる。
        /// グリッド上の配置情報を保存してパズルを閉じる。
        /// </summary>
        public void ConfirmPuzzle()
        {
            if (_activeRack == null || _isPuzzleTransitioning) return;

            _isPuzzleTransitioning = true;

            var data = CapturePlacementData();
            if (data.Dishes.Count > 0)
            {
                _activeRack.SavePlacementData(data);
                _activeRack.SetState(RackState.Packed);
            }
            else
            {
                _activeRack.ClearPlacementData();
                _activeRack.SetState(RackState.Empty);
            }

            PuzzleConfirmed?.Invoke();
            inputStateRouter?.ResetToDefault();
            puzzleGridView.PlayClosingAnimation(() =>
            {
                puzzleWindow.SetActive(false);
                _activeRack = null;
                _isPuzzleTransitioning = false;
            });
        }

        public void CompleteActivePuzzleForGameEnd()
        {
            if (_activeRack == null || _isPuzzleTransitioning || !ValidateReferences())
            {
                return;
            }

            ConfirmPuzzle();
        }

        /// <summary>
        /// Packed状態のラックを1つ返す（なければnull）
        /// </summary>
        public Rack FindPackedRack()
        {
            if (racks == null) return null;

            foreach (var rack in racks)
            {
                if (rack != null && rack.State == RackState.Packed)
                    return rack;
            }

            return null;
        }

        public Rack FindWashedRack()
        {
            if (racks == null) return null;

            foreach (var rack in racks)
            {
                if (rack != null && rack.State == RackState.Washed)
                {
                    return rack;
                }
            }

            return null;
        }

        /// <summary>
        /// 現在のグリッド上の配置済みピース情報をキャプチャする
        /// </summary>
        private RackPlacementData CapturePlacementData()
        {
            var occupancy = puzzleGridView.Grid.GetOccupancy();
            var data = new RackPlacementData(occupancy);

            var pieces = puzzlePieceGenerator.GetComponentsInChildren<PuzzlePiece>();
            foreach (var piece in pieces)
            {
                if (!piece.IsPlaced) continue;

                var info = new PlacedDishInfo(
                    piece.DishTypeKey,
                    piece.Shape.name,
                    piece.Shape.DishTypeName,
                    piece.SelectedDishSprite,
                    piece.PlacedGridOrigin,
                    piece.Shape.GetFilledCells(),
                    piece.Shape.Width,
                    piece.Shape.Height,
                    piece.Shape.ScorePoints
                );
                data.Dishes.Add(info);
            }

            return data;
        }

        private bool ValidateReferences()
        {
            if (puzzleWindow == null ||
                puzzlePieceGenerator == null ||
                puzzleGridView == null ||
                inputStateRouter == null ||
                sortingManager == null)
            {
                Debug.LogWarning($"{nameof(RackManager)} is missing scene references.", this);
                return false;
            }

            return true;
        }
    }
}
