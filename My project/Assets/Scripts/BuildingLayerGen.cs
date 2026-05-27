using UnityEngine;
using RPG.Map.Building;

namespace RPG.Map
{
    /// <summary>
    /// 第三层建筑分布生成主入口（Facade）
    /// 职责：按顺序统筹调度初始大本营与城邦的建筑放置，再调度世界遗迹和极客奇观的随机生成。
    /// 
    /// 执行顺序：
    ///   1. CampStatePlacer  → 放置中心大本营初始建筑以及第一层规划好的 3 个中立城邦初始建筑
    ///   2. RuinsGenerator   → 在遗迹地形格子上生成 5~8 个世界遗迹，在远圈层草原空白陆地上生成 2~4 个极客奇观
    /// </summary>
    public static class BuildingLayerGen
    {
        /// <summary>
        /// 统筹生成第三层（建筑层）并将结果写入 MapDataStore 中。
        /// </summary>
        /// <param name="seed">随机种子</param>
        /// <param name="store">全局唯一数据仓库</param>
        public static void Generate(long seed, MapDataStore store)
        {
            if (store == null)
            {
                Debug.LogError("[BuildingLayerGen] MapDataStore 为 null，建筑生成中止。");
                return;
            }

            if (store.TerrainLayer == null)
            {
                Debug.LogError("[BuildingLayerGen] TerrainLayer 为 null，地形层未生成，建筑层生成中止。");
                return;
            }

            if (store.ResourceLayer == null)
            {
                Debug.LogError("[BuildingLayerGen] ResourceLayer 为 null，资源层未生成，奇观定位因无法规避资源格而中止。");
                return;
            }

            // 兜底：如果建筑图层数组为 null，单独为它进行初始化分配内存，绝不使用 InitArrays 以防重置地形层
            if (store.BuildingLayer == null)
            {
                Debug.LogWarning("[BuildingLayerGen] BuildingLayer 数组为 null，单独执行实例化初始化。");
                store.BuildingLayer = new BuildingData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
            }

            Debug.Log("[BuildingLayerGen] 开始生成第三层（世界建筑分布层）...");

            // ── 步骤 1：放置初始大本营与城邦 ───────────────────────────────────────
            Debug.Log("[BuildingLayerGen] 步骤1：放置基地和大本营、城邦初始世界建筑...");
            CampStatePlacer.PlaceInitialBuildings(store.TerrainLayer, store.BuildingLayer);

            // ── 步骤 2：生成遗迹与奇观 ───────────────────────────────────────────
            Debug.Log("[BuildingLayerGen] 步骤2：正在生成废弃遗迹与世界极客奇观...");
            RuinsGenerator.GenerateRuinsAndWonders(seed, store.TerrainLayer, store.ResourceLayer, store.BuildingLayer);

            Debug.Log("[BuildingLayerGen] 第三层（建筑层）生成完成！");
        }
    }
}
