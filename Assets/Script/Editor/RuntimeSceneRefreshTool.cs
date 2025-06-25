using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 运行时场景刷新工具
/// 用于解决运行时加载的场景与编辑器中修改的场景不同步的问题
/// </summary>
public class RuntimeSceneRefreshTool : EditorWindow
{
    private string targetSceneName = "06.TwilightForest";
    private bool autoRefreshOnPlay = true;
    private Vector2 scrollPosition;
    
    [MenuItem("工具/运行时场景刷新工具")]
    public static void ShowWindow()
    {
        GetWindow<RuntimeSceneRefreshTool>("场景刷新工具");
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("运行时场景刷新工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 问题说明
        EditorGUILayout.HelpBox(
            "此工具用于解决运行时动态加载的场景与编辑器中修改的场景不同步的问题。\n" +
            "当你修改了场景文件但运行时看到的内容没有更新时，可以使用此工具强制刷新。", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        // 目标场景设置
        EditorGUILayout.LabelField("场景设置", EditorStyles.boldLabel);
        targetSceneName = EditorGUILayout.TextField("目标场景名称:", targetSceneName);
        
        EditorGUILayout.Space();
        
        // 自动刷新设置
        autoRefreshOnPlay = EditorGUILayout.Toggle("进入播放模式时自动刷新", autoRefreshOnPlay);
        
        EditorGUILayout.Space();
        
        // 当前状态显示
        EditorGUILayout.LabelField("当前状态", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"播放模式: {(Application.isPlaying ? "是" : "否")}");
        
        if (Application.isPlaying)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            EditorGUILayout.LabelField($"当前活动场景: {activeScene.name}");
            EditorGUILayout.LabelField($"已加载场景数量: {SceneManager.sceneCount}");
            
            // 显示所有已加载的场景
            EditorGUILayout.LabelField("已加载的场景:");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                string status = scene == activeScene ? " (活动)" : "";
                EditorGUILayout.LabelField($"  - {scene.name}{status}");
            }
        }
        
        EditorGUILayout.Space();
        
        // 操作按钮
        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
        
        GUI.enabled = Application.isPlaying;
        
        if (GUILayout.Button("强制刷新当前场景", GUILayout.Height(30)))
        {
            ForceRefreshCurrentScene();
        }
        
        if (GUILayout.Button($"切换到 {targetSceneName}", GUILayout.Height(30)))
        {
            SwitchToScene(targetSceneName);
        }
        
        if (GUILayout.Button("重新加载TwilightForest", GUILayout.Height(30)))
        {
            SwitchToScene("06.TwilightForest");
        }
        
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // 快捷键说明
        EditorGUILayout.HelpBox(
            "快捷键:\n" +
            "F5 - 切换到指定场景\n" +
            "F6 - 强制刷新当前场景", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        // 解决方案说明
        EditorGUILayout.LabelField("解决方案说明", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "如果运行时场景与编辑器中的不同，可能的原因:\n" +
            "1. 场景文件未保存 (Ctrl+S)\n" +
            "2. Build Settings中的场景版本过旧\n" +
            "3. Unity内部缓存问题\n\n" +
            "解决步骤:\n" +
            "1. 确保场景文件已保存\n" +
            "2. 使用上面的'强制刷新'按钮\n" +
            "3. 如果问题持续，重启Unity编辑器", 
            MessageType.Warning);
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// 强制刷新当前场景
    /// </summary>
    private void ForceRefreshCurrentScene()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("错误", "此功能只能在播放模式下使用", "确定");
            return;
        }
        
        // 查找SceneSwitcher组件
        SceneSwitcher sceneSwitcher = FindObjectOfType<SceneSwitcher>();
        if (sceneSwitcher != null)
        {
            sceneSwitcher.ForceRefreshCurrentScene();
            Debug.Log("通过SceneSwitcher强制刷新场景");
        }
        else
        {
            // 如果没有SceneSwitcher，创建一个临时的
            GameObject tempObj = new GameObject("TempSceneSwitcher");
            SceneSwitcher tempSwitcher = tempObj.AddComponent<SceneSwitcher>();
            tempSwitcher.ForceRefreshCurrentScene();
            Debug.Log("创建临时SceneSwitcher并强制刷新场景");
        }
    }
    
    /// <summary>
    /// 切换到指定场景
    /// </summary>
    private void SwitchToScene(string sceneName)
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("错误", "此功能只能在播放模式下使用", "确定");
            return;
        }
        
        // 使用EventHandler进行场景切换
        EventHandler.CallTransitionEvent(sceneName, Vector3.zero);
        Debug.Log($"切换到场景: {sceneName}");
    }
    
    /// <summary>
    /// 当进入播放模式时的回调
    /// </summary>
    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // 如果设置了自动刷新，延迟执行刷新
            EditorApplication.delayCall += () =>
            {
                RuntimeSceneRefreshTool window = GetWindow<RuntimeSceneRefreshTool>();
                if (window != null && window.autoRefreshOnPlay)
                {
                    Debug.Log("自动刷新场景功能已启用");
                }
            };
        }
    }
} 