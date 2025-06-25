using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 场景切换工具 - 用于在运行时替换当前场景
/// </summary>
public class SceneSwitcher : MonoBehaviour
{
    [Header("场景切换设置")]
    [SceneName]
    public string targetSceneName = "06.TwilightForest";
    
    [Header("快捷键设置")]
    public KeyCode switchKey = KeyCode.F5;
    public KeyCode refreshKey = KeyCode.F6; // 新增：强制刷新当前场景的快捷键
    
    private void Update()
    {
        // 按F5键切换到指定场景
        if (Input.GetKeyDown(switchKey))
        {
            SwitchToScene(targetSceneName);
        }
        
        // 按F6键强制刷新当前场景（重新加载最新版本）
        if (Input.GetKeyDown(refreshKey))
        {
            ForceRefreshCurrentScene();
        }
    }
    
    /// <summary>
    /// 切换到指定场景
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public void SwitchToScene(string sceneName)
    {
        Debug.Log($"正在切换到场景: {sceneName}");
        
        // 使用TransitionManager的事件系统进行场景切换
        // 这样可以确保所有相关的事件和逻辑都被正确处理
        EventHandler.CallTransitionEvent(sceneName, Vector3.zero);
    }
    
    /// <summary>
    /// 重新加载当前场景（用于刷新场景内容）
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName != "PersistentScene" && currentSceneName != "UI")
        {
            Debug.Log($"重新加载场景: {currentSceneName}");
            SwitchToScene(currentSceneName);
        }
    }
    
    /// <summary>
    /// 强制刷新当前场景 - 确保加载最新的场景文件
    /// 这个方法会强制Unity重新从磁盘加载场景文件
    /// </summary>
    public void ForceRefreshCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName != "PersistentScene" && currentSceneName != "UI")
        {
            Debug.Log($"强制刷新场景: {currentSceneName} - 这将重新加载最新的场景文件");
            StartCoroutine(ForceRefreshSceneCoroutine(currentSceneName));
        }
        else
        {
            Debug.LogWarning("无法刷新PersistentScene或UI场景");
        }
    }
    
    /// <summary>
    /// 强制刷新场景的协程实现
    /// </summary>
    private IEnumerator ForceRefreshSceneCoroutine(string sceneName)
    {
        Debug.Log($"开始强制刷新场景: {sceneName}");
        
        // 1. 触发场景卸载前事件
        EventHandler.CallBeforeSceneUnloadEvent();
        
        // 2. 等待一帧确保事件处理完成
        yield return null;
        
        // 3. 卸载当前场景
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.IsValid() && currentScene.name == sceneName)
        {
            Debug.Log($"卸载场景: {sceneName}");
            yield return SceneManager.UnloadSceneAsync(currentScene);
        }
        
        // 4. 强制垃圾回收，清理内存
        System.GC.Collect();
        yield return null;
        
        // 5. 重新加载场景
        Debug.Log($"重新加载场景: {sceneName}");
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        
        // 6. 设置新场景为活动场景
        Scene newScene = SceneManager.GetSceneByName(sceneName);
        if (newScene.IsValid())
        {
            SceneManager.SetActiveScene(newScene);
            Debug.Log($"设置 {sceneName} 为活动场景");
        }
        else
        {
            Debug.LogError($"无法找到重新加载的场景: {sceneName}");
        }
        
        // 7. 触发场景加载后事件
        EventHandler.CallAfterSceneLoadedEvent();
        
        Debug.Log($"场景强制刷新完成: {sceneName}");
    }
    
    /// <summary>
    /// 直接替换场景（不通过TransitionManager）
    /// 注意：这种方法可能会跳过一些重要的事件处理
    /// </summary>
    public void DirectReplaceScene(string sceneName)
    {
        StartCoroutine(DirectReplaceSceneCoroutine(sceneName));
    }
    
    private IEnumerator DirectReplaceSceneCoroutine(string sceneName)
    {
        Debug.Log($"直接替换场景: {sceneName}");
        
        // 触发场景卸载前事件
        EventHandler.CallBeforeSceneUnloadEvent();
        
        // 卸载当前活动场景（除了PersistentScene和UI）
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name != "PersistentScene" && currentScene.name != "UI")
        {
            yield return SceneManager.UnloadSceneAsync(currentScene);
        }
        
        // 加载新场景
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        
        // 设置新场景为活动场景
        Scene newScene = SceneManager.GetSceneByName(sceneName);
        if (newScene.IsValid())
        {
            SceneManager.SetActiveScene(newScene);
        }
        
        // 触发场景加载后事件
        EventHandler.CallAfterSceneLoadedEvent();
        
        Debug.Log($"场景替换完成: {sceneName}");
    }
} 