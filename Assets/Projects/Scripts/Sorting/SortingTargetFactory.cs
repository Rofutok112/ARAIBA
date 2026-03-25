using System.Collections.Generic;
using Projects.Scripts.Puzzle;
using UnityEngine;

namespace Projects.Scripts.Sorting
{
    internal sealed class SortingTargetFactory
    {
        private readonly Transform _parent;
        private readonly SortingTarget _targetPrefab;
        private readonly Vector3 _targetAreaOffset;
        private readonly float _targetSpacing;
        private readonly int _targetsPerLine;
        private readonly float _lineSpacing;
        private readonly SlotLayoutDirection _targetDirection;
        private readonly float _targetAlpha;
        private readonly Vector2 _cellLocalSize;
        private readonly Vector3 _stackPieceOffset;
        private readonly int _stackBaseSortingOrder;

        public SortingTargetFactory(
            Transform parent,
            SortingTarget targetPrefab,
            Vector3 targetAreaOffset,
            float targetSpacing,
            int targetsPerLine,
            float lineSpacing,
            SlotLayoutDirection targetDirection,
            float targetAlpha,
            Vector2 cellLocalSize,
            Vector3 stackPieceOffset,
            int stackBaseSortingOrder)
        {
            _parent = parent;
            _targetPrefab = targetPrefab;
            _targetAreaOffset = targetAreaOffset;
            _targetSpacing = targetSpacing;
            _targetsPerLine = targetsPerLine;
            _lineSpacing = lineSpacing;
            _targetDirection = targetDirection;
            _targetAlpha = targetAlpha;
            _cellLocalSize = cellLocalSize;
            _stackPieceOffset = stackPieceOffset;
            _stackBaseSortingOrder = stackBaseSortingOrder;
        }

        public List<SortingTarget> CreateTargets(IReadOnlyList<PuzzlePieceShape> shapes)
        {
            var createdTargets = new List<SortingTarget>();
            if (shapes == null || shapes.Count == 0 || _targetPrefab == null) return createdTargets;

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                if (shape == null) continue;

                var localOffset = GetLocalOffset(i, shapes.Count);
                var worldPos = _parent.TransformPoint(localOffset);

                var target = Object.Instantiate(_targetPrefab, worldPos, Quaternion.identity, _parent);
                target.name = $"SortingTarget_{shape.name}";
                target.Initialize(
                    shape.name,
                    shape.DishTypeName,
                    shape.GetEffectiveSprite(),
                    _targetAlpha,
                    shape.Width,
                    shape.Height,
                    _cellLocalSize,
                    _stackPieceOffset,
                    _stackBaseSortingOrder
                );
                createdTargets.Add(target);
            }

            return createdTargets;
        }

        public Vector3 GetPreviewPosition(int index, int totalCount)
        {
            return _parent.TransformPoint(GetLocalOffset(index, totalCount));
        }

        private Vector3 GetLocalOffset(int index, int totalCount)
        {
            return _targetAreaOffset + CalculateTargetOffset(index, totalCount);
        }

        private Vector3 CalculateTargetOffset(int index, int totalCount)
        {
            if (totalCount <= 0) return Vector3.zero;

            var effectivePerLine = Mathf.Max(1, _targetsPerLine);
            var lineIndex = index / effectivePerLine;
            var indexInLine = index % effectivePerLine;
            var lineWidth = (effectivePerLine - 1) * _targetSpacing;
            var lineStart = -lineWidth / 2f;

            if (_targetDirection == SlotLayoutDirection.Horizontal)
            {
                return new Vector3(
                    lineStart + indexInLine * _targetSpacing,
                    -lineIndex * _lineSpacing,
                    0f
                );
            }

            return new Vector3(
                lineIndex * _lineSpacing,
                -lineStart - indexInLine * _targetSpacing,
                0f
            );
        }
    }
}
