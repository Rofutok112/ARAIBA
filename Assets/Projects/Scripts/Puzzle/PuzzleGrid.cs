using System;
using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    /// <summary>
    /// パズルグリッドのデータを管理するクラス。
    /// セルの占有状態の管理と、ピース配置の判定を行う。
    /// </summary>
    public class PuzzleGrid
    {
        /// <summary>
        /// グリッドの一辺の長さ
        /// </summary>
        public int GridSize { get; }

        /// <summary>
        /// グリッドの占有状態。true = 埋まっている
        /// </summary>
        private readonly bool[,] _occupied;

        /// <summary>
        /// ピースが配置/除去されたときに発火するイベント
        /// </summary>
        public event Action OnGridChanged;

        public PuzzleGrid(int gridSize)
        {
            GridSize = gridSize;
            _occupied = new bool[gridSize, gridSize];
        }

        /// <summary>
        /// 指定セルが占有されているかどうかを返す
        /// </summary>
        public bool IsOccupied(int x, int y)
        {
            if (!IsInBounds(x, y)) return true; // 範囲外は埋まっているとみなす
            return _occupied[x, y];
        }

        /// <summary>
        /// 座標がグリッド範囲内かどうかを返す
        /// </summary>
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < GridSize && y >= 0 && y < GridSize;
        }

        /// <summary>
        /// 指定のピース形状を、グリッド上の指定位置に配置できるかどうかを判定する。
        /// originはピース形状の(0,0)がグリッド上のどこに来るかを示す。
        /// </summary>
        public bool CanPlace(PuzzlePieceShape shape, Vector2Int origin)
        {
            var filledCells = shape.GetFilledCells();
            foreach (var cell in filledCells)
            {
                var gx = origin.x + cell.x;
                var gy = origin.y + cell.y;
                if (!IsInBounds(gx, gy) || _occupied[gx, gy])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// ピースをグリッド上に配置する。配置可能な場合のみ成功しtrueを返す。
        /// </summary>
        public bool TryPlace(PuzzlePieceShape shape, Vector2Int origin)
        {
            if (!CanPlace(shape, origin)) return false;

            var filledCells = shape.GetFilledCells();
            foreach (var cell in filledCells)
            {
                _occupied[origin.x + cell.x, origin.y + cell.y] = true;
            }

            OnGridChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// ピースをグリッドから除去する。
        /// </summary>
        public void Remove(PuzzlePieceShape shape, Vector2Int origin)
        {
            var filledCells = shape.GetFilledCells();
            foreach (var cell in filledCells)
            {
                var gx = origin.x + cell.x;
                var gy = origin.y + cell.y;
                if (IsInBounds(gx, gy))
                    _occupied[gx, gy] = false;
            }

            OnGridChanged?.Invoke();
        }

        /// <summary>
        /// 完成した行と列をクリアし、クリアした行数 + 列数を返す。
        /// </summary>
        public int ClearCompletedLines()
        {
            var clearedCount = 0;

            // 行チェック
            for (var y = 0; y < GridSize; y++)
            {
                var full = true;
                for (var x = 0; x < GridSize; x++)
                {
                    if (!_occupied[x, y])
                    {
                        full = false;
                        break;
                    }
                }
                if (!full) continue;

                for (var x = 0; x < GridSize; x++)
                    _occupied[x, y] = false;
                clearedCount++;
            }

            // 列チェック
            for (var x = 0; x < GridSize; x++)
            {
                var full = true;
                for (var y = 0; y < GridSize; y++)
                {
                    if (!_occupied[x, y])
                    {
                        full = false;
                        break;
                    }
                }
                if (!full) continue;

                for (var y = 0; y < GridSize; y++)
                    _occupied[x, y] = false;
                clearedCount++;
            }

            if (clearedCount > 0)
                OnGridChanged?.Invoke();

            return clearedCount;
        }

        /// <summary>
        /// 現在のグリッドの占有率を返す（0.0～1.0）
        /// </summary>
        public float GetOccupancy()
        {
            var occupiedCount = 0;
            for (var x = 0; x < GridSize; x++)
                for (var y = 0; y < GridSize; y++)
                {
                    if (_occupied[x, y]) occupiedCount++;
                }
            return occupiedCount / (float)(GridSize * GridSize);
        }

        /// <summary>
        /// グリッドをすべてクリアし、占有率を返す。
        /// </summary>
        public float Clear()
        {
            var occupiedCount = 0;
            for (var x = 0; x < GridSize; x++)
                for (var y = 0; y < GridSize; y++)
                {
                    if (_occupied[x, y]) occupiedCount++;
                    _occupied[x, y] = false;
                }

            OnGridChanged?.Invoke();
            return occupiedCount / (float)(GridSize * GridSize);
        }
    }
}