using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using MFarm.Save;

public class TimelineManager : Singleton<TimelineManager>, ISaveable
{
    public PlayableDirector startDirector;//游戏开始时Director
    private PlayableDirector currentDirector;
    private bool isPause;
    private bool isDone;
    public bool IsDone { set => isDone = value; }//set:写,为IsDone赋值
    
    [Header("开场动画设置")]
    [Range(0.5f, 2f)]
    public float playbackSpeed = 1f; // 播放速度，可在Inspector中调整
    public GameObject skipButton; // 跳过按钮
    
    /// <summary>
    /// 开场动画Canvas引用
    /// </summary>
    [Header("UI引用")]
    public Canvas introCanvas; // Intro Canvas引用
    
    /// <summary>
    /// 玩家是否已经看过开场动画
    /// </summary>
    private bool hasSeenIntro = false;
    
    /// <summary>
    /// 获取或创建DataGUID组件
    /// </summary>
    private DataGUID dataGuidComponent;
    
    public string GUID 
    { 
        get 
        {
            // 如果DataGUID组件不存在，自动添加一个
            if (dataGuidComponent == null)
            {
                dataGuidComponent = GetComponent<DataGUID>();
                if (dataGuidComponent == null)
                {
                    dataGuidComponent = gameObject.AddComponent<DataGUID>();
                    Debug.Log($"为 {gameObject.name} 自动添加了 DataGUID 组件");
                }
            }
            return dataGuidComponent.guid;
        }
    }
    
    protected override void Awake()
    {
        base.Awake();
        currentDirector = startDirector;
        
        // 确保DataGUID组件存在
        dataGuidComponent = GetComponent<DataGUID>();
        if (dataGuidComponent == null)
        {
            dataGuidComponent = gameObject.AddComponent<DataGUID>();
            Debug.Log($"为 {gameObject.name} 添加了 DataGUID 组件");
        }
        
        // 自动查找Intro Canvas（如果没有手动设置）
        if (introCanvas == null)
        {
            GameObject introCanvasGO = GameObject.Find("Intro Canvas");
            if (introCanvasGO != null)
            {
                introCanvas = introCanvasGO.GetComponent<Canvas>();
                Debug.Log("自动找到 Intro Canvas");
            }
        }
        
        // 设置跳过按钮的点击事件
        if (skipButton != null)
        {
            skipButton.GetComponent<Button>().onClick.AddListener(SkipCutscene);
        }
    }

    private void Start()
    {
        // 在Start中设置初始播放速度，确保PlayableDirector已完全初始化
        if (currentDirector != null && currentDirector.playableGraph.IsValid())
        {
            currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(playbackSpeed);
        }
        
        // 注册为可保存对象
        ISaveable saveable = this;
        saveable.RegisterSaveable();
    }
    
    private void Update()
    {
        if (isPause && Input.GetKeyDown(KeyCode.Space) && isDone)
        {
            isPause = false;
            if (currentDirector != null && currentDirector.playableGraph.IsValid())
            {
                currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(playbackSpeed);
            }
        }
        
        // 添加快捷键跳过开场动画 - 支持ESC和空格键
        if (currentDirector != null && currentDirector.state == PlayState.Playing)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("检测到跳过按键，准备跳过开场动画");
                SkipCutscene();
            }
        }

        // 检查Timeline是否播放完毕
        if (currentDirector != null && currentDirector.state == PlayState.Playing)
        {
            if (currentDirector.time >= currentDirector.duration)
            {
                OnTimelineComplete();
            }
        }
    }
    
    private void OnEnable()
    {
        EventHandler.AfterSceneLoadedEvent += OnAfterSceneLoadedEvent;
        EventHandler.StartNewGameEvent += OnStartNewGameEvent;
    }
    
    private void OnDisable()
    {
        EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvent;
        EventHandler.StartNewGameEvent -= OnStartNewGameEvent;
    }
    
    /// <summary>
    /// 处理新游戏事件
    /// </summary>
    private void OnStartNewGameEvent(int index)
    {
        // 开始新游戏时重置开场动画状态
        hasSeenIntro = false;
        Debug.Log("开始新游戏：重置开场动画状态");
    }

    private void OnAfterSceneLoadedEvent()
    {
        currentDirector = FindObjectOfType<PlayableDirector>();//场景加载后找到Timeline播放器
        
        // 检查是否需要播放开场动画
        if (ShouldPlayIntro())
        {
            // 显示Intro Canvas并播放动画
            ShowIntroCanvas(true);
            
            if (currentDirector != null)
            {
                currentDirector.Play();
                // 应用设置的播放速度
                if (currentDirector.playableGraph.IsValid())
                {
                    currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(playbackSpeed);
                }
                Debug.Log("开始播放开场动画");
            }
        }
        else
        {
            // 隐藏Intro Canvas，跳过开场动画
            ShowIntroCanvas(false);
            Debug.Log("跳过开场动画，直接进入游戏");
            EventHandler.CallUpdateGameStateEvent(GameState.Gameplay);
        }
    }
    
    /// <summary>
    /// 显示或隐藏Intro Canvas
    /// </summary>
    private void ShowIntroCanvas(bool show)
    {
        if (introCanvas != null)
        {
            introCanvas.gameObject.SetActive(show);
            Debug.Log($"Intro Canvas {(show ? "显示" : "隐藏")}");
        }
        else
        {
            Debug.LogWarning("Intro Canvas 引用为空，无法控制显示状态");
        }
    }
    
    /// <summary>
    /// 判断是否应该播放开场动画
    /// </summary>
    private bool ShouldPlayIntro()
    {
        // 如果正在加载已有存档，直接跳过开场动画
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.isLoadingExistingSave)
        {
            Debug.Log("检测到正在加载存档，跳过开场动画");
            return false;
        }
        
        // 如果玩家已经看过开场动画，跳过播放
        if (hasSeenIntro)
        {
            Debug.Log("玩家已观看过开场动画，跳过播放");
            return false;
        }
        
        // 如果是开场动画且玩家未看过，则播放
        bool shouldPlay = currentDirector == startDirector && !hasSeenIntro;
        
        if (shouldPlay)
        {
            Debug.Log("播放开场动画");
        }
        
        return shouldPlay;
    }

    public void PauseTimeline(PlayableDirector director)
    {
        currentDirector = director;
        if (currentDirector != null && currentDirector.playableGraph.IsValid())
        {
        currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(0d);
        isPause = true;
        }
    }
    
    /// <summary>
    /// 跳过开场动画
    /// </summary>
    public void SkipCutscene()
    {
        if (currentDirector != null && currentDirector.state == PlayState.Playing)
        {
            Debug.Log("执行跳过开场动画");
            
            // 停止Timeline播放
            currentDirector.Stop();
            
            // 隐藏Intro Canvas
            ShowIntroCanvas(false);
            
            // 如果是开场动画，标记已观看并进入游戏
            if (currentDirector == startDirector)
            {
                // 标记玩家已经看过开场动画（包括跳过的情况）
                hasSeenIntro = true;
                Debug.Log("TimelineManager: 标记玩家已经看过开场动画");
                
                // 触发加载主场景等后续操作
                EventHandler.CallUpdateGameStateEvent(GameState.Gameplay);
            }
        }
        else
        {
            Debug.Log("当前没有播放中的Timeline，无法跳过");
        }
    }

    /// <summary>
    /// Timeline播放完成时的处理
    /// </summary>
    private void OnTimelineComplete()
    {
        if (currentDirector != null)
        {
            currentDirector.Stop();
            
            // 隐藏Intro Canvas
            ShowIntroCanvas(false);
            
            // 如果是开场动画，切换到游戏状态
            if (currentDirector == startDirector)
            {
                // 标记玩家已经看过开场动画
                hasSeenIntro = true;
                Debug.Log("开场动画播放完成，进入游戏");
                EventHandler.CallUpdateGameStateEvent(GameState.Gameplay);
            }
        }
    }
    
    /// <summary>
    /// 设置播放速度
    /// </summary>
    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = speed;
        if (currentDirector != null && currentDirector.state == PlayState.Playing && !isPause && currentDirector.playableGraph.IsValid())
        {
            currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(playbackSpeed);
        }
    }
    
    /// <summary>
    /// 生成保存数据
    /// </summary>
    public GameSaveData GenerateSaveData()
    {
        GameSaveData saveData = new GameSaveData();
        saveData.hasSeenIntro = this.hasSeenIntro;
        return saveData;
    }
    
    /// <summary>
    /// 恢复保存数据
    /// </summary>
    public void RestoreData(GameSaveData saveData)
    {
        // 恢复开场动画状态
        this.hasSeenIntro = saveData.hasSeenIntro;
        
        // 如果正在加载已有存档，说明玩家之前已经游玩过，应该跳过开场动画
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.isLoadingExistingSave)
        {
            // 对于旧存档（可能没有hasSeenIntro字段），默认认为已经看过开场动画
            this.hasSeenIntro = true;
            Debug.Log("加载旧存档：自动设置为已观看开场动画状态");
        }
        
        Debug.Log($"TimelineManager 恢复数据：hasSeenIntro = {this.hasSeenIntro}");
    }
}
