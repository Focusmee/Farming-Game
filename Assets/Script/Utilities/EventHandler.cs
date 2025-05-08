using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MFarm.Dialogue;

public static class EventHandler //����һ���ű���������Ϸ�����е��¼�����̬�ģ�ȫ�ֵ�
{

    public static event Action<InventoryLocation, List<InventoryItem>> UpdateInventoryUI; //����һ��ί���¼�����Ҫ֪�����¸��ӵ�λ�ú͸��µĵ�������
    public static void CallUpdateInventoryUI(InventoryLocation loaction, List<InventoryItem> list)//�����ű�������ʱ���и��¼�
    {
        UpdateInventoryUI?.Invoke(loaction, list);//?.:���ж��¼�ί���Ƿ�Ϊ���ټ���
    }

    public static event Action<int, Vector3> InstantiateItemInScene;//ί���¼�:�ڳ����д���һ������Ԥ����
    public static void CallInstantiateItemInScene(int ID,Vector3 pos)
    {
        InstantiateItemInScene?.Invoke(ID,pos);
    }
    public static event Action<int, Vector3,ItemType> DropItemEvent;//���ﶪ�����ߺ󴥷��¼�ί��
    public static void CallDropItemEvent(int ID,Vector3 pos,ItemType itemType)
    {
        DropItemEvent?.Invoke(ID, pos,itemType);
    }
    public static event Action<ItemDetails, bool> ItemSelectedEvent;
    public static void CallItemSelectedEvent(ItemDetails itemDetails,bool isSelected)
    {
        ItemSelectedEvent?.Invoke(itemDetails, isSelected);
    }
    public static event Action<int, int,Season,int> GameMinuteEvent;
    public static void CallGameMinuteEvent(int minute,int hour,Season season,int day)
    {
        GameMinuteEvent?.Invoke(minute,hour,season,day);
    }
    public static event Action<int, int, int, int, Season> GameDateSeason;
    public static void CallGameDateSeason(int hour,int day,int month,int year,Season season)
    {
        GameDateSeason?.Invoke(hour,day,month,year,season);
    }
    public static event Action<string, Vector3> TransitionEvent;//�����л�ί���¼�
    public static void CallTransitionEvent(string sceneName,Vector3 pos)
    {
        TransitionEvent?.Invoke(sceneName, pos);
    }
    public static event Action BeforeSceneUnloadEvent;//����ж��֮ǰ��Ҫ����һЩ�¼������ⱨ��
    public static void CallBeforeSceneUnloadEvent()
    {
        BeforeSceneUnloadEvent?.Invoke();
    }
    public static event Action AfterSceneLoadedEvent;//���س���֮����Ҫ����һЩ�¼����л�����
    public static void CallAfterSceneLoadedEvent()
    {
        AfterSceneLoadedEvent?.Invoke();
    }
    public static event Action<Vector3> MoveToPosition;//�л����������ƶ���ָ��λ��ί���¼�
    public static void CallMoveToPosition(Vector3 targetPosition)
    {
        MoveToPosition?.Invoke(targetPosition);
    }
    public static event Action<Vector3, ItemDetails> MouseClickedEvent;//������¼�
    public static void CallMouseClickedEvent(Vector3 pos,ItemDetails itemDetails)
    {
        MouseClickedEvent?.Invoke(pos,itemDetails);
    }
    public static event Action<Vector3, ItemDetails> ExecuteActionAfterAnimation;//ʵ���¼�,����Playerĳ����������������ɺ���õ�ʵ���¼�����
    public static void CallExecuteActionAfterAnimation(Vector3 pos,ItemDetails itemDetails)
    {
        ExecuteActionAfterAnimation?.Invoke(pos, itemDetails);
    }
    public static event Action<int, Season> GameDayEvent;//ÿ��ί���¼�,ÿ�µ�һ�죬����һ�δ�ί��
    public static void CallGameDayEvent(int day,Season season)
    {
        GameDayEvent?.Invoke(day, season);
    }
    public static event Action<int, TileDetails> PlantSeedEvent;
    public static void CallPlantSeedEvent(int ID,TileDetails tile)
    {
        PlantSeedEvent?.Invoke(ID, tile);
    }
    public static event Action<int> HarvestAtPlayerPostion;//�����ָ��λ������ID��Ʒ
    public static void CallHarvestAtPlayerPostion(int ID)
    {
        HarvestAtPlayerPostion?.Invoke(ID);
    }
    public static event Action RefreshCurrentMap;//ˢ�µ�ǰ��ͼί���¼�
    public static void CallRefreshCurrentMap()
    {
        RefreshCurrentMap?.Invoke();
    }
    public static event Action<ParticaleEffectType, Vector3> ParticaleEffectEvent;//������Ч�¼�ί��
    public static void CallParticaleEffectEvent(ParticaleEffectType effectType,Vector3 pos)
    {
        ParticaleEffectEvent?.Invoke(effectType, pos);
    }
    public static event Action GenerateCropEvent;
    public static void CallGenerateCropEvent()//ֱ���ڳ���������������ũ������¼�
    {
        GenerateCropEvent?.Invoke();
    }
    public static event Action<DialoguePiece> ShowDialogueEvent;
    public static void CallShowDialogueEvent(DialoguePiece piece)
    {
        ShowDialogueEvent?.Invoke(piece);
    }
    //�̵꿪���¼�
    public static event Action<SlotType, InventoryBag_SO> BaseBagOpenEvent;//�������ί���¼�,�̵�ͻῪ��
    public static void CallBaseBagOpenEvent(SlotType slotType,InventoryBag_SO bag_SO)
    {
        BaseBagOpenEvent?.Invoke(slotType, bag_SO);
    }
    //�̵�ر��¼�
    public static event Action<SlotType, InventoryBag_SO> BaseBagCloseEvent;//�������ί���¼�,�̵�ͻ�ر�
    public static void CallBaseBagCloseEvent(SlotType slotType, InventoryBag_SO bag_SO)
    {
        BaseBagCloseEvent?.Invoke(slotType, bag_SO);
    }
    //������Ϸ��ǰ״̬ί���¼�
    public static event Action<GameState> UpdateGameStateEvent;//��Ϸ�н�����״̬����ͣ״̬
    public static void CallUpdateGameStateEvent(GameState gameState)
    {
        UpdateGameStateEvent?.Invoke(gameState);
    }
    public static event Action<ItemDetails, bool> ShowTradeUI;
    public static void CallShowTradeUI(ItemDetails item,bool isSell)
    {
        ShowTradeUI?.Invoke(item,isSell);
    }
    //����
    public static event Action<int,Vector3> BuildFurnitureEvent;
    public static void CallBuildFurnitureEvent(int ID,Vector3 pos)
    {
        BuildFurnitureEvent?.Invoke(ID,pos);
    }
    //�ƹ�
    public static event Action<Season, LightShift, float> LightShiftChangeEvent;
    public static void CallLightShiftChangeEvent(Season season,LightShift lightShift,float timeDifference)
    {
        LightShiftChangeEvent?.Invoke(season, lightShift, timeDifference);
    }
    //��Ч
    public static event Action<SoundDetails> InitSoundEffect;
    public static void CallInitSoundEffect(SoundDetails soundDetails)
    {
        InitSoundEffect?.Invoke(soundDetails);
    }
    public static event Action<SoundName> PlaySoundEvent;
    public static void CallPlaySoundEvent(SoundName soundName)
    {
        PlaySoundEvent?.Invoke(soundName);
    }
    public static event Action<int> StartNewGameEvent;
    public static void CallStartNewGameEvent(int index)
    {
        StartNewGameEvent?.Invoke(index);
    }
    public static event Action EndGameEvent;
    public static void CallEndGameEvent()
    {
        EndGameEvent?.Invoke();
    }

    // AI对话输入框事件
    public static event Action<bool, Action<string>> ShowAIDialogueInputEvent;
    public static void CallShowAIDialogueInputEvent(bool show, Action<string> submitCallback)
    {
        ShowAIDialogueInputEvent?.Invoke(show, submitCallback);
    }

    // AI对话继续/结束选择事件
    public static event Action<Action<bool>> ShowAIContinueDialogueEvent;
    public static void CallShowAIContinueDialogueEvent(Action<bool> decisionCallback)
    {
        ShowAIContinueDialogueEvent?.Invoke(decisionCallback);
    }

    // AI对话配置更新事件
    public static event Action<string> UpdateAIApiKeyEvent;
    public static void CallUpdateAIApiKeyEvent(string apiKey)
    {
        UpdateAIApiKeyEvent?.Invoke(apiKey);
    }
}
