using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.InteractiveObjects
{
    /// <summary>
    /// パズル終了時のグリッド配置情報を保持するデータクラス。
    /// 選別画面で皿を種類ごとに分けるために使用する。
    /// </summary>
    public class RackPlacementData
    {
        public readonly List<PlacedDishInfo> Dishes = new();
        public readonly float Occupancy;

        public RackPlacementData(float occupancy)
        {
            Occupancy = occupancy;
        }
    }

    public class PlacedDishInfo
    {
        public readonly string DishTypeKey;
        public readonly string ShapeKey;
        public readonly string DishTypeName;
        public readonly Sprite Sprite;
        public readonly Vector2Int GridOrigin;
        public readonly Vector2Int[] ShapeCells;
        public readonly int ShapeWidth;
        public readonly int ShapeHeight;
        public readonly int ScorePoints;

        public PlacedDishInfo(string dishTypeKey, string shapeKey, string dishTypeName, Sprite sprite, Vector2Int gridOrigin, Vector2Int[] shapeCells, int shapeWidth, int shapeHeight, int scorePoints)
        {
            DishTypeKey = dishTypeKey;
            ShapeKey = shapeKey;
            DishTypeName = dishTypeName;
            Sprite = sprite;
            GridOrigin = gridOrigin;
            ShapeCells = shapeCells;
            ShapeWidth = shapeWidth;
            ShapeHeight = shapeHeight;
            ScorePoints = scorePoints;
        }
    }
}
