using UnityEngine;
using RPG.Map;

namespace RPG.Map.Resource
{
    /// <summary>
    /// 草药分布生成模块
    /// 职责：仅在沼泽（SWAMP）地形上，采用 40% 概率的确定性哈希散列算法生成草药。
    /// 实现说明书 §4.3。
    /// </summary>
    public static class HerbsGenerator
    {
        /// <summary>
        /// 生成全图的草药资源。
        /// </summary>
        /// <param name="seed">随机种子</param>
        /// <param name="terrainLayer">第一层地形数组</param>
        /// <param name="resourceLayer">待写入的资源层数组（将被修改）</param>
        public static void GenerateHerbs(long seed, TerrainType[,] terrainLayer, ResourceData[,] resourceLayer)
        {
            if (terrainLayer == null || resourceLayer == null)
            {
                Debug.LogError("[HerbsGenerator] terrainLayer 或 resourceLayer 为 null，草药生成中止。");
                return;
            }

            int size = MapDataStore.MAP_SIZE;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 默认值重置
                    resourceLayer[x, y].HasHerbs = false;

                    // 1. 目标格地形 != SWAMP → 跳过（草药是沼泽专属资源）
                    if (terrainLayer[x, y] != TerrainType.SWAMP) continue;

                    // 2. 使用简单的确定性哈希散列算法，有 40% 的概率在沼泽地上生成草药：
                    //    (x * 31 + y * 17 + seed) % 10 < 4
                    //    注意使用 long 类型防止计算溢出，并利用归正取模算法解决负数和 long.MinValue 溢出问题
                    long hashVal = x * 31L + y * 17L + seed;
                    if (((hashVal % 10L) + 10L) % 10L < 4)
                    {
                        resourceLayer[x, y].HasHerbs = true;
                    }
                }
            }

            Debug.Log("[HerbsGenerator] 草药资源生成完成。");
        }
    }
}
