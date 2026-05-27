using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPG.Map
{
    /// <summary>
    /// 地图渲染与相机视口裁剪优化组件
    /// 职责：读取 MapDataStore 的数据，渲染地形、资源、建筑及迷雾四个 Tilemap。
    /// 支持基于主相机的高性能增量视口裁剪以提升渲染效率，并使用计时器进行节流以避免每帧计算开销。
    /// 实现说明书 §6.5 & §8.2 节。
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        [Header("Tilemap 引用")]
        [SerializeField] private Tilemap _terrainTilemap;
        [SerializeField] private Tilemap _resourceTilemap;
        [SerializeField] private Tilemap _buildingTilemap;
        [SerializeField] private Tilemap _monsterFogTilemap;

        [System.Serializable]
        public struct TerrainTileMapping
        {
            public TerrainType terrainType;
            public TileBase tile;
        }

        [System.Serializable]
        public struct BuildingTileMapping
        {
            public string buildingType;
            public TileBase tile;
        }

        [Header("地形瓦片映射")]
        [SerializeField] private TerrainTileMapping[] _terrainTiles;

        [Header("资源瓦片配置")]
        [SerializeField] private TileBase _forestTile;
        [SerializeField] private TileBase _stoneTile;
        [SerializeField] private TileBase _ironTile;
        [SerializeField] private TileBase _crystalTile;
        [SerializeField] private TileBase _herbsTile;

        [Header("建筑瓦片配置")]
        [SerializeField] private BuildingTileMapping[] _buildingTiles;

        [Header("战争迷雾与怪物瓦片")]
        [SerializeField] private TileBase _blackFogTile;
        [SerializeField] private TileBase _slimeMonsterTile;
        [SerializeField] private TileBase _bugKnightMonsterTile;
        [SerializeField] private TileBase _nullGhostMonsterTile;
        [SerializeField] private TileBase _deadlockGolemMonsterTile;
        [SerializeField] private TileBase _memoryLeakTitanMonsterTile;
        [SerializeField] private TileBase _defaultWarningTile;

        [Header("视口裁剪优化 (Culling)")]
        [SerializeField] private bool _enableCulling = false;
        [SerializeField] private float _cullingUpdateInterval = 0.2f;

        private float _cullingTimer = 0f;

        // 缓存上一帧的视口裁剪网格范围边界，实现高性能的增量擦除与重绘
        private int _lastStartX = -1;
        private int _lastEndX = -1;
        private int _lastStartY = -1;
        private int _lastEndY = -1;

        private void Update()
        {
            // 问题一：使用 Update 计时器累加，实现节流视口裁剪调用
            if (_enableCulling)
            {
                _cullingTimer += Time.deltaTime;
                if (_cullingTimer >= _cullingUpdateInterval)
                {
                    _cullingTimer = 0f;
                    UpdateCulling();
                }
            }
        }

        /// <summary>
        /// 全量重绘地图。在生成地图完毕或载入存档时被调用。
        /// </summary>
        public void RenderAllLayers()
        {
            Debug.Log("[MapRenderer] 执行 RenderAllLayers()，开始全图渲染重绘...");

            // 1. 清空所有 Tilemap 上的现有瓦片
            ClearAllTilemaps();

            // 2. 重置上一帧裁剪状态，强迫下一次裁剪重算整个可视区
            _lastStartX = -1;
            _lastEndX = -1;
            _lastStartY = -1;
            _lastEndY = -1;

            // 3. 如果启用了 Culling，仅由 Culling 决定渲染哪些格子，否则进行全图 200x200 绘制
            if (_enableCulling)
            {
                UpdateCulling();
            }
            else
            {
                MapDataStore store = MapDataStore.Instance;
                if (store == null) return;

                for (int x = 0; x < MapDataStore.MAP_SIZE; x++)
                {
                    for (int y = 0; y < MapDataStore.MAP_SIZE; y++)
                    {
                        RenderSingleCell(x, y, store);
                    }
                }
            }
        }

        /// <summary>
        /// 动态视口裁剪逻辑。仅渲染相机视口加外扩 15 格内的瓦片。
        /// 通过上一帧缓存范围边界，实现高效的增量渲染，零帧率毛刺。
        /// </summary>
        public void UpdateCulling()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                // 解决新增问题：警告提示更加明确，极大缩短生命周期先后顺序带来的调试排查成本
                Debug.LogWarning("[MapRenderer] Camera.main 为空，若在生成阶段调用请改用全量渲染模式（_enableCulling 设为 false）。");
                return;
            }

            MapDataStore store = MapDataStore.Instance;
            if (store == null) return;

            // 1. 获取主相机在世界空间中的可视范围（正交模式）
            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            Vector3 camPos = cam.transform.position;

            float minX = camPos.x - width / 2f;
            float maxX = camPos.x + width / 2f;
            float minY = camPos.y - height / 2f;
            float maxY = camPos.y + height / 2f;

            // 2. 将世界范围转换为网格坐标（使用 TerrainTilemap 变换）
            Tilemap refTilemap = _terrainTilemap != null ? _terrainTilemap : _monsterFogTilemap;
            if (refTilemap == null) return;

            Vector3Int minCell = refTilemap.WorldToCell(new Vector3(minX, minY, 0));
            Vector3Int maxCell = refTilemap.WorldToCell(new Vector3(maxX, maxY, 0));

            // 3. 外扩 15 格安全余量并限制在 [0, MAP_SIZE - 1] 边界内
            int buffer = 15;
            int startX = Mathf.Clamp(minCell.x - buffer, 0, MapDataStore.MAP_SIZE - 1);
            int endX = Mathf.Clamp(maxCell.x + buffer, 0, MapDataStore.MAP_SIZE - 1);
            int startY = Mathf.Clamp(minCell.y - buffer, 0, MapDataStore.MAP_SIZE - 1);
            int endY = Mathf.Clamp(maxCell.y + buffer, 0, MapDataStore.MAP_SIZE - 1);

            // 问题二：高性能增量裁剪优化算法。如果新范围与上一帧范围完全相同，则不进行任何计算与渲染
            if (startX == _lastStartX && endX == _lastEndX && startY == _lastStartY && endY == _lastEndY)
            {
                return;
            }

            // A. 擦除：仅遍历上一帧范围，清空不再处于新范围内的出界格子
            if (_lastStartX != -1) // 非首帧渲染时
            {
                for (int x = _lastStartX; x <= _lastEndX; x++)
                {
                    for (int y = _lastStartY; y <= _lastEndY; y++)
                    {
                        bool outsideNewBounds = (x < startX || x > endX || y < startY || y > endY);
                        if (outsideNewBounds)
                        {
                            ClearSingleCell(x, y);
                        }
                    }
                }
            }

            // B. 渲染：仅遍历新范围，对原本不在上一帧范围内的格子调用渲染（若为首帧，则全部渲染）
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    bool insideOldBounds = (_lastStartX != -1) && (x >= _lastStartX && x <= _lastEndX && y >= _lastStartY && y <= _lastEndY);
                    if (!insideOldBounds)
                    {
                        RenderSingleCell(x, y, store);
                    }
                }
            }

            // C. 同步更新上一帧范围缓存边界
            _lastStartX = startX;
            _lastEndX = endX;
            _lastStartY = startY;
            _lastEndY = endY;
        }

        /// <summary>
        /// 清空所有 Tilemap 现有瓦片
        /// </summary>
        private void ClearAllTilemaps()
        {
            if (_terrainTilemap != null) _terrainTilemap.ClearAllTiles();
            if (_resourceTilemap != null) _resourceTilemap.ClearAllTiles();
            if (_buildingTilemap != null) _buildingTilemap.ClearAllTiles();
            if (_monsterFogTilemap != null) _monsterFogTilemap.ClearAllTiles();
        }

        /// <summary>
        /// 渲染单格的四层瓦片。如果 Tile 配置为空，则安全跳过绘制不报异常。
        /// </summary>
        private void RenderSingleCell(int x, int y, MapDataStore store)
        {
            Vector3Int pos = new Vector3Int(x, y, 0);

            // Layer 1: 地形层绘制
            if (_terrainTilemap != null)
            {
                TerrainType type = store.TerrainLayer[x, y];
                TileBase tile = GetTerrainTile(type);
                if (tile != null)
                {
                    _terrainTilemap.SetTile(pos, tile);
                }
            }

            // Layer 2: 资源层绘制
            if (_resourceTilemap != null)
            {
                ResourceData resource = store.ResourceLayer[x, y];
                TileBase tile = GetResourceTile(resource);
                _resourceTilemap.SetTile(pos, tile); // 允许为 null (即清空资源)
            }

            // Layer 3: 建筑层绘制
            if (_buildingTilemap != null)
            {
                BuildingData building = store.BuildingLayer[x, y];
                TileBase tile = GetBuildingTile(building);
                _buildingTilemap.SetTile(pos, tile); // 允许为 null (即无建筑)
            }

            // Layer 4: 迷雾与怪物层绘制 (严格满足说明书 §6.5 渲染表现三分支规则)
            if (_monsterFogTilemap != null)
            {
                MonsterFogData fog = store.MonsterFogLayer[x, y];
                TileBase tile = GetMonsterFogTile(fog);
                _monsterFogTilemap.SetTile(pos, tile); // 已探索且无怪时返回 null 自动抹除黑雾/警戒色块
            }
        }

        /// <summary>
        /// 清空单格的四层瓦片（Culling 超出范围时使用）
        /// </summary>
        private void ClearSingleCell(int x, int y)
        {
            Vector3Int pos = new Vector3Int(x, y, 0);
            if (_terrainTilemap != null) _terrainTilemap.SetTile(pos, null);
            if (_resourceTilemap != null) _resourceTilemap.SetTile(pos, null);
            if (_buildingTilemap != null) _buildingTilemap.SetTile(pos, null);
            if (_monsterFogTilemap != null) _monsterFogTilemap.SetTile(pos, null);
        }

        #region 瓦片获取辅助函数

        private TileBase GetTerrainTile(TerrainType type)
        {
            if (_terrainTiles != null)
            {
                for (int i = 0; i < _terrainTiles.Length; i++)
                {
                    if (_terrainTiles[i].terrainType == type)
                    {
                        return _terrainTiles[i].tile;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取资源层对应瓦片。
        /// 注释/优先级说明（问题四）：资源生成层逻辑中，矿脉、森林和草药在地势分布上是完全互斥的（山地生矿脉，草原生森林，沼泽生草药）。
        /// 这里制定的优先级“矿脉优先 > 森林优先 > 草药优先”是为了保证在特殊玩法或未来混合多资源单元格子时的安全渲染表现。
        /// </summary>
        private TileBase GetResourceTile(ResourceData data)
        {
            if (data.HasMineralVein)
            {
                switch (data.MineralType)
                {
                    case "STONE": return _stoneTile;
                    case "IRON": return _ironTile;
                    case "CRYSTAL": return _crystalTile;
                    default: return null;
                }
            }
            if (data.HasForest)
            {
                return _forestTile;
            }
            if (data.HasHerbs)
            {
                return _herbsTile;
            }
            return null;
        }

        private TileBase GetBuildingTile(BuildingData data)
        {
            if (!data.HasBuilding) return null;

            // 问题三：完全统一走 Inspector 中唯一的建筑映射表配置，防止配置歧义与 Fallback 重合混乱
            if (_buildingTiles != null)
            {
                for (int i = 0; i < _buildingTiles.Length; i++)
                {
                    if (_buildingTiles[i].buildingType == data.BuildingType)
                    {
                        return _buildingTiles[i].tile;
                    }
                }
            }

            return null;
        }

        private TileBase GetMonsterFogTile(MonsterFogData data)
        {
            // 解决问题二：三分支渲染机制
            // 1. 分支一：若未探索：在该格渲染全黑的迷雾瓦片
            if (!data.IsExplored)
            {
                return _blackFogTile;
            }
            
            // 2. 分支二：若已探索且有怪：渲染怪物对应的高清警戒瓦片，缺省时使用通用警戒色块
            if (data.HasMonster)
            {
                switch (data.MonsterType)
                {
                    case "SLIME":
                        return _slimeMonsterTile != null ? _slimeMonsterTile : _defaultWarningTile;
                    case "BUG_KNIGHT":
                        return _bugKnightMonsterTile != null ? _bugKnightMonsterTile : _defaultWarningTile;
                    case "NULL_GHOST":
                        return _nullGhostMonsterTile != null ? _nullGhostMonsterTile : _defaultWarningTile;
                    case "DEADLOCK_GOLEM":
                        return _deadlockGolemMonsterTile != null ? _deadlockGolemMonsterTile : _defaultWarningTile;
                    case "MEMORY_LEAK_TITAN":
                        return _memoryLeakTitanMonsterTile != null ? _memoryLeakTitanMonsterTile : _defaultWarningTile;
                    default:
                        return _defaultWarningTile;
                }
            }

            // 3. 分支三：若已探索且无怪物：返回 null 以执行 SetTile(pos, null) 完美清除本格迷雾
            return null;
        }

        #endregion
    }
}
