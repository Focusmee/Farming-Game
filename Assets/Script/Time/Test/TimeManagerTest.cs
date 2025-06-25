using UnityEngine;

/// <summary>
/// 时间管理器测试脚本
/// 用于验证日期进位修复效果
/// </summary>
public class TimeManagerTest : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool enableTestOnStart = false;
    [SerializeField] private bool showTimeInGUI = true;
    
    private void Start()
    {
        if (enableTestOnStart)
        {
            TestDateCarryLogic();
        }
    }
    
    private void OnGUI()
    {
        if (!showTimeInGUI || TimeManager.Instance == null) return;
        
        // 在屏幕左上角显示当前游戏时间
        GUI.Box(new Rect(10, 10, 400, 80), "");
        GUI.Label(new Rect(20, 20, 380, 20), "当前游戏时间:");
        GUI.Label(new Rect(20, 40, 380, 20), TimeManager.Instance.GetFormattedGameTime());
        
        // 添加测试按钮
        if (GUI.Button(new Rect(20, 65, 100, 20), "测试日期进位"))
        {
            TestDateCarryLogic();
        }
        
        if (GUI.Button(new Rect(130, 65, 100, 20), "跳转到月末"))
        {
            JumpToEndOfMonth();
        }
        
        if (GUI.Button(new Rect(240, 65, 100, 20), "跳转到年末"))
        {
            JumpToEndOfYear();
        }
    }
    
    /// <summary>
    /// 测试日期进位逻辑
    /// </summary>
    [ContextMenu("测试日期进位逻辑")]
    public void TestDateCarryLogic()
    {
        Debug.Log("=== 开始测试日期进位逻辑 ===");
        
        // 测试各种月份的天数
        int[] testYears = { 2024, 2025, 2100, 2000 }; // 包含闰年和非闰年
        
        foreach (int year in testYears)
        {
            bool isLeap = System.DateTime.IsLeapYear(year);
            Debug.Log($"测试年份: {year} (闰年: {isLeap})");
            
            for (int month = 1; month <= 12; month++)
            {
                int days = System.DateTime.DaysInMonth(year, month);
                string monthName = GetMonthName(month);
                Debug.Log($"  {year}年{monthName}({month}月): {days}天");
                
                // 特别标注2月
                if (month == 2)
                {
                    Debug.Log($"    -> 2月天数验证: {(isLeap ? "闰年29天" : "平年28天")}");
                }
            }
        }
        
        Debug.Log("=== 日期进位逻辑测试完成 ===");
    }
    
    /// <summary>
    /// 跳转到当前月份的最后一天，用于测试月份进位
    /// </summary>
    [ContextMenu("跳转到月末")]
    public void JumpToEndOfMonth()
    {
        if (TimeManager.Instance == null) return;
        
        // 通过反射获取私有字段（仅用于测试）
        var timeManagerType = typeof(TimeManager);
        var gameYearField = timeManagerType.GetField("gameYear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var gameMonthField = timeManagerType.GetField("gameMonth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var gameDayField = timeManagerType.GetField("gameDay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (gameYearField != null && gameMonthField != null && gameDayField != null)
        {
            int currentYear = (int)gameYearField.GetValue(TimeManager.Instance);
            int currentMonth = (int)gameMonthField.GetValue(TimeManager.Instance);
            
            int daysInMonth = System.DateTime.DaysInMonth(currentYear, currentMonth);
            gameDayField.SetValue(TimeManager.Instance, daysInMonth - 1); // 设置为月末前一天
            
            Debug.Log($"已跳转到 {currentYear}年{currentMonth}月{daysInMonth - 1}日，下一天将触发月份进位");
        }
    }
    
    /// <summary>
    /// 跳转到年末，用于测试年份进位
    /// </summary>
    [ContextMenu("跳转到年末")]
    public void JumpToEndOfYear()
    {
        if (TimeManager.Instance == null) return;
        
        // 通过反射获取私有字段（仅用于测试）
        var timeManagerType = typeof(TimeManager);
        var gameYearField = timeManagerType.GetField("gameYear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var gameMonthField = timeManagerType.GetField("gameMonth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var gameDayField = timeManagerType.GetField("gameDay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (gameYearField != null && gameMonthField != null && gameDayField != null)
        {
            int currentYear = (int)gameYearField.GetValue(TimeManager.Instance);
            
            gameMonthField.SetValue(TimeManager.Instance, 12);
            gameDayField.SetValue(TimeManager.Instance, 30); // 设置为12月30日
            
            Debug.Log($"已跳转到 {currentYear}年12月30日，下一天将触发年份进位");
        }
    }
    
    /// <summary>
    /// 获取月份中文名称
    /// </summary>
    private string GetMonthName(int month)
    {
        return month switch
        {
            1 => "一月", 2 => "二月", 3 => "三月", 4 => "四月",
            5 => "五月", 6 => "六月", 7 => "七月", 8 => "八月",
            9 => "九月", 10 => "十月", 11 => "十一月", 12 => "十二月",
            _ => "未知月份"
        };
    }
} 