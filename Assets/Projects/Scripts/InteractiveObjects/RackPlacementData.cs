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
        public readonly Sprite Sprite;
        public readonly Vector2Int GridOrigin;
        public readonly Vector2Int[] ShapeCells;

        public PlacedDishInfo(string dishTypeKey, Sprite sprite, Vector2Int gridOrigin, Vector2Int[] shapeCells)
        {
            DishTypeKey = dishTypeKey;
            Sprite = sprite;
            GridOrigin = gridOrigin;
            ShapeCells = shapeCells;
        }
    }
}
