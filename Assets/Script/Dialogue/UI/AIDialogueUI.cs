using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MFarm.Dialogue
{
    public class AIDialogueUI : MonoBehaviour
    {
        [Header("对话输入")]
        [SerializeField] private GameObject inputPanel;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;

        [Header("对话继续/结束")]
        [SerializeField] private GameObject continuePanel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button endButton;

        [Header("设置面板")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TMP_InputField apiKeyInput;
        [SerializeField] private Button saveSettingsButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button openSettingsButton;

        private Action<string> currentSubmitCallback;
        private Action<bool> currentContinueCallback;

        private void OnEnable()
        {
            EventHandler.ShowAIDialogueInputEvent += OnShowAIDialogueInput;
            EventHandler.ShowAIContinueDialogueEvent += OnShowAIContinueDialogue;
        }

        private void OnDisable()
        {
            EventHandler.ShowAIDialogueInputEvent -= OnShowAIDialogueInput;
            EventHandler.ShowAIContinueDialogueEvent -= OnShowAIContinueDialogue;
        }

        private void Start()
        {
            // 确保初始状态下所有面板隐藏
            inputPanel.SetActive(false);
            continuePanel.SetActive(false);
            settingsPanel.SetActive(false);

            // 设置按钮监听
            submitButton.onClick.AddListener(OnSubmitInput);
            cancelButton.onClick.AddListener(OnCancelInput);
            continueButton.onClick.AddListener(() => OnContinueDecision(true));
            endButton.onClick.AddListener(() => OnContinueDecision(false));
            saveSettingsButton.onClick.AddListener(OnSaveSettings);
            closeSettingsButton.onClick.AddListener(() => settingsPanel.SetActive(false));
            openSettingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));

            // 加载API密钥
            apiKeyInput.text = PlayerPrefs.GetString("DeepSeekAPIKey", "");
        }

        /// <summary>
        /// 显示AI对话输入框
        /// </summary>
        private void OnShowAIDialogueInput(bool show, Action<string> submitCallback)
        {
            inputPanel.SetActive(show);
            
            if (show)
            {
                currentSubmitCallback = submitCallback;
                inputField.text = string.Empty;
                inputField.Select();
                inputField.ActivateInputField();
            }
        }

        /// <summary>
        /// 显示继续/结束对话选择面板
        /// </summary>
        private void OnShowAIContinueDialogue(Action<bool> decisionCallback)
        {
            continuePanel.SetActive(true);
            currentContinueCallback = decisionCallback;
        }

        /// <summary>
        /// 提交玩家输入
        /// </summary>
        private void OnSubmitInput()
        {
            string input = inputField.text.Trim();
            
            // 隐藏输入面板
            inputPanel.SetActive(false);
            
            // 调用回调函数
            currentSubmitCallback?.Invoke(input);
            currentSubmitCallback = null;
        }

        /// <summary>
        /// 取消输入
        /// </summary>
        private void OnCancelInput()
        {
            inputPanel.SetActive(false);
            currentSubmitCallback?.Invoke(string.Empty);
            currentSubmitCallback = null;
        }

        /// <summary>
        /// 处理继续/结束对话的决定
        /// </summary>
        private void OnContinueDecision(bool continueTalking)
        {
            continuePanel.SetActive(false);
            currentContinueCallback?.Invoke(continueTalking);
            currentContinueCallback = null;
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void OnSaveSettings()
        {
            string apiKey = apiKeyInput.text.Trim();
            PlayerPrefs.SetString("DeepSeekAPIKey", apiKey);
            PlayerPrefs.Save();
            
            // 通知更新API密钥
            EventHandler.CallUpdateAIApiKeyEvent(apiKey);
            
            // 隐藏设置面板
            settingsPanel.SetActive(false);
        }
    }
} 