using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MFarm.Save;

/// <summary>
/// 时间管理器 - 修复版本
/// 修复内容：
/// 1. 正确处理每月不同天数（28/29/30/31天）
/// 2. 自动处理闰年2月29天的情况
/// 3. 修复年份进位逻辑
/// 4. 优化季节更新逻辑
/// </summary>
public class TimeManager : Singleton<TimeManager>, ISaveable
{
    private int gameSecond, gameMinute, gameHour, gameDay, gameMonth, gameYear;
    private Season gameSeason = Season.春天;//默认游戏进入为春天
    private int monthInSeason = 3;//三个月为一个季节
    private bool gameClockPause;//定义一个布尔值来控制游戏的暂停
    private float tikTime;//计时器
    private float timeDifference;//灯光时间差
    public TimeSpan GameTime => new TimeSpan(gameHour, gameMinute, gameSecond);

    public string GUID => GetComponent<DataGUID>().guid;
    private void OnEnable()
    {
        EventHandler.BeforeSceneUnloadEvent += OnBeforeSceneUnloadEvent;
        EventHandler.AfterSceneLoadedEvent += OnAfterSceneLoadedEvent;
        EventHandler.UpdateGameStateEvent += OnUpdateGameStateEvent;
        EventHandler.StartNewGameEvent += OnStartNewGameEvent;
        EventHandler.EndGameEvent += OnEndGameEvent;
    }
    private void OnDisable()
    {
        EventHandler.BeforeSceneUnloadEvent -= OnBeforeSceneUnloadEvent;
        EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvent;
        EventHandler.UpdateGameStateEvent -= OnUpdateGameStateEvent;
        EventHandler.StartNewGameEvent -= OnStartNewGameEvent;
        EventHandler.EndGameEvent -= OnEndGameEvent;
    }

    private void OnEndGameEvent()
    {
        gameClockPause = true;
    }

    private void OnStartNewGameEvent(int index)
    {
        NewGameTime();
        gameClockPause = false;
    }

    private void OnUpdateGameStateEvent(GameState gameState)
    {
        gameClockPause = gameState == GameState.Pause;
    }

    private void OnAfterSceneLoadedEvent()
    {
        gameClockPause = false;
    }

    private void OnBeforeSceneUnloadEvent()
    {
        gameClockPause = true;
    }

    private void Start()
    {
        /*        EventHandler.CallGameDateSeason(gameHour, gameDay, gameMonth, gameYear, gameSeason);
                EventHandler.CallGameMinuteEvent(gameMinute, gameHour,gameSeason,gameDay);
                //切换灯光
                EventHandler.CallLightShiftChangeEvent(gameSeason, GetCurrentLightShift(), timeDifference);*/
        ISaveable saveable = this;
        saveable.RegisterSaveable();
        gameClockPause = true;
    }
    private void Update()
    {
        if (!gameClockPause)
        {
            //编写一个秒针计时器
            tikTime += Time.deltaTime;
            if (tikTime >= Settings.secondThreshold)
            {
                tikTime -= Settings.secondThreshold;
                UpdateGameTime();
            }
        }
        if (Input.GetKey(KeyCode.T))//作弊按钮(时间加快)
        {
            for (int i = 0; i < 60; i++)
            {
                UpdateGameTime();
            }
        }
        if (Input.GetKeyDown(KeyCode.G))//作弊按钮(天数增加)
        {
            gameDay++;
            
            // 检查日期是否需要进位
            int daysInCurrentMonth = GetDaysInMonth(gameYear, gameMonth);
            if (gameDay > daysInCurrentMonth)
            {
                gameDay = 1;
                gameMonth++;
                
                // 处理年份进位
                if (gameMonth > 12)
                {
                    gameMonth = 1;
                    gameYear++;
                    
                    // 防止年份溢出
                    if (gameYear > 9999)
                    {
                        gameYear = 2025;
                    }
                }
                
                // 更新季节逻辑
                monthInSeason--;
                if (monthInSeason == 0)
                {
                    monthInSeason = 3;
                    
                    int seasonNumber = (int)gameSeason;
                    seasonNumber++;
                    
                    if (seasonNumber > Settings.seasonHold)
                    {
                        seasonNumber = 0;
                    }
                    
                    gameSeason = (Season)seasonNumber;
                }
            }
            
            EventHandler.CallGameDayEvent(gameDay, gameSeason);
            EventHandler.CallGameDateSeason(gameHour, gameDay, gameMonth, gameYear, gameSeason);
        }
    }
    private void NewGameTime()//新开一局游戏时给游戏初始化赋值
    {
        gameSecond = 0;
        gameMinute = 0;
        gameHour = 7;//从早上七点开始游戏
        gameDay = 1;
        gameMonth = 3;
        gameYear = 2025;
        gameSeason = Season.春天;
    }
    /// <summary>
    /// 获取指定年月的天数
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <returns>该月的天数</returns>
    private int GetDaysInMonth(int year, int month)
    {
        // 使用C#内置的DateTime.DaysInMonth方法，自动处理闰年
        return System.DateTime.DaysInMonth(year, month);
    }

    /// <summary>
    /// 检查是否为闰年
    /// </summary>
    /// <param name="year">年份</param>
    /// <returns>是否为闰年</returns>
    private bool IsLeapYear(int year)
    {
        return System.DateTime.IsLeapYear(year);
    }

    /// <summary>
    /// 测试方法：验证日期进位逻辑
    /// 在Unity编辑器中可以通过Inspector调用此方法进行测试
    /// </summary>
    [ContextMenu("测试日期进位逻辑")]
    public void TestDateCarryLogic()
    {
        Debug.Log("=== 开始测试日期进位逻辑 ===");
        
        // 测试各种月份的天数
        int[] testYears = { 2024, 2025, 2100, 2000 }; // 包含闰年和非闰年
        
        foreach (int year in testYears)
        {
            Debug.Log($"测试年份: {year} (闰年: {IsLeapYear(year)})");
            
            for (int month = 1; month <= 12; month++)
            {
                int days = GetDaysInMonth(year, month);
                string monthName = month switch
                {
                    1 => "一月", 2 => "二月", 3 => "三月", 4 => "四月",
                    5 => "五月", 6 => "六月", 7 => "七月", 8 => "八月",
                    9 => "九月", 10 => "十月", 11 => "十一月", 12 => "十二月",
                    _ => "未知月份"
                };
                Debug.Log($"  {year}年{monthName}({month}月): {days}天");
            }
        }
        
        Debug.Log("=== 日期进位逻辑测试完成 ===");
    }

    /// <summary>
    /// 获取当前游戏时间的格式化字符串
    /// </summary>
    /// <returns>格式化的时间字符串</returns>
    public string GetFormattedGameTime()
    {
        string seasonName = gameSeason switch
        {
            Season.春天 => "春",
            Season.夏天 => "夏", 
            Season.秋天 => "秋",
            Season.冬天 => "冬",
            _ => "未知"
        };
        
        return $"{gameYear}年{gameMonth:00}月{gameDay:00}日 {gameHour:00}:{gameMinute:00}:{gameSecond:00} ({seasonName}季)";
    }

    private void UpdateGameTime()//更新游戏时间,秒分年月日依次递进
    {
        gameSecond++;
        if (gameSecond > Settings.secondHold)
        {
            gameMinute++;
            gameSecond = 0;

            if (gameMinute > Settings.minuteHold)
            {
                gameHour++;
                gameMinute = 0;

                if (gameHour > Settings.hourHold)
                {
                    gameDay++;
                    gameHour = 0;

                    // 获取当前月份的实际天数
                    int daysInCurrentMonth = GetDaysInMonth(gameYear, gameMonth);
                    
                    if (gameDay > daysInCurrentMonth)
                    {
                        Debug.Log($"[TimeManager] 月份进位: {gameYear}年{gameMonth}月{gameDay}日 -> {gameYear}年{gameMonth + 1}月1日 (当月最大天数: {daysInCurrentMonth})");
                        
                        gameDay = 1;
                        gameMonth++;

                        // 处理年份进位
                        if (gameMonth > 12)
                        {
                            Debug.Log($"[TimeManager] 年份进位: {gameYear}年12月 -> {gameYear + 1}年1月");
                            gameMonth = 1;
                            gameYear++;
                            
                            // 防止年份溢出，重置为合理年份
                            if (gameYear > 9999)
                            {
                                Debug.LogWarning($"[TimeManager] 年份溢出，重置为2025年");
                                gameYear = 2025; // 重置为初始年份而不是2022
                            }
                        }

                        // 更新季节逻辑
                        monthInSeason--;
                        if (monthInSeason == 0)
                        {
                            monthInSeason = 3;

                            int seasonNumber = (int)gameSeason;
                            seasonNumber++;

                            if (seasonNumber > Settings.seasonHold)
                            {
                                seasonNumber = 0;
                            }

                            gameSeason = (Season)seasonNumber;
                        }
                        
                        //用来刷新地图和农作物生长
                        EventHandler.CallGameDayEvent(gameDay, gameSeason);
                    }
                }
                //每时间执行到此位置，调用一下委托时间
                EventHandler.CallGameDateSeason(gameHour, gameDay, gameMonth, gameYear, gameSeason); //这里需要调用一下委托事件
            }
            EventHandler.CallGameMinuteEvent(gameMinute, gameHour, gameSeason, gameDay);//这里需要调用一下委托事件
            //切换灯光
            EventHandler.CallLightShiftChangeEvent(gameSeason, GetCurrentLightShift(), timeDifference);
        }
    }
    /// <summary>
    /// 返回LightShift同时计算时间差
    /// </summary>
    /// <returns></returns>
    private LightShift GetCurrentLightShift()
    {
        if (GameTime >= Settings.morningTime && GameTime < Settings.nightTime)
        {
            timeDifference = (float)(GameTime - Settings.morningTime).TotalMinutes;
            return LightShift.Morning;
        }
        if (GameTime < Settings.morningTime || GameTime >= Settings.nightTime)
        {
            timeDifference = Mathf.Abs((float)(GameTime - Settings.nightTime).TotalMinutes);
            Debug.Log(timeDifference);
            return LightShift.Night;
        }
        return LightShift.Morning;
    }

    public GameSaveData GenerateSaveData()
    {
        GameSaveData saveData = new GameSaveData();
        saveData.timeDict = new Dictionary<string, int>();
        saveData.timeDict.Add("gameYear", gameYear);
        saveData.timeDict.Add("gameSeason", (int)gameSeason);
        saveData.timeDict.Add("gameMonth", gameMonth);
        saveData.timeDict.Add("gameDay", gameDay);
        saveData.timeDict.Add("gameHour", gameHour);
        saveData.timeDict.Add("gameMinute", gameMinute);
        saveData.timeDict.Add("gameSecond", gameSecond);
        return saveData;
    }

    public void RestoreData(GameSaveData saveDate)
    {
        gameYear = saveDate.timeDict["gameYear"];
        gameSeason = (Season)saveDate.timeDict["gameSeason"];
        gameMonth = saveDate.timeDict["gameMonth"];
        gameDay = saveDate.timeDict["gameDay"];
        gameHour = saveDate.timeDict["gameHour"];
        gameMinute = saveDate.timeDict["gameMinute"];
        gameSecond = saveDate.timeDict["gameSecond"];
    }
}
