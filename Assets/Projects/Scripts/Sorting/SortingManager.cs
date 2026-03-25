using System.Collections.Generic;
using Projects.Scripts.Control;
using Projects.Scripts.InteractiveObjects;
using UnityEngine;
using UnityEngine.Events;


namespace Projects.Scripts.Sorting
{
    /// <summary>
    /// 選別画面の管理。
    /// Washedラックの皿を種類ごとに分けるフローを制御する。
    /// </summary>
    public class SortingManager : MonoBehaviour
    {
        [Header("Window")]
        [Tooltip("選別画面の親オブジェクト（表示/非表示切り替え用）")]
        [SerializeField] private GameObject sortingWindow;

        [Header("Grid")]
        [Tooltip("選別画面用のグリッド表示")]
        [SerializeField] private SortingGridView sortingGridView;

        [Header("Targets")]
        [SerializeField] private SortingTargetGroup sortingTargetGroup;

        [Header("Input")]
        [SerializeField] private InputStateRouter inputStateRouter;

        [Header("Score")]
        [SerializeField] private UnityEvent<int> onSortingScoreConfirmed;

        [Header("Events")]
        [SerializeField] private UnityEvent onSortingCompleted;

        private readonly List<SortingDish> _activeDishes = new();
        private readonly List<GameObject> _spawnedObjects = new();
        private Rack _currentRack;
        private bool _isSortingTransitioning;

        private void Awake()
        {
            if (inputStateRouter == null)
            {
                inputStateRouter = FindFirstObjectByType<InputStateRouter>();
            }

            if (inputStateRouter == null)
            {
                var inputManager = FindFirstObjectByType<InputManager>();
                if (inputManager != null)
                {
                    inputStateRouter = inputManager.GetComponent<InputStateRouter>();
                    if (inputStateRouter == null)
                    {
                        inputStateRouter = inputManager.gameObject.AddComponent<InputStateRouter>();
                    }
                }
            }

            if (sortingTargetGroup == null)
            {
                sortingTargetGroup = FindFirstObjectByType<SortingTargetGroup>();
            }
        }

        /// <summary>
        /// 選別画面を開始する
        /// </summary>
        public void StartSorting(Rack rack)
        {
            if (rack == null || rack.PlacementData == null || _currentRack != null || _isSortingTransitioning) return;

            _isSortingTransitioning = true;
            _currentRack = rack;
            rack.SetState(RackState.Sorting);

            sortingWindow.SetActive(true);
            SpawnDishes(rack.PlacementData);
            sortingGridView.PlayOpeningAnimation(() =>
            {
                inputStateRouter?.SetOperationState(InputOperationState.Sorting);
                _isSortingTransitioning = false;
            });
        }

        /// <summary>
        /// ラック上の皿をD&D可能なオブジェクトとして生成する
        /// </summary>
        private void SpawnDishes(RackPlacementData data)
        {
            var factory = new SortingDishFactory(
                sortingGridView.transform,
                sortingGridView.Geometry,
                sortingGridView.CellLocalSize,
                sortingTargetGroup != null ? sortingTargetGroup.ActiveTargets : null,
                sortingTargetGroup != null ? sortingTargetGroup.TargetRadius : 1.5f,
                InputTargetRole.Sorting
            );

            foreach (var dish in data.Dishes)
            {
                var spawnResult = factory.Create(dish, OnDishSorted);
                _activeDishes.Add(spawnResult.SortingDish);
                _spawnedObjects.Add(spawnResult.GameObject);
            }
        }

        private void OnDishSorted(SortingDish dish, int scorePoints)
        {
            onSortingScoreConfirmed?.Invoke(Mathf.Max(0, scorePoints));
            _activeDishes.Remove(dish);

            if (_activeDishes.Count == 0)
            {
                CompleteSorting();
            }
        }

        private void CompleteSorting()
        {
            if (_isSortingTransitioning) return;

            _isSortingTransitioning = true;

            if (_currentRack != null)
            {
                _currentRack.ClearPlacementData();
                _currentRack.SetState(RackState.Empty);
                _currentRack = null;
            }

            inputStateRouter?.ResetToDefault();
            sortingGridView.PlayClosingAnimation(() =>
            {
                Cleanup();
                sortingWindow.SetActive(false);
                _isSortingTransitioning = false;
                onSortingCompleted?.Invoke();
            });
        }

        private void Cleanup()
        {
            foreach (var obj in _spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }

            _spawnedObjects.Clear();
            _activeDishes.Clear();
        }
    }
}
