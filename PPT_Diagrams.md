# PPT所需图表 - Mermaid语法

## 第3页：系统总体架构图

```mermaid
graph TB
    subgraph "UI层"
        A1[菜单系统]
        A2[背包界面]
        A3[交易界面]
        A4[HUD显示]
    end
    
    subgraph "控制层"
        B1[角色控制]
        B2[NPC控制]
        B3[动画管理]
        B4[交互系统]
    end
    
    subgraph "逻辑层"
        C1[A*寻路]
        C2[行为调度]
        C3[时间系统]
        C4[农场管理]
    end
    
    subgraph "数据层"
        D1[ScriptableObject]
        D2[存档系统]
        D3[配置管理]
    end
    
    A1 --> B1
    A2 --> B2
    A3 --> B3
    A4 --> B4
    
    B1 --> C1
    B2 --> C2
    B3 --> C3
    B4 --> C4
    
    C1 --> D1
    C2 --> D2
    C3 --> D3
    C4 --> D1
    
    style A1 fill:#e1f5fe
    style A2 fill:#e1f5fe
    style A3 fill:#e1f5fe
    style A4 fill:#e1f5fe
    style B1 fill:#f3e5f5
    style B2 fill:#f3e5f5
    style B3 fill:#f3e5f5
    style B4 fill:#f3e5f5
    style C1 fill:#e8f5e8
    style C2 fill:#e8f5e8
    style C3 fill:#e8f5e8
    style C4 fill:#e8f5e8
    style D1 fill:#fff3e0
    style D2 fill:#fff3e0
    style D3 fill:#fff3e0
```

## 第4页：A*寻路算法流程图

```mermaid
flowchart TD
    A[开始] --> B[初始化开放/关闭列表]
    B --> C[将起点加入开放列表]
    C --> D[从开放列表选择F值最小节点]
    D --> E{是否为目标点?}
    E -->|是| F[构建路径]
    E -->|否| G[将当前节点移至关闭列表]
    G --> H[检查相邻节点]
    H --> I[计算G、H、F值]
    I --> J[更新父节点信息]
    J --> K{开放列表是否为空?}
    K -->|否| D
    K -->|是| L[无路径]
    F --> M[结束]
    L --> M
    
    style A fill:#4caf50
    style M fill:#f44336
    style E fill:#ff9800
    style K fill:#ff9800
    style F fill:#2196f3
```

## 第5页：核心玩法循环图

```mermaid
graph LR
    A[玩家输入] --> B[角色移动]
    B --> C[工具使用]
    C --> D[环境交互]
    D --> E[种子播种]
    E --> F[作物生长]
    F --> G[作物收获]
    G --> H[资源获得]
    H --> I[背包管理]
    I --> J[商店交易]
    J --> K[资源配置]
    K --> L[策略规划]
    L --> A
    
    style A fill:#e3f2fd
    style E fill:#e8f5e8
    style F fill:#e8f5e8
    style G fill:#e8f5e8
    style I fill:#fff3e0
    style J fill:#fff3e0
```

## 第7页：系统集成架构图

```mermaid
graph TB
    subgraph "第一层"
        A[时间系统] --> B[NPC系统]
        B --> C[动画系统]
    end
    
    subgraph "第二层"
        D[农场系统] <--> E[寻路系统]
        E --> F[交互系统]
    end
    
    subgraph "第三层"
        G[背包系统] --> H[交易系统]
        H --> I[UI系统]
    end
    
    A --> D
    B --> E
    C --> F
    D --> G
    E --> H
    F --> I
    
    style A fill:#ffeb3b
    style B fill:#4caf50
    style C fill:#2196f3
    style D fill:#8bc34a
    style E fill:#ff9800
    style F fill:#9c27b0
    style G fill:#795548
    style H fill:#607d8b
    style I fill:#e91e63
```

## 第9页：技术路线图

```mermaid
gantt
    title 技术路线图
    dateFormat  X
    axisFormat %s
    
    section 需求分析
    市场调研     :done, des1, 0, 1
    用户分析     :done, des2, 0, 1
    
    section 系统设计
    架构设计     :done, des3, 1, 2
    模块划分     :done, des4, 1, 2
    
    section 原型开发
    MVP验证      :done, des5, 2, 3
    可行性       :done, des6, 2, 3
    
    section 核心实现
    功能开发     :active, des7, 3, 5
    系统集成     :des8, 4, 6
    
    section 测试优化
    性能调优     :des9, 5, 6
    Bug修复      :des10, 5, 6
    
    section 部署发布
    版本发布     :des11, 6, 7
    文档完善     :des12, 6, 7
```

## 第10页：开发时间轴

```mermaid
timeline
    title 16周开发时间轴
    
    section 第1-2周
        需求分析与设计阶段
        : 市场调研与竞品分析
        : 功能需求文档编写
        : 游戏设计文档制作
        : 技术架构设计
    
    section 第3-4周
        原型开发阶段
        : 核心玩法原型实现
        : 基础角色控制系统
        : 简单地图系统
        : 基础背包系统
    
    section 第5-8周
        核心功能实现
        : NPC AI系统开发
        : A*寻路算法实现
        : 时间调度系统
        : 农场经营系统
    
    section 第9-12周
        系统集成与优化
        : 各系统集成联调
        : 性能优化实施
        : UI/UX完善
        : 音效系统集成
    
    section 第13-14周
        测试与修复
        : 功能测试执行
        : Bug修复与优化
        : 多平台适配
        : 用户体验测试
    
    section 第15-16周
        发布准备
        : 文档完善
        : 演示视频制作
        : 最终版本打包
        : 答辩准备
```

## 第11页：Git工作流程图

```mermaid
gitgraph
    commit id: "Initial"
    branch develop
    checkout develop
    commit id: "Setup"
    branch feature/npc-ai
    checkout feature/npc-ai
    commit id: "A* Algorithm"
    commit id: "Behavior Tree"
    checkout develop
    merge feature/npc-ai
    branch feature/farming
    checkout feature/farming
    commit id: "Crop System"
    commit id: "Inventory"
    checkout develop
    merge feature/farming
    checkout main
    merge develop
    commit id: "Release v1.0"
```

## 第11页：测试金字塔图

```mermaid
graph TB
    subgraph "测试金字塔"
        A[UI测试<br/>5个用例]
        B[集成测试<br/>15个用例]
        C[单元测试<br/>15个用例]
    end
    
    A --> B
    B --> C
    
    style A fill:#ff5722
    style B fill:#ff9800
    style C fill:#4caf50
```

## 第13页：性能优化对比图

```mermaid
xychart-beta
    title "性能优化对比"
    x-axis [内存使用, CPU使用率, 帧率稳定性, 加载时间]
    y-axis "性能指标" 0 --> 100
    bar [100, 40, 45, 100]
    bar [35, 25, 60, 42]
```

## 第14页：测试覆盖范围饼图

```mermaid
pie title 测试用例分布
    "角色控制系统" : 5
    "NPC系统" : 5
    "物品背包系统" : 5
    "交易系统" : 5
    "时间环境系统" : 5
    "种植系统" : 5
    "性能测试" : 5
```

## 第15页：市场规模分析图

```mermaid
pie title 目标市场分析
    "独立游戏市场" : 1200
    "模拟经营类游戏" : 180
    "目标市场容量" : 180
```

## 第15页：技术产品化路线图

```mermaid
graph LR
    A[核心技术] --> B[插件开发]
    B --> C[商业授权]
    C --> D[平台服务]
    
    A --> E[开源贡献]
    B --> F[Asset Store]
    C --> G[技术咨询]
    D --> H[SaaS平台]
    
    style A fill:#4caf50
    style B fill:#2196f3
    style C fill:#ff9800
    style D fill:#9c27b0
    style E fill:#8bc34a
    style F fill:#03a9f4
    style G fill:#ffc107
    style H fill:#e91e63
```

## 第16页：项目目标达成情况图

```mermaid
xychart-beta
    title "项目目标达成情况"
    x-axis [完整游戏系统, NPC智能行为, 性能优化, 技术创新, 文档完善]
    y-axis "达成度(%)" 0 --> 100
    bar [100, 100, 100, 100, 100]
```

## 第16页：未来发展规划图

```mermaid
graph TB
    A[当前项目] --> B[短期计划<br/>6个月内]
    A --> C[中长期规划<br/>1-2年]
    
    B --> D[多人联机功能]
    B --> E[移动端适配]
    B --> F[机器学习集成]
    B --> G[云存档系统]
    
    C --> H[深度学习AI]
    C --> I[开放世界技术]
    C --> J[社交系统]
    C --> K[程序化生成]
    
    style A fill:#4caf50
    style B fill:#2196f3
    style C fill:#ff9800
    style D fill:#e3f2fd
    style E fill:#e3f2fd
    style F fill:#e3f2fd
    style G fill:#e3f2fd
    style H fill:#fff3e0
    style I fill:#fff3e0
    style J fill:#fff3e0
    style K fill:#fff3e0
```

## 第6页：NPC行为状态机图

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Walking : 收到移动指令
    Walking --> Working : 到达工作地点
    Working --> Walking : 工作完成
    Walking --> Idle : 到达目标点
    Idle --> Sleeping : 夜晚时间
    Sleeping --> Idle : 白天时间
    Working --> Idle : 任务中断
    
    state Working {
        [*] --> Farming
        Farming --> Harvesting
        Harvesting --> [*]
    }
```

## 第8页：NPC系统架构图

```mermaid
graph TB
    subgraph "NPC AI系统架构"
        A[感知层<br/>Perception Layer]
        B[决策层<br/>Decision Layer]
        C[行为层<br/>Behavior Layer]
        D[执行层<br/>Execution Layer]
    end
    
    A --> B
    B --> C
    C --> D
    
    subgraph "感知模块"
        A1[环境感知]
        A2[时间感知]
        A3[玩家感知]
    end
    
    subgraph "决策模块"
        B1[行为树]
        B2[状态机]
        B3[调度系统]
    end
    
    subgraph "行为模块"
        C1[寻路行为]
        C2[工作行为]
        C3[交互行为]
    end
    
    subgraph "执行模块"
        D1[移动控制]
        D2[动画控制]
        D3[音效控制]
    end
    
    A --> A1
    A --> A2
    A --> A3
    B --> B1
    B --> B2
    B --> B3
    C --> C1
    C --> C2
    C --> C3
    D --> D1
    D --> D2
    D --> D3
    
    style A fill:#e3f2fd
    style B fill:#f3e5f5
    style C fill:#e8f5e8
    style D fill:#fff3e0
```

## 第12页：项目规模统计图

```mermaid
xychart-beta
    title "项目规模统计"
    x-axis [代码行数, 功能模块, 资源文件, 测试用例, 开发周期]
    y-axis "数量" 0 --> 16000
    bar [15000, 12, 500, 35, 16]
```

## 第13页：A*寻路性能对比图

```mermaid
xychart-beta
    title "A*寻路系统性能达成情况"
    x-axis [简单路径计算, 复杂路径计算, 多NPC同时寻路, 动态避障响应]
    y-axis "性能倍数" 0 --> 900
    line [240, 833, 156, 476]
```

## 第14页：帧率稳定性分布图

```mermaid
pie title 帧率稳定性测试结果
    "60FPS" : 95
    "50-59FPS" : 4
    "<50FPS" : 1
```

## 第14页：内存使用趋势图

```mermaid
xychart-beta
    title "内存使用趋势"
    x-axis [初始, 1小时, 2小时, 3小时, 4小时]
    y-axis "内存使用(MB)" 150 --> 250
    line [180, 200, 210, 230, 245]
```

## 螺旋搜索算法示意图

```mermaid
graph TB
    subgraph "螺旋搜索模式"
        A[起始点<br/>0,0] --> B[右<br/>1,0]
        B --> C[上<br/>1,1]
        C --> D[左<br/>0,1]
        D --> E[左<br/>-1,1]
        E --> F[下<br/>-1,0]
        F --> G[下<br/>-1,-1]
        G --> H[右<br/>0,-1]
        H --> I[右<br/>1,-1]
        I --> J[右<br/>2,-1]
        J --> K[继续螺旋...]
    end
    
    style A fill:#4caf50
    style B fill:#2196f3
    style C fill:#2196f3
    style D fill:#ff9800
    style E fill:#ff9800
    style F fill:#f44336
    style G fill:#f44336
    style H fill:#9c27b0
    style I fill:#9c27b0
    style J fill:#9c27b0
```

## 对象池系统工作原理图

```mermaid
graph LR
    subgraph "对象池系统"
        A[对象池<br/>Object Pool] 
        B[活跃对象<br/>Active Objects]
        C[非活跃对象<br/>Inactive Objects]
    end
    
    D[请求对象] --> A
    A --> E{池中有对象?}
    E -->|是| F[从池中取出]
    E -->|否| G[创建新对象]
    F --> B
    G --> B
    B --> H[使用完毕]
    H --> I[返回池中]
    I --> C
    C --> A
    
    style A fill:#4caf50
    style B fill:#2196f3
    style C fill:#ff9800
    style D fill:#e3f2fd
    style H fill:#f3e5f5
``` 