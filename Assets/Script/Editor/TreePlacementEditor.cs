using UnityEngine;
using UnityEditor;
using MFarm.CropPlant;
using MFarm.Map;

/// <summary>
/// 树木放置编辑器工具
/// 提供可视化界面来管理TwilightForest场景中的树木
/// </summary>
public class TreePlacementEditor : EditorWindow
{
    private int selectedTreeType = 1; // 1=CropTree01, 2=CropTree02
    private bool showDebugInfo = true;
    private Vector2 scrollPosition;
    
    // 树木配置
    private int cropTree01SeedID = 1026;
    private int cropTree02SeedID = 1027;
    
    [MenuItem("农场游戏/树木放置工具")]
    public static void ShowWindow()
    {
        TreePlacementEditor window = GetWindow<TreePlacementEditor>("树木放置工具");
        window.minSize = new Vector2(400, 600);
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("树木放置工具", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 基本设置
        EditorGUILayout.LabelField("基本设置", EditorStyles.boldLabel);
        selectedTreeType = EditorGUILayout.IntPopup("树木类型", selectedTreeType, 
            new string[] { "CropTree01", "CropTree02" }, new int[] { 1, 2 });
        
        showDebugInfo = EditorGUILayout.Toggle("显示调试信息", showDebugInfo);
        
        GUILayout.Space(10);
        
        // 种子ID配置
        EditorGUILayout.LabelField("种子ID配置", EditorStyles.boldLabel);
        cropTree01SeedID = EditorGUILayout.IntField("CropTree01 种子ID", cropTree01SeedID);
        cropTree02SeedID = EditorGUILayout.IntField("CropTree02 种子ID", cropTree02SeedID);
        
        GUILayout.Space(10);
        
        // 操作按钮
        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
        
        if (GUILayout.Button("转换现有树木到作物系统", GUILayout.Height(30)))
        {
            ConvertExistingTrees();
        }
        
        EditorGUILayout.HelpBox("此操作会将场景中手动放置的CropTree01和CropTree02预制体转换为作物系统管理的对象。", MessageType.Info);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("清理无效树木对象", GUILayout.Height(30)))
        {
            CleanupInvalidTrees();
        }
        
        EditorGUILayout.HelpBox("此操作会删除场景中没有CropGenerator组件的树木对象。", MessageType.Warning);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("刷新场景作物", GUILayout.Height(30)))
        {
            RefreshSceneCrops();
        }
        
        EditorGUILayout.HelpBox("此操作会触发场景中所有作物的重新生成。", MessageType.Info);
        
        GUILayout.Space(20);
        
        // 场景信息
        EditorGUILayout.LabelField("场景信息", EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("游戏正在运行中。某些操作可能需要停止游戏后执行。", MessageType.Info);
        }
        
        // 统计信息
        int cropTree01Count = CountTreesInScene("CropTree01");
        int cropTree02Count = CountTreesInScene("CropTree02");
        int generatorCount = CountCropGenerators();
        
        EditorGUILayout.LabelField($"场景中的CropTree01数量: {cropTree01Count}");
        EditorGUILayout.LabelField($"场景中的CropTree02数量: {cropTree02Count}");
        EditorGUILayout.LabelField($"CropGenerator组件数量: {generatorCount}");
        
        GUILayout.Space(20);
        
        // 使用说明
        EditorGUILayout.LabelField("使用说明", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. 首先在TwilightForest场景中手动放置CropTree01和CropTree02预制体\n" +
            "2. 点击'转换现有树木到作物系统'按钮\n" +
            "3. 保存场景\n" +
            "4. 运行游戏测试\n\n" +
            "注意：转换后的树木会被作物系统管理，在游戏运行时会正确显示。",
            MessageType.Info);
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// 转换现有树木到作物系统
    /// </summary>
    private void ConvertExistingTrees()
    {
        if (!ConfirmOperation("转换现有树木", "此操作会修改场景中的树木对象，是否继续？"))
            return;
        
        Grid currentGrid = FindObjectOfType<Grid>();
        if (currentGrid == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中没有找到Grid组件！", "确定");
            return;
        }
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int convertedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("CropTree01"))
            {
                ConvertTreeToCropSystem(obj, cropTree01SeedID, currentGrid);
                convertedCount++;
            }
            else if (obj.name.Contains("CropTree02"))
            {
                ConvertTreeToCropSystem(obj, cropTree02SeedID, currentGrid);
                convertedCount++;
            }
        }
        
        EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
        
        string message = $"成功转换了 {convertedCount} 棵树木到作物系统！";
        EditorUtility.DisplayDialog("转换完成", message, "确定");
        
        if (showDebugInfo)
        {
            Debug.Log($"[TreePlacementEditor] {message}");
        }
    }
    
    /// <summary>
    /// 将树木对象转换为作物系统管理
    /// </summary>
    private void ConvertTreeToCropSystem(GameObject treeObject, int seedID, Grid currentGrid)
    {
        // 添加CropGenerator组件
        CropGenerator generator = treeObject.GetComponent<CropGenerator>();
        if (generator == null)
        {
            generator = treeObject.AddComponent<CropGenerator>();
        }
        
        generator.seedItemID = seedID;
        generator.growthDays = GetTreeGrowthDays(seedID);
        
        // 确保对象在正确的父对象下
        Transform cropParent = GameObject.FindWithTag("CropParent")?.transform;
        if (cropParent != null && treeObject.transform.parent != cropParent)
        {
            treeObject.transform.SetParent(cropParent);
        }
        
        if (showDebugInfo)
        {
            Vector3Int gridPos = currentGrid.WorldToCell(treeObject.transform.position);
            Debug.Log($"[TreePlacementEditor] 转换树木: {treeObject.name} 位置: {gridPos} 种子ID: {seedID}");
        }
    }
    
    /// <summary>
    /// 获取树木的成长天数
    /// </summary>
    private int GetTreeGrowthDays(int seedID)
    {
        // 根据CropDataList_SO中的配置，树木的总成长天数是58天
        return 58;
    }
    
    /// <summary>
    /// 清理无效树木对象
    /// </summary>
    private void CleanupInvalidTrees()
    {
        if (!ConfirmOperation("清理无效树木", "此操作会删除没有CropGenerator组件的树木对象，是否继续？"))
            return;
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int cleanedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if ((obj.name.Contains("CropTree01") || obj.name.Contains("CropTree02")) && 
                obj.GetComponent<CropGenerator>() == null)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[TreePlacementEditor] 清理无效树木: {obj.name}");
                }
                DestroyImmediate(obj);
                cleanedCount++;
            }
        }
        
        string message = $"清理了 {cleanedCount} 个无效树木对象！";
        EditorUtility.DisplayDialog("清理完成", message, "确定");
        
        if (showDebugInfo)
        {
            Debug.Log($"[TreePlacementEditor] {message}");
        }
    }
    
    /// <summary>
    /// 刷新场景作物
    /// </summary>
    private void RefreshSceneCrops()
    {
        if (Application.isPlaying)
        {
            EventHandler.CallRefreshCurrentMap();
            Debug.Log("[TreePlacementEditor] 已触发场景作物刷新");
        }
        else
        {
            EditorUtility.DisplayDialog("提示", "此操作需要在游戏运行时执行！", "确定");
        }
    }
    
    /// <summary>
    /// 统计场景中指定名称的树木数量
    /// </summary>
    private int CountTreesInScene(string treeName)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int count = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains(treeName))
            {
                count++;
            }
        }
        
        return count;
    }
    
    /// <summary>
    /// 统计场景中CropGenerator组件的数量
    /// </summary>
    private int CountCropGenerators()
    {
        return FindObjectsOfType<CropGenerator>().Length;
    }
    
    /// <summary>
    /// 确认操作对话框
    /// </summary>
    private bool ConfirmOperation(string title, string message)
    {
        return EditorUtility.DisplayDialog(title, message, "确定", "取消");
    }
} 