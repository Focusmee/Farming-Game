using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace MFarm.Dialogue
{
    public class DeepSeekClient : MonoBehaviour
    {
        // DeepSeek API配置
        private const string API_URL = "https://api.deepseek.com/v1/chat/completions";
        private string apiKey;

        [SerializeField] private bool useDebugMode = false;
        [SerializeField] private string debugResponse = "这是一个测试回复，用于调试模式。";

        // 模型配置
        [SerializeField] private string modelName = "deepseek-chat";
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private int maxTokens = 1000;

        private void Awake()
        {
            apiKey = PlayerPrefs.GetString("DeepSeekAPIKey", "");
                        apiKey = "sk-9ee58c3306b34f23995e42907ae6d6ff";

        }

        private void OnEnable()
        {
            EventHandler.UpdateAIApiKeyEvent += OnUpdateApiKey;
        }

        private void OnDisable()
        {
            EventHandler.UpdateAIApiKeyEvent -= OnUpdateApiKey;
        }

        private void OnUpdateApiKey(string newApiKey)
        {
            SetApiKey(newApiKey);
        }

        /// <summary>
        /// 调用DeepSeek API获取对话回复
        /// </summary>
        /// <param name="prompt">发送给模型的提示</param>
        /// <param name="onComplete">回调函数，处理API返回的结果</param>
        /// <returns>协程</returns>
        public IEnumerator GetResponse(string prompt, Action<string> onComplete)
        {
            // 如果在调试模式下，直接返回测试回复
            if (useDebugMode)
            {
                onComplete?.Invoke(debugResponse);
                yield break;
            }

            // 检查API密钥是否设置
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("DeepSeek API Key未设置！请在设置中配置API密钥。");
                onComplete?.Invoke("对不起，我暂时无法回应，因为未配置API密钥。");
                yield break;
            }

            // 构建请求数据
            var requestData = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = temperature,
                max_tokens = maxTokens,
                stream = false
            };

            string jsonData = JsonConvert.SerializeObject(requestData);
            Debug.Log($"Sending request to DeepSeek API: {jsonData}");

            using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"API请求错误: {request.error}\n响应: {request.downloadHandler.text}");
                    onComplete?.Invoke("对不起，我暂时遇到了一些问题，无法回应。");
                }
                else
                {
                    try
                    {
                        Debug.Log($"API Response: {request.downloadHandler.text}");
                        var response = JsonConvert.DeserializeObject<DeepSeekResponse>(request.downloadHandler.text);
                        string aiMessage = response?.choices[0]?.message?.content;
                        
                        if (!string.IsNullOrEmpty(aiMessage))
                        {
                            onComplete?.Invoke(aiMessage);
                        }
                        else
                        {
                            Debug.LogError("API返回了空响应");
                            onComplete?.Invoke("对不起，我无法提供回应。");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"解析API响应时出错: {e.Message}");
                        onComplete?.Invoke("对不起，处理回应时出现了错误。");
                    }
                }
            }
        }

        // DeepSeek API响应结构
        [Serializable]
        private class DeepSeekResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
        }

        [Serializable]
        private class Message
        {
            public string content;
        }

        /// <summary>
        /// 设置API密钥
        /// </summary>
        public void SetApiKey(string key)
        {
            apiKey = key;
            PlayerPrefs.SetString("DeepSeekAPIKey", key);
            PlayerPrefs.Save();
            Debug.Log("API密钥已更新");
        }
    }
} 