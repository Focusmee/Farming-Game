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
    private SortedSet<SchedulDetails> scheduleSet;//ʼ�ձ����������ݵ�����˳���Լ�Ψһ�Եļ���
    private SchedulDetails currentSchedule;
    //��ʱ�洢��Ϣ
    [SerializeField]private string currentScene;
    private string targetScene;
    private Vector3Int currentGridPostion;//��ǰ����λ��
    private Vector3Int targetGridPostion;//Ŀ������λ��
    private Vector3Int nextGridPosition;//��һ������λ������
    private Vector3 nextWorldPosition;//��һ������������
    public string StartScene { set => currentScene = value; }//NPC��һ��ʼ���ڵĳ���
    [Header("�ƶ�����")]
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
    private Coroutine npcMoveRoutine;//NPC�ƶ���һ��Э�̽�����Ϊ��������
    private bool isInitialised;//�ж�NPC�Ƿ��ǵ�һ�μ���
    private bool npcMove;//�ж�NPC�Ƿ��ƶ�
    private bool sceneLoaded;//�жϳ����Ƿ�������
    public bool interactable;//�Ƿ���Ի���
    public bool isFirstLoad;
    private Season currentSeason;
    private float animationBreakTime;//������ʱ��
    private bool canPlayStopAnimation;
    private AnimationClip stopAnimationClip;
    public AnimationClip blankAnimationClip;//����һ���հ׵Ķ���Ƭ��
    private AnimatorOverrideController animOverride;//�ع�һ������������
    private TimeSpan GameTime => TimeManager.Instance.GameTime;//TimeSpan:��ʾһ��ʱ���

    public string GUID => GetComponent<DataGUID>().guid;

    //����:����,����һ��ʱ��� TimeSpan targetTime = new TimeSpan(10,20); ���ʱ�������10��20��
    //ӵ��ʱ���֮���������ʱ�����Ϊ����,����GameTime��Ϸʱ�䵽��10��20��ʱ�ܹ�����ʲô�¼�

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        coll = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        movementSteps = new Stack<MovementStep>();
        animOverride = new AnimatorOverrideController(anim.runtimeAnimatorController);
        anim.runtimeAnimatorController = animOverride;
        //runtimeAnimatorController:AnimatorController������ʱ��ʾ.ʹ�ô˱�ʾ��������ʱ�ڼ����AnimatorController��
        scheduleSet = new SortedSet<SchedulDetails>();
        foreach (var schedule in schedulData.schedulList)
        {
            scheduleSet.Add(schedule);
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
        //��ʱ��
        animationBreakTime -= Time.deltaTime;
        canPlayStopAnimation = animationBreakTime <= 0;
    }
    private void FixedUpdate()
    {
        if (sceneLoaded)//������������˲ſ�����NPC�ƶ�
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
        CheckVisiable();
        if (!isInitialised)
        {
            InitNPC();//��һ�μ��صĻ��ͳ�ʼ��NPC
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
        targetScene = currentScene;//��ʼ��Ŀ�곡�����ǵ�ǰ����
        //�����ڵ�ǰ������������ĵ�
        currentGridPostion = grid.WorldToCell(transform.position);//��������ת��Ϊ��������
        transform.position = new Vector3(currentGridPostion.x + Settings.gridCellSize / 2f, currentGridPostion.y + Settings.gridCellSize / 2f, 0);
        targetGridPostion = currentGridPostion;
    }
    /// <summary>
    /// ��Ҫ�ƶ�����
    /// </summary>
    private void Movement()
    {
        if (!npcMove)
        {
            if (movementSteps.Count>0)//�ж��ƶ�·����ջ���Ƿ��й����õĲ���·��
            {
                MovementStep step = movementSteps.Pop();//�õ���ջ�еĵ�һ�����ҴӶ�ջ���Ƴ���
                currentScene = step.sceneName;//��ǰ�������ڲ����д�ŵĳ���
                CheckVisiable();//ʵʱ���NPC�Ƿ�ɼ�
                nextGridPosition = (Vector3Int)step.gridCoordinate;
                TimeSpan stepTime = new TimeSpan(step.hour, step.minute, step.second);//�õ���һ����ʱ���
                MoveToGridPosition(nextGridPosition, stepTime);
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
    //��ΪNPC���ƶ�����������(�Լ��ڳ������Լ������Լ��ƶ�)����Ҫ��Э�̸���
    private IEnumerator MoveRoutine(Vector3Int gridPos,TimeSpan stepTime)
    {
        npcMove = true;//NPC�ƶ�״̬��Ϊtrue
        nextWorldPosition = GetWorldPosition(gridPos);
        if (stepTime > GameTime)//�����Ϸʱ�仹û����ǰ����һ����ʱ���
        {
            //��ȡ�����ƶ���ʱ���,����Ϊ��λ
            float timeToMove = (float)(stepTime.TotalSeconds - GameTime.TotalSeconds);
            //ʵ���ƶ�����
            float distance = Vector3.Distance(transform.position, nextWorldPosition);
            //����NPC��ʵ���ƶ��ٶ�
            float speed = Mathf.Max(minSpeed, (distance / timeToMove / Settings.secondThreshold));//����Сֵ���Ƚ���Ϊ��ȷ���ٶȲ��������Сֵ
            //�ٶ�=����/ʱ��(m/s)

            if (speed <= maxSpeed)//ͬ�����ܴ�������ƶ��ٶ�
            {
                while (Vector3.Distance(transform.position, nextWorldPosition) > Settings.pixelSize)//���NPC��ǰ�������һ��Ҫ����ľ��뻹����һ�����ص�˵����û���ߵ�
                {
                    dir = (nextWorldPosition - transform.position).normalized;//NPC�ƶ�����
                    Vector2 posOffset = new Vector2(dir.x * speed * Time.fixedDeltaTime, dir.y * speed * Time.fixedDeltaTime);//�ƶ�ƫ��
                    rb.MovePosition(rb.position + posOffset);//�ƶ����嵽ĳ��λ��
                    yield return new WaitForFixedUpdate();//�ȴ�һС���
                }
            }
        }
        //���ʱ���Ѿ����˾�˲��
        rb.position = nextWorldPosition;
        currentGridPostion = gridPos;
        nextGridPosition = currentGridPostion;

        npcMove = false;
    }
    /// <summary>
    /// ����Scheduleʱ�������·��
    /// </summary>
    /// <param name="schedule"></param>
    public void BuildPath(SchedulDetails schedule)
    {
        movementSteps.Clear();//����֮ǰ��ջ�е�·��
        currentSchedule = schedule;
        targetScene = schedule.targetScene;
        targetGridPostion = (Vector3Int)schedule.targetGridPosition;
        stopAnimationClip = schedule.clipAtStop;
        this.interactable = schedule.interactable;
        if (schedule.targetScene == currentScene)//���Ŀ�곡������ڵ�ǰ����,������AStar��ʼ�������·��
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

        if (movementSteps.Count > 1)//·������1���������ƶ���
        {
            //����ÿһ����ʱ��ʱ���
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
    /// ����NPCÿ��һ����ʱ���
    /// </summary>
    private void UpdateTimeOnPath()
    {
        MovementStep previousSetp = null;

        TimeSpan currentGameTime = GameTime;

        foreach (MovementStep step in movementSteps)//����AStar�㷨�Ѿ��������˶�ջ,��������ÿһ������
        {
            if (previousSetp == null)//��Ϸһ��ʼNPCû����һ��,����ֱ�ӽ���һ�����ݸ�����
                previousSetp = step;

            step.hour = currentGameTime.Hours;
            step.minute = currentGameTime.Minutes;
            step.second = currentGameTime.Seconds;//��¼��ǰ����Ϸʱ���

            TimeSpan gridMovementStepTime;//����һ���µ�ʱ�������ȡ�ߵ�ÿһ����ʱ���

            if (MoveInDiagonal(step, previousSetp))//�ж��Ƿ���б�����ƶ�,б�����ƶ���ֱ��������Ҫ��ʱ�������ͬ
                gridMovementStepTime = new TimeSpan(0, 0, (int)(Settings.gridCellDiagonalSize / normalSpeed / Settings.secondThreshold));
            else
                gridMovementStepTime = new TimeSpan(0, 0, (int)(Settings.gridCellSize / normalSpeed / Settings.secondThreshold));

            //�ۼӻ����һ����ʱ���
            currentGameTime = currentGameTime.Add(gridMovementStepTime);//��Ϸʱ�������·ʱ����ۼ������γ�һ��ʱ�������
            //ѭ����һ���˲���Ϊ����
            previousSetp = step;
        }
    }

    /// <summary>
    /// �ж��Ƿ���б����
    /// </summary>
    /// <param name="currentStep"></param>
    /// <param name="previousStep"></param>
    /// <returns></returns>
    private bool MoveInDiagonal(MovementStep currentStep, MovementStep previousStep)
    {
        return (currentStep.gridCoordinate.x != previousStep.gridCoordinate.x) && (currentStep.gridCoordinate.y != previousStep.gridCoordinate.y);
    }
    /// <summary>
    /// �õ��������ĵ���������
    /// </summary>
    /// <param name="gridPos">��������</param>
    /// <returns></returns>
    private Vector3 GetWorldPosition(Vector3Int gridPos)
    {
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
        //ǿ������ͷ
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
    #region ����NPC��ʾ���
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
