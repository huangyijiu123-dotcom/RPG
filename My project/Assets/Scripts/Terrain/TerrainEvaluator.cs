using UnityEngine;
using RPG.Map;

namespace RPG.Map.Terrain
{
    /// <summary>
    /// 地形判定决策树模块
    /// 职责：根据气候值（高度/湿度/温度）和圈层距离，决定单元格最终地形类型。
    /// 实现说明书 §3.3（圈层系数）和 §3.4（地形决策树）。
    /// </summary>
    public static class TerrainEvaluator
    {
        // ── 圈层边界（切比雪夫距离，说明书 §3.3） ────────────────────────────
        private const float SAFE_ZONE_RADIUS       = 10f;
        private const float TRANSITION_ZONE_RADIUS = 25f;
        private const float DANGER_ZONE_RADIUS     = 50f;

        // ── 圈层 dangerBias 系数（偏向极端地形） ──────────────────────────────
        private const float BIAS_SAFE        = 0.00f;
        private const float BIAS_TRANSITION  = 0.10f;
        private const float BIAS_DANGER      = 0.20f;
        private const float BIAS_EXTREME     = 0.35f;

        // ── 地形判定阈值（说明书 §3.4） ──────────────────────────────────────
        private const float WATER_THRESHOLD    = 0.18f;   // finalAltitude < 此值 → WATER
        private const float MOUNTAIN_THRESHOLD = 0.60f;   // finalAltitude > 此值 → MOUNTAIN
        private const float SWAMP_HUMIDITY_MIN = 0.65f;   // 湿度高
        private const float SWAMP_TEMP_MAX     = 0.45f;   // 温度低 → SWAMP

        // ── 极危险圈层地形触发参数 ───────────────────────────────────────────
        private const float EXTREME_RING_MIN   = 70f;
        private const float VOLCANO_TEMP_MIN   = 0.75f;   // 极热 → VOLCANO
        private const float TUNDRA_TEMP_MAX    = 0.25f;   // 极寒 → TUNDRA

        // ── 遗迹触发参数（山地格子的一定概率变为遗迹） ───────────────────────
        private const float RUINS_RING_MIN     = 40f;
        private const float RUINS_CHANCE       = 0.08f;   // 8% 概率

        /// <summary>
        /// 根据圈层距离计算该格的 dangerBias（越远偏向越强）。
        /// </summary>
        public static float CalculateDangerBias(int x, int y)
        {
            float ringDistance = Mathf.Max(
                Mathf.Abs(x - MapDataStore.CENTER),
                Mathf.Abs(y - MapDataStore.CENTER));

            if (ringDistance <= SAFE_ZONE_RADIUS)       return BIAS_SAFE;
            if (ringDistance <= TRANSITION_ZONE_RADIUS) return BIAS_TRANSITION;
            if (ringDistance <= DANGER_ZONE_RADIUS)     return BIAS_DANGER;
            return BIAS_EXTREME;
        }

        /// <summary>
        /// 根据气候数据和圈层偏向决定该格的地形枚举。
        /// </summary>
        /// <param name="x">格子 X 坐标</param>
        /// <param name="y">格子 Y 坐标</param>
        /// <param name="climate">该格气候数据（已含融合后的最终高度）</param>
        /// <param name="dangerBias">圈层偏向系数（由 CalculateDangerBias 得到）</param>
        /// <param name="randomValue">一个 [0,1) 的随机数（用于遗迹概率判定）</param>
        /// <returns>决策后的地形类型</returns>
        public static TerrainType EvaluateCell(int x, int y, ClimateData climate, float dangerBias, float randomValue)
        {
            float ringDistance  = Mathf.Max(
                Mathf.Abs(x - MapDataStore.CENTER),
                Mathf.Abs(y - MapDataStore.CENTER));

            // 将 dangerBias 叠加到高度，让边缘地带更偏向极端地形
            float finalAltitude = Mathf.Clamp01(climate.Altitude + dangerBias);

            // ── 步骤1：极危险圈层强制覆盖（>= 70格）────────────────────────
            if (ringDistance >= EXTREME_RING_MIN)
            {
                if (climate.Temperature > VOLCANO_TEMP_MIN) return TerrainType.VOLCANO;
                if (climate.Temperature < TUNDRA_TEMP_MAX)  return TerrainType.TUNDRA;
            }

            // ── 步骤2：水域判断（高优先级，成片连通）────────────────────────
            if (finalAltitude < WATER_THRESHOLD) return TerrainType.WATER;

            // ── 步骤3：山地与遗迹判断 ────────────────────────────────────────
            if (finalAltitude > MOUNTAIN_THRESHOLD)
            {
                // 中远圈层的山地有小概率成为遗迹
                if (ringDistance >= RUINS_RING_MIN && randomValue < RUINS_CHANCE)
                    return TerrainType.RUINS;
                return TerrainType.MOUNTAIN;
            }

            // ── 步骤4：草原与特殊地形（中等高度值）──────────────────────────
            if (climate.Humidity > SWAMP_HUMIDITY_MIN && climate.Temperature < SWAMP_TEMP_MAX)
                return TerrainType.SWAMP;

            return TerrainType.PLAINS;
        }
    }
}
