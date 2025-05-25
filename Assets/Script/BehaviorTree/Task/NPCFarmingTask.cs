using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MFarm.Map;
using MFarm.AStar;
using MFarm.CropPlant;

/// <summary>
/// NPC耕种任务行为树节点
/// 包含寻找可耕种地块、导航到目标位置、执行耕种操作的完整流程
/// </summary>
public class NPCFarmingTask : MonoBehaviour
{
    [Header("耕种设置")]
    public int preferredSeedID = 1001; // NPC偏好的种子ID
    public float searchRadius = 10f; // 搜索可耕种地块的半径
    public float workingTime = 2f; // 执行耕种操作的时间
    
    [Header("移动设置")]
    public float moveSpeed = 1.5f; // NPC移动速度，降低避免瞬移
    public float arrivalThreshold = 0.8f; // 到达目标的距离阈值
    
    [Header("工具设置")]
    public int hoeToolID = 2001; // 锄头工具ID
    public int waterToolID = 2002; // 浇水工具ID
    
    [Header("工具动画系统 - Animation Clip方案")]
    [Tooltip("锄头工具上方向动画")]
    public AnimationClip hoeToolUpClip;
    [Tooltip("锄头工具下方向动画")]
    public AnimationClip hoeToolDownClip;
    [Tooltip("锄头工具左方向动画")]
    public AnimationClip hoeToolLeftClip;
    [Tooltip("锄头工具右方向动画")]
    public AnimationClip hoeToolRightClip;
    
    [Tooltip("浇水工具上方向动画")]
    public AnimationClip waterToolUpClip;
    [Tooltip("浇水工具下方向动画")]
    public AnimationClip waterToolDownClip;
    [Tooltip("浇水工具左方向动画")]
    public AnimationClip waterToolLeftClip;
    [Tooltip("浇水工具右方向动画")]
    public AnimationClip waterToolRightClip;
    
    [Header("动画设置")]
    [Tooltip("工具动画播放速度倍率")]
    public float toolAnimationSpeedMultiplier = 1f;
    [Tooltip("是否在动画结束后自动恢复到待机状态")]
    public bool autoReturnToIdle = true;
    
    [Header("调试设置")]
    public bool enableDailyFarming = true; // 是否启用每日耕种
    public float restTimeAfterComplete = 8f; // 完成任务后的休息时间
    public float restTimeAfterFailed = 15f; // 失败后的休息时间
    
    private NPCMovement npcMovement;
    private Animator animator; // NPC动画控制器
    private Stack<MovementStep> movementStack;
    private Vector2Int currentTarget;
    private TaskState currentState;
    private float workTimer;
    private bool isPlayingWorkAnimation; // 是否正在播放工作动画
    private Vector3 targetWorldPosition; // 目标世界坐标
    private bool isMovingToTarget; // 是否正在移动到目标
    
    // 动画状态跟踪
    private string currentPlayingClipName; // 当前播放的动画片段名称
    private float toolAnimationStartTime; // 工具动画开始时间
    private Vector2 currentFacingDirection; // 当前面朝方向
    
    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskState
    {
        SearchingTarget,    // 搜索目标地块
        MovingToTarget,     // 移动到目标地块
        Digging,           // 挖地
        Planting,          // 种植
        Watering,          // 浇水
        Completed,         // 任务完成
        Failed,            // 任务失败
        Resting            // 休息状态
    }
    
    /// <summary>
    /// 方向枚举，用于选择对应的动画片段
    /// </summary>
    public enum FacingDirection
    {
        Up,
        Down, 
        Left,
        Right
    }
    
    private void Awake()
    {
        npcMovement = GetComponent<NPCMovement>();
        movementStack = new Stack<MovementStep>();
        currentState = TaskState.SearchingTarget;
        animator = GetComponent<Animator>();
    }
    
    /// <summary>
    /// 开始执行耕种任务
    /// </summary>
    public void StartFarmingTask()
    {
        // 检查是否应该执行耕种任务
        if (!enableDailyFarming)
        {
            Debug.Log($"{gameObject.name} 每日耕种功能已禁用");
            return;
        }
        
        currentState = TaskState.SearchingTarget;
        workTimer = 0f;
        isPlayingWorkAnimation = false;
        isMovingToTarget = false;
        
        Debug.Log($"{gameObject.name} 开始执行耕种任务");
    }
    
    private void Update()
    {
        ExecuteCurrentTask();
    }
    
    /// <summary>
    /// 执行当前任务状态的逻辑
    /// </summary>
    private void ExecuteCurrentTask()
    {
        switch (currentState)
        {
            case TaskState.SearchingTarget:
                SearchFarmableTile();
                break;
            case TaskState.MovingToTarget:
                HandleMovement();
                break;
            case TaskState.Digging:
                HandleDigging();
                break;
            case TaskState.Planting:
                HandlePlanting();
                break;
            case TaskState.Watering:
                HandleWatering();
                break;
            case TaskState.Completed:
                OnTaskCompleted();
                break;
            case TaskState.Failed:
                OnTaskFailed();
                break;
            case TaskState.Resting:
                HandleResting();
                break;
        }
    }
    
    /// <summary>
    /// 搜索可耕种的地块
    /// 优化版本：使用螺旋搜索算法，优先搜索距离近的地块
    /// </summary>
    private void SearchFarmableTile()
    {
        Vector3 npcPosition = transform.position;
        Vector2Int npcGridPos = new Vector2Int(
            Mathf.RoundToInt(npcPosition.x),
            Mathf.RoundToInt(npcPosition.y)
        );
        
        // 使用螺旋搜索算法，从近到远搜索
        List<Vector2Int> candidatePositions = GetSpiralSearchPositions(npcGridPos, searchRadius);
        
        foreach (Vector2Int checkPos in candidatePositions)
        {
            // 获取地块详情
            string key = checkPos.x + "x" + checkPos.y + "y" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            TileDetails tileDetails = GridMapManager.Instance.GetTileDetailes(key);
            
            // 检查是否是可耕种的地块
            if (tileDetails != null && IsFarmableTileWithManager(tileDetails, checkPos))
            {
                // 检查路径是否可达
                if (IsPathReachable(npcGridPos, checkPos))
                {
                    currentTarget = checkPos;
                    targetWorldPosition = new Vector3(checkPos.x + 0.5f, checkPos.y + 0.5f, 0f);
                    
                    // 向管理器注册地块占用
                    if (NPCFarmingManager.Instance != null)
                    {
                        NPCFarmingManager.Instance.RegisterTileOccupation(this, currentTarget);
                    }
                    
                    // 开始移动到目标
                    isMovingToTarget = true;
                    currentState = TaskState.MovingToTarget;
                    Debug.Log($"{gameObject.name} 找到可达的目标地块 {currentTarget}，开始移动");
                    return;
                }
            }
        }
        
        Debug.Log($"{gameObject.name} 未找到可耕种的地块");
        currentState = TaskState.Failed;
    }
    
    /// <summary>
    /// 生成螺旋搜索位置列表，从近到远排序
    /// </summary>
    /// <param name="center">中心位置</param>
    /// <param name="radius">搜索半径</param>
    /// <returns>按距离排序的位置列表</returns>
    private List<Vector2Int> GetSpiralSearchPositions(Vector2Int center, float radius)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        int maxSteps = Mathf.CeilToInt(radius);
        
        // 螺旋搜索
        int x = 0, y = 0;
        int dx = 0, dy = -1;
        
        for (int i = 0; i < (maxSteps * 2 + 1) * (maxSteps * 2 + 1); i++)
        {
            Vector2Int pos = new Vector2Int(center.x + x, center.y + y);
            float distance = Vector2.Distance(center, pos);
            
            if (distance <= radius)
            {
                positions.Add(pos);
            }
            
            if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
            {
                int temp = dx;
                dx = -dy;
                dy = temp;
            }
            
            x += dx;
            y += dy;
        }
        
        // 按距离排序，近距离优先
        positions.Sort((a, b) => Vector2.Distance(center, a).CompareTo(Vector2.Distance(center, b)));
        
        return positions;
    }
    
    /// <summary>
    /// 检查路径是否可达
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="target">目标位置</param>
    /// <returns>是否可达</returns>
    private bool IsPathReachable(Vector2Int start, Vector2Int target)
    {
        // 简化的可达性检查：检查直线距离是否合理
        float distance = Vector2.Distance(start, target);
        
        // 距离过远认为不可达
        if (distance > searchRadius * 1.5f)
        {
            return false;
        }
        
        // 这里可以添加更复杂的路径检查逻辑
        // 例如使用A*算法预检查路径
        
        return true;
    }
    
    /// <summary>
    /// 检查地块是否可耕种（简化版）
    /// </summary>
    /// <param name="tile">地块详情</param>
    /// <returns>是否可耕种</returns>
    private bool IsFarmableTile(TileDetails tile)
    {
        // 基本检查：地块可挖掘且没有已种植的作物
        return tile.canDig && tile.seedItemID == -1;
    }
    
    /// <summary>
    /// 与管理器协作检查地块是否可耕种
    /// </summary>
    /// <param name="tile">地块详情</param>
    /// <param name="position">地块位置</param>
    /// <returns>是否可耕种</returns>
    private bool IsFarmableTileWithManager(TileDetails tile, Vector2Int position)
    {
        // 基本耕种条件检查
        if (!IsFarmableTile(tile))
        {
            return false;
        }
        
        // 检查地块是否已被其他NPC占用
        if (NPCFarmingManager.Instance != null && NPCFarmingManager.Instance.IsTileOccupied(position))
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 处理移动逻辑 - 修复版本
    /// </summary>
    private void HandleMovement()
    {
        if (!isMovingToTarget)
        {
            return;
        }
        
        // 计算到目标的距离
        float distanceToTarget = Vector3.Distance(transform.position, targetWorldPosition);
        
        // 检查是否已到达目标
        if (distanceToTarget <= arrivalThreshold)
        {
            // 停止移动并开始挖地
            isMovingToTarget = false;
            currentState = TaskState.Digging;
            workTimer = 0f;
            Debug.Log($"{gameObject.name} 到达目标地块 {currentTarget}，开始挖地");
            return;
        }
        
        // 平滑移动到目标位置
        Vector3 direction = (targetWorldPosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        // 更新NPC移动动画
        if (animator != null)
        {
            animator.SetBool("isMoving", true);
            animator.SetFloat("DirX", direction.x);
            animator.SetFloat("DirY", direction.y);
        }
        
        // 检查移动超时
        workTimer += Time.deltaTime;
        if (workTimer > 20f) // 20秒超时
        {
            Debug.LogWarning($"{gameObject.name} 移动超时，任务失败");
            isMovingToTarget = false;
            currentState = TaskState.Failed;
        }
    }
    
    /// <summary>
    /// 处理挖地操作 - 使用Animation Clip版本
    /// </summary>
    private void HandleDigging()
    {
        // 停止移动动画，保持NPC在Idle状态
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            
            // 更新面朝方向
            UpdateFacingDirection();
        }
        
        // 首次进入挖地状态时开始工具动画
        if (workTimer == 0f && !isPlayingWorkAnimation)
        {
            PlayHoeToolAnimation();
            isPlayingWorkAnimation = true;
            Debug.Log($"{gameObject.name} 开始播放挖地工具动画");
        }
        
        // 检查工具动画是否完成
        if (isPlayingWorkAnimation)
        {
            CheckToolAnimationComplete();
        }
        
        workTimer += Time.deltaTime;
        
        if (workTimer >= workingTime)
        {
            // 确保动画停止并执行挖地操作
            StopToolAnimation();
            ExecuteDigAction();
            currentState = TaskState.Planting;
            workTimer = 0f;
            isPlayingWorkAnimation = false;
            Debug.Log($"{gameObject.name} 完成挖地，开始种植");
        }
    }
    
    /// <summary>
    /// 处理种植操作
    /// </summary>
    private void HandlePlanting()
    {
        workTimer += Time.deltaTime;
        
        if (workTimer >= workingTime)
        {
            // 执行种植操作
            ExecutePlantAction();
            currentState = TaskState.Watering;
            workTimer = 0f;
            Debug.Log($"{gameObject.name} 完成种植，开始浇水");
        }
    }
    
    /// <summary>
    /// 处理浇水操作 - 使用Animation Clip版本
    /// </summary>
    private void HandleWatering()
    {
        // 停止移动动画，保持NPC在Idle状态
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            
            // 更新面朝方向
            UpdateFacingDirection();
        }
        
        // 首次进入浇水状态时开始工具动画
        if (workTimer == 0f && !isPlayingWorkAnimation)
        {
            PlayWaterToolAnimation();
            isPlayingWorkAnimation = true;
            Debug.Log($"{gameObject.name} 开始播放浇水工具动画");
        }
        
        // 检查工具动画是否完成
        if (isPlayingWorkAnimation)
        {
            CheckToolAnimationComplete();
        }
        
        workTimer += Time.deltaTime;
        
        if (workTimer >= workingTime)
        {
            // 确保动画停止并执行浇水操作
            StopToolAnimation();
            ExecuteWaterAction();
            currentState = TaskState.Completed;
            workTimer = 0f;
            isPlayingWorkAnimation = false;
            Debug.Log($"{gameObject.name} 完成浇水，耕种任务完成");
        }
    }
    
    /// <summary>
    /// 处理休息状态
    /// </summary>
    private void HandleResting()
    {
        workTimer += Time.deltaTime;
        
        // 休息时间结束，重新开始耕种任务
        if (workTimer >= restTimeAfterComplete)
        {
            if (enableDailyFarming)
            {
                Debug.Log($"{gameObject.name} 休息结束，重新开始耕种任务");
                StartFarmingTask();
            }
            else
            {
                Debug.Log($"{gameObject.name} 每日耕种已禁用，停止自动耕种");
            }
        }
    }
    
    /// <summary>
    /// 执行挖地动作
    /// 使用锄头工具挖掘地块，为种植做准备
    /// 完成后更新地块瓦片显示
    /// </summary>
    private void ExecuteDigAction()
    {
        string key = currentTarget.x + "x" + currentTarget.y + "y" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        TileDetails tileDetails = GridMapManager.Instance.GetTileDetailes(key);
        
        if (tileDetails != null && tileDetails.canDig)
        {
            // 直接更新地块状态
            tileDetails.daysSinceDug = 0;
            tileDetails.canDig = false;
            tileDetails.canDropItm = false;
            
            // 更新地块信息到GridMapManager
            GridMapManager.Instance.UpdateTileDetails(tileDetails);
            
            // ✨ 新增：更新挖地瓦片显示
            UpdateDigTileVisual(tileDetails);
            
            // 播放挖地音效
            EventHandler.CallPlaySoundEvent(SoundName.Hoe);
            
            Debug.Log($"{gameObject.name} 在位置 {currentTarget} 使用锄头挖掘了地块，瓦片已更新");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 无法在位置 {currentTarget} 挖地：地块不可挖掘或不存在");
            currentState = TaskState.Failed;
        }
    }
    
    /// <summary>
    /// 执行种植动作
    /// </summary>
    private void ExecutePlantAction()
    {
        string key = currentTarget.x + "x" + currentTarget.y + "y" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        TileDetails tileDetails = GridMapManager.Instance.GetTileDetailes(key);
        
        if (tileDetails != null && tileDetails.daysSinceDug > -1 && tileDetails.seedItemID == -1)
        {
            // 种植作物
            EventHandler.CallPlantSeedEvent(preferredSeedID, tileDetails);
            
            // 播放种植音效
            EventHandler.CallPlaySoundEvent(SoundName.Plant);
            
            Debug.Log($"{gameObject.name} 在位置 {currentTarget} 种植了种子 {preferredSeedID}");
        }
    }
    
    /// <summary>
    /// 执行浇水动作
    /// 浇水完成后更新地块瓦片显示
    /// </summary>
    private void ExecuteWaterAction()
    {
        string key = currentTarget.x + "x" + currentTarget.y + "y" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        TileDetails tileDetails = GridMapManager.Instance.GetTileDetailes(key);
        
        if (tileDetails != null && tileDetails.daysSinceDug > -1 && tileDetails.daysSinceWatered == -1)
        {
            // 浇水
            tileDetails.daysSinceWatered = 0;
            
            // 更新地块信息
            GridMapManager.Instance.UpdateTileDetails(tileDetails);
            
            // ✨ 新增：更新浇水瓦片显示
            UpdateWaterTileVisual(tileDetails);
            
            // 播放浇水音效
            EventHandler.CallPlaySoundEvent(SoundName.Water);
            
            Debug.Log($"{gameObject.name} 在位置 {currentTarget} 执行了浇水操作，瓦片已更新");
        }
    }
    
    /// <summary>
    /// 更新挖地瓦片的视觉显示
    /// 调用GridMapManager的瓦片设置方法，在Dig层显示挖掘瓦片
    /// </summary>
    /// <param name="tileDetails">地块详细信息</param>
    private void UpdateDigTileVisual(TileDetails tileDetails)
    {
        // 使用GridMapManager提供的公共接口
        var gridMapManager = GridMapManager.Instance;
        if (gridMapManager != null)
        {
            gridMapManager.SetDigGroundForNPC(tileDetails);
        }
        else
        {
            Debug.LogWarning("GridMapManager实例不存在，无法更新挖地瓦片");
        }
    }
    
    /// <summary>
    /// 更新浇水瓦片的视觉显示
    /// 调用GridMapManager的瓦片设置方法，在Water层显示浇水瓦片
    /// </summary>
    /// <param name="tileDetails">地块详细信息</param>
    private void UpdateWaterTileVisual(TileDetails tileDetails)
    {
        // 使用GridMapManager提供的公共接口
        var gridMapManager = GridMapManager.Instance;
        if (gridMapManager != null)
        {
            gridMapManager.SetWaterGroundForNPC(tileDetails);
        }
        else
        {
            Debug.LogWarning("GridMapManager实例不存在，无法更新浇水瓦片");
        }
    }
    
    /// <summary>
    /// 播放锄头工具动画 - Animation Clip版本
    /// 根据NPC朝向选择对应方向的动画片段
    /// </summary>
    private void PlayHoeToolAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning($"{gameObject.name} 动画控制器未正确设置");
            return;
        }
        
        // 获取当前面朝方向
        FacingDirection direction = GetCurrentFacingDirection();
        
        // 根据方向选择对应的动画片段
        AnimationClip clipToPlay = GetHoeToolClipByDirection(direction);
        
        if (clipToPlay != null)
        {
            // 播放工具动画
            PlayToolAnimationClip(clipToPlay);
            
            Debug.Log($"{gameObject.name} 开始播放锄头动画 - 方向: {direction}, 动画: {clipToPlay.name}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 未找到方向 {direction} 对应的锄头动画片段");
        }
    }
    
    /// <summary>
    /// 播放浇水工具动画 - Animation Clip版本
    /// 根据NPC朝向选择对应方向的动画片段
    /// </summary>
    private void PlayWaterToolAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning($"{gameObject.name} 动画控制器未正确设置");
            return;
        }
        
        // 获取当前面朝方向
        FacingDirection direction = GetCurrentFacingDirection();
        
        // 根据方向选择对应的动画片段
        AnimationClip clipToPlay = GetWaterToolClipByDirection(direction);
        
        if (clipToPlay != null)
        {
            // 播放工具动画
            PlayToolAnimationClip(clipToPlay);
            
            Debug.Log($"{gameObject.name} 开始播放浇水动画 - 方向: {direction}, 动画: {clipToPlay.name}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 未找到方向 {direction} 对应的浇水动画片段");
        }
    }
    
    /// <summary>
    /// 播放指定的工具动画片段
    /// </summary>
    /// <param name="clip">要播放的动画片段</param>
    private void PlayToolAnimationClip(AnimationClip clip)
    {
        if (animator != null && clip != null)
        {
            // 设置动画播放速度
            animator.speed = toolAnimationSpeedMultiplier;
            
            // 播放指定动画片段
            animator.Play(clip.name);
            
            // 记录当前播放的动画信息
            currentPlayingClipName = clip.name;
            toolAnimationStartTime = Time.time;
            
            Debug.Log($"{gameObject.name} 播放动画片段: {clip.name}, 速度倍率: {toolAnimationSpeedMultiplier}");
        }
    }
    
    /// <summary>
    /// 停止工具动画
    /// </summary>
    private void StopToolAnimation()
    {
        if (animator != null)
        {
            // 恢复正常动画速度
            animator.speed = 1f;
            
            // 如果设置了自动返回待机状态
            if (autoReturnToIdle)
            {
                // 让NPC回到正常的Idle状态
                animator.SetBool("isMoving", false);
            }
        }
        
        // 重置动画跟踪变量
        currentPlayingClipName = string.Empty;
        toolAnimationStartTime = 0f;
        
        Debug.Log($"{gameObject.name} 停止工具动画并恢复正常状态");
    }
    
    /// <summary>
    /// 检查工具动画是否播放完成
    /// </summary>
    private void CheckToolAnimationComplete()
    {
        if (animator == null || string.IsNullOrEmpty(currentPlayingClipName))
        {
            return;
        }
        
        // 获取当前动画状态信息
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // 检查当前是否在播放工具动画，且动画已完成一个循环
        if (stateInfo.IsName(currentPlayingClipName) && stateInfo.normalizedTime >= 1.0f)
        {
            Debug.Log($"{gameObject.name} 工具动画 {currentPlayingClipName} 播放完成");
            // 动画播放完成，可以在这里添加额外的逻辑
        }
    }
    
    /// <summary>
    /// 更新NPC面朝方向
    /// </summary>
    private void UpdateFacingDirection()
    {
        Vector2 direction = GetDirectionToTarget();
        currentFacingDirection = direction;
        
        // 更新动画控制器的方向参数
        if (animator != null)
        {
            animator.SetFloat("DirX", direction.x);
            animator.SetFloat("DirY", direction.y);
        }
    }
    
    /// <summary>
    /// 获取当前NPC面朝的方向
    /// </summary>
    /// <returns>面朝方向枚举</returns>
    private FacingDirection GetCurrentFacingDirection()
    {
        Vector2 direction = GetDirectionToTarget();
        
        // 根据方向向量确定主要朝向
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            // 垂直方向为主
            return direction.y > 0 ? FacingDirection.Up : FacingDirection.Down;
        }
        else
        {
            // 水平方向为主
            return direction.x > 0 ? FacingDirection.Right : FacingDirection.Left;
        }
    }
    
    /// <summary>
    /// 根据方向获取对应的锄头工具动画片段
    /// </summary>
    /// <param name="direction">面朝方向</param>
    /// <returns>对应的动画片段</returns>
    private AnimationClip GetHoeToolClipByDirection(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.Up:
                return hoeToolUpClip;
            case FacingDirection.Down:
                return hoeToolDownClip;
            case FacingDirection.Left:
                return hoeToolLeftClip;
            case FacingDirection.Right:
                return hoeToolRightClip;
            default:
                return hoeToolDownClip; // 默认使用下方向
        }
    }
    
    /// <summary>
    /// 根据方向获取对应的浇水工具动画片段
    /// </summary>
    /// <param name="direction">面朝方向</param>
    /// <returns>对应的动画片段</returns>
    private AnimationClip GetWaterToolClipByDirection(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.Up:
                return waterToolUpClip;
            case FacingDirection.Down:
                return waterToolDownClip;
            case FacingDirection.Left:
                return waterToolLeftClip;
            case FacingDirection.Right:
                return waterToolRightClip;
            default:
                return waterToolDownClip; // 默认使用下方向
        }
    }
    
    /// <summary>
    /// 获取到目标的方向向量
    /// </summary>
    /// <returns>标准化的方向向量</returns>
    private Vector2 GetDirectionToTarget()
    {
        Vector3 npcPos = transform.position;
        Vector3 targetPos = new Vector3(currentTarget.x + 0.5f, currentTarget.y + 0.5f, 0);
        
        Vector2 direction = (targetPos - npcPos).normalized;
        return direction;
    }
    
    /// <summary>
    /// 任务完成回调 - 修复持续耕种问题
    /// </summary>
    private void OnTaskCompleted()
    {
        // 释放地块占用
        if (NPCFarmingManager.Instance != null)
        {
            NPCFarmingManager.Instance.ReleaseTileOccupation(this);
        }
        
        Debug.Log($"{gameObject.name} 耕种任务完成！进入休息状态");
        
        // 进入休息状态，而不是立即重新开始
        currentState = TaskState.Resting;
        workTimer = 0f;
    }
    
    /// <summary>
    /// 任务失败回调 - 修复持续耕种问题
    /// </summary>
    private void OnTaskFailed()
    {
        // 释放地块占用
        if (NPCFarmingManager.Instance != null)
        {
            NPCFarmingManager.Instance.ReleaseTileOccupation(this);
        }
        
        Debug.Log($"{gameObject.name} 耕种任务失败，进入休息状态");
        
        // 进入休息状态，等待更长时间后重试
        currentState = TaskState.Resting;
        workTimer = 0f;
        restTimeAfterComplete = restTimeAfterFailed; // 使用失败休息时间
    }
    
    /// <summary>
    /// 设置NPC偏好的种子类型
    /// </summary>
    /// <param name="seedID">种子ID</param>
    public void SetPreferredSeed(int seedID)
    {
        preferredSeedID = seedID;
    }
    
    /// <summary>
    /// 设置搜索半径
    /// </summary>
    /// <param name="radius">搜索半径</param>
    public void SetSearchRadius(float radius)
    {
        searchRadius = radius;
    }
    
    /// <summary>
    /// 获取当前任务状态
    /// </summary>
    /// <returns>当前任务状态</returns>
    public TaskState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// 启用或禁用每日耕种
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void SetDailyFarmingEnabled(bool enable)
    {
        enableDailyFarming = enable;
        Debug.Log($"{gameObject.name} 每日耕种设置为: {(enable ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 立即停止当前任务
    /// </summary>
    public void StopCurrentTask()
    {
        currentState = TaskState.Resting;
        isMovingToTarget = false;
        isPlayingWorkAnimation = false;
        workTimer = 0f;
        
        // 停止动画
        StopToolAnimation();
        
        // 释放地块占用
        if (NPCFarmingManager.Instance != null)
        {
            NPCFarmingManager.Instance.ReleaseTileOccupation(this);
        }
        
        Debug.Log($"{gameObject.name} 停止当前耕种任务");
    }
} 