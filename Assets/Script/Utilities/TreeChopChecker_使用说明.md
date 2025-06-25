# 树木砍伐检查工具使用说明

## 概述
`TreeChopChecker` 是一个用于检查地图上树木是否符合砍伐条件的工具。它可以帮助你快速识别哪些树木可以砍伐，哪些还未成熟，以及哪些不符合砍伐条件。

## 功能特性

### 🔍 检查功能
- **自动检查**: 启动时自动检查范围内的所有树木
- **手动检查**: 通过右键菜单或代码调用检查
- **单点检查**: 检查指定位置的树木状态
- **范围检查**: 在指定半径内检查所有树木

### 📊 状态分类
- **✅ 可砍伐**: 成熟且工具匹配的树木 (绿色)
- **⏳ 未成熟**: 还在成长中的树木 (黄色)  
- **❌ 不可砍伐**: 工具不匹配或其他原因 (红色)

### 🎨 可视化显示
- **Scene视图**: 在Scene视图中用不同颜色的方框显示树木状态
- **控制台输出**: 详细的文字信息输出
- **统计信息**: 显示各类树木的数量统计

## Unity编辑器设置

### 1. 添加组件
1. 在Hierarchy中创建一个空的GameObject
2. 重命名为 "TreeChopChecker"
3. 添加 `TreeChopChecker` 组件

### 2. 配置参数

#### 检查设置
- **Show Gizmos In Scene**: 是否在Scene视图显示检查结果
- **Log Detailed Info**: 是否在控制台输出详细信息
- **Check Radius**: 检查范围半径 (默认20米)

#### 工具ID设置
- **Axe Tool ID**: 斧头工具ID (默认1001，对应游戏中的斧头)

#### 颜色设置
- **Choppable Color**: 可砍伐树木颜色 (默认绿色)
- **Non Choppable Color**: 不可砍伐树木颜色 (默认红色)
- **Immature Color**: 未成熟树木颜色 (默认黄色)

## 使用方法

### 方法1: 自动检查
组件启动时会自动检查一次范围内的所有树木。

### 方法2: 手动检查
在Inspector中右键点击组件，选择 "检查所有树木"。

### 方法3: 代码调用
```csharp
// 获取TreeChopChecker组件
TreeChopChecker checker = FindObjectOfType<TreeChopChecker>();

// 检查所有树木
checker.CheckAllTrees();

// 检查指定位置的树木
TreeChopChecker.TreeInfo treeInfo = checker.CheckTreeAtPosition(worldPosition);

// 获取所有可砍伐的树木
List<TreeChopChecker.TreeInfo> choppableTrees = checker.GetChoppableTrees();

// 获取最近的可砍伐树木
TreeChopChecker.TreeInfo nearestTree = checker.GetNearestChoppableTree(playerPosition);
```

## 输出信息解读

### 控制台统计信息
```
=== 树木砍伐检查结果 ===
检查范围: 20m
总树木数量: 15
可砍伐树木: 8 (绿色)
未成熟树木: 5 (黄色)
不可砍伐树木: 2 (红色)
```

### 详细树木信息
```
✅ 可砍伐 | 位置: (10.5, 5.2, 0) | 种子ID: 1026 | 可以砍伐 (需要砍伐次数: 20)
⏳ 未成熟 | 位置: (8.3, 7.1, 0) | 种子ID: 1026 | 树木未成熟 (成长天数: 45/68)
❌ 不可砍伐 | 位置: (12.1, 3.8, 0) | 种子ID: 1028 | 工具不匹配 (需要工具ID: 1001, 当前工具ID: 1002)
```

## Scene视图可视化

### 显示元素
- **白色圆圈**: 检查范围边界
- **彩色方框**: 树木位置和状态
  - 绿色: 可砍伐
  - 黄色: 未成熟
  - 红色: 不可砍伐
- **小球**: 树木上方的状态指示器

### 查看方式
1. 选中TreeChopChecker游戏对象
2. 在Scene视图中查看可视化结果
3. 可以通过 "Show Gizmos In Scene" 开关控制显示

## 树木砍伐条件

### 成熟条件
树木必须达到完全成熟状态：
- `tileDetails.growthDays >= cropDetails.TotalGrowthDays`

### 工具匹配
必须使用正确的砍伐工具：
- 针叶树 (种子ID: 1026): 需要斧头 (工具ID: 1001)
- 橡树 (种子ID: 1028): 需要斧头 (工具ID: 1001)

### 砍伐次数
不同树木需要不同的砍伐次数：
- 针叶树: 20次
- 橡树: 15次

## 常见问题

### Q: 为什么有些树木显示为红色？
A: 可能的原因：
1. 工具ID不匹配
2. 树木数据配置错误
3. 树木组件缺失

### Q: 如何修改斧头工具ID？
A: 
1. 在Inspector中修改 "Axe Tool ID" 参数
2. 或通过代码调用 `UpdateAxeToolID(newID)`

### Q: 检查范围太小怎么办？
A: 增大 "Check Radius" 参数值

### Q: 控制台信息太多怎么办？
A: 关闭 "Log Detailed Info" 选项

## 扩展功能

### 自定义检查逻辑
可以修改 `AnalyzeTree` 方法来添加自定义的检查条件。

### 集成到NPC系统
可以将此工具集成到NPC砍伐系统中，让NPC自动寻找可砍伐的树木。

### 添加新的树木类型
在 `AnalyzeTree` 方法中添加对新树木类型的支持。

## 注意事项

1. **性能考虑**: 大范围检查可能影响性能，建议合理设置检查半径
2. **数据同步**: 确保CropDetails数据与实际游戏配置一致
3. **工具ID**: 确保斧头工具ID与游戏中的实际ID匹配
4. **场景切换**: 切换场景后需要重新检查树木状态 