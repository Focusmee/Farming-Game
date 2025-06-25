using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简化版树木砍伐检查工具
/// 用于检查地图上的树木是否符合砍伐条件
/// </summary>
public class SimpleTreeChecker : MonoBehaviour
{
    [Header("检查设置")]
    [Tooltip("检查范围半径")]
    public float checkRadius = 20f;
    
    [Tooltip("斧头工具ID")]
    public int axeToolID = 1001;
    
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;
    
    [Header("可视化颜色")]
    public Color choppableColor = Color.green;
    public Color immatureColor = Color.yellow;
    public Color nonChoppableColor = Color.red;
    
    private List<TreeData> treeList = new List<TreeData>();
    
    [System.Serializable]
    public class TreeData
    {
        public GameObject treeObject;
        public Vector3 position;
        public bool canChop;
        public bool isMature;
        public string status;
        public int seedID;
    }
    
    private void Start()
    {
        // 启动时检查一次
        CheckTrees();
    }
    
    /// <summary>
    /// 检查所有树木
    /// </summary>
    [ContextMenu("检查树木")]
    public void CheckTrees()
    {
        treeList.Clear();
        
        // 查找所有Crop组件
        var allCrops = FindObjectsOfType<MonoBehaviour>();
        
        int totalTrees = 0;
        int choppable = 0;
        int immature = 0;
        int nonChoppable = 0;
        
        foreach (var obj in allCrops)
        {
            // 检查距离
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance > checkRadius) continue;
            
            // 尝试获取Crop组件
            var crop = obj.GetComponent<MonoBehaviour>();
            if (crop == null) continue;
            
            // 检查是否是树木（通过名称或标签）
            if (!IsTree(obj.gameObject)) continue;
            
            TreeData treeData = AnalyzeTreeSimple(obj.gameObject);
            if (treeData != null)
            {
                treeList.Add(treeData);
                totalTrees++;
                
                if (treeData.canChop) choppable++;
                else if (!treeData.isMature) immature++;
                else nonChoppable++;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"=== 简化版树木检查结果 ===");
            Debug.Log($"检查范围: {checkRadius}m");
            Debug.Log($"总树木数量: {totalTrees}");
            Debug.Log($"可砍伐: {choppable} | 未成熟: {immature} | 不可砍伐: {nonChoppable}");
            
            foreach (var tree in treeList)
            {
                string icon = tree.canChop ? "✅" : !tree.isMature ? "⏳" : "❌";
                Debug.Log($"{icon} {tree.treeObject.name} | 位置: {tree.position} | {tree.status}");
            }
        }
    }
    
    /// <summary>
    /// 简单的树木分析
    /// </summary>
    private TreeData AnalyzeTreeSimple(GameObject treeObj)
    {
        TreeData data = new TreeData
        {
            treeObject = treeObj,
            position = treeObj.transform.position
        };
        
        // 简单的成熟度检查（基于名称或其他简单条件）
        string name = treeObj.name.ToLower();
        
        if (name.Contains("tree") || name.Contains("木"))
        {
            // 假设所有找到的树木都是成熟的（简化版本）
            data.isMature = true;
            data.canChop = true;
            data.status = "可以砍伐";
            data.seedID = 1026; // 默认种子ID
        }
        else
        {
            data.isMature = false;
            data.canChop = false;
            data.status = "未知类型";
        }
        
        return data;
    }
    
    /// <summary>
    /// 检查是否是树木
    /// </summary>
    private bool IsTree(GameObject obj)
    {
        string name = obj.name.ToLower();
        return name.Contains("tree") || 
               name.Contains("木") || 
               name.Contains("crop") ||
               obj.CompareTag("Tree");
    }
    
    /// <summary>
    /// 获取可砍伐的树木列表
    /// </summary>
    public List<TreeData> GetChoppableTrees()
    {
        List<TreeData> result = new List<TreeData>();
        foreach (var tree in treeList)
        {
            if (tree.canChop)
            {
                result.Add(tree);
            }
        }
        return result;
    }
    
    /// <summary>
    /// 获取最近的可砍伐树木
    /// </summary>
    public TreeData GetNearestChoppableTree(Vector3 fromPosition)
    {
        TreeData nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var tree in treeList)
        {
            if (tree.canChop)
            {
                float distance = Vector3.Distance(fromPosition, tree.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = tree;
                }
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// 在Scene视图中绘制
    /// </summary>
    private void OnDrawGizmos()
    {
        // 绘制检查范围 - 使用兼容的方法
        Gizmos.color = Color.white;
        DrawWireCircle(transform.position, checkRadius);
        
        // 绘制树木
        foreach (var tree in treeList)
        {
            if (tree.canChop)
                Gizmos.color = choppableColor;
            else if (!tree.isMature)
                Gizmos.color = immatureColor;
            else
                Gizmos.color = nonChoppableColor;
            
            Gizmos.DrawWireCube(tree.position, Vector3.one * 0.5f);
            Gizmos.DrawSphere(tree.position + Vector3.up * 1.5f, 0.15f);
        }
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