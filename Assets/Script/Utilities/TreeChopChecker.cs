using System.Collections.Generic;
using UnityEngine;
using MFarm.CropPlant;

/// <summary>
/// 树木砍伐检查工具
/// 用于检查地图上的树木是否符合砍伐条件
/// </summary>
public class TreeChopChecker : MonoBehaviour
{
    [Header("检查设置")]
    [Tooltip("是否在场景视图中显示检查结果")]
    public bool showGizmosInScene = true;
    
    [Tooltip("是否在控制台输出详细信息")]
    public bool logDetailedInfo = true;
    
    [Tooltip("检查范围半径")]
    public float checkRadius = 20f;
    
    [Header("工具ID设置")]
    [Tooltip("斧头工具ID")]
    public int axeToolID = 1001; // 斧头工具ID (对应CropDataList中的e9030000)
    
    [Header("颜色设置")]
    [Tooltip("可砍伐树木的颜色")]
    public Color choppableColor = Color.green;
    
    [Tooltip("不可砍伐树木的颜色")]
    public Color nonChoppableColor = Color.red;
    
    [Tooltip("未成熟树木的颜色")]
    public Color immatureColor = Color.yellow;
    
    private List<TreeInfo> treeInfoList = new List<TreeInfo>();
    
    /// <summary>
    /// 树木信息结构
    /// </summary>
    [System.Serializable]
    public class TreeInfo
    {
        public GameObject treeObject;
        public Crop cropComponent;
        public CropDetails cropDetails;
        public TileDetails tileDetails;
        public bool canChop;
        public bool isMature;
        public string reason;
        public Vector3 position;
    }
    
    private void Start()
    {
        // 启动时自动检查一次
        CheckAllTrees();
    }
    
    /// <summary>
    /// 检查所有树木的砍伐条件
    /// </summary>
    [ContextMenu("检查所有树木")]
    public void CheckAllTrees()
    {
        treeInfoList.Clear();
        
        // 查找所有带有Crop组件的游戏对象（包括树木）
        Crop[] allCrops = FindObjectsOfType<Crop>();
        
        int totalTrees = 0;
        int choppableTrees = 0;
        int immatureTrees = 0;
        int nonChoppableTrees = 0;
        
        foreach (Crop crop in allCrops)
        {
            // 检查是否在检查范围内
            float distance = Vector3.Distance(transform.position, crop.transform.position);
            if (distance > checkRadius) continue;
            
            TreeInfo treeInfo = AnalyzeTree(crop);
            if (treeInfo != null)
            {
                treeInfoList.Add(treeInfo);
                totalTrees++;
                
                if (treeInfo.canChop)
                {
                    choppableTrees++;
                }
                else if (!treeInfo.isMature)
                {
                    immatureTrees++;
                }
                else
                {
                    nonChoppableTrees++;
                }
            }
        }
        
        // 输出统计信息
        Debug.Log($"=== 树木砍伐检查结果 ===");
        Debug.Log($"检查范围: {checkRadius}m");
        Debug.Log($"总树木数量: {totalTrees}");
        Debug.Log($"可砍伐树木: {choppableTrees} (绿色)");
        Debug.Log($"未成熟树木: {immatureTrees} (黄色)");
        Debug.Log($"不可砍伐树木: {nonChoppableTrees} (红色)");
        
        if (logDetailedInfo)
        {
            LogDetailedTreeInfo();
        }
    }
    
    /// <summary>
    /// 分析单个树木的砍伐条件
    /// </summary>
    /// <param name="crop">作物组件</param>
    /// <returns>树木信息</returns>
    private TreeInfo AnalyzeTree(Crop crop)
    {
        if (crop.cropDetails == null) return null;
        
        // 检查是否是树木（通过是否有动画来判断，树木通常有倒下动画）
        if (!crop.cropDetails.hasAnimation) return null;
        
        TreeInfo treeInfo = new TreeInfo
        {
            treeObject = crop.gameObject,
            cropComponent = crop,
            cropDetails = crop.cropDetails,
            tileDetails = crop.tileDetails,
            position = crop.transform.position
        };
        
        // 检查是否成熟
        treeInfo.isMature = crop.canHarvest;
        
        if (!treeInfo.isMature)
        {
            treeInfo.canChop = false;
            treeInfo.reason = $"树木未成熟 (成长天数: {crop.tileDetails.growthDays}/{crop.cropDetails.TotalGrowthDays})";
            return treeInfo;
        }
        
        // 检查工具是否可用
        bool toolAvailable = crop.cropDetails.CheckToolAvailable(axeToolID);
        
        if (!toolAvailable)
        {
            treeInfo.canChop = false;
            treeInfo.reason = $"工具不匹配 (需要工具ID: {string.Join(",", crop.cropDetails.harvestToolItemID)}, 当前工具ID: {axeToolID})";
            return treeInfo;
        }
        
        // 所有条件都满足
        treeInfo.canChop = true;
        treeInfo.reason = $"可以砍伐 (需要砍伐次数: {crop.cropDetails.GetTotalRequireCount(axeToolID)})";
        
        return treeInfo;
    }
    
    /// <summary>
    /// 输出详细的树木信息
    /// </summary>
    private void LogDetailedTreeInfo()
    {
        Debug.Log("\n=== 详细树木信息 ===");
        
        foreach (TreeInfo tree in treeInfoList)
        {
            string status = tree.canChop ? "✅ 可砍伐" : 
                           !tree.isMature ? "⏳ 未成熟" : "❌ 不可砍伐";
            
            Debug.Log($"{status} | 位置: {tree.position} | 种子ID: {tree.cropDetails.seedItemID} | {tree.reason}");
        }
    }
    
    /// <summary>
    /// 检查指定位置的树木
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    /// <returns>树木信息，如果没有树木则返回null</returns>
    public TreeInfo CheckTreeAtPosition(Vector3 worldPosition)
    {
        // 使用物理检测查找指定位置的作物
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPosition);
        
        foreach (Collider2D collider in colliders)
        {
            Crop crop = collider.GetComponent<Crop>();
            if (crop != null)
            {
                TreeInfo treeInfo = AnalyzeTree(crop);
                if (treeInfo != null)
                {
                    if (logDetailedInfo)
                    {
                        string status = treeInfo.canChop ? "✅ 可砍伐" : 
                                       !treeInfo.isMature ? "⏳ 未成熟" : "❌ 不可砍伐";
                        Debug.Log($"位置 {worldPosition} 的树木状态: {status} | {treeInfo.reason}");
                    }
                    return treeInfo;
                }
            }
        }
        
        if (logDetailedInfo)
        {
            Debug.Log($"位置 {worldPosition} 没有找到树木");
        }
        return null;
    }
    
    /// <summary>
    /// 获取所有可砍伐的树木
    /// </summary>
    /// <returns>可砍伐的树木列表</returns>
    public List<TreeInfo> GetChoppableTrees()
    {
        List<TreeInfo> choppableTrees = new List<TreeInfo>();
        
        foreach (TreeInfo tree in treeInfoList)
        {
            if (tree.canChop)
            {
                choppableTrees.Add(tree);
            }
        }
        
        return choppableTrees;
    }
    
    /// <summary>
    /// 获取最近的可砍伐树木
    /// </summary>
    /// <param name="fromPosition">起始位置</param>
    /// <returns>最近的可砍伐树木，如果没有则返回null</returns>
    public TreeInfo GetNearestChoppableTree(Vector3 fromPosition)
    {
        TreeInfo nearestTree = null;
        float nearestDistance = float.MaxValue;
        
        foreach (TreeInfo tree in treeInfoList)
        {
            if (tree.canChop)
            {
                float distance = Vector3.Distance(fromPosition, tree.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTree = tree;
                }
            }
        }
        
        return nearestTree;
    }
    
    /// <summary>
    /// 在Scene视图中绘制检查结果
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmosInScene) return;
        
        // 绘制检查范围 - 使用兼容的方法
        Gizmos.color = Color.white;
        DrawWireCircle(transform.position, checkRadius);
        
        // 绘制树木状态
        foreach (TreeInfo tree in treeInfoList)
        {
            if (tree.canChop)
            {
                Gizmos.color = choppableColor;
            }
            else if (!tree.isMature)
            {
                Gizmos.color = immatureColor;
            }
            else
            {
                Gizmos.color = nonChoppableColor;
            }
            
            Gizmos.DrawWireCube(tree.position, Vector3.one * 0.5f);
            
            // 绘制状态图标
            Gizmos.DrawSphere(tree.position + Vector3.up * 2f, 0.2f);
        }
    }
    
    /// <summary>
    /// 更新斧头工具ID
    /// </summary>
    /// <param name="newAxeToolID">新的斧头工具ID</param>
    public void UpdateAxeToolID(int newAxeToolID)
    {
        axeToolID = newAxeToolID;
        Debug.Log($"斧头工具ID已更新为: {axeToolID}");
        
        // 重新检查所有树木
        CheckAllTrees();
    }
    
    /// <summary>
    /// 绘制线框圆圈 - 兼容不同Unity版本
    /// </summary>
    /// <param name="center">圆心位置</param>
    /// <param name="radius">半径</param>
    private void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32; // 圆圈分段数
        float angleStep = 360f / segments;
        
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
} 