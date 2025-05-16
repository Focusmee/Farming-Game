using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class TimelineManager : Singleton<TimelineManager>
{
    public PlayableDirector startDirector;//ϷʼʱDirector
    private PlayableDirector currentDirector;
    private bool isPause;
    private bool isDone;
    public bool IsDone { set => isDone = value; }//set:д,ΪIsDoneֵ
    
    [Header("开场动画设置")]
    [Range(0.5f, 2f)]
    public float playbackSpeed = 1f; // 播放速度，可在Inspector中调整
    public GameObject skipButton; // 跳过按钮
    
    protected override void Awake()
    {
        base.Awake();
        currentDirector = startDirector;
        
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
        
        // 添加快捷键跳过开场动画
        if (currentDirector != null && currentDirector.state == PlayState.Playing && Input.GetKeyDown(KeyCode.Escape))
        {
            SkipCutscene();
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
    }
    
    private void OnDisable()
    {
        EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvent;
    }

    private void OnAfterSceneLoadedEvent()
    {
        currentDirector = FindObjectOfType<PlayableDirector>();//ϷغҵTimelineϷŹ
        if (currentDirector != null)
        {
            currentDirector.Play();
            // 应用设置的播放速度
            if (currentDirector.playableGraph.IsValid())
            {
                currentDirector.playableGraph.GetRootPlayable(0).SetSpeed(playbackSpeed);
            }
        }
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
            // 跳到Timeline的结尾
            currentDirector.time = currentDirector.duration;
            
            // 如果是开场动画，可以直接停止播放
            if (currentDirector == startDirector)
            {
                currentDirector.Stop();
                
                // 触发加载主场景等后续操作
                EventHandler.CallUpdateGameStateEvent(GameState.Gameplay);
            }
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
            
            // 如果是开场动画，切换到游戏状态
            if (currentDirector == startDirector)
            {
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
}
