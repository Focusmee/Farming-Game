using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MFarm.Save
{
    [System.Serializable]
    //这只是一个数据存储的类
    public class GameSaveData//创建一个类来记录所有Save时需要保存的数据，保存到字典上，Load游戏时直接将字典内的数据全部还原到游戏里去
    {
        public string dataSceneName;//需要保存场景名
        public Dictionary<string, SerializableVector3> characterPosDict;//保存人物坐标(string:NPC名，坐标数据)
        public Dictionary<string, List<SceneItem>> sceneItemDict;//保存场景中的物品数据
        public Dictionary<string, List<SceneFurniture>> sceneFurnitureDict;//保存场景中的家具数据
        public Dictionary<string, TileDetails> tileDetailsDict;//保存场景中的瓦片信息需要记录
        public Dictionary<string, bool> firstLoadDict;//保存是否第一次加载也需要记录
        public Dictionary<string, List<InventoryItem>> inventoryDict;//保存场景中的物品，也需要保存
        public Dictionary<string, int> timeDict;//时间也需要保存
        public int playerMoney;//玩家的钱需要保存
        //NPC
        public string targetScene;//NPC的目标场景也需要保存
        public bool interactable;//NPC是否可对话，也需要保存
        public int animationInstanceID;//NPC的动画，通过实例化来保存，因为这样通过这个ID来区分

        // 任务系统数据
        public Dictionary<string, QuestStatus> questStatusDict;    // 任务状态字典
        public Dictionary<string, int> questProgressDict;          // 任务进度字典
        
        // 开场动画相关数据
        public bool hasSeenIntro = false;   // 标记玩家是否已经看过开场动画
    }
}

