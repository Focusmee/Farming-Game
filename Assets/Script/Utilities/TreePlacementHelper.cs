using UnityEngine;
using MFarm.CropPlant;
using MFarm.Map;

/// <summary>
/// 树木放置助手工具
/// 用于正确地在TwilightForest场景中添加CropTree01和CropTree02
/// </summary>
public class TreePlacementHelper : MonoBehaviour
{
    [Header("树木配置")]
    [Tooltip("CropTree01的种子ID")]
    public int cropTree01SeedID = 1026; // 对应CropDataList中的树木1
    
    [Tooltip("CropTree02的种子ID")]
    public int cropTree02SeedID = 1027; // 对应CropDataList中的树木2
    
    [Tooltip("树木的成长天数（-1表示完全成熟）")]
    public int treeGrowthDays = -1; // 完全成熟的树木
    
    [Header("放置设置")]
    [Tooltip("是否在Start时自动转换现有的树木")]
    public bool autoConvertOnStart = true;
    
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;
    
    private Grid currentGrid;
    
    private void Start()
    {
        currentGrid = FindObjectOfType<Grid>();
        
        if (autoConvertOnStart)
        {
            ConvertExistingTrees();
        }
    }
    
    /// <summary>
    /// 转换现有的树木预制体为正确的作物系统管理
    /// </summary>
    [ContextMenu("转换现有树木")]
    public void ConvertExistingTrees()
    {
        if (currentGrid == null)
        {
            currentGrid = FindObjectOfType<Grid>();
        }
        
        // 查找所有CropTree01预制体
        GameObject[] cropTree01s = GameObject.FindGameObjectsWithTag("Untagged");
        int convertedCount = 0;
        
        foreach (GameObject obj in cropTree01s)
        {
            if (obj.name.Contains("CropTree01"))
            {
                ConvertTreeToCropSystem(obj, cropTree01SeedID);
                convertedCount++;
            }
            else if (obj.name.Contains("CropTree02"))
            {
                ConvertTreeToCropSystem(obj, cropTree02SeedID);
                convertedCount++;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[TreePlacementHelper] 成功转换了 {convertedCount} 棵树木到作物系统");
        }
    }
    
    /// <summary>
    /// 将树木对象转换为作物系统管理
    /// </summary>
    /// <param name="treeObject">树木游戏对象</param>
    /// <param name="seedID">对应的种子ID</param>
    private void ConvertTreeToCropSystem(GameObject treeObject, int seedID)
    {
        // 获取树木的网格位置
        Vector3 worldPos = treeObject.transform.position;
        Vector3Int gridPos = currentGrid.WorldToCell(worldPos);
        
        // 创建或更新TileDetails
        TileDetails tileDetails = GridMapManager.Instance.GetTileDetailsOnMousePosition(gridPos);
        if (tileDetails == null)
        {
            tileDetails = new TileDetails();
            tileDetails.gridX = gridPos.x;
            tileDetails.gridY = gridPos.y;
        }
        
        // 设置作物信息
        tileDetails.seedItemID = seedID;
        tileDetails.growthDays = GetTreeGrowthDays(seedID); // 设置为完全成熟
        tileDetails.daysSinceWatered = -1;
        tileDetails.daysSinceDug = -1;
        tileDetails.daysSinceLastHarvest = -1;
        
        // 更新到系统中
        GridMapManager.Instance.UpdateTileDetails(tileDetails);
        
        // 添加CropGenerator组件到现有对象
        CropGenerator generator = treeObject.GetComponent<CropGenerator>();
        if (generator == null)
        {
            generator = treeObject.AddComponent<CropGenerator>();
        }
        
        generator.seedItemID = seedID;
        generator.growthDays = GetTreeGrowthDays(seedID);
        
        if (showDebugInfo)
        {
            Debug.Log($"[TreePlacementHelper] 转换树木: {treeObject.name} 位置: {gridPos} 种子ID: {seedID}");
        }
    }
    
    /// <summary>
    /// 获取树木的成长天数
    /// </summary>
    /// <param name="seedID">种子ID</param>
    /// <returns>成长天数</returns>
    private int GetTreeGrowthDays(int seedID)
    {
        CropDetails cropDetails = CropManager.Instance.GetCropDetails(seedID);
        if (cropDetails != null)
        {
            return cropDetails.TotalGrowthDays; // 返回完全成熟所需的天数
        }
        return 58; // 默认值（对应CropDataList中树木的总成长天数）
    }
    
    /// <summary>
    /// 在指定位置放置树木
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    /// <param name="treeType">树木类型（1=CropTree01, 2=CropTree02）</param>
    [ContextMenu("在当前位置放置CropTree01")]
    public void PlaceCropTree01AtCurrentPosition()
    {
        PlaceTreeAtPosition(transform.position, 1);
    }
    
    [ContextMenu("在当前位置放置CropTree02")]
    public void PlaceCropTree02AtCurrentPosition()
    {
        PlaceTreeAtPosition(transform.position, 2);
    }
    
    /// <summary>
    /// 在指定位置放置树木
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <param name="treeType">树木类型</param>
    public void PlaceTreeAtPosition(Vector3 worldPosition, int treeType)
    {
        if (currentGrid == null)
        {
            currentGrid = FindObjectOfType<Grid>();
        }
        
        int seedID = treeType == 1 ? cropTree01SeedID : cropTree02SeedID;
        Vector3Int gridPos = currentGrid.WorldToCell(worldPosition);
        
        // 创建TileDetails
        TileDetails tileDetails = new TileDetails();
        tileDetails.gridX = gridPos.x;
        tileDetails.gridY = gridPos.y;
        tileDetails.seedItemID = seedID;
        tileDetails.growthDays = GetTreeGrowthDays(seedID);
        tileDetails.daysSinceWatered = -1;
        tileDetails.daysSinceDug = -1;
        tileDetails.daysSinceLastHarvest = -1;
        
        // 更新到系统中
        GridMapManager.Instance.UpdateTileDetails(tileDetails);
        
        // 创建CropGenerator对象
        GameObject generatorObj = new GameObject($"TreeGenerator_{treeType}_{gridPos.x}_{gridPos.y}");
        generatorObj.transform.position = worldPosition;
        
        CropGenerator generator = generatorObj.AddComponent<CropGenerator>();
        generator.seedItemID = seedID;
        generator.growthDays = GetTreeGrowthDays(seedID);
        
        if (showDebugInfo)
        {
            Debug.Log($"[TreePlacementHelper] 在位置 {gridPos} 放置了树木类型 {treeType}");
        }
    }
    
    /// <summary>
    /// 清理场景中的无效树木对象
    /// </summary>
    [ContextMenu("清理无效树木")]
    public void CleanupInvalidTrees()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int cleanedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if ((obj.name.Contains("CropTree01") || obj.name.Contains("CropTree02")) && 
                obj.GetComponent<CropGenerator>() == null)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[TreePlacementHelper] 清理无效树木: {obj.name}");
                }
                DestroyImmediate(obj);
                cleanedCount++;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[TreePlacementHelper] 清理了 {cleanedCount} 个无效树木对象");
        }
    }
} 