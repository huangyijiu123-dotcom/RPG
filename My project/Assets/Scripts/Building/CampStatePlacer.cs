using UnityEngine;
using RPG.Map;

namespace RPG.Map.Building
{
    /// <summary>
    /// 初始大本营与城邦建筑放置器
    /// 职责：检索第一层地形图，并在大本营（BASE_CAMP）和城邦（CITY_STATE）对应的格子上放置初始建筑。
    /// 实现说明书 §5.1。
    /// </summary>
    public static class CampStatePlacer
    {
        // 建筑类型 ID 定义（大写定义以防混乱）
        public const string BUILDING_BASE_CAMP  = "BASE_CAMP";
        public const string BUILDING_CITY_STATE = "CITY_STATE";

        /// <summary>
        /// 放置初始的大本营和城邦建筑。
        /// </summary>
        /// <param name="terrainLayer">第一层已生成地形图</param>
        /// <param name="buildingLayer">待写入的建筑图层数据（将被修改）</param>
        public static void PlaceInitialBuildings(TerrainType[,] terrainLayer, BuildingData[,] buildingLayer)
        {
            if (terrainLayer == null || buildingLayer == null)
            {
                Debug.LogError("[CampStatePlacer] terrainLayer 或 buildingLayer 为 null，放置中止。");
                return;
            }

            int size = MapDataStore.MAP_SIZE;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 默认重置为无建筑
                    buildingLayer[x, y].HasBuilding        = false;
                    buildingLayer[x, y].BuildingType       = "";
                    buildingLayer[x, y].IsWorldGenerated   = false;
                    buildingLayer[x, y].BuildingLevel      = 0;
                    buildingLayer[x, y].IsBuildingBlocked  = false;

                    // 1. 大本营放置
                    if (terrainLayer[x, y] == TerrainType.BASE_CAMP)
                    {
                        buildingLayer[x, y].HasBuilding      = true;
                        buildingLayer[x, y].BuildingType     = BUILDING_BASE_CAMP;
                        buildingLayer[x, y].IsWorldGenerated = true;
                        buildingLayer[x, y].BuildingLevel    = 1;
                        // 运行时如果怪物占领才会 Block，生成时默认为 false
                        buildingLayer[x, y].IsBuildingBlocked = false; 
                    }
                    // 2. 城邦放置
                    else if (terrainLayer[x, y] == TerrainType.CITY_STATE)
                    {
                        buildingLayer[x, y].HasBuilding      = true;
                        buildingLayer[x, y].BuildingType     = BUILDING_CITY_STATE;
                        buildingLayer[x, y].IsWorldGenerated = true;
                        buildingLayer[x, y].BuildingLevel    = 1;
                        buildingLayer[x, y].IsBuildingBlocked = false;
                    }
                }
            }

            Debug.Log("[CampStatePlacer] 大本营及城邦初始世界建筑放置完成。");
        }
    }
}
