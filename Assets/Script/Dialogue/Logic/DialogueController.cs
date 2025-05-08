using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace MFarm.Dialogue
{
    [RequireComponent(typeof(NPCMovement))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class DialogueController : MonoBehaviour
    {
        private NPCMovement npc => GetComponent<NPCMovement>();
        public UnityEvent OnFinishEvent;//Unity�Դ����¼���������,���Ӵ˺����ڽű���,�ýű��ͻ����һ���Ͱ�ť��OnClickEvent��ͬ���ܵ�ѡ��
        public List<DialoguePiece> dialogueList = new List<DialoguePiece>();
        private Stack<DialoguePiece> dialogueStack;//�Ƚ����
        private bool canTalk;
        private bool isTalking;
        private GameObject uiSign;

        protected virtual void Awake()
        {
            uiSign = transform.GetChild(1).gameObject;
            FillDialogueStack();
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                canTalk = !npc.isMoving && npc.interactable;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                canTalk = false;
            }
        }
        private void Update()
        {
            uiSign.SetActive(canTalk);
            if (canTalk && Input.GetKeyDown(KeyCode.Space) && !isTalking)
            {
                StartCoroutine(DialogueRoutine());
            }
        }
        //����ջ
        private void FillDialogueStack()
        {
            dialogueStack = new Stack<DialoguePiece>();
            for (int i = dialogueList.Count - 1; i > -1; i--)//��Ϊ��ջ��ѭ�Ƚ������ԭ��,����������õ���ѹ���ջ
            {
                dialogueList[i].isDone = false;
                dialogueStack.Push(dialogueList[i]);
            }
        }
        private IEnumerator DialogueRoutine()
        {
            isTalking = true;
            if(dialogueStack.TryPop(out DialoguePiece result))//Pop:��һ����һ��
            {
                //����UI��ʾ�Ի�
                EventHandler.CallShowDialogueEvent(result);
                EventHandler.CallUpdateGameStateEvent(GameState.Pause);
                yield return new WaitUntil(() => result.isDone);//WaitUntil:�ȴ�...ֱ��...(ֱ��װ�Ի�Ƭ�εĶ�ջ��û���˶Ի�Ƭ��)
                isTalking = false;
            }
            else
            {
                EventHandler.CallUpdateGameStateEvent(GameState.Gameplay);
                EventHandler.CallShowDialogueEvent(null);
                FillDialogueStack();//�Ի�Ƭ�δӶ�ջ���ÿ���,�����������
                isTalking = false;
                if (OnFinishEvent != null)
                {
                    OnFinishEvent.Invoke();
                    canTalk = false;
                }
            }
        }
    }
}

