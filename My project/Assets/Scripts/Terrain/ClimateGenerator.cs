using UnityEngine;
using RPG.Map;

namespace RPG.Map.Terrain
{
    /// <summary>
    /// 气候与噪声生成模块
    /// 职责：使用四套独立柏林噪声，计算整张地图的高度、湿度、温度及河流方向字段，
    /// 返回 MAP_SIZE × MAP_SIZE 的静态气候属性矩阵。
    ///
    /// ⚠️ 约定：ClimateData.Altitude 存储的是【河流融合后】的高度值
    ///     （= rawAltitude × 0.7 + riverNoise × 0.3），而非原始柏林噪声值。
    ///     TerrainEvaluator.EvaluateCell 会在此基础上临时叠加 dangerBias，
    ///     计算 finalAltitude 用于地形判定，但不会写回 ClimateData，保持数据干净。
    /// </summary>
    public static class ClimateGenerator
    {
        // ── 噪声参数（与说明书 §3.1/§3.2 严格对应） ──────────────────────────
        private const float ALTITUDE_SCALE     = 0.04f;
        private const float HUMIDITY_SCALE     = 0.035f;
        private const float TEMPERATURE_SCALE  = 0.025f;
        private const float RIVER_SCALE        = 0.015f;   // 极低频，让水域形成长河走廊

        // 河流噪声融合权重
        private const float ALTITUDE_WEIGHT    = 0.7f;
        private const float RIVER_WEIGHT       = 0.3f;

        /// <summary>
        /// 生成整张地图的静态气候矩阵。
        /// </summary>
        /// <param name="seed">随机种子（与 MapDataStore.CurrentSeed 对应）</param>
        /// <returns>MAP_SIZE × MAP_SIZE 的 ClimateData 数组</returns>
        public static ClimateData[,] GenerateClimate(long seed)
        {
            int size = MapDataStore.MAP_SIZE;
            var result = new ClimateData[size, size];

            // 根据种子计算各套噪声的偏移量，确保四套物理场互相独立
            float altitudeOffset    = (float)(seed % 10000);
            float humidityOffset    = (float)((seed * 3) % 10000);
            float temperatureOffset = (float)((seed * 7) % 10000);
            float riverOffset       = (float)((seed * 13) % 10000);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 原始高度值（柏林噪声）
                    float rawAltitude = Mathf.Clamp01(
                        Mathf.PerlinNoise(
                            x * ALTITUDE_SCALE + altitudeOffset,
                            y * ALTITUDE_SCALE + altitudeOffset));

                    // 河流走向噪声（极低频，让水域形成长条河流）
                    float riverNoise = Mathf.Clamp01(
                        Mathf.PerlinNoise(
                            x * RIVER_SCALE + riverOffset,
                            y * RIVER_SCALE + riverOffset));

                    // 融合高度 = 原始高度 * 0.7 + 河流噪声 * 0.3
                    // 此值作为 ClimateData.Altitude 存储，是后续地形判定的起点，
                    // dangerBias 由 TerrainEvaluator 在判定时临时叠加，不在此处写入。
                    float fusedAltitude = Mathf.Clamp01(rawAltitude * ALTITUDE_WEIGHT + riverNoise * RIVER_WEIGHT);

                    // 湿度
                    float humidity = Mathf.Clamp01(
                        Mathf.PerlinNoise(
                            x * HUMIDITY_SCALE + humidityOffset,
                            y * HUMIDITY_SCALE + humidityOffset));

                    // 温度
                    float temperature = Mathf.Clamp01(
                        Mathf.PerlinNoise(
                            x * TEMPERATURE_SCALE + temperatureOffset,
                            y * TEMPERATURE_SCALE + temperatureOffset));

                    result[x, y] = new ClimateData(fusedAltitude, humidity, temperature);
                }
            }

            return result;
        }
    }
}
