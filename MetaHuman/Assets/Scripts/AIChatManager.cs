using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIChatManager : MonoBehaviour
{
    [Header("API 设置")]
    [Tooltip("API Key (火山方舟/DeepSeek)")]
    public string apiKey = "sk-7dee18f1d37a48d6aaa3b907e74e4bc3";

    [Tooltip("模型 ID - 例如: deepseek-chat, deepseek-reasoner")]
    public string model = "deepseek-chat";

    [Tooltip("API 基础 URL")]
    public string baseUrl = "https://api.deepseek.com/v1";

    [Header("对话设置")]
    [Tooltip("最大历史消息数")]
    public int maxMessages = 50;

    [Tooltip("Temperature (创造性, 0-2)")]
    public float temperature = 0.7f;

    [Tooltip("系统提示词")]
    public string systemPrompt = "你是一个友好的AI助手。";

    private List<MessageItem> messageHistory = new List<MessageItem>();
    private bool isWaiting = false;

    public event Action<string> OnMessageReceived;
    public event Action<string> OnError;

    [Serializable]
    private class MessageItem
    {
        public string role;
        public string content;

        public MessageItem(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [Serializable]
    private class ApiResponse
    {
        public List<Choice> choices;

        [Serializable]
        public class Choice
        {
            public MessageItem message;

            [Serializable]
            public class MessageItem
            {
                public string role;
                public string content;
            }
        }
    }

    private void Start()
    {
        InitializeWithSystemPrompt();
    }

    private void InitializeWithSystemPrompt()
    {
        messageHistory.Clear();
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messageHistory.Add(new MessageItem("system", systemPrompt));
            Debug.Log("已设置AI角色：" + systemPrompt);
        }
    }

    public void SendMessage(string userMessage)
    {
        Debug.Log($"[AIChat] Model: {model}, API Key已设置: {!string.IsNullOrEmpty(apiKey)}");

        if (isWaiting)
        {
            Debug.LogWarning("[AIChat] 等待中，请稍候...");
            return;
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            OnError?.Invoke("请先设置 API Key");
            return;
        }

        StartCoroutine(SendMessageCoroutine(userMessage));
    }

    private string BuildJsonRequest()
    {
        var sb = new StringBuilder();
        sb.Append("{\"model\":\"");
        sb.Append(model);
        sb.Append("\",\"messages\":[");

        for (int i = 0; i < messageHistory.Count; i++)
        {
            var msg = messageHistory[i];
            sb.Append("{\"role\":\"");
            sb.Append(msg.role);
            sb.Append("\",\"content\":\"");
            sb.Append(EscapeJson(msg.content));
            sb.Append("\"}");
            if (i < messageHistory.Count - 1)
                sb.Append(",");
        }

        sb.Append("],\"temperature\":");
        sb.Append(temperature.ToString(System.Globalization.CultureInfo.InvariantCulture));
        sb.Append("}");

        return sb.ToString();
    }

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }

    private IEnumerator SendMessageCoroutine(string userMessage)
    {
        isWaiting = true;

        messageHistory.Add(new MessageItem("user", userMessage));

        while (messageHistory.Count > maxMessages + 1)
        {
            messageHistory.RemoveAt(1);
        }

        string json = BuildJsonRequest();
        Debug.Log($"[AIChat] Request Body: {json}");

        string fullUrl = baseUrl.TrimEnd('/') + "/chat/completions";

        using (UnityWebRequest www = new UnityWebRequest(fullUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"[AIChat] Response: {responseText}");

                try
                {
                    ApiResponse response = JsonUtility.FromJson<ApiResponse>(responseText);
                    if (response != null && response.choices != null && response.choices.Count > 0)
                    {
                        string aiMessage = response.choices[0].message.content;
                        messageHistory.Add(new MessageItem("assistant", aiMessage));
                        OnMessageReceived?.Invoke(aiMessage);
                    }
                    else
                    {
                        OnError?.Invoke("响应格式错误");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Parse Error: {e.Message}");
                    OnError?.Invoke("解析响应失败: " + e.Message);
                }
            }
            else
            {
                string error = www.error ?? "Unknown error";
                string responseBody = www.downloadHandler?.text ?? "No response body";
                Debug.LogError($"API Error: {error}");
                Debug.LogError($"Response Code: {www.responseCode}");
                Debug.LogError($"Response Body: {responseBody}");
                OnError?.Invoke($"API错误 ({www.responseCode}): {error}");
                messageHistory.RemoveAt(messageHistory.Count - 1);
            }
        }

        isWaiting = false;
    }

    public void ClearHistory()
    {
        InitializeWithSystemPrompt();
    }

    public bool IsWaiting => isWaiting;
}
