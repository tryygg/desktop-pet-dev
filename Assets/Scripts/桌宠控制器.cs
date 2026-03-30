using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class 桌宠控制器 : MonoBehaviour
{
    [Header("引用")]
    [SerializeField, FormerlySerializedAs("characterAnimator"), InspectorName("角色动画器")]
    private Animator 角色动画器;
    [SerializeField, FormerlySerializedAs("visualRoot"), InspectorName("视觉根节点")]
    private Transform 视觉根节点;
    [SerializeField, FormerlySerializedAs("windowController"), InspectorName("窗口控制器")]
    private 透明窗口 窗口控制器;
    [SerializeField, FormerlySerializedAs("petCamera"), InspectorName("桌宠相机")]
    private Camera 桌宠相机;
    [SerializeField, FormerlySerializedAs("sceneUiCanvas"), InspectorName("场景UI画布")]
    private Canvas 场景UI画布;
    [SerializeField, FormerlySerializedAs("llmClient"), InspectorName("大模型客户端")]
    private 大模型客户端 大模型客户端引用;
    [SerializeField, InspectorName("GPT-SoVITS客户端")]
    private GPTSoVITS客户端 GPTSoVITS客户端引用;

    public Animator characterAnimator { get => 角色动画器; set => 角色动画器 = value; }
    public Transform visualRoot { get => 视觉根节点; set => 视觉根节点 = value; }
    public 透明窗口 windowController { get => 窗口控制器; set => 窗口控制器 = value; }
    public Camera petCamera { get => 桌宠相机; set => 桌宠相机 = value; }
    public Canvas sceneUiCanvas { get => 场景UI画布; set => 场景UI画布 = value; }
    public 大模型客户端 llmClient { get => 大模型客户端引用; set => 大模型客户端引用 = value; }
    public GPTSoVITS客户端 gptSoVITSClient { get => GPTSoVITS客户端引用; set => GPTSoVITS客户端引用 = value; }

    [Header("动画")]
    [SerializeField, FormerlySerializedAs("initialStateName"), InspectorName("初始状态名")]
    private string 初始状态名 = "\u6c89\u601d";
    [SerializeField, FormerlySerializedAs("menuStateNames"), InspectorName("菜单状态列表")]
    private string[] 菜单状态列表 =
    {
        "\u6c89\u601d",
        "\u8bf4\u8bdd1",
        "\u8bf4\u8bdd2",
        "\u8bf4\u8bdd3",
        "\u8f9b\u82e6\u5566",
        "\u6253\u62db\u547c",
        "\u6bd4\u5fc3"
    };
    [SerializeField, FormerlySerializedAs("transitionDuration"), InspectorName("过渡时长")]
    private float 过渡时长 = 0.15f;

    public string initialStateName { get => 初始状态名; set => 初始状态名 = value; }
    public string[] menuStateNames { get => 菜单状态列表; set => 菜单状态列表 = value; }
    public float transitionDuration { get => 过渡时长; set => 过渡时长 = value; }

    [Header("缩放")]
    [SerializeField, FormerlySerializedAs("initialScale"), InspectorName("初始缩放")]
    private float 初始缩放 = 0.82f;
    [SerializeField, FormerlySerializedAs("minScale"), InspectorName("最小缩放")]
    private float 最小缩放 = 0.35f;
    [SerializeField, FormerlySerializedAs("maxScale"), InspectorName("最大缩放")]
    private float 最大缩放 = 3f;
    [SerializeField, FormerlySerializedAs("scaleStep"), InspectorName("缩放步进")]
    private float 缩放步进 = 0.12f;

    public float initialScale { get => 初始缩放; set => 初始缩放 = value; }
    public float minScale { get => 最小缩放; set => 最小缩放 = value; }
    public float maxScale { get => 最大缩放; set => 最大缩放 = value; }
    public float scaleStep { get => 缩放步进; set => 缩放步进 = value; }

    [Header("窗口行为")]
    [SerializeField, FormerlySerializedAs("startCenteredOnTaskbar"), InspectorName("启动时任务栏居中")]
    private bool 启动时任务栏居中 = true;
    [SerializeField, FormerlySerializedAs("autoFitWindowToPet"), InspectorName("自动贴合桌宠")]
    private bool 自动贴合桌宠 = true;
    [SerializeField, FormerlySerializedAs("minWindowSize"), InspectorName("最小窗口尺寸")]
    private Vector2Int 最小窗口尺寸 = new Vector2Int(240, 320);
    [SerializeField, FormerlySerializedAs("contentPadding"), InspectorName("内容边距")]
    private Vector2Int 内容边距 = new Vector2Int(72, 96);
    [SerializeField, FormerlySerializedAs("maxAutoFitPadding"), InspectorName("最大自动边距")]
    private Vector2Int 最大自动边距 = new Vector2Int(28, 40);
    [SerializeField, FormerlySerializedAs("edgeSnapDistance"), InspectorName("边缘吸附距离")]
    private int 边缘吸附距离 = 32;
    [SerializeField, FormerlySerializedAs("taskbarSnapDistance"), InspectorName("任务栏吸附距离")]
    private int 任务栏吸附距离 = 56;
    [SerializeField, FormerlySerializedAs("bounceDistance"), InspectorName("回弹距离")]
    private int 回弹距离 = 18;
    [SerializeField, FormerlySerializedAs("snapDuration"), InspectorName("吸附时长")]
    private float 吸附时长 = 0.08f;
    [SerializeField, FormerlySerializedAs("bounceDuration"), InspectorName("回弹时长")]
    private float 回弹时长 = 0.08f;

    public bool startCenteredOnTaskbar { get => 启动时任务栏居中; set => 启动时任务栏居中 = value; }
    public bool autoFitWindowToPet { get => 自动贴合桌宠; set => 自动贴合桌宠 = value; }
    public Vector2Int minWindowSize { get => 最小窗口尺寸; set => 最小窗口尺寸 = value; }
    public Vector2Int contentPadding { get => 内容边距; set => 内容边距 = value; }
    public Vector2Int maxAutoFitPadding { get => 最大自动边距; set => 最大自动边距 = value; }
    public int edgeSnapDistance { get => 边缘吸附距离; set => 边缘吸附距离 = value; }
    public int taskbarSnapDistance { get => 任务栏吸附距离; set => 任务栏吸附距离 = value; }
    public int bounceDistance { get => 回弹距离; set => 回弹距离 = value; }
    public float snapDuration { get => 吸附时长; set => 吸附时长 = value; }
    public float bounceDuration { get => 回弹时长; set => 回弹时长 = value; }

    [Header("遮罩区域")]
    [SerializeField, FormerlySerializedAs("useRenderedMaskRegion"), InspectorName("使用渲染遮罩区域")]
    private bool 使用渲染遮罩区域 = false;
    [SerializeField, FormerlySerializedAs("maskRegionUpdateInterval"), InspectorName("遮罩更新间隔")]
    private float 遮罩更新间隔 = 0.08f;
    [SerializeField, FormerlySerializedAs("maskRowStep"), InspectorName("遮罩行步进")]
    private int 遮罩行步进 = 2;
    [SerializeField, FormerlySerializedAs("maskColorTolerance"), InspectorName("遮罩颜色容差")]
    private int 遮罩颜色容差 = 16;
    [SerializeField, FormerlySerializedAs("maskEdgePadding"), InspectorName("遮罩边缘留白")]
    private int 遮罩边缘留白 = 2;

    public bool useRenderedMaskRegion { get => 使用渲染遮罩区域; set => 使用渲染遮罩区域 = value; }
    public float maskRegionUpdateInterval { get => 遮罩更新间隔; set => 遮罩更新间隔 = value; }
    public int maskRowStep { get => 遮罩行步进; set => 遮罩行步进 = value; }
    public int maskColorTolerance { get => 遮罩颜色容差; set => 遮罩颜色容差 = value; }
    public int maskEdgePadding { get => 遮罩边缘留白; set => 遮罩边缘留白 = value; }

    [Header("性能")]
    [SerializeField, FormerlySerializedAs("idleFrameRate"), InspectorName("空闲帧率")]
    private int 空闲帧率 = 60;
    [SerializeField, FormerlySerializedAs("activeFrameRate"), InspectorName("活跃帧率")]
    private int 活跃帧率 = 60;

    public int idleFrameRate { get => 空闲帧率; set => 空闲帧率 = value; }
    public int activeFrameRate { get => 活跃帧率; set => 活跃帧率 = value; }

    [Header("对话")]
    [SerializeField, FormerlySerializedAs("chatDockSize"), InspectorName("对话框尺寸")]
    private Vector2 对话框尺寸 = new Vector2(304f, 92f);
    [SerializeField, FormerlySerializedAs("chatDockOffset"), InspectorName("对话框偏移")]
    private Vector2 对话框偏移 = new Vector2(44f, 24f);
    [SerializeField, FormerlySerializedAs("chatPanelSize"), InspectorName("聊天面板尺寸")]
    private Vector2 聊天面板尺寸 = new Vector2(292f, 236f);
    [SerializeField, FormerlySerializedAs("chatBubbleMaxSize"), InspectorName("气泡最大尺寸")]
    private Vector2 气泡最大尺寸 = new Vector2(340f, 158f);
    [SerializeField, InspectorName("气泡偏移")]
    private Vector2 气泡偏移 = new Vector2(0f, 0f);
    [SerializeField, FormerlySerializedAs("chatAnimationHoldDuration"), InspectorName("对话动画保持时长")]
    private float 对话动画保持时长 = 1.35f;
    [SerializeField, InspectorName("每字额外动画时长")]
    private float 每字额外动画时长 = 0.045f;
    [SerializeField, InspectorName("最短对话动画时长")]
    private float 最短对话动画时长 = 0.9f;
    [SerializeField, InspectorName("最长对话动画时长")]
    private float 最长对话动画时长 = 5.6f;
    [SerializeField, FormerlySerializedAs("chatBubbleDuration"), InspectorName("气泡显示时长")]
    private float 气泡显示时长 = 3.25f;
    [SerializeField, FormerlySerializedAs("chatBubbleBodySpriteOverride"), InspectorName("气泡美术图")]
    private Sprite 气泡美术图;
    [TextArea(2, 4)]
    [SerializeField, FormerlySerializedAs("defaultGreeting"), InspectorName("默认问候语")]
    private string 默认问候语 = "\u4f60\u597d\u5440\uff0c\u6211\u73b0\u5728\u8fd8\u6ca1\u63a5\u5165\u5927\u6a21\u578b\uff0c\u4f46\u5df2\u7ecf\u53ef\u4ee5\u966a\u4f60\u804a\u804a\u5929\u5566\u3002";
    [SerializeField, FormerlySerializedAs("localChatReplies"), InspectorName("本地回复库")]
    private string[] 本地回复库 =
    {
        "\u6211\u5728\u8ba4\u771f\u542c\u4f60\u8bf4\u5462\u3002",
        "\u8fd9\u53e5\u8bdd\u6211\u6536\u5230\u5566\uff0c\u6211\u5f88\u5f00\u5fc3\u3002",
        "\u542c\u8d77\u6765\u633a\u6709\u610f\u601d\u7684\uff0c\u6211\u60f3\u7ee7\u7eed\u966a\u4f60\u804a\u3002",
        "\u597d\u5440\uff0c\u6211\u4f1a\u8bb0\u4f4f\u4f60\u521a\u624d\u8bf4\u7684\u8bdd\u3002"
    };
    [SerializeField, FormerlySerializedAs("chatAnimationStateNames"), InspectorName("对话动画状态名")]
    private string[] 对话动画状态名 =
    {
        "\u8bf4\u8bdd1",
        "\u8bf4\u8bdd2",
        "\u8bf4\u8bdd3"
    };

    public Vector2 chatDockSize { get => 对话框尺寸; set => 对话框尺寸 = value; }
    public Vector2 chatDockOffset { get => 对话框偏移; set => 对话框偏移 = value; }
    public Vector2 chatPanelSize { get => 聊天面板尺寸; set => 聊天面板尺寸 = value; }
    public Vector2 chatBubbleMaxSize { get => 气泡最大尺寸; set => 气泡最大尺寸 = value; }
    public float chatAnimationHoldDuration { get => 对话动画保持时长; set => 对话动画保持时长 = value; }
    public float chatAnimationExtraDurationPerCharacter { get => 每字额外动画时长; set => 每字额外动画时长 = value; }
    public float chatAnimationMinDuration { get => 最短对话动画时长; set => 最短对话动画时长 = value; }
    public float chatAnimationMaxDuration { get => 最长对话动画时长; set => 最长对话动画时长 = value; }
    public float chatBubbleDuration { get => 气泡显示时长; set => 气泡显示时长 = value; }
    public Sprite chatBubbleBodySpriteOverride { get => 气泡美术图; set => 气泡美术图 = value; }
    public string defaultGreeting { get => 默认问候语; set => 默认问候语 = value; }
    public string[] localChatReplies { get => 本地回复库; set => 本地回复库 = value; }
    public string[] chatAnimationStateNames { get => 对话动画状态名; set => 对话动画状态名 = value; }

    [Header("菜单外观")]
    [SerializeField, FormerlySerializedAs("menuPadding"), InspectorName("菜单内边距")]
    private Vector2 菜单内边距 = new Vector2(12f, 12f);
    [SerializeField, FormerlySerializedAs("menuWidth"), InspectorName("菜单宽度")]
    private float 菜单宽度 = 176f;
    [SerializeField, FormerlySerializedAs("menuHeaderHeight"), InspectorName("菜单标题高度")]
    private float 菜单标题高度 = 24f;
    [SerializeField, FormerlySerializedAs("menuItemHeight"), InspectorName("菜单项高度")]
    private float 菜单项高度 = 30f;
    [SerializeField, FormerlySerializedAs("menuPanelColor"), InspectorName("菜单面板颜色")]
    private Color 菜单面板颜色 = new Color(0.08f, 0.1f, 0.14f, 1f);
    [SerializeField, FormerlySerializedAs("menuItemColor"), InspectorName("菜单项颜色")]
    private Color 菜单项颜色 = new Color(0.15f, 0.18f, 0.24f, 1f);
    [SerializeField, FormerlySerializedAs("menuHoverColor"), InspectorName("菜单悬停颜色")]
    private Color 菜单悬停颜色 = new Color(0.24f, 0.3f, 0.39f, 1f);
    [SerializeField, FormerlySerializedAs("menuActiveColor"), InspectorName("菜单激活颜色")]
    private Color 菜单激活颜色 = new Color(0.3f, 0.44f, 0.56f, 1f);
    [SerializeField, FormerlySerializedAs("menuBorderColor"), InspectorName("菜单边框颜色")]
    private Color 菜单边框颜色 = new Color(0.24f, 0.29f, 0.35f, 1f);

    public Vector2 menuPadding { get => 菜单内边距; set => 菜单内边距 = value; }
    public float menuWidth { get => 菜单宽度; set => 菜单宽度 = value; }
    public float menuHeaderHeight { get => 菜单标题高度; set => 菜单标题高度 = value; }
    public float menuItemHeight { get => 菜单项高度; set => 菜单项高度 = value; }
    public Color menuPanelColor { get => 菜单面板颜色; set => 菜单面板颜色 = value; }
    public Color menuItemColor { get => 菜单项颜色; set => 菜单项颜色 = value; }
    public Color menuHoverColor { get => 菜单悬停颜色; set => 菜单悬停颜色 = value; }
    public Color menuActiveColor { get => 菜单激活颜色; set => 菜单激活颜色 = value; }
    public Color menuBorderColor { get => 菜单边框颜色; set => 菜单边框颜色 = value; }

    private enum InteractionMode
    {
        Passive,
        Dragging
    }

    private readonly List<string> availableMenuStates = new List<string>();
    private Renderer[] characterRenderers = new Renderer[0];
    private CapsuleCollider generatedCollider;
    private Coroutine snapRoutine;
    private InteractionMode interactionMode = InteractionMode.Passive;
    private Vector3 originalVisualScale = Vector3.one;
    private float currentScale = 1f;

    private bool menuOpen;
    private bool chatPanelOpen;
    private bool chatHistoryOpen;
    private bool chatBubbleVisible;
    private bool cursorInsideWindow;
    private bool cursorOverPet;
    private bool cursorOverMenu;
    private bool cursorOverChat;
    private bool cursorOverHistory;
    private bool previousLeftDown;
    private bool previousRightDown;
    private bool chatRequestInFlight;
    private int hoveredMenuIndex = -1;
    private string currentStateName = string.Empty;

    private Rect menuRect;
    private Rect chatPanelRect;
    private Rect historyPanelRect;
    private Rect chatBubbleRect;
    private string currentChatBubbleMessage = string.Empty;
    private Vector2 clientCursorPosition;
    private Vector2 guiCursorPosition;
    private Vector2Int dragOffset;
    private Vector2 chatPanelDragOffset;
    private Vector2 historyPanelDragOffset;
    private bool draggingChatPanel;
    private bool draggingHistoryPanel;
    private bool chatPanelManuallyPositioned;
    private bool historyPanelManuallyPositioned;
    private bool scrollHistoryToBottomPending;
    private bool hasCachedPetClientRect;

    private Canvas runtimeMenuCanvas;
    private RectTransform runtimeMenuRoot;
    private RectTransform runtimeMenuItemsRoot;
    private RectTransform runtimeChatPanelRoot;
    private RectTransform runtimeChatHistoryPanelRoot;
    private RectTransform runtimeChatBubbleRoot;
    private RectTransform runtimeChatBubbleBodyRoot;
    private RectTransform runtimeChatBubbleTailRoot;
    private RectTransform runtimeLoadingRoot;
    private Text runtimeMenuHeader;
    private Text runtimeChatHeader;
    private Text runtimeChatHistoryHeader;
    private Text runtimeChatHistoryText;
    private Text runtimeChatBubbleText;
    private Text runtimeLoadingText;
    private InputField runtimeChatInputField;
    private bool pendingChatSubmitFromInputField;
    private ScrollRect runtimeChatHistoryScrollRect;
    private readonly List<Image> runtimeMenuItemBackgrounds = new List<Image>();
    private readonly List<string> chatHistory = new List<string>();
    private RectInt cachedPetClientRect;
    private string cachedChatHistoryText = string.Empty;
    private bool chatHistoryDirty = true;
    private Image runtimeChatMenuBackground;
    private Image runtimeExitBackground;
    private Image runtimeChatPanelBackground;
    private Image runtimeChatHistoryPanelBackground;
    private Image runtimeChatBubbleBackground;
    private Image runtimeChatBubbleTailBackground;
    private Image runtimeLoadingBackground;
    private Outline runtimeChatBubbleOutline;
    private Sprite runtimeChatBubbleBodySprite;
    private Sprite runtimeChatBubbleTailSprite;
    private Texture2D runtimeChatBubbleProcessedTexture;
    private Button runtimeChatSendButton;
    private Button runtimeChatCloseButton;
    private Button runtimeChatHistoryButton;
    private Button runtimeChatHistoryCloseButton;
    private Font runtimeFont;
    private Texture2D regionMaskTexture;
    private Coroutine regionMaskRoutine;
    private Coroutine chatAnimationRoutine;
    private Coroutine chatBubbleRoutine;
    private WaitForEndOfFrame endOfFrameYield;
    private float nextMaskUpdateTime;
    private GameObject runtimeEventSystemObject;
    private bool ownsSceneUiCanvas;
    private bool ownsRuntimeEventSystem;
    private bool startupLoadingVisible;
    private const int BubbleAlphaCutoff = 200;
    private const float BubbleSideInsetRatio = 0.16f;
    private const float BubbleTopInsetRatio = 0.16f;
    private const float BubbleBottomInsetRatio = 112f / 430f;

    private const string ExitMenuLabel = "\u9000\u51fa";
    private const string MenuHeaderLabel = "\u52a8\u753b\u5207\u6362";
    private const string ChatMenuLabel = "\u5bf9\u8bdd";
    private const string ChatHeaderLabel = "\u804a\u5929";
    private const string ChatInputPlaceholderLabel = "\u8f93\u5165\u4f60\u60f3\u8bf4\u7684\u8bdd...";
    private const string ChatSendLabel = "\u53d1\u9001";
    private const string ChatCloseLabel = "\u5173\u95ed";
    private const string ChatHistoryLabel = "\u5386\u53f2";
    private const string ChatHistoryHeaderLabel = "\u5bf9\u8bdd\u8bb0\u5f55";
    private const string StartupLoadingLabel = "\u52a0\u8f7d\u4e2d...";
    private void Awake()
    {
        DisablePreviewControls();
        CleanupLegacyPreviewUI();
        endOfFrameYield = new WaitForEndOfFrame();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        ResolveReferences();
        RebuildMenuStateList();
        EnsureRuntimeMenu();
        EnsureRuntimeChatPanel();
        RebuildRuntimeMenuItems();
        RefreshRuntimeCanvasVisibility();
    }

    private IEnumerator Start()
    {
        ResolveReferences();
        EnsureRuntimeMenu();
        EnsureRuntimeLoadingOverlay();
        ShowStartupLoadingOverlay();
        DisablePreviewControls();
        CacheCharacterData();
        EnsureCharacterCollider();

        ApplyScale(initialScale);
        float loadingStartTime = Time.realtimeSinceStartup;

        int waitFrames = 0;
        while (windowController != null && !windowController.IsReady && waitFrames < 120)
        {
            waitFrames++;
            yield return null;
        }

        RebuildMenuStateList();
        PlayState(initialStateName, 0f);

        yield return new WaitForEndOfFrame();

        if (autoFitWindowToPet)
        {
            CropWindowToCharacter();
        }

        if (windowController != null && startCenteredOnTaskbar)
        {
            MoveToTaskbarCenter();
        }

        if (useRenderedMaskRegion)
        {
            regionMaskRoutine = StartCoroutine(UpdateRenderedMaskRegionLoop());
        }

        float elapsed = Time.realtimeSinceStartup - loadingStartTime;
        if (elapsed < 1f)
        {
            yield return new WaitForSecondsRealtime(1f - elapsed);
        }

        HideStartupLoadingOverlay();
    }

    private void Update()
    {
        if (characterAnimator == null || windowController == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            RequestQuit();
            return;
        }

        UpdateCursorState();
        UpdateScaleInput();
        UpdateChatKeyboardInput();

        bool leftDown = windowController.IsPrimaryButtonDown();
        bool rightDown = windowController.IsSecondaryButtonDown();

        bool keepInteractive = interactionMode == InteractionMode.Dragging ||
                               menuOpen ||
                               draggingChatPanel ||
                               draggingHistoryPanel ||
                               cursorOverPet ||
                               cursorOverChat ||
                               cursorOverHistory;
        windowController.SetClickThrough(!keepInteractive && windowController.clickThroughOnEmptyArea);

        if (menuOpen)
        {
            UpdateMenuInteraction(leftDown, rightDown);
            UpdateRuntimeMenuVisuals();
        }
        else if (draggingHistoryPanel)
        {
            UpdateHistoryPanelDragging(leftDown);
        }
        else if (draggingChatPanel)
        {
            UpdateChatPanelDragging(leftDown);
        }
        else if (interactionMode == InteractionMode.Dragging)
        {
            UpdateDragging(leftDown);
        }
        else if (chatPanelOpen || chatHistoryOpen)
        {
            UpdateChatPanelInteraction(leftDown);
        }
        else
        {
            UpdatePassiveInteraction(leftDown, rightDown);
        }

        if (chatPanelOpen || chatHistoryOpen || chatBubbleVisible)
        {
            UpdateRuntimeChatVisuals();
        }

        keepInteractive = interactionMode == InteractionMode.Dragging ||
                          menuOpen ||
                          draggingChatPanel ||
                          draggingHistoryPanel ||
                          cursorOverPet ||
                          cursorOverMenu ||
                          cursorOverChat ||
                          cursorOverHistory;
        windowController.SetClickThrough(!keepInteractive && windowController.clickThroughOnEmptyArea);
        Application.targetFrameRate = keepInteractive ? activeFrameRate : idleFrameRate;

        previousLeftDown = leftDown;
        previousRightDown = rightDown;
    }

    private void ResolveReferences()
    {
        if (characterAnimator == null)
        {
            characterAnimator = FindObjectOfType<Animator>();
        }

        if (visualRoot == null && characterAnimator != null)
        {
            visualRoot = characterAnimator.transform;
        }

        if (visualRoot != null)
        {
            originalVisualScale = visualRoot.localScale;
        }

        if (petCamera == null)
        {
            petCamera = GetComponent<Camera>();
        }

        if (windowController == null)
        {
            windowController = GetComponent<透明窗口>();
        }

        if (sceneUiCanvas == null)
        {
            sceneUiCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (llmClient == null)
        {
            llmClient = GetComponent<大模型客户端>();
        }

        if (gptSoVITSClient == null)
        {
            gptSoVITSClient = GetComponent<GPTSoVITS客户端>();
        }

        if (gptSoVITSClient == null)
        {
            gptSoVITSClient = FindObjectOfType<GPTSoVITS客户端>();
        }
    }

    private void DisablePreviewControls()
    {
        动画界面控制器 previewController = GetComponent<动画界面控制器>();
        if (previewController == null)
        {
            return;
        }

        previewController.enablePreviewControls = false;
        previewController.showControlUI = false;
        previewController.enabled = false;
    }

    private void CacheCharacterData()
    {
        if (characterAnimator == null)
        {
            return;
        }

        characterRenderers = characterAnimator.GetComponentsInChildren<Renderer>(true);
    }

    private void UpdateScaleInput()
    {
        if (!cursorInsideWindow || cursorOverMenu || chatPanelOpen || chatHistoryOpen)
        {
            return;
        }

        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scrollDelta) >= 0.01f)
        {
            ApplyScale(currentScale + Mathf.Sign(scrollDelta) * scaleStep);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            ApplyScale(currentScale + scaleStep);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            ApplyScale(currentScale - scaleStep);
        }
    }

    private void ApplyScale(float newScale)
    {
        currentScale = Mathf.Clamp(newScale, minScale, maxScale);
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localScale = originalVisualScale * currentScale;
        CacheCharacterData();
        EnsureCharacterCollider();

        if (autoFitWindowToPet && windowController != null && windowController.IsReady && !menuOpen && !chatPanelOpen)
        {
            CropWindowToCharacter();
        }
    }

    private void UpdateCursorState()
    {
        cursorInsideWindow = false;
        cursorOverPet = false;
        cursorOverMenu = false;
        cursorOverChat = false;
        cursorOverHistory = false;
        hoveredMenuIndex = -1;

        if (windowController == null || !windowController.TryGetCursorClientPosition(out clientCursorPosition, out cursorInsideWindow))
        {
            return;
        }

        guiCursorPosition = new Vector2(
            clientCursorPosition.x,
            Mathf.Max(0f, windowController.WindowSize.y - clientCursorPosition.y));

        if (menuOpen && cursorInsideWindow && menuRect.Contains(guiCursorPosition))
        {
            cursorOverMenu = true;
            hoveredMenuIndex = GetMenuIndexAt(guiCursorPosition);
        }

        if (chatPanelOpen && cursorInsideWindow && chatPanelRect.Contains(guiCursorPosition))
        {
            cursorOverChat = true;
        }

        if (chatHistoryOpen && cursorInsideWindow && historyPanelRect.Contains(guiCursorPosition))
        {
            cursorOverHistory = true;
        }

        if (cursorInsideWindow)
        {
            cursorOverPet = IsCursorOverPet(clientCursorPosition);
        }
    }

    private void UpdatePassiveInteraction(bool leftDown, bool rightDown)
    {
        if (cursorOverPet && rightDown && !previousRightDown)
        {
            OpenContextMenu();
            return;
        }

        if (cursorOverPet && leftDown && !previousLeftDown)
        {
            BeginDragging();
        }
    }

    private void UpdateChatKeyboardInput()
    {
        if (!chatPanelOpen || runtimeChatInputField == null || !IsRuntimeChatInputFocused())
        {
            return;
        }

        if (pendingChatSubmitFromInputField)
        {
            pendingChatSubmitFromInputField = false;
            SubmitChatMessage();
            return;
        }

        bool enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (!enterPressed)
        {
            return;
        }

        bool altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (altPressed)
        {
            return;
        }

        if (runtimeChatInputField.lineType == InputField.LineType.MultiLineNewline)
        {
            SubmitChatMessage();
        }
    }

    private bool IsRuntimeChatInputFocused()
    {
        if (runtimeChatInputField == null)
        {
            return false;
        }

        if (runtimeChatInputField.isFocused)
        {
            return true;
        }

        EventSystem currentEventSystem = EventSystem.current;
        return currentEventSystem != null && currentEventSystem.currentSelectedGameObject == runtimeChatInputField.gameObject;
    }

    private char ValidateChatInputCharacter(string currentText, int charIndex, char addedChar)
    {
        if (addedChar != '\n' && addedChar != '\r')
        {
            return addedChar;
        }

        bool altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (altPressed)
        {
            return '\n';
        }

        pendingChatSubmitFromInputField = true;
        return '\0';
    }

    private void ConfigureRuntimeChatInputField()
    {
        if (runtimeChatInputField == null)
        {
            return;
        }

        runtimeChatInputField.lineType = InputField.LineType.MultiLineNewline;
        runtimeChatInputField.onValidateInput = ValidateChatInputCharacter;
    }

    private void UpdateChatPanelInteraction(bool leftDown)
    {
        if (!leftDown || previousLeftDown)
        {
            return;
        }

        if (chatHistoryOpen && cursorOverHistory && GetHistoryPanelDragRect().Contains(guiCursorPosition))
        {
            BeginHistoryPanelDrag();
            return;
        }

        if (chatPanelOpen && cursorOverChat && GetChatPanelDragRect().Contains(guiCursorPosition))
        {
            BeginChatPanelDrag();
        }
    }

    private void BeginDragging()
    {
        if (!windowController.TryGetCursorScreenPosition(out Vector2Int cursorPosition))
        {
            return;
        }

        StopSnapRoutine();
        CloseContextMenu();

        interactionMode = InteractionMode.Dragging;
        dragOffset = cursorPosition - windowController.GetVisibleWindowPosition();
        windowController.SetClickThrough(false);
    }

    private void UpdateDragging(bool leftDown)
    {
        if (!leftDown)
        {
            FinishDragging();
            return;
        }

        if (!windowController.TryGetCursorScreenPosition(out Vector2Int cursorPosition))
        {
            return;
        }

        Vector2Int targetPosition = new Vector2Int(
            cursorPosition.x - dragOffset.x,
            cursorPosition.y - dragOffset.y);
        Vector2Int clampedPosition = ClampToWorkingArea(targetPosition);

        windowController.MoveVisibleWindowTo(clampedPosition.x, clampedPosition.y);
    }

    private void BeginChatPanelDrag()
    {
        chatPanelManuallyPositioned = true;
        draggingChatPanel = true;
        draggingHistoryPanel = false;
        chatPanelDragOffset = guiCursorPosition - new Vector2(chatPanelRect.x, chatPanelRect.y);
        windowController.SetClickThrough(false);
    }

    private void UpdateChatPanelDragging(bool leftDown)
    {
        if (!leftDown)
        {
            draggingChatPanel = false;
            ExpandWindowRegionForChatUI();
            return;
        }

        chatPanelRect = ClampFloatingRect(new Rect(
            guiCursorPosition.x - chatPanelDragOffset.x,
            guiCursorPosition.y - chatPanelDragOffset.y,
            chatPanelRect.width,
            chatPanelRect.height));

        if (chatHistoryOpen && !historyPanelManuallyPositioned)
        {
            historyPanelRect = BuildHistoryPanelRect();
        }

        UpdateRuntimeChatVisuals();
        ExpandWindowRegionForChatUI();
    }

    private void BeginHistoryPanelDrag()
    {
        historyPanelManuallyPositioned = true;
        draggingHistoryPanel = true;
        draggingChatPanel = false;
        historyPanelDragOffset = guiCursorPosition - new Vector2(historyPanelRect.x, historyPanelRect.y);
        windowController.SetClickThrough(false);
    }

    private void UpdateHistoryPanelDragging(bool leftDown)
    {
        if (!leftDown)
        {
            draggingHistoryPanel = false;
            ExpandWindowRegionForChatUI();
            return;
        }

        historyPanelRect = ClampFloatingRect(new Rect(
            guiCursorPosition.x - historyPanelDragOffset.x,
            guiCursorPosition.y - historyPanelDragOffset.y,
            historyPanelRect.width,
            historyPanelRect.height));

        UpdateRuntimeChatVisuals();
        ExpandWindowRegionForChatUI();
    }

    private void FinishDragging()
    {
        interactionMode = InteractionMode.Passive;

        Vector2Int releasedPosition = ClampToWorkingArea(windowController.GetVisibleWindowPosition());
        windowController.MoveVisibleWindowTo(releasedPosition.x, releasedPosition.y);

        Vector2Int snapTarget = GetSnapTarget(releasedPosition, out bool snapX, out bool snapY);
        if (!snapX && !snapY)
        {
            return;
        }

        StopSnapRoutine();
        snapRoutine = StartCoroutine(AnimateSnapBounce(releasedPosition, snapTarget, snapX, snapY));
    }

    private void UpdateMenuInteraction(bool leftDown, bool rightDown)
    {
        if (!cursorInsideWindow && (leftDown && !previousLeftDown || rightDown && !previousRightDown))
        {
            CloseContextMenu();
            return;
        }

        if (rightDown && !previousRightDown && !cursorOverMenu && !cursorOverPet)
        {
            CloseContextMenu();
            return;
        }

        if (!leftDown || previousLeftDown)
        {
            return;
        }

        if (cursorOverMenu && hoveredMenuIndex >= 0 && hoveredMenuIndex < availableMenuStates.Count)
        {
            PlayState(availableMenuStates[hoveredMenuIndex], transitionDuration);
            CloseContextMenu();
            return;
        }

        if (cursorOverMenu && hoveredMenuIndex == GetChatMenuIndex())
        {
            CloseContextMenu();
            OpenChatPanel();
            return;
        }

        if (cursorOverMenu && hoveredMenuIndex == GetExitMenuIndex())
        {
            RequestQuit();
            return;
        }

        CloseContextMenu();

        if (cursorOverPet)
        {
            BeginDragging();
        }
    }

    private void OpenContextMenu()
    {
        RebuildMenuStateList();
        StopSnapRoutine();

        menuOpen = true;
        interactionMode = InteractionMode.Passive;
        menuRect = BuildMenuRect(guiCursorPosition);
        ExpandWindowRegionForMenu();
        ShowRuntimeMenu();
        windowController.SetClickThrough(false);
    }

    private void CloseContextMenu()
    {
        menuOpen = false;
        cursorOverMenu = false;
        hoveredMenuIndex = -1;
        HideRuntimeMenu();

        if (autoFitWindowToPet && windowController != null && windowController.IsReady && !chatPanelOpen)
        {
            CropWindowToCharacter();
        }
    }

    private void RebuildMenuStateList()
    {
        availableMenuStates.Clear();
        AddMenuState(initialStateName);

        if (menuStateNames == null)
        {
            return;
        }

        for (int i = 0; i < menuStateNames.Length; i++)
        {
            AddMenuState(menuStateNames[i]);
        }
    }

    private void AddMenuState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName) || availableMenuStates.Contains(stateName))
        {
            return;
        }

        if (!HasAnimatorState(stateName))
        {
            return;
        }

        availableMenuStates.Add(stateName);
    }

    private bool HasAnimatorState(string stateName)
    {
        if (characterAnimator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        return characterAnimator.HasState(0, Animator.StringToHash(stateName));
    }

    public bool PlayInitialState(float fadeDuration = -1f)
    {
        return TryPlayAnimationState(initialStateName, fadeDuration);
    }

    public bool TryPlayAnimationState(string stateName, float fadeDuration = -1f)
    {
        float actualFadeDuration = fadeDuration >= 0f ? fadeDuration : transitionDuration;
        return PlayState(stateName, actualFadeDuration);
    }

    public bool TryPlayFirstAvailableState(string[] stateNames, float fadeDuration = -1f)
    {
        if (stateNames == null || stateNames.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < stateNames.Length; i++)
        {
            if (TryPlayAnimationState(stateNames[i], fadeDuration))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryPlayRandomAvailableState(string[] stateNames, float fadeDuration = -1f)
    {
        if (stateNames == null || stateNames.Length == 0)
        {
            return false;
        }

        List<string> candidates = new List<string>();
        for (int i = 0; i < stateNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(stateNames[i]) && HasAnimatorState(stateNames[i]))
            {
                candidates.Add(stateNames[i]);
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        return TryPlayAnimationState(candidates[Random.Range(0, candidates.Count)], fadeDuration);
    }

    private bool PlayState(string stateName, float fadeDuration)
    {
        if (!HasAnimatorState(stateName))
        {
            return false;
        }

        characterAnimator.CrossFade(Animator.StringToHash(stateName), fadeDuration, 0);
        currentStateName = stateName;
        return true;
    }

    private bool IsCursorOverPet(Vector2 clientPosition)
    {
        return HitTestByRaycast(clientPosition) || HitTestByScreenBounds(clientPosition);
    }

    private bool HitTestByRaycast(Vector2 clientPosition)
    {
        if (petCamera == null || characterAnimator == null)
        {
            return false;
        }

        Ray ray = petCamera.ScreenPointToRay(clientPosition);
        return Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Collide) &&
               hit.transform.IsChildOf(characterAnimator.transform);
    }

    private bool HitTestByScreenBounds(Vector2 clientPosition)
    {
        if (petCamera == null || characterRenderers == null || characterRenderers.Length == 0)
        {
            return false;
        }

        if (!TryGetProjectedBounds(out Rect projectedRect))
        {
            return false;
        }

        projectedRect.xMin -= 12f;
        projectedRect.xMax += 12f;
        projectedRect.yMin -= 12f;
        projectedRect.yMax += 12f;
        return projectedRect.Contains(clientPosition);
    }

    private bool TryGetProjectedBounds(out Rect projectedRect)
    {
        bool hasPoint = false;
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            Renderer renderer = characterRenderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldPoint = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 screenPoint = petCamera.WorldToScreenPoint(worldPoint);
                        if (screenPoint.z <= 0f)
                        {
                            continue;
                        }

                        hasPoint = true;
                        minX = Mathf.Min(minX, screenPoint.x);
                        minY = Mathf.Min(minY, screenPoint.y);
                        maxX = Mathf.Max(maxX, screenPoint.x);
                        maxY = Mathf.Max(maxY, screenPoint.y);
                    }
                }
            }
        }

        if (!hasPoint)
        {
            projectedRect = default;
            return false;
        }

        projectedRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    private void EnsureCharacterCollider()
    {
        if (characterAnimator == null)
        {
            return;
        }

        Collider existingCollider = characterAnimator.GetComponentInChildren<Collider>();
        if (existingCollider != null && generatedCollider == null)
        {
            return;
        }

        if (characterRenderers == null || characterRenderers.Length == 0)
        {
            return;
        }

        Bounds combinedBounds = characterRenderers[0].bounds;
        for (int i = 1; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] == null)
            {
                continue;
            }

            combinedBounds.Encapsulate(characterRenderers[i].bounds);
        }

        generatedCollider = characterAnimator.gameObject.GetComponent<CapsuleCollider>();
        if (generatedCollider == null)
        {
            generatedCollider = characterAnimator.gameObject.AddComponent<CapsuleCollider>();
        }

        generatedCollider.direction = 1;
        generatedCollider.center = characterAnimator.transform.InverseTransformPoint(combinedBounds.center);
        generatedCollider.height = Mathf.Max(0.1f, combinedBounds.size.y);
        generatedCollider.radius = Mathf.Max(0.05f, Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.z) * 0.65f);
        generatedCollider.isTrigger = true;
    }


    private void CropWindowToCharacter()
    {
        if (windowController == null || !TryGetProjectedBounds(out Rect projectedRect))
        {
            return;
        }

        Vector2Int fullWindowSize = windowController.WindowSize;
        if (fullWindowSize.x <= 0 || fullWindowSize.y <= 0)
        {
            return;
        }

        Vector2Int effectivePadding = new Vector2Int(
            Mathf.Max(0, Mathf.Min(contentPadding.x, maxAutoFitPadding.x)),
            Mathf.Max(0, Mathf.Min(contentPadding.y, maxAutoFitPadding.y)));

        int cropWidth = Mathf.Clamp(
            Mathf.CeilToInt(projectedRect.width) + effectivePadding.x,
            1,
            fullWindowSize.x);
        int cropHeight = Mathf.Clamp(
            Mathf.CeilToInt(projectedRect.height) + effectivePadding.y,
            1,
            fullWindowSize.y);

        int cropLeft = Mathf.FloorToInt(projectedRect.xMin) - effectivePadding.x / 2;
        int cropBottom = Mathf.FloorToInt(projectedRect.yMin) - effectivePadding.y / 2;

        cropLeft = Mathf.Clamp(cropLeft, 0, Mathf.Max(0, fullWindowSize.x - cropWidth));
        cropBottom = Mathf.Clamp(cropBottom, 0, Mathf.Max(0, fullWindowSize.y - cropHeight));

        int cropTop = fullWindowSize.y - cropBottom - cropHeight;
        cropTop = Mathf.Clamp(cropTop, 0, Mathf.Max(0, fullWindowSize.y - cropHeight));

        RectInt cropRect = new RectInt(cropLeft, cropTop, cropWidth, cropHeight);
        windowController.SetWindowRegion(cropRect);
    }

    private void MoveToTaskbarCenter()
    {
        Vector2Int visibleSize = windowController.VisibleWindowSize.x > 0 && windowController.VisibleWindowSize.y > 0
            ? windowController.VisibleWindowSize
            : windowController.WindowSize;

        int left = 0;
        int top = 0;
        int right = Mathf.Max(0, Display.main.systemWidth - visibleSize.x);
        int bottom = Mathf.Max(0, Display.main.systemHeight - visibleSize.y);
        int centeredX = left + Mathf.Max(0, (right - left) / 2);
        windowController.MoveVisibleWindowTo(centeredX, bottom);
    }

    private Vector2Int ClampToWorkingArea(Vector2Int position)
    {
        GetWindowLimits(out int left, out int top, out int right, out int bottom);
        return new Vector2Int(
            Mathf.Clamp(position.x, left, right),
            Mathf.Clamp(position.y, top, bottom));
    }

    private Vector2Int GetSnapTarget(Vector2Int currentPosition, out bool snapX, out bool snapY)
    {
        GetWindowLimits(out int left, out int top, out int right, out int bottom);

        int targetX = currentPosition.x;
        int targetY = currentPosition.y;
        snapX = false;
        snapY = false;

        int distanceToLeft = Mathf.Abs(currentPosition.x - left);
        int distanceToRight = Mathf.Abs(currentPosition.x - right);
        int distanceToTop = Mathf.Abs(currentPosition.y - top);
        int distanceToBottom = Mathf.Abs(currentPosition.y - bottom);

        int horizontalDistance = Mathf.Min(distanceToLeft, distanceToRight);
        if (horizontalDistance <= edgeSnapDistance)
        {
            snapX = true;
            targetX = distanceToLeft <= distanceToRight ? left : right;
        }

        if (distanceToBottom <= taskbarSnapDistance)
        {
            snapY = true;
            targetY = bottom;
        }
        else if (distanceToTop <= edgeSnapDistance)
        {
            snapY = true;
            targetY = top;
        }

        return new Vector2Int(targetX, targetY);
    }

    private IEnumerator AnimateSnapBounce(Vector2Int from, Vector2Int target, bool snappedX, bool snappedY)
    {
        yield return AnimateWindow(from, target, snapDuration);

        GetWindowLimits(out int left, out int top, out int right, out int bottom);
        Vector2Int bounceTarget = target;

        if (snappedX)
        {
            bounceTarget.x = Mathf.Clamp(
                target.x == left ? target.x + bounceDistance : target.x - bounceDistance,
                left,
                right);
        }

        if (snappedY)
        {
            if (target.y == bottom)
            {
                bounceTarget.y = Mathf.Clamp(target.y - bounceDistance, top, bottom);
            }
            else if (target.y == top)
            {
                bounceTarget.y = Mathf.Clamp(target.y + bounceDistance, top, bottom);
            }
        }

        if (bounceTarget != target)
        {
            yield return AnimateWindow(target, bounceTarget, bounceDuration);
            yield return AnimateWindow(bounceTarget, target, bounceDuration);
        }

        snapRoutine = null;
    }

    private IEnumerator AnimateWindow(Vector2Int from, Vector2Int to, float duration)
    {
        if (duration <= 0f)
        {
            windowController.MoveVisibleWindowTo(to.x, to.y);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector2 lerped = Vector2.Lerp(from, to, eased);
            windowController.MoveVisibleWindowTo(
                Mathf.RoundToInt(lerped.x),
                Mathf.RoundToInt(lerped.y));
            yield return null;
        }

        windowController.MoveVisibleWindowTo(to.x, to.y);
    }

    private void StopSnapRoutine()
    {
        if (snapRoutine == null)
        {
            return;
        }

        StopCoroutine(snapRoutine);
        snapRoutine = null;
    }

    private IEnumerator UpdateRenderedMaskRegionLoop()
    {
        while (true)
        {
            yield return endOfFrameYield;

            if (!ShouldUpdateRenderedMaskRegion())
            {
                continue;
            }

            if (Time.unscaledTime < nextMaskUpdateTime)
            {
                continue;
            }

            nextMaskUpdateTime = Time.unscaledTime + Mathf.Max(0.02f, maskRegionUpdateInterval);
            UpdateWindowRegionFromRenderedMask();
        }
    }

    private bool ShouldUpdateRenderedMaskRegion()
    {
        return useRenderedMaskRegion &&
               windowController != null &&
               windowController.IsReady &&
               !menuOpen &&
               !chatPanelOpen &&
               interactionMode != InteractionMode.Dragging &&
               !cursorOverMenu &&
               Application.isPlaying;
    }

    private void UpdateWindowRegionFromRenderedMask()
    {
        if (windowController == null)
        {
            return;
        }

        Vector2Int windowSize = windowController.WindowSize;
        if (windowSize.x <= 0 || windowSize.y <= 0)
        {
            return;
        }

        EnsureRegionMaskTexture(windowSize.x, windowSize.y);
        regionMaskTexture.ReadPixels(new Rect(0f, 0f, windowSize.x, windowSize.y), 0, 0, false);
        regionMaskTexture.Apply(false, false);

        Color32[] pixels = regionMaskTexture.GetPixels32();
        List<RectInt> regions = BuildMaskRegions(pixels, windowSize.x, windowSize.y);
        if (regions.Count == 0)
        {
            CropWindowToCharacter();
            return;
        }

        windowController.SetWindowRegions(regions.ToArray());
    }

    private void EnsureRegionMaskTexture(int width, int height)
    {
        if (regionMaskTexture != null && regionMaskTexture.width == width && regionMaskTexture.height == height)
        {
            return;
        }

        if (regionMaskTexture != null)
        {
            Destroy(regionMaskTexture);
        }

        regionMaskTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    private List<RectInt> BuildMaskRegions(Color32[] pixels, int width, int height)
    {
        List<RectInt> regions = new List<RectInt>(height / Mathf.Max(1, maskRowStep));
        Color32 background = windowController != null
            ? (Color32)windowController.transparentBackground
            : new Color32(255, 0, 255, 255);
        int rowStep = Mathf.Max(1, maskRowStep);
        int edgePadding = Mathf.Max(0, maskEdgePadding);

        for (int y = 0; y < height; y += rowStep)
        {
            int rowStart = y * width;
            int segmentStart = -1;

            for (int x = 0; x < width; x++)
            {
                bool visiblePixel = !IsBackgroundPixel(pixels[rowStart + x], background);
                if (visiblePixel)
                {
                    if (segmentStart < 0)
                    {
                        segmentStart = x;
                    }

                    continue;
                }

                AddMaskSegment(regions, segmentStart, x, y, width, height, rowStep, edgePadding);
                segmentStart = -1;
            }

            AddMaskSegment(regions, segmentStart, width, y, width, height, rowStep, edgePadding);
        }

        return regions;
    }

    private bool IsBackgroundPixel(Color32 pixel, Color32 background)
    {
        return Mathf.Abs(pixel.r - background.r) <= maskColorTolerance &&
               Mathf.Abs(pixel.g - background.g) <= maskColorTolerance &&
               Mathf.Abs(pixel.b - background.b) <= maskColorTolerance;
    }

    private static void AddMaskSegment(
        List<RectInt> regions,
        int segmentStart,
        int segmentEnd,
        int sourceRow,
        int width,
        int height,
        int rowStep,
        int edgePadding)
    {
        if (segmentStart < 0 || segmentEnd <= segmentStart)
        {
            return;
        }

        int xMin = Mathf.Max(0, segmentStart - edgePadding);
        int xMax = Mathf.Min(width, segmentEnd + edgePadding);
        int rowHeight = Mathf.Min(rowStep, height - sourceRow);
        int top = height - (sourceRow + rowHeight);
        top = Mathf.Max(0, top - edgePadding);
        int paddedHeight = Mathf.Min(height - top, rowHeight + edgePadding * 2);

        if (xMax <= xMin || paddedHeight <= 0)
        {
            return;
        }

        regions.Add(new RectInt(xMin, top, xMax - xMin, paddedHeight));
    }

    private void GetWindowLimits(out int left, out int top, out int right, out int bottom)
    {
        RectInt workArea = windowController.VirtualDesktopBounds.width > 0
            ? windowController.VirtualDesktopBounds
            : windowController.WorkingArea;
        Vector2Int effectiveWindowSize = windowController.VisibleWindowSize.x > 0 && windowController.VisibleWindowSize.y > 0
            ? windowController.VisibleWindowSize
            : windowController.WindowSize;
        left = workArea.x;
        top = workArea.y;
        right = Mathf.Max(left, workArea.x + workArea.width - effectiveWindowSize.x);
        bottom = Mathf.Max(top, workArea.y + workArea.height - effectiveWindowSize.y);
    }

    private Rect BuildMenuRect(Vector2 guiCursor)
    {
        float menuHeight = menuPadding.y * 2f + menuHeaderHeight + (availableMenuStates.Count + 2) * menuItemHeight;
        float maxX = Mathf.Max(8f, windowController.WindowSize.x - menuWidth - 8f);
        float maxY = Mathf.Max(8f, windowController.WindowSize.y - menuHeight - 8f);

        float x = Mathf.Clamp(guiCursor.x + 12f, 8f, maxX);
        float y = Mathf.Clamp(guiCursor.y + 12f, 8f, maxY);
        return new Rect(x, y, menuWidth, menuHeight);
    }

    private int GetChatMenuIndex()
    {
        return availableMenuStates.Count;
    }

    private int GetExitMenuIndex()
    {
        return availableMenuStates.Count + 1;
    }

    private int GetMenuIndexAt(Vector2 guiPosition)
    {
        for (int i = 0; i < availableMenuStates.Count; i++)
        {
            if (GetMenuItemRect(i).Contains(guiPosition))
            {
                return i;
            }
        }

        if (GetChatItemRect().Contains(guiPosition))
        {
            return GetChatMenuIndex();
        }

        if (GetExitItemRect().Contains(guiPosition))
        {
            return GetExitMenuIndex();
        }

        return -1;
    }

    private Rect GetMenuItemRect(int index)
    {
        float x = menuRect.x + 6f;
        float y = menuRect.y + menuPadding.y + menuHeaderHeight + index * menuItemHeight;
        float width = menuRect.width - 12f;
        float height = menuItemHeight - 2f;
        return new Rect(x, y, width, height);
    }

    private Rect GetExitItemRect()
    {
        return GetMenuItemRect(GetExitMenuIndex());
    }

    private Rect GetChatItemRect()
    {
        return GetMenuItemRect(GetChatMenuIndex());
    }

    private Rect BuildChatPanelRect()
    {
        float width = Mathf.Clamp(chatDockSize.x, 240f, Mathf.Max(240f, windowController.WindowSize.x - 16f));
        float height = Mathf.Clamp(chatDockSize.y, 74f, Mathf.Max(74f, windowController.WindowSize.y - 16f));
        RectInt petRect = GetPetClientRectOrFallback();

        Rect panelRect = new Rect(
            petRect.center.x - width * 0.5f + chatDockOffset.x,
            petRect.yMax + 12f + chatDockOffset.y,
            width,
            height);

        return ClampFloatingRect(panelRect);
    }

    private Rect BuildHistoryPanelRect()
    {
        float width = Mathf.Clamp(chatPanelSize.x, 240f, Mathf.Max(240f, windowController.WindowSize.x - 16f));
        float height = Mathf.Clamp(chatPanelSize.y, 180f, Mathf.Max(180f, windowController.WindowSize.y - 16f));
        const float gap = 12f;
        const float padding = 8f;

        float maxX = Mathf.Max(padding, windowController.WindowSize.x - width - padding);
        float maxY = Mathf.Max(padding, windowController.WindowSize.y - height - padding);

        float rightX = chatPanelRect.xMax + gap;
        float leftX = chatPanelRect.x - width - gap;
        float topY = chatPanelRect.y - height - gap;

        Rect panelRect;
        if (rightX <= maxX)
        {
            panelRect = new Rect(rightX, Mathf.Clamp(chatPanelRect.y, padding, maxY), width, height);
        }
        else if (leftX >= padding)
        {
            panelRect = new Rect(leftX, Mathf.Clamp(chatPanelRect.y, padding, maxY), width, height);
        }
        else
        {
            panelRect = new Rect(
                Mathf.Clamp(chatPanelRect.center.x - width * 0.5f, padding, maxX),
                Mathf.Clamp(topY, padding, maxY),
                width,
                height);
        }

        return ClampFloatingRect(panelRect);
    }

    private Rect BuildChatBubbleRect()
    {
        RectInt petRect = GetPetClientRectOrFallback();
        Sprite bubbleSprite = chatBubbleBodySpriteOverride != null ? chatBubbleBodySpriteOverride : runtimeChatBubbleBodySprite;
        Vector2 bubbleSize = CalculateChatBubbleSize(currentChatBubbleMessage, bubbleSprite);
        float bubbleWidth = bubbleSize.x;
        float bubbleHeight = bubbleSize.y;

        Rect panelRect = new Rect(
            petRect.center.x - bubbleWidth * 0.38f + 气泡偏移.x,
            petRect.y - bubbleHeight - 16f + 气泡偏移.y,
            bubbleWidth,
            bubbleHeight);

        return ClampFloatingRect(panelRect);
    }

    private Vector2 CalculateChatBubbleSize(string message, Sprite bubbleSprite)
    {
        float spriteAspect = bubbleSprite != null && bubbleSprite.rect.height > 0.01f
            ? bubbleSprite.rect.width / bubbleSprite.rect.height
            : 1.8f;
        spriteAspect = Mathf.Max(0.1f, spriteAspect);

        float minBubbleWidth = 132f;
        float minBubbleHeight = 100f;
        float maxBubbleWidth = Mathf.Max(minBubbleWidth, windowController != null ? windowController.WindowSize.x - 16f : chatBubbleMaxSize.x);
        float configuredMaxWidth = Mathf.Max(minBubbleWidth, chatBubbleMaxSize.x);
        float configuredMaxHeight = Mathf.Max(minBubbleHeight, chatBubbleMaxSize.y);
        maxBubbleWidth = Mathf.Min(maxBubbleWidth, configuredMaxWidth, configuredMaxHeight * spriteAspect);

        if (string.IsNullOrWhiteSpace(message) || runtimeChatBubbleText == null)
        {
            float fallbackWidth = Mathf.Clamp(configuredMaxWidth, minBubbleWidth, maxBubbleWidth);
            return new Vector2(fallbackWidth, fallbackWidth / spriteAspect);
        }

        string trimmedMessage = message.Trim();
        TextGenerator generator = new TextGenerator();
        float maxTextWidth = maxBubbleWidth * (1f - BubbleSideInsetRatio * 2f);
        TextGenerationSettings preferredWidthSettings = runtimeChatBubbleText.GetGenerationSettings(new Vector2(10000f, 10000f));
        preferredWidthSettings.horizontalOverflow = HorizontalWrapMode.Overflow;
        preferredWidthSettings.verticalOverflow = VerticalWrapMode.Overflow;
        preferredWidthSettings.generateOutOfBounds = true;
        float preferredTextWidth = generator.GetPreferredWidth(trimmedMessage, preferredWidthSettings);

        float desiredTextWidth = Mathf.Clamp(preferredTextWidth + 10f, 64f, maxTextWidth);
        float bestWidth = Mathf.Clamp(
            desiredTextWidth / Mathf.Max(0.1f, 1f - BubbleSideInsetRatio * 2f),
            minBubbleWidth,
            maxBubbleWidth);
        float widthStep = 10f;

        for (float candidateWidth = bestWidth; candidateWidth <= maxBubbleWidth + 0.01f; candidateWidth += widthStep)
        {
            float candidateHeight = candidateWidth / spriteAspect;
            float textWidth = candidateWidth * (1f - BubbleSideInsetRatio * 2f);
            float textHeight = candidateHeight * (1f - BubbleTopInsetRatio - BubbleBottomInsetRatio);

            TextGenerationSettings settings = runtimeChatBubbleText.GetGenerationSettings(new Vector2(textWidth, 10000f));
            settings.horizontalOverflow = HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.generateOutOfBounds = true;
            float preferredHeight = generator.GetPreferredHeight(trimmedMessage, settings);

            if (preferredHeight <= textHeight)
            {
                bestWidth = candidateWidth;
                break;
            }

            bestWidth = candidateWidth;
        }

        float finalWidth = Mathf.Clamp(bestWidth, minBubbleWidth, maxBubbleWidth);
        float finalHeight = finalWidth / spriteAspect;
        return new Vector2(finalWidth, finalHeight);
    }

    private Rect GetChatPanelDragRect()
    {
        return new Rect(
            chatPanelRect.x + 10f,
            chatPanelRect.y + 8f,
            Mathf.Max(72f, chatPanelRect.width - 150f),
            28f);
    }

    private Rect GetHistoryPanelDragRect()
    {
        return new Rect(
            historyPanelRect.x + 10f,
            historyPanelRect.y + 8f,
            Mathf.Max(72f, historyPanelRect.width - 86f),
            28f);
    }

    private Rect ClampFloatingRect(Rect rect)
    {
        if (windowController == null)
        {
            return rect;
        }

        const float padding = 8f;
        float maxX = Mathf.Max(padding, windowController.WindowSize.x - rect.width - padding);
        float maxY = Mathf.Max(padding, windowController.WindowSize.y - rect.height - padding);

        rect.x = Mathf.Clamp(rect.x, padding, maxX);
        rect.y = Mathf.Clamp(rect.y, padding, maxY);
        return rect;
    }

    private bool TryGetPetClientRect(out RectInt petRect)
    {
        petRect = default;

        if (windowController == null || !TryGetProjectedBounds(out Rect projectedRect))
        {
            return false;
        }

        Vector2Int fullWindowSize = windowController.WindowSize;
        if (fullWindowSize.x <= 0 || fullWindowSize.y <= 0)
        {
            return false;
        }

        Vector2Int effectivePadding = new Vector2Int(
            Mathf.Max(0, Mathf.Min(contentPadding.x, maxAutoFitPadding.x)),
            Mathf.Max(0, Mathf.Min(contentPadding.y, maxAutoFitPadding.y)));

        int cropWidth = Mathf.Clamp(
            Mathf.CeilToInt(projectedRect.width) + effectivePadding.x,
            1,
            fullWindowSize.x);
        int cropHeight = Mathf.Clamp(
            Mathf.CeilToInt(projectedRect.height) + effectivePadding.y,
            1,
            fullWindowSize.y);

        int cropLeft = Mathf.FloorToInt(projectedRect.xMin) - effectivePadding.x / 2;
        int cropBottom = Mathf.FloorToInt(projectedRect.yMin) - effectivePadding.y / 2;

        cropLeft = Mathf.Clamp(cropLeft, 0, Mathf.Max(0, fullWindowSize.x - cropWidth));
        cropBottom = Mathf.Clamp(cropBottom, 0, Mathf.Max(0, fullWindowSize.y - cropHeight));

        int cropTop = fullWindowSize.y - cropBottom - cropHeight;
        cropTop = Mathf.Clamp(cropTop, 0, Mathf.Max(0, fullWindowSize.y - cropHeight));

        petRect = new RectInt(cropLeft, cropTop, cropWidth, cropHeight);
        cachedPetClientRect = petRect;
        hasCachedPetClientRect = true;
        return true;
    }

    private RectInt GetPetClientRectOrFallback()
    {
        if (TryGetPetClientRect(out RectInt petRect))
        {
            return petRect;
        }

        if (hasCachedPetClientRect)
        {
            return cachedPetClientRect;
        }

        return windowController != null
            ? windowController.VisibleContentRect
            : new RectInt(0, 0, 240, 320);
    }

    private static RectInt ToClientRect(Rect rect)
    {
        return new RectInt(
            Mathf.FloorToInt(rect.x),
            Mathf.FloorToInt(rect.y),
            Mathf.CeilToInt(rect.width),
            Mathf.CeilToInt(rect.height));
    }

    private void EnsureVisibleWindowInsideWorkArea()
    {
        if (windowController == null || !windowController.IsReady)
        {
            return;
        }

        Vector2Int visiblePosition = ClampToWorkingArea(windowController.GetVisibleWindowPosition());
        windowController.MoveVisibleWindowTo(visiblePosition.x, visiblePosition.y);
    }

    private void ExpandWindowRegionForMenu()
    {
        if (windowController == null)
        {
            return;
        }

        RectInt petRect = GetPetClientRectOrFallback();
        RectInt menuClientRect = ToClientRect(menuRect);

        windowController.SetWindowRegions(petRect, menuClientRect);
        EnsureVisibleWindowInsideWorkArea();
    }

    private void ExpandWindowRegionForChatUI()
    {
        if (windowController == null)
        {
            return;
        }

        List<RectInt> regions = new List<RectInt>(4)
        {
            GetPetClientRectOrFallback()
        };

        if (chatPanelOpen)
        {
            regions.Add(ToClientRect(chatPanelRect));
        }

        if (chatHistoryOpen)
        {
            regions.Add(ToClientRect(historyPanelRect));
        }

        if (chatBubbleVisible)
        {
            regions.Add(ToClientRect(chatBubbleRect));
        }

        windowController.SetWindowRegions(regions.ToArray());
        EnsureVisibleWindowInsideWorkArea();
    }

    private void OpenChatPanel()
    {
        EnsureRuntimeChatPanel();
        bool showGreetingBubble = false;

        if (chatHistory.Count == 0)
        {
            AppendChatLine("\u5c0f\u6d63\u718a", defaultGreeting);
            showGreetingBubble = true;
        }

        chatPanelOpen = true;
        chatHistoryOpen = false;
        chatPanelManuallyPositioned = false;
        historyPanelManuallyPositioned = false;
        draggingChatPanel = false;
        draggingHistoryPanel = false;
        chatPanelRect = BuildChatPanelRect();
        if (runtimeChatHistoryPanelRoot != null)
        {
            runtimeChatHistoryPanelRoot.gameObject.SetActive(false);
        }

        UpdateRuntimeChatVisuals();
        runtimeChatPanelRoot.gameObject.SetActive(true);
        RefreshRuntimeCanvasVisibility();
        if (showGreetingBubble)
        {
            ShowPetChatBubble(defaultGreeting);
        }
        ExpandWindowRegionForChatUI();
        windowController.SetClickThrough(false);

        if (runtimeChatInputField != null)
        {
            runtimeChatInputField.Select();
            runtimeChatInputField.ActivateInputField();
        }
    }

    private void CloseChatPanel()
    {
        chatPanelOpen = false;
        chatHistoryOpen = false;
        draggingChatPanel = false;
        draggingHistoryPanel = false;
        chatPanelManuallyPositioned = false;
        historyPanelManuallyPositioned = false;
        cursorOverChat = false;
        cursorOverHistory = false;

        if (runtimeChatPanelRoot != null)
        {
            runtimeChatPanelRoot.gameObject.SetActive(false);
        }

        if (runtimeChatHistoryPanelRoot != null)
        {
            runtimeChatHistoryPanelRoot.gameObject.SetActive(false);
        }

        HidePetChatBubble(false);
        RefreshRuntimeCanvasVisibility();

        if (autoFitWindowToPet && windowController != null && windowController.IsReady && !menuOpen)
        {
            CropWindowToCharacter();
        }
    }

    private void ToggleChatHistoryPanel()
    {
        if (!chatPanelOpen)
        {
            return;
        }

        chatHistoryOpen = !chatHistoryOpen;
        draggingHistoryPanel = false;
        cursorOverHistory = false;

        if (chatHistoryOpen)
        {
            if (!historyPanelManuallyPositioned)
            {
                historyPanelRect = BuildHistoryPanelRect();
            }

            scrollHistoryToBottomPending = true;
            if (runtimeChatHistoryPanelRoot != null)
            {
                runtimeChatHistoryPanelRoot.gameObject.SetActive(true);
            }
        }
        else if (runtimeChatHistoryPanelRoot != null)
        {
            runtimeChatHistoryPanelRoot.gameObject.SetActive(false);
        }

        UpdateRuntimeChatVisuals();
        RefreshRuntimeCanvasVisibility();
        ExpandWindowRegionForChatUI();
    }

    private void ShowRuntimeMenu()
    {
        EnsureRuntimeMenu();
        RebuildRuntimeMenuItems();
        UpdateRuntimeMenuVisuals();
        runtimeMenuRoot.gameObject.SetActive(true);
        RefreshRuntimeCanvasVisibility();
    }

    private void HideRuntimeMenu()
    {
        if (runtimeMenuRoot != null)
        {
            runtimeMenuRoot.gameObject.SetActive(false);
        }

        RefreshRuntimeCanvasVisibility();
    }

    private void EnsureRuntimeMenu()
    {
        if (runtimeMenuCanvas != null)
        {
            TryBindExistingSceneUi();
            if (runtimeMenuRoot != null)
            {
                return;
            }
        }

        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (runtimeFont == null)
        {
            runtimeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        runtimeMenuCanvas = sceneUiCanvas;
        if (runtimeMenuCanvas == null)
        {
            Transform existingCanvasTransform = transform.Find("DesktopPetMenuCanvas");
            if (existingCanvasTransform != null)
            {
                runtimeMenuCanvas = existingCanvasTransform.GetComponent<Canvas>();
            }
        }

        if (runtimeMenuCanvas == null)
        {
            GameObject canvasObject = new GameObject("DesktopPetMenuCanvas");
            canvasObject.transform.SetParent(transform, false);
            runtimeMenuCanvas = canvasObject.AddComponent<Canvas>();
            runtimeMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeMenuCanvas.sortingOrder = 2000;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            ownsSceneUiCanvas = true;
            sceneUiCanvas = runtimeMenuCanvas;
        }
        else
        {
            ownsSceneUiCanvas = false;
            sceneUiCanvas = runtimeMenuCanvas;
            runtimeMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeMenuCanvas.sortingOrder = 2000;

            if (runtimeMenuCanvas.GetComponent<CanvasScaler>() == null)
            {
                runtimeMenuCanvas.gameObject.AddComponent<CanvasScaler>();
            }

            if (runtimeMenuCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                runtimeMenuCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        runtimeMenuCanvas.gameObject.SetActive(false);

        TryBindExistingSceneUi();
        if (runtimeMenuRoot != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("MenuRoot");
        rootObject.transform.SetParent(runtimeMenuCanvas.transform, false);
        runtimeMenuRoot = rootObject.AddComponent<RectTransform>();
        runtimeMenuRoot.anchorMin = new Vector2(0f, 1f);
        runtimeMenuRoot.anchorMax = new Vector2(0f, 1f);
        runtimeMenuRoot.pivot = new Vector2(0f, 1f);
        Image rootImage = rootObject.AddComponent<Image>();
        rootImage.color = GetRuntimeMenuColor(menuPanelColor);

        runtimeMenuHeader = CreateRuntimeMenuText("Header", rootObject.transform, 14, FontStyle.Bold);
        RectTransform headerRect = runtimeMenuHeader.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -menuPadding.y);
        headerRect.sizeDelta = new Vector2(-menuPadding.x * 2f, menuHeaderHeight);
        runtimeMenuHeader.alignment = TextAnchor.MiddleLeft;
        runtimeMenuHeader.text = MenuHeaderLabel;
        runtimeMenuHeader.color = new Color(0.96f, 0.97f, 0.99f, 1f);

        GameObject itemsRootObject = new GameObject("Items");
        itemsRootObject.transform.SetParent(rootObject.transform, false);
        runtimeMenuItemsRoot = itemsRootObject.AddComponent<RectTransform>();
        runtimeMenuItemsRoot.anchorMin = new Vector2(0f, 1f);
        runtimeMenuItemsRoot.anchorMax = new Vector2(0f, 1f);
        runtimeMenuItemsRoot.pivot = new Vector2(0f, 1f);
        runtimeMenuItemsRoot.anchoredPosition = Vector2.zero;
        runtimeMenuItemsRoot.sizeDelta = Vector2.zero;
        runtimeMenuRoot.gameObject.SetActive(false);
    }

    private void EnsureRuntimeEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        runtimeEventSystemObject = new GameObject("DesktopPetEventSystem");
        runtimeEventSystemObject.AddComponent<EventSystem>();
        runtimeEventSystemObject.AddComponent<StandaloneInputModule>();
        ownsRuntimeEventSystem = true;
    }

    private void EnsureRuntimeChatPanel()
    {
        EnsureRuntimeMenu();
        EnsureRuntimeEventSystem();
        EnsureRuntimeLoadingOverlay();

        TryBindExistingSceneUi();
        if (runtimeChatPanelRoot != null)
        {
            RebindChatButtonListeners();
            return;
        }

        GameObject panelObject = new GameObject("ChatPanel");
        panelObject.transform.SetParent(runtimeMenuCanvas.transform, false);
        runtimeChatPanelRoot = panelObject.AddComponent<RectTransform>();
        runtimeChatPanelRoot.anchorMin = new Vector2(0f, 1f);
        runtimeChatPanelRoot.anchorMax = new Vector2(0f, 1f);
        runtimeChatPanelRoot.pivot = new Vector2(0f, 1f);
        runtimeChatPanelBackground = panelObject.AddComponent<Image>();
        runtimeChatPanelBackground.color = GetRuntimeMenuColor(menuPanelColor);

        runtimeChatHeader = CreateRuntimeMenuText("ChatHeader", panelObject.transform, 15, FontStyle.Bold);
        RectTransform headerRect = runtimeChatHeader.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -12f);
        headerRect.sizeDelta = new Vector2(-144f, 26f);
        runtimeChatHeader.alignment = TextAnchor.MiddleLeft;
        runtimeChatHeader.text = ChatHeaderLabel;
        runtimeChatHeader.color = new Color(0.96f, 0.97f, 0.99f, 1f);

        GameObject historyButtonObject = CreateChatButtonObject("ChatHistoryButton", panelObject.transform, ChatHistoryLabel, out runtimeChatHistoryButton);
        RectTransform historyButtonRect = historyButtonObject.GetComponent<RectTransform>();
        historyButtonRect.anchorMin = new Vector2(1f, 1f);
        historyButtonRect.anchorMax = new Vector2(1f, 1f);
        historyButtonRect.pivot = new Vector2(1f, 1f);
        historyButtonRect.sizeDelta = new Vector2(54f, 24f);
        historyButtonRect.anchoredPosition = new Vector2(-72f, -8f);
        runtimeChatHistoryButton.onClick.AddListener(ToggleChatHistoryPanel);

        GameObject closeButtonObject = CreateChatButtonObject("ChatCloseButton", panelObject.transform, ChatCloseLabel, out runtimeChatCloseButton);
        RectTransform closeRect = closeButtonObject.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(54f, 24f);
        closeRect.anchoredPosition = new Vector2(-10f, -8f);
        runtimeChatCloseButton.onClick.AddListener(CloseChatPanel);

        GameObject inputBackgroundObject = new GameObject("ChatInputBackground");
        inputBackgroundObject.transform.SetParent(panelObject.transform, false);
        RectTransform inputBackgroundRect = inputBackgroundObject.AddComponent<RectTransform>();
        inputBackgroundRect.anchorMin = new Vector2(0f, 0f);
        inputBackgroundRect.anchorMax = new Vector2(1f, 0f);
        inputBackgroundRect.pivot = new Vector2(0.5f, 0f);
        inputBackgroundRect.sizeDelta = new Vector2(-88f, 56f);
        inputBackgroundRect.anchoredPosition = new Vector2(-34f, 10f);
        Image inputBackground = inputBackgroundObject.AddComponent<Image>();
        inputBackground.color = GetRuntimeMenuColor(new Color(0.16f, 0.19f, 0.25f, 1f));

        runtimeChatInputField = inputBackgroundObject.AddComponent<InputField>();
        runtimeChatInputField.targetGraphic = inputBackground;
        ConfigureRuntimeChatInputField();

        Text inputText = CreateRuntimeMenuText("Text", inputBackgroundObject.transform, 13, FontStyle.Normal);
        RectTransform inputTextRect = inputText.rectTransform;
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = new Vector2(10f, 6f);
        inputTextRect.offsetMax = new Vector2(-10f, -6f);
        inputText.alignment = TextAnchor.UpperLeft;
        inputText.color = new Color(0.97f, 0.98f, 0.99f, 1f);
        inputText.supportRichText = false;
        inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
        inputText.verticalOverflow = VerticalWrapMode.Overflow;

        Text placeholderText = CreateRuntimeMenuText("Placeholder", inputBackgroundObject.transform, 13, FontStyle.Italic);
        RectTransform placeholderRect = placeholderText.rectTransform;
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10f, 6f);
        placeholderRect.offsetMax = new Vector2(-10f, -6f);
        placeholderText.alignment = TextAnchor.UpperLeft;
        placeholderText.color = new Color(0.68f, 0.72f, 0.78f, 1f);
        placeholderText.text = ChatInputPlaceholderLabel;

        runtimeChatInputField.textComponent = inputText;
        runtimeChatInputField.placeholder = placeholderText;

        GameObject sendButtonObject = CreateChatButtonObject("ChatSendButton", panelObject.transform, ChatSendLabel, out runtimeChatSendButton);
        RectTransform sendRect = sendButtonObject.GetComponent<RectTransform>();
        sendRect.anchorMin = new Vector2(1f, 0f);
        sendRect.anchorMax = new Vector2(1f, 0f);
        sendRect.pivot = new Vector2(1f, 0f);
        sendRect.sizeDelta = new Vector2(70f, 56f);
        sendRect.anchoredPosition = new Vector2(-10f, 10f);
        runtimeChatSendButton.onClick.AddListener(SubmitChatMessage);

        GameObject historyPanelObject = new GameObject("ChatHistoryPanel");
        historyPanelObject.transform.SetParent(runtimeMenuCanvas.transform, false);
        runtimeChatHistoryPanelRoot = historyPanelObject.AddComponent<RectTransform>();
        runtimeChatHistoryPanelRoot.anchorMin = new Vector2(0f, 1f);
        runtimeChatHistoryPanelRoot.anchorMax = new Vector2(0f, 1f);
        runtimeChatHistoryPanelRoot.pivot = new Vector2(0f, 1f);
        runtimeChatHistoryPanelBackground = historyPanelObject.AddComponent<Image>();
        runtimeChatHistoryPanelBackground.color = GetRuntimeMenuColor(menuPanelColor);

        runtimeChatHistoryHeader = CreateRuntimeMenuText("ChatHistoryHeader", historyPanelObject.transform, 15, FontStyle.Bold);
        RectTransform historyHeaderRect = runtimeChatHistoryHeader.rectTransform;
        historyHeaderRect.anchorMin = new Vector2(0f, 1f);
        historyHeaderRect.anchorMax = new Vector2(1f, 1f);
        historyHeaderRect.pivot = new Vector2(0.5f, 1f);
        historyHeaderRect.anchoredPosition = new Vector2(0f, -10f);
        historyHeaderRect.sizeDelta = new Vector2(-88f, 28f);
        runtimeChatHistoryHeader.alignment = TextAnchor.MiddleLeft;
        runtimeChatHistoryHeader.text = ChatHistoryHeaderLabel;
        runtimeChatHistoryHeader.color = new Color(0.96f, 0.97f, 0.99f, 1f);

        GameObject historyCloseButtonObject = CreateChatButtonObject("ChatHistoryCloseButton", historyPanelObject.transform, ChatCloseLabel, out runtimeChatHistoryCloseButton);
        RectTransform historyCloseRect = historyCloseButtonObject.GetComponent<RectTransform>();
        historyCloseRect.anchorMin = new Vector2(1f, 1f);
        historyCloseRect.anchorMax = new Vector2(1f, 1f);
        historyCloseRect.pivot = new Vector2(1f, 1f);
        historyCloseRect.sizeDelta = new Vector2(54f, 24f);
        historyCloseRect.anchoredPosition = new Vector2(-10f, -8f);
        runtimeChatHistoryCloseButton.onClick.AddListener(ToggleChatHistoryPanel);

        GameObject viewportObject = new GameObject("ChatHistoryViewport");
        viewportObject.transform.SetParent(historyPanelObject.transform, false);
        RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-10f, -42f);
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = GetRuntimeMenuColor(new Color(0.12f, 0.15f, 0.2f, 0.96f));
        viewportObject.AddComponent<RectMask2D>();

        runtimeChatHistoryText = CreateRuntimeMenuText("ChatHistory", viewportObject.transform, 13, FontStyle.Normal);
        RectTransform historyTextRect = runtimeChatHistoryText.rectTransform;
        historyTextRect.anchorMin = new Vector2(0f, 1f);
        historyTextRect.anchorMax = new Vector2(1f, 1f);
        historyTextRect.pivot = new Vector2(0.5f, 1f);
        historyTextRect.anchoredPosition = new Vector2(0f, -10f);
        historyTextRect.sizeDelta = new Vector2(-18f, 0f);
        ContentSizeFitter historyFitter = runtimeChatHistoryText.gameObject.AddComponent<ContentSizeFitter>();
        historyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        runtimeChatHistoryText.alignment = TextAnchor.UpperLeft;
        runtimeChatHistoryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        runtimeChatHistoryText.verticalOverflow = VerticalWrapMode.Overflow;
        runtimeChatHistoryText.supportRichText = false;
        runtimeChatHistoryText.color = new Color(0.94f, 0.96f, 0.98f, 1f);
        runtimeChatHistoryText.text = string.Empty;

        runtimeChatHistoryScrollRect = historyPanelObject.AddComponent<ScrollRect>();
        runtimeChatHistoryScrollRect.viewport = viewportRect;
        runtimeChatHistoryScrollRect.content = historyTextRect;
        runtimeChatHistoryScrollRect.horizontal = false;
        runtimeChatHistoryScrollRect.movementType = ScrollRect.MovementType.Clamped;
        runtimeChatHistoryScrollRect.scrollSensitivity = 22f;

        GameObject bubbleObject = new GameObject("ChatBubble");
        bubbleObject.transform.SetParent(runtimeMenuCanvas.transform, false);
        runtimeChatBubbleRoot = bubbleObject.AddComponent<RectTransform>();
        runtimeChatBubbleRoot.anchorMin = new Vector2(0f, 1f);
        runtimeChatBubbleRoot.anchorMax = new Vector2(0f, 1f);
        runtimeChatBubbleRoot.pivot = new Vector2(0f, 1f);
        runtimeChatBubbleRoot.sizeDelta = new Vector2(chatBubbleMaxSize.x, chatBubbleMaxSize.y);

        GameObject bubbleBodyObject = new GameObject("BubbleBody");
        bubbleBodyObject.transform.SetParent(bubbleObject.transform, false);
        runtimeChatBubbleBodyRoot = bubbleBodyObject.AddComponent<RectTransform>();
        runtimeChatBubbleBodyRoot.anchorMin = new Vector2(0f, 1f);
        runtimeChatBubbleBodyRoot.anchorMax = new Vector2(0f, 1f);
        runtimeChatBubbleBodyRoot.pivot = new Vector2(0f, 1f);
        runtimeChatBubbleBodyRoot.anchoredPosition = Vector2.zero;
        runtimeChatBubbleBodyRoot.sizeDelta = new Vector2(chatBubbleMaxSize.x, chatBubbleMaxSize.y);
        runtimeChatBubbleBackground = bubbleBodyObject.AddComponent<Image>();
        runtimeChatBubbleBackground.color = GetRuntimeMenuColor(new Color(0.98f, 0.99f, 1f, 1f));
        runtimeChatBubbleBackground.raycastTarget = false;
        runtimeChatBubbleOutline = bubbleBodyObject.AddComponent<Outline>();
        ConfigureChatBubbleOutline(runtimeChatBubbleOutline);

        GameObject bubbleTailObject = new GameObject("BubbleTail");
        bubbleTailObject.transform.SetParent(bubbleObject.transform, false);
        runtimeChatBubbleTailRoot = bubbleTailObject.AddComponent<RectTransform>();
        runtimeChatBubbleTailRoot.anchorMin = new Vector2(0.5f, 1f);
        runtimeChatBubbleTailRoot.anchorMax = new Vector2(0.5f, 1f);
        runtimeChatBubbleTailRoot.pivot = new Vector2(0.5f, 1f);
        runtimeChatBubbleTailRoot.anchoredPosition = new Vector2(0f, 0f);
        runtimeChatBubbleTailRoot.sizeDelta = new Vector2(24f, 18f);
        runtimeChatBubbleTailBackground = bubbleTailObject.AddComponent<Image>();
        runtimeChatBubbleTailBackground.color = GetRuntimeMenuColor(new Color(0.98f, 0.99f, 1f, 1f));
        runtimeChatBubbleTailBackground.raycastTarget = false;
        bubbleTailObject.SetActive(false);

        runtimeChatBubbleText = CreateRuntimeMenuText("BubbleText", bubbleBodyObject.transform, 13, FontStyle.Bold);
        RectTransform bubbleTextRect = runtimeChatBubbleText.rectTransform;
        bubbleTextRect.anchorMin = Vector2.zero;
        bubbleTextRect.anchorMax = Vector2.one;
        bubbleTextRect.offsetMin = new Vector2(28f, 34f);
        bubbleTextRect.offsetMax = new Vector2(-28f, -94f);
        runtimeChatBubbleText.alignment = TextAnchor.UpperCenter;
        runtimeChatBubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        runtimeChatBubbleText.verticalOverflow = VerticalWrapMode.Overflow;
        runtimeChatBubbleText.supportRichText = false;
        runtimeChatBubbleText.color = new Color(0.16f, 0.19f, 0.23f, 1f);
        runtimeChatBubbleText.text = string.Empty;
        EnsureBubbleSprites();

        runtimeChatPanelRoot.gameObject.SetActive(false);
        runtimeChatHistoryPanelRoot.gameObject.SetActive(false);
        runtimeChatBubbleRoot.gameObject.SetActive(false);
    }

    private void EnsureRuntimeLoadingOverlay()
    {
        EnsureRuntimeMenu();
        TryBindExistingSceneUi();

        if (runtimeLoadingRoot != null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("StartupLoadingOverlay");
        overlayObject.transform.SetParent(runtimeMenuCanvas.transform, false);
        runtimeLoadingRoot = overlayObject.AddComponent<RectTransform>();
        runtimeLoadingRoot.anchorMin = Vector2.zero;
        runtimeLoadingRoot.anchorMax = Vector2.one;
        runtimeLoadingRoot.offsetMin = Vector2.zero;
        runtimeLoadingRoot.offsetMax = Vector2.zero;
        runtimeLoadingRoot.SetAsLastSibling();

        runtimeLoadingBackground = overlayObject.AddComponent<Image>();
        runtimeLoadingBackground.color = GetRuntimeMenuColor(new Color(0.08f, 0.1f, 0.14f, 1f));
        runtimeLoadingBackground.raycastTarget = true;

        runtimeLoadingText = CreateRuntimeMenuText("LoadingText", overlayObject.transform, 22, FontStyle.Bold);
        RectTransform loadingTextRect = runtimeLoadingText.rectTransform;
        loadingTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        loadingTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        loadingTextRect.pivot = new Vector2(0.5f, 0.5f);
        loadingTextRect.sizeDelta = new Vector2(320f, 48f);
        loadingTextRect.anchoredPosition = Vector2.zero;
        runtimeLoadingText.alignment = TextAnchor.MiddleCenter;
        runtimeLoadingText.color = new Color(0.98f, 0.99f, 1f, 1f);
        runtimeLoadingText.text = StartupLoadingLabel;

        runtimeLoadingRoot.gameObject.SetActive(false);
    }

    private void ShowStartupLoadingOverlay()
    {
        EnsureRuntimeLoadingOverlay();
        startupLoadingVisible = true;

        if (runtimeLoadingRoot != null)
        {
            runtimeLoadingRoot.gameObject.SetActive(true);
            runtimeLoadingRoot.SetAsLastSibling();
        }

        RefreshRuntimeCanvasVisibility();
    }

    private void HideStartupLoadingOverlay()
    {
        startupLoadingVisible = false;

        if (runtimeLoadingRoot != null)
        {
            runtimeLoadingRoot.gameObject.SetActive(false);
        }

        RefreshRuntimeCanvasVisibility();
    }

    private void RebuildRuntimeMenuItems()
    {
        EnsureRuntimeMenu();
        TryBindExistingSceneUi();

        ClearMenuItemBindings();
        HideUnusedMenuItems();
        runtimeMenuItemBackgrounds.Clear();
        runtimeChatMenuBackground = null;
        runtimeExitBackground = null;

        for (int i = 0; i < availableMenuStates.Count; i++)
        {
            Image itemBackground = CreateOrUpdateRuntimeMenuItem("State_" + i, availableMenuStates[i], i);
            runtimeMenuItemBackgrounds.Add(itemBackground);
        }

        runtimeChatMenuBackground = CreateOrUpdateRuntimeMenuItem("Chat", ChatMenuLabel, GetChatMenuIndex());
        runtimeExitBackground = CreateOrUpdateRuntimeMenuItem("Exit", ExitMenuLabel, GetExitMenuIndex());
        Text exitLabel = runtimeExitBackground.GetComponentInChildren<Text>();
        if (exitLabel != null)
        {
            exitLabel.color = new Color(1f, 0.94f, 0.94f, 1f);
        }
    }

    private Image CreateOrUpdateRuntimeMenuItem(string objectName, string label, int index)
    {
        Transform parent = runtimeMenuItemsRoot != null ? runtimeMenuItemsRoot : runtimeMenuRoot;
        if (parent == null)
        {
            return null;
        }

        Transform existingTransform = parent.Find(objectName);
        GameObject itemObject;
        RectTransform itemRect;
        Image background;
        Text itemLabel;
        if (existingTransform != null)
        {
            itemObject = existingTransform.gameObject;
            itemRect = existingTransform as RectTransform;
            background = itemObject.GetComponent<Image>();
            itemLabel = itemObject.GetComponentInChildren<Text>(true);
        }
        else
        {
            itemObject = new GameObject(objectName);
            itemObject.transform.SetParent(parent, false);
            itemRect = itemObject.AddComponent<RectTransform>();
            background = itemObject.AddComponent<Image>();
            itemLabel = CreateRuntimeMenuText("Label", itemObject.transform, 13, FontStyle.Normal);
        }

        Rect localRect = GetMenuItemRect(index);
        if (index == GetChatMenuIndex())
        {
            localRect = GetChatItemRect();
        }
        else if (index == GetExitMenuIndex())
        {
            localRect = GetExitItemRect();
        }
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(0f, 1f);
        itemRect.pivot = new Vector2(0f, 1f);
        itemRect.sizeDelta = new Vector2(localRect.width, localRect.height);
        itemRect.anchoredPosition = new Vector2(localRect.x - menuRect.x, -(localRect.y - menuRect.y));
        background.color = GetRuntimeMenuColor(menuItemColor);
        RectTransform labelRect = itemLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);
        itemLabel.alignment = TextAnchor.MiddleLeft;
        itemLabel.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        itemLabel.text = label;
        itemObject.SetActive(true);

        return background;
    }

    private void ClearMenuItemBindings()
    {
        runtimeMenuItemBackgrounds.Clear();
        runtimeChatMenuBackground = null;
        runtimeExitBackground = null;
    }

    private void HideUnusedMenuItems()
    {
        Transform parent = runtimeMenuItemsRoot != null ? runtimeMenuItemsRoot : runtimeMenuRoot;
        if (parent == null)
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private Text CreateRuntimeMenuText(string objectName, Transform parent, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = runtimeFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.raycastTarget = false;
        return text;
    }

    private GameObject CreateChatButtonObject(string objectName, Transform parent, string label, out Button button)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);
        buttonObject.AddComponent<RectTransform>();
        Image buttonBackground = buttonObject.AddComponent<Image>();
        buttonBackground.color = GetRuntimeMenuColor(menuHoverColor);
        button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonBackground;

        Text buttonLabel = CreateRuntimeMenuText("Label", buttonObject.transform, 12, FontStyle.Bold);
        RectTransform labelRect = buttonLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(6f, 4f);
        labelRect.offsetMax = new Vector2(-6f, -4f);
        buttonLabel.alignment = TextAnchor.MiddleCenter;
        buttonLabel.color = new Color(0.98f, 0.99f, 1f, 1f);
        buttonLabel.text = label;

        return buttonObject;
    }

    private void EnsureBubbleSprites()
    {
        if (runtimeChatBubbleBodySprite == null)
        {
            runtimeChatBubbleBodySprite = chatBubbleBodySpriteOverride != null
                ? CreateColorKeySafeSprite(chatBubbleBodySpriteOverride)
                : CreateEllipseSprite(128);
        }

        if (runtimeChatBubbleTailSprite == null)
        {
            runtimeChatBubbleTailSprite = CreateTriangleSprite(64, 40);
        }

        if (runtimeChatBubbleBackground != null)
        {
            runtimeChatBubbleBackground.sprite = runtimeChatBubbleBodySprite;
            runtimeChatBubbleBackground.type = Image.Type.Simple;
            runtimeChatBubbleBackground.preserveAspect = false;
        }

        if (runtimeChatBubbleOutline != null)
        {
            ConfigureChatBubbleOutline(runtimeChatBubbleOutline);
        }
    }

    private void ConfigureChatBubbleOutline(Outline outline)
    {
        if (outline == null)
        {
            return;
        }

        outline.effectColor = new Color(0f, 0f, 0f, 0.96f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);
        outline.useGraphicAlpha = false;
    }

    private void TryBindExistingSceneUi()
    {
        if (runtimeMenuCanvas == null)
        {
            return;
        }

        if (runtimeMenuRoot == null)
        {
            Transform menuRoot = runtimeMenuCanvas.transform.Find("MenuRoot");
            if (menuRoot != null)
            {
                runtimeMenuRoot = menuRoot as RectTransform;
                runtimeMenuHeader = menuRoot.Find("Header")?.GetComponent<Text>();
                runtimeMenuItemsRoot = menuRoot.Find("Items") as RectTransform;
            }
        }

        if (runtimeChatPanelRoot == null)
        {
            Transform chatPanel = runtimeMenuCanvas.transform.Find("ChatPanel");
            if (chatPanel != null)
            {
                runtimeChatPanelRoot = chatPanel as RectTransform;
                runtimeChatPanelBackground = chatPanel.GetComponent<Image>();
                runtimeChatHeader = chatPanel.Find("ChatHeader")?.GetComponent<Text>();
                runtimeChatHistoryButton = chatPanel.Find("ChatHistoryButton")?.GetComponent<Button>();
                runtimeChatCloseButton = chatPanel.Find("ChatCloseButton")?.GetComponent<Button>();

                Transform inputBackground = chatPanel.Find("ChatInputBackground");
                if (inputBackground != null)
                {
                    runtimeChatInputField = inputBackground.GetComponent<InputField>();
                    ConfigureRuntimeChatInputField();
                }

                runtimeChatSendButton = chatPanel.Find("ChatSendButton")?.GetComponent<Button>();
            }
        }

        if (runtimeChatHistoryPanelRoot == null)
        {
            Transform historyPanel = runtimeMenuCanvas.transform.Find("ChatHistoryPanel");
            if (historyPanel != null)
            {
                runtimeChatHistoryPanelRoot = historyPanel as RectTransform;
                runtimeChatHistoryPanelBackground = historyPanel.GetComponent<Image>();
                runtimeChatHistoryHeader = historyPanel.Find("ChatHistoryHeader")?.GetComponent<Text>();
                runtimeChatHistoryCloseButton = historyPanel.Find("ChatHistoryCloseButton")?.GetComponent<Button>();
                runtimeChatHistoryScrollRect = historyPanel.GetComponent<ScrollRect>();
                runtimeChatHistoryText = historyPanel.Find("ChatHistoryViewport/ChatHistory")?.GetComponent<Text>();
            }
        }

        if (runtimeChatBubbleRoot == null)
        {
            Transform bubbleRoot = runtimeMenuCanvas.transform.Find("ChatBubble");
            if (bubbleRoot != null)
            {
                runtimeChatBubbleRoot = bubbleRoot as RectTransform;
                runtimeChatBubbleBodyRoot = bubbleRoot.Find("BubbleBody") as RectTransform;
                runtimeChatBubbleTailRoot = bubbleRoot.Find("BubbleTail") as RectTransform;
                runtimeChatBubbleBackground = bubbleRoot.Find("BubbleBody")?.GetComponent<Image>();
                runtimeChatBubbleOutline = bubbleRoot.Find("BubbleBody")?.GetComponent<Outline>();
                runtimeChatBubbleTailBackground = bubbleRoot.Find("BubbleTail")?.GetComponent<Image>();
                runtimeChatBubbleText = bubbleRoot.Find("BubbleBody/BubbleText")?.GetComponent<Text>();

                if (runtimeChatBubbleBackground != null && runtimeChatBubbleOutline == null)
                {
                    runtimeChatBubbleOutline = runtimeChatBubbleBackground.gameObject.AddComponent<Outline>();
                }

                ConfigureChatBubbleOutline(runtimeChatBubbleOutline);
            }
        }

        if (runtimeLoadingRoot == null)
        {
            Transform loadingRoot = runtimeMenuCanvas.transform.Find("StartupLoadingOverlay");
            if (loadingRoot != null)
            {
                runtimeLoadingRoot = loadingRoot as RectTransform;
                runtimeLoadingBackground = loadingRoot.GetComponent<Image>();
                runtimeLoadingText = loadingRoot.Find("LoadingText")?.GetComponent<Text>();
            }
        }
    }

    private void RebindChatButtonListeners()
    {
        if (runtimeChatHistoryButton != null)
        {
            runtimeChatHistoryButton.onClick.RemoveAllListeners();
            runtimeChatHistoryButton.onClick.AddListener(ToggleChatHistoryPanel);
        }

        if (runtimeChatCloseButton != null)
        {
            runtimeChatCloseButton.onClick.RemoveAllListeners();
            runtimeChatCloseButton.onClick.AddListener(CloseChatPanel);
        }

        if (runtimeChatSendButton != null)
        {
            runtimeChatSendButton.onClick.RemoveAllListeners();
            runtimeChatSendButton.onClick.AddListener(SubmitChatMessage);
        }

        if (runtimeChatHistoryCloseButton != null)
        {
            runtimeChatHistoryCloseButton.onClick.RemoveAllListeners();
            runtimeChatHistoryCloseButton.onClick.AddListener(ToggleChatHistoryPanel);
        }
    }

    private void DestroyUiObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
            return;
        }

        DestroyImmediate(target);
    }

    private Sprite CreateColorKeySafeSprite(Sprite sourceSprite)
    {
        if (sourceSprite == null)
        {
            return null;
        }

        Texture2D sourceTexture = sourceSprite.texture;
        if (sourceTexture == null)
        {
            return sourceSprite;
        }

        Rect rect = sourceSprite.rect;
        int width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
        int height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
        Texture2D readableTexture = CreateReadableTexture(sourceTexture, width, height);
        if (readableTexture == null)
        {
            return sourceSprite;
        }

        Color32[] sourcePixels = readableTexture.GetPixels32();
        Color32[] processedPixels = new Color32[sourcePixels.Length];
        Color32 opaqueFill = new Color32(255, 255, 255, 255);

        for (int i = 0; i < sourcePixels.Length; i++)
        {
            processedPixels[i] = sourcePixels[i].a >= BubbleAlphaCutoff
                ? opaqueFill
                : new Color32(255, 255, 255, 0);
        }

        Texture2D processedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        processedTexture.SetPixels32(processedPixels);
        processedTexture.Apply(false, false);

        Destroy(readableTexture);

        runtimeChatBubbleProcessedTexture = processedTexture;
        return Sprite.Create(processedTexture, new Rect(0f, 0f, width, height), sourceSprite.pivot / rect.size, sourceSprite.pixelsPerUnit);
    }

    private static Texture2D CreateReadableTexture(Texture2D sourceTexture, int width, int height)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;

        try
        {
            Graphics.Blit(sourceTexture, renderTexture);
            RenderTexture.active = renderTexture;
            Texture2D readableTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            readableTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            readableTexture.Apply(false, false);
            return readableTexture;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    private static Sprite CreateEllipseSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - radius) / radius;
                float dy = (y + 0.5f - radius) / radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                bool inside = distance <= 1f;
                pixels[y * size + x] = inside
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateTriangleSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[width * height];
        float centerX = (width - 1) * 0.5f;

        for (int y = 0; y < height; y++)
        {
            float normalizedY = (float)y / Mathf.Max(1f, height - 1f);
            float halfWidth = normalizedY * centerX;

            for (int x = 0; x < width; x++)
            {
                float distanceFromCenter = Mathf.Abs(x - centerX);
                bool inside = distanceFromCenter <= halfWidth;
                pixels[y * width + x] = inside
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), width);
    }

    private void UpdateRuntimeChatVisuals()
    {
        if (runtimeMenuCanvas == null)
        {
            return;
        }

        if (chatPanelOpen && runtimeChatPanelRoot != null)
        {
            if (!draggingChatPanel && !chatPanelManuallyPositioned)
            {
                chatPanelRect = BuildChatPanelRect();
            }

            runtimeChatPanelRoot.sizeDelta = chatPanelRect.size;
            runtimeChatPanelRoot.anchoredPosition = new Vector2(chatPanelRect.x, -chatPanelRect.y);

            if (runtimeChatPanelBackground != null)
            {
                runtimeChatPanelBackground.color = GetRuntimeMenuColor(menuPanelColor);
            }

            Image historyButtonImage = runtimeChatHistoryButton != null
                ? runtimeChatHistoryButton.targetGraphic as Image
                : null;
            if (historyButtonImage != null)
            {
                historyButtonImage.color = GetRuntimeMenuColor(chatHistoryOpen ? menuActiveColor : menuHoverColor);
            }
        }

        if (chatHistoryOpen && runtimeChatHistoryPanelRoot != null)
        {
            if (!draggingHistoryPanel && !historyPanelManuallyPositioned)
            {
                historyPanelRect = BuildHistoryPanelRect();
            }

            runtimeChatHistoryPanelRoot.sizeDelta = historyPanelRect.size;
            runtimeChatHistoryPanelRoot.anchoredPosition = new Vector2(historyPanelRect.x, -historyPanelRect.y);

            if (runtimeChatHistoryPanelBackground != null)
            {
                runtimeChatHistoryPanelBackground.color = GetRuntimeMenuColor(menuPanelColor);
            }

            if (runtimeChatHistoryText != null)
            {
                if (chatHistoryDirty)
                {
                    cachedChatHistoryText = chatHistory.Count > 0
                        ? string.Join("\n\n", chatHistory.ToArray())
                        : "\u8fd8\u6ca1\u6709\u5bf9\u8bdd\u8bb0\u5f55\u3002";
                    chatHistoryDirty = false;
                }

                runtimeChatHistoryText.text = cachedChatHistoryText;
            }

            if (scrollHistoryToBottomPending && runtimeChatHistoryScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                runtimeChatHistoryScrollRect.verticalNormalizedPosition = 0f;
                scrollHistoryToBottomPending = false;
            }
        }

        if (chatBubbleVisible && runtimeChatBubbleRoot != null)
        {
            EnsureBubbleSprites();
            chatBubbleRect = BuildChatBubbleRect();
            runtimeChatBubbleRoot.sizeDelta = chatBubbleRect.size;
            runtimeChatBubbleRoot.anchoredPosition = new Vector2(chatBubbleRect.x, -chatBubbleRect.y);

            if (runtimeChatBubbleBodyRoot != null)
            {
                runtimeChatBubbleBodyRoot.sizeDelta = new Vector2(chatBubbleRect.width, chatBubbleRect.height);
            }

            if (runtimeChatBubbleTailRoot != null)
            {
                runtimeChatBubbleTailRoot.gameObject.SetActive(false);
            }

            if (runtimeChatBubbleText != null)
            {
                RectTransform bubbleTextRect = runtimeChatBubbleText.rectTransform;
                float sideInset = chatBubbleRect.width * BubbleSideInsetRatio;
                float topInset = chatBubbleRect.height * BubbleTopInsetRatio;
                float bottomInset = chatBubbleRect.height * BubbleBottomInsetRatio;
                bubbleTextRect.offsetMin = new Vector2(sideInset, bottomInset);
                bubbleTextRect.offsetMax = new Vector2(-sideInset, -topInset);
            }

            if (runtimeChatBubbleBackground != null)
            {
                runtimeChatBubbleBackground.color = GetRuntimeMenuColor(new Color(0.98f, 0.99f, 1f, 1f));
            }
        }
    }

    private void SubmitChatMessage()
    {
        if (runtimeChatInputField == null)
        {
            return;
        }

        if (chatRequestInFlight)
        {
            runtimeChatInputField.Select();
            runtimeChatInputField.ActivateInputField();
            return;
        }

        string userMessage = runtimeChatInputField.text.Trim();
        if (string.IsNullOrEmpty(userMessage))
        {
            runtimeChatInputField.Select();
            runtimeChatInputField.ActivateInputField();
            return;
        }

        AppendChatLine("\u6211", userMessage);
        runtimeChatInputField.text = string.Empty;
        UpdateRuntimeChatVisuals();
        ExpandWindowRegionForChatUI();

        if (llmClient != null && llmClient.已完成接口配置())
        {
            chatRequestInFlight = true;
            if (runtimeChatInputField != null)
            {
                runtimeChatInputField.interactable = false;
            }

            if (runtimeChatSendButton != null)
            {
                runtimeChatSendButton.interactable = false;
            }

            ShowPetChatBubble("\u601d\u8003\u4e2d...", null, false);
            llmClient.请求聊天回复(
                userMessage,
                HandleLargeModelReplySuccess,
                HandleLargeModelReplyError,
                false);
        }
        else
        {
            string reply = GetLocalChatReply(userMessage);
            PresentAssistantReply(reply);
        }

        runtimeChatInputField.Select();
        runtimeChatInputField.ActivateInputField();
    }

    private void HandleLargeModelReplySuccess(string reply)
    {
        chatRequestInFlight = false;
        SetChatRequestControlsInteractable(true);

        string finalReply = string.IsNullOrWhiteSpace(reply)
            ? "\u6211\u8FD9\u6B21\u6CA1\u7EC4\u7EC7\u597D\u56DE\u7B54\uFF0C\u4F60\u518D\u8DDF\u6211\u8BF4\u4E00\u904D\u5427\u3002"
            : reply.Trim();
        PresentAssistantReply(finalReply);

        if (runtimeChatInputField != null)
        {
            runtimeChatInputField.Select();
            runtimeChatInputField.ActivateInputField();
        }
    }

    private void HandleLargeModelReplyError(string errorMessage)
    {
        chatRequestInFlight = false;
        SetChatRequestControlsInteractable(true);

        string finalReply = string.IsNullOrWhiteSpace(errorMessage)
            ? "\u6211\u73B0\u5728\u6709\u70B9\u8FDE\u4E0D\u4E0A\uFF0C\u7A0D\u540E\u6211\u4EEC\u518D\u8BD5\u8BD5\u3002"
            : errorMessage.Trim();

        PresentAssistantReply(finalReply);

        if (runtimeChatInputField != null)
        {
            runtimeChatInputField.Select();
            runtimeChatInputField.ActivateInputField();
        }
    }

    private void SetChatRequestControlsInteractable(bool interactable)
    {
        if (runtimeChatInputField != null)
        {
            runtimeChatInputField.interactable = interactable;
        }

        if (runtimeChatSendButton != null)
        {
            runtimeChatSendButton.interactable = interactable;
        }
    }

    private void PresentAssistantReply(string reply)
    {
        string finalReply = string.IsNullOrWhiteSpace(reply)
            ? "\u6211\u8FD9\u6B21\u6CA1\u7EC4\u7EC7\u597D\u56DE\u7B54\uFF0C\u4F60\u518D\u8DDF\u6211\u8BF4\u4E00\u904D\u5427\u3002"
            : reply.Trim();

        AppendChatLine("\u5c0f\u6d63\u718a", finalReply);
        UpdateRuntimeChatVisuals();
        ExpandWindowRegionForChatUI();

        if (gptSoVITSClient != null && gptSoVITSClient.已完成接口配置())
        {
            ShowPetChatBubble("思考中...", null, false);
            gptSoVITSClient.请求语音播放(
                finalReply,
                duration => HandleSpeechPlaybackReady(finalReply, duration),
                error => HandleSpeechPlaybackError(finalReply, error));
            return;
        }

        PresentAssistantReplyWithoutVoice(finalReply);
    }

    private void PresentAssistantReplyWithoutVoice(string reply)
    {
        float presentationDuration = GetChatPresentationDuration(reply);
        PlayRandomChatAnimation(reply, presentationDuration);
        ShowPetChatBubble(reply, presentationDuration);
        UpdateRuntimeChatVisuals();
        ExpandWindowRegionForChatUI();
    }

    private void HandleSpeechPlaybackReady(string reply, float clipDuration)
    {
        float presentationDuration = Mathf.Max(GetChatPresentationDuration(reply), Mathf.Max(0.8f, clipDuration));
        PlayRandomChatAnimation(reply, presentationDuration);
        ShowPetChatBubble(reply, presentationDuration);
        UpdateRuntimeChatVisuals();
        ExpandWindowRegionForChatUI();
    }

    private void HandleSpeechPlaybackError(string reply, string error)
    {
        Debug.LogWarning("GPT-SoVITS语音播放失败，已回退到纯文本展示。 " + error);
        PresentAssistantReplyWithoutVoice(reply);
    }

    private void AppendChatLine(string speaker, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        chatHistory.Add(speaker + "\uff1a" + message.Trim());
        scrollHistoryToBottomPending = true;
        chatHistoryDirty = true;
        const int maxLines = 120;
        while (chatHistory.Count > maxLines)
        {
            chatHistory.RemoveAt(0);
        }
    }

    private void ShowPetChatBubble(string message, float? displayDuration = null, bool autoHide = true)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        EnsureRuntimeChatPanel();
        if (chatBubbleRoutine != null)
        {
            StopCoroutine(chatBubbleRoutine);
            chatBubbleRoutine = null;
        }

        if (!autoHide)
        {
            ApplyChatBubblePage(message.Trim());
            return;
        }

        Sprite bubbleSprite = chatBubbleBodySpriteOverride != null ? chatBubbleBodySpriteOverride : runtimeChatBubbleBodySprite;
        List<string> bubblePages = SplitChatBubblePages(message.Trim(), bubbleSprite);
        if (bubblePages.Count == 0)
        {
            bubblePages.Add(message.Trim());
        }

        float sequenceDuration = displayDuration ?? GetChatPresentationDuration(message);
        chatBubbleRoutine = StartCoroutine(PlayChatBubbleSequence(bubblePages, sequenceDuration));
    }

    private void HidePetChatBubble(bool updateWindow = true)
    {
        if (chatBubbleRoutine != null)
        {
            StopCoroutine(chatBubbleRoutine);
            chatBubbleRoutine = null;
        }

        chatBubbleVisible = false;
        if (runtimeChatBubbleRoot != null)
        {
            runtimeChatBubbleRoot.gameObject.SetActive(false);
        }

        RefreshRuntimeCanvasVisibility();

        if (!updateWindow)
        {
            return;
        }

        if (chatPanelOpen || chatHistoryOpen)
        {
            ExpandWindowRegionForChatUI();
        }
        else if (autoFitWindowToPet && windowController != null && windowController.IsReady && !menuOpen)
        {
            CropWindowToCharacter();
        }
    }

    private IEnumerator HidePetChatBubbleAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.8f, delay));
        chatBubbleRoutine = null;
        HidePetChatBubble();
    }

    private IEnumerator PlayChatBubbleSequence(List<string> bubblePages, float totalDuration)
    {
        if (bubblePages == null || bubblePages.Count == 0)
        {
            chatBubbleRoutine = null;
            yield break;
        }

        if (bubblePages.Count == 1)
        {
            ApplyChatBubblePage(bubblePages[0]);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.8f, totalDuration));
            chatBubbleRoutine = null;
            HidePetChatBubble();
            yield break;
        }

        float safeTotalDuration = Mathf.Max(0.8f * bubblePages.Count, totalDuration);
        float perPageDuration = safeTotalDuration / bubblePages.Count;

        for (int i = 0; i < bubblePages.Count; i++)
        {
            ApplyChatBubblePage(bubblePages[i]);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.8f, perPageDuration));
        }

        chatBubbleRoutine = null;
        HidePetChatBubble();
    }

    private void ApplyChatBubblePage(string message)
    {
        currentChatBubbleMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        chatBubbleVisible = !string.IsNullOrEmpty(currentChatBubbleMessage);

        if (runtimeChatBubbleText != null)
        {
            runtimeChatBubbleText.text = currentChatBubbleMessage;
        }

        if (runtimeChatBubbleRoot != null)
        {
            runtimeChatBubbleRoot.gameObject.SetActive(chatBubbleVisible);
        }

        UpdateRuntimeChatVisuals();
        RefreshRuntimeCanvasVisibility();
        ExpandWindowRegionForChatUI();
    }

    private string GetLocalChatReply(string userMessage)
    {
        string normalizedMessage = userMessage.Trim();
        if (normalizedMessage.Contains("\u4f60\u597d") || normalizedMessage.Contains("hello") || normalizedMessage.Contains("hi"))
        {
            return "\u4f60\u597d\u5440\uff0c\u5f88\u5f00\u5fc3\u53c8\u542c\u5230\u4f60\u8bf4\u8bdd\u3002";
        }

        if (normalizedMessage.Contains("\u540d\u5b57"))
        {
            return "\u6211\u662f\u4f60\u7684\u684c\u5ba0\u5c0f\u6d63\u718a\uff0c\u968f\u65f6\u53ef\u4ee5\u966a\u4f60\u804a\u804a\u5929\u3002";
        }

        if (localChatReplies != null && localChatReplies.Length > 0)
        {
            string[] nonEmptyReplies = System.Array.FindAll(localChatReplies, reply => !string.IsNullOrWhiteSpace(reply));
            if (nonEmptyReplies.Length > 0)
            {
                return nonEmptyReplies[Random.Range(0, nonEmptyReplies.Length)];
            }
        }

        return "\u6211\u542c\u5230\u4e86\uff1a" + normalizedMessage;
    }

    private void PlayRandomChatAnimation(string message, float? durationOverride = null)
    {
        if (!TryPlayRandomAvailableState(chatAnimationStateNames))
        {
            return;
        }

        if (chatAnimationRoutine != null)
        {
            StopCoroutine(chatAnimationRoutine);
        }

        chatAnimationRoutine = StartCoroutine(RestoreInitialStateAfterDelay(durationOverride ?? GetChatPresentationDuration(message)));
    }

    private float GetChatPresentationDuration(string message)
    {
        Sprite bubbleSprite = chatBubbleBodySpriteOverride != null ? chatBubbleBodySpriteOverride : runtimeChatBubbleBodySprite;
        List<string> bubblePages = SplitChatBubblePages(message, bubbleSprite);
        if (bubblePages.Count <= 1)
        {
            return GetChatAnimationDuration(message);
        }

        float totalDuration = 0f;
        for (int i = 0; i < bubblePages.Count; i++)
        {
            totalDuration += GetChatAnimationDuration(bubblePages[i]);
        }

        return totalDuration;
    }

    private float GetChatAnimationDuration(string message)
    {
        string trimmedMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        int effectiveLength = trimmedMessage.Length;
        float minDuration = Mathf.Max(0.2f, 最短对话动画时长);
        float maxDuration = Mathf.Max(minDuration, 最长对话动画时长);
        float duration = 对话动画保持时长 + effectiveLength * 每字额外动画时长;
        return Mathf.Clamp(duration, minDuration, maxDuration);
    }

    private List<string> SplitChatBubblePages(string message, Sprite bubbleSprite)
    {
        List<string> pages = new List<string>();
        string remaining = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        if (string.IsNullOrEmpty(remaining) || runtimeChatBubbleText == null)
        {
            return pages;
        }

        while (!string.IsNullOrEmpty(remaining))
        {
            int takeLength = FindLargestBubblePageLength(remaining, bubbleSprite);
            takeLength = Mathf.Clamp(takeLength, 1, remaining.Length);

            string page = remaining.Substring(0, takeLength).Trim();
            if (string.IsNullOrEmpty(page))
            {
                page = remaining.Substring(0, 1);
                takeLength = 1;
            }

            pages.Add(page);
            remaining = remaining.Substring(takeLength).TrimStart();
        }

        return pages;
    }

    private int FindLargestBubblePageLength(string text, Sprite bubbleSprite)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        if (CanTextFitInBubble(text, bubbleSprite))
        {
            return text.Length;
        }

        int low = 1;
        int high = text.Length;
        int best = 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            string candidate = text.Substring(0, mid).TrimEnd();
            if (string.IsNullOrEmpty(candidate))
            {
                candidate = text.Substring(0, 1);
                mid = 1;
            }

            if (CanTextFitInBubble(candidate, bubbleSprite))
            {
                best = Mathf.Max(best, mid);
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return best;
    }

    private bool CanTextFitInBubble(string message, Sprite bubbleSprite)
    {
        if (string.IsNullOrWhiteSpace(message) || runtimeChatBubbleText == null)
        {
            return true;
        }

        float spriteAspect = bubbleSprite != null && bubbleSprite.rect.height > 0.01f
            ? bubbleSprite.rect.width / bubbleSprite.rect.height
            : 1.8f;
        spriteAspect = Mathf.Max(0.1f, spriteAspect);

        float minBubbleWidth = 132f;
        float minBubbleHeight = 100f;
        float maxBubbleWidth = Mathf.Max(minBubbleWidth, windowController != null ? windowController.WindowSize.x - 16f : chatBubbleMaxSize.x);
        float configuredMaxWidth = Mathf.Max(minBubbleWidth, chatBubbleMaxSize.x);
        float configuredMaxHeight = Mathf.Max(minBubbleHeight, chatBubbleMaxSize.y);
        maxBubbleWidth = Mathf.Min(maxBubbleWidth, configuredMaxWidth, configuredMaxHeight * spriteAspect);

        float bubbleWidth = maxBubbleWidth;
        float bubbleHeight = bubbleWidth / spriteAspect;
        float textWidth = bubbleWidth * (1f - BubbleSideInsetRatio * 2f);
        float textHeight = bubbleHeight * (1f - BubbleTopInsetRatio - BubbleBottomInsetRatio);

        TextGenerationSettings settings = runtimeChatBubbleText.GetGenerationSettings(new Vector2(textWidth, 10000f));
        settings.horizontalOverflow = HorizontalWrapMode.Wrap;
        settings.verticalOverflow = VerticalWrapMode.Overflow;
        settings.generateOutOfBounds = true;

        TextGenerator generator = new TextGenerator();
        float preferredHeight = generator.GetPreferredHeight(message.Trim(), settings);
        return preferredHeight <= textHeight;
    }

    private IEnumerator RestoreInitialStateAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, delay));
        PlayInitialState();
        chatAnimationRoutine = null;
    }

    private void UpdateRuntimeMenuVisuals()
    {
        if (!menuOpen || runtimeMenuRoot == null)
        {
            return;
        }

        runtimeMenuRoot.sizeDelta = new Vector2(menuRect.width, menuRect.height);
        runtimeMenuRoot.anchoredPosition = new Vector2(menuRect.x, -menuRect.y);

        for (int i = 0; i < runtimeMenuItemBackgrounds.Count; i++)
        {
            if (runtimeMenuItemBackgrounds[i] == null)
            {
                continue;
            }

            Color backgroundColor = menuItemColor;
            if (i < availableMenuStates.Count && availableMenuStates[i] == currentStateName)
            {
                backgroundColor = menuActiveColor;
            }
            else if (i == hoveredMenuIndex)
            {
                backgroundColor = menuHoverColor;
            }

            runtimeMenuItemBackgrounds[i].color = GetRuntimeMenuColor(backgroundColor);
        }

        if (runtimeChatMenuBackground != null)
        {
            Color chatColor = hoveredMenuIndex == GetChatMenuIndex()
                ? menuHoverColor
                : menuItemColor;
            runtimeChatMenuBackground.color = GetRuntimeMenuColor(chatColor);
        }

        if (runtimeExitBackground != null)
        {
            Color exitColor = hoveredMenuIndex == GetExitMenuIndex()
                ? menuHoverColor
                : new Color(0.45f, 0.2f, 0.2f, 1f);
            runtimeExitBackground.color = GetRuntimeMenuColor(exitColor);
        }
    }

    private void RefreshRuntimeCanvasVisibility()
    {
        if (runtimeMenuCanvas == null)
        {
            return;
        }

        bool shouldShow = startupLoadingVisible || menuOpen || chatPanelOpen || chatHistoryOpen || chatBubbleVisible;
        runtimeMenuCanvas.gameObject.SetActive(shouldShow);
    }

    private Color GetRuntimeMenuColor(Color source)
    {
        if (windowController != null && windowController.useColorKeyTransparency)
        {
            source.a = 1f;
        }

        return source;
    }

    private void CleanupLegacyPreviewUI()
    {
        DestroyNamedObjectIfPresent("AnimationCanvas");
        DestroyNamedObjectIfPresent("DesktopPetEventSystem");
    }

    private static void DestroyNamedObjectIfPresent(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            return;
        }

        Destroy(target);
    }

    private void RequestQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnDestroy()
    {
        if (regionMaskRoutine != null)
        {
            StopCoroutine(regionMaskRoutine);
            regionMaskRoutine = null;
        }

        if (chatAnimationRoutine != null)
        {
            StopCoroutine(chatAnimationRoutine);
            chatAnimationRoutine = null;
        }

        if (chatBubbleRoutine != null)
        {
            StopCoroutine(chatBubbleRoutine);
            chatBubbleRoutine = null;
        }

        if (regionMaskTexture != null)
        {
            Destroy(regionMaskTexture);
            regionMaskTexture = null;
        }

        if (runtimeMenuCanvas != null)
        {
            if (ownsSceneUiCanvas)
            {
                Destroy(runtimeMenuCanvas.gameObject);
            }

            runtimeMenuCanvas = null;
            sceneUiCanvas = null;
            ownsSceneUiCanvas = false;
        }

        if (runtimeEventSystemObject != null)
        {
            if (ownsRuntimeEventSystem)
            {
                Destroy(runtimeEventSystemObject);
            }

            runtimeEventSystemObject = null;
            ownsRuntimeEventSystem = false;
        }

        if (runtimeChatBubbleBodySprite != null)
        {
            if (runtimeChatBubbleProcessedTexture != null)
            {
                Destroy(runtimeChatBubbleProcessedTexture);
                runtimeChatBubbleProcessedTexture = null;
            }

            if (chatBubbleBodySpriteOverride == null && runtimeChatBubbleBodySprite.texture != null)
            {
                Destroy(runtimeChatBubbleBodySprite.texture);
            }

            if (chatBubbleBodySpriteOverride == null)
            {
                Destroy(runtimeChatBubbleBodySprite);
            }

            runtimeChatBubbleBodySprite = null;
        }

        if (runtimeChatBubbleTailSprite != null)
        {
            if (runtimeChatBubbleTailSprite.texture != null)
            {
                Destroy(runtimeChatBubbleTailSprite.texture);
            }

            Destroy(runtimeChatBubbleTailSprite);
            runtimeChatBubbleTailSprite = null;
        }
    }
}

