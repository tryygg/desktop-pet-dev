using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class 动画界面控制器 : MonoBehaviour
{
    [Header("角色引用")]
    [Tooltip("拖入角色 Animator；为空时会自动查找场景中的第一个 Animator")]
    [InspectorName("角色动画器")]
    public Animator characterAnimator;

    [Header("动画切换")]
    [Range(0f, 1f)]
    [InspectorName("过渡时长")]
    public float transitionDuration = 0.25f;

    [Header("三维软件式相机操作")]
    [Tooltip("Alt + 左键拖拽：绕目标旋转")]
    [InspectorName("环绕速度")]
    public float orbitSpeed = 180f;
    [Tooltip("中键拖拽：平移")]
    [InspectorName("平移速度")]
    public float panSpeed = 0.0035f;
    [Tooltip("滚轮 / Alt + 右键拖拽：缩放")]
    [InspectorName("缩放速度")]
    public float zoomSpeed = 4f;
    [InspectorName("最小距离")]
    public float minDistance = 0.25f;
    [InspectorName("最大距离")]
    public float maxDistance = 20f;

    [Header("UI 样式")]
    [InspectorName("面板颜色")]
    public Color panelColor = new Color(0.06f, 0.09f, 0.16f, 0.82f);
    [InspectorName("按钮常态颜色")]
    public Color buttonNormalColor = new Color(0.15f, 0.22f, 0.36f, 0.92f);
    [InspectorName("按钮悬停颜色")]
    public Color buttonHoverColor = new Color(0.23f, 0.35f, 0.54f, 0.95f);
    [InspectorName("按钮按下颜色")]
    public Color buttonPressedColor = new Color(0.11f, 0.17f, 0.28f, 1f);
    [InspectorName("启用预览控制")]
    public bool enablePreviewControls = false;
    [InspectorName("显示控制UI")]
    public bool showControlUI = false;

    private readonly string[] animationStates =
    {
        "沉思",
        "说话1",
        "说话2",
        "说话3",
        "辛苦啦",
        "打招呼",
        "比心"
    };

    private float yaw;
    private float pitch;
    private float distance = 3f;
    private Vector3 pivot;

    private Text statusText;
    private Image statusBackground;
    private Coroutine statusCoroutine;
    private GameObject runtimeCanvasObject;
    private GameObject runtimeEventSystemObject;

    private void Start()
    {
        if (!showControlUI && !enablePreviewControls)
        {
            DestroyGeneratedUI();
            enabled = false;
            return;
        }

        if (characterAnimator == null)
        {
            characterAnimator = FindObjectOfType<Animator>();
        }

        pivot = characterAnimator != null ? characterAnimator.transform.position : Vector3.zero;

        Vector3 offset = transform.position - pivot;
        distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);

        if (offset.sqrMagnitude < 0.0001f)
        {
            offset = new Vector3(0f, 1f, -distance);
        }

        yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        float xz = new Vector2(offset.x, offset.z).magnitude;
        pitch = Mathf.Atan2(offset.y, xz) * Mathf.Rad2Deg;

        UpdateCameraTransform();

        if (showControlUI)
        {
            SetupEventSystem();
            SetupUI();
            ShowStatus("就绪：点击右侧按钮切换动作");
        }
    }

    private void Update()
    {
        if (!enablePreviewControls)
        {
            return;
        }

        HandleCameraControl();
    }

    private void HandleCameraControl()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (alt && Input.GetMouseButton(0))
        {
            yaw += mouseX * orbitSpeed * Time.deltaTime;
            pitch -= mouseY * orbitSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 right = transform.right;
            Vector3 up = transform.up;
            float panFactor = Mathf.Max(0.01f, distance) * panSpeed;
            pivot -= right * mouseX * panFactor;
            pivot -= up * mouseY * panFactor;
        }

        if (alt && Input.GetMouseButton(1))
        {
            distance += mouseY * zoomSpeed * 0.1f;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance -= scroll * zoomSpeed;
        }

        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        UpdateCameraTransform();
    }

    private void UpdateCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
        transform.position = pivot + offset;
        transform.rotation = rotation;
    }

    private void SetupEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            runtimeEventSystemObject = eventSystemObj;
        }
    }

    private void SetupUI()
    {
        GameObject canvasObj = new GameObject("AnimationCanvas");
        runtimeCanvasObject = canvasObj;
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        CreateBackdrop(canvasObj.transform);
        Transform buttonPanel = CreateButtonPanel(canvasObj.transform);
        CreateHintPanel(canvasObj.transform);
        CreateStatusBar(canvasObj.transform);

        float buttonWidth = 320f;
        float buttonHeight = 68f;
        float spacing = 12f;
        float firstY = 188f;

        for (int i = 0; i < animationStates.Length; i++)
        {
            string stateName = animationStates[i];
            Vector2 pos = new Vector2(0f, firstY - i * (buttonHeight + spacing));
            CreateButton(buttonPanel, stateName, pos, new Vector2(buttonWidth, buttonHeight));
        }
    }

    private void CreateBackdrop(Transform parent)
    {
        GameObject backdrop = new GameObject("UIBackdrop");
        backdrop.transform.SetParent(parent, false);

        RectTransform rt = backdrop.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = backdrop.AddComponent<Image>();
        image.color = new Color(0.01f, 0.02f, 0.04f, 0.25f);
        image.raycastTarget = false;
    }

    private Transform CreateButtonPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("AnimationControlPanel");
        panelObj.transform.SetParent(parent, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(390f, 740f);
        rt.anchoredPosition = new Vector2(-230f, 0f);

        Image bg = panelObj.AddComponent<Image>();
        bg.color = panelColor;

        Shadow shadow = panelObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(8f, -8f);

        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.39f, 0.58f, 0.94f, 0.25f);
        outline.effectDistance = new Vector2(1f, -1f);

        CreateLabel(panelObj.transform, "动作控制", 40, FontStyle.Bold, TextAnchor.UpperCenter,
            new Vector2(0f, -28f), new Vector2(320f, 72f), new Color(0.9f, 0.96f, 1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        CreateLabel(panelObj.transform, "点击按钮即可切换角色动作", 24, FontStyle.Normal, TextAnchor.UpperCenter,
            new Vector2(0f, -74f), new Vector2(330f, 48f), new Color(0.68f, 0.8f, 0.94f, 0.95f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        return panelObj.transform;
    }

    private void CreateHintPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("OperationHintPanel");
        panelObj.transform.SetParent(parent, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(560f, 252f);
        rt.anchoredPosition = new Vector2(320f, -168f);

        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.14f, 0.78f);

        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.34f, 0.78f, 1f, 0.25f);
        outline.effectDistance = new Vector2(1f, -1f);

        CreateLabel(panelObj.transform, "操作提示", 34, FontStyle.Bold, TextAnchor.UpperLeft,
            new Vector2(24f, -20f), new Vector2(240f, 56f), new Color(0.86f, 0.95f, 1f, 1f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        string hints =
            "Alt + 左键拖拽  旋转视角\n" +
            "中键拖拽       平移视角\n" +
            "滚轮 / Alt + 右键  缩放镜头\n" +
            "点击右侧动作按钮可即时切换状态";

        Text hintText = CreateLabel(panelObj.transform, hints, 24, FontStyle.Normal, TextAnchor.UpperLeft,
            new Vector2(24f, -82f), new Vector2(510f, 160f), new Color(0.79f, 0.9f, 0.98f, 1f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        hintText.lineSpacing = 1.2f;
    }

    private void CreateStatusBar(Transform parent)
    {
        GameObject statusObj = new GameObject("StatusBar");
        statusObj.transform.SetParent(parent, false);

        RectTransform rt = statusObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(760f, 70f);
        rt.anchoredPosition = new Vector2(0f, 56f);

        statusBackground = statusObj.AddComponent<Image>();
        statusBackground.color = new Color(0.05f, 0.22f, 0.36f, 0f);

        Outline outline = statusObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.36f, 0.86f, 1f, 0f);
        outline.effectDistance = new Vector2(1f, -1f);

        statusText = CreateLabel(statusObj.transform, string.Empty, 26, FontStyle.Bold,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(730f, 60f), new Color(0.93f, 0.98f, 1f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    }

    private Text CreateLabel(
        Transform parent,
        string content,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor anchor,
        Vector2 anchoredPos,
        Vector2 size,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;

        Text txt = textObj.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null)
        {
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        txt.fontSize = fontSize;
        txt.fontStyle = fontStyle;
        txt.alignment = anchor;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.color = color;
        txt.raycastTarget = false;

        return txt;
    }

    private void CreateButton(Transform parent, string label, Vector2 position, Vector2 size)
    {
        GameObject buttonObj = new GameObject(label + "_Btn");
        buttonObj.transform.SetParent(parent, false);

        RectTransform rt = buttonObj.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        Image img = buttonObj.AddComponent<Image>();
        img.color = buttonNormalColor;

        Shadow shadow = buttonObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(3f, -3f);

        Outline outline = buttonObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.62f, 0.82f, 1f, 0.32f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button btn = buttonObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        GameObject accentObj = new GameObject("Accent");
        accentObj.transform.SetParent(buttonObj.transform, false);
        RectTransform accentRt = accentObj.AddComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0f, 1f);
        accentRt.anchorMax = new Vector2(1f, 1f);
        accentRt.pivot = new Vector2(0.5f, 1f);
        accentRt.sizeDelta = new Vector2(0f, 6f);
        accentRt.anchoredPosition = Vector2.zero;
        Image accentImage = accentObj.AddComponent<Image>();
        accentImage.color = new Color(0.58f, 0.84f, 1f, 0.8f);
        accentImage.raycastTarget = false;

        Text txt = CreateLabel(buttonObj.transform, label, 30, FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, size - new Vector2(20f, 6f), new Color(0.91f, 0.97f, 1f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        按钮反馈 feedback = buttonObj.AddComponent<按钮反馈>();
        feedback.targetImage = img;
        feedback.targetText = txt;
        feedback.normalColor = buttonNormalColor;
        feedback.hoverColor = buttonHoverColor;
        feedback.pressedColor = buttonPressedColor;

        btn.onClick.AddListener(() =>
        {
            if (characterAnimator != null)
            {
                characterAnimator.CrossFade(label, transitionDuration);
                ShowStatus("已切换动作：" + label);
            }
            else
            {
                ShowStatus("未找到 Animator，请先绑定角色");
            }

            feedback.PlayPulse();
        });
    }

    private void ShowStatus(string message)
    {
        if (statusText == null || statusBackground == null)
        {
            return;
        }

        statusText.text = message;

        if (statusCoroutine != null)
        {
            StopCoroutine(statusCoroutine);
        }

        statusCoroutine = StartCoroutine(StatusRoutine());
    }

    private IEnumerator StatusRoutine()
    {
        float fadeIn = 0.18f;
        float hold = 1.1f;
        float fadeOut = 0.35f;

        yield return FadeStatus(0f, 1f, fadeIn);
        yield return new WaitForSeconds(hold);
        yield return FadeStatus(1f, 0f, fadeOut);
    }

    private IEnumerator FadeStatus(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(time / duration);
            float alpha = Mathf.Lerp(from, to, t);

            statusBackground.color = new Color(0.05f, 0.22f, 0.36f, 0.78f * alpha);
            statusText.color = new Color(0.93f, 0.98f, 1f, alpha);

            yield return null;
        }

        statusBackground.color = new Color(0.05f, 0.22f, 0.36f, 0.78f * to);
        statusText.color = new Color(0.93f, 0.98f, 1f, to);
    }

    private void OnDisable()
    {
        if (!showControlUI && !enablePreviewControls)
        {
            DestroyGeneratedUI();
        }
    }

    private void DestroyGeneratedUI()
    {
        if (runtimeCanvasObject == null)
        {
            GameObject existingCanvas = GameObject.Find("AnimationCanvas");
            if (existingCanvas != null)
            {
                Destroy(existingCanvas);
            }
        }
        else
        {
            Destroy(runtimeCanvasObject);
            runtimeCanvasObject = null;
        }

        if (runtimeEventSystemObject != null)
        {
            Destroy(runtimeEventSystemObject);
            runtimeEventSystemObject = null;
        }
    }
}

public class 按钮反馈 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [InspectorName("目标图片")]
    public Image targetImage;
    [InspectorName("目标文本")]
    public Text targetText;

    [InspectorName("常态颜色")]
    public Color normalColor = new Color(0.15f, 0.22f, 0.36f, 0.92f);
    [InspectorName("悬停颜色")]
    public Color hoverColor = new Color(0.23f, 0.35f, 0.54f, 0.95f);
    [InspectorName("按下颜色")]
    public Color pressedColor = new Color(0.11f, 0.17f, 0.28f, 1f);

    private readonly Color normalTextColor = new Color(0.91f, 0.97f, 1f, 1f);
    private readonly Color hoverTextColor = new Color(1f, 1f, 1f, 1f);
    private readonly Color pressedTextColor = new Color(0.84f, 0.93f, 1f, 1f);

    private Vector3 targetScale = Vector3.one;
    private bool isInside;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        targetScale = Vector3.one;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 18f);

        if (targetImage != null)
        {
            targetImage.color = Color.Lerp(targetImage.color, targetColor, Time.deltaTime * 18f);
        }

        if (targetText != null)
        {
            targetText.color = Color.Lerp(targetText.color, targetTextColor, Time.deltaTime * 18f);
        }
    }

    private Color targetColor;
    private Color targetTextColor;

    private void Start()
    {
        targetColor = normalColor;
        targetTextColor = normalTextColor;

        if (targetImage != null)
        {
            targetImage.color = normalColor;
        }

        if (targetText != null)
        {
            targetText.color = normalTextColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isInside = true;
        targetScale = Vector3.one * 1.035f;
        targetColor = hoverColor;
        targetTextColor = hoverTextColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isInside = false;
        targetScale = Vector3.one;
        targetColor = normalColor;
        targetTextColor = normalTextColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = Vector3.one * 0.97f;
        targetColor = pressedColor;
        targetTextColor = pressedTextColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isInside)
        {
            targetScale = Vector3.one * 1.035f;
            targetColor = hoverColor;
            targetTextColor = hoverTextColor;
        }
        else
        {
            targetScale = Vector3.one;
            targetColor = normalColor;
            targetTextColor = normalTextColor;
        }
    }

    public void PlayPulse()
    {
        StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        Vector3 pulseScale = Vector3.one * 1.08f;

        float t = 0f;
        while (t < 0.08f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, pulseScale, Mathf.Clamp01(t / 0.08f));
            yield return null;
        }

        targetScale = isInside ? Vector3.one * 1.035f : Vector3.one;
    }
}
