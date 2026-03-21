using System.Collections.Generic;
using Projects.Scripts.Puzzle;
using UnityEngine;
using UnityEngine.Events;

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

        [Header("Racks")]
        [Tooltip("管理対象のラック一覧")]
        [SerializeField] private List<Rack> racks;

        [SerializeField] private UnityEvent onPuzzleConfirmed;

        /// <summary>
        /// 現在パズル中のラック（null = パズル画面を開いていない）
        /// </summary>
        private Rack _activeRack;

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
            }
        }

        /// <summary>
        /// パズル画面を開く
        /// </summary>
        private void OpenPuzzle(Rack rack)
        {
            if (_activeRack != null) return;

            _activeRack = rack;
            rack.SetState(RackState.Packing);

            puzzlePieceGenerator.ResetPuzzle();
            puzzleWindow.SetActive(true);
            puzzleGridView.PlayOpeningAnimation();
        }

        /// <summary>
        /// パズルの確定ボタンから呼ばれる。
        /// グリッド上の配置情報を保存してパズルを閉じる。
        /// </summary>
        public void ConfirmPuzzle()
        {
            if (_activeRack == null) return;

            var data = CapturePlacementData();
            _activeRack.SavePlacementData(data);
            _activeRack.SetState(RackState.Packed);

            PuzzleScoreStore.SaveScore(data.Occupancy);

            puzzleGridView.PlayClosingAnimation();
            puzzleWindow.SetActive(false);
            _activeRack = null;
            
            onPuzzleConfirmed.Invoke();
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
                    piece.SelectedDishSprite,
                    piece.PlacedGridOrigin,
                    piece.Shape.GetFilledCells()
                );
                data.Dishes.Add(info);
            }

            return data;
        }
    }
}
