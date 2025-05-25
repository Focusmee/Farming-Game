using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MFarm.Save;

public class SaveSlotUI : MonoBehaviour
{
    public Text dataTime, dataScene;
    private Button currentButton;
    private DataSlot currentData;
    private int index => transform.GetSiblingIndex();//得到当前object在同级Hierarchy中的位置索引
    private void Awake()
    {
        currentButton = GetComponent<Button>();
        currentButton.onClick.AddListener(LoadGameData);//为当前按钮添加监听事件
        //这里表示当前按钮与调用(LoadGameData)绑定
    }
    private void OnEnable()
    {
        SetupSlotUI();
    }
    private void SetupSlotUI()
    {
        currentData = SaveLoadManager.Instance.dataSlots[index];
        if (currentData != null)
        {
            dataTime.text = currentData.DataTime;
            dataScene.text = currentData.DataScene;
        }
        else
        {
            dataTime.text = "空存档还没有开始";
            dataScene.text = "新游戏还未开始";
        }
    }
    private void LoadGameData()
    {
        if (currentData != null)
        {
            // 加载存档 - 设置标志表示这是加载旧存档
            Debug.Log("加载存档 " + index);
            SaveLoadManager.Instance.isLoadingExistingSave = true;
            SaveLoadManager.Instance.Load(index);
        }
        else
        {
            // 开始新游戏 - 清除标志
            Debug.Log("新游戏");
            SaveLoadManager.Instance.isLoadingExistingSave = false;
            EventHandler.CallStartNewGameEvent(index);
        }
    }
}
