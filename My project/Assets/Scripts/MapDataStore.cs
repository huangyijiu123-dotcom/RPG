using System;
using UnityEngine;

namespace RPG.Map
{
    /// <summary>
    /// 地形类型枚举
    /// </summary>
    public enum TerrainType
    {
        PLAINS = 0,      // 草原（主要可建造地形）
        MOUNTAIN = 1,    // 山地（采石/采矿）
        WATER = 2,       // 水域（不可通行，成片连续分布）
        SWAMP = 3,       // 沼泽（低温度+高湿度区域）
        TUNDRA = 4,      // 冻土（低温度+低湿度极远圈层）
        VOLCANO = 5,     // 火山地表（极高温度，极危险圈层）
        RUINS = 6,       // 遗迹（废弃文明残骸，特殊圈层）
        CITY_STATE = 7,  // 城邦（极远圈层，可交互的中立势力）
        BASE_CAMP = 8    // 大本营（玩家出生点，唯一固定在地图中心）
    }

    /// <summary>
    /// 气候数据结构
    /// </summary>
    [Serializable]
    public struct ClimateData
    {
        public float Altitude;     // 高度值 0.0 ~ 1.0
        public float Humidity;     // 湿度值 0.0 ~ 1.0
        public float Temperature;  // 温度值 0.0 ~ 1.0（0=极寒，1=极热）

        public ClimateData(float altitude, float humidity, float temperature)
        {
            Altitude = altitude;
            Humidity = humidity;
            Temperature = temperature;
        }
    }

    /// <summary>
    /// 资源数据结构（第二层：ResourceLayer）
    /// </summary>
    [Serializable]
    public struct ResourceData
    {
        public bool HasForest;          // 是否有森林
        public int ForestDensity;       // 森林密度 0~100（0=无，100=茂密）
        public bool HasMineralVein;     // 是否有矿脉
        public string MineralType;      // 矿脉类型 "IRON" / "STONE" / "CRYSTAL" (空=无)
        public bool HasHerbs;           // 是否有草药（沼泽地形专属资源）
    }

    /// <summary>
    /// 建筑数据结构（第三层：BuildingLayer）
    /// </summary>
    [Serializable]
    public struct BuildingData
    {
        public bool HasBuilding;        // 是否有建筑
        public string BuildingType;     // 建筑类型 ID (如 "LUMBER_CAMP", 空=无)
        public bool IsWorldGenerated;   // true=世界生成的遗迹/中立，false=玩家建造的
        public int BuildingLevel;       // 建筑等级，默认 1
        public bool IsBuildingBlocked;  // 该建筑是否因为怪物占领而失效/无法使用
    }

    /// <summary>
    /// 怪物与迷雾数据结构（第四层：MonsterFogLayer）
    /// </summary>
    [Serializable]
    public struct MonsterFogData
    {
        public bool IsDangerZone;       // 是否是危险格（基础生成标记，视觉上红色/暗色外框）
        public int DangerLevel;         // 危险等级 1~5
        public string MonsterType;      // 怪物类型 ID (如 "SLIME", 空=随机)
        public bool HasMonster;         // 该格子当前是否正有存活的怪物
        public bool IsExplored;         // 迷雾/探索状态（true=已探索无雾，false=未探索被黑雾笼罩）
    }

    /// <summary>
    /// 气候数据控制器接口（支持未来动态季节系统）
    /// </summary>
    public interface IClimateController
    {
        ClimateData GetClimateData(int x, int y);
        void UpdateClimate(float gameTime);
    }

    /// <summary>
    /// 静态气候控制器（静态气候在运行时不发生变化，但预留此接口）
    /// </summary>
    public class StaticClimateController : IClimateController
    {
        private readonly ClimateData[,] _staticClimate;
        private readonly int _width;
        private readonly int _height;

        public StaticClimateController(ClimateData[,] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _staticClimate = data;
            _width = data.GetLength(0);
            _height = data.GetLength(1);
        }

        public ClimateData GetClimateData(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                // 超出边界时返回默认数据，防崩溃
                return new ClimateData(0f, 0f, 0f);
            }
            return _staticClimate[x, y];
        }

        public void UpdateClimate(float gameTime)
        {
            // 静态气候在运行时不发生变化
        }
    }

    /// <summary>
    /// 全局唯一数据仓库，存储四层数组及状态
    /// </summary>
    public class MapDataStore : MonoBehaviour
    {
        public static MapDataStore Instance { get; private set; }

        public const int MAP_SIZE = 200;
        public const int CENTER = 100; // 地图中心坐标点常量
        
        [Header("地图基本参数")]
        public long CurrentSeed = 114514L;

        // 四层及属性矩阵数据（由具体生成器填充，保留 public set 以便生成器直接写入）
        public TerrainType[,] TerrainLayer { get; set; }
        public ClimateData[,] RawClimateLayer { get; set; }
        public ResourceData[,] ResourceLayer { get; set; }
        public BuildingData[,] BuildingLayer { get; set; }
        public MonsterFogData[,] MonsterFogLayer { get; set; }

        // 气候控制器
        public IClimateController ClimateController { get; set; }

        private void Awake()
        {
            // 单例模式初始化
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitArrays();
        }

        /// <summary>
        /// 初始化各层数组
        /// </summary>
        public void InitArrays()
        {
            TerrainLayer = new TerrainType[MAP_SIZE, MAP_SIZE];
            RawClimateLayer = new ClimateData[MAP_SIZE, MAP_SIZE];
            ResourceLayer = new ResourceData[MAP_SIZE, MAP_SIZE];
            BuildingLayer = new BuildingData[MAP_SIZE, MAP_SIZE];
            MonsterFogLayer = new MonsterFogData[MAP_SIZE, MAP_SIZE];
        }

        /// <summary>
        /// 工具方法：检查坐标是否合法
        /// </summary>
        public static bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < MAP_SIZE && y >= 0 && y < MAP_SIZE;
        }
    }
}
