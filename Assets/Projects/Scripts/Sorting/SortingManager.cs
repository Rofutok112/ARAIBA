using System.Collections.Generic;
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

        [Header("Events")]
        [SerializeField] private UnityEvent onSortingCompleted;

        private readonly List<SortingDish> _activeDishes = new();
        private readonly List<GameObject> _spawnedObjects = new();
        private Rack _currentRack;

        /// <summary>
        /// 選別画面を開始する
        /// </summary>
        public void StartSorting(Rack rack)
        {
            if (rack == null || rack.PlacementData == null) return;

            _currentRack = rack;
            rack.SetState(RackState.Sorting);

            sortingWindow.SetActive(true);
            SpawnDishes(rack.PlacementData);
        }

        /// <summary>
        /// ラック上の皿をD&D可能なオブジェクトとして生成する
        /// </summary>
        private void SpawnDishes(RackPlacementData data)
        {
            foreach (var dish in data.Dishes)
            {
                var obj = new GameObject($"SortingDish_{dish.ShapeKey}");
                obj.transform.SetParent(sortingGridView.transform, false);

                var cellSize = sortingGridView.CellSize;
                var originPos = sortingGridView.GridToWorldPosition(dish.GridOrigin);
                var centerOffset = new Vector2(
                    (dish.ShapeWidth - 1) * cellSize / 2f,
                    (dish.ShapeHeight - 1) * cellSize / 2f
                );
                obj.transform.position = (Vector2)originPos + centerOffset;

                var sortingDish = obj.AddComponent<SortingDish>();
                sortingDish.Initialize(
                    dish.ShapeKey,
                    dish.Sprite,
                    dish.ShapeWidth,
                    dish.ShapeHeight,
                    cellSize,
                    sortingGridView.ActiveTargets,
                    sortingGridView.TargetRadius,
                    OnDishSorted
                );
                _activeDishes.Add(sortingDish);
                _spawnedObjects.Add(obj);
            }
        }

        private void OnDishSorted(SortingDish dish)
        {
            _activeDishes.Remove(dish);

            if (_activeDishes.Count == 0)
            {
                CompleteSorting();
            }
        }

        private void CompleteSorting()
        {
            if (_currentRack != null)
            {
                _currentRack.ClearPlacementData();
                _currentRack.SetState(RackState.Empty);
                _currentRack = null;
            }

            Cleanup();
            sortingWindow.SetActive(false);
            onSortingCompleted?.Invoke();
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
