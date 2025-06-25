using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// 场景替换工具 - Unity编辑器扩展
/// </summary>
public class SceneReplacementTool : EditorWindow
{
    private string sourceScenePath = "";
    private string targetScenePath = "";
    private bool backupOriginal = true;
    
    [MenuItem("工具/场景替换工具")]
    public static void ShowWindow()
    {
        GetWindow<SceneReplacementTool>("场景替换工具");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("场景替换工具", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 源场景选择
        GUILayout.Label("要替换的场景（当前在游戏中的）:");
        if (GUILayout.Button("选择源场景"))
        {
            sourceScenePath = EditorUtility.OpenFilePanel("选择源场景", "Assets/Scenes", "unity");
            if (sourceScenePath.StartsWith(Application.dataPath))
            {
                sourceScenePath = "Assets" + sourceScenePath.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.LabelField("源场景:", sourceScenePath);
        
        GUILayout.Space(10);
        
        // 目标场景选择
        GUILayout.Label("新的场景文件（你修改过的）:");
        if (GUILayout.Button("选择目标场景"))
        {
            targetScenePath = EditorUtility.OpenFilePanel("选择目标场景", "Assets/Scenes", "unity");
            if (targetScenePath.StartsWith(Application.dataPath))
            {
                targetScenePath = "Assets" + targetScenePath.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.LabelField("目标场景:", targetScenePath);
        
        GUILayout.Space(10);
        
        // 备份选项
        backupOriginal = EditorGUILayout.Toggle("备份原始场景", backupOriginal);
        
        GUILayout.Space(20);
        
        // 替换按钮
        GUI.enabled = !string.IsNullOrEmpty(sourceScenePath) && !string.IsNullOrEmpty(targetScenePath);
        if (GUILayout.Button("替换场景", GUILayout.Height(30)))
        {
            ReplaceScene();
        }
        GUI.enabled = true;
        
        GUILayout.Space(20);
        
        // 快速操作区域
        GUILayout.Label("快速操作", EditorStyles.boldLabel);
        
        if (GUILayout.Button("重新加载TwilightForest"))
        {
            ReloadTwilightForest();
        }
        
        if (GUILayout.Button("从项目中的TwilightForest替换运行时场景"))
        {
            ReplaceWithProjectTwilightForest();
        }
        
        GUILayout.Space(10);
        
        // 帮助信息
        EditorGUILayout.HelpBox(
            "使用说明:\n" +
            "1. 选择要替换的源场景（通常是Assets/Scenes/TwilightForest.unity）\n" +
            "2. 选择你修改过的目标场景文件\n" +
            "3. 点击'替换场景'按钮\n" +
            "4. 如果游戏正在运行，场景会自动重新加载",
            MessageType.Info);
    }
    
    /// <summary>
    /// 替换场景文件
    /// </summary>
    private void ReplaceScene()
    {
        try
        {
            // 备份原始文件
            if (backupOriginal)
            {
                string backupPath = sourceScenePath.Replace(".unity", "_backup.unity");
                AssetDatabase.CopyAsset(sourceScenePath, backupPath);
                Debug.Log($"已备份原始场景到: {backupPath}");
            }
            
            // 复制目标文件到源文件位置
            AssetDatabase.CopyAsset(targetScenePath, sourceScenePath);
            AssetDatabase.Refresh();
            
            Debug.Log($"场景替换完成: {targetScenePath} -> {sourceScenePath}");
            
            // 如果游戏正在运行，尝试重新加载场景
            if (Application.isPlaying)
            {
                ReloadSceneInPlayMode();
            }
            
            EditorUtility.DisplayDialog("成功", "场景替换完成！", "确定");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"场景替换失败: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"场景替换失败: {e.Message}", "确定");
        }
    }
    
    /// <summary>
    /// 重新加载TwilightForest场景
    /// </summary>
    private void ReloadTwilightForest()
    {
        if (Application.isPlaying)
        {
            // 运行时重新加载
            var sceneSwitcher = FindObjectOfType<SceneSwitcher>();
            if (sceneSwitcher != null)
            {
                sceneSwitcher.SwitchToScene("TwilightForest");
            }
            else
            {
                Debug.LogWarning("未找到SceneSwitcher组件，请手动添加到场景中");
            }
        }
        else
        {
            // 编辑器模式下打开场景
            EditorSceneManager.OpenScene("Assets/Scenes/TwilightForest.unity");
        }
    }
    
    /// <summary>
    /// 使用项目中的TwilightForest替换运行时场景
    /// </summary>
    private void ReplaceWithProjectTwilightForest()
    {
        sourceScenePath = "Assets/Scenes/TwilightForest.unity";
        
        // 查找项目中的其他TwilightForest场景文件
        string[] guids = AssetDatabase.FindAssets("TwilightForest t:Scene");
        
        if (guids.Length > 1)
        {
            // 如果有多个，让用户选择
            string[] paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }
            
            int choice = EditorUtility.DisplayDialogComplex(
                "选择场景",
                "找到多个TwilightForest场景，请选择要使用的版本:",
                "使用第一个", "取消", "显示所有选项");
                
            if (choice == 0 && paths.Length > 0)
            {
                targetScenePath = paths[0];
                ReplaceScene();
            }
            else if (choice == 2)
            {
                // 显示选择窗口
                ShowSceneSelectionWindow(paths);
            }
        }
        else if (guids.Length == 1)
        {
            targetScenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            ReplaceScene();
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "未找到TwilightForest场景文件", "确定");
        }
    }
    
    /// <summary>
    /// 显示场景选择窗口
    /// </summary>
    private void ShowSceneSelectionWindow(string[] scenePaths)
    {
        GenericMenu menu = new GenericMenu();
        
        foreach (string path in scenePaths)
        {
            string displayName = Path.GetFileNameWithoutExtension(path) + " (" + Path.GetDirectoryName(path) + ")";
            menu.AddItem(new GUIContent(displayName), false, () => {
                targetScenePath = path;
                ReplaceScene();
            });
        }
        
        menu.ShowAsContext();
    }
    
    /// <summary>
    /// 在运行模式下重新加载场景
    /// </summary>
    private void ReloadSceneInPlayMode()
    {
        if (!Application.isPlaying) return;
        
        // 查找SceneSwitcher组件
        var sceneSwitcher = FindObjectOfType<SceneSwitcher>();
        if (sceneSwitcher != null)
        {
            sceneSwitcher.ReloadCurrentScene();
        }
        else
        {
            // 如果没有SceneSwitcher，直接使用EventHandler
            EventHandler.CallTransitionEvent("TwilightForest", Vector3.zero);
        }
    }
} 