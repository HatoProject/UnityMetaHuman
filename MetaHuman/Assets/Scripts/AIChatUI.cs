using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class AIChatUI : MonoBehaviour
{
    [Header("UI 组件")]
    public InputField inputField;
    public Button sendButton;
    public Button clearButton;
    public Button settingsButton;
    public TextMeshProUGUI chatDisplay;
    public ScrollRect scrollRect;
    public GameObject panel;

    [Header("API 配置")]
    public AIChatManager chatManager;

    [Header("设置面板")]
    public InputField apiKeyInput;
    public InputField systemPromptInput;
    public Dropdown modelDropdown;

    private bool isPanelVisible = false;

    private void Start()
    {
        Debug.Log("[AIChatUI] Start() begin");

        if (chatManager == null)
        {
            chatManager = FindObjectOfType<AIChatManager>();
            Debug.Log($"[AIChatUI] chatManager auto-found: {chatManager != null}");
        }

        if (sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(OnSendClicked);
            Debug.Log("[AIChatUI] sendButton OK");
        }
        else
        {
            Debug.LogWarning("[AIChatUI] sendButton is NULL!");
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(OnClearClicked);
        }
        else
        {
            Debug.LogWarning("[AIChatUI] clearButton is NULL!");
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnToggleSettings);
            Debug.Log("[AIChatUI] settingsButton OK");
        }
        else
        {
            Debug.LogWarning("[AIChatUI] settingsButton is NULL!");
        }

        if (inputField != null)
        {
            inputField.onEndEdit.RemoveAllListeners();
            inputField.onEndEdit.AddListener(OnInputEndEdit);
            Debug.Log("[AIChatUI] inputField OK");
        }
        else
        {
            Debug.LogWarning("[AIChatUI] inputField is NULL!");
        }

        if (chatManager != null)
        {
            chatManager.OnMessageReceived -= OnAIResponse;
            chatManager.OnMessageReceived += OnAIResponse;
            chatManager.OnError -= OnError;
            chatManager.OnError += OnError;

            if (apiKeyInput != null)
            {
                string savedKey = PlayerPrefs.GetString("DeepSeekAPIKey", chatManager.apiKey);
                chatManager.apiKey = savedKey;
                apiKeyInput.text = savedKey;
            }

            if (systemPromptInput != null)
            {
                string savedPrompt = PlayerPrefs.GetString("SystemPrompt", chatManager.systemPrompt);
                chatManager.systemPrompt = savedPrompt;
                systemPromptInput.text = savedPrompt;
            }
        }
        else
        {
            Debug.LogWarning("[AIChatUI] chatManager is NULL!");
        }

        Debug.Log($"[AIChatUI] chatDisplay = {chatDisplay}");
        Debug.Log($"[AIChatUI] scrollRect = {scrollRect}");

        if (panel != null)
            panel.SetActive(false);

        Debug.Log("[AIChatUI] Start() done");
    }

    private void OnInputEndEdit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        OnSendClicked(text);
    }

    public void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;
        if (panel != null)
            panel.SetActive(isPanelVisible);
    }

    public void OnToggleSettings()
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
            isPanelVisible = panel.activeSelf;
        }
    }

    public void OnSendClicked()
    {
        OnSendClicked(inputField != null ? inputField.text : "");
    }

    public void OnSendClicked(string inputText)
    {
        if (chatManager == null)
        {
            Debug.LogWarning("[AIChatUI] chatManager is null, cannot send");
            return;
        }

        string message = (string.IsNullOrEmpty(inputText) && inputField != null)
            ? inputField.text
            : inputText;
        message = message.Trim();

        Debug.Log($"[AIChatUI] OnSendClicked: \"{message}\"");

        if (string.IsNullOrEmpty(message)) return;

        UpdateDisplay($"\n<color=cyan>[你]</color> {message}");

        if (inputField != null)
            inputField.text = "";

        chatManager.SendMessage(message);
    }

    private void OnAIResponse(string response)
    {
        Debug.Log($"[AIChatUI] OnAIResponse: \"{response}\"");
        UpdateDisplay($"\n<color=yellow>[AI]</color> {response}");
    }

    private void OnError(string error)
    {
        Debug.Log($"[AIChatUI] OnError: {error}");
        UpdateDisplay($"\n<color=red>[错误]</color> {error}");
    }

    public void UpdateDisplay(string text)
    {
        Debug.Log($"[AIChatUI] UpdateDisplay called. chatDisplay = {(chatDisplay != null ? "OK" : "NULL")}");

        if (chatDisplay == null)
        {
            Debug.LogError("[AIChatUI] chatDisplay is NULL! Text will not show. "
                + "Check that AIChatSetup correctly assigns AIChatUI.chatDisplay in the Inspector.");
            return;
        }

        chatDisplay.text += text;
        Debug.Log($"[AIChatUI] chatDisplay.text updated. New length = {chatDisplay.text.Length}");

        if (scrollRect != null && scrollRect.content != null && chatDisplay != null)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    private System.Collections.IEnumerator ScrollToBottom()
    {
        // 等待布局系统计算完成
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (chatDisplay != null && scrollRect != null && scrollRect.content != null)
        {
            // 强制更新 Canvas
            Canvas.ForceUpdateCanvases();

            // 获取文本实际高度
            float textHeight = chatDisplay.preferredHeight;
            float contentHeight = Mathf.Max(textHeight, scrollRect.viewport.rect.height);

            // 设置 Content 高度
            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, contentHeight);

            // 再次强制更新
            Canvas.ForceUpdateCanvases();

            // 滚动到底部 (normalized position 0 = 底部对于从上到下扩展的内容)
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ClearChat()
    {
        OnClearClicked();
    }

    private void OnClearClicked()
    {
        if (chatDisplay != null)
        {
            chatDisplay.text = "=== AI 对话 ===\n";
        }
        if (inputField != null)
        {
            inputField.text = "";
        }
        chatManager?.ClearHistory();
        Debug.Log("[AIChatUI] Chat cleared");
    }

    public void OnAPIKeyChanged(string value)
    {
        if (chatManager != null)
        {
            chatManager.apiKey = value;
            PlayerPrefs.SetString("DeepSeekAPIKey", value);
            Debug.Log($"[AIChatUI] API Key saved (length={value.Length})");
        }
    }

    public void OnSystemPromptChanged(string value)
    {
        if (chatManager != null)
        {
            chatManager.systemPrompt = value;
            PlayerPrefs.SetString("SystemPrompt", value);
        }
    }

    public void OnModelChanged(int index)
    {
        if (chatManager != null && modelDropdown != null)
        {
            chatManager.model = modelDropdown.options[index].text;
            Debug.Log($"[AIChatUI] Model changed to: {chatManager.model}");
        }
    }
}
