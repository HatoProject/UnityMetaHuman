using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class AIChatSetup : MonoBehaviour
{
    [Header("聊天面板设置")]
    public float chatWidth = 400f;
    public float chatHeight = 500f;
    public float panelPadding = 20f;

    [Header("手动指定 Manager (可选)")]
    public AIChatManager existingManager;

    [Header("颜色配置")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    public Color inputBackgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
    public Color buttonColor = new Color(0.2f, 0.4f, 0.8f, 1f);
    public Color titleColor = Color.white;

    [Header("字体配置")]
    public Font customFont = null;
    public TMP_FontAsset customTMPFont = null;

    [Header("UI 引用 (生成后自动赋值)")]
    public Canvas canvas;
    public RectTransform chatPanel;
    public ScrollRect chatScrollRect;
    public TextMeshProUGUI chatDisplay;
    public InputField inputField;
    public Button sendButton;
    public Button clearButton;
    public Button settingsButton;
    public GameObject settingsPanel;
    public InputField apiKeyInput;
    public InputField systemPromptInput;
    public Dropdown modelDropdown;

    [ContextMenu("生成 AI Chat UI")]
    public void GenerateUI()
    {
        SetupAIChat();
    }

    [ContextMenu("删除 AI Chat UI")]
    public void DeleteUI()
    {
        // 删除 Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null && canvas.gameObject.name == "AIChatCanvas")
        {
            if (Application.isPlaying)
                Destroy(canvas.gameObject);
            else
                DestroyImmediate(canvas.gameObject);
            Debug.Log("已删除 AI Chat Canvas");
        }
    }

    public void SetupAIChat()
    {
        // 查找现有 AIChatManager
        AIChatManager chatManager = existingManager;
        if (chatManager == null)
        {
            chatManager = FindObjectOfType<AIChatManager>();
        }

        if (chatManager == null)
        {
            Debug.LogWarning("未找到 AIChatManager，请先创建 Manager");
            return;
        }

        GameObject managerObj = chatManager.gameObject;

        // 创建 Canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null || canvas.gameObject.name != "AIChatCanvas")
        {
            GameObject canvasObj = new GameObject("AIChatCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 创建 EventSystem (支持 UI 输入)
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // 检查是否已有 AIChatUI
        AIChatUI chatUI = managerObj.GetComponent<AIChatUI>();
        if (chatUI == null)
        {
            chatUI = managerObj.AddComponent<AIChatUI>();
        }

        // 删除旧的 UI
        Transform oldPanel = canvas.transform.Find("ChatPanel");
        if (oldPanel != null)
        {
            if (Application.isPlaying)
                Destroy(oldPanel.gameObject);
            else
                DestroyImmediate(oldPanel.gameObject);
        }
        Transform oldSettings = canvas.transform.Find("SettingsPanel");
        if (oldSettings != null)
        {
            if (Application.isPlaying)
                Destroy(oldSettings.gameObject);
            else
                DestroyImmediate(oldSettings.gameObject);
        }

        // 创建聊天面板
        GameObject panelObj = CreatePanel(canvas.transform);
        chatPanel = panelObj.GetComponent<RectTransform>();

        // 创建标题
        CreateTitle(chatPanel);

        // 创建聊天显示区域
        GameObject scrollObj = CreateChatScrollView(chatPanel);
        chatScrollRect = scrollObj.GetComponent<ScrollRect>();
        chatDisplay = scrollObj.transform.Find("Viewport/Content/ChatText").GetComponent<TextMeshProUGUI>();

        // 创建输入区域
        GameObject inputAreaObj = CreateInputArea(chatPanel);
        inputField = inputAreaObj.transform.Find("InputField").GetComponent<InputField>();
        sendButton = inputAreaObj.transform.Find("SendButton").GetComponent<Button>();
        clearButton = inputAreaObj.transform.Find("ClearButton").GetComponent<Button>();

        // 创建设置面板
        GameObject settingsPanelObj = CreateSettingsPanel(canvas.transform, chatManager);
        settingsPanel = settingsPanelObj;
        apiKeyInput = settingsPanelObj.transform.Find("APIKeyInput").GetComponent<InputField>();
        systemPromptInput = settingsPanelObj.transform.Find("SystemPromptInput").GetComponent<InputField>();
        modelDropdown = settingsPanelObj.transform.Find("ModelDropdown").GetComponent<Dropdown>();

        // 创建设置按钮
        settingsButton = CreateSettingsButton(chatPanel, settingsPanelObj);
        Debug.Log($"[Setup] SettingsButton created: {settingsButton != null}, name: {settingsButton?.gameObject.name}");

        // 手动初始化 AIChatUI 的引用
        chatUI.inputField = inputField;
        chatUI.sendButton = sendButton;
        chatUI.clearButton = clearButton;
        chatUI.settingsButton = settingsButton;
        chatUI.chatDisplay = chatDisplay;
        chatUI.scrollRect = chatScrollRect;
        chatUI.panel = settingsPanelObj;
        chatUI.apiKeyInput = apiKeyInput;
        chatUI.systemPromptInput = systemPromptInput;
        chatUI.modelDropdown = modelDropdown;

        Debug.Log($"[AIChatSetup] chatDisplay assigned: {chatDisplay != null}, text: {chatDisplay?.text}");

        // 初始化显示文本
        chatDisplay.text = "=== AI 对话 ===\n";

        // 注册事件
        chatUI.sendButton.onClick.RemoveAllListeners();
        chatUI.sendButton.onClick.AddListener(() => {
            Debug.Log("[Event] SendButton clicked");
            chatUI.OnSendClicked();
        });
        chatUI.clearButton.onClick.RemoveAllListeners();
        chatUI.clearButton.onClick.AddListener(() => {
            Debug.Log("[Event] ClearButton clicked");
            chatUI.ClearChat();
        });

        // 输入框按回车发送 (onEndEdit 在回车时会触发)
        chatUI.inputField.onEndEdit.RemoveAllListeners();
        chatUI.inputField.onEndEdit.AddListener((text) =>
        {
            if (!string.IsNullOrEmpty(text))
                chatUI.OnSendClicked(text);
        });

        // 设置按钮事件 - 切换设置面板显示
        settingsButton.onClick.RemoveAllListeners();
        settingsButton.onClick.AddListener(() =>
        {
            Debug.Log($"[Event] SettingsButton clicked! Current state: {settingsPanelObj.activeSelf}");
            settingsPanelObj.SetActive(!settingsPanelObj.activeSelf);
        });
        Debug.Log($"[Setup] All button listeners registered. send:{chatUI.sendButton.onClick.GetPersistentEventCount()}, clear:{chatUI.clearButton.onClick.GetPersistentEventCount()}, settings:{settingsButton.onClick.GetPersistentEventCount()}");

        // 设置面板事件监听
        chatUI.apiKeyInput.onValueChanged.RemoveAllListeners();
        chatUI.apiKeyInput.onValueChanged.AddListener((value) => chatUI.OnAPIKeyChanged(value));
        chatUI.systemPromptInput.onValueChanged.RemoveAllListeners();
        chatUI.systemPromptInput.onValueChanged.AddListener((value) => chatUI.OnSystemPromptChanged(value));
        chatUI.modelDropdown.onValueChanged.RemoveAllListeners();
        chatUI.modelDropdown.onValueChanged.AddListener((index) => chatUI.OnModelChanged(index));

        // 关联 chatManager
        chatUI.chatManager = chatManager;

        // 从 PlayerPrefs 加载设置
        string savedKey = PlayerPrefs.GetString("DeepSeekAPIKey", "");
        string savedPrompt = PlayerPrefs.GetString("SystemPrompt", chatManager.systemPrompt);
        string savedModel = PlayerPrefs.GetString("Model", "deepseek-chat");

        chatUI.apiKeyInput.text = savedKey;
        chatUI.systemPromptInput.text = savedPrompt;
        chatManager.apiKey = savedKey;
        chatManager.systemPrompt = savedPrompt;

        // 初始化模型下拉菜单
        chatUI.modelDropdown.ClearOptions();
        if (string.IsNullOrEmpty(savedModel) || savedModel == "deepseek-v3-250324" || savedModel == "ep-20260402173502-jqhgv")
            savedModel = "deepseek-chat";
        chatUI.modelDropdown.options.Add(new Dropdown.OptionData(savedModel));
        chatUI.modelDropdown.value = 0;
        chatManager.model = savedModel;

        // 隐藏设置面板
        settingsPanelObj.SetActive(false);

        Debug.Log("AI Chat UI 生成完成！");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("ChatPanel");
        panel.transform.SetParent(parent);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(chatWidth, -40);
        rect.anchoredPosition = new Vector2(chatWidth / 2 + 20, 0);

        Image bg = panel.AddComponent<Image>();
        bg.color = backgroundColor;

        // 不使用 CanvasGroup，让子元素可以接收点击
        // panel.AddComponent<CanvasGroup>();
        return panel;
    }

    private void CreateTitle(RectTransform parent)
    {
        GameObject title = new GameObject("Title");
        title.transform.SetParent(parent);

        RectTransform rect = title.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(0, 40);
        rect.anchoredPosition = new Vector2(0, 0);

        Text text = title.AddComponent<Text>();
        text.text = "AI Chat";
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.color = titleColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont();
    }

    private GameObject CreateChatScrollView(RectTransform parent)
    {
        GameObject scrollObj = new GameObject("ChatScrollView");
        scrollObj.transform.SetParent(parent);

        RectTransform rect = scrollObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.15f);
        rect.anchorMax = new Vector2(1, 0.85f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        Image bg = scrollObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.5f);

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform);
        RectTransform vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero;
        vpRect.pivot = new Vector2(0, 1);
        viewport.AddComponent<Mask>();
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(1, 1, 1, 0);

        scroll.viewport = vpRect;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0, 1);
        contentRect.sizeDelta = new Vector2(0, 100);
        contentRect.anchoredPosition = new Vector2(0, 0);

        LayoutElement contentLayout = content.AddComponent<LayoutElement>();
        contentLayout.minHeight = 100;
        contentLayout.flexibleHeight = 9999;

        scroll.content = contentRect;

        // 创建垂直 Scrollbar
        GameObject scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(scrollObj.transform);
        RectTransform sbRect = scrollbarObj.AddComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(1, 0);
        sbRect.anchorMax = Vector2.one;
        sbRect.sizeDelta = new Vector2(20, 0);
        sbRect.anchoredPosition = Vector2.zero;
        sbRect.pivot = new Vector2(1, 1);

        Image sbImage = scrollbarObj.AddComponent<Image>();
        sbImage.color = new Color(0.3f, 0.3f, 0.4f, 0.8f);

        Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.TopToBottom;
        scrollbar.size = 0.2f;
        scrollbar.numberOfSteps = 0;

        // 创建 Handle
        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(scrollbarObj.transform);
        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.sizeDelta = new Vector2(-4, -4);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;

        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = new Color(0.4f, 0.6f, 1f, 1f);

        // 先创建Handle，再设置handleRect
        scrollbar.handleRect = handleRect;

        // 连接 Scrollbar 到 ScrollRect
        scroll.verticalScrollbar = scrollbar;

        GameObject chatText = new GameObject("ChatText");
        chatText.transform.SetParent(content.transform);
        RectTransform textRect = chatText.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0, 1);
        textRect.anchoredPosition = new Vector2(0, 0);

        LayoutElement textLayout = chatText.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1;
        textLayout.flexibleHeight = 0;

        TMP_Text text = chatText.AddComponent<TMP_Text>();
        text.text = "=== AI 对话 ===\n";
        text.fontSize = 16;
        text.color = Color.white;
        text.font = GetTMPFont();
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10;
        text.fontSizeMax = 24;

        return scrollObj;
    }

    private GameObject CreateInputArea(RectTransform parent)
    {
        GameObject inputArea = new GameObject("InputArea");
        inputArea.transform.SetParent(parent);

        RectTransform rect = inputArea.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0.15f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        GameObject inputField = CreateInputFieldObj(inputArea.transform);
        inputField.name = "InputField";

        RectTransform inputRect = inputField.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 0.1f);
        inputRect.anchorMax = new Vector2(0.8f, 0.9f);
        inputRect.sizeDelta = Vector2.zero;
        inputRect.anchoredPosition = Vector2.zero;

        GameObject sendBtn = CreateButton(inputArea.transform, "SendButton", "发送", new Vector2(0.82f, 0.5f), new Vector2(80, 35));
        sendBtn.name = "SendButton";

        GameObject clearBtn = CreateButton(inputArea.transform, "ClearButton", "清空", new Vector2(0.92f, 0.5f), new Vector2(60, 35));
        clearBtn.name = "ClearButton";

        return inputArea;
    }

    private GameObject CreateInputFieldObj(Transform parent)
    {
        GameObject inputObj = new GameObject("InputField");
        inputObj.transform.SetParent(parent);

        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.pivot = new Vector2(0, 0.5f);

        InputField input = inputObj.AddComponent<InputField>();
        input.characterLimit = 500;
        input.lineType = InputField.LineType.SingleLine;
        input.contentType = InputField.ContentType.Standard;

        Image bg = inputObj.AddComponent<Image>();
        bg.color = inputBackgroundColor;
        input.targetGraphic = bg;

        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(inputObj.transform);
        RectTransform phRect = placeholder.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.sizeDelta = new Vector2(-10, 0);
        phRect.anchoredPosition = Vector2.zero;
        phRect.pivot = new Vector2(0, 1);

        Text phText = placeholder.AddComponent<Text>();
        phText.text = "输入消息...";
        phText.fontSize = 14;
        phText.color = new Color(0.6f, 0.6f, 0.6f);
        phText.font = GetFont();
        input.placeholder = phText;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-10, 0);
        textRect.anchoredPosition = Vector2.zero;
        textRect.pivot = new Vector2(0, 1);

        Text text = textObj.AddComponent<Text>();
        text.fontSize = 14;
        text.color = Color.white;
        text.font = GetFont();
        input.textComponent = text;

        return inputObj;
    }

    private GameObject CreateButton(Transform parent, string name, string label, Vector2 anchorPos, Vector2 size)
    {
        GameObject btn = new GameObject(name);
        btn.transform.SetParent(parent);

        RectTransform rect = btn.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.sizeDelta = size;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image bg = btn.AddComponent<Image>();
        bg.color = buttonColor;

        Button button = btn.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock()
        {
            normalColor = buttonColor,
            highlightedColor = new Color(0.3f, 0.5f, 1f),
            pressedColor = new Color(0.1f, 0.3f, 0.8f),
            disabledColor = new Color(0.3f, 0.3f, 0.3f),
            colorMultiplier = 1
        };

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btn.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont();

        return btn;
    }

    private GameObject CreateSettingsPanel(Transform parent, AIChatManager chatManager)
    {
        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(parent);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400, 350);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.98f);

        CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        panel.SetActive(false);

        CreateSettingsTitle(panel.transform);
        CreateAPIKeyField(panel.transform, chatManager);
        CreateModelDropdown(panel.transform);
        CreateSystemPromptField(panel.transform);
        CreateCloseButton(panel.transform, panel);

        return panel;
    }

    private void CreateSettingsTitle(Transform parent)
    {
        GameObject title = new GameObject("Title");
        title.transform.SetParent(parent);

        RectTransform rect = title.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(0, 50);
        rect.anchoredPosition = new Vector2(0, -25);
        rect.pivot = new Vector2(0.5f, 1);

        Text text = title.AddComponent<Text>();
        text.text = "API 设置";
        text.fontSize = 22;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont();
    }

    private void CreateAPIKeyField(Transform parent, AIChatManager chatManager)
    {
        GameObject label = new GameObject("APIKeyLabel");
        label.transform.SetParent(parent);
        RectTransform labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.75f);
        labelRect.anchorMax = new Vector2(0, 0.75f);
        labelRect.sizeDelta = new Vector2(100, 30);
        labelRect.pivot = new Vector2(0, 0.5f);
        labelRect.anchoredPosition = new Vector2(20, 0);

        Text labelText = label.AddComponent<Text>();
        labelText.text = "API Key:";
        labelText.fontSize = 14;
        labelText.color = Color.white;
        labelText.font = GetFont();

        GameObject inputObj = CreateInputFieldForSettings(parent);
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 0.65f);
        inputRect.anchorMax = new Vector2(1, 0.65f);
        inputRect.sizeDelta = new Vector2(-40, 30);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.anchoredPosition = new Vector2(0, 0);
        inputObj.name = "APIKeyInput";

        InputField input = inputObj.GetComponent<InputField>();
        input.contentType = InputField.ContentType.Password;
        input.characterLimit = 100;
        input.text = PlayerPrefs.GetString("DeepSeekAPIKey", "");
        chatManager.apiKey = input.text;

        GameObject placeholder = input.placeholder.gameObject;
        placeholder.GetComponent<Text>().text = "请输入 DeepSeek API Key";
    }

    private void CreateModelDropdown(Transform parent)
    {
        GameObject label = new GameObject("ModelLabel");
        label.transform.SetParent(parent);
        RectTransform labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(0, 0.5f);
        labelRect.sizeDelta = new Vector2(60, 30);
        labelRect.pivot = new Vector2(0, 0.5f);
        labelRect.anchoredPosition = new Vector2(20, 0);

        Text labelText = label.AddComponent<Text>();
        labelText.text = "模型:";
        labelText.fontSize = 14;
        labelText.color = Color.white;
        labelText.font = GetFont();

        GameObject dropdownObj = new GameObject("ModelDropdown");
        dropdownObj.transform.SetParent(parent);
        RectTransform dropRect = dropdownObj.AddComponent<RectTransform>();
        dropRect.anchorMin = new Vector2(0, 0.5f);
        dropRect.anchorMax = new Vector2(1, 0.5f);
        dropRect.sizeDelta = new Vector2(-100, 30);
        dropRect.pivot = new Vector2(0.5f, 0.5f);
        dropRect.anchoredPosition = new Vector2(20, 0);

        Image bg = dropdownObj.AddComponent<Image>();
        bg.color = inputBackgroundColor;

        Dropdown dropdown = dropdownObj.AddComponent<Dropdown>();
        dropdown.targetGraphic = bg;

        GameObject caption = new GameObject("Caption");
        caption.transform.SetParent(dropdownObj.transform);
        RectTransform capRect = caption.AddComponent<RectTransform>();
        capRect.anchorMin = Vector2.zero;
        capRect.anchorMax = Vector2.one;
        capRect.sizeDelta = new Vector2(-30, 0);
        capRect.anchoredPosition = new Vector2(10, 0);
        capRect.pivot = new Vector2(0, 0.5f);

        Text capText = caption.AddComponent<Text>();
        capText.text = "deepseek-chat";
        capText.fontSize = 14;
        capText.color = Color.white;
        capText.font = GetFont();
        dropdown.captionText = capText;

        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(dropdownObj.transform);
        RectTransform arrowRect = arrow.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 15);
        arrowRect.anchoredPosition = new Vector2(-15, 0);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        Image arrowImg = arrow.AddComponent<Image>();
        arrowImg.color = Color.white;

        // Create dropdown template
        GameObject template = new GameObject("Template");
        template.transform.SetParent(dropdownObj.transform);
        RectTransform templateRect = template.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.sizeDelta = new Vector2(0, 150);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, 0);

        Image templateBg = template.AddComponent<Image>();
        templateBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.pivot = new Vector2(0, 1);
        viewport.AddComponent<Mask>();
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = Color.white;

        scrollRect.viewport = viewportRect.GetComponent<RectTransform>();

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.sizeDelta = new Vector2(0, 28);
        contentRect.pivot = new Vector2(0.5f, 1);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;

        // Create template item (Toggle)
        GameObject item = new GameObject("Item");
        item.transform.SetParent(content.transform);
        RectTransform itemRect = item.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 1);
        itemRect.anchorMax = new Vector2(1, 1);
        itemRect.sizeDelta = new Vector2(0, 28);
        itemRect.pivot = new Vector2(0.5f, 1);

        Toggle toggle = item.AddComponent<Toggle>();
        toggle.targetGraphic = item.AddComponent<Image>();
        toggle.targetGraphic.color = new Color(0.2f, 0.4f, 0.8f, 1f);

        GameObject itemBackground = new GameObject("Background");
        itemBackground.transform.SetParent(item.transform);
        RectTransform bgRect = itemBackground.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.pivot = new Vector2(0.5f, 0.5f);

        Image itemBgImg = itemBackground.AddComponent<Image>();
        toggle.graphic = itemBgImg;

        GameObject itemCheckmark = new GameObject("Item Checkmark");
        itemCheckmark.transform.SetParent(itemBackground.transform);
        RectTransform checkRect = itemCheckmark.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0, 0.5f);
        checkRect.anchorMax = new Vector2(0, 0.5f);
        checkRect.sizeDelta = new Vector2(20, 20);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.anchoredPosition = new Vector2(15, 0);
        Image checkImg = itemCheckmark.AddComponent<Image>();
        checkImg.color = new Color(0.2f, 0.4f, 0.8f, 1f);
        toggle.graphic = checkImg;

        GameObject itemLabel = new GameObject("Item Label");
        itemLabel.transform.SetParent(itemBackground.transform);
        RectTransform labelRect2 = itemLabel.AddComponent<RectTransform>();
        labelRect2.anchorMin = new Vector2(0, 0);
        labelRect2.anchorMax = Vector2.one;
        labelRect2.sizeDelta = new Vector2(-40, 0);
        labelRect2.pivot = new Vector2(0.5f, 0.5f);
        Text itemText = itemLabel.AddComponent<Text>();
        itemText.text = "Option";
        itemText.fontSize = 14;
        itemText.color = Color.white;
        itemText.font = GetFont();

        // Assign template to dropdown
        dropdown.template = templateRect;
        template.SetActive(false);

        dropdown.options.Add(new Dropdown.OptionData("deepseek-chat"));
        dropdown.value = 0;
    }

    private void CreateSystemPromptField(Transform parent)
    {
        GameObject label = new GameObject("SystemPromptLabel");
        label.transform.SetParent(parent);
        RectTransform labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.35f);
        labelRect.anchorMax = new Vector2(0, 0.35f);
        labelRect.sizeDelta = new Vector2(100, 30);
        labelRect.pivot = new Vector2(0, 0.5f);
        labelRect.anchoredPosition = new Vector2(20, 0);

        Text labelText = label.AddComponent<Text>();
        labelText.text = "系统提示:";
        labelText.fontSize = 14;
        labelText.color = Color.white;
        labelText.font = GetFont();

        GameObject inputObj = CreateInputFieldForSettings(parent);
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 0.08f);
        inputRect.anchorMax = new Vector2(1, 0.32f);
        inputRect.sizeDelta = new Vector2(-40, 0);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.anchoredPosition = new Vector2(0, 0);
        inputObj.name = "SystemPromptInput";

        InputField input = inputObj.GetComponent<InputField>();
        input.contentType = InputField.ContentType.Standard;
        input.text = PlayerPrefs.GetString("SystemPrompt", "你是一个友好的AI助手。");

        GameObject placeholder = input.placeholder.gameObject;
        placeholder.GetComponent<Text>().text = "设置 AI 的角色和行为...";
    }

    private void CreateCloseButton(Transform parent, GameObject panel)
    {
        GameObject closeBtn = new GameObject("CloseButton");
        closeBtn.transform.SetParent(parent);

        RectTransform rect = closeBtn.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(40, 40);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-10, -10);

        Image bg = closeBtn.AddComponent<Image>();
        bg.color = new Color(0.8f, 0.2f, 0.2f);

        Button button = closeBtn.AddComponent<Button>();
        button.onClick.AddListener(() => panel.SetActive(false));

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(closeBtn.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);

        Text text = textObj.AddComponent<Text>();
        text.text = "X";
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont();
    }

    private GameObject CreateInputFieldForSettings(Transform parent)
    {
        GameObject inputObj = new GameObject("InputField");
        inputObj.transform.SetParent(parent);

        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.pivot = new Vector2(0, 0.5f);

        InputField input = inputObj.AddComponent<InputField>();
        input.characterLimit = 500;

        Image bg = inputObj.AddComponent<Image>();
        bg.color = inputBackgroundColor;
        input.targetGraphic = bg;

        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(inputObj.transform);
        RectTransform phRect = placeholder.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.sizeDelta = new Vector2(-10, 0);
        phRect.anchoredPosition = Vector2.zero;
        phRect.pivot = new Vector2(0, 0.5f);

        Text phText = placeholder.AddComponent<Text>();
        phText.text = "";
        phText.fontSize = 14;
        phText.color = new Color(0.6f, 0.6f, 0.6f);
        phText.font = GetFont();
        input.placeholder = phText;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-10, 0);
        textRect.anchoredPosition = Vector2.zero;
        textRect.pivot = new Vector2(0, 0.5f);

        Text text = textObj.AddComponent<Text>();
        text.fontSize = 14;
        text.color = Color.white;
        text.font = GetFont();
        input.textComponent = text;

        return inputObj;
    }

    private Button CreateSettingsButton(RectTransform parent, GameObject settingsPanel)
    {
        GameObject btn = new GameObject("SettingsButton");
        btn.transform.SetParent(parent);

        RectTransform rect = btn.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(40, 40);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-10, -10);

        Image bg = btn.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.3f, 0.35f);

        Button button = btn.AddComponent<Button>();
        button.onClick.AddListener(() => settingsPanel.SetActive(!settingsPanel.activeSelf));

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btn.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);

        Text text = textObj.AddComponent<Text>();
        text.text = "⚙";
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont();

        return button;
    }

    private Font GetFont()
    {
        return customFont != null ? customFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private TMP_FontAsset GetTMPFont()
    {
        return customTMPFont != null ? customTMPFont : Resources.Load<TMP_FontAsset>("Fonts & Materials/ LiberationSans SDF");
    }
}
