using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class 桌宠控制器 : MonoBehaviour
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
    [SerializeField, InspectorName("界面控制器")]
    private 桌宠界面控制器 界面控制器引用;
    [SerializeField, InspectorName("界面预制体")]
    private 桌宠界面控制器 界面预制体;
    [SerializeField, FormerlySerializedAs("llmClient"), InspectorName("大模型客户端")]
    private 大模型客户端 大模型客户端引用;
    [SerializeField, InspectorName("GPT-SoVITS客户端")]
    private GPTSoVITS客户端 GPTSoVITS客户端引用;
    [SerializeField, InspectorName("Gemini语音输入客户端")]
    private Gemini语音输入客户端 Gemini语音输入客户端引用;

    public Animator characterAnimator { get => 角色动画器; set => 角色动画器 = value; }
    public Transform visualRoot { get => 视觉根节点; set => 视觉根节点 = value; }
    public 透明窗口 windowController { get => 窗口控制器; set => 窗口控制器 = value; }
    public Camera petCamera { get => 桌宠相机; set => 桌宠相机 = value; }
    public Canvas sceneUiCanvas { get => 场景UI画布; set => 场景UI画布 = value; }
    public 桌宠界面控制器 uiController { get => 界面控制器引用; set => 界面控制器引用 = value; }
    public 大模型客户端 llmClient { get => 大模型客户端引用; set => 大模型客户端引用 = value; }
    public GPTSoVITS客户端 gptSoVITSClient { get => GPTSoVITS客户端引用; set => GPTSoVITS客户端引用 = value; }
    public Gemini语音输入客户端 geminiVoiceInputClient { get => Gemini语音输入客户端引用; set => Gemini语音输入客户端引用 = value; }

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
    private Vector2 对话框尺寸 = new Vector2(420f, 104f);
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

    private Texture2D regionMaskTexture;
    private Coroutine regionMaskRoutine;
    private WaitForEndOfFrame endOfFrameYield;
    private float nextMaskUpdateTime;
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

        if (界面控制器引用 == null)
        {
            if (sceneUiCanvas != null)
            {
                界面控制器引用 = sceneUiCanvas.GetComponent<桌宠界面控制器>();
            }

            if (界面控制器引用 == null)
            {
                界面控制器引用 = GetComponentInChildren<桌宠界面控制器>(true);
            }
        }

        if (界面控制器引用 == null && sceneUiCanvas != null)
        {
            界面控制器引用 = sceneUiCanvas.gameObject.AddComponent<桌宠界面控制器>();
        }

        if (界面控制器引用 != null)
        {
            界面控制器引用.自动绑定();
            if (sceneUiCanvas == null)
            {
                sceneUiCanvas = 界面控制器引用.界面Canvas;
            }
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

        if (Gemini语音输入客户端引用 == null)
        {
            Gemini语音输入客户端引用 = GetComponent<Gemini语音输入客户端>();
        }

        if (Gemini语音输入客户端引用 == null)
        {
            Gemini语音输入客户端引用 = gameObject.AddComponent<Gemini语音输入客户端>();
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

        NativeArray<Color32> pixels = regionMaskTexture.GetPixelData<Color32>(0);
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

    private List<RectInt> BuildMaskRegions(NativeArray<Color32> pixels, int width, int height)
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


    private void SubmitChatMessage()
    {
        SubmitChatMessageCore(runtimeChatInputField != null ? runtimeChatInputField.text : string.Empty, true);
    }

    public void 提交外部聊天消息(string message)
    {
        SubmitChatMessageCore(message, false);
    }

    private void SubmitChatMessageCore(string rawMessage, bool clearInputField)
    {
        if (chatRequestInFlight)
        {
            if (runtimeChatInputField != null)
            {
                runtimeChatInputField.Select();
                runtimeChatInputField.ActivateInputField();
            }

            return;
        }

        string userMessage = string.IsNullOrWhiteSpace(rawMessage) ? string.Empty : rawMessage.Trim();
        if (string.IsNullOrEmpty(userMessage))
        {
            if (runtimeChatInputField != null)
            {
                runtimeChatInputField.Select();
                runtimeChatInputField.ActivateInputField();
            }

            return;
        }

        AppendChatLine("\u6211", userMessage);
        if (clearInputField && runtimeChatInputField != null)
        {
            runtimeChatInputField.text = string.Empty;
        }

        UpdateRuntimeChatVisuals();
        ExpandWindowRegionForChatUI();

        if (llmClient != null && llmClient.已完成接口配置())
        {
            chatRequestInFlight = true;
            RefreshChatInputControlsState();

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

        if (runtimeChatInputField != null)
        {
            runtimeChatInputField.Select();
            runtimeChatInputField.ActivateInputField();
        }
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
        RefreshChatInputControlsState();
    }

    private void ToggleVoiceInput()
    {
        ResolveReferences();
        if (Gemini语音输入客户端引用 == null)
        {
            Debug.LogWarning("Gemini语音输入客户端不可用。");
            return;
        }

        if (Gemini语音输入客户端引用.正在录音)
        {
            ShowPetChatBubble(ChatVoiceProcessingHint, null, false);
            Gemini语音输入客户端引用.停止录音并转写(HandleVoiceTranscriptionSuccess, HandleVoiceTranscriptionError);
            RefreshChatInputControlsState();
            return;
        }

        if (chatRequestInFlight || Gemini语音输入客户端引用.正在转写)
        {
            return;
        }

        if (gptSoVITSClient != null)
        {
            AudioSource ttsAudioSource = gptSoVITSClient.GetComponent<AudioSource>();
            if (ttsAudioSource != null && ttsAudioSource.isPlaying)
            {
                ttsAudioSource.Stop();
            }
        }

        if (!Gemini语音输入客户端引用.开始录音(out string error))
        {
            HandleVoiceTranscriptionError(error);
            return;
        }

        PlayInitialState(transitionDuration);
        RefreshChatInputControlsState();
        ShowPetChatBubble(ChatVoiceListeningHint, null, false);
    }

    private void HandleVoiceTranscriptionSuccess(string message)
    {
        RefreshChatInputControlsState();

        string finalMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        if (string.IsNullOrEmpty(finalMessage))
        {
            HandleVoiceTranscriptionError("\u6211\u8fd9\u6b21\u6ca1\u542c\u6e05\uff0c\u4f60\u518d\u8bf4\u4e00\u904d\u5427\u3002");
            return;
        }

        if (runtimeChatInputField != null)
        {
            runtimeChatInputField.text = finalMessage;
        }

        SubmitChatMessageCore(finalMessage, true);
    }

    private void HandleVoiceTranscriptionError(string error)
    {
        RefreshChatInputControlsState();
        string finalError = string.IsNullOrWhiteSpace(error)
            ? "\u6211\u8fd9\u6b21\u6ca1\u542c\u6e05\uff0c\u4f60\u518d\u8bf4\u4e00\u904d\u5427\u3002"
            : error.Trim();

        Debug.LogWarning("Gemini语音识别失败。 " + finalError);
        ShowPetChatBubble(finalError, 2.8f);
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

        if (runtimeVoiceMicSprite != null)
        {
            if (runtimeVoiceMicTexture != null)
            {
                Destroy(runtimeVoiceMicSprite);
            }

            runtimeVoiceMicSprite = null;
        }

        if (runtimeVoiceStopSprite != null)
        {
            if (runtimeVoiceStopTexture != null)
            {
                Destroy(runtimeVoiceStopSprite);
            }

            runtimeVoiceStopSprite = null;
        }

        if (runtimeVoiceBusySprite != null)
        {
            if (runtimeVoiceBusyTexture != null)
            {
                Destroy(runtimeVoiceBusySprite);
            }

            runtimeVoiceBusySprite = null;
        }

        if (runtimeVoiceMicTexture != null)
        {
            Destroy(runtimeVoiceMicTexture);
            runtimeVoiceMicTexture = null;
        }

        if (runtimeVoiceStopTexture != null)
        {
            Destroy(runtimeVoiceStopTexture);
            runtimeVoiceStopTexture = null;
        }

        if (runtimeVoiceBusyTexture != null)
        {
            Destroy(runtimeVoiceBusyTexture);
            runtimeVoiceBusyTexture = null;
        }
    }
}

