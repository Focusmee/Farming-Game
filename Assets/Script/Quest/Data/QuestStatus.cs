using UnityEngine;

/// <summary>
/// 任务状态枚举
/// </summary>
public enum QuestStatus
{
    /// <summary>
    /// 等待开始
    /// </summary>
    WaitingToStart,
    
    /// <summary>
    /// 进行中
    /// </summary>
    InProgress,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Completed,
    
    /// <summary>
    /// 失败
    /// </summary>
    Failed
} 