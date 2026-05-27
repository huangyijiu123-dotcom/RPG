using System.Collections.Generic;
using UnityEngine;
using RPG.Map;

namespace RPG.Map.Building
{
    /// <summary>
    /// 世界遗迹与极客奇观生成器
    /// 职责：在遗迹（RUINS）地形上生成 5~8 个世界遗迹；
    ///       在距离大本营 50 格以外的无建筑/无资源的非水域陆地上生成 2~4 个极客奇观（两两间距 >= 20）。
    /// 实现说明书 §5.1 与验证计划修正。
    /// </summary>
    public static class RuinsGenerator
    {
        // ── 遗迹与奇观生成数量限制 ────────────────────────────
        private const int RUINS_MIN = 5;
        private const int RUINS_MAX = 8;

        private const int WONDERS_MIN = 2;
        private const int WONDERS_MAX = 4;

        // 奇观参数
        private const int WONDER_RING_MIN = 50;
        private const int MIN_WONDER_DIST = 20;

        // 防死循环最大尝试次数
        private const int MAX_ATTEMPTS = 5000;

        // 预设遗迹建筑ID（中二极客好玩风格）
        private static readonly string[] RUINS_IDS = {
            "RUINS_GHOST_REPO",      // 幽灵仓库遗迹
            "RUINS_NULL_POINTER",     // 空指针公墓
            "RUINS_STACK_OVERFLOW",   // 栈溢出方尖碑
            "RUINS_MEMORY_LEAK"       // 内存泄漏神庙
        };

        // 预设极客奇观ID
        private static readonly string[] WONDER_IDS = {
            "WONDER_GIL_LOCK",              // GIL全局锁链
            "WONDER_SINGLETON_ALTAR",       // 单例高地台座
            "WONDER_INFINITE_LOOP",         // 无限循环回廊
            "WONDER_RACE_CONDITION_SPIRE"   // 竞态条件之塔
        };

        /// <summary>
        /// 生成遗迹与极客奇观，并将建筑数据写入建筑图层。
        /// </summary>
        /// <param name="seed">随机种子</param>
        /// <param name="terrainLayer">地形层矩阵</param>
        /// <param name="resourceLayer">资源层矩阵（用于规避奇观生长在资源上）</param>
        /// <param name="buildingLayer">建筑层矩阵（将被修改）</param>
        public static void GenerateRuinsAndWonders(
            long seed, 
            TerrainType[,] terrainLayer, 
            ResourceData[,] resourceLayer, 
            BuildingData[,] buildingLayer)
        {
            if (terrainLayer == null || resourceLayer == null || buildingLayer == null)
            {
                Debug.LogError("[RuinsGenerator] 输入的地形、资源或建筑矩阵为 null，遗迹与奇观生成中止。");
                return;
            }

            // 使用确定性种子
            var rng = new System.Random((int)(seed ^ (seed >> 32)));

            int size = MapDataStore.MAP_SIZE;
            int cx   = MapDataStore.CENTER;
            int cy   = MapDataStore.CENTER;

            // ── 步骤 1：寻找并放置遗迹 ─────────────────────────────────────────────
            List<Vector2Int> ruinsTiles = new List<Vector2Int>();
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 仅当是遗迹地形，且未有大本营/城邦建筑占据时，可放置预置遗迹
                    if (terrainLayer[x, y] == TerrainType.RUINS && !buildingLayer[x, y].HasBuilding)
                    {
                        ruinsTiles.Add(new Vector2Int(x, y));
                    }
                }
            }

            // 遗迹容错数量限制：若 RUINS 瓦片不够，优雅缩减至 available 数量，严防溢出
            int targetRuinsCount = Mathf.Min(ruinsTiles.Count, rng.Next(RUINS_MIN, RUINS_MAX + 1));

            if (ruinsTiles.Count > 0 && targetRuinsCount > 0)
            {
                // Fisher-Yates 确定性随机打乱遗迹候选格子，以保证均匀挑选不重合
                for (int i = ruinsTiles.Count - 1; i > 0; i--)
                {
                    int k = rng.Next(i + 1);
                    var temp = ruinsTiles[i];
                    ruinsTiles[i] = ruinsTiles[k];
                    ruinsTiles[k] = temp;
                }

                // 放置遗迹
                for (int i = 0; i < targetRuinsCount; i++)
                {
                    Vector2Int pos = ruinsTiles[i];
                    buildingLayer[pos.x, pos.y].HasBuilding      = true;
                    buildingLayer[pos.x, pos.y].BuildingType     = RUINS_IDS[rng.Next(RUINS_IDS.Length)];
                    buildingLayer[pos.x, pos.y].IsWorldGenerated = true;
                    buildingLayer[pos.x, pos.y].BuildingLevel    = 1;
                    buildingLayer[pos.x, pos.y].IsBuildingBlocked = false;
                }
                Debug.Log($"[RuinsGenerator] 成功生成 {targetRuinsCount} 个中立遗迹建筑。");
            }
            else
            {
                Debug.LogWarning("[RuinsGenerator] 地图中未发现可容纳遗迹的 RUINS 地形，遗迹生成为 0 (兼容容错)。");
            }

            // ── 步骤 2：随机寻找并放置极客奇观 ──────────────────────────────────────
            int targetWonderCount = rng.Next(WONDERS_MIN, WONDERS_MAX + 1);
            int wondersPlaced = 0;
            int attempts      = 0;
            List<Vector2Int> wonderPositions = new List<Vector2Int>(targetWonderCount);

            while (wondersPlaced < targetWonderCount && attempts < MAX_ATTEMPTS)
            {
                attempts++;
                int x = rng.Next(0, size);
                int y = rng.Next(0, size);

                // 条件 1：奇观必须落在中心 50 格圈层之外 (严格大于)
                int ringDistance = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));
                if (ringDistance <= WONDER_RING_MIN) continue;

                // 条件 2：奇观只要落在非水域（WATER）地形上即可（包含草原、冻土、荒野等，征服极端地形）
                if (terrainLayer[x, y] == TerrainType.WATER) continue;

                // 条件 3：该格上绝不能已有任何世界预置建筑（基地/城邦/遗迹）
                if (buildingLayer[x, y].HasBuilding) continue;

                // 条件 4：奇观只落户在空白陆地上，格子里不能有任何森林、矿脉或草药
                if (resourceLayer[x, y].HasForest || 
                    resourceLayer[x, y].HasMineralVein || 
                    resourceLayer[x, y].HasHerbs)
                {
                    continue;
                }

                // 条件 5：与所有已放置的奇观距离必须满足切比雪夫间距 >= 20 格
                bool tooClose = false;
                foreach (var pos in wonderPositions)
                {
                    int distBetween = Mathf.Max(Mathf.Abs(x - pos.x), Mathf.Abs(y - pos.y));
                    if (distBetween < MIN_WONDER_DIST)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // 满足所有严苛条件，放置世界级探索奇观
                buildingLayer[x, y].HasBuilding      = true;
                buildingLayer[x, y].BuildingType     = WONDER_IDS[rng.Next(WONDER_IDS.Length)];
                buildingLayer[x, y].IsWorldGenerated = true;
                buildingLayer[x, y].BuildingLevel    = 1;
                buildingLayer[x, y].IsBuildingBlocked = false;

                wonderPositions.Add(new Vector2Int(x, y));
                wondersPlaced++;
            }

            if (wondersPlaced < targetWonderCount)
            {
                Debug.LogWarning($"[RuinsGenerator] 仅成功放置 {wondersPlaced}/{targetWonderCount} 个奇观，" +
                                 $"尝试次数已达 {MAX_ATTEMPTS} 上限。可能是空白陆地格不足或间距冲突。");
            }
            else
            {
                Debug.Log($"[RuinsGenerator] 成功在偏远陆地圈层生成 {wondersPlaced} 个极客奇观。");
            }
        }
    }
}
