using UnityEngine;
using RPG.Map;

namespace RPG.Map.Resource
{
    /// <summary>
    /// 森林分布生成模块
    /// 职责：仅在草原（PLAINS）地形上生成森林和森林密度，并对大本营周围进行减密修正。
    /// 实现说明书 §4.1。
    /// </summary>
    public static class ForestGenerator
    {
        // ── 噪声与判定参数（与说明书严格对应） ────────────────────────────
        private const float FOREST_NOISE_SCALE = 0.06f;
        private const float FOREST_THRESHOLD   = 0.55f;
        
        // 减密半径与系数
        private const int FADE_RADIUS         = 12;
        private const float FADE_MULTIPLIER   = 0.6f;

        /// <summary>
        /// 生成全图的森林资源。
        /// </summary>
        /// <param name="seed">随机种子</param>
        /// <param name="terrainLayer">第一层已生成好的地形矩阵</param>
        /// <param name="resourceLayer">待写入的资源矩阵（将被修改）</param>
        public static void GenerateForests(long seed, TerrainType[,] terrainLayer, ResourceData[,] resourceLayer)
        {
            if (terrainLayer == null || resourceLayer == null)
            {
                Debug.LogError("[ForestGenerator] terrainLayer 或 resourceLayer 为 null，森林生成中止。");
                return;
            }

            int size = MapDataStore.MAP_SIZE;
            int cx   = MapDataStore.CENTER;
            int cy   = MapDataStore.CENTER;

            // 噪声偏移量
            float forestOffset = (float)((seed * 17) % 10000);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 默认值重置
                    resourceLayer[x, y].HasForest     = false;
                    resourceLayer[x, y].ForestDensity = 0;

                    // 1. 目标格地形 != PLAINS → 跳过，不生成森林（森林仅生在草原上）
                    if (terrainLayer[x, y] != TerrainType.PLAINS) continue;

                    // 计算 Perlin 噪声值并 Clamp01 确保在 [0, 1] 范围内
                    float noiseValue = Mathf.Clamp01(
                        Mathf.PerlinNoise(
                            x * FOREST_NOISE_SCALE + forestOffset,
                            y * FOREST_NOISE_SCALE + forestOffset
                        )
                    );

                    // 2. noiseValue > 0.55f → hasForest = true
                    if (noiseValue > FOREST_THRESHOLD)
                    {
                        resourceLayer[x, y].HasForest = true;

                        // 3. forestDensity = (int)((noiseValue - 0.55f) / 0.45f * 100f)（归一化映射到 0~100）
                        float rawDensity = (noiseValue - FOREST_THRESHOLD) / (1.0f - FOREST_THRESHOLD) * 100f;
                        int density = Mathf.Clamp((int)rawDensity, 0, 100);

                        // 4. 圈层修正：在大本营周围（ringDistance < 12），森林密度折减
                        int ringDistance = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));
                        if (ringDistance < FADE_RADIUS)
                        {
                            density = (int)(density * FADE_MULTIPLIER);
                        }

                        resourceLayer[x, y].ForestDensity = Mathf.Clamp(density, 0, 100);
                    }
                }
            }

            Debug.Log("[ForestGenerator] 森林资源生成完成。");
        }
    }
}
