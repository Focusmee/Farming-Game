using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
namespace MFarm.Save
{
    public class SaveLoadManager : Singleton<SaveLoadManager>
    {
        private List<ISaveable> saveableList = new List<ISaveable>();//<ISaveable>�ǽӿ�,�������б��п��Դ�����е�����ISaveable�ӿڵ���
        public List<DataSlot> dataSlots = new List<DataSlot>(new DataSlot[3]);//�浵����������
        private string jsonFolder;//���Json�ļ���·��
        private int currentDataIndex;//��ǰ���ڽ��е���Ϸ�Ĵ浵Slot��Index
        protected override void Awake()
        {
            base.Awake();
            jsonFolder = Application.persistentDataPath + "/SAVE DATA/";//����һ��ϵͳ·���µ���ΪSAVE DATA���ļ���(��б�ܱ��������ļ���,����б�ܱ������ļ�)
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
            // 检查是否已经有其他对象使用相同的GUID
            bool guidConflict = true;
            int retryCount = 0;
            const int maxRetries = 3; // 最大重试次数
            
            while (guidConflict && retryCount < maxRetries)
            {
                guidConflict = false;
                foreach (var existingSaveable in saveableList)
                {
                    if (existingSaveable.GUID == saveable.GUID && existingSaveable != saveable)
                    {
                        guidConflict = true;
                        retryCount++;
                        
                        Debug.LogWarning($"发现重复的GUID: {saveable.GUID}\n" +
                                         $"已存在对象: {existingSaveable}\n" +
                                         $"新对象: {saveable}\n" +
                                         $"尝试第 {retryCount} 次解决");
                        
                        // 尝试获取DataGUID组件并生成新的GUID
                        var dataGUID = (saveable as MonoBehaviour)?.GetComponent<DataGUID>();
                        if (dataGUID != null)
                        {
                            dataGUID.GenerateNewGUID();
                            Debug.Log($"为对象 {saveable} 生成了新的GUID: {saveable.GUID}");
                            break; // 重新检查新生成的GUID
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
                // u68c0u67e5GUIDu662fu5426u5df2u5b58u5728u4e8eu5b57u5178u4e2d
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
        }
    }
}

