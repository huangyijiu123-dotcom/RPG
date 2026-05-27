using UnityEngine;

namespace RPG.Map
{
    /// <summary>
    /// 全图四层数据生成最高统筹管理器
    /// 职责：在启动或读档时，按顺序调度一至四层数据生成器，并注入气候控制器及重绘 Tilemap。
    /// 实现说明书 §8 节。
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        [Header("地图生成配置")]
        public bool generateOnStart = true; // 是否在启动时自动运行生成
        public long seed = 114514L;         // 默认生成的随机种子

        // 缓存 MapRenderer 的私有引用，规避运行时重复调用 FindObjectOfType 的性能消耗
        private MapRenderer _mapRenderer;

        private void Start()
        {
            // 1. 在初始化阶段执行一次性查找并缓存渲染器引用
            _mapRenderer = FindObjectOfType<MapRenderer>();

            if (generateOnStart)
            {
                GenerateMap(seed);
            }
        }

        /// <summary>
        /// 核心统筹生成入口。
        /// 按照第一层 → 第二层 → 第三层 → 第四层 的严格顺序序列化生成。
        /// </summary>
        /// <param name="mapSeed">传入的随机种子</param>
        public void GenerateMap(long mapSeed)
        {
            // 2. 获取全局唯一单例存储中心
            MapDataStore store = MapDataStore.Instance;
            if (store == null)
            {
                Debug.LogError("[MapGenerator] 无法找到 MapDataStore.Instance 实例，地图统筹生成中止。");
                return;
            }

            // 同步保存当前使用的种子
            store.CurrentSeed = mapSeed;

            Debug.Log($"[MapGenerator] ========== 🚀 开始统筹生成全新 200x200 游戏地图 | 种子: {mapSeed} ==========");

            // 3. 严格遵循开发规则 1.10，不调用 InitArrays() 例外，而是逐层安全单独开辟全新大数组空间，防止重置逻辑缺陷
            store.TerrainLayer    = new TerrainType[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
            store.RawClimateLayer  = new ClimateData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
            store.ResourceLayer    = new ResourceData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
            store.BuildingLayer    = new BuildingData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
            store.MonsterFogLayer  = new MonsterFogData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];

            // 4. 第一层：生成基础地形、气候数据、大本营保护及 3 个中立城邦位置标记
            TerrainLayerGen.Generate(mapSeed, store);

            // 5. 第二层：读取地形标记，基于Perlin噪声生成森林、矿脉圈层、及沼泽稀有草药分布
            ResourceLayerGen.Generate(mapSeed, store);

            // 6. 第三层：将大本营/城邦转换为实体世界级建筑数据，并生成预置世界遗迹与远古极客奇观
            BuildingLayerGen.Generate(mapSeed, store);

            // 7. 第四层：执行全图迷雾覆盖与大本营开雾可见，并基于三段式硬阈值及分档哈希刷新极客怪
            MonsterFogLayerGen.Generate(mapSeed, store);

            // 8. 气候偏向源注入：初始化 StaticClimateController 并注入，为未来动态季节系统提供标准 IClimateController 接口支持
            store.ClimateController = new StaticClimateController(store.RawClimateLayer);

            Debug.Log("[MapGenerator] ========== 🏆 四层游戏底层网格数据生成统筹完成 ==========");

            // 9. 触发渲染层重绘：如果缓存的 _mapRenderer 不为空则执行全量重绘瓦片
            if (_mapRenderer != null)
            {
                Debug.Log("[MapGenerator] 正在通知 MapRenderer 执行瓦片全量绘制更新...");
                _mapRenderer.RenderAllLayers();
            }
            else
            {
                Debug.LogWarning("[MapGenerator] 场景中未检测到合法的 MapRenderer 实例（或尚未实现），已安全跳过渲染绘制阶段。");
            }
        }
    }
}
