# NPC自主耕种系统 - 修复版本

## 更新说明

此版本修复了以下问题：
1. ✅ **修复NPC移动瞬间闪现问题** - 降低移动速度，使用平滑移动
2. ✅ **修复动画闪烁问题** - 改进动画播放和停止逻辑
3. ✅ **修复只在第一天耕种问题** - 添加休息状态和持续耕种机制

## 系统概述

NPC自主耕种系统允许多个NPC自动执行完整的耕种流程：
- 🔍 **智能地块搜索** - 螺旋搜索算法，优先搜索近距离地块
- 🚶 **平滑移动** - 使用改进的移动算法，避免瞬间传送
- 🛠️ **完整耕种流程** - 挖地→种植→浇水
- 🎭 **动画系统** - 根据朝向播放对应的工作动画
- 👥 **多NPC协调** - 避免重复劳动，智能任务分配
- 🌱 **季节性种植** - 根据季节选择合适的种子
- ⏰ **持续工作** - 自动休息后重新开始耕种

## Unity设置步骤

### 1. 设置NPCFarmingManager

在场景中创建管理器：
1. 创建空物体，命名为 "NPCFarmingManager"
2. 添加 `NPCFarmingManager` 脚本
3. 在Inspector中配置以下参数：

**管理设置：**
- `Task Update Interval`: 5（任务更新间隔，秒）
- `Max Simultaneous Farmers`: 3（最大同时耕种NPC数量）

**搜索优化设置：**
- `Default Search Radius`: 8（默认搜索半径）
- `Max Search Radius`: 15（最大搜索半径）
- `Enable Smart Search`: ✓（启用智能搜索）

**种子配置示例：**
```
Element 0:
  Seed ID: 1002
  Seed Name: "马铃薯种子"
  Priority: 8
  Preferred Season: 春天

Element 1:
  Seed ID: 1005
  Seed Name: "辣椒种子"  
  Priority: 9
  Preferred Season: 夏天

Element 2:
  Seed ID: 1009
  Seed Name: "南瓜种子"
  Priority: 8
  Preferred Season: 秋天
```

### 2. 设置NPC

为每个需要耕种的NPC添加组件：
1. 选择NPC GameObject
2. 添加 `NPCFarmingTask` 脚本
3. 配置以下参数：

**耕种设置：**
- `Preferred Seed ID`: 1001（偏好种子ID）
- `Search Radius`: 10（搜索半径）
- `Working Time`: 2（工作时间，秒）

**移动设置：**
- `Move Speed`: 1.5（移动速度，降低避免瞬移）
- `Arrival Threshold`: 0.8（到达目标距离阈值）

**动画设置：**
需要拖拽对应的动画片段：
- `Hoe Animation Down/Left/Right/Up`（锄头动画）
- `Water Animation Down/Left/Right/Up`（浇水动画）
- `Blank Animation Clip`（空白动画片段）

**调试设置：**
- `Enable Daily Farming`: ✓（启用每日耕种）
- `Rest Time After Complete`: 8（完成后休息时间）
- `Rest Time After Failed`: 15（失败后休息时间）

### 3. 配置地图

确保在 `GridMapManager` 中正确设置可耕种区域：
1. 在场景的瓦片地图中标记可耕种区域
2. 确保地块的 `canDig` 属性为 `true`
3. 设置合适的地块尺寸和边界

## 新增功能

### 1. 休息机制
- NPC完成任务后进入休息状态
- 可配置休息时间（完成后/失败后）
- 休息结束后自动重新开始耕种

### 2. 改进的移动系统
- 降低移动速度避免瞬移（可配置）
- 平滑移动算法
- 可配置的到达阈值
- 移动超时保护

### 3. 动画修复
- 修复动画闪烁问题
- 改进的动画播放逻辑
- 更好的动画状态管理

### 4. 状态管理改进
- 添加休息状态
- 更智能的状态转换
- 防止无限循环

## 代码使用示例

### 启动NPC耕种
```csharp
// 获取NPC组件
NPCFarmingTask npcTask = npc.GetComponent<NPCFarmingTask>();

// 设置种子偏好
npcTask.SetPreferredSeed(1005); // 辣椒种子

// 设置搜索半径
npcTask.SetSearchRadius(12f);

// 启动耕种任务
npcTask.StartFarmingTask();
```

### 管理多个NPC
```csharp
// 获取管理器
NPCFarmingManager manager = NPCFarmingManager.Instance;

// 添加NPC到管理器
manager.AddFarmingNPC(npcTask);

// 设置最大同时工作NPC数量
manager.SetMaxSimultaneousFarmers(5);

// 启用/禁用所有NPC的每日耕种
manager.SetAllNPCDailyFarmingEnabled(true);

// 停止所有NPC的耕种任务
manager.StopAllFarmingTasks();
```

### 单个NPC控制
```csharp
// 启用/禁用每日耕种
npcTask.SetDailyFarmingEnabled(true);

// 立即停止当前任务
npcTask.StopCurrentTask();

// 获取当前状态
NPCFarmingTask.TaskState currentState = npcTask.GetCurrentState();
```

## 故障排除

### 问题：NPC移动太快/瞬移
**解决方案：**
- 调低 `Move Speed` 参数（推荐1.0-2.0）
- 检查 `Arrival Threshold` 设置（推荐0.5-1.0）

### 问题：动画闪烁
**解决方案：**
- 确保所有动画片段都已正确拖拽到Inspector
- 检查 `Blank Animation Clip` 是否设置
- 确认Animator Controller包含 `EventAnimation` 参数

### 问题：NPC不持续耕种
**解决方案：**
- 确保 `Enable Daily Farming` 已勾选
- 检查 `Rest Time After Complete` 设置
- 查看Console确认是否有错误信息

### 问题：NPC找不到可耕种地块
**解决方案：**
- 增加 `Search Radius` 参数
- 检查地图中是否有足够的可耕种区域
- 确认 `GridMapManager` 中地块的 `canDig` 属性

### 问题：多个NPC冲突
**解决方案：**
- 调整 `Max Simultaneous Farmers` 参数
- 启用 `Enable Smart Search` 选项
- 增加NPC之间的初始距离

## 性能优化建议

1. **合理设置更新间隔** - `taskUpdateInterval` 不要设置太小
2. **限制搜索半径** - 避免过大的搜索范围
3. **控制同时工作NPC数量** - 根据硬件性能调整
4. **使用智能搜索** - 启用智能搜索优化

## 调试技巧

1. **Console日志** - 查看详细的调试信息
2. **状态监控** - 观察NPC状态变化
3. **地块占用** - 监控地块占用情况
4. **性能分析** - 使用Unity Profiler检查性能

## 更新历史

### v2.0 - 修复版本
- ✅ 修复移动瞬移问题
- ✅ 修复动画闪烁问题  
- ✅ 修复持续耕种问题
- ✅ 添加休息机制
- ✅ 改进状态管理
- ✅ 优化性能

### v1.0 - 初始版本
- ✅ 基础耕种功能
- ✅ 多NPC协调
- ✅ 季节性种植
- ✅ A*寻路集成 

## 动画配置指南

### 🚨 重要：解决动画闪烁问题

如果你遇到NPC耕种和浇水时动画闪烁的问题，这是因为动画控制器配置不正确。现在提供一个完全改进的解决方案：

#### 🎯 解决方案：工具显示系统（推荐 - 简单有效）

这种方法不修改NPC的基础动画，而是通过显示/隐藏工具精灵来实现视觉效果。

**第一步：为NPC添加工具显示组件**

1. 选择你的NPC GameObject
2. 为锄头工具创建子对象：
   - 右键NPC → Create Empty
   - 命名为 "HoeTool"
   - 添加 `Sprite Renderer` 组件
   - 设置Layer Order为较高值（如5），确保工具在NPC前面显示

3. 为浇水工具创建子对象：
   - 右键NPC → Create Empty  
   - 命名为 "WaterTool"
   - 添加 `Sprite Renderer` 组件
   - 设置Layer Order为较高值（如5）

**第二步：获取工具精灵图**

在Project窗口中找到工具精灵图：
- **锄头精灵**：`Assets/M Studio/Art/Items/Tools/` 或类似路径
- **浇水壶精灵**：`Assets/M Studio/Art/Items/Tools/` 或类似路径

**第三步：配置NPCFarmingTask组件**

1. 选择带有`NPCFarmingTask`脚本的NPC
2. 在Inspector窗口中找到**"工具显示系统"**部分
3. 进行以下设置：

**工具Renderer设置：**
- `Hoe Tool Renderer` ← 拖拽 HoeTool 的 Sprite Renderer 组件
- `Water Tool Renderer` ← 拖拽 WaterTool 的 Sprite Renderer 组件

**工具精灵设置：**
- `Hoe Tool Sprite` ← 拖拽锄头工具的精灵图
- `Water Tool Sprite` ← 拖拽浇水壶工具的精灵图

**第四步：调整工具位置**

1. 选择 HoeTool 子对象
2. 在Scene视图中调整其Transform位置，使其看起来像NPC在手持工具
3. 对 WaterTool 做同样的调整

**第五步：初始状态设置**

确保工具初始时是隐藏的：
1. 选择 HoeTool → Inspector → Sprite Renderer → 取消勾选 ✓ (disabled)
2. 选择 WaterTool → Inspector → Sprite Renderer → 取消勾选 ✓ (disabled)

#### 动画工作原理

改进后的系统工作方式：
1. **正常状态**：NPC使用默认动画控制器，所有工具隐藏
2. **挖地时**：显示锄头工具，NPC播放EventAnimation状态
3. **浇水时**：显示浇水工具，NPC播放EventAnimation状态  
4. **完成后**：隐藏所有工具，恢复正常动画

#### 优势

这个新方案的优势：
- ✅ **不修改基础动画**：NPC保持正常的行走和静止动画
- ✅ **简单设置**：只需要设置工具的Sprite Renderer
- ✅ **视觉效果好**：工具在正确的时机显示/隐藏
- ✅ **兼容性强**：适用于任何NPC控制器
- ✅ **易于调试**：可以直接在Scene视图中看到工具位置

#### 常见问题解决

**问题：工具位置不正确**
- 在Scene视图中调整工具子对象的Transform位置
- 确保工具看起来像是NPC在手持

**问题：工具不显示**
- 检查Sprite Renderer组件是否正确设置
- 确认工具精灵图已正确拖拽到相应字段
- 检查Layer Order，确保工具在NPC前面

**问题：工具显示时机不对**
- 检查Console中的日志信息
- 确认NPCFarmingTask组件中的工具Renderer引用正确

**问题：NPC动画仍然有问题**
- 确保NPC的Animator Controller包含EventAnimation参数
- 检查NPC控制器是否有EventAnimationClip状态

### 3. 配置地图

确保在 `GridMapManager` 中正确设置可耕种区域：
1. 在场景的瓦片地图中标记可耕种区域
2. 确保地块的 `canDig` 属性为 `true`
3. 设置合适的地块尺寸和边界

## 新增功能

### 1. 休息机制
- NPC完成任务后进入休息状态
- 可配置休息时间（完成后/失败后）
- 休息结束后自动重新开始耕种

### 2. 改进的移动系统
- 降低移动速度避免瞬移（可配置）
- 平滑移动算法
- 可配置的到达阈值
- 移动超时保护

### 3. 动画修复
- 修复动画闪烁问题
- 改进的动画播放逻辑
- 更好的动画状态管理

### 4. 状态管理改进
- 添加休息状态
- 更智能的状态转换
- 防止无限循环

## 代码使用示例

### 启动NPC耕种
```csharp
// 获取NPC组件
NPCFarmingTask npcTask = npc.GetComponent<NPCFarmingTask>();

// 设置种子偏好
npcTask.SetPreferredSeed(1005); // 辣椒种子

// 设置搜索半径
npcTask.SetSearchRadius(12f);

// 启动耕种任务
npcTask.StartFarmingTask();
```

### 管理多个NPC
```csharp
// 获取管理器
NPCFarmingManager manager = NPCFarmingManager.Instance;

// 添加NPC到管理器
manager.AddFarmingNPC(npcTask);

// 设置最大同时工作的NPC数量
manager.maxSimultaneousFarmers = 3;

// 配置季节性种子
manager.UpdateSeasonalPreferences(Season.夏天);
```

## 调试和排错

### 常见问题

1. **NPC找不到可耕种地块**
   - 检查搜索半径是否足够大
   - 确认地图中有可用的耕种区域
   - 查看Console中的调试信息

2. **NPC移动缓慢或卡住**
   - 调整移动速度设置
   - 检查A*寻路网格设置
   - 确认没有障碍物阻挡路径

3. **动画闪烁或不显示**
   - 按照上面的**动画配置指南**正确设置工具动画控制器
   - 检查Animator组件参数设置
   - 确认动画控制器路径正确

4. **多个NPC冲突**
   - 调整最大同时工作NPC数量
   - 检查地块占用管理
   - 查看NPCFarmingManager的调试信息

### 调试工具

使用以下方法进行调试：

```csharp
// 检查NPC当前状态
Debug.Log($"NPC状态: {npcTask.GetCurrentState()}");

// 启用详细日志
npcTask.enableDailyFarming = true;

// 检查管理器状态
Debug.Log($"活跃农民数量: {NPCFarmingManager.Instance.farmingNPCs.Count}");
```

## 最佳实践

1. **性能优化**
   - 合理设置搜索半径（推荐8-12）
   - 限制同时工作的NPC数量
   - 使用任务更新间隔控制CPU使用

2. **动画流畅性**
   - 正确配置工具动画控制器
   - 确保动画过渡自然
   - 使用适当的工作时间设置

3. **游戏平衡**
   - 根据游戏难度调整种子优先级
   - 设置合理的休息时间
   - 考虑季节性种植策略

4. **维护性**
   - 定期检查Console中的警告信息
   - 备份动画配置
   - 记录自定义参数设置 