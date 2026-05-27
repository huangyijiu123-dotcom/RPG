using System.Collections.Generic;
using UnityEngine;
using RPG.Map;

namespace RPG.Map.Terrain
{
    /// <summary>
    /// 城邦搜寻与放置模块
    /// 职责：在中心 60 格之外的草原上，寻找并放置 3 个城邦（CITY_STATE），
    /// 要求两两间距 >= 40 格（切比雪夫距离）。
    /// 实现说明书 §3.5。
    /// </summary>
    public static class CityStateGenerator
    {
        private const int CITY_STATE_COUNT        = 3;    // 固定生成 3 个城邦
        private const float MIN_DIST_FROM_CENTER  = 60f;  // 城邦与大本营的最小距离
        private const float MIN_DIST_BETWEEN      = 40f;  // 城邦两两之间的最小距离
        private const int MAX_ATTEMPTS            = 2000; // 最大随机尝试次数，防止死循环

        /// <summary>
        /// 在已生成的地形数组上放置城邦。
        /// </summary>
        /// <param name="seed">随机种子</param>
        /// <param name="terrainLayer">当前地形数组（将被修改）</param>
        public static void PlaceCityStates(long seed, TerrainType[,] terrainLayer)
        {
            if (terrainLayer == null)
            {
                Debug.LogError("[CityStateGenerator] terrainLayer 为 null，城邦放置跳过。");
                return;
            }

            // 使用固定种子以保证同种子地图城邦位置稳定
            var rng = new System.Random((int)(seed ^ (seed >> 32)));
            var placedPositions = new List<Vector2Int>(CITY_STATE_COUNT);

            int size = MapDataStore.MAP_SIZE;
            int cx   = MapDataStore.CENTER;
            int cy   = MapDataStore.CENTER;
            int attempts = 0;

            while (placedPositions.Count < CITY_STATE_COUNT && attempts < MAX_ATTEMPTS)
            {
                attempts++;
                int x = rng.Next(0, size);
                int y = rng.Next(0, size);

                // 条件1：城邦必须在中心 60 格之外
                float distFromCenter = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));
                if (distFromCenter < MIN_DIST_FROM_CENTER) continue;

                // 条件2：城邦只能落在草原上
                if (terrainLayer[x, y] != TerrainType.PLAINS) continue;

                // 条件3：与已放置城邦两两间距 >= 40
                bool tooClose = false;
                foreach (var pos in placedPositions)
                {
                    float distBetween = Mathf.Max(Mathf.Abs(x - pos.x), Mathf.Abs(y - pos.y));
                    if (distBetween < MIN_DIST_BETWEEN)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // 所有条件通过，放置城邦
                terrainLayer[x, y] = TerrainType.CITY_STATE;
                placedPositions.Add(new Vector2Int(x, y));
            }

            // 如果尝试次数耗尽仍未放满，记录警告（非崩溃性错误）
            if (placedPositions.Count < CITY_STATE_COUNT)
            {
                Debug.LogWarning($"[CityStateGenerator] 仅成功放置 {placedPositions.Count}/{CITY_STATE_COUNT} 个城邦，" +
                                 $"尝试次数已达 {MAX_ATTEMPTS} 次上限。可能是地图草原区域不足。");
            }
            else
            {
                Debug.Log($"[CityStateGenerator] 成功放置 {CITY_STATE_COUNT} 个城邦。");
            }
        }
    }
}
