using UnityEngine;
using RPG.Map;

namespace RPG.Map.Terrain
{
    /// <summary>
    /// 大本营保护区安全模块
    /// 职责：强制执行中心大本营周围的安全区覆盖。
    /// 实现说明书 §3.4 步骤5：半径 5 格内强制设为 PLAINS，中心点设为 BASE_CAMP。
    /// </summary>
    public static class BaseCampProtector
    {
        // 保护区半径（说明书 §3.4 步骤5：半径 5 格内）
        private const int PROTECTION_RADIUS = 5;

        /// <summary>
        /// 对整张地形图应用大本营保护区，覆盖之前噪声生成的地形。
        /// </summary>
        /// <param name="terrainLayer">需要被修改的地形数组</param>
        public static void ApplyProtection(TerrainType[,] terrainLayer)
        {
            if (terrainLayer == null)
            {
                Debug.LogError("[BaseCampProtector] terrainLayer 为 null，无法应用大本营保护。");
                return;
            }

            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;

            for (int dx = -PROTECTION_RADIUS; dx <= PROTECTION_RADIUS; dx++)
            {
                for (int dy = -PROTECTION_RADIUS; dy <= PROTECTION_RADIUS; dy++)
                {
                    int x = cx + dx;
                    int y = cy + dy;

                    // 确保坐标在地图范围内
                    if (!MapDataStore.IsValidCoordinate(x, y)) continue;

                    // 使用切比雪夫距离判定是否在保护区内
                    float dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                    if (dist > PROTECTION_RADIUS) continue;

                    // 中心点本身设为 BASE_CAMP，其余强制为 PLAINS
                    terrainLayer[x, y] = (dx == 0 && dy == 0)
                        ? TerrainType.BASE_CAMP
                        : TerrainType.PLAINS;
                }
            }
        }
    }
}
