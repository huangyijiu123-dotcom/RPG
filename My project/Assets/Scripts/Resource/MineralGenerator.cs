using UnityEngine;
using RPG.Map;

namespace RPG.Map.Resource
{
    /// <summary>
    /// 矿脉分布生成模块
    /// 职责：仅在山地（MOUNTAIN）地形上，根据噪声六和圈层切比雪夫距离判定矿脉生成并确定矿种。
    /// 实现说明书 §4.2。
    /// </summary>
    public static class MineralGenerator
    {
        // ── 噪声与判定参数（与说明书严格对应） ────────────────────────────
        private const float MINE_NOISE_SCALE = 0.08f;
        private const float BASE_THRESHOLD   = 0.60f;
        private const float DISTANCE_FACTOR  = 0.25f;

        // 圈层界限
        private const int STONE_ZONE_MAX = 20;
        private const int IRON_ZONE_MAX  = 45;

        // 矿物名称定义
        public const string MINERAL_STONE   = "STONE";
        public const string MINERAL_IRON    = "IRON";
        public const string MINERAL_CRYSTAL = "CRYSTAL";

        /// <summary>
        /// 生成全图的矿石资源。
        /// </summary>
        /// <param name="seed">随机种子</param>
        /// <param name="terrainLayer">第一层地形数组</param>
        /// <param name="resourceLayer">待写入的资源层数组（将被修改）</param>
        public static void GenerateMinerals(long seed, TerrainType[,] terrainLayer, ResourceData[,] resourceLayer)
        {
            if (terrainLayer == null || resourceLayer == null)
            {
                Debug.LogError("[MineralGenerator] terrainLayer 或 resourceLayer 为 null，矿物生成中止。");
                return;
            }

            int size = MapDataStore.MAP_SIZE;
            int cx   = MapDataStore.CENTER;
            int cy   = MapDataStore.CENTER;

            // 噪声种子偏移
            float mineOffset = (float)((seed * 23) % 10000);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 默认值重置
                    resourceLayer[x, y].HasMineralVein = false;
                    resourceLayer[x, y].MineralType    = "";

                    // 1. 目标格地形 != MOUNTAIN → 跳过（矿石只在山地上刷新）
                    if (terrainLayer[x, y] != TerrainType.MOUNTAIN) continue;

                    // 计算 Perlin 噪声值并 Clamp01
                    float noiseValue = Mathf.Clamp01(
                        Mathf.PerlinNoise(
                            x * MINE_NOISE_SCALE + mineOffset,
                            y * MINE_NOISE_SCALE + mineOffset
                        )
                    );

                    // 计算切比雪夫距离
                    int ringDistance = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));

                    // 2. 动态判定阈值：noiseValue > (0.60f - (ringDistance / 200f * 0.25f)) → hasMineralVein = true
                    // 注意：随着 ringDistance 的增加，阈值逐渐降低（即中远圈层矿脉越密集）
                    float dynamicThreshold = BASE_THRESHOLD - (ringDistance / 200f * DISTANCE_FACTOR);
                    
                    if (noiseValue > dynamicThreshold)
                    {
                        resourceLayer[x, y].HasMineralVein = true;

                        // 3. 矿种由圈层距离决定
                        if (ringDistance <= STONE_ZONE_MAX)
                        {
                            resourceLayer[x, y].MineralType = MINERAL_STONE;
                        }
                        else if (ringDistance <= IRON_ZONE_MAX)
                        {
                            resourceLayer[x, y].MineralType = MINERAL_IRON;
                        }
                        else
                        {
                            resourceLayer[x, y].MineralType = MINERAL_CRYSTAL;
                        }
                    }
                }
            }

            Debug.Log("[MineralGenerator] 矿石资源生成完成。");
        }
    }
}
