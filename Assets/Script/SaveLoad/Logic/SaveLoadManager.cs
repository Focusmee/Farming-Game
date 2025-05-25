using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
namespace MFarm.Save
{
    public class SaveLoadManager : Singleton<SaveLoadManager>
    {
        private List<ISaveable> saveableList = new List<ISaveable>();//<ISaveable>是接口,这个数组列表中可以存储所有继承ISaveable接口的类
        public List<DataSlot> dataSlots = new List<DataSlot>(new DataSlot[3]);//存档数据列表索引
        private string jsonFolder;//存放Json文件的路径
        private int currentDataIndex;//当前正在进行的游戏的存档Slot的Index
        
        /// <summary>
        /// 标志是否正在加载已有存档（而不是开始新游戏）
        /// </summary>
        [System.NonSerialized]
        public bool isLoadingExistingSave = false;
        
        protected override void Awake()
        {
            base.Awake();
            jsonFolder = Application.persistentDataPath + "/SAVE DATA/";//创建一个系统路径下的命名为SAVE DATA的文件夹(不会被覆盖的文件夹,即便覆盖软件文件)
            ReadSaveData();
        }
        private void OnEnable()
        {
            EventHandler.StartNewGameEvent += OnStartNewGameEvent;
            EventHandler.EndGameEvent += OnEndGameEvent;
        }
        private void OnDisable()
        {
            EventHandler.StartNewGameEvent -= OnStartNewGameEvent;
            EventHandler.EndGameEvent -= OnEndGameEvent;
        }

        private void OnEndGameEvent()
        {
            Save(currentDataIndex);
        }

        private void OnStartNewGameEvent(int index)
        {
            currentDataIndex = index;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                Save(currentDataIndex);
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                Load(currentDataIndex);
            }
        }

        public void RegisterSaveable(ISaveable saveable)
        {
            if (saveable == null)
            {
                Debug.LogError("尝试注册空的ISaveable对象");
                return;
            }

            string saveableGUID;
            try
            {
                // 安全地获取GUID
                saveableGUID = saveable.GUID;
                if (string.IsNullOrEmpty(saveableGUID))
                {
                    Debug.LogError($"对象 {saveable} 的GUID为空，无法注册");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"获取对象 {saveable} 的GUID时发生错误: {ex.Message}");
                return;
            }

            // 检查是否已经有其他对象使用相同的GUID
            bool guidConflict = true;
            int retryCount = 0;
            const int maxRetries = 3; // 最大重试次数
            
            while (guidConflict && retryCount < maxRetries)
            {
                guidConflict = false;
                foreach (var existingSaveable in saveableList)
                {
                    string existingGUID;
                    try
                    {
                        existingGUID = existingSaveable.GUID;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"获取已存在对象的GUID时出错: {ex.Message}，跳过该对象");
                        continue;
                    }

                    if (existingGUID == saveableGUID && existingSaveable != saveable)
                    {
                        guidConflict = true;
                        retryCount++;
                        
                        Debug.LogWarning($"发现重复的GUID: {saveableGUID}\n" +
                                         $"已存在对象: {existingSaveable}\n" +
                                         $"新对象: {saveable}\n" +
                                         $"尝试第 {retryCount} 次解决");
                        
                        // 尝试获取DataGUID组件并生成新的GUID
                        var dataGUID = (saveable as MonoBehaviour)?.GetComponent<DataGUID>();
                        if (dataGUID != null)
                        {
                            dataGUID.GenerateNewGUID();
                            try
                            {
                                saveableGUID = saveable.GUID; // 更新saveableGUID变量
                                Debug.Log($"为对象 {saveable} 生成了新的GUID: {saveableGUID}");
                                break; // 重新检查新生成的GUID
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"生成新GUID后获取失败: {ex.Message}");
                                return;
                            }
                        }
                        else
                        {
                            Debug.LogError($"无法为对象 {saveable} 生成新的GUID，未找到DataGUID组件");
                            return; // 避免添加有重复GUID的对象
                        }
                    }
                }
            }
            
            if (retryCount >= maxRetries && guidConflict)
            {
                Debug.LogError($"对象 {saveable} 的GUID冲突经过 {maxRetries} 次尝试仍无法解决");
                return;
            }
            
            if (!saveableList.Contains(saveable))
            {
                saveableList.Add(saveable);
                Debug.Log($"成功注册可保存对象: {saveable} (GUID: {saveableGUID})");
            }
            else
            {
                Debug.LogWarning($"对象 {saveable} 已经在可保存列表中");
            }
        }
        private void ReadSaveData()
        {
            if (Directory.Exists(jsonFolder))
            {
                for (int i = 0; i < dataSlots.Count; i++)
                {
                    var resultPath = jsonFolder + "data" + i + ".json";
                    if (File.Exists(resultPath))
                    {
                        var stringData = File.ReadAllText(resultPath);
                        var jsonData = JsonConvert.DeserializeObject<DataSlot>(stringData);
                        dataSlots[i] = jsonData;
                    }
                }
            }
        }
        private void Save(int index)
        {
            DataSlot data = new DataSlot();
            foreach (var saveable in saveableList)
            {
                // 检查GUID是否已存在于字典中
                if (!data.dataDict.ContainsKey(saveable.GUID))
                {
                    data.dataDict.Add(saveable.GUID, saveable.GenerateSaveData());
                }
                else
                {
                    Debug.LogWarning("发现重复的GUID: " + saveable.GUID + "，跳过添加该项");
                    // 可选：如果想要更新而不是跳过，可以使用下面的代码
                    // data.dataDict[saveable.GUID] = saveable.GenerateSaveData();
                }
            }
            dataSlots[index] = data;
            
            var resultPath = jsonFolder + "data" + index + ".json";
            var jsonData = JsonConvert.SerializeObject(dataSlots[index], Formatting.Indented);
            if (!File.Exists(resultPath))
            {
                Directory.CreateDirectory(jsonFolder);
            }
            Debug.Log("DATA" + index + "SAVED!");
            File.WriteAllText(resultPath, jsonData);
        }
        public void Load(int index)
        {
            currentDataIndex = index;
            var resultPath = jsonFolder + "data" + index + ".json";
            
            if (!File.Exists(resultPath))
            {
                Debug.LogWarning($"存档文件不存在: {resultPath}");
                return;
            }
            
            var stringData = File.ReadAllText(resultPath);
            var jsonData = JsonConvert.DeserializeObject<DataSlot>(stringData);
            
            if (jsonData == null)
            {
                Debug.LogError($"无法解析存档数据: {resultPath}");
                return;
            }
            
            foreach (var saveable in saveableList)
            {
                try
                {
                    // 检查saveable.GUID是否存在于字典中
                    if (jsonData.dataDict.ContainsKey(saveable.GUID))
                    {
                        saveable.RestoreData(jsonData.dataDict[saveable.GUID]);
                    }
                    else
                    {
                        Debug.LogWarning($"存档中不包含对象 {saveable} 的数据，GUID: {saveable.GUID}");
                        // 这里可以添加默认值或跳过此对象的恢复
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"恢复对象 {saveable} 数据时出错: {ex.Message}");
                }
            }
            
            Debug.Log($"加载存档 {index} 完成");
            
            // 如果是加载已有存档，触发加载场景而不是新游戏事件
            if (isLoadingExistingSave)
            {
                // 直接切换到游戏状态，不触发新游戏事件
                StartCoroutine(LoadGameAfterRestore());
            }
            else
            {
                // 如果是新游戏，触发新游戏事件
                EventHandler.CallStartNewGameEvent(index);
            }
        }
        
        /// <summary>
        /// 在恢复存档数据后加载游戏
        /// </summary>
        private IEnumerator LoadGameAfterRestore()
        {
            // 等待一帧确保所有数据恢复完成
            yield return null;
            
            // 直接切换到游戏状态
            EventHandler.CallUpdateGameStateEvent(GameState.Gameplay);
            
            // 重置标志
            isLoadingExistingSave = false;
        }
    }
}

