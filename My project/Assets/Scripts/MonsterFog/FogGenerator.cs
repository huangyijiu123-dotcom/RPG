using UnityEngine;
using RPG.Map;

namespace RPG.Map.MonsterFog
{
    /// <summary>
    /// 战争迷雾开雾子模块
    /// 职责：初始全图覆盖战争迷雾，并在大本营安全区（ringDistance <= 10）内默认开雾可见。
    /// 实现说明书 §6.1。
    /// </summary>
    public static class FogGenerator
    {
        // ── 迷雾参数 ──────────────────────────────────────────────────
        public const int SAFE_ZONE_EXPLORE_RADIUS = 10; // 大本营周围安全区开雾半径（切比雪夫距离）

        /// <summary>
        /// 生成全图的初始迷雾状态。
        /// </summary>
        /// <param name="monsterFogLayer">第四层待写入的迷雾怪物矩阵</param>
        public static void GenerateFog(MonsterFogData[,] monsterFogLayer)
        {
            if (monsterFogLayer == null)
            {
                Debug.LogError("[FogGenerator] monsterFogLayer 为 null，迷雾生成中止。");
                return;
            }

            int size = MapDataStore.MAP_SIZE;
            int cx   = MapDataStore.CENTER;
            int cy   = MapDataStore.CENTER;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 计算当前格子与地图中心大本营的切比雪夫距离
                    int ringDistance = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy));

                    // 1. 大本营安全区 ringDistance <= 10 的格子默认可见（IsExplored = true）
                    if (ringDistance <= SAFE_ZONE_EXPLORE_RADIUS)
                    {
                        monsterFogLayer[x, y].IsExplored = true;
                    }
                    // 2. 其余格子初始全被黑雾笼罩（IsExplored = false）
                    else
                    {
                        monsterFogLayer[x, y].IsExplored = false;
                    }
                }
            }

            Debug.Log("[FogGenerator] 战争迷雾初始状态生成完成。");
        }
    }
}
