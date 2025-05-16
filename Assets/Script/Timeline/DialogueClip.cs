using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using MFarm.Dialogue;

[System.Serializable]
public class DialogueClip : PlayableAsset/*ĻPlayableʵ*/,ITimelineClipAsset//ýӿڿʵTimelineĸ߼
{
    public ClipCaps clipCaps => ClipCaps.None;
    
    [SerializeField]
    public DialogueBehaviour dialogue = new DialogueBehaviour();

    // 添加一个序列化字段，用于在Inspector中设置对话内容
    [SerializeField]
    private DialoguePiece dialoguePiece = new DialoguePiece();

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DialogueBehaviour>.Create(graph, dialogue);
        
        // 将设置好的对话内容传递给行为类
        DialogueBehaviour clone = playable.GetBehaviour();
        clone.dialoguePiece = dialoguePiece;
        
        return playable;
    }
}
