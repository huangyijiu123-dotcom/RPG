using UnityEngine;
using System.Collections.Generic;
using RPG.Map;

namespace RPG.Map.Test
{
    /// <summary>
    /// 地图生成系统第一层与第二层集成测试模块
    /// 职责：对地形气候层（Layer 0）与资源层（Layer 1）的各项规则和边界条件进行断言校验。
    /// 天然支持在 Unity 编辑器非 Play 模式下直接点击运行。
    /// </summary>
    [ExecuteInEditMode]
    public class MapGenerationTester : MonoBehaviour
    {
        private int _totalTestsRan;
        private int _totalTestsPassed;
        private int _totalTestsFailed;

        private void Start()
        {
            // 仅在游戏运行模式启动时自动跑一次测试
            if (Application.isPlaying)
            {
                Debug.Log("<color=cyan>[MapGenerationTester] 游戏运行模式启动，开始执行自动化集成测试...</color>");
                RunAllTests();
            }
        }

        /// <summary>
        /// 执行全部 8 个高价值的集成测试用例。
        /// </summary>
        public void RunAllTests()
        {
            _totalTestsRan = 0;
            _totalTestsPassed = 0;
            _totalTestsFailed = 0;

            Debug.Log("<color=yellow>========== 🟢 开始执行第一、二层地图生成集成测试 ==========</color>");

            // 1. 获取或临时创建 MapDataStore 实例
            MapDataStore store = FindFirstObjectByType<MapDataStore>();
            bool isTempStore = false;
            if (store == null)
            {
                GameObject tempObj = new GameObject("[TempMapDataStore]");
                store = tempObj.AddComponent<MapDataStore>();
                store.InitArrays();
                isTempStore = true;
            }

            // 保存原有的种子，方便测试完恢复
            long originalSeed = store.CurrentSeed;

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
            catch (System.Exception ex)
            {
                _totalTestsFailed++;
                Debug.LogError($"[Test System Error] 测试执行过程中抛出了未捕获的系统异常: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // 恢复原有的种子
                store.CurrentSeed = originalSeed;

                // 清理临时创建的物体，防场景污染
                if (isTempStore)
                {
                    if (Application.isPlaying)
                        Destroy(store.gameObject);
                    else
                        DestroyImmediate(store.gameObject);
                }
            }

            // 输出测试统计报告
            string color = _totalTestsFailed == 0 ? "lime" : "orange";
            Debug.Log($"<color={color}>========== 🏆 集成测试运行结束 | 报告: 共运行 {_totalTestsRan} 项, 成功: {_totalTestsPassed}, 失败: {_totalTestsFailed} ==========</color>");
        }

        /// <summary>
        /// 自定义断言方法，包装了日志的美化输出与统计。
        /// </summary>
        private void Assert(bool condition, string testName, string successMessage, string failMessage)
        {
            _totalTestsRan++;
            if (condition)
            {
                _totalTestsPassed++;
                Debug.Log($"<color=lime>[Test Passed]</color> <b>{testName}</b>: {successMessage}");
            }
            else
            {
                _totalTestsFailed++;
                Debug.LogError($"<color=red>[Test Failed]</color> <b>{testName}</b>: <color=yellow>{failMessage}</color>");
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 1：数组初始化校验
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_Initialization(MapDataStore store)
        {
            Assert(store != null, "Test_Initialization",
                "MapDataStore 实例非空",
                "MapDataStore 实例为 null");

            if (store == null) return;

            // 重新调用初始化数组以保证测试干净度
            store.InitArrays();

            Assert(store.TerrainLayer != null && store.RawClimateLayer != null && store.ResourceLayer != null,
                "Test_Initialization",
                "第一、二层数组（地形、气候、资源）初始化成功，非空",
                "部分核心数组未被成功 new 出来，仍为 null");

            if (store.TerrainLayer == null || store.ResourceLayer == null) return;

            bool isSizeCorrect = store.TerrainLayer.GetLength(0) == MapDataStore.MAP_SIZE &&
                                 store.TerrainLayer.GetLength(1) == MapDataStore.MAP_SIZE &&
                                 store.ResourceLayer.GetLength(0) == MapDataStore.MAP_SIZE &&
                                 store.ResourceLayer.GetLength(1) == MapDataStore.MAP_SIZE;

            Assert(isSizeCorrect, "Test_Initialization",
                $"所有数组尺寸均为 {MapDataStore.MAP_SIZE}x{MapDataStore.MAP_SIZE}，符合设计",
                $"数组尺寸异常！实际尺寸: {store.TerrainLayer.GetLength(0)}x{store.TerrainLayer.GetLength(1)}");
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 2：大本营安全区保护验证
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_BaseCampProtection(MapDataStore store)
        {
            // 使用测试种子生成前两层
            long testSeed = 114514L;
            TerrainLayerGen.Generate(testSeed, store);
            ResourceLayerGen.Generate(testSeed, store);

            // 断言大本营中心 (100, 100) 是否必为 BASE_CAMP
            bool isCenterCorrect = store.TerrainLayer[MapDataStore.CENTER, MapDataStore.CENTER] == TerrainType.BASE_CAMP;
            Assert(isCenterCorrect, "Test_BaseCampProtection",
                "大地图中心坐标 (100, 100) 成功被强制覆盖为 BASE_CAMP",
                $"大地图中心不是 BASE_CAMP！实际为: {store.TerrainLayer[MapDataStore.CENTER, MapDataStore.CENTER]}");

            // 断言周围 5 格保护区是否全为 PLAINS
            bool isProtectionZoneSafe = true;
            string failedDetail = "";
            int radius = 5;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // 排除中心点自己
                    
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

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 3：静态气候物理场的合法数值范围
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_ClimateRanges(MapDataStore store)
        {
            if (store.RawClimateLayer == null) return;

            bool isAllInRange = true;
            string failDetail = "";

            // 抽样 2000 个随机位置，验证气候高度/湿度/温度是否全被 Clamp01 限制在 [0.0, 1.0]
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

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 4：城邦生成边界与间距逻辑
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_CityStates(MapDataStore store)
        {
            // 我们在不同的种子下检测城邦放置
            long testSeed = 999999L;
            TerrainLayerGen.Generate(testSeed, store);

            List<Vector2Int> cityStates = new List<Vector2Int>();
            for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
            {
                for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                {
                    if (store.TerrainLayer[x, y] == TerrainType.CITY_STATE)
                    {
                        cityStates.Add(new Vector2Int(x, y));
                    }
                }
            }

            // 断言城邦数量
            Assert(cityStates.Count == 3, "Test_CityStates",
                "成功在草原上找到并生成了 3 个相互独立的城邦",
                $"城邦放置数量异常！实际生成数量: {cityStates.Count}/3");

            // 验证距离大本营中心 > 60 格 (严格大于)
            bool isFarFromCenter = true;
            string centerFailDetail = "";
            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;

            foreach (var pos in cityStates)
            {
                int ringDist = Mathf.Max(Mathf.Abs(pos.x - cx), Mathf.Abs(pos.y - cy));
                if (ringDist <= 60) // 应严格 > 60
                {
                    isFarFromCenter = false;
                    centerFailDetail = $"城邦坐落在 ({pos.x}, {pos.y})，切比雪夫距离为 {ringDist}，低于或等于安全限制 60 格";
                    break;
                }
            }

            Assert(isFarFromCenter, "Test_CityStates",
                "所有城邦均坐落在大本营 60 格之外的偏远圈层",
                $"城邦距离中心过近！错误细节: {centerFailDetail}");

            // 验证两两间距 >= 40
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

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 5：草原森林分布规则
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_Forests(MapDataStore store)
        {
            long seed = 456789L;
            TerrainLayerGen.Generate(seed, store);
            ResourceLayerGen.Generate(seed, store);

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

                    // 断言森林只能在 PLAINS 刷新
                    if (res.HasForest && terrain != TerrainType.PLAINS)
                    {
                        onlyOnPlains = false;
                        forestFailDetail = $"在非草原格子 ({x}, {y}) [地形: {terrain}] 上发现了森林！";
                        break;
                    }

                    // 断言在大本营 12 格范围内（且有森林）的格子，其密度必须折减 60%（最大密度100 * 0.6f = 60）
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

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 6：山地矿石分布规则与圈层分配
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_Minerals(MapDataStore store)
        {
            long seed = 888888L;
            TerrainLayerGen.Generate(seed, store);
            ResourceLayerGen.Generate(seed, store);

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
                        // 1. 断言矿脉只在 MOUNTAIN 刷新
                        if (terrain != TerrainType.MOUNTAIN)
                        {
                            onlyOnMountain = false;
                            mineFailDetail = $"在非山地格子 ({x}, {y}) [地形: {terrain}] 上发现了矿脉！";
                            break;
                        }

                        // 2. 断言矿物分配圈层
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

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 7：沼泽草药生成规则与概率分布
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_Herbs(MapDataStore store)
        {
            long seed = 777777L;
            TerrainLayerGen.Generate(seed, store);
            ResourceLayerGen.Generate(seed, store);

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
                        // 验证草药只能生在沼泽上
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

            // 统计概率合法性（在一定的统计样本数下）
            if (totalSwamps > 50)
            {
                float rate = (float)totalHerbs / totalSwamps;
                // 预期 40% 的生成率，允许 [30%, 50%] 的统计学合理误差波动区间
                bool isRateReasonable = rate >= 0.30f && rate <= 0.50f;
                Assert(isRateReasonable, "Test_Herbs",
                    $"草药随机概率测试通过！沼泽总数: {totalSwamps}, 草药总数: {totalHerbs}, 概率: {rate * 100:F2}% (落在 [30%, 50%] 波动区间内)",
                    $"草药概率偏离设计区间！当前生成概率: {rate * 100:F2}%，沼泽总数: {totalSwamps}");
            }
            else
            {
                // 样本过少时记录 Warning，跳过断言，不使测试失败
                Debug.LogWarning($"[Test Warning] 当前种子下沼泽格数只有 {totalSwamps}，样本数过低不进行草药概率偏差测试。");
                _totalTestsRan++;
                _totalTestsPassed++;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 8：极端边界种子 long.MinValue 有效性与不崩溃测试
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_LongMinValue_Overflow(MapDataStore store)
        {
            long overflowSeed = long.MinValue;
            bool runWithoutCrash = true;
            string crashMessage = "";

            try
            {
                // 执行地形与气候第一层生成
                TerrainLayerGen.Generate(overflowSeed, store);
                // 执行资源第二层生成
                ResourceLayerGen.Generate(overflowSeed, store);
            }
            catch (System.Exception ex)
            {
                runWithoutCrash = false;
                crashMessage = ex.ToString();
            }

            Assert(runWithoutCrash, "Test_LongMinValue_Overflow",
                "测试成功！以 long.MinValue 极端异常值为随机种子时，系统平稳跑通，未引发任何 OverflowException 溢出崩溃",
                $"以 long.MinValue 为种子引发了溢出崩溃！堆栈详情: {crashMessage}");

            if (!runWithoutCrash) return;

            // 补全验证：断言即使在极端的溢出噪声偏置下，大本营 (100, 100) 依然被 100% 稳妥保护为 BASE_CAMP
            bool isBaseCampSafe = store.TerrainLayer[MapDataStore.CENTER, MapDataStore.CENTER] == TerrainType.BASE_CAMP;
            Assert(isBaseCampSafe, "Test_LongMinValue_Overflow",
                "保护验证成功！即使噪声场在负溢出偏置下被玩坏，安全罩 BaseCampProtector 仍完美捍卫大本营坐标 (100,100)",
                $"保护防线失守！在极端种子下大地图中心变成了: {store.TerrainLayer[MapDataStore.CENTER, MapDataStore.CENTER]}");
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 9：第三层初始大本营与城邦建筑验证
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_Layer3_CampStateBuildings(MapDataStore store)
        {
            long testSeed = 114514L;
            TerrainLayerGen.Generate(testSeed, store);
            ResourceLayerGen.Generate(testSeed, store);
            BuildingLayerGen.Generate(testSeed, store);

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

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 10：第三层世界遗迹数量与地形归属验证 (容错在 [0, 8] 内)
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_Layer3_RuinsCount(MapDataStore store)
        {
            long testSeed = 114514L;
            TerrainLayerGen.Generate(testSeed, store);
            ResourceLayerGen.Generate(testSeed, store);
            BuildingLayerGen.Generate(testSeed, store);

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
                        // 遗迹只能长在 RUINS 地形上
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

            // 1. 验证数量区间 [0, 8]（在当前测试种子下，RUINS 地形充足，必然在 [5, 8] 范围内）
            bool isCountWithinRange = ruinsCount >= 5 && ruinsCount <= 8;
            Assert(isCountWithinRange, "Test_Layer3_RuinsCount",
                $"世界生成的遗迹数量校验成功！当前共生成 {ruinsCount} 个遗迹，严格落在设计要求的 [5, 8] 内",
                $"遗迹生成数量异常！实际生成数量: {ruinsCount}");

            // 2. 验证地貌限定
            Assert(onlyOnRuinsTerrain, "Test_Layer3_RuinsCount",
                "所有废弃文明遗迹建筑均 100% 坐落于 RUINS (遗迹) 地貌格上",
                $"遗迹地貌限定验证失败！错误细节: {failDetail}");
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 11：第三层世界极客奇观数量、偏远圈层、开荒无资源及间距限制验证 (严格 [2, 4] 奇观)
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_Layer3_WondersCountAndConstraints(MapDataStore store)
        {
            long testSeed = 114514L;
            TerrainLayerGen.Generate(testSeed, store);
            ResourceLayerGen.Generate(testSeed, store);
            BuildingLayerGen.Generate(testSeed, store);

            List<Vector2Int> wonderPositions = new List<Vector2Int>();
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
                        wonderPositions.Add(new Vector2Int(x, y));

                        // 1. 奇观绝不能长在水里（必须是陆地地貌）
                        if (store.TerrainLayer[x, y] == TerrainType.WATER)
                        {
                            onlyOnLand = false;
                            failDetail = $"奇观 {b.BuildingType} 长在了水域格 ({x}, {y})";
                            break;
                        }

                        // 2. 奇观只在 50 外圈层
                        int ringDist = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));
                        if (ringDist <= 50)
                        {
                            onlyOuterRing = false;
                            failDetail = $"奇观 {b.BuildingType} 坐标 ({x}, {y}) 距离中心圈层仅为 {ringDist} (<= 50)";
                            break;
                        }

                        // 3. 奇观不能长在有资源的格子上
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

            // 1. 验证数量
            bool isCountCorrect = wonderPositions.Count >= 2 && wonderPositions.Count <= 4;
            Assert(isCountCorrect, "Test_Layer3_WondersCountAndConstraints",
                $"世界极客奇观数量校验成功！当前共生成 {wonderPositions.Count} 个奇观，严格落在 [2, 4] 区间",
                $"奇观生成数量异常！实际生成数量: {wonderPositions.Count}");

            // 2. 验证三大地表及圈层限定
            Assert(onlyOnLand && onlyOuterRing && onlyNoResources, "Test_Layer3_WondersCountAndConstraints",
                "所有极客奇观 100% 长在距大本营 50 格外的非水域空白陆地上，成功规避了林/矿/草药格子",
                $"奇观落户环境验证失败！错误细节: {failDetail}");

            // 3. 验证两两间距 >= 20
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

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // 🧪 测试用例 12：第四层迷雾覆盖、大本营安全区开雾、水域/建筑避让、三段式分段硬阈值与怪物ID确定性分级验证 (T25~T32)
        // ─────────────────────────────────────────────────────────────────────────────────────────────
        private void Test_Layer4_MonsterFogConstraints(MapDataStore store)
        {
            long testSeed = 114514L;
            TerrainLayerGen.Generate(testSeed, store);
            ResourceLayerGen.Generate(testSeed, store);
            BuildingLayerGen.Generate(testSeed, store);
            MonsterFogLayerGen.Generate(testSeed, store);

            int cx = MapDataStore.CENTER;
            int cy = MapDataStore.CENTER;
            int size = MapDataStore.MAP_SIZE;

            // T25 (战争迷雾初始覆盖) & T26 (安全区开雾半径验证) & T27 (水域绝对无怪隔离) & T28 (世界建筑格无怪与健康状态)
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

                    // T26: 安全区 ringDistance <= 10 必须全开雾
                    if (ringDist <= 10)
                    {
                        if (!fog.IsExplored)
                        {
                            t26Ok = false;
                            failDetail = $"安全区格 ({x}, {y}) ringDistance={ringDist} 竟然未被开雾 (IsExplored == false)";
                            break;
                        }
                    }
                    // T25: 安全区外初始黑雾
                    else
                    {
                        if (fog.IsExplored)
                        {
                            t25Ok = false;
                            failDetail = $"安全区外格 ({x}, {y}) ringDistance={ringDist} 竟然已开雾 (IsExplored == true)";
                            break;
                        }
                    }

                    // T27: 水域绝对无怪
                    if (store.TerrainLayer[x, y] == TerrainType.WATER)
                    {
                        if (fog.HasMonster)
                        {
                            t27Ok = false;
                            failDetail = $"水域格 ({x}, {y}) 竟然刷新了怪物 {fog.MonsterType}";
                            break;
                        }
                    }

                    // T28: 已有建筑格绝对无怪且 IsBuildingBlocked 为健康的 false
                    if (store.BuildingLayer[x, y].HasBuilding)
                    {
                        if (fog.HasMonster)
                        {
                            t28Ok = false;
                            failDetail = $"建筑格 ({x}, {y}) ({store.BuildingLayer[x, y].BuildingType}) 竟然刷新了怪物 {fog.MonsterType}";
                            break;
                        }
                        if (store.BuildingLayer[x, y].IsBuildingBlocked)
                        {
                            t28Ok = false;
                            failDetail = $"建筑格 ({x}, {y}) ({store.BuildingLayer[x, y].BuildingType}) 的 IsBuildingBlocked 竟然在开图时被设为了 true";
                            break;
                        }
                    }

                    // T29 & T30 & T31: 怪物刷新条件与分段硬阈值校验
                    if (fog.HasMonster)
                    {
                        // 圈层 10 格内绝对不可能刷怪
                        if (ringDist <= 10)
                        {
                            t29Ok = false;
                            failDetail = $"安全区内 ({x}, {y}) 竟然刷新了怪物 {fog.MonsterType}";
                            break;
                        }

                        // 噪声值校验
                        float noise = Mathf.Clamp01(Mathf.PerlinNoise(x * scale + monsterOffset, y * scale + monsterOffset));

                        // 近圈 10~25
                        if (ringDist <= 25)
                        {
                            if (noise <= 0.75f)
                            {
                                t29Ok = false;
                                failDetail = $"近圈格子 ({x}, {y}) 噪声值仅为 {noise} (<= 0.75f) 却强行刷新了怪";
                                break;
                            }
                            if (fog.DangerLevel != 1 || fog.MonsterType != "SLIME")
                            {
                                t29Ok = false;
                                failDetail = $"近圈怪物格 ({x}, {y}) DangerLevel={fog.DangerLevel}, Type={fog.MonsterType} 与 SLIME (Level 1) 不符";
                                break;
                            }
                        }
                        // 中圈 25~50
                        else if (ringDist <= 50)
                        {
                            if (noise <= 0.60f)
                            {
                                t30Ok = false;
                                failDetail = $"中圈格子 ({x}, {y}) 噪声值仅为 {noise} (<= 0.60f) 却强行刷新了怪";
                                break;
                            }
                            bool isMidMonsterOk = (fog.DangerLevel == 2 && fog.MonsterType == "BUG_KNIGHT") || 
                                                  (fog.DangerLevel == 3 && fog.MonsterType == "NULL_GHOST");
                            if (!isMidMonsterOk)
                            {
                                t30Ok = false;
                                failDetail = $"中圈怪物格 ({x}, {y}) DangerLevel={fog.DangerLevel}, Type={fog.MonsterType} 不属于 BUG_KNIGHT(2) 或 NULL_GHOST(3)";
                                break;
                            }
                        }
                        // 外圈 50+
                        else
                        {
                            if (noise <= 0.45f)
                            {
                                t31Ok = false;
                                failDetail = $"外圈格子 ({x}, {y}) 噪声值仅为 {noise} (<= 0.45f) 却强行刷新了怪";
                                break;
                            }
                            bool isFarMonsterOk = (fog.DangerLevel == 3 && fog.MonsterType == "NULL_GHOST") ||
                                                  (fog.DangerLevel == 4 && fog.MonsterType == "DEADLOCK_GOLEM") ||
                                                  (fog.DangerLevel == 5 && fog.MonsterType == "MEMORY_LEAK_TITAN");
                            if (!isFarMonsterOk)
                            {
                                t31Ok = false;
                                failDetail = $"外圈怪物格 ({x}, {y}) DangerLevel={fog.DangerLevel}, Type={fog.MonsterType} 不属于 NULL_GHOST(3)/DEADLOCK_GOLEM(4)/MEMORY_LEAK_TITAN(5)";
                                break;
                            }
                        }
                    }
                }
                if (!t25Ok || !t26Ok || !t27Ok || !t28Ok || !t29Ok || !t30Ok || !t31Ok) break;
            }

            Assert(t25Ok, "T25_MonsterFog_InitialFogOutsideSafeZone",
                "战争迷雾初始覆盖校验成功！大本营安全区（ringDistance > 10）以外的所有迷雾状态均为未探索黑雾",
                $"迷雾未覆盖验证失败！错误细节: {failDetail}");

            Assert(t26Ok, "T26_MonsterFog_SafeZoneRevealed",
                "安全区开雾半径验证成功！所有 ringDistance <= 10 的格子全部被默认探索开雾",
                $"安全区探索未开验证失败！错误细节: {failDetail}");

            Assert(t27Ok, "T27_MonsterFog_WaterHasNoMonster",
                "水域绝对无怪隔离校验成功！全图所有的水域（WATER）瓦片绝对没有刷新任何极客怪物",
                $"水域刷怪限制失败！错误细节: {failDetail}");

            Assert(t28Ok, "T28_MonsterFog_BuildingCellsSafeAndHealthy",
                "世界建筑格无怪与健康状态校验成功！所有大本营、城邦与文明遗迹格子开图均无怪物，且 IsBuildingBlocked 完好无损（false）",
                $"建筑格刷怪与阻挡状态校验失败！错误细节: {failDetail}");

            Assert(t29Ok, "T29_MonsterFog_NearZoneMonsterAndLevel",
                "近圈（10~25）怪物判定校验成功！所有近圈怪物格 DangerLevel 均为 1 且全为极客 SLIME (黏液怪)，且阈值严格 > 0.75f",
                $"近圈刷怪规则验证失败！错误细节: {failDetail}");

            Assert(t30Ok, "T30_MonsterFog_MidZoneMonsterAndLevel",
                "中圈（25~50）怪物确定性校验成功！所有中圈怪物格的 DangerLevel 均严格属于 [2, 3] 区间且全为极客 BUG_KNIGHT 或 NULL_GHOST，阈值严格 > 0.60f",
                $"中圈刷怪规则验证失败！错误细节: {failDetail}");

            Assert(t31Ok, "T31_MonsterFog_FarZoneMonsterAndLevel",
                "外圈（50+）怪物与巨兽BOSS校验成功！所有外圈怪物格 DangerLevel 严格分配在 [3, 4, 5] 并映射极客幽灵/死锁魔像/内存泄漏巨兽，且阈值严格 > 0.45f",
                $"外圈刷怪规则验证失败！错误细节: {failDetail}");

            // T32: 验证种子哈希一致性
            bool t32Ok = true;
            MapDataStore store2 = store.gameObject.AddComponent<MapDataStore>();
            store2.InitArrays();
            TerrainLayerGen.Generate(testSeed, store2);
            ResourceLayerGen.Generate(testSeed, store2);
            BuildingLayerGen.Generate(testSeed, store2);
            MonsterFogLayerGen.Generate(testSeed, store2);

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
                        failDetail = $"种子 114514L 两次生成的 MonsterFogLayer 在格 ({x}, {y}) 产生差异！f1=(Explored={f1.IsExplored}, Monster={f1.HasMonster}, Type={f1.MonsterType}), f2=(Explored={f2.IsExplored}, Monster={f2.HasMonster}, Type={f2.MonsterType})";
                        break;
                    }
                }
                if (!t32Ok) break;
            }
            GameObject.DestroyImmediate(store2);

            Assert(t32Ok, "T32_MonsterFog_SeedConsistency",
                "种子哈希一致性校验成功！使用相同种子生成的迷雾探索、怪物分布、红框危险状态完全一致，100% 确定性重现",
                $"种子生成一致性验证失败！错误细节: {failDetail}");
        }
    }
}
