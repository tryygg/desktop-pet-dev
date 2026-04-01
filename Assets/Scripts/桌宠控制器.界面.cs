using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class 桌宠控制器
{
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
    private Text runtimeChatPlaceholderText;
    private Text runtimeChatVoiceButtonLabel;
    private Image runtimeChatVoiceButtonIcon;
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
    private Sprite runtimeVoiceMicSprite;
    private Sprite runtimeVoiceStopSprite;
    private Sprite runtimeVoiceBusySprite;
    private Texture2D runtimeChatBubbleProcessedTexture;
    private Texture2D runtimeVoiceMicTexture;
    private Texture2D runtimeVoiceStopTexture;
    private Texture2D runtimeVoiceBusyTexture;
    private Button runtimeChatSendButton;
    private Button runtimeChatVoiceButton;
    private Button runtimeChatCloseButton;
    private Button runtimeChatHistoryButton;
    private Button runtimeChatHistoryCloseButton;
    private Font runtimeFont;
    private Coroutine chatAnimationRoutine;
    private Coroutine chatBubbleRoutine;
    private GameObject runtimeEventSystemObject;
    private bool ownsSceneUiCanvas;
    private bool ownsRuntimeEventSystem;
    private bool startupLoadingVisible;
    private const int BubbleAlphaCutoff = 200;
    private const float BubbleSideInsetRatio = 0.16f;
    private const float BubbleTopInsetRatio = 0.16f;
    private const float BubbleBottomInsetRatio = 112f / 430f;

    private const string ExitMenuLabel = "退出";
    private const string MenuHeaderLabel = "动画切换";
    private const string ChatMenuLabel = "对话";
    private const string ChatHeaderLabel = "聊天";
    private const string ChatInputPlaceholderLabel = "输入你想说的话...";
    private const string ChatSendLabel = "发送";
    private const string ChatVoiceLabel = "语音";
    private const string ChatVoiceStopLabel = "停止";
    private const string ChatVoiceProcessingLabel = "识别";
    private const string ChatVoiceListeningHint = "正在听你说话...";
    private const string ChatVoiceProcessingHint = "正在识别语音...";
    private const string ChatCloseLabel = "关闭";
    private const string ChatHistoryLabel = "历史";
    private const string ChatHistoryHeaderLabel = "对话记录";
    private const string StartupLoadingLabel = "加载中...";

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
        float minWidth = Mathf.Min(420f, Mathf.Max(320f, windowController.WindowSize.x - 16f));
        float minHeight = Mathf.Min(104f, Mathf.Max(82f, windowController.WindowSize.y - 16f));
        float width = Mathf.Clamp(chatDockSize.x, minWidth, Mathf.Max(minWidth, windowController.WindowSize.x - 16f));
        float height = Mathf.Clamp(chatDockSize.y, minHeight, Mathf.Max(minHeight, windowController.WindowSize.y - 16f));
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
        PlayInitialState(transitionDuration);
        bool showGreetingBubble = false;

        if (chatHistory.Count == 0)
        {
            AppendChatLine("小浣熊", defaultGreeting);
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
        RefreshChatInputControlsState();
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
        if (Gemini语音输入客户端引用 != null && Gemini语音输入客户端引用.正在录音)
        {
            Gemini语音输入客户端引用.取消录音();
        }

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
        RefreshChatInputControlsState();
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

    private void ShowPetChatBubble(string message, float? displayDuration = null, bool autoHide = true)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            HidePetChatBubble();
            return;
        }

        EnsureRuntimeChatPanel();
        if (chatBubbleRoutine != null)
        {
            StopCoroutine(chatBubbleRoutine);
            chatBubbleRoutine = null;
        }

        float sequenceDuration = Mathf.Max(0.2f, displayDuration ?? chatBubbleDuration);
        if (!autoHide)
        {
            ApplyChatBubblePage(message.Trim());
            return;
        }

        Sprite bubbleSprite = chatBubbleBodySpriteOverride != null ? chatBubbleBodySpriteOverride : runtimeChatBubbleBodySprite;
        List<string> bubblePages = SplitChatBubblePages(message.Trim(), bubbleSprite);
        if (bubblePages.Count <= 1)
        {
            ApplyChatBubblePage(message.Trim());
            chatBubbleRoutine = StartCoroutine(HidePetChatBubbleAfterDelay(sequenceDuration));
            return;
        }

        chatBubbleRoutine = StartCoroutine(PlayChatBubbleSequence(bubblePages, sequenceDuration));
    }

    private void HidePetChatBubble(bool updateWindow = true)
    {
        if (chatBubbleRoutine != null)
        {
            StopCoroutine(chatBubbleRoutine);
            chatBubbleRoutine = null;
        }

        currentChatBubbleMessage = string.Empty;
        chatBubbleVisible = false;
        if (runtimeChatBubbleRoot != null)
        {
            runtimeChatBubbleRoot.gameObject.SetActive(false);
        }

        RefreshRuntimeCanvasVisibility();
        if (updateWindow && autoFitWindowToPet && !chatPanelOpen && !chatHistoryOpen)
        {
            CropWindowToCharacter();
        }
        else if (updateWindow)
        {
            ExpandWindowRegionForChatUI();
        }
    }

    private IEnumerator HidePetChatBubbleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        chatBubbleRoutine = null;
        HidePetChatBubble();
    }

    private IEnumerator PlayChatBubbleSequence(List<string> bubblePages, float totalDuration)
    {
        if (bubblePages == null || bubblePages.Count == 0)
        {
            chatBubbleRoutine = null;
            HidePetChatBubble();
            yield break;
        }

        if (bubblePages.Count == 1)
        {
            ApplyChatBubblePage(bubblePages[0]);
            yield return new WaitForSeconds(totalDuration);
            chatBubbleRoutine = null;
            HidePetChatBubble();
            yield break;
        }

        float perPageDuration = Mathf.Max(0.35f, totalDuration / bubblePages.Count);
        for (int i = 0; i < bubblePages.Count; i++)
        {
            ApplyChatBubblePage(bubblePages[i]);
            yield return new WaitForSeconds(perPageDuration);
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
            string page = remaining;
            while (page.Length > 1 && DoesChatBubbleTextOverflow(page, bubbleSprite))
            {
                page = page.Substring(0, page.Length - 1);
            }

            if (page.Length <= 0)
            {
                break;
            }

            int splitIndex = page.Length;
            if (splitIndex < remaining.Length)
            {
                int punctuationIndex = FindPreferredPageBreak(page);
                if (punctuationIndex > 0)
                {
                    splitIndex = punctuationIndex;
                    page = remaining.Substring(0, splitIndex).Trim();
                }
            }

            pages.Add(page);
            remaining = remaining.Substring(Mathf.Min(splitIndex, remaining.Length)).TrimStart();
        }

        return pages;
    }

    private static int FindPreferredPageBreak(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return -1;
        }

        int bestIndex = -1;
        char[] separators = { '。', '！', '？', '.', '!', '?', '，', ',', '；', ';', '\n' };
        for (int i = text.Length - 1; i >= 0; i--)
        {
            if (System.Array.IndexOf(separators, text[i]) >= 0)
            {
                bestIndex = i + 1;
                break;
            }
        }

        return bestIndex;
    }

    private bool DoesChatBubbleTextOverflow(string message, Sprite bubbleSprite)
    {
        if (string.IsNullOrWhiteSpace(message) || runtimeChatBubbleText == null)
        {
            return false;
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

        float candidateWidth = Mathf.Clamp(maxBubbleWidth, minBubbleWidth, configuredMaxWidth);
        float candidateHeight = candidateWidth / spriteAspect;
        float textWidth = candidateWidth * (1f - BubbleSideInsetRatio * 2f);
        float textHeight = candidateHeight * (1f - BubbleTopInsetRatio - BubbleBottomInsetRatio);

        TextGenerator generator = new TextGenerator();
        TextGenerationSettings settings = runtimeChatBubbleText.GetGenerationSettings(new Vector2(textWidth, 10000f));
        settings.horizontalOverflow = HorizontalWrapMode.Wrap;
        settings.verticalOverflow = VerticalWrapMode.Overflow;
        settings.generateOutOfBounds = true;
        float preferredHeight = generator.GetPreferredHeight(message.Trim(), settings);
        return preferredHeight > textHeight;
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

    private 桌宠界面控制器 ResolveUiController(bool createIfMissing)
    {
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

        if (界面控制器引用 == null && createIfMissing && Application.isPlaying && 界面预制体 != null)
        {
            界面控制器引用 = Instantiate(界面预制体, transform, false);
            界面控制器引用.name = "DesktopPetMenuCanvas";
            ownsSceneUiCanvas = true;
        }

        if (界面控制器引用 == null && createIfMissing && sceneUiCanvas != null)
        {
            界面控制器引用 = sceneUiCanvas.GetComponent<桌宠界面控制器>();
            if (界面控制器引用 == null)
            {
                界面控制器引用 = sceneUiCanvas.gameObject.AddComponent<桌宠界面控制器>();
            }
        }

        if (界面控制器引用 != null)
        {
            界面控制器引用.自动绑定();
            if (界面控制器引用.界面Canvas != null)
            {
                sceneUiCanvas = 界面控制器引用.界面Canvas;
            }
        }

        return 界面控制器引用;
    }

    private void ApplyUiControllerReferences(桌宠界面控制器 uiController)
    {
        if (uiController == null)
        {
            return;
        }

        uiController.自动绑定();

        if (uiController.界面Canvas != null)
        {
            runtimeMenuCanvas = uiController.界面Canvas;
            sceneUiCanvas = uiController.界面Canvas;
        }

        if (runtimeMenuRoot == null)
        {
            runtimeMenuRoot = uiController.菜单Root;
        }

        if (runtimeMenuItemsRoot == null)
        {
            runtimeMenuItemsRoot = uiController.菜单ItemsRoot;
        }

        if (runtimeChatPanelRoot == null)
        {
            runtimeChatPanelRoot = uiController.聊天面板Root;
        }

        if (runtimeChatHistoryPanelRoot == null)
        {
            runtimeChatHistoryPanelRoot = uiController.历史面板Root;
        }

        if (runtimeChatBubbleRoot == null)
        {
            runtimeChatBubbleRoot = uiController.气泡Root;
        }

        if (runtimeChatBubbleBodyRoot == null)
        {
            runtimeChatBubbleBodyRoot = uiController.气泡主体Root;
        }

        if (runtimeChatBubbleTailRoot == null)
        {
            runtimeChatBubbleTailRoot = uiController.气泡尾巴Root;
        }

        if (runtimeLoadingRoot == null)
        {
            runtimeLoadingRoot = uiController.加载遮罩Root;
        }

        if (runtimeMenuHeader == null)
        {
            runtimeMenuHeader = uiController.菜单标题Text;
        }

        if (runtimeChatHeader == null)
        {
            runtimeChatHeader = uiController.聊天标题Text;
        }

        if (runtimeChatHistoryHeader == null)
        {
            runtimeChatHistoryHeader = uiController.历史标题Text;
        }

        if (runtimeChatHistoryText == null)
        {
            runtimeChatHistoryText = uiController.历史内容Text;
        }

        if (runtimeChatBubbleText == null)
        {
            runtimeChatBubbleText = uiController.气泡Text;
        }

        if (runtimeLoadingText == null)
        {
            runtimeLoadingText = uiController.加载Text;
        }

        if (runtimeChatPlaceholderText == null)
        {
            runtimeChatPlaceholderText = uiController.输入框占位Text;
        }

        if (runtimeChatVoiceButtonLabel == null)
        {
            runtimeChatVoiceButtonLabel = uiController.语音按钮Label;
        }

        if (runtimeChatVoiceButtonIcon == null)
        {
            runtimeChatVoiceButtonIcon = uiController.语音按钮Icon;
        }

        if (runtimeChatInputField == null)
        {
            runtimeChatInputField = uiController.聊天输入框;
        }

        if (runtimeChatHistoryScrollRect == null)
        {
            runtimeChatHistoryScrollRect = uiController.历史ScrollRect;
        }

        if (runtimeChatPanelBackground == null)
        {
            runtimeChatPanelBackground = uiController.聊天面板Background;
        }

        if (runtimeChatHistoryPanelBackground == null)
        {
            runtimeChatHistoryPanelBackground = uiController.历史面板Background;
        }

        if (runtimeChatBubbleBackground == null)
        {
            runtimeChatBubbleBackground = uiController.气泡Background;
        }

        if (runtimeChatBubbleTailBackground == null)
        {
            runtimeChatBubbleTailBackground = uiController.气泡尾巴Background;
        }

        if (runtimeLoadingBackground == null)
        {
            runtimeLoadingBackground = uiController.加载遮罩Background;
        }

        if (runtimeChatBubbleOutline == null)
        {
            runtimeChatBubbleOutline = uiController.气泡Outline;
        }

        if (runtimeChatHistoryButton == null)
        {
            runtimeChatHistoryButton = uiController.历史Button;
        }

        if (runtimeChatSendButton == null)
        {
            runtimeChatSendButton = uiController.发送Button;
        }

        if (runtimeChatCloseButton == null)
        {
            runtimeChatCloseButton = uiController.关闭Button;
        }

        if (runtimeChatVoiceButton == null)
        {
            runtimeChatVoiceButton = uiController.语音Button;
        }

        if (runtimeChatHistoryCloseButton == null)
        {
            runtimeChatHistoryCloseButton = uiController.历史关闭Button;
        }

        if (runtimeChatBubbleBackground != null && runtimeChatBubbleOutline == null)
        {
            runtimeChatBubbleOutline = runtimeChatBubbleBackground.gameObject.AddComponent<Outline>();
        }

        if (runtimeChatInputField != null)
        {
            ConfigureRuntimeChatInputField();
        }

        ConfigureChatBubbleOutline(runtimeChatBubbleOutline);
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

        桌宠界面控制器 uiController = ResolveUiController(true);
        ApplyUiControllerReferences(uiController);

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
            uiController = ResolveUiController(true);
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

        ApplyUiControllerReferences(ResolveUiController(true));
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
            EnsureChatVoiceButton();
            RebindChatButtonListeners();
            RefreshChatInputControlsState();
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
        headerRect.sizeDelta = new Vector2(-128f, 26f);
        runtimeChatHeader.alignment = TextAnchor.MiddleLeft;
        runtimeChatHeader.text = ChatHeaderLabel;
        runtimeChatHeader.color = new Color(0.96f, 0.97f, 0.99f, 1f);

        GameObject historyButtonObject = CreateChatButtonObject("ChatHistoryButton", panelObject.transform, ChatHistoryLabel, out runtimeChatHistoryButton);
        RectTransform historyButtonRect = historyButtonObject.GetComponent<RectTransform>();
        historyButtonRect.anchorMin = new Vector2(1f, 1f);
        historyButtonRect.anchorMax = new Vector2(1f, 1f);
        historyButtonRect.pivot = new Vector2(1f, 1f);
        historyButtonRect.sizeDelta = new Vector2(52f, 24f);
        historyButtonRect.anchoredPosition = new Vector2(-68f, -8f);
        runtimeChatHistoryButton.onClick.AddListener(ToggleChatHistoryPanel);

        GameObject closeButtonObject = CreateChatButtonObject("ChatCloseButton", panelObject.transform, ChatCloseLabel, out runtimeChatCloseButton);
        RectTransform closeRect = closeButtonObject.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(52f, 24f);
        closeRect.anchoredPosition = new Vector2(-10f, -8f);
        runtimeChatCloseButton.onClick.AddListener(CloseChatPanel);

        GameObject inputBackgroundObject = new GameObject("ChatInputBackground");
        inputBackgroundObject.transform.SetParent(panelObject.transform, false);
        RectTransform inputBackgroundRect = inputBackgroundObject.AddComponent<RectTransform>();
        inputBackgroundRect.anchorMin = new Vector2(0f, 0f);
        inputBackgroundRect.anchorMax = new Vector2(1f, 0f);
        inputBackgroundRect.pivot = new Vector2(0.5f, 0f);
        inputBackgroundRect.sizeDelta = new Vector2(-144f, 56f);
        inputBackgroundRect.anchoredPosition = new Vector2(-62f, 10f);
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
        runtimeChatPlaceholderText = placeholderText;

        runtimeChatInputField.textComponent = inputText;
        runtimeChatInputField.placeholder = placeholderText;

        GameObject voiceButtonObject = CreateChatButtonObject("ChatVoiceButton", panelObject.transform, ChatVoiceLabel, out runtimeChatVoiceButton, out runtimeChatVoiceButtonLabel);
        RectTransform voiceRect = voiceButtonObject.GetComponent<RectTransform>();
        voiceRect.anchorMin = new Vector2(1f, 0f);
        voiceRect.anchorMax = new Vector2(1f, 0f);
        voiceRect.pivot = new Vector2(1f, 0f);
        voiceRect.sizeDelta = new Vector2(52f, 56f);
        voiceRect.anchoredPosition = new Vector2(-66f, 10f);
        runtimeChatVoiceButton.onClick.AddListener(ToggleVoiceInput);

        GameObject sendButtonObject = CreateChatButtonObject("ChatSendButton", panelObject.transform, ChatSendLabel, out runtimeChatSendButton);
        RectTransform sendRect = sendButtonObject.GetComponent<RectTransform>();
        sendRect.anchorMin = new Vector2(1f, 0f);
        sendRect.anchorMax = new Vector2(1f, 0f);
        sendRect.pivot = new Vector2(1f, 0f);
        sendRect.sizeDelta = new Vector2(52f, 56f);
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
        RefreshChatInputControlsState();

        ApplyUiControllerReferences(ResolveUiController(true));
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

        ApplyUiControllerReferences(ResolveUiController(true));
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
        return CreateChatButtonObject(objectName, parent, label, out button, out _);
    }

    private GameObject CreateChatButtonObject(string objectName, Transform parent, string label, out Button button, out Text buttonLabel)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);
        buttonObject.AddComponent<RectTransform>();
        Image buttonBackground = buttonObject.AddComponent<Image>();
        buttonBackground.color = GetRuntimeMenuColor(menuHoverColor);
        button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonBackground;

        buttonLabel = CreateRuntimeMenuText("Label", buttonObject.transform, 12, FontStyle.Bold);
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

    private void EnsureVoiceButtonIcon()
    {
        if (runtimeChatVoiceButton == null)
        {
            return;
        }

        RectTransform buttonRect = runtimeChatVoiceButton.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            return;
        }

        if (runtimeChatVoiceButtonLabel != null)
        {
            runtimeChatVoiceButtonLabel.text = string.Empty;
            runtimeChatVoiceButtonLabel.gameObject.SetActive(false);
        }

        if (runtimeChatVoiceButtonIcon == null)
        {
            Transform iconTransform = runtimeChatVoiceButton.transform.Find("Icon");
            if (iconTransform != null)
            {
                runtimeChatVoiceButtonIcon = iconTransform.GetComponent<Image>();
            }
        }

        if (runtimeChatVoiceButtonIcon == null)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(runtimeChatVoiceButton.transform, false);
            runtimeChatVoiceButtonIcon = iconObject.AddComponent<Image>();
            runtimeChatVoiceButtonIcon.raycastTarget = false;
        }

        RectTransform iconRect = runtimeChatVoiceButtonIcon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(30f, 30f);
        iconRect.anchoredPosition = Vector2.zero;
        runtimeChatVoiceButtonIcon.color = new Color(0.98f, 0.99f, 1f, 1f);
        runtimeChatVoiceButtonIcon.preserveAspect = true;
        runtimeChatVoiceButtonIcon.type = Image.Type.Simple;
    }

    private Sprite GetVoiceMicSprite()
    {
        if (runtimeVoiceMicSprite == null)
        {
            runtimeVoiceMicSprite = Resources.Load<Sprite>("麦克风图标");
            if (runtimeVoiceMicSprite == null)
            {
                runtimeVoiceMicTexture = CreateVoiceIconTexture(CreateVoiceMicPixel);
                runtimeVoiceMicSprite = CreateVoiceIconSprite(runtimeVoiceMicTexture);
            }
        }

        return runtimeVoiceMicSprite;
    }

    private Sprite GetVoiceStopSprite()
    {
        if (runtimeVoiceStopSprite == null)
        {
            runtimeVoiceStopTexture = CreateVoiceIconTexture(CreateVoiceStopPixel);
            runtimeVoiceStopSprite = CreateVoiceIconSprite(runtimeVoiceStopTexture);
        }

        return runtimeVoiceStopSprite;
    }

    private Sprite GetVoiceBusySprite()
    {
        if (runtimeVoiceBusySprite == null)
        {
            runtimeVoiceBusyTexture = CreateVoiceIconTexture(CreateVoiceBusyPixel);
            runtimeVoiceBusySprite = CreateVoiceIconSprite(runtimeVoiceBusyTexture);
        }

        return runtimeVoiceBusySprite;
    }

    private static Sprite CreateVoiceIconSprite(Texture2D texture)
    {
        return texture == null
            ? null
            : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
    }

    private static Texture2D CreateVoiceIconTexture(System.Func<float, float, bool> fillPredicate)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        Color32 opaque = new Color32(255, 255, 255, 255);
        Color32 transparent = new Color32(255, 255, 255, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size;
                float ny = (y + 0.5f) / size;
                pixels[y * size + x] = fillPredicate(nx, ny) ? opaque : transparent;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static bool CreateVoiceMicPixel(float x, float y)
    {
        bool capsule = InRoundedRect(x, y, 0.31f, 0.52f, 0.69f, 0.85f, 0.18f);
        bool stem = x >= 0.46f && x <= 0.54f && y >= 0.28f && y <= 0.52f;
        bool foot = x >= 0.36f && x <= 0.64f && y >= 0.18f && y <= 0.24f;
        bool arc = y >= 0.34f && y <= 0.60f && Mathf.Abs(x - 0.5f) >= 0.18f && Mathf.Abs(x - 0.5f) <= 0.23f;
        return capsule || stem || foot || arc;
    }

    private static bool CreateVoiceStopPixel(float x, float y)
    {
        return x >= 0.28f && x <= 0.72f && y >= 0.28f && y <= 0.72f;
    }

    private static bool CreateVoiceBusyPixel(float x, float y)
    {
        return InCircle(x, y, 0.28f, 0.5f, 0.08f)
               || InCircle(x, y, 0.5f, 0.5f, 0.08f)
               || InCircle(x, y, 0.72f, 0.5f, 0.08f);
    }

    private static bool InCircle(float x, float y, float cx, float cy, float radius)
    {
        float dx = x - cx;
        float dy = y - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool InRoundedRect(float x, float y, float xMin, float yMin, float xMax, float yMax, float radius)
    {
        if (x < xMin || x > xMax || y < yMin || y > yMax)
        {
            return false;
        }

        float clampedX = Mathf.Clamp(x, xMin + radius, xMax - radius);
        float clampedY = Mathf.Clamp(y, yMin + radius, yMax - radius);
        float dx = x - clampedX;
        float dy = y - clampedY;
        return dx * dx + dy * dy <= radius * radius;
    }

    private void EnsureChatVoiceButton()
    {
        if (runtimeChatPanelRoot == null)
        {
            return;
        }

        Transform inputBackground = runtimeChatPanelRoot.Find("ChatInputBackground");
        if (inputBackground != null)
        {
            RectTransform inputRect = inputBackground as RectTransform;
            if (inputRect != null)
            {
                inputRect.sizeDelta = new Vector2(-144f, 56f);
                inputRect.anchoredPosition = new Vector2(-62f, 10f);
            }

            if (runtimeChatPlaceholderText == null)
            {
                runtimeChatPlaceholderText = inputBackground.Find("Placeholder")?.GetComponent<Text>();
            }
        }

        Transform sendButtonTransform = runtimeChatPanelRoot.Find("ChatSendButton");
        if (sendButtonTransform != null)
        {
            RectTransform sendRect = sendButtonTransform as RectTransform;
            if (sendRect != null)
            {
                sendRect.sizeDelta = new Vector2(52f, 56f);
                sendRect.anchoredPosition = new Vector2(-10f, 10f);
            }
        }

        if (runtimeChatVoiceButton == null)
        {
            Transform voiceButtonTransform = runtimeChatPanelRoot.Find("ChatVoiceButton");
            if (voiceButtonTransform != null)
            {
                runtimeChatVoiceButton = voiceButtonTransform.GetComponent<Button>();
                runtimeChatVoiceButtonLabel = voiceButtonTransform.Find("Label")?.GetComponent<Text>();
            }
        }

        if (runtimeChatVoiceButton == null)
        {
            GameObject voiceButtonObject = CreateChatButtonObject("ChatVoiceButton", runtimeChatPanelRoot, ChatVoiceLabel, out runtimeChatVoiceButton, out runtimeChatVoiceButtonLabel);
            RectTransform voiceRect = voiceButtonObject.GetComponent<RectTransform>();
            voiceRect.anchorMin = new Vector2(1f, 0f);
            voiceRect.anchorMax = new Vector2(1f, 0f);
            voiceRect.pivot = new Vector2(1f, 0f);
            voiceRect.sizeDelta = new Vector2(52f, 56f);
            voiceRect.anchoredPosition = new Vector2(-66f, 10f);
        }
        else
        {
            RectTransform voiceRect = runtimeChatVoiceButton.GetComponent<RectTransform>();
            if (voiceRect != null)
            {
                voiceRect.anchorMin = new Vector2(1f, 0f);
                voiceRect.anchorMax = new Vector2(1f, 0f);
                voiceRect.pivot = new Vector2(1f, 0f);
                voiceRect.sizeDelta = new Vector2(52f, 56f);
                voiceRect.anchoredPosition = new Vector2(-66f, 10f);
            }
        }

        EnsureVoiceButtonIcon();
        RefreshVoiceInputButtonVisuals();
    }

    private void RefreshChatInputControlsState()
    {
        bool isVoiceRecording = Gemini语音输入客户端引用 != null && Gemini语音输入客户端引用.正在录音;
        bool isVoiceBusy = Gemini语音输入客户端引用 != null && Gemini语音输入客户端引用.正在转写;
        bool chatControlsInteractable = !chatRequestInFlight && !isVoiceRecording && !isVoiceBusy;

        if (runtimeChatInputField != null)
        {
            runtimeChatInputField.interactable = chatControlsInteractable;
        }

        if (runtimeChatSendButton != null)
        {
            runtimeChatSendButton.interactable = chatControlsInteractable;
        }

        if (runtimeChatVoiceButton != null)
        {
            runtimeChatVoiceButton.interactable = !chatRequestInFlight && !isVoiceBusy;
        }

        if (runtimeChatPlaceholderText != null)
        {
            if (isVoiceRecording)
            {
                runtimeChatPlaceholderText.text = ChatVoiceListeningHint;
            }
            else if (isVoiceBusy)
            {
                runtimeChatPlaceholderText.text = ChatVoiceProcessingHint;
            }
            else
            {
                runtimeChatPlaceholderText.text = ChatInputPlaceholderLabel;
            }
        }

        RefreshVoiceInputButtonVisuals();
    }

    private void RefreshVoiceInputButtonVisuals()
    {
        if (runtimeChatVoiceButtonLabel != null)
        {
            runtimeChatVoiceButtonLabel.text = string.Empty;
            runtimeChatVoiceButtonLabel.gameObject.SetActive(false);
        }

        if (runtimeChatVoiceButton != null && runtimeChatVoiceButton.targetGraphic is Image voiceButtonImage)
        {
            bool isRecording = Gemini语音输入客户端引用 != null && Gemini语音输入客户端引用.正在录音;
            voiceButtonImage.color = GetRuntimeMenuColor(isRecording ? menuActiveColor : menuHoverColor);
        }

        if (runtimeChatVoiceButtonIcon != null)
        {
            if (Gemini语音输入客户端引用 != null && Gemini语音输入客户端引用.正在录音)
            {
                runtimeChatVoiceButtonIcon.sprite = GetVoiceStopSprite();
            }
            else if (Gemini语音输入客户端引用 != null && Gemini语音输入客户端引用.正在转写)
            {
                runtimeChatVoiceButtonIcon.sprite = GetVoiceBusySprite();
            }
            else
            {
                runtimeChatVoiceButtonIcon.sprite = GetVoiceMicSprite();
            }
        }
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
        ApplyUiControllerReferences(ResolveUiController(runtimeMenuCanvas != null || sceneUiCanvas != null));

        if (runtimeMenuCanvas == null && sceneUiCanvas != null)
        {
            runtimeMenuCanvas = sceneUiCanvas;
        }

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
                    runtimeChatPlaceholderText = inputBackground.Find("Placeholder")?.GetComponent<Text>();
                    ConfigureRuntimeChatInputField();
                }

                runtimeChatVoiceButton = chatPanel.Find("ChatVoiceButton")?.GetComponent<Button>();
                runtimeChatVoiceButtonLabel = chatPanel.Find("ChatVoiceButton/Label")?.GetComponent<Text>();
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

        EnsureChatVoiceButton();
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

        if (runtimeChatVoiceButton != null)
        {
            runtimeChatVoiceButton.onClick.RemoveAllListeners();
            runtimeChatVoiceButton.onClick.AddListener(ToggleVoiceInput);
        }

        if (runtimeChatHistoryCloseButton != null)
        {
            runtimeChatHistoryCloseButton.onClick.RemoveAllListeners();
            runtimeChatHistoryCloseButton.onClick.AddListener(ToggleChatHistoryPanel);
        }

        RefreshChatInputControlsState();
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
}
