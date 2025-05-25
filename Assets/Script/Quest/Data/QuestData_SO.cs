using UnityEngine;
using System.Collections.Generic;
using MFarm.Inventory;

/// <summary>
/// 任务数据ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New Quest", menuName = "MFarm/Quest/Quest Data")]
public class QuestData_SO : ScriptableObject
{
    [Header("任务基本信息")]
    public int questID;                  // 任务ID
    public string questName;             // 任务名称
    [TextArea]
    public string questDescription;      // 任务描述
    public QuestType questType;          // 任务类型
    public QuestStatus questStatus;      // 任务状态
    
    [Header("任务目标")]
    public int targetAmount;             // 目标数量
    public ItemDetails targetItem;       // 目标物品（如果是收集任务）
    public string targetNPC;             // 目标NPC（如果是对话任务）
    public string targetScene;           // 目标场景（如果是到达地点任务）
    public Vector2Int targetPosition;    // 目标位置（如果是到达地点任务）
    
    [Header("任务奖励")]
    public int rewardMoney;              // 金钱奖励
    public List<InventoryItem> rewardItems;  // 物品奖励列表
    
    [Header("任务要求")]
    public int requireLevel;             // 需要等级
    public List<QuestData_SO> preQuests; // 前置任务列表
    
    [Header("任务进度")]
    [SerializeField]
    private int currentAmount;           // 当前进度
    
    /// <summary>
    /// 更新任务进度
    /// </summary>
    /// <param name="amount">增加的数量</param>
    public void UpdateQuestProgress(int amount)
    {
        currentAmount += amount;
        
        // 检查是否完成任务
        if (currentAmount >= targetAmount)
        {
            questStatus = QuestStatus.Completed;
        }
    }
    
    /// <summary>
    /// 重置任务状态
    /// </summary>
    public void ResetQuest()
    {
        currentAmount = 0;
        questStatus = QuestStatus.WaitingToStart;
    }
    
    /// <summary>
    /// 获取当前进度
    /// </summary>
    public int GetCurrentAmount()
    {
        return currentAmount;
    }
} 