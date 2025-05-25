using UnityEngine;
using System.Collections.Generic;
using MFarm.Save;

/// <summary>
/// 任务管理器
/// </summary>
public class QuestManager : Singleton<QuestManager>, ISaveable
{
    [Header("任务数据")]
    public List<QuestData_SO> questDataList;              // 所有任务列表
    private Dictionary<int, QuestData_SO> questDict;      // 任务字典，用于快速查找
    
    public string GUID => GetComponent<DataGUID>().guid;
    
    private void Awake()
    {
        // 初始化任务字典
        questDict = new Dictionary<int, QuestData_SO>();
        foreach (var quest in questDataList)
        {
            questDict.Add(quest.questID, quest);
        }
    }
    
    private void OnEnable()
    {
        EventHandler.StartNewGameEvent += OnStartNewGameEvent;
        EventHandler.EndGameEvent += OnEndGameEvent;
        ISaveable saveable = this;
        saveable.RegisterSaveable();
    }
    
    private void OnDisable()
    {
        EventHandler.StartNewGameEvent -= OnStartNewGameEvent;
        EventHandler.EndGameEvent -= OnEndGameEvent;
    }
    
    /// <summary>
    /// 开始新游戏时重置所有任务
    /// </summary>
    private void OnStartNewGameEvent(int index)
    {
        foreach (var quest in questDataList)
        {
            quest.ResetQuest();
        }
    }
    
    private void OnEndGameEvent()
    {
        // 保存任务状态
    }
    
    /// <summary>
    /// 接受任务
    /// </summary>
    public void AcceptQuest(int questID)
    {
        if (questDict.ContainsKey(questID))
        {
            QuestData_SO quest = questDict[questID];
            if (CanAcceptQuest(quest))
            {
                quest.questStatus = QuestStatus.InProgress;
                EventHandler.CallQuestStateChangeEvent(quest);
            }
        }
    }
    
    /// <summary>
    /// 检查是否可以接受任务
    /// </summary>
    private bool CanAcceptQuest(QuestData_SO quest)
    {
        // 检查前置任务是否完成
        if (quest.preQuests != null)
        {
            foreach (var preQuest in quest.preQuests)
            {
                if (preQuest.questStatus != QuestStatus.Completed)
                    return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// 更新任务进度
    /// </summary>
    public void UpdateQuestProgress(int questID, int amount)
    {
        if (questDict.ContainsKey(questID))
        {
            QuestData_SO quest = questDict[questID];
            if (quest.questStatus == QuestStatus.InProgress)
            {
                quest.UpdateQuestProgress(amount);
                EventHandler.CallQuestProgressEvent(quest, quest.GetCurrentAmount());
                
                // 如果任务完成，触发状态改变事件
                if (quest.questStatus == QuestStatus.Completed)
                {
                    EventHandler.CallQuestStateChangeEvent(quest);
                }
            }
        }
    }
    
    /// <summary>
    /// 完成任务，发放奖励
    /// </summary>
    public void CompleteQuest(int questID)
    {
        if (questDict.ContainsKey(questID))
        {
            QuestData_SO quest = questDict[questID];
            if (quest.questStatus == QuestStatus.Completed)
            {
                // 发放奖励
                if (quest.rewardMoney > 0)
                {
                    // TODO: 增加金钱
                }
                
                if (quest.rewardItems != null && quest.rewardItems.Count > 0)
                {
                    foreach (var item in quest.rewardItems)
                    {
                        // TODO: 添加物品到背包
                    }
                }
                
                EventHandler.CallQuestStateChangeEvent(quest);
            }
        }
    }
    
    public GameSaveData GenerateSaveData()
    {
        GameSaveData saveData = new GameSaveData();
        saveData.questStatusDict = new Dictionary<string, QuestStatus>();
        saveData.questProgressDict = new Dictionary<string, int>();
        
        foreach (var quest in questDataList)
        {
            saveData.questStatusDict.Add(quest.questID.ToString(), quest.questStatus);
            saveData.questProgressDict.Add(quest.questID.ToString(), quest.GetCurrentAmount());
        }
        
        return saveData;
    }
    
    public void RestoreData(GameSaveData saveData)
    {
        if (saveData.questStatusDict != null)
        {
            foreach (var pair in saveData.questStatusDict)
            {
                int questID = int.Parse(pair.Key);
                if (questDict.ContainsKey(questID))
                {
                    questDict[questID].questStatus = pair.Value;
                }
            }
        }
        
        if (saveData.questProgressDict != null)
        {
            foreach (var pair in saveData.questProgressDict)
            {
                int questID = int.Parse(pair.Key);
                if (questDict.ContainsKey(questID))
                {
                    questDict[questID].UpdateQuestProgress(pair.Value);
                }
            }
        }
    }
} 