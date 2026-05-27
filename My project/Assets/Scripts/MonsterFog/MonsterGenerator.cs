using UnityEngine;
using RPG.Map;

namespace RPG.Map.MonsterFog
{
    /// <summary>
    /// 极客怪物刷新与建筑避让模块
    /// 职责：使用 Simplex/Perlin 噪声、圈层分段硬阈值对非安全区、非水域、非建筑的格子刷新极客怪物，
    /// 并对有怪物的格子标记 DangerZone 和相应的 DangerLevel 与 MonsterType。
    /// 实现说明书 §6.2。
    /// </summary>
    public static class MonsterGenerator
    {
        // ── 刷怪参数（与说明书严格对应） ──────────────────────────────────
        public const float MONSTER_NOISE_SCALE = 0.10f; // 怪物分布噪声频率

        // 极客怪物 ID 定义
        public const string MONSTER_SLIME               = "SLIME";              // DangerLevel = 1 (黏液怪)
        public const string MONSTER_BUG_KNIGHT          = "BUG_KNIGHT";         // DangerLevel = 2 (Bug骑士)
        public const string MONSTER_NULL_GHOST          = "NULL_GHOST";         // DangerLevel = 3 (空指针幽灵)
        public const string MONSTER_DEADLOCK_GOLEM      = "DEADLOCK_GOLEM";     // DangerLevel = 4 (死锁魔像)
        public const string MONSTER_MEMORY_LEAK_TITAN   = "MEMORY_LEAK_TITAN";  // DangerLevel = 5 (内存泄漏巨兽，BOSS级)

        // 三段式分段硬阈值
        public const float THRESHOLD_NEAR = 0.75f; // 10 < ringDistance <= 25 阈值
        public const float THRESHOLD_MID  = 0.60f; // 25 < ringDistance <= 50 阈值
        public const float THRESHOLD_FAR  = 0.45f; // ringDistance > 50 阈值

        /// <summary>
        /// 刷新全图的怪物分布。
        /// </summary>
        /// <param name="seed">随机种子</param>
        /// <param name="terrainLayer">第一层已生成的地形矩阵</param>
        /// <param name="buildingLayer">第三层已生成的建筑矩阵</param>
        /// <param name="monsterFogLayer">待写入的第四层迷雾怪物矩阵</param>
        public static void GenerateMonsters(long seed, TerrainType[,] terrainLayer, BuildingData[,] buildingLayer, MonsterFogData[,] monsterFogLayer)
        {
            if (terrainLayer == null || buildingLayer == null || monsterFogLayer == null)
            {
                Debug.LogError("[MonsterGenerator] 输入层矩阵为 null，怪物生成中止。");
                return;
            }

            int size = MapDataStore.MAP_SIZE;
            int cx   = MapDataStore.CENTER;
            int cy   = MapDataStore.CENTER;

            // 噪声种子哈希偏移量，长整型防溢出
            float monsterOffset = (float)((seed * 37L) % 10000L);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 1. 默认重置重写该格除 IsExplored 之外的状态
                    monsterFogLayer[x, y].IsDangerZone = false;
                    monsterFogLayer[x, y].DangerLevel  = 0;
                    monsterFogLayer[x, y].MonsterType  = "";
                    monsterFogLayer[x, y].HasMonster   = false;

                    // 2. 避让法则1：目标格地形是水域（WATER） → 绝对不刷怪，跳过
                    if (terrainLayer[x, y] == TerrainType.WATER) continue;

                    // 3. 避让法则2：大本营安全区（ringDistance <= 10） → 绝对不刷怪，跳过（严格隔离）
                    int ringDistance = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));
                    if (ringDistance <= 10) continue;

                    // 4. 避让法则3：该格子已有世界建筑/奇观（HasBuilding == true） → 绝对不刷怪，且不可触碰 IsBuildingBlocked，直接跳过
                    if (buildingLayer[x, y].HasBuilding) continue;

                    // 5. 计算 Perlin 噪声值，Clamp01 确保范围 [0, 1]
                    float noiseValue = Mathf.Clamp01(
                        Mathf.PerlinNoise(
                            x * MONSTER_NOISE_SCALE + monsterOffset,
                            y * MONSTER_NOISE_SCALE + monsterOffset
                        )
                    );

                    bool spawnMonster = false;
                    int calculatedDangerLevel = 0;

                    // 6. 三段式固定噪声判定与确定性哈希危险级分配
                    // 近圈 10 ~ 25
                    if (ringDistance <= 25)
                    {
                        if (noiseValue > THRESHOLD_NEAR)
                        {
                            spawnMonster = true;
                            calculatedDangerLevel = 1;
                        }
                    }
                    // 中圈 25 ~ 50
                    else if (ringDistance <= 50)
                    {
                        if (noiseValue > THRESHOLD_MID)
                        {
                            spawnMonster = true;
                            // 确定性分配 DangerLevel = 2 或 3
                            long cellHash = seed + x * 3137L + y * 7139L;
                            calculatedDangerLevel = 2 + (int)(((cellHash % 2L) + 2L) % 2L);
                        }
                    }
                    // 外圈 50 以上
                    else
                    {
                        if (noiseValue > THRESHOLD_FAR)
                        {
                            spawnMonster = true;
                            // 确定性分配 DangerLevel = 3、4 或 5
                            long cellHash = seed + x * 3137L + y * 7139L;
                            calculatedDangerLevel = 3 + (int)(((cellHash % 3L) + 3L) % 3L);
                        }
                    }

                    // 7. 写入刷怪状态与极客 ID 映射
                    if (spawnMonster)
                    {
                        monsterFogLayer[x, y].HasMonster   = true;
                        monsterFogLayer[x, y].IsDangerZone = true;
                        monsterFogLayer[x, y].DangerLevel  = calculatedDangerLevel;

                        // 极客怪物 ID 转换映射
                        switch (calculatedDangerLevel)
                        {
                            case 1:
                                monsterFogLayer[x, y].MonsterType = MONSTER_SLIME;
                                break;
                            case 2:
                                monsterFogLayer[x, y].MonsterType = MONSTER_BUG_KNIGHT;
                                break;
                            case 3:
                                monsterFogLayer[x, y].MonsterType = MONSTER_NULL_GHOST;
                                break;
                            case 4:
                                monsterFogLayer[x, y].MonsterType = MONSTER_DEADLOCK_GOLEM;
                                break;
                            case 5:
                                monsterFogLayer[x, y].MonsterType = MONSTER_MEMORY_LEAK_TITAN;
                                break;
                            default:
                                monsterFogLayer[x, y].MonsterType = MONSTER_SLIME; // 安全兜底
                                break;
                        }
                    }
                }
            }

            Debug.Log("[MonsterGenerator] 极客怪物初始分布生成完成。");
        }
    }
}
