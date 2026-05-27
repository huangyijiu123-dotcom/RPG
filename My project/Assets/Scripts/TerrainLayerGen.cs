using UnityEngine;
using RPG.Map;
using RPG.Map.Terrain;

/// <summary>
/// 第一层地形与气候生成的主入口（Facade）
/// 职责：按顺序统筹调度四个子模块，将生成结果填充到 MapDataStore 中。
/// 
/// 执行顺序：
///   1. ClimateGenerator   → 计算静态气候属性矩阵（噪声生成）
///   2. TerrainEvaluator   → 逐格判定地形类型
///   3. BaseCampProtector  → 强制设置大本营保护区（覆盖噪声结果）
///   4. CityStateGenerator → 放置 3 个城邦
/// </summary>
public static class TerrainLayerGen
{
    /// <summary>
    /// 统筹生成第一层（地形 + 气候）并将结果写入 MapDataStore。
    /// </summary>
    /// <param name="seed">随机种子</param>
    /// <param name="store">全局数据仓库（将被修改）</param>
    public static void Generate(long seed, MapDataStore store)
    {
        if (store == null)
        {
            Debug.LogError("[TerrainLayerGen] MapDataStore 为 null，地形生成中止。");
            return;
        }

        int size = MapDataStore.MAP_SIZE;

        // ── 步骤 1：生成气候矩阵 ─────────────────────────────────────────────
        Debug.Log("[TerrainLayerGen] 步骤1：生成气候矩阵（高度/湿度/温度）...");
        ClimateData[,] climateData = ClimateGenerator.GenerateClimate(seed);

        // 将气候数据写入 store
        store.RawClimateLayer = climateData;
        store.ClimateController = new StaticClimateController(climateData);

        // ── 步骤 2：逐格判定地形类型 ─────────────────────────────────────────
        Debug.Log("[TerrainLayerGen] 步骤2：逐格评估地形类型（决策树）...");
        // 使用种子驱动的随机数生成器，确保遗迹概率分布稳定可复现
        var rng = new System.Random((int)(seed ^ (seed >> 32)));

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dangerBias   = TerrainEvaluator.CalculateDangerBias(x, y);
                float randomValue  = (float)rng.NextDouble();
                ClimateData climate = climateData[x, y];

                store.TerrainLayer[x, y] = TerrainEvaluator.EvaluateCell(x, y, climate, dangerBias, randomValue);
            }
        }

        // ── 步骤 3：大本营保护区覆盖 ─────────────────────────────────────────
        Debug.Log("[TerrainLayerGen] 步骤3：应用大本营保护区覆盖...");
        BaseCampProtector.ApplyProtection(store.TerrainLayer);

        // ── 步骤 4：放置城邦 ─────────────────────────────────────────────────
        Debug.Log("[TerrainLayerGen] 步骤4：搜寻并放置城邦...");
        CityStateGenerator.PlaceCityStates(seed, store.TerrainLayer);

        Debug.Log("[TerrainLayerGen] 第一层（地形与气候）生成完成！");
    }
}
