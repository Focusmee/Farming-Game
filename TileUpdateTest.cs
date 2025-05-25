using UnityEngine;
using MFarm.Map;

/// <summary>
/// NPC瓦片更新系统测试脚本
/// 用于验证NPC执行耕种和浇水操作后瓦片是否正确更新
/// </summary>
public class TileUpdateTest : MonoBehaviour
{
    [Header("测试设置")]
    public Vector2Int testPosition = new Vector2Int(0, 0); // 测试位置
    public bool testDigTile = false; // 测试挖地瓦片
    public bool testWaterTile = false; // 测试浇水瓦片
    
    private void Update()
    {
        // 按键测试
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestTileUpdate();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTestTile();
        }
    }
    
    /// <summary>
    /// 测试瓦片更新功能
    /// </summary>
    private void TestTileUpdate()
    {
        Debug.Log("开始测试瓦片更新系统...");
        
        // 创建测试用的TileDetails
        TileDetails testTile = new TileDetails
        {
            gridX = testPosition.x,
            gridY = testPosition.y,
            canDig = true,
            daysSinceDug = -1,
            daysSinceWatered = -1,
            seedItemID = -1
        };
        
        var gridMapManager = GridMapManager.Instance;
        if (gridMapManager == null)
        {
            Debug.LogError("GridMapManager实例不存在！");
            return;
        }
        
        // 测试挖地瓦片
        if (testDigTile)
        {
            testTile.daysSinceDug = 0;
            testTile.canDig = false;
            
            gridMapManager.SetDigGroundForNPC(testTile);
            Debug.Log($"测试挖地瓦片更新：位置({testPosition.x}, {testPosition.y})");
        }
        
        // 测试浇水瓦片
        if (testWaterTile)
        {
            testTile.daysSinceWatered = 0;
            
            gridMapManager.SetWaterGroundForNPC(testTile);
            Debug.Log($"测试浇水瓦片更新：位置({testPosition.x}, {testPosition.y})");
        }
        
        // 更新地块信息
        gridMapManager.UpdateTileDetails(testTile);
        
        Debug.Log("瓦片更新测试完成！");
    }
    
    /// <summary>
    /// 重置测试地块
    /// </summary>
    private void ResetTestTile()
    {
        Debug.Log("重置测试地块...");
        
        TileDetails resetTile = new TileDetails
        {
            gridX = testPosition.x,
            gridY = testPosition.y,
            canDig = true,
            daysSinceDug = -1,
            daysSinceWatered = -1,
            seedItemID = -1
        };
        
        var gridMapManager = GridMapManager.Instance;
        if (gridMapManager != null)
        {
            gridMapManager.UpdateTileDetails(resetTile);
            Debug.Log($"测试地块已重置：位置({testPosition.x}, {testPosition.y})");
        }
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("NPC瓦片更新测试");
        GUILayout.Label("按 T 键测试瓦片更新");
        GUILayout.Label("按 R 键重置测试地块");
        GUILayout.Label($"测试位置: ({testPosition.x}, {testPosition.y})");
        GUILayout.Label($"测试挖地: {testDigTile}");
        GUILayout.Label($"测试浇水: {testWaterTile}");
        GUILayout.EndArea();
    }
} 