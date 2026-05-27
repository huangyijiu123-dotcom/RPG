using UnityEngine;
using RPG.Map.MonsterFog;

namespace RPG.Map
{
    /// <summary>
    /// 第四层怪物与迷雾主入口 Facade
    /// 职责：初始化第四层数组，并调度战争迷雾开雾及极客怪物分布的生成。
    /// 实现说明书 §6 节。
    /// </summary>
    public static class MonsterFogLayerGen
    {
        /// <summary>
        /// 生成第四层（战争迷雾与极客怪物层）的数据。
        /// </summary>
        /// <param name="seed">随机种子</param>
        /// <param name="store">全局单例存储仓库</param>
        public static void Generate(long seed, MapDataStore store)
        {
            if (store == null)
            {
                Debug.LogError("[MonsterFogLayerGen] store 为 null，生成中止。");
                return;
            }

            if (store.TerrainLayer == null)
            {
                Debug.LogError("[MonsterFogLayerGen] TerrainLayer 为 null，地形层未生成，迷雾怪物层生成中止。");
                return;
            }

            if (store.BuildingLayer == null)
            {
                Debug.LogError("[MonsterFogLayerGen] BuildingLayer 为 null，建筑层未生成，迷雾怪物层生成中止。");
                return;
            }

            // 1. 安全初始化判定：若 store.MonsterFogLayer 为 null，只初始化自身数组
            // 严禁调用 store.InitArrays()，防止冲掉前三层数据
            if (store.MonsterFogLayer == null)
            {
                Debug.LogWarning("[MonsterFogLayerGen] MonsterFogLayer 数组为 null，单独执行实例化初始化。");
                store.MonsterFogLayer = new MonsterFogData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
            }

            Debug.Log("[MonsterFogLayerGen] 开始生成第四层（战争迷雾与极客怪物层）...");

            // 2. 步骤一：初始全图覆盖黑雾，并解锁大本营周围安全区
            Debug.Log("[MonsterFogLayerGen] 步骤1：正在生成战争迷雾覆盖...");
            FogGenerator.GenerateFog(store.MonsterFogLayer);

            // 3. 步骤二：三段式硬阈值刷怪及安全区、世界建筑避让
            Debug.Log("[MonsterFogLayerGen] 步骤2：正在生成极客怪物分布...");
            MonsterGenerator.GenerateMonsters(seed, store.TerrainLayer, store.BuildingLayer, store.MonsterFogLayer);

            Debug.Log("[MonsterFogLayerGen] 第四层（战争迷雾与极客怪物层）生成完成！");
        }
    }
}
