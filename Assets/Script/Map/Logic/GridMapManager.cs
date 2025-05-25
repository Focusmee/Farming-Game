using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using MFarm.CropPlant;
using MFarm.Save;
namespace MFarm.Map
{
    public class GridMapManager : Singleton<GridMapManager>,ISaveable
    {
        [Header("�ֵ���Ƭ�л���Ϣ")]
        public RuleTile digTile;//ʵ��������������Ƭ
        public RuleTile waterTile;
        private Tilemap digTilemap;//ʵ��������Ϳ��������Ƭ����Ƭ��ͼ
        private Tilemap waterTilemap;
        [Header("��ͼ��Ϣ")]
        public List<MapData_SO> mapDataList;
        //����һ���ֵ���������ӳ�������+���������Ӧ����Ƭ��Ϣ
        private Dictionary<string, TileDetails> tileDetailesDict = new Dictionary<string, TileDetails>();
        //��¼�����Ƿ��һ�μ���
        private Dictionary<string, bool> firstLoadDic = new Dictionary<string, bool>();
        private Grid currentGrid;
        private Season currentSeason;
        //�Ӳ��б�
        private List<ReapItem> itemsInRadius;

        public string GUID => GetComponent<DataGUID>().guid;

        private void OnEnable()
        {
            EventHandler.ExecuteActionAfterAnimation += OnExecuteActionAfterAnimation;
            EventHandler.AfterSceneLoadedEvent += OnAfterSceneLoadedEvent;
            EventHandler.GameDayEvent += OnGameDayEvent;
            EventHandler.RefreshCurrentMap += RefreshMap;
        }
        private void OnDisable()
        {
            EventHandler.ExecuteActionAfterAnimation -= OnExecuteActionAfterAnimation;
            EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvent;
            EventHandler.GameDayEvent -= OnGameDayEvent;
            EventHandler.RefreshCurrentMap -= RefreshMap;
        }
        /// <summary>
        /// ÿ��ִ��һ��
        /// </summary>
        /// <param name="day"></param>
        /// <param name="season"></param>
        private void OnGameDayEvent(int day, Season season)
        {
            currentSeason = season;
            foreach (var tile in tileDetailesDict)
            {
                if (tile.Value.daysSinceWatered > -1)
                {
                    tile.Value.daysSinceWatered = -1;//����ø��ӽ���ˮ,�ڶ����ֻ���
                }
                if (tile.Value.daysSinceDug > -1)
                {
                    tile.Value.daysSinceDug++;//����ø��ӱ�����,��ÿ�쿪������++
                }
                if (tile.Value.daysSinceDug > 5 && tile.Value.seedItemID == -1)
                {
                    tile.Value.daysSinceDug = -1;//���һ�����ӱ�����������û����ֲ����,����������
                    tile.Value.canDig = true;
                    tile.Value.growthDays = -1;
                }
                if (tile.Value.seedItemID != -1)
                {
                    tile.Value.growthDays++;
                }
            }
            RefreshMap();
        }

        private void OnAfterSceneLoadedEvent()
        {
            currentGrid = FindObjectOfType<Grid>();
            digTilemap = GameObject.FindWithTag("Dig").GetComponent<Tilemap>();
            waterTilemap = GameObject.FindWithTag("Water").GetComponent<Tilemap>();
            /*DisplayMap(SceneManager.GetActiveScene().name);*/
            if (firstLoadDic[SceneManager.GetActiveScene().name])
            {
                //Ԥ������ũ����
                EventHandler.CallGenerateCropEvent();
                firstLoadDic[SceneManager.GetActiveScene().name] = false;//����ǰ�������Ƿ��ǵ�һ�μ���״̬��Ϊfalse
            }

            RefreshMap();
        }

        private void Start()
        {
            foreach (var mapData in mapDataList)
            {
                firstLoadDic.Add(mapData.sceneName, true);//���еĳ������ǵ�һ�μ���,���س���֮��Ͱ�����ΪFalse
                InitTileDetailsDict(mapData);
            }
            ISaveable saveable = this;
            saveable.RegisterSaveable();
        }
        /// <summary>
        /// ���ݵ�ͼ��Ϣ�����ֵ�
        /// </summary>
        /// <param name="mapData">��ͼ��Ϣ</param>
        private void InitTileDetailsDict(MapData_SO mapData)
        {
            foreach (TileProperty tileProperty in mapData.tileProperties)
            {
                TileDetails tileDetails = new TileDetails
                {
                    gridX = tileProperty.tileCoordinate.x,
                    gridY = tileProperty.tileCoordinate.y
                };//���������ݿ��е�������Ϣ���ݸ�����Ȼ�����List
                //���������ֵ��key
                string key = tileDetails.gridX + "x" + tileDetails.gridY + "y" + mapData.sceneName;
                if (GetTileDetailes(key) != null)
                {
                    tileDetails = GetTileDetailes(key);//�����������Ѿ�������key�ˣ���ֱ�Ӹ�ֵ
                }
                switch (tileProperty.gridType)//����������Ϣ
                {
                    case GridType.Digable:
                        tileDetails.canDig = tileProperty.boolTypeValue;
                        break;
                    case GridType.DropItem:
                        tileDetails.canDropItm = tileProperty.boolTypeValue;
                        break;
                    case GridType.PlaceFurniture:
                        tileDetails.canPlaceFurniturn = tileProperty.boolTypeValue;
                        break;
                    case GridType.NPCObstacle:
                        tileDetails.isNPCObstacle = tileProperty.boolTypeValue;
                        break;
                }
                if (GetTileDetailes(key) != null)
                    tileDetailesDict[key] = tileDetails;
                else
                    tileDetailesDict.Add(key, tileDetails);//���ֵ丳ֵ
            }
        }
        /// <summary>
        /// ����key������Ƭ��Ϣ
        /// </summary>
        /// <param name="key">x+y+��ͼ����</param>
        /// <returns></returns>
        public TileDetails GetTileDetailes(string key)
        {
            if (tileDetailesDict.ContainsKey(key))
            {
                return tileDetailesDict[key];
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// ��������������귵����Ƭ��Ϣ
        /// </summary>
        /// <param name="mouseGridPos">�����������</param>
        /// <returns></returns>
        public TileDetails GetTileDetailsOnMousePosition(Vector3Int mouseGridPos)
        {
            string key = mouseGridPos.x + "x" + mouseGridPos.y + "y" + SceneManager.GetActiveScene().name;
            return GetTileDetailes(key);
        }
        /// <summary>
        /// ִ��ʵ�ʹ��߻���Ʒ����
        /// </summary>
        /// <param name="mouseWorldPos">�������</param>
        /// <param name="itemDetails">��Ʒ��Ϣ</param>
        private void OnExecuteActionAfterAnimation(Vector3 mouseWorldPos, ItemDetails itemDetails)
        {
            var mouseGridPos = currentGrid.WorldToCell(mouseWorldPos);//���������ָ��Ƭ������
            var currentTile = GetTileDetailsOnMousePosition(mouseGridPos);//���������ָ��Ƭ
            if (currentTile != null)//�����ǰ��Ƭ��Ϣ��Ϊnull
            {
                Crop currentCrop = GetCropObject(mouseWorldPos);
                //WORKFLOW:��Ʒʹ��ʵ�ʹ���
                switch (itemDetails.itemType)
                {
                    case ItemType.Seed:
                        EventHandler.CallPlantSeedEvent(itemDetails.itemID, currentTile);
                        EventHandler.CallDropItemEvent(itemDetails.itemID, mouseWorldPos,itemDetails.itemType);
                        EventHandler.CallPlaySoundEvent(SoundName.Plant);
                        break;
                    case ItemType.Commodity:
                        EventHandler.CallDropItemEvent(itemDetails.itemID, mouseWorldPos, itemDetails.itemType);//����һ����Ʒ�����λ��
                        break;
                    case ItemType.HolTool:
                        SetDigGround(currentTile);
                        currentTile.daysSinceDug = 0;//�����ڿ�ʼ����Ӷ�������
                        currentTile.canDig = false;
                        currentTile.canDropItm = false;
                        //��Ч
                        EventHandler.CallPlaySoundEvent(SoundName.Hoe);
                        break;
                    case ItemType.WaterTool:
                        SetWaterGround(currentTile);
                        currentTile.daysSinceWatered = 0;
                        //��Ч
                        EventHandler.CallPlaySoundEvent(SoundName.Water);
                        break;
                    case ItemType.BreakTool:
                    case ItemType.ChopTool:
                        //ִ���ո��
                        currentCrop?.ProcessToolAction(itemDetails, currentCrop.tileDetails);
                        break;
                    case ItemType.CollectTool:
                        //ִ���ո��
                        currentCrop.ProcessToolAction(itemDetails,currentTile);
                        break;
                    case ItemType.ReapTool:
                        var reapCount = 0;
                        for (int i = 0; i < itemsInRadius.Count; i++)
                        {
                            EventHandler.CallParticaleEffectEvent(ParticaleEffectType.ReapableScenery, itemsInRadius[i].transform.position + Vector3.up);
                            itemsInRadius[i].SpawnHarvestItems();
                            Destroy(itemsInRadius[i].gameObject);
                            reapCount++;
                            if (reapCount >= Settings.reapAmount)
                            {
                                break;//����һ��һ�������ո�Ĳ���������ֱ���ո�һ��Ƭ
                            }
                        }
                        EventHandler.CallPlaySoundEvent(SoundName.Reap);
                        break;
                    case ItemType.Furniture:
                        //�ڵ�ͼ��������Ʒ ItemManager
                        //�Ƴ���ǰ��Ʒ(ͼֽ) InventoryManager
                        //�Ƴ���Դ��Ʒ InventoryManager
                        //����ֻ��Ҫ����һ���¼�����,Ȼ�󼤻��¼�֮��,��Ӧ�Ľű�ȥ�������Լ��øɵĻ��
                        EventHandler.CallBuildFurnitureEvent(itemDetails.itemID,mouseWorldPos);
                        break;
                }
                UpdateTileDetails(currentTile);
            }
        }
        /// <summary>
        /// ��������س����ֲ���ʵObject
        /// </summary>
        /// <param name="mouseWorldPos">�����λ��</param>
        /// <returns></returns>
        public Crop GetCropObject(Vector3 mouseWorldPos)
        {
            Collider2D[] colliders = Physics2D.OverlapPointAll(mouseWorldPos);//Physics2D.OverlapPointAll:����ĳ������Χ��������ײ��ŵ�һ��������
            Crop currentCrop = null;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].GetComponent<Crop>())
                {
                    currentCrop = colliders[i].GetComponent<Crop>();
                }
            }
            return currentCrop;
        }
        /// <summary>
        /// �ж�������ⷶΧ֮���Ƿ��п�ʵ���ո����Ʒ(��)
        /// </summary>
        /// <returns></returns>
        public bool HaveReapableItemsInRadius(Vector3 mouseWorldPos,ItemDetails tool)
        {
            itemsInRadius = new List<ReapItem>();
            Collider2D[] colliders = new Collider2D[20];//��⵽����ײ�����������
            //Բ���������(�������ĵ�,���ķ�Χ,��⵽��������뵽������)
            //OverlapCircleNonAlloc��OverlapCircle������
            //OverlapCircle��ϵͳ�Զ����㽫��⵽�����岻��new�������뵽��ʱList�б���
            //OverlapCircleNonAlloc���������趨��һ���̶���С������,����ϵͳ�Լ�new�б�����������,����CG
            Physics2D.OverlapCircleNonAlloc(mouseWorldPos, tool.itemUseRadius, colliders);
            if (colliders.Length > 0)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                    {
                        if (colliders[i].GetComponent<ReapItem>())
                        {
                            var item = colliders[i].GetComponent<ReapItem>();
                            itemsInRadius.Add(item);//��ReapItem�����б���
                        }
                    }
                }
            }
            return itemsInRadius.Count > 0;
        }
        /// <summary>
        /// ��ʾ�ڿ���Ƭ
        /// </summary>
        /// <param name="tile"></param>
        private void SetDigGround(TileDetails tile)
        {
            Vector3Int pos = new Vector3Int(tile.gridX, tile.gridY, 0);
            if (digTilemap != null)
            {
                digTilemap.SetTile(pos, digTile);//������Ƭ����(λ��,��Ƭ)
            }
        }
        /// <summary>
        /// ��ʾ��ˮ��Ƭ
        /// </summary>
        /// <param name="tile"></param>
        private void SetWaterGround(TileDetails tile)
        {
            Vector3Int pos = new Vector3Int(tile.gridX, tile.gridY, 0);
            if (waterTilemap != null)
            {
                waterTilemap.SetTile(pos, waterTile);//������Ƭ����(λ��,��Ƭ)
            }
        }
        /// <summary>
        /// ������Ƭ��Ϣ
        /// </summary>
        /// <param name="tileDetails"></param>
        public void UpdateTileDetails(TileDetails tileDetails)
        {
            string key = tileDetails.gridX + "x" + tileDetails.gridY + "y" + SceneManager.GetActiveScene().name;
            if (tileDetailesDict.ContainsKey(key))
            {
                tileDetailesDict[key] = tileDetails;
            }
            else
            {
                tileDetailesDict.Add(key, tileDetails);
            }
        }
        /// <summary>
        /// ˢ�µ�ǰ��ͼ
        /// </summary>
        private void RefreshMap()
        {
            if (digTilemap != null)
            {
                digTilemap.ClearAllTiles();
            }
            if (waterTilemap != null)
            {
                waterTilemap.ClearAllTiles();
            }
            foreach (var crop in FindObjectsOfType<Crop>())
            {
                Destroy(crop.gameObject);
            }
            DisplayMap(SceneManager.GetActiveScene().name);
        }
        /// <summary>
        /// ��ʾ��ͼ��Ƭ
        /// </summary>
        /// <param name="sceneName">��������</param>
        private void DisplayMap(string sceneName)
        {
            foreach (var tile in tileDetailesDict)
            {
                var key = tile.Key;
                var tileDetails = tile.Value;
                if (key.Contains(sceneName))
                {
                    if (tileDetails.daysSinceDug > -1)
                    {
                        SetDigGround(tileDetails);
                    }
                    if (tileDetails.daysSinceWatered > -1)
                    {
                        SetWaterGround(tileDetails);
                    }
                    //TODO:����
                    if (tileDetails.seedItemID > -1)//��ǰ��������������Ϣ
                        EventHandler.CallPlantSeedEvent(tileDetails.seedItemID, tileDetails);
                }
            }
        }
        /// <summary>
        /// ���ݳ������ֹ�������Χ,�����Χ��ԭ��
        /// </summary>
        /// <param name="sceneName">��������</param>
        /// <param name="gridDimensions">����Χ</param>
        /// <param name="gridOrigin">����ԭ��</param>
        /// <returns>�Ƿ��е�ǰ������Ϣ</returns>
        public bool GetGridDimensions(string sceneName,out Vector2Int gridDimensions,out Vector2Int gridOrigin)
        {
            gridDimensions = Vector2Int.zero;
            gridOrigin = Vector2Int.zero;//��ʼ��Ϊ0
            foreach (var mapData in mapDataList)//ѭ���ó�ÿһ�ŵ�ͼ
            {
                if (mapData.sceneName == sceneName)//����иó���
                {
                    gridDimensions.x = mapData.gridWidth;
                    gridDimensions.y = mapData.gridHeight;
                    gridOrigin.x = mapData.originX;
                    gridOrigin.y = mapData.originY;
                    return true;
                }//�򽫵�ǰ�����е�����ȫ���������ڵ��в�����true
            }
            return false;
        }

        public GameSaveData GenerateSaveData()
        {
            GameSaveData saveData = new GameSaveData();
            saveData.tileDetailsDict = this.tileDetailesDict;
            saveData.firstLoadDict = this.firstLoadDic;
            return saveData;
        }

        public void RestoreData(GameSaveData saveDate)
        {
            this.tileDetailesDict = saveDate.tileDetailsDict;
            this.firstLoadDic = saveDate.firstLoadDict;
        }

        /// <summary>
        /// 为NPC提供的公共接口：更新挖地瓦片显示
        /// NPC执行挖地操作后调用此方法更新地块视觉效果
        /// </summary>
        /// <param name="tileDetails">地块详细信息</param>
        public void SetDigGroundForNPC(TileDetails tileDetails)
        {
            if (tileDetails != null)
            {
                SetDigGround(tileDetails);
                Debug.Log($"NPC挖地瓦片已更新：位置({tileDetails.gridX}, {tileDetails.gridY})");
            }
        }
        
        /// <summary>
        /// 为NPC提供的公共接口：更新浇水瓦片显示
        /// NPC执行浇水操作后调用此方法更新地块视觉效果
        /// </summary>
        /// <param name="tileDetails">地块详细信息</param>
        public void SetWaterGroundForNPC(TileDetails tileDetails)
        {
            if (tileDetails != null)
            {
                SetWaterGround(tileDetails);
                Debug.Log($"NPC浇水瓦片已更新：位置({tileDetails.gridX}, {tileDetails.gridY})");
            }
        }
    }
}

