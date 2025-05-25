using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC耕种管理器
/// 负责协调和管理多个NPC的耕种任务，避免重复劳动，优化任务分配
/// </summary>
public class NPCFarmingManager : Singleton<NPCFarmingManager>
{
    [Header("管理设置")]
    public List<NPCFarmingTask> farmingNPCs = new List<NPCFarmingTask>(); // 可进行耕种的NPC列表
    public float taskUpdateInterval = 5f; // 任务更新间隔（秒）- 增加间隔减少CPU负担
    public int maxSimultaneousFarmers = 3; // 同时进行耕种的最大NPC数量
    
    [Header("搜索优化设置")]
    public float defaultSearchRadius = 8f; // 默认搜索半径（减小提高效率）
    public float maxSearchRadius = 15f; // 最大搜索半径
    public bool enableSmartSearch = true; // 启用智能搜索优化
    
    [Header("种子配置")]
    public List<SeedConfig> seedConfigs = new List<SeedConfig>(); // 种子配置
    
    /*
    示例配置（在Unity Inspector中设置）：
    
    种子ID: 1002, 种子名称: "马铃薯种子", 优先级: 8, 偏好季节: 春天
    种子ID: 1003, 种子名称: "甜菜根种子", 优先级: 7, 偏好季节: 春天  
    种子ID: 1004, 种子名称: "白萝卜种子", 优先级: 6, 偏好季节: 春天
    种子ID: 1005, 种子名称: "辣椒种子", 优先级: 9, 偏好季节: 夏天
    种子ID: 1006, 种子名称: "蓝莓种子", 优先级: 7, 偏好季节: 冬天
    种子ID: 1008, 种子名称: "葡萄种子", 优先级: 8, 偏好季节: 夏天
    种子ID: 1010, 种子名称: "番茄种子", 优先级: 9, 偏好季节: 夏天
    种子ID: 1011, 种子名称: "西瓜种子", 优先级: 10, 偏好季节: 夏天
    种子ID: 1009, 种子名称: "南瓜种子", 优先级: 8, 偏好季节: 秋天
    */
    
    private HashSet<Vector2Int> occupiedTiles = new HashSet<Vector2Int>(); // 已被占用的地块
    private Dictionary<NPCFarmingTask, Vector2Int> npcAssignments = new Dictionary<NPCFarmingTask, Vector2Int>(); // NPC任务分配
    private Season currentSeason = Season.春天; // 当前季节，默认为春天
    
    /// <summary>
    /// 种子配置类
    /// </summary>
    [System.Serializable]
    public class SeedConfig
    {
        public int seedID;
        public string seedName;
        public int priority; // 种植优先级 (1-10，数字越大优先级越高)
        public Season preferredSeason; // 偏好季节
    }
    
    private void Start()
    {
        // 自动发现场景中的NPCFarmingTask组件
        AutoDiscoverFarmingNPCs();
        
        // 开始任务管理协程
        StartCoroutine(TaskManagementRoutine());
    }
    
    private void OnEnable()
    {
        EventHandler.GameDayEvent += OnGameDayEvent;
    }
    
    private void OnDisable()
    {
        EventHandler.GameDayEvent -= OnGameDayEvent;
    }
    
    /// <summary>
    /// 游戏日期变化事件处理
    /// </summary>
    /// <param name="day">当前天数</param>
    /// <param name="season">当前季节</param>
    private void OnGameDayEvent(int day, Season season)
    {
        // 更新当前季节
        currentSeason = season;
        
        // 每天重新评估种子优先级
        UpdateSeasonalPreferences(season);
        
        // 清除已完成的地块占用
        ClearCompletedTiles();
        
        Debug.Log($"游戏新的一天 - 天数: {day}, 季节: {season}");
    }
    
    /// <summary>
    /// 自动发现场景中的耕种NPC
    /// </summary>
    private void AutoDiscoverFarmingNPCs()
    {
        NPCFarmingTask[] foundNPCs = FindObjectsOfType<NPCFarmingTask>();
        
        foreach (var npc in foundNPCs)
        {
            if (!farmingNPCs.Contains(npc))
            {
                farmingNPCs.Add(npc);
                Debug.Log($"发现耕种NPC: {npc.gameObject.name}");
            }
        }
        
        Debug.Log($"总共发现 {farmingNPCs.Count} 个耕种NPC");
    }
    
    /// <summary>
    /// 任务管理协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator TaskManagementRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(taskUpdateInterval);
            
            // 管理NPC任务
            ManageNPCTasks();
        }
    }
    
    /// <summary>
    /// 管理NPC任务分配 - 修复版本
    /// </summary>
    private void ManageNPCTasks()
    {
        // 统计当前活跃的耕种NPC数量
        int activeFarmers = 0;
        int restingNPCs = 0;
        List<NPCFarmingTask> idleNPCs = new List<NPCFarmingTask>();
        
        foreach (var npc in farmingNPCs)
        {
            if (npc == null) continue;
            
            var state = npc.GetCurrentState();
            
            // 只在搜索目标状态时考虑分配新任务
            if (state == NPCFarmingTask.TaskState.SearchingTarget)
            {
                idleNPCs.Add(npc);
            }
            else if (state == NPCFarmingTask.TaskState.MovingToTarget ||
                     state == NPCFarmingTask.TaskState.Digging ||
                     state == NPCFarmingTask.TaskState.Planting ||
                     state == NPCFarmingTask.TaskState.Watering)
            {
                activeFarmers++;
            }
            else if (state == NPCFarmingTask.TaskState.Resting)
            {
                restingNPCs++;
            }
            // Failed和Completed状态的NPC不参与计数，让它们自然恢复
        }
        
        // 如果活跃农民数量未达到上限，分配新任务
        int availableSlots = maxSimultaneousFarmers - activeFarmers;
        
        for (int i = 0; i < Mathf.Min(availableSlots, idleNPCs.Count); i++)
        {
            AssignOptimalTask(idleNPCs[i]);
        }
        
        // 调试信息（减少频率）
        if (Time.time % 10f < taskUpdateInterval) // 每10秒输出一次
        {
            Debug.Log($"NPC状态统计 - 活跃农民: {activeFarmers}, 空闲NPC: {idleNPCs.Count}, 休息NPC: {restingNPCs}, 可用位置: {availableSlots}");
        }
    }
    
    /// <summary>
    /// 为NPC分配最优任务
    /// </summary>
    /// <param name="npc">目标NPC</param>
    private void AssignOptimalTask(NPCFarmingTask npc)
    {
        // 获取当前季节最适合的种子
        int optimalSeedID = GetOptimalSeedForSeason();
        
        if (optimalSeedID != -1)
        {
            // 设置NPC的偏好种子
            npc.SetPreferredSeed(optimalSeedID);
            
            // 根据当前设置优化搜索半径
            if (enableSmartSearch)
            {
                // 动态调整搜索半径：如果活跃农民多，减小搜索半径避免冲突
                float adjustedRadius = Mathf.Lerp(defaultSearchRadius, maxSearchRadius,
                     1f - (float)GetActiveFarmersCount() / maxSimultaneousFarmers);
                npc.SetSearchRadius(adjustedRadius);
            }
            else
            {
                npc.SetSearchRadius(defaultSearchRadius);
            }
            
            // 开始耕种任务
            npc.StartFarmingTask();
            
            Debug.Log($"为 {npc.gameObject.name} 分配耕种任务，种子ID: {optimalSeedID}，搜索半径: {(enableSmartSearch ? "智能调整" : defaultSearchRadius.ToString())}");
        }
        else
        {
            Debug.Log($"当前季节没有合适的种子可种植");
        }
    }
    
    /// <summary>
    /// 获取当前季节最适合的种子
    /// </summary>
    /// <returns>种子ID，如果没有合适的种子返回-1</returns>
    private int GetOptimalSeedForSeason()
    {
        if (seedConfigs.Count == 0) return 1001; // 默认种子ID
        
        int bestSeedID = -1;
        int highestPriority = -1;
        
        foreach (var config in seedConfigs)
        {
            // 检查是否适合当前季节
            if (config.preferredSeason == currentSeason || config.preferredSeason == Season.春天) // 春天作为通用季节
            {
                if (config.priority > highestPriority)
                {
                    highestPriority = config.priority;
                    bestSeedID = config.seedID;
                }
            }
        }
        
        return bestSeedID != -1 ? bestSeedID : seedConfigs[0].seedID; // 如果没有找到合适的，返回第一个
    }
    
    /// <summary>
    /// 更新季节性偏好
    /// </summary>
    /// <param name="currentSeason">当前季节</param>
    private void UpdateSeasonalPreferences(Season currentSeason)
    {
        Debug.Log($"更新季节偏好，当前季节: {currentSeason}");
        
        // 为所有NPC更新种子偏好
        foreach (var npc in farmingNPCs)
        {
            if (npc != null)
            {
                int optimalSeed = GetOptimalSeedForSeason();
                npc.SetPreferredSeed(optimalSeed);
            }
        }
    }
    
    /// <summary>
    /// 清除已完成的地块占用
    /// </summary>
    private void ClearCompletedTiles()
    {
        occupiedTiles.Clear();
        npcAssignments.Clear();
        Debug.Log("清除地块占用记录");
    }
    
    /// <summary>
    /// 注册地块占用
    /// </summary>
    /// <param name="npc">NPC实例</param>
    /// <param name="tilePosition">地块位置</param>
    public void RegisterTileOccupation(NPCFarmingTask npc, Vector2Int tilePosition)
    {
        occupiedTiles.Add(tilePosition);
        npcAssignments[npc] = tilePosition;
        
        Debug.Log($"{npc.gameObject.name} 占用地块 {tilePosition}");
    }
    
    /// <summary>
    /// 释放地块占用
    /// </summary>
    /// <param name="npc">NPC实例</param>
    public void ReleaseTileOccupation(NPCFarmingTask npc)
    {
        if (npcAssignments.ContainsKey(npc))
        {
            Vector2Int tilePosition = npcAssignments[npc];
            occupiedTiles.Remove(tilePosition);
            npcAssignments.Remove(npc);
            
            Debug.Log($"{npc.gameObject.name} 释放地块 {tilePosition}");
        }
    }
    
    /// <summary>
    /// 检查地块是否被占用
    /// </summary>
    /// <param name="tilePosition">地块位置</param>
    /// <returns>是否被占用</returns>
    public bool IsTileOccupied(Vector2Int tilePosition)
    {
        return occupiedTiles.Contains(tilePosition);
    }
    
    /// <summary>
    /// 添加耕种NPC
    /// </summary>
    /// <param name="npc">NPC实例</param>
    public void AddFarmingNPC(NPCFarmingTask npc)
    {
        if (!farmingNPCs.Contains(npc))
        {
            farmingNPCs.Add(npc);
            Debug.Log($"添加耕种NPC: {npc.gameObject.name}");
        }
    }
    
    /// <summary>
    /// 移除耕种NPC
    /// </summary>
    /// <param name="npc">NPC实例</param>
    public void RemoveFarmingNPC(NPCFarmingTask npc)
    {
        if (farmingNPCs.Contains(npc))
        {
            farmingNPCs.Remove(npc);
            ReleaseTileOccupation(npc);
            Debug.Log($"移除耕种NPC: {npc.gameObject.name}");
        }
    }
    
    /// <summary>
    /// 设置最大同时耕种者数量
    /// </summary>
    /// <param name="maxCount">最大数量</param>
    public void SetMaxSimultaneousFarmers(int maxCount)
    {
        maxSimultaneousFarmers = Mathf.Max(1, maxCount);
        Debug.Log($"设置最大同时耕种者数量: {maxSimultaneousFarmers}");
    }
    
    /// <summary>
    /// 获取当前活跃的耕种NPC数量
    /// </summary>
    /// <returns>活跃NPC数量</returns>
    public int GetActiveFarmersCount()
    {
        int count = 0;
        foreach (var npc in farmingNPCs)
        {
            if (npc != null)
            {
                var state = npc.GetCurrentState();
                if (state == NPCFarmingTask.TaskState.MovingToTarget ||
                    state == NPCFarmingTask.TaskState.Digging ||
                    state == NPCFarmingTask.TaskState.Planting ||
                    state == NPCFarmingTask.TaskState.Watering)
                {
                    count++;
                }
            }
        }
        return count;
    }
    
    /// <summary>
    /// 停止所有NPC的耕种任务
    /// </summary>
    public void StopAllFarmingTasks()
    {
        foreach (var npc in farmingNPCs)
        {
            if (npc != null)
            {
                npc.StopCurrentTask();
            }
        }
        Debug.Log("停止所有NPC耕种任务");
    }
    
    /// <summary>
    /// 启用或禁用所有NPC的每日耕种
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void SetAllNPCDailyFarmingEnabled(bool enable)
    {
        foreach (var npc in farmingNPCs)
        {
            if (npc != null)
            {
                npc.SetDailyFarmingEnabled(enable);
            }
        }
        Debug.Log($"设置所有NPC每日耕种: {(enable ? "启用" : "禁用")}");
    }
} 