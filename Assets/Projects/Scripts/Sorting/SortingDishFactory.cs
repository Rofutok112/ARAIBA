using System;
using System.Collections.Generic;
using Projects.Scripts.Common;
using Projects.Scripts.Control;
using Projects.Scripts.InteractiveObjects;
using UnityEngine;

namespace Projects.Scripts.Sorting
{
    internal sealed class SortingDishFactory
    {
        private readonly Transform _parent;
        private readonly GridGeometry _geometry;
        private readonly Vector2 _cellLocalSize;
        private readonly SortingDropResolver _dropResolver;
        private readonly InputTargetRole _inputTargetRole;

        public SortingDishFactory(
            Transform parent,
            GridGeometry geometry,
            Vector2 cellLocalSize,
            IReadOnlyList<SortingTarget> targets,
            float targetRadius,
            InputTargetRole inputTargetRole)
        {
            _parent = parent;
            _geometry = geometry;
            _cellLocalSize = cellLocalSize;
            _dropResolver = new SortingDropResolver(targets, targetRadius);
            _inputTargetRole = inputTargetRole;
        }

        public SortingDishSpawnResult Create(PlacedDishInfo dish, Action<SortingDish, int> onSorted)
        {
            var obj = new GameObject($"SortingDish_{dish.ShapeKey}");
            obj.transform.SetParent(_parent, false);
            obj.transform.position = GetDishWorldPosition(dish);
            var inputTargetLayer = obj.AddComponent<InputTargetLayer>();
            inputTargetLayer.SetRole(_inputTargetRole);

            var sortingDish = obj.AddComponent<SortingDish>();
            sortingDish.Initialize(
                dish.ShapeKey,
                dish.ScorePoints,
                dish.Sprite,
                dish.ShapeWidth,
                dish.ShapeHeight,
                _cellLocalSize,
                _dropResolver,
                onSorted
            );

            return new SortingDishSpawnResult(obj, sortingDish);
        }

        private Vector2 GetDishWorldPosition(PlacedDishInfo dish)
        {
            var originPos = _geometry.GridToWorldPosition(dish.GridOrigin);
            var centerOffset = new Vector2(
                (dish.ShapeWidth - 1) * _geometry.CellWorldSize.x / 2f,
                (dish.ShapeHeight - 1) * _geometry.CellWorldSize.y / 2f
            );
            return originPos + centerOffset;
        }

    }

    internal readonly struct SortingDishSpawnResult
    {
        public SortingDishSpawnResult(GameObject gameObject, SortingDish sortingDish)
        {
            GameObject = gameObject;
            SortingDish = sortingDish;
        }

        public GameObject GameObject { get; }
        public SortingDish SortingDish { get; }
    }
}
