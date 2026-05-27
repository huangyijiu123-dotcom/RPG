using UnityEngine;
using RPG.Map.Resource;

namespace RPG.Map
{
    /// <summary>
    /// 第二层资源分布生成主入口（Facade）
    /// 职责：按顺序统筹调度森林生成器、矿石生成器和草药生成器，
    /// 将生成的资源信息写入 MapDataStore.ResourceLayer。
    /// 
    /// 执行顺序：
    ///   1. ForestGenerator   → 生成草原上的森林及密度，并对大本营附近进行减密修正
    ///   2. MineralGenerator  → 生成山地上的矿脉（基础石材、铁矿石、算力晶体）
    ///   3. HerbsGenerator    → 在沼泽地中采用哈希确定性散列生成草药
    /// </summary>
    public static class ResourceLayerGen
    {
        /// <summary>
        /// 统筹生成第二层（资源层）并将结果写入 MapDataStore 中。
        /// </summary>
        /// <param name="seed">随机种子</param>
        /// <param name="store">全局唯一数据仓库</param>
        public static void Generate(long seed, MapDataStore store)
        {
            if (store == null)
            {
                Debug.LogError("[ResourceLayerGen] MapDataStore 为 null，资源生成中止。");
                return;
            }

            if (store.TerrainLayer == null)
            {
                Debug.LogError("[ResourceLayerGen] TerrainLayer 为 null，地形层未生成，资源层生成必须依赖地形层。生成中止。");
                return;
            }

            // 兜底：如果资源层数组未被初始化，则在此单独初始化，不能使用 InitArrays 以防重置地形层
            if (store.ResourceLayer == null)
            {
                Debug.LogWarning("[ResourceLayerGen] ResourceLayer 数组为 null，单独执行实例化初始化。");
                store.ResourceLayer = new ResourceData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
            }

            Debug.Log("[ResourceLayerGen] 开始生成第二层（资源分布层）...");

            // ── 步骤 1：生成森林 ─────────────────────────────────────────────────
            Debug.Log("[ResourceLayerGen] 步骤1：正在生成森林资源...");
            ForestGenerator.GenerateForests(seed, store.TerrainLayer, store.ResourceLayer);

            // ── 步骤 2：生成矿脉 ─────────────────────────────────────────────────
            Debug.Log("[ResourceLayerGen] 步骤2：正在生成矿物资源...");
            MineralGenerator.GenerateMinerals(seed, store.TerrainLayer, store.ResourceLayer);

            // ── 步骤 3：生成草药 ─────────────────────────────────────────────────
            Debug.Log("[ResourceLayerGen] 步骤3：正在生成草药资源...");
            HerbsGenerator.GenerateHerbs(seed, store.TerrainLayer, store.ResourceLayer);

            Debug.Log("[ResourceLayerGen] 第二层（资源层）生成完成！");
        }
    }
}
