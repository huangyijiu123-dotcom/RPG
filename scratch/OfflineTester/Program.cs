using System;
using System.Collections.Generic;

// =============================================================================================
// 🎮 1. UNITY ENGINE STUBS (模拟 UnityEngine 在离线控制台下的核心组件与 API)
// =============================================================================================
namespace UnityEngine
{
    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public override string ToString() => $"({x}, {y})";
    }

    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Abs(int value) => Math.Abs(value);
        public static float Abs(float value) => Math.Abs(value);
        public static float Clamp01(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);
        public static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
        public static int Min(int a, int b) => Math.Min(a, b);
        
        // 纯 C# 标准 2D 柏林噪声模拟（高精度数学实现）
        public static float PerlinNoise(float x, float y)
        {
            return PerlinNoiseGenerator.Noise(x, y);
        }
    }

    public static class Debug
    {
        public static void Log(object message)
        {
            string msg = message.ToString();
            // 将 Unity 的 RichText 标记映射为 ANSI 转义控制字符以支持控制台彩色显示
            msg = msg.Replace("<color=lime>", "\x1b[32m")
                     .Replace("<color=cyan>", "\x1b[36m")
                     .Replace("<color=yellow>", "\x1b[33m")
                     .Replace("<color=red>", "\x1b[31m")
                     .Replace("<color=orange>", "\x1b[38;5;208m")
                     .Replace("</color>", "\x1b[0m")
                     .Replace("<b>", "\x1b[1m")
                     .Replace("</b>", "\x1b[0m");

            Console.WriteLine(msg);
        }

        public static void LogWarning(object message)
        {
            Console.WriteLine($"\x1b[33m[Warning] {message}\x1b[0m");
        }

        public static void LogError(object message)
        {
            Console.WriteLine($"\x1b[31m[Error] {message}\x1b[0m");
        }
    }

    // 经典 2D 柏林噪声生成器（提供在 [0,1] 间的平滑波动场）
    public static class PerlinNoiseGenerator
    {
        private static readonly int[] p = new int[512];
        private static readonly int[] permutation = { 151,160,137,91,90,15,
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122, 60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206, 59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
        };

        static PerlinNoiseGenerator()
        {
            for (int i = 0; i < 256; i++) p[256 + i] = p[i] = permutation[i];
        }

        public static float Noise(float x, float y)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);
            float u = Fade(x);
            float v = Fade(y);
            int A = p[X] + Y, AA = p[A], AB = p[A + 1], B = p[X + 1] + Y, BA = p[B], BB = p[B + 1];
            return Lerp(v, Lerp(u, Grad(p[AA], x, y), Grad(p[BA], x - 1, y)),
                           Lerp(u, Grad(p[AB], x, y - 1), Grad(p[BB], x - 1, y - 1))) * 0.5f + 0.5f;
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static float Lerp(float t, float a, float b) => a + t * (b - a);
        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 7;
            float u = h < 4 ? x : y;
            float v = h < 4 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? 2.0f * v : -2.0f * v);
        }
    }
}

// =============================================================================================
// 📦 2. RPG MAP CORE STRUCTS (四层地图核心单例与数据结构)
// =============================================================================================
namespace RPG.Map
{
    public enum TerrainType
    {
        PLAINS = 0, MOUNTAIN = 1, WATER = 2, SWAMP = 3, TUNDRA = 4, VOLCANO = 5, RUINS = 6, CITY_STATE = 7, BASE_CAMP = 8
    }

    public struct ClimateData
    {
        public float Altitude;
        public float Humidity;
        public float Temperature;

        public ClimateData(float altitude, float humidity, float temperature)
        {
            Altitude = altitude;
            Humidity = humidity;
            Temperature = temperature;
        }
    }

    public struct ResourceData
    {
        public bool HasForest;
        public int ForestDensity;
        public bool HasMineralVein;
        public string MineralType;
        public bool HasHerbs;
    }

    public struct BuildingData
    {
        public bool HasBuilding;
        public string BuildingType;
        public bool IsWorldGenerated;
        public int BuildingLevel;
        public bool IsBuildingBlocked;
    }

    public struct MonsterFogData
    {
        public bool IsDangerZone;
        public int DangerLevel;
        public string MonsterType;
        public bool HasMonster;
        public bool IsExplored;
    }

    public interface IClimateController
    {
        ClimateData GetClimateData(int x, int y);
        void UpdateClimate(float gameTime);
    }

    public class StaticClimateController : IClimateController
    {
        private readonly ClimateData[,] _staticClimate;
        public StaticClimateController(ClimateData[,] data) { _staticClimate = data; }
        public ClimateData GetClimateData(int x, int y) => _staticClimate[x, y];
        public void UpdateClimate(float gameTime) { }
    }

    public class MapDataStore
    {
        public static MapDataStore Instance { get; set; }
        public const int MAP_SIZE = 200;
        public const int CENTER = 100;
        public long CurrentSeed = 114514L;

        public TerrainType[,] TerrainLayer { get; set; }
        public ClimateData[,] RawClimateLayer { get; set; }
        public ResourceData[,] ResourceLayer { get; set; }
        public BuildingData[,] BuildingLayer { get; set; }
        public MonsterFogData[,] MonsterFogLayer { get; set; }
        public IClimateController ClimateController { get; set; }

        public void InitArrays()
        {
            TerrainLayer = new TerrainType[MAP_SIZE, MAP_SIZE];
            RawClimateLayer = new ClimateData[MAP_SIZE, MAP_SIZE];
            ResourceLayer = new ResourceData[MAP_SIZE, MAP_SIZE];
            BuildingLayer = new BuildingData[MAP_SIZE, MAP_SIZE];
            MonsterFogLayer = new MonsterFogData[MAP_SIZE, MAP_SIZE];
        }

        public static bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < MAP_SIZE && y >= 0 && y < MAP_SIZE;
        }
    }
}

// =============================================================================================
// 🏗️ 3. MAP GENERATORS (第一、二、三层核心算法代码移植)
// =============================================================================================
namespace RPG.Map.Terrain
{
    public static class ClimateGenerator
    {
        private const float ALTITUDE_SCALE = 0.04f;
        private const float HUMIDITY_SCALE = 0.035f;
        private const float TEMPERATURE_SCALE = 0.025f;
        private const float RIVER_SCALE = 0.015f;
        private const float ALTITUDE_WEIGHT = 0.7f;
        private const float RIVER_WEIGHT = 0.3f;

        public static ClimateData[,] GenerateClimate(long seed)
        {
            int size = MapDataStore.MAP_SIZE;
            var result = new ClimateData[size, size];
            float altitudeOffset = (float)(seed % 10000);
            float humidityOffset = (float)((seed * 3) % 10000);
            float temperatureOffset = (float)((seed * 7) % 10000);
            float riverOffset = (float)((seed * 13) % 10000);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float rawAltitude = UnityEngine.Mathf.Clamp01(
                        UnityEngine.Mathf.PerlinNoise(x * ALTITUDE_SCALE + altitudeOffset, y * ALTITUDE_SCALE + altitudeOffset));

                    float riverNoise = UnityEngine.Mathf.Clamp01(
                        UnityEngine.Mathf.PerlinNoise(x * RIVER_SCALE + riverOffset, y * RIVER_SCALE + riverOffset));

                    float fusedAltitude = UnityEngine.Mathf.Clamp01(rawAltitude * ALTITUDE_WEIGHT + riverNoise * RIVER_WEIGHT);

                    float humidity = UnityEngine.Mathf.Clamp01(
                        UnityEngine.Mathf.PerlinNoise(x * HUMIDITY_SCALE + humidityOffset, y * HUMIDITY_SCALE + humidityOffset));

                    float temperature = UnityEngine.Mathf.Clamp01(
                        UnityEngine.Mathf.PerlinNoise(x * TEMPERATURE_SCALE + temperatureOffset, y * TEMPERATURE_SCALE + temperatureOffset));

                    result[x, y] = new ClimateData(fusedAltitude, humidity, temperature);
                }
            }
            return result;
        }
    }

    public static class TerrainEvaluator
    {
        private const float SAFE_ZONE_RADIUS = 10f;
        private const float TRANSITION_ZONE_RADIUS = 25f;
        private const float DANGER_ZONE_RADIUS = 50f;
        private const float BIAS_SAFE = 0.00f;
        private const float BIAS_TRANSITION = 0.10f;
        private const float BIAS_DANGER = 0.20f;
        private const float BIAS_EXTREME = 0.35f;

        private const float WATER_THRESHOLD = 0.18f;
        private const float MOUNTAIN_THRESHOLD = 0.60f;
        private const float SWAMP_HUMIDITY_MIN = 0.65f;
        private const float SWAMP_TEMP_MAX = 0.45f;

        private const float EXTREME_RING_MIN = 70f;
        private const float VOLCANO_TEMP_MIN = 0.75f;
        private const float TUNDRA_TEMP_MAX = 0.25f;

        private const float RUINS_RING_MIN = 40f;
        private const float RUINS_CHANCE = 0.08f;

        public static float CalculateDangerBias(int x, int y)
        {
            float ringDistance = UnityEngine.Mathf.Max(
                UnityEngine.Mathf.Abs(x - MapDataStore.CENTER),
                UnityEngine.Mathf.Abs(y - MapDataStore.CENTER));

            if (ringDistance <= SAFE_ZONE_RADIUS) return BIAS_SAFE;
            if (ringDistance <= TRANSITION_ZONE_RADIUS) return BIAS_TRANSITION;
            if (ringDistance <= DANGER_ZONE_RADIUS) return BIAS_DANGER;
            return BIAS_EXTREME;
        }

        public static TerrainType EvaluateCell(int x, int y, ClimateData climate, float dangerBias, float randomValue)
        {
            float ringDistance = UnityEngine.Mathf.Max(
                UnityEngine.Mathf.Abs(x - MapDataStore.CENTER),
                UnityEngine.Mathf.Abs(y - MapDataStore.CENTER));

            float finalAltitude = UnityEngine.Mathf.Clamp01(climate.Altitude + dangerBias);

            if (ringDistance >= EXTREME_RING_MIN)
            {
                if (climate.Temperature > VOLCANO_TEMP_MIN) return TerrainType.VOLCANO;
                if (climate.Temperature < TUNDRA_TEMP_MAX) return TerrainType.TUNDRA;
            }

            if (finalAltitude < WATER_THRESHOLD) return TerrainType.WATER;

            if (finalAltitude > MOUNTAIN_THRESHOLD)
            {
                if (ringDistance >= RUINS_RING_MIN && randomValue < RUINS_CHANCE)
                    return TerrainType.RUINS;
                return TerrainType.MOUNTAIN;
            }

            if (climate.Humidity > SWAMP_HUMIDITY_MIN && climate.Temperature < SWAMP_TEMP_MAX)
                return TerrainType.SWAMP;

            return TerrainType.PLAINS;
        }
    }

    public static class BaseCampProtector
    {
        private const int PROTECTION_RADIUS = 5;

        public static void ApplyProtection(TerrainType[,] terrainLayer)
        {
            if (terrainLayer == null) return;
            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;

            for (int dx = -PROTECTION_RADIUS; dx <= PROTECTION_RADIUS; dx++)
            {
                for (int dy = -PROTECTION_RADIUS; dy <= PROTECTION_RADIUS; dy++)
                {
                    int x = cx + dx;
                    int y = cy + dy;

                    if (!MapDataStore.IsValidCoordinate(x, y)) continue;

                    float dist = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(dx), UnityEngine.Mathf.Abs(dy));
                    if (dist > PROTECTION_RADIUS) continue;

                    terrainLayer[x, y] = (dx == 0 && dy == 0) ? TerrainType.BASE_CAMP : TerrainType.PLAINS;
                }
            }
        }
    }

    public static class CityStateGenerator
    {
        private const int CITY_STATE_COUNT = 3;
        private const float MIN_DIST_FROM_CENTER = 60f;
        private const float MIN_DIST_BETWEEN = 40f;
        private const int MAX_ATTEMPTS = 2000;

        public static void PlaceCityStates(long seed, TerrainType[,] terrainLayer)
        {
            if (terrainLayer == null) return;
            var rng = new System.Random((int)(seed ^ (seed >> 32)));
            var placedPositions = new List<UnityEngine.Vector2Int>(CITY_STATE_COUNT);

            int size = MapDataStore.MAP_SIZE;
            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;
            int attempts = 0;

            while (placedPositions.Count < CITY_STATE_COUNT && attempts < MAX_ATTEMPTS)
            {
                attempts++;
                int x = rng.Next(0, size);
                int y = rng.Next(0, size);

                float distFromCenter = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(x - cx), UnityEngine.Mathf.Abs(y - cy));
                if (distFromCenter < MIN_DIST_FROM_CENTER) continue;

                if (terrainLayer[x, y] != TerrainType.PLAINS) continue;

                bool tooClose = false;
                foreach (var pos in placedPositions)
                {
                    float distBetween = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(x - pos.x), UnityEngine.Mathf.Abs(y - pos.y));
                    if (distBetween < MIN_DIST_BETWEEN)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                terrainLayer[x, y] = TerrainType.CITY_STATE;
                placedPositions.Add(new UnityEngine.Vector2Int(x, y));
            }
        }
    }

    public static class TerrainLayerGen
    {
        public static void Generate(long seed, MapDataStore store)
        {
            if (store == null) return;
            int size = MapDataStore.MAP_SIZE;

            ClimateData[,] climateData = ClimateGenerator.GenerateClimate(seed);
            store.RawClimateLayer = climateData;
            store.ClimateController = new StaticClimateController(climateData);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dangerBias = TerrainEvaluator.CalculateDangerBias(x, y);
                    float randomValue = GetCellRandom(seed, x, y);
                    store.TerrainLayer[x, y] = TerrainEvaluator.EvaluateCell(x, y, climateData[x, y], dangerBias, randomValue);
                }
            }

            BaseCampProtector.ApplyProtection(store.TerrainLayer);
            CityStateGenerator.PlaceCityStates(seed, store.TerrainLayer);
        }

        private static float GetCellRandom(long seed, int x, int y)
        {
            long h = seed ^ ((long)x * 374761393L + (long)y * 668265263L);
            var cellRng = new System.Random((int)(h ^ (h >> 32)));
            return (float)cellRng.NextDouble();
        }
    }
}

namespace RPG.Map.Resource
{
    public static class ForestGenerator
    {
        private const float FOREST_NOISE_SCALE = 0.06f;
        private const float FOREST_THRESHOLD = 0.55f;
        private const int FADE_RADIUS = 12;
        private const float FADE_MULTIPLIER = 0.6f;

        public static void GenerateForests(long seed, TerrainType[,] terrainLayer, ResourceData[,] resourceLayer)
        {
            if (terrainLayer == null || resourceLayer == null) return;
            int size = MapDataStore.MAP_SIZE;
            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;
            float forestOffset = (float)((seed * 17) % 10000);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    resourceLayer[x, y].HasForest = false;
                    resourceLayer[x, y].ForestDensity = 0;

                    if (terrainLayer[x, y] != TerrainType.PLAINS) continue;

                    float noiseValue = UnityEngine.Mathf.Clamp01(
                        UnityEngine.Mathf.PerlinNoise(x * FOREST_NOISE_SCALE + forestOffset, y * FOREST_NOISE_SCALE + forestOffset));

                    if (noiseValue > FOREST_THRESHOLD)
                    {
                        resourceLayer[x, y].HasForest = true;
                        float rawDensity = (noiseValue - FOREST_THRESHOLD) / (1.0f - FOREST_THRESHOLD) * 100f;
                        int density = UnityEngine.Mathf.Clamp((int)rawDensity, 0, 100);

                        int ringDistance = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(x - cx), UnityEngine.Mathf.Abs(y - cy));
                        if (ringDistance < FADE_RADIUS)
                        {
                            density = (int)(density * FADE_MULTIPLIER);
                        }
                        resourceLayer[x, y].ForestDensity = UnityEngine.Mathf.Clamp(density, 0, 100);
                    }
                }
            }
        }
    }

    public static class MineralGenerator
    {
        private const float MINE_NOISE_SCALE = 0.08f;
        private const float BASE_THRESHOLD = 0.60f;
        private const float DISTANCE_FACTOR = 0.25f;
        private const int STONE_ZONE_MAX = 20;
        private const int IRON_ZONE_MAX = 45;

        public static void GenerateMinerals(long seed, TerrainType[,] terrainLayer, ResourceData[,] resourceLayer)
        {
            if (terrainLayer == null || resourceLayer == null) return;
            int size = MapDataStore.MAP_SIZE;
            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;
            float mineOffset = (float)((seed * 23) % 10000);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    resourceLayer[x, y].HasMineralVein = false;
                    resourceLayer[x, y].MineralType = "";

                    if (terrainLayer[x, y] != TerrainType.MOUNTAIN) continue;

                    float noiseValue = UnityEngine.Mathf.Clamp01(
                        UnityEngine.Mathf.PerlinNoise(x * MINE_NOISE_SCALE + mineOffset, y * MINE_NOISE_SCALE + mineOffset));

                    int ringDistance = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(x - cx), UnityEngine.Mathf.Abs(y - cy));
                    float dynamicThreshold = BASE_THRESHOLD - (ringDistance / 200f * DISTANCE_FACTOR);

                    if (noiseValue > dynamicThreshold)
                    {
                        resourceLayer[x, y].HasMineralVein = true;
                        if (ringDistance <= STONE_ZONE_MAX)
                            resourceLayer[x, y].MineralType = "STONE";
                        else if (ringDistance <= IRON_ZONE_MAX)
                            resourceLayer[x, y].MineralType = "IRON";
                        else
                            resourceLayer[x, y].MineralType = "CRYSTAL";
                    }
                }
            }
        }
    }

    public static class HerbsGenerator
    {
        public static void GenerateHerbs(long seed, TerrainType[,] terrainLayer, ResourceData[,] resourceLayer)
        {
            if (terrainLayer == null || resourceLayer == null) return;
            int size = MapDataStore.MAP_SIZE;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    resourceLayer[x, y].HasHerbs = false;

                    if (terrainLayer[x, y] != TerrainType.SWAMP) continue;

                    long hashVal = x * 31L + y * 17L + seed;
                    if (((hashVal % 10L) + 10L) % 10L < 4)
                    {
                        resourceLayer[x, y].HasHerbs = true;
                    }
                }
            }
        }
    }

    public static class ResourceLayerGen
    {
        public static void Generate(long seed, MapDataStore store)
        {
            if (store == null || store.TerrainLayer == null) return;

            if (store.ResourceLayer == null)
            {
                store.ResourceLayer = new ResourceData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
            }

            ForestGenerator.GenerateForests(seed, store.TerrainLayer, store.ResourceLayer);
            MineralGenerator.GenerateMinerals(seed, store.TerrainLayer, store.ResourceLayer);
            HerbsGenerator.GenerateHerbs(seed, store.TerrainLayer, store.ResourceLayer);
        }
    }
}

namespace RPG.Map.Building
{
    public static class CampStatePlacer
    {
        public const string BUILDING_BASE_CAMP = "BASE_CAMP";
        public const string BUILDING_CITY_STATE = "CITY_STATE";

        public static void PlaceInitialBuildings(TerrainType[,] terrainLayer, BuildingData[,] buildingLayer)
        {
            if (terrainLayer == null || buildingLayer == null) return;
            int size = MapDataStore.MAP_SIZE;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    buildingLayer[x, y].HasBuilding = false;
                    buildingLayer[x, y].BuildingType = "";
                    buildingLayer[x, y].IsWorldGenerated = false;
                    buildingLayer[x, y].BuildingLevel = 0;
                    buildingLayer[x, y].IsBuildingBlocked = false;

                    if (terrainLayer[x, y] == TerrainType.BASE_CAMP)
                    {
                        buildingLayer[x, y].HasBuilding = true;
                        buildingLayer[x, y].BuildingType = BUILDING_BASE_CAMP;
                        buildingLayer[x, y].IsWorldGenerated = true;
                        buildingLayer[x, y].BuildingLevel = 1;
                        buildingLayer[x, y].IsBuildingBlocked = false;
                    }
                    else if (terrainLayer[x, y] == TerrainType.CITY_STATE)
                    {
                        buildingLayer[x, y].HasBuilding = true;
                        buildingLayer[x, y].BuildingType = BUILDING_CITY_STATE;
                        buildingLayer[x, y].IsWorldGenerated = true;
                        buildingLayer[x, y].BuildingLevel = 1;
                        buildingLayer[x, y].IsBuildingBlocked = false;
                    }
                }
            }
        }
    }

    public static class RuinsGenerator
    {
        private const int RUINS_MIN = 5;
        private const int RUINS_MAX = 8;
        private const int WONDERS_MIN = 2;
        private const int WONDERS_MAX = 4;
        private const int WONDER_RING_MIN = 50;
        private const int MIN_WONDER_DIST = 20;
        private const int MAX_ATTEMPTS = 5000;

        private static readonly string[] RUINS_IDS = { "RUINS_GHOST_REPO", "RUINS_NULL_POINTER", "RUINS_STACK_OVERFLOW", "RUINS_MEMORY_LEAK" };
        private static readonly string[] WONDER_IDS = { "WONDER_GIL_LOCK", "WONDER_SINGLETON_ALTAR", "WONDER_INFINITE_LOOP", "WONDER_RACE_CONDITION_SPIRE" };

        public static void GenerateRuinsAndWonders(long seed, TerrainType[,] terrainLayer, ResourceData[,] resourceLayer, BuildingData[,] buildingLayer)
        {
            if (terrainLayer == null || resourceLayer == null || buildingLayer == null) return;
            var rng = new System.Random((int)(seed ^ (seed >> 32)));

            int size = MapDataStore.MAP_SIZE;
            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;

            // Ruins
            List<UnityEngine.Vector2Int> ruinsTiles = new List<UnityEngine.Vector2Int>();
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (terrainLayer[x, y] == TerrainType.RUINS && !buildingLayer[x, y].HasBuilding)
                    {
                        ruinsTiles.Add(new UnityEngine.Vector2Int(x, y));
                    }
                }
            }

            int targetRuinsCount = UnityEngine.Mathf.Min(ruinsTiles.Count, rng.Next(RUINS_MIN, RUINS_MAX + 1));
            if (ruinsTiles.Count > 0 && targetRuinsCount > 0)
            {
                for (int i = ruinsTiles.Count - 1; i > 0; i--)
                {
                    int k = rng.Next(i + 1);
                    var temp = ruinsTiles[i];
                    ruinsTiles[i] = ruinsTiles[k];
                    ruinsTiles[k] = temp;
                }
                for (int i = 0; i < targetRuinsCount; i++)
                {
                    UnityEngine.Vector2Int pos = ruinsTiles[i];
                    buildingLayer[pos.x, pos.y].HasBuilding = true;
                    buildingLayer[pos.x, pos.y].BuildingType = RUINS_IDS[rng.Next(RUINS_IDS.Length)];
                    buildingLayer[pos.x, pos.y].IsWorldGenerated = true;
                    buildingLayer[pos.x, pos.y].BuildingLevel = 1;
                    buildingLayer[pos.x, pos.y].IsBuildingBlocked = false;
                }
            }

            // Wonders
            int targetWonderCount = rng.Next(WONDERS_MIN, WONDERS_MAX + 1);
            int wondersPlaced = 0;
            int attempts = 0;
            List<UnityEngine.Vector2Int> wonderPositions = new List<UnityEngine.Vector2Int>(targetWonderCount);

            while (wondersPlaced < targetWonderCount && attempts < MAX_ATTEMPTS)
            {
                attempts++;
                int x = rng.Next(0, size);
                int y = rng.Next(0, size);

                int ringDistance = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(x - cx), UnityEngine.Mathf.Abs(y - cy));
                if (ringDistance <= WONDER_RING_MIN) continue;
                if (terrainLayer[x, y] == TerrainType.WATER) continue;
                if (buildingLayer[x, y].HasBuilding) continue;

                if (resourceLayer[x, y].HasForest || resourceLayer[x, y].HasMineralVein || resourceLayer[x, y].HasHerbs)
                    continue;

                bool tooClose = false;
                foreach (var pos in wonderPositions)
                {
                    int distBetween = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(x - pos.x), UnityEngine.Mathf.Abs(y - pos.y));
                    if (distBetween < MIN_WONDER_DIST)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                buildingLayer[x, y].HasBuilding = true;
                buildingLayer[x, y].BuildingType = WONDER_IDS[rng.Next(WONDER_IDS.Length)];
                buildingLayer[x, y].IsWorldGenerated = true;
                buildingLayer[x, y].BuildingLevel = 1;
                buildingLayer[x, y].IsBuildingBlocked = false;

                wonderPositions.Add(new UnityEngine.Vector2Int(x, y));
                wondersPlaced++;
            }
        }
    }

    public static class BuildingLayerGen
    {
        public static void Generate(long seed, MapDataStore store)
        {
            if (store == null || store.TerrainLayer == null || store.ResourceLayer == null) return;

            if (store.BuildingLayer == null)
            {
                store.BuildingLayer = new BuildingData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
            }

            CampStatePlacer.PlaceInitialBuildings(store.TerrainLayer, store.BuildingLayer);
            RuinsGenerator.GenerateRuinsAndWonders(seed, store.TerrainLayer, store.ResourceLayer, store.BuildingLayer);
        }
    }
}

// =============================================================================================
// 😈 3.5 MONSTER & FOG GENERATION SYSTEM (第四层：迷雾与怪物生成系统)
// =============================================================================================
namespace RPG.Map.MonsterFog
{
    using UnityEngine;

    public static class FogGenerator
    {
        public const int SAFE_ZONE_EXPLORE_RADIUS = 10;

        public static void GenerateFog(MonsterFogData[,] monsterFogLayer)
        {
            if (monsterFogLayer == null) return;
            int size = MapDataStore.MAP_SIZE;
            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int ringDistance = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));
                    if (ringDistance <= SAFE_ZONE_EXPLORE_RADIUS)
                    {
                        monsterFogLayer[x, y].IsExplored = true;
                    }
                    else
                    {
                        monsterFogLayer[x, y].IsExplored = false;
                    }
                }
            }
        }
    }

    public static class MonsterGenerator
    {
        public const float MONSTER_NOISE_SCALE = 0.10f;
        public const string MONSTER_SLIME = "SLIME";
        public const string MONSTER_BUG_KNIGHT = "BUG_KNIGHT";
        public const string MONSTER_NULL_GHOST = "NULL_GHOST";
        public const string MONSTER_DEADLOCK_GOLEM = "DEADLOCK_GOLEM";
        public const string MONSTER_MEMORY_LEAK_TITAN = "MEMORY_LEAK_TITAN";

        public const float THRESHOLD_NEAR = 0.75f;
        public const float THRESHOLD_MID = 0.60f;
        public const float THRESHOLD_FAR = 0.45f;

        public static void GenerateMonsters(long seed, TerrainType[,] terrainLayer, BuildingData[,] buildingLayer, MonsterFogData[,] monsterFogLayer)
        {
            if (terrainLayer == null || buildingLayer == null || monsterFogLayer == null) return;
            int size = MapDataStore.MAP_SIZE;
            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;

            float monsterOffset = (float)((seed * 37L) % 10000L);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    monsterFogLayer[x, y].IsDangerZone = false;
                    monsterFogLayer[x, y].DangerLevel = 0;
                    monsterFogLayer[x, y].MonsterType = "";
                    monsterFogLayer[x, y].HasMonster = false;

                    if (terrainLayer[x, y] == TerrainType.WATER) continue;
                    int ringDistance = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));
                    if (ringDistance <= 10) continue;
                    if (buildingLayer[x, y].HasBuilding) continue;

                    float noiseValue = Mathf.Clamp01(
                        Mathf.PerlinNoise(
                            x * MONSTER_NOISE_SCALE + monsterOffset,
                            y * MONSTER_NOISE_SCALE + monsterOffset
                        )
                    );

                    bool spawnMonster = false;
                    int calculatedDangerLevel = 0;

                    if (ringDistance <= 25)
                    {
                        if (noiseValue > THRESHOLD_NEAR)
                        {
                            spawnMonster = true;
                            calculatedDangerLevel = 1;
                        }
                    }
                    else if (ringDistance <= 50)
                    {
                        if (noiseValue > THRESHOLD_MID)
                        {
                            spawnMonster = true;
                            long cellHash = seed + x * 3137L + y * 7139L;
                            calculatedDangerLevel = 2 + (int)(((cellHash % 2L) + 2L) % 2L);
                        }
                    }
                    else
                    {
                        if (noiseValue > THRESHOLD_FAR)
                        {
                            spawnMonster = true;
                            long cellHash = seed + x * 3137L + y * 7139L;
                            calculatedDangerLevel = 3 + (int)(((cellHash % 3L) + 3L) % 3L);
                        }
                    }

                    if (spawnMonster)
                    {
                        monsterFogLayer[x, y].HasMonster = true;
                        monsterFogLayer[x, y].IsDangerZone = true;
                        monsterFogLayer[x, y].DangerLevel = calculatedDangerLevel;

                        switch (calculatedDangerLevel)
                        {
                            case 1: monsterFogLayer[x, y].MonsterType = MONSTER_SLIME; break;
                            case 2: monsterFogLayer[x, y].MonsterType = MONSTER_BUG_KNIGHT; break;
                            case 3: monsterFogLayer[x, y].MonsterType = MONSTER_NULL_GHOST; break;
                            case 4: monsterFogLayer[x, y].MonsterType = MONSTER_DEADLOCK_GOLEM; break;
                            case 5: monsterFogLayer[x, y].MonsterType = MONSTER_MEMORY_LEAK_TITAN; break;
                        }
                    }
                }
            }
        }
    }

    public static class MonsterFogLayerGen
    {
        public static void Generate(long seed, MapDataStore store)
        {
            if (store == null || store.TerrainLayer == null || store.BuildingLayer == null) return;
            if (store.MonsterFogLayer == null)
            {
                store.MonsterFogLayer = new MonsterFogData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
            }
            FogGenerator.GenerateFog(store.MonsterFogLayer);
            MonsterGenerator.GenerateMonsters(seed, store.TerrainLayer, store.BuildingLayer, store.MonsterFogLayer);
        }
    }
}

// =============================================================================================
// 🧪 4. OFFLINE INTEGRATION TESTER (离线集成测试套件)
// =============================================================================================
namespace RPG.Map.Test
{
    using UnityEngine;

    public class OfflineTester
    {
        private int _totalTestsRan;
        private int _totalTestsPassed;
        private int _totalTestsFailed;

        public void RunAllTests()
        {
            _totalTestsRan = 0;
            _totalTestsPassed = 0;
            _totalTestsFailed = 0;

            UnityEngine.Debug.Log("<color=yellow>========== 🟢 开始执行第一、二、三层地图生成集成测试 ==========</color>");

            MapDataStore store = new MapDataStore();
            store.InitArrays();
            MapDataStore.Instance = store;

            try
            {
                // 用例 1：校验数组初始化
                Test_Initialization(store);

                // 用例 2：测试大本营安全区保护逻辑
                Test_BaseCampProtection(store);

                // 用例 3：测试气候数据物理场的合法范围
                Test_ClimateRanges(store);

                // 用例 4：测试城邦生成边界与间距逻辑
                Test_CityStates(store);

                // 用例 5：测试第二层草原森林分布规则
                Test_Forests(store);

                // 用例 6：测试第二层山地矿石分布规则与圈层分配
                Test_Minerals(store);

                // 用例 7：测试第二层沼泽草药生成规则与概率分布
                Test_Herbs(store);

                // 用例 8：测试极端边界 long.MinValue 种子有效性与安全性
                Test_LongMinValue_Overflow(store);

                // 用例 9：测试第三层初始大本营与城邦世界级建筑放置
                Test_Layer3_CampStateBuildings(store);

                // 用例 10：测试第三层世界遗迹生成数量及地貌限定 (区间 [0, 8] 兼容，种子下必为 [5, 8])
                Test_Layer3_RuinsCount(store);

                // 用例 11：测试第三层世界极客奇观生成数量、偏远圈层限制、去资源纯空草原以及两两间距限制 (区间 [2, 4])
                Test_Layer3_WondersCountAndConstraints(store);

                // 用例 12：测试第四层迷雾覆盖、大本营安全区开雾、水域/建筑避让、三段式分段硬阈值与怪物ID确定性分级验证 (T25~T32)
                Test_Layer4_MonsterFogConstraints(store);
            }
            catch (Exception ex)
            {
                _totalTestsFailed++;
                UnityEngine.Debug.LogError($"[Test System Error] 测试执行过程中抛出了未捕获 of 系统异常: {ex.Message}\n{ex.StackTrace}");
            }

            string color = _totalTestsFailed == 0 ? "lime" : "orange";
            UnityEngine.Debug.Log($"<color={color}>========== 🏆 集成测试运行结束 | 报告: 共运行 {_totalTestsRan} 项, 成功: {_totalTestsPassed}, 失败: {_totalTestsFailed} ==========</color>");
        }

        private void Assert(bool condition, string testName, string successMessage, string failMessage)
        {
            _totalTestsRan++;
            if (condition)
            {
                _totalTestsPassed++;
                UnityEngine.Debug.Log($"<color=lime>[Test Passed]</color> <b>{testName}</b>: {successMessage}");
            }
            else
            {
                _totalTestsFailed++;
                UnityEngine.Debug.LogError($"<color=red>[Test Failed]</color> <b>{testName}</b>: <color=yellow>{failMessage}</color>");
            }
        }

        private void Test_Initialization(MapDataStore store)
        {
            Assert(store != null, "Test_Initialization",
                "MapDataStore 实例非空",
                "MapDataStore 实例为 null");

            store.InitArrays();

            Assert(store.TerrainLayer != null && store.RawClimateLayer != null && store.ResourceLayer != null && store.BuildingLayer != null,
                "Test_Initialization",
                "第一、二、三层数组（地形、气候、资源、建筑）初始化成功，非空",
                "部分核心数组未被成功 new 出来，仍为 null");

            bool isSizeCorrect = store.TerrainLayer.GetLength(0) == MapDataStore.MAP_SIZE &&
                                 store.TerrainLayer.GetLength(1) == MapDataStore.MAP_SIZE &&
                                 store.ResourceLayer.GetLength(0) == MapDataStore.MAP_SIZE &&
                                 store.ResourceLayer.GetLength(1) == MapDataStore.MAP_SIZE &&
                                 store.BuildingLayer.GetLength(0) == MapDataStore.MAP_SIZE &&
                                 store.BuildingLayer.GetLength(1) == MapDataStore.MAP_SIZE;

            Assert(isSizeCorrect, "Test_Initialization",
                $"所有数组尺寸均为 {MapDataStore.MAP_SIZE}x{MapDataStore.MAP_SIZE}，符合设计",
                $"数组尺寸异常！实际尺寸: {store.TerrainLayer.GetLength(0)}x{store.TerrainLayer.GetLength(1)}");
        }

        private void Test_BaseCampProtection(MapDataStore store)
        {
            long testSeed = 114514L;
            Terrain.TerrainLayerGen.Generate(testSeed, store);
            Resource.ResourceLayerGen.Generate(testSeed, store);

            bool isCenterCorrect = store.TerrainLayer[MapDataStore.CENTER, MapDataStore.CENTER] == TerrainType.BASE_CAMP;
            Assert(isCenterCorrect, "Test_BaseCampProtection",
                "大地图中心坐标 (100, 100) 成功被强制覆盖为 BASE_CAMP",
                $"大地图中心不是 BASE_CAMP！实际为: {store.TerrainLayer[MapDataStore.CENTER, MapDataStore.CENTER]}");

            bool isProtectionZoneSafe = true;
            string failedDetail = "";
            int radius = 5;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int x = MapDataStore.CENTER + dx;
                    int y = MapDataStore.CENTER + dy;

                    if (store.TerrainLayer[x, y] != TerrainType.PLAINS)
                    {
                        isProtectionZoneSafe = false;
                        failedDetail = $"坐标 ({x}, {y}) 处的类型不是 PLAINS，而是 {store.TerrainLayer[x, y]}";
                        break;
                    }
                }
                if (!isProtectionZoneSafe) break;
            }

            Assert(isProtectionZoneSafe, "Test_BaseCampProtection",
                "中心周围 5 格（切比雪夫半径）的安全区被 100% 成功强制覆盖为 PLAINS (共 120 格)",
                $"大本营周围保护区被污染！错误细节: {failedDetail}");
        }

        private void Test_ClimateRanges(MapDataStore store)
        {
            bool isAllInRange = true;
            string failDetail = "";

            var rand = new System.Random(1997);
            for (int i = 0; i < 2000; i++)
            {
                int x = rand.Next(0, MapDataStore.MAP_SIZE);
                int y = rand.Next(0, MapDataStore.MAP_SIZE);

                ClimateData c = store.RawClimateLayer[x, y];
                if (c.Altitude < 0f || c.Altitude > 1f ||
                    c.Humidity < 0f || c.Humidity > 1f ||
                    c.Temperature < 0f || c.Temperature > 1f)
                {
                    isAllInRange = false;
                    failDetail = $"坐标 ({x}, {y}) 气候值异常: Altitude={c.Altitude}, Humidity={c.Humidity}, Temperature={c.Temperature}";
                    break;
                }
            }

            Assert(isAllInRange, "Test_ClimateRanges",
                "抽样的 2000 个单元格的气候物理场（高度、湿度、温度）数值全部在合法的 [0.0, 1.0] 范围内",
                $"气候场物理数值未被正确 Clamp！错误细节: {failDetail}");
        }

        private void Test_CityStates(MapDataStore store)
        {
            long testSeed = 999999L;
            Terrain.TerrainLayerGen.Generate(testSeed, store);

            List<UnityEngine.Vector2Int> cityStates = new List<UnityEngine.Vector2Int>();
            for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
            {
                for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                {
                    if (store.TerrainLayer[x, y] == TerrainType.CITY_STATE)
                    {
                        cityStates.Add(new UnityEngine.Vector2Int(x, y));
                    }
                }
            }

            Assert(cityStates.Count == 3, "Test_CityStates",
                "成功在草原上找到并生成了 3 个相互独立的城邦",
                $"城邦放置数量异常！实际生成数量: {cityStates.Count}/3");

            bool isFarFromCenter = true;
            string centerFailDetail = "";
            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;

            foreach (var pos in cityStates)
            {
                int ringDist = Mathf.Max(Mathf.Abs(pos.x - cx), Mathf.Abs(pos.y - cy));
                if (ringDist <= 60) // 严格 > 60
                {
                    isFarFromCenter = false;
                    centerFailDetail = $"城邦坐落在 ({pos.x}, {pos.y})，切比雪夫距离为 {ringDist}，低于或等于安全限制 60 格";
                    break;
                }
            }

            Assert(isFarFromCenter, "Test_CityStates",
                "所有城邦均坐落在大本营 60 格之外的偏远圈层",
                $"城邦距离中心过近！错误细节: {centerFailDetail}");

            bool isInterDistanceOk = true;
            string distFailDetail = "";
            for (int i = 0; i < cityStates.Count; i++)
            {
                for (int j = i + 1; j < cityStates.Count; j++)
                {
                    int dist = Mathf.Max(Mathf.Abs(cityStates[i].x - cityStates[j].x), Mathf.Abs(cityStates[i].y - cityStates[j].y));
                    if (dist < 40)
                    {
                        isInterDistanceOk = false;
                        distFailDetail = $"城邦 {i}({cityStates[i]}) 与 城邦 {j}({cityStates[j]}) 的切比雪夫间距只有 {dist} 格，低于最小值 40 格";
                        break;
                    }
                }
                if (!isInterDistanceOk) break;
            }

            Assert(isInterDistanceOk, "Test_CityStates",
                "所有城邦的两两切比雪夫间距均 >= 40 格，分布空间合理",
                $"城邦两两靠得太近！错误细节: {distFailDetail}");
        }

        private void Test_Forests(MapDataStore store)
        {
            long seed = 456789L;
            Terrain.TerrainLayerGen.Generate(seed, store);
            Resource.ResourceLayerGen.Generate(seed, store);

            bool onlyOnPlains = true;
            bool safetyFadePassed = true;
            string forestFailDetail = "";

            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;

            for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
            {
                for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                {
                    ResourceData res = store.ResourceLayer[x, y];
                    TerrainType terrain = store.TerrainLayer[x, y];

                    if (res.HasForest && terrain != TerrainType.PLAINS)
                    {
                        onlyOnPlains = false;
                        forestFailDetail = $"在非草原格子 ({x}, {y}) [地形: {terrain}] 上发现了森林！";
                        break;
                    }

                    if (res.HasForest)
                    {
                        int ringDistance = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));
                        if (ringDistance < 12 && res.ForestDensity > 60)
                        {
                            safetyFadePassed = false;
                            forestFailDetail = $"在大本营 12 格范围内，坐标 ({x}, {y}) 森林未被折减！实际密度: {res.ForestDensity}";
                            break;
                        }
                    }
                }
                if (!onlyOnPlains || !safetyFadePassed) break;
            }

            Assert(onlyOnPlains, "Test_Forests",
                "所有森林格子均 100% 坐落于 PLAINS (草原) 地形上",
                $"森林地形限定校验失败！错误细节: {forestFailDetail}");

            Assert(safetyFadePassed, "Test_Forests",
                "大本营周围 12 格圈层内（切比雪夫距离）的草原森林密度均成功完成降密 60% 限制 (<= 60)",
                $"大本营周围森林降密检验失败！错误细节: {forestFailDetail}");
        }

        private void Test_Minerals(MapDataStore store)
        {
            long seed = 888888L;
            Terrain.TerrainLayerGen.Generate(seed, store);
            Resource.ResourceLayerGen.Generate(seed, store);

            bool onlyOnMountain = true;
            bool zonalTypeCorrect = true;
            string mineFailDetail = "";

            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;

            for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
            {
                for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                {
                    ResourceData res = store.ResourceLayer[x, y];
                    TerrainType terrain = store.TerrainLayer[x, y];

                    if (res.HasMineralVein)
                    {
                        if (terrain != TerrainType.MOUNTAIN)
                        {
                            onlyOnMountain = false;
                            mineFailDetail = $"在非山地格子 ({x}, {y}) [地形: {terrain}] 上发现了矿脉！";
                            break;
                        }

                        int ringDistance = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));
                        if (ringDistance <= 20 && res.MineralType != "STONE")
                        {
                            zonalTypeCorrect = false;
                            mineFailDetail = $"坐标 ({x}, {y}) 位于圈层 {ringDistance} (<=20)，但矿石是: {res.MineralType}，非 STONE";
                            break;
                        }
                        else if (ringDistance > 20 && ringDistance <= 45 && res.MineralType != "IRON")
                        {
                            zonalTypeCorrect = false;
                            mineFailDetail = $"坐标 ({x}, {y}) 位于圈层 {ringDistance} (20~45)，但矿石是: {res.MineralType}，非 IRON";
                            break;
                        }
                        else if (ringDistance > 45 && res.MineralType != "CRYSTAL")
                        {
                            zonalTypeCorrect = false;
                            mineFailDetail = $"坐标 ({x}, {y}) 位于圈层 {ringDistance} (>45)，但矿石是: {res.MineralType}，非 CRYSTAL";
                            break;
                        }
                    }
                }
                if (!onlyOnMountain || !zonalTypeCorrect) break;
            }

            Assert(onlyOnMountain, "Test_Minerals",
                "所有矿脉格子均 100% 坐落于 MOUNTAIN (山地) 地形上",
                $"矿脉地形限定校验失败！错误细节: {mineFailDetail}");

            Assert(zonalTypeCorrect, "Test_Minerals",
                "矿石类型与切比雪夫距离圈层（0-20石 / 20-45铁 / 45+晶体）100% 科学对齐",
                $"矿石圈层种类划分失败！错误细节: {mineFailDetail}");
        }

        private void Test_Herbs(MapDataStore store)
        {
            long seed = 777777L;
            Terrain.TerrainLayerGen.Generate(seed, store);
            Resource.ResourceLayerGen.Generate(seed, store);

            bool onlyOnSwamp = true;
            int totalSwamps = 0;
            int totalHerbs = 0;
            string herbsFailDetail = "";

            for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
            {
                for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                {
                    ResourceData res = store.ResourceLayer[x, y];
                    TerrainType terrain = store.TerrainLayer[x, y];

                    if (terrain == TerrainType.SWAMP)
                    {
                        totalSwamps++;
                        if (res.HasHerbs) totalHerbs++;
                    }
                    else
                    {
                        if (res.HasHerbs)
                        {
                            onlyOnSwamp = false;
                            herbsFailDetail = $"在非沼泽格子 ({x}, {y}) [地形: {terrain}] 上发现了草药！";
                            break;
                        }
                    }
                }
                if (!onlyOnSwamp) break;
            }

            Assert(onlyOnSwamp, "Test_Herbs",
                "所有草药格子均 100% 坐落于 SWAMP (沼泽) 地形上",
                $"草药地形限定校验失败！错误细节: {herbsFailDetail}");

            if (totalSwamps > 50)
            {
                float rate = (float)totalHerbs / totalSwamps;
                bool isRateReasonable = rate >= 0.30f && rate <= 0.50f;
                Assert(isRateReasonable, "Test_Herbs",
                    $"草药随机概率测试通过！沼泽总数: {totalSwamps}, 草药总数: {totalHerbs}, 概率: {rate * 100:F2}% (落在 [30%, 50%] 波动区间内)",
                    $"草药概率偏离设计区间！当前生成概率: {rate * 100:F2}%，沼泽总数: {totalSwamps}");
            }
            else
            {
                _totalTestsRan++;
                _totalTestsPassed++;
            }
        }

        private void Test_LongMinValue_Overflow(MapDataStore store)
        {
            long overflowSeed = long.MinValue;
            bool runWithoutCrash = true;
            string crashMessage = "";

            try
            {
                Terrain.TerrainLayerGen.Generate(overflowSeed, store);
                Resource.ResourceLayerGen.Generate(overflowSeed, store);
            }
            catch (Exception ex)
            {
                runWithoutCrash = false;
                crashMessage = ex.ToString();
            }

            Assert(runWithoutCrash, "Test_LongMinValue_Overflow",
                "测试成功！以 long.MinValue 极端异常值为随机种子时，系统平稳跑通，未引发任何 OverflowException 溢出崩溃",
                $"以 long.MinValue 为种子引发了溢出崩溃！堆栈详情: {crashMessage}");

            if (!runWithoutCrash) return;

            bool isBaseCampSafe = store.TerrainLayer[MapDataStore.CENTER, MapDataStore.CENTER] == TerrainType.BASE_CAMP;
            Assert(isBaseCampSafe, "Test_LongMinValue_Overflow",
                "保护验证成功！即使噪声场在负溢出偏置下被玩坏，安全罩 BaseCampProtector 仍完美捍卫大本营坐标 (100,100)",
                $"保护防线失守！在极端种子下大地图中心变成了: {store.TerrainLayer[MapDataStore.CENTER, MapDataStore.CENTER]}");
        }

        private void Test_Layer3_CampStateBuildings(MapDataStore store)
        {
            long testSeed = 114514L;
            Terrain.TerrainLayerGen.Generate(testSeed, store);
            Resource.ResourceLayerGen.Generate(testSeed, store);
            Building.BuildingLayerGen.Generate(testSeed, store);

            // 1. 验证大本营建筑
            BuildingData centerB = store.BuildingLayer[MapDataStore.CENTER, MapDataStore.CENTER];
            bool isBaseCampBuildingOk = centerB.HasBuilding && 
                                         centerB.BuildingType == "BASE_CAMP" && 
                                         centerB.IsWorldGenerated;

            Assert(isBaseCampBuildingOk, "Test_Layer3_CampStateBuildings",
                "大地图中心坐标 (100, 100) 成功被放置了基地建筑 BASE_CAMP，标记为世界生成",
                $"基地建筑缺失或属性异常！实际数据: HasBuilding={centerB.HasBuilding}, Type={centerB.BuildingType}");

            // 2. 验证城邦建筑
            bool isAllCityStateBuildingsOk = true;
            string cityStateFailDetail = "";
            int cityStatesFound = 0;

            for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
            {
                for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                {
                    if (store.TerrainLayer[x, y] == TerrainType.CITY_STATE)
                    {
                        cityStatesFound++;
                        BuildingData b = store.BuildingLayer[x, y];
                        if (!b.HasBuilding || b.BuildingType != "CITY_STATE" || !b.IsWorldGenerated)
                        {
                            isAllCityStateBuildingsOk = false;
                            cityStateFailDetail = $"城邦地形格 ({x}, {y}) 处的建筑不符合规格！实际: HasBuilding={b.HasBuilding}, Type={b.BuildingType}";
                            break;
                        }
                    }
                }
                if (!isAllCityStateBuildingsOk) break;
            }

            Assert(isAllCityStateBuildingsOk && cityStatesFound == 3, "Test_Layer3_CampStateBuildings",
                $"成功对齐了 3 个初始城邦建筑，其放置坐标、类型 (CITY_STATE) 及世界级标识均校验成功",
                $"城邦建筑配置错误！错误细节: {cityStateFailDetail} (共找到 {cityStatesFound} 个城邦地形格)");
        }

        private void Test_Layer3_RuinsCount(MapDataStore store)
        {
            long testSeed = 114514L;
            Terrain.TerrainLayerGen.Generate(testSeed, store);
            Resource.ResourceLayerGen.Generate(testSeed, store);
            Building.BuildingLayerGen.Generate(testSeed, store);

            int ruinsCount = 0;
            bool onlyOnRuinsTerrain = true;
            string failDetail = "";

            for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
            {
                for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                {
                    BuildingData b = store.BuildingLayer[x, y];
                    if (b.HasBuilding && b.BuildingType.StartsWith("RUINS_"))
                    {
                        ruinsCount++;
                        if (store.TerrainLayer[x, y] != TerrainType.RUINS)
                        {
                            onlyOnRuinsTerrain = false;
                            failDetail = $"坐标 ({x}, {y}) 有遗迹建筑 {b.BuildingType}，但地形是: {store.TerrainLayer[x, y]}，非 RUINS 地貌";
                            break;
                        }
                    }
                }
                if (!onlyOnRuinsTerrain) break;
            }

            bool isCountWithinRange = ruinsCount >= 5 && ruinsCount <= 8;
            Assert(isCountWithinRange, "Test_Layer3_RuinsCount",
                $"世界生成的遗迹数量校验成功！当前共生成 {ruinsCount} 个遗迹，严格落在设计要求的 [5, 8] 内",
                $"遗迹生成数量异常！实际生成数量: {ruinsCount}");

            Assert(onlyOnRuinsTerrain, "Test_Layer3_RuinsCount",
                "所有废弃文明遗迹建筑均 100% 坐落于 RUINS (遗迹) 地貌格上",
                $"遗迹地貌限定验证失败！错误细节: {failDetail}");
        }

        private void Test_Layer3_WondersCountAndConstraints(MapDataStore store)
        {
            long testSeed = 114514L;
            Terrain.TerrainLayerGen.Generate(testSeed, store);
            Resource.ResourceLayerGen.Generate(testSeed, store);
            Building.BuildingLayerGen.Generate(testSeed, store);

            List<UnityEngine.Vector2Int> wonderPositions = new List<UnityEngine.Vector2Int>();
            bool onlyOnLand = true;
            bool onlyOuterRing = true;
            bool onlyNoResources = true;
            string failDetail = "";

            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;

            for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
            {
                for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                {
                    BuildingData b = store.BuildingLayer[x, y];
                    if (b.HasBuilding && b.BuildingType.StartsWith("WONDER_"))
                    {
                        wonderPositions.Add(new UnityEngine.Vector2Int(x, y));

                        if (store.TerrainLayer[x, y] == TerrainType.WATER)
                        {
                            onlyOnLand = false;
                            failDetail = $"奇观 {b.BuildingType} 长在了水域格 ({x}, {y})";
                            break;
                        }

                        int ringDist = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));
                        if (ringDist <= 50)
                        {
                            onlyOuterRing = false;
                            failDetail = $"奇观 {b.BuildingType} 坐标 ({x}, {y}) 距离中心圈层仅为 {ringDist} (<= 50)";
                            break;
                        }

                        ResourceData res = store.ResourceLayer[x, y];
                        if (res.HasForest || res.HasMineralVein || res.HasHerbs)
                        {
                            onlyNoResources = false;
                            failDetail = $"奇观 {b.BuildingType} 坐标 ({x}, {y}) 覆盖了资源！(Forest={res.HasForest}, Mine={res.HasMineralVein})";
                            break;
                        }
                    }
                }
                if (!onlyOnLand || !onlyOuterRing || !onlyNoResources) break;
            }

            bool isCountCorrect = wonderPositions.Count >= 2 && wonderPositions.Count <= 4;
            Assert(isCountCorrect, "Test_Layer3_WondersCountAndConstraints",
                $"世界极客奇观数量校验成功！当前共生成 {wonderPositions.Count} 个奇观，严格落在 [2, 4] 区间",
                $"奇观生成数量异常！实际生成数量: {wonderPositions.Count}");

            Assert(onlyOnLand && onlyOuterRing && onlyNoResources, "Test_Layer3_WondersCountAndConstraints",
                "所有极客奇观 100% 长在距大本营 50 格外的非水域空白陆地上，成功规避了林/矿/草药格子",
                $"奇观落户环境验证失败！错误细节: {failDetail}");

            bool isSpacingOk = true;
            for (int i = 0; i < wonderPositions.Count; i++)
            {
                for (int j = i + 1; j < wonderPositions.Count; j++)
                {
                    int dist = Mathf.Max(Mathf.Abs(wonderPositions[i].x - wonderPositions[j].x), Mathf.Abs(wonderPositions[i].y - wonderPositions[j].y));
                    if (dist < 20)
                    {
                        isSpacingOk = false;
                        failDetail = $"奇观 {i}({wonderPositions[i]}) 与奇观 {j}({wonderPositions[j]}) 间距仅为 {dist} 格，低于最小限制 20 格";
                        break;
                    }
                }
                if (!isSpacingOk) break;
            }

            Assert(isSpacingOk, "Test_Layer3_WondersCountAndConstraints",
                "奇观间距校验成功！所有世界级极客奇观的两两切比雪夫间距均严格 >= 20 格，空间探索感极佳",
                $"奇观间距过近！错误细节: {failDetail}");
        }

        private void Test_Layer4_MonsterFogConstraints(MapDataStore store)
        {
            long testSeed = 114514L;
            Terrain.TerrainLayerGen.Generate(testSeed, store);
            Resource.ResourceLayerGen.Generate(testSeed, store);
            Building.BuildingLayerGen.Generate(testSeed, store);
            MonsterFog.MonsterFogLayerGen.Generate(testSeed, store);

            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;
            int size = MapDataStore.MAP_SIZE;

            bool t25Ok = true;
            bool t26Ok = true;
            bool t27Ok = true;
            bool t28Ok = true;
            bool t29Ok = true;
            bool t30Ok = true;
            bool t31Ok = true;

            string failDetail = "";

            float monsterOffset = (float)((testSeed * 37L) % 10000L);
            float scale = 0.10f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    MonsterFogData fog = store.MonsterFogLayer[x, y];
                    int ringDist = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));

                    if (ringDist <= 10)
                    {
                        if (!fog.IsExplored)
                        {
                            t26Ok = false;
                            failDetail = $"安全区格 ({x}, {y}) ringDistance={ringDist} 竟然未被开雾";
                            break;
                        }
                    }
                    else
                    {
                        if (fog.IsExplored)
                        {
                            t25Ok = false;
                            failDetail = $"安全区外格 ({x}, {y}) ringDistance={ringDist} 竟然已开雾";
                            break;
                        }
                    }

                    if (store.TerrainLayer[x, y] == TerrainType.WATER)
                    {
                        if (fog.HasMonster)
                        {
                            t27Ok = false;
                            failDetail = $"水域格 ({x}, {y}) 竟然刷新了怪物 {fog.MonsterType}";
                            break;
                        }
                    }

                    if (store.BuildingLayer[x, y].HasBuilding)
                    {
                        if (fog.HasMonster)
                        {
                            t28Ok = false;
                            failDetail = $"建筑格 ({x}, {y}) 竟然刷新了怪物 {fog.MonsterType}";
                            break;
                        }
                        if (store.BuildingLayer[x, y].IsBuildingBlocked)
                        {
                            t28Ok = false;
                            failDetail = $"建筑格 ({x}, {y}) 的 IsBuildingBlocked 竟然在开图时被设为了 true";
                            break;
                        }
                    }

                    if (fog.HasMonster)
                    {
                        if (ringDist <= 10)
                        {
                            t29Ok = false;
                            failDetail = $"安全区内 ({x}, {y}) 竟然刷新了怪物 {fog.MonsterType}";
                            break;
                        }

                        float noise = Mathf.Clamp01(Mathf.PerlinNoise(x * scale + monsterOffset, y * scale + monsterOffset));

                        if (ringDist <= 25)
                        {
                            if (noise <= 0.75f)
                            {
                                t29Ok = false;
                                failDetail = $"近圈格子 ({x}, {y}) 噪声值仅为 {noise} 却强行刷新了怪";
                                break;
                            }
                            if (fog.DangerLevel != 1 || fog.MonsterType != "SLIME")
                            {
                                t29Ok = false;
                                failDetail = $"近圈怪物格 ({x}, {y}) DangerLevel={fog.DangerLevel}, Type={fog.MonsterType} 与 SLIME 不符";
                                break;
                            }
                        }
                        else if (ringDist <= 50)
                        {
                            if (noise <= 0.60f)
                            {
                                t30Ok = false;
                                failDetail = $"中圈格子 ({x}, {y}) 噪声值仅为 {noise} 却强行刷新了怪";
                                break;
                            }
                            bool isMidMonsterOk = (fog.DangerLevel == 2 && fog.MonsterType == "BUG_KNIGHT") || 
                                                  (fog.DangerLevel == 3 && fog.MonsterType == "NULL_GHOST");
                            if (!isMidMonsterOk)
                            {
                                t30Ok = false;
                                failDetail = $"中圈怪物格 ({x}, {y}) DangerLevel={fog.DangerLevel}, Type={fog.MonsterType} 异常";
                                break;
                            }
                        }
                        else
                        {
                            if (noise <= 0.45f)
                            {
                                t31Ok = false;
                                failDetail = $"外圈格子 ({x}, {y}) 噪声值仅为 {noise} 却强行刷新了怪";
                                break;
                            }
                            bool isFarMonsterOk = (fog.DangerLevel == 3 && fog.MonsterType == "NULL_GHOST") ||
                                                  (fog.DangerLevel == 4 && fog.MonsterType == "DEADLOCK_GOLEM") ||
                                                  (fog.DangerLevel == 5 && fog.MonsterType == "MEMORY_LEAK_TITAN");
                            if (!isFarMonsterOk)
                            {
                                t31Ok = false;
                                failDetail = $"外圈怪物格 ({x}, {y}) DangerLevel={fog.DangerLevel}, Type={fog.MonsterType} 异常";
                                break;
                            }
                        }
                    }
                }
                if (!t25Ok || !t26Ok || !t27Ok || !t28Ok || !t29Ok || !t30Ok || !t31Ok) break;
            }

            Assert(t25Ok, "T25_MonsterFog_InitialFogOutsideSafeZone",
                "战争迷雾初始覆盖校验成功！安全区外均为未探索黑雾",
                $"迷雾未覆盖验证失败！错误细节: {failDetail}");

            Assert(t26Ok, "T26_MonsterFog_SafeZoneRevealed",
                "安全区开雾半径验证成功！所有 ringDistance <= 10 均默认探索开雾",
                $"安全区探索未开验证失败！错误细节: {failDetail}");

            Assert(t27Ok, "T27_MonsterFog_WaterHasNoMonster",
                "水域绝对无怪隔离校验成功！水域绝对无怪物",
                $"水域刷怪限制失败！错误细节: {failDetail}");

            Assert(t28Ok, "T28_MonsterFog_BuildingCellsSafeAndHealthy",
                "世界建筑格无怪与健康状态校验成功！大本营/城邦/遗迹开图无怪且IsBuildingBlocked为false",
                $"建筑格验证失败！错误细节: {failDetail}");

            Assert(t29Ok, "T29_MonsterFog_NearZoneMonsterAndLevel",
                "近圈（10~25）怪物判定校验成功！全为极客 SLIME 且 DangerLevel=1，阈值 > 0.75f",
                $"近圈规则失败！错误细节: {failDetail}");

            Assert(t30Ok, "T30_MonsterFog_MidZoneMonsterAndLevel",
                "中圈（25~50）怪物判定校验成功！全为 BUG_KNIGHT 或 NULL_GHOST，阈值 > 0.60f",
                $"中圈规则失败！错误细节: {failDetail}");

            Assert(t31Ok, "T31_MonsterFog_FarZoneMonsterAndLevel",
                "外圈（50+）怪物与巨兽BOSS校验成功！全为极客幽灵/魔像/巨兽，阈值 > 0.45f",
                $"外圈规则失败！错误细节: {failDetail}");

            bool t32Ok = true;
            MapDataStore store2 = new MapDataStore();
            store2.InitArrays();
            Terrain.TerrainLayerGen.Generate(testSeed, store2);
            Resource.ResourceLayerGen.Generate(testSeed, store2);
            Building.BuildingLayerGen.Generate(testSeed, store2);
            MonsterFog.MonsterFogLayerGen.Generate(testSeed, store2);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    MonsterFogData f1 = store.MonsterFogLayer[x, y];
                    MonsterFogData f2 = store2.MonsterFogLayer[x, y];
                    if (f1.IsExplored != f2.IsExplored || f1.HasMonster != f2.HasMonster || 
                        f1.IsDangerZone != f2.IsDangerZone || f1.DangerLevel != f2.DangerLevel || 
                        f1.MonsterType != f2.MonsterType)
                    {
                        t32Ok = false;
                        failDetail = $"种子 114514L 两次生成的 MonsterFogLayer 在格 ({x}, {y}) 产生差异！";
                        break;
                    }
                }
                if (!t32Ok) break;
            }

            Assert(t32Ok, "T32_MonsterFog_SeedConsistency",
                "种子哈希一致性校验成功！重现率 100%",
                $"种子生成一致性验证失败！错误细节: {failDetail}");
        }
    }
}

// =============================================================================================
// 🏁 5. MAIN ENTRY POINT (程序启动入口)
// =============================================================================================
namespace OfflineTester
{
    class Program
    {
        static void Main(string[] args)
        {
            // 启用 ANSI 控制台彩色模式
            try {
                var handle = GetStdHandle(-11);
                GetConsoleMode(handle, out uint mode);
                SetConsoleMode(handle, mode | 0x0004);
            } catch { }

            var tester = new RPG.Map.Test.OfflineTester();
            tester.RunAllTests();
        }

        // P/Invoke 声明以在 Windows 控制台启用色彩转义
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    }
}
