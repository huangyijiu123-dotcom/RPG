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
        // 注意：每个格子使用独立的哈希随机数，与循环顺序完全无关。
        // 保证同种子地图可完美复现（存档读档后一致）。

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dangerBias  = TerrainEvaluator.CalculateDangerBias(x, y);
                float randomValue = GetCellRandom(seed, x, y); // 确定性逐格哈希
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

    /// <summary>
    /// 逐格确定性哈希随机数生成器。
    /// 返回 [0, 1) 范围内的浮点数，完全由 (seed, x, y) 决定，与循环顺序无关。
    /// 天然支持存档读档后的途径复现。
    /// </summary>
    private static float GetCellRandom(long seed, int x, int y)
    {
        // 使用经典整数哈希常数，将 (seed, x, y) 混入生成高分布的伪随机种子
        long h = seed ^ ((long)x * 374761393L + (long)y * 668265263L);
        var cellRng = new System.Random((int)(h ^ (h >> 32)));
        return (float)cellRng.NextDouble();
    }
}
