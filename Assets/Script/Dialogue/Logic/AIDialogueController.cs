using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MFarm.Dialogue
{
    [RequireComponent(typeof(NPCMovement))]
    public class AIDialogueController : DialogueController
    {
        [Header("AI对话设置")]
        [SerializeField] private bool useAI = true;
        [SerializeField] private string npcRole = "友好的村民";
        [SerializeField] private string npcPersonality = "热情、乐于助人";
        [SerializeField] private string npcBackground = "在这个小镇生活了很久，了解这里的一切。";
        [SerializeField, TextArea(3, 5)] private string promptTemplate = "你是一个名叫{0}的{1}，性格{2}。{3}请以友好的方式回应玩家的问题：\n{4}";
        
        private DeepSeekClient deepSeekClient;
        private bool isWaitingForAIResponse = false;
        private DialoguePiece currentAIDialogue;
        
        protected override void Awake()
        {
            base.Awake();
            
            // 获取或添加DeepSeekClient组件
            deepSeekClient = FindObjectOfType<DeepSeekClient>();
            if (deepSeekClient == null)
            {
                GameObject clientObject = new GameObject("DeepSeekClient");
                deepSeekClient = clientObject.AddComponent<DeepSeekClient>();
                DontDestroyOnLoad(clientObject); // 防止场景切换时被销毁
            }
            
            // 创建AI对话使用的对话片段
            currentAIDialogue = new DialoguePiece
            {
                name = gameObject.name,
                onLeft = false, // 根据UI布局调整
                hasToPause = false
            };
        }
        
        /// <summary>
        /// 显示AI对话输入框并处理玩家输入
        /// </summary>
        public void StartAIDialogue()
        {
            if (!useAI || isWaitingForAIResponse)
                return;
                
            // 显示输入框UI
            EventHandler.CallShowAIDialogueInputEvent(true, OnPlayerInputSubmitted);
            // 暂停游戏状态
            EventHandler.CallUpdateGameStateEvent(GameState.Pause);
        }
        
        /// <summary>
        /// 处理玩家提交的对话输入
        /// </summary>
        /// <param name="playerInput">玩家输入的文本</param>
        private void OnPlayerInputSubmitted(string playerInput)
        {
            if (string.IsNullOrEmpty(playerInput))
            {
                // 如果输入为空，恢复游戏状态
                EventHandler.CallUpdateGameStateEvent(GameState.Gameplay);
                return;
            }
            
            isWaitingForAIResponse = true;
            
            // 显示"正在思考"的对话
            currentAIDialogue.dialogueText = "正在思考...";
            EventHandler.CallShowDialogueEvent(currentAIDialogue);
            
            // 构建提示词
            string prompt = string.Format(
                promptTemplate, 
                gameObject.name,
                npcRole, 
                npcPersonality, 
                npcBackground, 
                playerInput
            );
            
            // 调用DeepSeek API
            StartCoroutine(deepSeekClient.GetResponse(prompt, OnAIResponseReceived));
        }
        
        /// <summary>
        /// 处理AI返回的回复
        /// </summary>
        /// <param name="response">AI生成的回复文本</param>
        private void OnAIResponseReceived(string response)
        {
            isWaitingForAIResponse = false;
            
            // 更新对话内容
            currentAIDialogue.dialogueText = response;
            currentAIDialogue.isDone = false;
            
            // 显示AI回复
            EventHandler.CallShowDialogueEvent(currentAIDialogue);
            
            // 等待对话完成后，允许玩家继续输入或结束对话
            StartCoroutine(WaitForDialogueComplete());
        }
        
        private IEnumerator WaitForDialogueComplete()
        {
            yield return new WaitUntil(() => currentAIDialogue.isDone);
            
            // 询问玩家是否继续对话
            EventHandler.CallShowAIContinueDialogueEvent(OnDialogueContinuationDecided);
        }
        
        private void OnDialogueContinuationDecided(bool continueTalking)
        {
            if (continueTalking)
            {
                // 继续对话，显示输入框
                StartAIDialogue();
            }
            else
            {
                // 结束对话，恢复游戏状态
                EventHandler.CallUpdateGameStateEvent(GameState.Gameplay);
                EventHandler.CallShowDialogueEvent(null);
            }
        }
        
        // 在玩家进入触发区域时可以开始AI对话
        protected override void OnTriggerStay2D(Collider2D other)
        {
            base.OnTriggerStay2D(other);
            
            if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E) && !isWaitingForAIResponse && useAI)
            {
                StartAIDialogue();
            }
        }
    }
} 