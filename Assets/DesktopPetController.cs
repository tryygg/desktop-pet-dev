using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DesktopPetController : MonoBehaviour
{
    [Header("References")]
    public Animator characterAnimator;
    public Transform visualRoot;
    public TransparentWindow windowController;
    public Camera petCamera;

    [Header("Animation")]
    public string initialStateName = "\u6c89\u601d";
    public string[] menuStateNames =
    {
        "\u6c89\u601d",
        "\u8bf4\u8bdd1",
        "\u8bf4\u8bdd2",
        "\u8bf4\u8bdd3",
        "\u8f9b\u82e6\u5566",
        "\u6253\u62db\u547c",
        "\u6bd4\u5fc3"
    };
    public float transitionDuration = 0.15f;

    [Header("Scale")]
    public float initialScale = 1f;
    public float minScale = 0.35f;
    public float maxScale = 3f;
    public float scaleStep = 0.12f;

    [Header("Window Behavior")]
    public bool startCenteredOnTaskbar = true;
    public bool autoFitWindowToPet = true;
    public Vector2Int minWindowSize = new Vector2Int(240, 320);
    public Vector2Int contentPadding = new Vector2Int(72, 96);
    public Vector2Int maxAutoFitPadding = new Vector2Int(28, 40);
    public int edgeSnapDistance = 32;
    public int taskbarSnapDistance = 56;
    public int bounceDistance = 18;
    public float snapDuration = 0.08f;
    public float bounceDuration = 0.08f;

    [Header("Mask Region")]
    public bool useRenderedMaskRegion = false;
    public float maskRegionUpdateInterval = 0.08f;
    public int maskRowStep = 2;
    public int maskColorTolerance = 16;
    public int maskEdgePadding = 2;

    [Header("Performance")]
    public int idleFrameRate = 60;
    public int activeFrameRate = 60;

    [Header("Menu Look")]
    public Vector2 menuPadding = new Vector2(12f, 12f);
    public float menuWidth = 176f;
    public float menuHeaderHeight = 24f;
    public float menuItemHeight = 30f;
    public Color menuPanelColor = new Color(0.08f, 0.1f, 0.14f, 1f);
    public Color menuItemColor = new Color(0.15f, 0.18f, 0.24f, 1f);
    public Color menuHoverColor = new Color(0.24f, 0.3f, 0.39f, 1f);
    public Color menuActiveColor = new Color(0.3f, 0.44f, 0.56f, 1f);
    public Color menuBorderColor = new Color(0.24f, 0.29f, 0.35f, 1f);

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
    private bool cursorInsideWindow;
    private bool cursorOverPet;
    private bool cursorOverMenu;
    private bool previousLeftDown;
    private bool previousRightDown;
    private int hoveredMenuIndex = -1;
    private string currentStateName = string.Empty;

    private Rect menuRect;
    private Vector2 clientCursorPosition;
    private Vector2 guiCursorPosition;
    private Vector2Int dragOffset;

    private Canvas runtimeMenuCanvas;
    private RectTransform runtimeMenuRoot;
    private Text runtimeMenuHeader;
    private readonly List<Image> runtimeMenuItemBackgrounds = new List<Image>();
    private Image runtimeExitBackground;
    private Font runtimeFont;
    private Texture2D regionMaskTexture;
    private Coroutine regionMaskRoutine;
    private WaitForEndOfFrame endOfFrameYield;
    private float nextMaskUpdateTime;

    private const string ExitMenuLabel = "\u9000\u51fa";
    private const string MenuHeaderLabel = "\u52a8\u753b\u5207\u6362";
    private void Awake()
    {
        DisablePreviewControls();
        CleanupLegacyPreviewUI();
        endOfFrameYield = new WaitForEndOfFrame();
    }

    private IEnumerator Start()
    {
        ResolveReferences();
        DisablePreviewControls();
        CacheCharacterData();
        EnsureCharacterCollider();

        ApplyScale(initialScale);

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

        bool leftDown = windowController.IsPrimaryButtonDown();
        bool rightDown = windowController.IsSecondaryButtonDown();

        bool keepInteractive = interactionMode == InteractionMode.Dragging || menuOpen || cursorOverPet;
        windowController.SetClickThrough(!keepInteractive && windowController.clickThroughOnEmptyArea);

        if (menuOpen)
        {
            UpdateMenuInteraction(leftDown, rightDown);
            UpdateRuntimeMenuVisuals();
        }
        else if (interactionMode == InteractionMode.Dragging)
        {
            UpdateDragging(leftDown);
        }
        else
        {
            UpdatePassiveInteraction(leftDown, rightDown);
        }

        keepInteractive = interactionMode == InteractionMode.Dragging || menuOpen || cursorOverPet || cursorOverMenu;
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
            windowController = GetComponent<TransparentWindow>();
        }
    }

    private void DisablePreviewControls()
    {
        AnimationUIController previewController = GetComponent<AnimationUIController>();
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
        if (!cursorInsideWindow || cursorOverMenu)
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

        if (autoFitWindowToPet && windowController != null && windowController.IsReady && !menuOpen)
        {
            CropWindowToCharacter();
        }
    }

    private void UpdateCursorState()
    {
        cursorInsideWindow = false;
        cursorOverPet = false;
        cursorOverMenu = false;
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

        if (cursorOverMenu && hoveredMenuIndex == availableMenuStates.Count)
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
        if (availableMenuStates.Count == 0)
        {
            return;
        }

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

        if (autoFitWindowToPet && windowController != null && windowController.IsReady)
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
        GetWindowLimits(out int left, out int top, out int right, out int bottom);
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
        float menuHeight = menuPadding.y * 2f + menuHeaderHeight + availableMenuStates.Count * menuItemHeight;
        menuHeight += menuItemHeight;
        float maxX = Mathf.Max(8f, windowController.WindowSize.x - menuWidth - 8f);
        float maxY = Mathf.Max(8f, windowController.WindowSize.y - menuHeight - 8f);

        float x = Mathf.Clamp(guiCursor.x + 12f, 8f, maxX);
        float y = Mathf.Clamp(guiCursor.y + 12f, 8f, maxY);
        return new Rect(x, y, menuWidth, menuHeight);
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

        if (GetExitItemRect().Contains(guiPosition))
        {
            return availableMenuStates.Count;
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
        return GetMenuItemRect(availableMenuStates.Count);
    }

    private void ExpandWindowRegionForMenu()
    {
        if (windowController == null)
        {
            return;
        }

        RectInt petRect = windowController.VisibleContentRect;
        RectInt menuClientRect = new RectInt(
            Mathf.FloorToInt(menuRect.x),
            Mathf.FloorToInt(menuRect.y),
            Mathf.CeilToInt(menuRect.width),
            Mathf.CeilToInt(menuRect.height));

        windowController.SetWindowRegions(petRect, menuClientRect);
    }

    private void ShowRuntimeMenu()
    {
        EnsureRuntimeMenu();
        RebuildRuntimeMenuItems();
        UpdateRuntimeMenuVisuals();
        runtimeMenuCanvas.gameObject.SetActive(true);
    }

    private void HideRuntimeMenu()
    {
        if (runtimeMenuCanvas != null)
        {
            runtimeMenuCanvas.gameObject.SetActive(false);
        }
    }

    private void EnsureRuntimeMenu()
    {
        if (runtimeMenuCanvas != null)
        {
            return;
        }

        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (runtimeFont == null)
        {
            runtimeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject canvasObject = new GameObject("DesktopPetMenuCanvas");
        runtimeMenuCanvas = canvasObject.AddComponent<Canvas>();
        runtimeMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        runtimeMenuCanvas.sortingOrder = 2000;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);
        canvasObject.SetActive(false);

        GameObject rootObject = new GameObject("MenuRoot");
        rootObject.transform.SetParent(canvasObject.transform, false);
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
    }

    private void RebuildRuntimeMenuItems()
    {
        EnsureRuntimeMenu();

        for (int i = 0; i < runtimeMenuItemBackgrounds.Count; i++)
        {
            if (runtimeMenuItemBackgrounds[i] != null)
            {
                Destroy(runtimeMenuItemBackgrounds[i].gameObject);
            }
        }

        runtimeMenuItemBackgrounds.Clear();
        runtimeExitBackground = null;

        for (int i = 0; i < availableMenuStates.Count; i++)
        {
            Image itemBackground = CreateRuntimeMenuItem("State_" + i, availableMenuStates[i], i);
            runtimeMenuItemBackgrounds.Add(itemBackground);
        }

        runtimeExitBackground = CreateRuntimeMenuItem("Exit", ExitMenuLabel, availableMenuStates.Count);
        Text exitLabel = runtimeExitBackground.GetComponentInChildren<Text>();
        if (exitLabel != null)
        {
            exitLabel.color = new Color(1f, 0.94f, 0.94f, 1f);
        }
    }

    private Image CreateRuntimeMenuItem(string objectName, string label, int index)
    {
        GameObject itemObject = new GameObject(objectName);
        itemObject.transform.SetParent(runtimeMenuRoot, false);
        RectTransform itemRect = itemObject.AddComponent<RectTransform>();
        Rect localRect = index == availableMenuStates.Count ? GetExitItemRect() : GetMenuItemRect(index);
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(0f, 1f);
        itemRect.pivot = new Vector2(0f, 1f);
        itemRect.sizeDelta = new Vector2(localRect.width, localRect.height);
        itemRect.anchoredPosition = new Vector2(localRect.x - menuRect.x, -(localRect.y - menuRect.y));

        Image background = itemObject.AddComponent<Image>();
        background.color = GetRuntimeMenuColor(menuItemColor);

        Text itemLabel = CreateRuntimeMenuText("Label", itemObject.transform, 13, FontStyle.Normal);
        RectTransform labelRect = itemLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);
        itemLabel.alignment = TextAnchor.MiddleLeft;
        itemLabel.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        itemLabel.text = label;

        return background;
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

        if (runtimeExitBackground != null)
        {
            Color exitColor = hoveredMenuIndex == availableMenuStates.Count
                ? menuHoverColor
                : new Color(0.45f, 0.2f, 0.2f, 1f);
            runtimeExitBackground.color = GetRuntimeMenuColor(exitColor);
        }
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

        if (regionMaskTexture != null)
        {
            Destroy(regionMaskTexture);
            regionMaskTexture = null;
        }
    }
}

