using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MFarm.AStar;
using UnityEngine.SceneManagement;
using System;
using MFarm.Save;
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class NPCMovement : MonoBehaviour,ISaveable
{
    public SchedulDataList_SO schedulData;
    private SortedSet<SchedulDetails> scheduleSet;//ʼձݵ˳ԼΨһ�Եļ���
    private SchedulDetails currentSchedule;
    //ʱ洢Ϣ
    [SerializeField]private string currentScene;
    private string targetScene;
    private Vector3Int currentGridPostion;//ǰλ
    private Vector3Int targetGridPostion;//Ŀ����λ
    private Vector3Int nextGridPosition;//һλ
    private Vector3 nextWorldPosition;//һ
    public string StartScene { set => currentScene = value; }//NPCһʼڵĳ
    [Header("ƶ")]
    public float normalSpeed = 2f;
    private float minSpeed = 1;
    private float maxSpeed = 3;
    private Vector2 dir;//����
    public bool isMoving;
    //Component
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D coll;
    private Animator anim;
    private Grid grid;
    private Stack<MovementStep> movementSteps;
    private Coroutine npcMoveRoutine;//NPCƶһЭ̽Ϊ
    private bool isInitialised;//жNPCǷһμ
    private bool npcMove;//жNPCǷƶ
    private bool sceneLoaded;//жϳǷ
    public bool interactable;//ǷԻ
    public bool isFirstLoad;
    private Season currentSeason;
    private float animationBreakTime;//ʱ
    private bool canPlayStopAnimation;
    private AnimationClip stopAnimationClip;
    public AnimationClip blankAnimationClip;//һհ׵ĶƬ
    private AnimatorOverrideController animOverride;//عһ
    private TimeSpan GameTime => TimeManager.Instance.GameTime;//TimeSpan:ʾһʱ

    public string GUID => GetComponent<DataGUID>().guid;

    //:,һʱ TimeSpan targetTime = new TimeSpan(10,20); ʱ1020
    //ӵʱ֮ʱΪ,GameTimeϷʱ䵽1020ʱܹʲô¼

    private void Awake()
    {
        // 获取必要的组件
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        coll = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        
        // 验证组件是否成功获取
        if (rb == null)
            Debug.LogError($"NPC {gameObject.name}: 缺少Rigidbody2D组件");
        if (spriteRenderer == null)
            Debug.LogError($"NPC {gameObject.name}: 缺少SpriteRenderer组件");
        if (coll == null)
            Debug.LogError($"NPC {gameObject.name}: 缺少BoxCollider2D组件");
        if (anim == null)
            Debug.LogError($"NPC {gameObject.name}: 缺少Animator组件");
        
        // 初始化移动步骤栈
        movementSteps = new Stack<MovementStep>();
        
        // 初始化动画控制器
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            animOverride = new AnimatorOverrideController(anim.runtimeAnimatorController);
            anim.runtimeAnimatorController = animOverride;
        }
        else
        {
            Debug.LogWarning($"NPC {gameObject.name}: Animator或RuntimeAnimatorController为null，无法初始化动画覆盖控制器");
        }
        
        // 初始化调度集合
        scheduleSet = new SortedSet<SchedulDetails>();
        if (schedulData != null && schedulData.schedulList != null)
        {
            foreach (var schedule in schedulData.schedulList)
            {
                if (schedule != null)
                    scheduleSet.Add(schedule);
            }
        }
        else
        {
            Debug.LogWarning($"NPC {gameObject.name}: schedulData或schedulList为null，无法初始化调度数据");
        }
    }
    private void Start()
    {
        ISaveable saveable = this;
        saveable.RegisterSaveable();
    }
    private void OnEnable()
    {
        EventHandler.AfterSceneLoadedEvent += OnAfterSceneLoadedEvent;
        EventHandler.BeforeSceneUnloadEvent += OnBeforeSceneUnloadEvent;
        EventHandler.GameMinuteEvent += OnGameMinuteEvent;
        EventHandler.EndGameEvent += OnEndGameEvent;
        EventHandler.StartNewGameEvent += OnStartNewGameEvent;
    }
    private void OnDisable()
    {
        EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvent;
        EventHandler.BeforeSceneUnloadEvent -= OnBeforeSceneUnloadEvent;
        EventHandler.GameMinuteEvent -= OnGameMinuteEvent;
        EventHandler.EndGameEvent -= OnEndGameEvent;
        EventHandler.StartNewGameEvent -= OnStartNewGameEvent;
    }

    private void OnStartNewGameEvent(int obj)
    {
        isInitialised = false;
        isFirstLoad = true;
    }

    private void OnEndGameEvent()
    {
        sceneLoaded= false;
        npcMove = false;
        if (npcMoveRoutine != null)
        {
            StopCoroutine(npcMoveRoutine);
        }
    }

    private void Update()
    {
        if (sceneLoaded)
            SwitchAnimation();
        //ʱ
        animationBreakTime -= Time.deltaTime;
        canPlayStopAnimation = animationBreakTime <= 0;
    }
    private void FixedUpdate()
    {
        if (sceneLoaded)//˲ſNPCƶ
        {
            Movement();
        }

    }
    private void OnBeforeSceneUnloadEvent()
    {
        sceneLoaded = false;
    }
    private void OnAfterSceneLoadedEvent()
    {
        grid = FindObjectOfType<Grid>();
        
        // 检查grid是否成功找到
        if (grid == null)
        {
            Debug.LogError($"NPC {gameObject.name}: 场景中未找到Grid组件，NPC移动功能将无法正常工作");
            return;
        }
        
        CheckVisiable();
        if (!isInitialised)
        {
            InitNPC();//第一次加载的话就初始化NPC
            isInitialised = true;
        }
        sceneLoaded = true;
        if (!isFirstLoad)
        {
            currentGridPostion = grid.WorldToCell(transform.position);
            var schedule = new SchedulDetails(0, 0, 0, 0, currentSeason, targetScene, (Vector2Int)targetGridPostion, stopAnimationClip, interactable);
        }
    }
    private void OnGameMinuteEvent(int minute, int hour, Season season,int day)
    {
        int time = (hour * 100) + minute;
        currentSeason= season;
        SchedulDetails matchSchedule = null;
        foreach (var schedule in scheduleSet)
        {
            if (schedule.Time == time)
            {
                if (schedule.day != day && schedule.day != 0)
                    continue;
                if (schedule.season != season)
                    continue;
                matchSchedule = schedule;
            }
            else if(schedule.Time>time)
            {
                break;
            }
        }
        if (matchSchedule != null)
            BuildPath(matchSchedule);
    }
    private void CheckVisiable()
    {
        if (currentScene == SceneManager.GetActiveScene().name)
        {
            SetActiveInScene();
        }
        else
        {
            SetInActiveInScene();
        }
    }
    private void InitNPC()
    {
        // 检查grid是否可用
        if (grid == null)
        {
            Debug.LogError($"NPC {gameObject.name}: Grid为null，无法初始化NPC位置");
            return;
        }
        
        targetScene = currentScene;//初始化目标场景就是当前场景
        //保证在当前场景坐标正确的点
        currentGridPostion = grid.WorldToCell(transform.position);//世界坐标转换为网格坐标
        transform.position = new Vector3(currentGridPostion.x + Settings.gridCellSize / 2f, currentGridPostion.y + Settings.gridCellSize / 2f, 0);
        targetGridPostion = currentGridPostion;
    }
    /// <summary>
    /// 核心移动方法
    /// </summary>
    private void Movement()
    {
        if (!npcMove)
        {
            // 添加空引用检查
            if (movementSteps != null && movementSteps.Count > 0)//判断移动路径栈是否有可用的步骤路径
            {
                MovementStep step = movementSteps.Pop();//得到栈中的第一个并从栈中移除
                
                // 检查step是否为null
                if (step != null)
                {
                    currentScene = step.sceneName;//当前场景存储步骤中存储的场景
                    CheckVisiable();//实时检查NPC是否可见
                    nextGridPosition = (Vector3Int)step.gridCoordinate;
                    TimeSpan stepTime = new TimeSpan(step.hour, step.minute, step.second);//得到下一步的时间
                    MoveToGridPosition(nextGridPosition, stepTime);
                }
                else
                {
                    Debug.LogWarning($"NPC {gameObject.name}: MovementStep为null，跳过此步骤");
                }
            }
            else if (!isMoving && canPlayStopAnimation)
            {
                StartCoroutine(SetStopAnimation());
            }
        }
    }
    private void MoveToGridPosition(Vector3Int gridPos,TimeSpan stepTime)
    {
        npcMoveRoutine = StartCoroutine(MoveRoutine(gridPos, stepTime));
    }
    //ΪNPCƶ(ԼڳԼƶ)ҪЭ̸
    private IEnumerator MoveRoutine(Vector3Int gridPos,TimeSpan stepTime)
    {
        npcMove = true;//NPCƶ״̬Ϊtrue
        nextWorldPosition = GetWorldPosition(gridPos);
        if (stepTime > GameTime)//Ϸʱ仹ûǰһʱ
        {
            //ȡƶʱ,Ϊλ
            float timeToMove = (float)(stepTime.TotalSeconds - GameTime.TotalSeconds);
            //ʵƶ
            float distance = Vector3.Distance(transform.position, nextWorldPosition);
            //NPCʵƶٶ
            float speed = Mathf.Max(minSpeed, (distance / timeToMove / Settings.secondThreshold));//СֵȽΪȷٶȲСֵ
            //ٶ=/ʱ(m/s)

            if (speed <= maxSpeed)//ܴͬƶٶ
            {
                while (Vector3.Distance(transform.position, nextWorldPosition) > Settings.pixelSize)//NPCǰһҪľ뻹һص˵ûߵ
                {
                    dir = (nextWorldPosition - transform.position).normalized;//NPCƶ
                    Vector2 posOffset = new Vector2(dir.x * speed * Time.fixedDeltaTime, dir.y * speed * Time.fixedDeltaTime);//ƶƫ
                    rb.MovePosition(rb.position + posOffset);//ƶ嵽ĳλ
                    yield return new WaitForFixedUpdate();//ȴһС
                }
            }
        }
        //ʱѾ˾˲
        rb.position = nextWorldPosition;
        currentGridPostion = gridPos;
        nextGridPosition = currentGridPostion;

        npcMove = false;
    }
    /// <summary>
    /// Scheduleʱ·
    /// </summary>
    /// <param name="schedule"></param>
    public void BuildPath(SchedulDetails schedule)
    {
        movementSteps.Clear();//֮ǰջе·
        currentSchedule = schedule;
        targetScene = schedule.targetScene;
        targetGridPostion = (Vector3Int)schedule.targetGridPosition;
        stopAnimationClip = schedule.clipAtStop;
        this.interactable = schedule.interactable;
        if (schedule.targetScene == currentScene)//Ŀ곡ڵǰ,AStarʼ·
        {
            AStar.Instance.BuildPath(schedule.targetScene, (Vector2Int)currentGridPostion, schedule.targetGridPosition, movementSteps);
        }
        else if (schedule.targetScene != currentScene)
        {
            SceneRoute sceneRoute = NPCManager.Instance.GetSceneRoute(currentScene, schedule.targetScene);
            if (sceneRoute != null)
            {
                for (int i = 0; i < sceneRoute.scenePathList.Count; i++)
                {
                    Vector2Int fromPos, gotoPos;
                    ScenePath path = sceneRoute.scenePathList[i];
                    if (path.fromGridCell.x >= Settings.maxGridSize)
                    {
                        fromPos = (Vector2Int)currentGridPostion;
                    }
                    else
                    {
                        fromPos = path.fromGridCell;
                    }
                    if (path.gotoGridCell.x >= Settings.maxGridSize)
                    {
                        gotoPos = schedule.targetGridPosition;
                    }
                    else
                    {
                        gotoPos = path.gotoGridCell;
                    }
                    AStar.Instance.BuildPath(path.sceneName, fromPos, gotoPos, movementSteps);
                }
            }
        }

        if (movementSteps.Count > 1)//·����1ƶ
        {
            //ÿһʱʱ
            UpdateTimeOnPath();
        }
    }
    
    /// <summary>
    /// 设置NPC移动路径
    /// 用于耕种任务等外部系统控制NPC移动
    /// </summary>
    /// <param name="movementStack">移动步骤栈</param>
    public void SetUpMovement(Stack<MovementStep> movementStack)
    {
        movementSteps.Clear();
        
        // 复制移动步骤到NPC的移动栈中
        Stack<MovementStep> tempStack = new Stack<MovementStep>();
        while (movementStack.Count > 0)
        {
            tempStack.Push(movementStack.Pop());
        }
        
        while (tempStack.Count > 0)
        {
            MovementStep step = tempStack.Pop();
            movementSteps.Push(step);
            movementStack.Push(step); // 保持原栈不变
        }
        
        Debug.Log($"NPC {gameObject.name} 设置移动路径，步数: {movementSteps.Count}");
    }
    
    /// <summary>
    /// 停止NPC移动
    /// </summary>
    public void StopMovement()
    {
        npcMove = false;
        if (npcMoveRoutine != null)
        {
            StopCoroutine(npcMoveRoutine);
            npcMoveRoutine = null;
        }
        movementSteps.Clear();
        Debug.Log($"NPC {gameObject.name} 停止移动");
    }
    /// <summary>
    /// NPCÿһһʱ
    /// </summary>
    private void UpdateTimeOnPath()
    {
        MovementStep previousSetp = null;

        TimeSpan currentGameTime = GameTime;

        foreach (MovementStep step in movementSteps)//AStar㷨Ѿ˶ջ,ÿһ
        {
            if (previousSetp == null)//ϷһʼNPCûһ,ֱӽһݸ
                previousSetp = step;

            step.hour = currentGameTime.Hours;
            step.minute = currentGameTime.Minutes;
            step.second = currentGameTime.Seconds;//¼ǰϷʱ

            TimeSpan gridMovementStepTime;//һµʱȡߵÿһʱ

            if (MoveInDiagonal(step, previousSetp))//жǷбƶ,бƶֱҪʱͬ
                gridMovementStepTime = new TimeSpan(0, 0, (int)(Settings.gridCellDiagonalSize / normalSpeed / Settings.secondThreshold));
            else
                gridMovementStepTime = new TimeSpan(0, 0, (int)(Settings.gridCellSize / normalSpeed / Settings.secondThreshold));

            //ۼӻһʱ
            currentGameTime = currentGameTime.Add(gridMovementStepTime);//Ϸʱ·ʱۼγһʱ
            //ѭһ˲Ϊ
            previousSetp = step;
        }
    }

    /// <summary>
    /// жǷб
    /// </summary>
    /// <param name="currentStep"></param>
    /// <param name="previousStep"></param>
    /// <returns></returns>
    private bool MoveInDiagonal(MovementStep currentStep, MovementStep previousStep)
    {
        return (currentStep.gridCoordinate.x != previousStep.gridCoordinate.x) && (currentStep.gridCoordinate.y != previousStep.gridCoordinate.y);
    }
    /// <summary>
    /// 得到网格坐标的世界坐标
    /// </summary>
    /// <param name="gridPos">网格坐标</param>
    /// <returns></returns>
    private Vector3 GetWorldPosition(Vector3Int gridPos)
    {
        // 检查grid是否可用
        if (grid == null)
        {
            Debug.LogError($"NPC {gameObject.name}: Grid为null，无法转换坐标");
            return transform.position; // 返回当前位置作为备用
        }
        
        Vector3 worldPos = grid.CellToWorld(gridPos);
        return new Vector3(worldPos.x + Settings.gridCellSize / 2f, worldPos.y + Settings.gridCellSize / 2f);
    }
    private void SwitchAnimation()
    {
        isMoving = transform.position != GetWorldPosition(targetGridPostion);
        anim.SetBool("isMoving", isMoving);
        if (isMoving)
        {
            anim.SetBool("Exit", true);
            anim.SetFloat("DirX", dir.x);
            anim.SetFloat("DirY", dir.y);
        }
        else
        {
            anim.SetBool("Exit", false);
        }
    }
    private IEnumerator SetStopAnimation()
    {
        //ǿͷ
        anim.SetFloat("DirX", 0);
        anim.SetFloat("DirY", -1);
        animationBreakTime = Settings.animationBreakTime;
        if (stopAnimationClip != null)
        {
            animOverride[blankAnimationClip] = stopAnimationClip;
            anim.SetBool("EventAnimation", true);
            yield return null;
            anim.SetBool("EventAnimation", false);
        }
        else
        {
            animOverride[stopAnimationClip] = blankAnimationClip;
            anim.SetBool("EventAnimation", false);
        }
    }
    #region NPCʾ
    private void SetActiveInScene()
    {
        spriteRenderer.enabled = true;
        coll.enabled = true;
        transform.GetChild(0).gameObject.SetActive(true);
    }
    private void SetInActiveInScene()
    {
        spriteRenderer.enabled = false;
        coll.enabled = false;
        transform.GetChild(0).gameObject.SetActive(false);
    }
    #endregion
    public GameSaveData GenerateSaveData()
    {
        GameSaveData saveData = new GameSaveData();
        saveData.characterPosDict = new Dictionary<string, SerializableVector3>();
        saveData.characterPosDict.Add("targetGridPosition", new SerializableVector3(targetGridPostion));
        saveData.characterPosDict.Add("currentPosition", new SerializableVector3(transform.position));
        saveData.dataSceneName = currentScene;
        saveData.targetScene = this.targetScene;
        if (stopAnimationClip != null)
        {
            saveData.animationInstanceID = stopAnimationClip.GetInstanceID();
        }
        saveData.interactable = this.interactable;
        saveData.timeDict = new Dictionary<string, int>
        {
            { "currentSeason", (int)currentSeason }
        };
        return saveData;
    }

    public void RestoreData(GameSaveData saveDate)
    {
        isInitialised = true;
        isFirstLoad = false;
        currentScene = saveDate.dataSceneName;
        targetScene = saveDate.targetScene;
        Vector3 pos = saveDate.characterPosDict["currentPosition"].ToVector3();
        Vector3Int gridPos = (Vector3Int)saveDate.characterPosDict["targetGridPosition"].ToVector2Int();
        transform.position = pos;
        targetGridPostion = gridPos;
        if (saveDate.animationInstanceID != 0)
        {
            this.stopAnimationClip = Resources.InstanceIDToObject(saveDate.animationInstanceID) as AnimationClip;
        }
        this.interactable = saveDate.interactable;
        this.currentSeason = (Season)saveDate.timeDict["currentSeason"];
    }
}
