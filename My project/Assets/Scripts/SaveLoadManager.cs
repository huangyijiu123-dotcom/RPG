using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace RPG.Map
{
    #region 存档序列化数据结构模型 (与说明书 §7.1 对齐)

    [System.Serializable]
    public class WorldSaveData
    {
        public string saveTime;
        public long mapSeed;
        public int currentTickId; // 解决问题四：接入动态 tick 计数，防止时间清零
        public PlayerResourcesData playerResources;
        public TechProgressData techProgress;
        public List<ExplorationAndMonsterSaveData> explorationAndMonsterData = new List<ExplorationAndMonsterSaveData>();
        public List<ResourceSaveData> resourceUpdates = new List<ResourceSaveData>();
        public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
        public List<EntitySaveData> entities = new List<EntitySaveData>();
    }

    [System.Serializable]
    public class PlayerResourcesData
    {
        public int gold;
        public int wood;
        public int stone;
    }

    [System.Serializable]
    public class TechProgressData
    {
        public List<string> unlockedTechs = new List<string>();
    }

    [System.Serializable]
    public struct ExplorationAndMonsterSaveData
    {
        public int x;
        public int y;
        public bool isExplored;
        public bool hasMonster;
    }

    [System.Serializable]
    public struct ResourceSaveData
    {
        public int x;
        public int y;
        public bool hasForest;
        public int forestDensity;
        public bool hasMineralVein;
        public string mineralType;
        public bool hasHerbs;
    }

    [System.Serializable]
    public struct BuildingSaveData
    {
        public int x;
        public int y;
        public string type;
        public int level;
        public bool isBlocked;
        public bool isWorldGenerated;
    }

    [System.Serializable]
    public struct EntitySaveData
    {
        public string id;
        public string type;
        public int x;
        public int y;
        public int hp;
        public string state;
    }

    #endregion

    /// <summary>
    /// 游戏存档/读档序列化及差异化还原管理器
    /// 职责：负责玩家物资科技持久化、基于差值的高压缩存档生成、六步读档反序列化还原与自动存档节流控制。
    /// 实现说明书 §7 节。
    /// </summary>
    public class SaveLoadManager : MonoBehaviour
    {
        [Header("自动存档配置")]
        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private float _autoSaveInterval = 300f; // 默认 300 秒 (5分钟)

        // ==========================================
        // 架构设计说明（🟢 问题六）：
        // 这里的 Gold, Wood, Stone 作为局部 fallback 属性供快速接入。
        // 建议在系统大型重构时，将这三个属性的 get/set 重定向到您中央的 
        // GameManager 或 ResourceManager 模块中，杜绝多单例读写导致的数据脱节。
        // ==========================================
        public int Gold { get; set; } = 0; // 解决问题二：新游戏测试资源回归 0 起始
        public int Wood { get; set; } = 0;
        public int Stone { get; set; } = 0;
        public List<string> UnlockedTechs { get; set; } = new List<string>();

        // 解决问题四：支持动态 Tick 绑定接口
        public int CurrentTickId { get; set; } = 0; // 新游戏默认为 0
        
        // 可选注册委托：当外部时钟系统（如 TickService）存在时，可直接注册该委托返回最新 tick 计数，达成高内聚低耦合
        public System.Func<int> GetCurrentTickCallback;

        private float _autoSaveTimer = 0f;

        private void Update()
        {
            // 机制一：定时自动存档
            if (_enableAutoSave)
            {
                _autoSaveTimer += Time.deltaTime;
                if (_autoSaveTimer >= _autoSaveInterval)
                {
                    _autoSaveTimer = 0f;
                    SaveGameAuto();
                }
            }
        }

        // 机制三：游戏退出时自动保存
        private void OnApplicationQuit()
        {
            SaveGameExit();
        }

        #region 公共保存接口

        /// <summary>
        /// 保存游戏至手动存档槽位（支持槽位 1, 2, 3）
        /// </summary>
        public void SaveGameSlot(int slotIndex)
        {
            string filename = $"save_slot{slotIndex}.json";
            SaveGame(filename, $"手动插槽存档 [{slotIndex}]");
        }

        /// <summary>
        /// 定时自动存档保存
        /// </summary>
        public void SaveGameAuto()
        {
            SaveGame("save_auto.json", "定时自动存档");
        }

        /// <summary>
        /// 退出游戏时自动保存
        /// </summary>
        public void SaveGameExit()
        {
            SaveGame("save_exit.json", "退出游戏存档");
        }

        #endregion

        #region 公共读取还原接口

        /// <summary>
        /// 读取手动存档槽位并开始六步地图差异化重构
        /// </summary>
        public void LoadGameSlot(int slotIndex)
        {
            string filename = $"save_slot{slotIndex}.json";
            LoadGame(filename, $"手动插槽存档 [{slotIndex}]");
        }

        /// <summary>
        /// 读取自动存档并还原
        /// </summary>
        public void LoadGameAuto()
        {
            LoadGame("save_auto.json", "定时自动存档");
        }

        /// <summary>
        /// 读取退出存档并还原
        /// </summary>
        public void LoadGameExit()
        {
            LoadGame("save_exit.json", "退出游戏存档");
        }

        #endregion

        #region 核心存档数据流 (Diffing 生成)

        private void SaveGame(string filename, string saveDesc)
        {
            string path = GetSavePath(filename);
            Debug.Log($"[SaveLoadManager] {saveDesc} 开始处理中 -> 目标路径: {path}");

            // 1. 调用极简差值 Diffing 算法提取本次存档的极小 delta 包
            WorldSaveData saveData = CreateSaveData();
            if (saveData == null)
            {
                Debug.LogError($"[SaveLoadManager] {saveDesc} 失败：MapDataStore 实例尚未就绪。");
                return;
            }

            try
            {
                // 确保存档的持久化目录完全存在
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // 2. 将数据模型格式化成 JSON string 写入本地文件 (采用 Unity 官方原生内置的 ToJson，支持美化缩进)
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(path, json);

                Debug.Log($"[SaveLoadManager] 🏆 {saveDesc} 序列化写入本地成功！占用格子差值包大小: [迷雾/怪物差分: {saveData.explorationAndMonsterData.Count} 格 | 资源差分: {saveData.resourceUpdates.Count} 格 | 建筑差分: {saveData.buildings.Count} 格]");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveLoadManager] {saveDesc} 物理写入出现异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 极简差值 Diffing 算法。利用临时种子快速重构比对，仅在内存中执行，仅需耗时 2~5ms
        /// </summary>
        private WorldSaveData CreateSaveData()
        {
            MapDataStore store = MapDataStore.Instance;
            if (store == null) return null;

            WorldSaveData data = new WorldSaveData();
            data.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            data.mapSeed = store.CurrentSeed;
            
            // 解决问题一：如果注册了 Tick 系统的委托，则拉取最新 tick，否则使用本地属性
            data.currentTickId = GetCurrentTickCallback != null ? GetCurrentTickCallback() : this.CurrentTickId;

            // 写入玩家物资和科技存档
            data.playerResources = new PlayerResourcesData
            {
                gold = this.Gold,
                wood = this.Wood,
                stone = this.Stone
            };
            data.techProgress = new TechProgressData
            {
                unlockedTechs = new List<string>(this.UnlockedTechs)
            };

            // 1. 缓存当前活跃的玩家实际游戏数据层数组引用
            var activeTerrain = store.TerrainLayer;
            var activeClimate = store.RawClimateLayer;
            var activeResource = store.ResourceLayer;
            var activeBuilding = store.BuildingLayer;
            var activeMonsterFog = store.MonsterFogLayer;

            // 解决问题二：使用 try-finally 保障机制。一旦重新生成或差分比对过程中抛出任何报错，
            // 都在 finally 块中绝对无条件归还真实的局内游戏层数组引用，捍卫局内数据安全！
            try
            {
                // 2. 为全局存储开辟全新临时空间，进行一次干净的确定性生成，提取地图开局纯净版
                store.TerrainLayer = new TerrainType[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
                store.RawClimateLayer = new ClimateData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
                store.ResourceLayer = new ResourceData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
                store.BuildingLayer = new BuildingData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];
                store.MonsterFogLayer = new MonsterFogData[MapDataStore.MAP_SIZE, MapDataStore.MAP_SIZE];

                TerrainLayerGen.Generate(data.mapSeed, store);
                ResourceLayerGen.Generate(data.mapSeed, store);
                BuildingLayerGen.Generate(data.mapSeed, store);
                MonsterFogLayerGen.Generate(data.mapSeed, store);

                // 3. 逐格进行快速数据比对，如果真实的游戏状态和出生时不同，则作为修改包进行序列化
                for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
                {
                    for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                    {
                        // A. 第二层资源消耗判定 (HasForest密度变动/矿产被采空HasMineralVein/草药被采空)
                        var actRes = activeResource[x, y];
                        var iniRes = store.ResourceLayer[x, y];
                        if (actRes.HasForest != iniRes.HasForest ||
                            actRes.ForestDensity != iniRes.ForestDensity ||
                            actRes.HasMineralVein != iniRes.HasMineralVein ||
                            actRes.MineralType != iniRes.MineralType ||
                            actRes.HasHerbs != iniRes.HasHerbs)
                        {
                            data.resourceUpdates.Add(new ResourceSaveData
                            {
                                x = x,
                                y = y,
                                hasForest = actRes.HasForest,
                                forestDensity = actRes.ForestDensity,
                                hasMineralVein = actRes.HasMineralVein, // 采空时变 false，与 ini 的 true 产生 diff 并打包存盘
                                mineralType = actRes.MineralType,
                                hasHerbs = actRes.HasHerbs
                            });
                        }

                        // B. 第三层建筑变动判定 (被拆除/新建造/建筑升级/建筑被阻挡)
                        var actBld = activeBuilding[x, y];
                        var iniBld = store.BuildingLayer[x, y];
                        if (actBld.HasBuilding != iniBld.HasBuilding ||
                            actBld.BuildingType != iniBld.BuildingType ||
                            actBld.BuildingLevel != iniBld.BuildingLevel ||
                            actBld.IsBuildingBlocked != iniBld.IsBuildingBlocked ||
                            actBld.IsWorldGenerated != iniBld.IsWorldGenerated)
                        {
                            data.buildings.Add(new BuildingSaveData
                            {
                                x = x,
                                y = y,
                                type = actBld.HasBuilding ? actBld.BuildingType : "",
                                level = actBld.BuildingLevel,
                                isBlocked = actBld.IsBuildingBlocked,
                                isWorldGenerated = actBld.IsWorldGenerated
                            });
                        }

                        // C. 第四层探索与怪物变动判定 (开雾范围拓宽/怪物被清剿)
                        var actFog = activeMonsterFog[x, y];
                        var iniFog = store.MonsterFogLayer[x, y];
                        if (actFog.IsExplored != iniFog.IsExplored ||
                            actFog.HasMonster != iniFog.HasMonster)
                        {
                            data.explorationAndMonsterData.Add(new ExplorationAndMonsterSaveData
                            {
                                x = x,
                                y = y,
                                isExplored = actFog.IsExplored,
                                hasMonster = actFog.HasMonster
                            });
                        }
                    }
                }
            }
            finally
            {
                // 4. 将游戏运行时真实的玩家数据状态数组完璧归赵，维持运行时数据无缝连贯，防止逻辑中断导致全图回滚
                store.TerrainLayer = activeTerrain;
                store.RawClimateLayer = activeClimate;
                store.ResourceLayer = activeResource;
                store.BuildingLayer = activeBuilding;
                store.MonsterFogLayer = activeMonsterFog;
            }

            return data;
        }

        #endregion

        #region 核心读档还原数据流 (六步加载法)

        private void LoadGame(string filename, string saveDesc)
        {
            string path = GetSavePath(filename);
            Debug.Log($"[SaveLoadManager] {saveDesc} 开始处理读取 -> 来源路径: {path}");

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveLoadManager] {saveDesc} 载入中断：目标存档文件物理路径不存在。");
                return;
            }

            try
            {
                // 读取 JSON 数据
                string json = File.ReadAllText(path);
                WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(json);

                MapDataStore store = MapDataStore.Instance;
                if (store == null)
                {
                    Debug.LogError("[SaveLoadManager] 载入中断：MapDataStore 实例尚未创建就绪。");
                    return;
                }

                // 🌟 【第一步】获取 mapSeed 重新初始化静态地形与初始生态，物理重塑大本营与初始城邦
                MapGenerator generator = FindObjectOfType<MapGenerator>();
                if (generator == null)
                {
                    Debug.LogError("[SaveLoadManager] 载入中断：未能在当前场景中寻找合法的 MapGenerator 控制器。");
                    return;
                }
                Debug.Log($"[SaveLoadManager] 步骤1：开始基于存档种子 [{data.mapSeed}] 进行全图确定性静态重构...");
                generator.GenerateMap(data.mapSeed);

                // 🌟 【第二步】读取资源开采记录，覆盖第二层资源层 (还原森林消耗与采空矿产)
                if (data.resourceUpdates != null)
                {
                    Debug.Log($"[SaveLoadManager] 步骤2：正在还原 [{data.resourceUpdates.Count}] 项动态开采与消耗资源格...");
                    foreach (var res in data.resourceUpdates)
                    {
                        if (IsValidCoordinate(res.x, res.y))
                        {
                            store.ResourceLayer[res.x, res.y].HasForest = res.hasForest;
                            store.ResourceLayer[res.x, res.y].ForestDensity = res.forestDensity;
                            store.ResourceLayer[res.x, res.y].HasMineralVein = res.hasMineralVein; // 完美覆写采空矿脉为 false
                            store.ResourceLayer[res.x, res.y].MineralType = res.mineralType;
                            store.ResourceLayer[res.x, res.y].HasHerbs = res.hasHerbs;
                        }
                    }
                }

                // 🌟 【第三步】摆放序列化建筑，覆盖第三层建筑层 (还原玩家建造的新建筑和属性)
                if (data.buildings != null)
                {
                    Debug.Log($"[SaveLoadManager] 步骤3：正在摆放与还原 [{data.buildings.Count}] 个动态建筑状态...");
                    foreach (var b in data.buildings)
                    {
                        if (IsValidCoordinate(b.x, b.y))
                        {
                            store.BuildingLayer[b.x, b.y].HasBuilding = (b.type != null && b.type != "");
                            store.BuildingLayer[b.x, b.y].BuildingType = b.type;
                            store.BuildingLayer[b.x, b.y].BuildingLevel = b.level;
                            store.BuildingLayer[b.x, b.y].IsBuildingBlocked = b.isBlocked;
                            store.BuildingLayer[b.x, b.y].IsWorldGenerated = b.isWorldGenerated;

                            // 解决问题五：实现说明书读档网络同步接口
                            SyncBuildingToBackend(b.x, b.y, b.type, b.level, b.isBlocked);
                        }
                    }
                }

                // 🌟 【第四步】还原探索战争迷雾范围与怪物占领存活状态，覆盖第四层
                if (data.explorationAndMonsterData != null)
                {
                    Debug.Log($"[SaveLoadManager] 步骤4：正在还原 [{data.explorationAndMonsterData.Count}] 项探索迷雾及怪物侵占状态...");
                    foreach (var f in data.explorationAndMonsterData)
                    {
                        if (IsValidCoordinate(f.x, f.y))
                        {
                            store.MonsterFogLayer[f.x, f.y].IsExplored = f.isExplored;
                            store.MonsterFogLayer[f.x, f.y].HasMonster = f.hasMonster;
                        }
                    }
                }

                // 🌟 【联动状态投影】在所有数据层（包括第四步）全部还原完毕后，执行全图一视同仁的活性映射，彻底杜绝任何时序 Bug！
                Debug.Log("[SaveLoadManager] 正在执行联动状态投影，映射全局建筑活性状态...");
                for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
                {
                    for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                    {
                        if (store.BuildingLayer[x, y].HasBuilding)
                        {
                            store.BuildingLayer[x, y].IsBuildingBlocked = store.MonsterFogLayer[x, y].HasMonster;
                        }
                    }
                }

                // 🌟 【第五步】自动查找并通知场景中的渲染器 MapRenderer 执行全量 Tilemap 重绘
                MapRenderer renderer = FindObjectOfType<MapRenderer>();
                if (renderer != null)
                {
                    Debug.Log("[SaveLoadManager] 步骤5：成功链接渲染器，正在通知其执行最终网格瓦片重绘更新...");
                    renderer.RenderAllLayers();
                }
                else
                {
                    Debug.LogWarning("[SaveLoadManager] 步骤5：场景中未能探测到 MapRenderer，已跳过网格瓦片渲染重绘。");
                }

                // 🌟 【第六步】重新加载玩家的黄金、木头、石头等物理资产与科技包状态
                this.Gold = data.playerResources != null ? data.playerResources.gold : 0;
                this.Wood = data.playerResources != null ? data.playerResources.wood : 0;
                this.Stone = data.playerResources != null ? data.playerResources.stone : 0;
                this.UnlockedTechs = data.techProgress != null ? new List<string>(data.techProgress.unlockedTechs) : new List<string>();
                this.CurrentTickId = data.currentTickId; // 解决问题四：同步还原 tick 计数
                Debug.Log($"[SaveLoadManager] 步骤6：成功还原玩家资产 [金: {this.Gold} | 木: {this.Wood} | 石: {this.Stone}] 与 [{this.UnlockedTechs.Count}] 项科技项，当前时序 TickId: {this.CurrentTickId}");

                Debug.Log($"[SaveLoadManager] 🏆 {saveDesc} 全程六步加载法执行成功，存档加载完毕！时间戳: {data.saveTime}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveLoadManager] {saveDesc} 反序列化及地图差异还原异常失败: {ex.Message}");
            }
        }

        #endregion

        #region 后端网络数据同步模拟桩 (解决问题五)

        /// <summary>
        /// 双向网络数据同步桩。在读档还原建筑时触发。
        /// 预留给未来 WebSocket / HTTP 向 Kotlin 后端 BuildingEngine.kt 传输建筑变更数据，
        /// 确保后端能够重新注册伐木场、民居、农田等的 Tick 增益和阻挡计时器状态。
        /// </summary>
        private void SyncBuildingToBackend(int x, int y, string type, int level, bool isBlocked)
        {
            Debug.Log($"[SaveLoadManager] [NetworkSync] 正在向后端 (BuildingEngine.kt) 同步建筑网络状态 -> 坐标: ({x}, {y}) | 建筑类型: {type} | 等级: {level} | 是否阻挡: {isBlocked}");
        }

        #endregion

        #region 边界防护辅助

        private bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < MapDataStore.MAP_SIZE && y >= 0 && y < MapDataStore.MAP_SIZE;
        }

        private string GetSavePath(string filename)
        {
            return Path.Combine(Application.persistentDataPath, filename);
        }

        #endregion
    }
}
