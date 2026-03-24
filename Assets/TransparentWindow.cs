using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class TransparentWindow : MonoBehaviour
{
    public static TransparentWindow Instance { get; private set; }

    [Header("Window")]
    public Vector2Int initialWindowSize = new Vector2Int(520, 620);
    public bool forceWindowedModeOnStart = true;
    public bool keepTopMost = true;
    public bool hideFromAltTab = true;
    public bool clickThroughOnEmptyArea = true;
    public bool useColorKeyTransparency = true;
    public bool useDwmAlphaTransparency = false;
    public bool disablePostProcessingForTransparency = true;
    public Color transparentBackground = new Color(1f, 0f, 1f, 1f);

    public bool IsReady { get; private set; }
    public Vector2Int WindowSize { get; private set; }
    public RectInt WorkingArea { get; private set; }
    public RectInt VirtualDesktopBounds { get; private set; }
    public RectInt VisibleContentRect { get; private set; }
    public Vector2Int VisibleWindowSize => new Vector2Int(VisibleContentRect.width, VisibleContentRect.height);

    private Camera cachedCamera;
    private UniversalAdditionalCameraData urpCameraData;
    private IntPtr windowHandle = IntPtr.Zero;
    private bool clickThroughEnabled;
    private RectInt[] activeRegionRects = Array.Empty<RectInt>();

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint WS_EX_TRANSPARENT = 0x00000020;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_APPWINDOW = 0x00040000;
    private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    private const uint LWA_COLORKEY = 0x00000001;
    private const uint LWA_ALPHA = 0x00000002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const int RGN_OR = 2;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    private const uint DWM_BB_ENABLE = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int left;
        public int right;
        public int top;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DwmBlurBehind
    {
        public uint dwFlags;
        public bool fEnable;
        public IntPtr hRgnBlur;
        public bool fTransitionOnMaximized;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessId();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref Point point);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    [DllImport("gdi32.dll")]
    private static extern int CombineRgn(IntPtr hrgnDst, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int fnCombineMode);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins margins);

    [DllImport("dwmapi.dll")]
    private static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref DwmBlurBehind blurBehind);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
        {
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }

        return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
        {
            return GetWindowLongPtr64(hWnd, nIndex);
        }

        return new IntPtr(GetWindowLong32(hWnd, nIndex));
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        cachedCamera = GetComponent<Camera>();
        urpCameraData = GetComponent<UniversalAdditionalCameraData>();
        ConfigureCamera();
        Application.runInBackground = true;
        QualitySettings.antiAliasing = 0;
    }

    private IEnumerator Start()
    {
        if (forceWindowedModeOnStart)
        {
            Screen.SetResolution(initialWindowSize.x, initialWindowSize.y, FullScreenMode.Windowed);
        }

        float probeEndTime = Time.realtimeSinceStartup + 5f;
        while (!IsReady && Time.realtimeSinceStartup < probeEndTime)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            InitializeWindow();
        }

        if (!IsReady)
        {
            InitializeWindow();
        }

        if (IsReady)
        {
            StartCoroutine(MonitorWindowHandle());
        }
    }

    private void ConfigureCamera()
    {
        if (cachedCamera == null)
        {
            return;
        }

        cachedCamera.clearFlags = CameraClearFlags.SolidColor;
        cachedCamera.backgroundColor = GetEffectiveBackgroundColor();
        cachedCamera.allowMSAA = false;
        cachedCamera.allowHDR = false;

        if (!disablePostProcessingForTransparency || urpCameraData == null)
        {
            return;
        }

        urpCameraData.renderPostProcessing = false;
        urpCameraData.antialiasing = AntialiasingMode.None;
    }

    private void InitializeWindow()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IntPtr resolvedWindowHandle = ResolvePlayerWindowHandle();
        if (resolvedWindowHandle == IntPtr.Zero)
        {
            return;
        }

        windowHandle = resolvedWindowHandle;
        if (!IsReady)
        {
            clickThroughEnabled = clickThroughOnEmptyArea;
        }

        ApplyWindowStyles();

        Margins margins = new Margins
        {
            left = -1,
            right = 0,
            top = 0,
            bottom = 0
        };
        DwmExtendFrameIntoClientArea(windowHandle, ref margins);
        ApplyTransparencyMode();

        IntPtr zOrderTarget = keepTopMost ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(
            windowHandle,
            zOrderTarget,
            0,
            0,
            initialWindowSize.x,
            initialWindowSize.y,
            SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);

        UpdateWindowMetrics();
        VisibleContentRect = new RectInt(0, 0, WindowSize.x, WindowSize.y);
#else
        WindowSize = initialWindowSize;
        WorkingArea = new RectInt(0, 0, Screen.width, Screen.height);
        VirtualDesktopBounds = WorkingArea;
        VisibleContentRect = new RectInt(0, 0, WindowSize.x, WindowSize.y);
#endif

        IsReady = true;
    }

    public void SetClickThrough(bool enable)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (windowHandle == IntPtr.Zero || clickThroughEnabled == enable)
        {
            return;
        }

        clickThroughEnabled = enable;
        ApplyWindowStyles();
        ApplyTransparencyMode();
#else
        clickThroughEnabled = enable;
#endif
    }

    public Vector2Int GetWindowPosition()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (windowHandle != IntPtr.Zero && GetWindowRect(windowHandle, out Rect rect))
        {
            return new Vector2Int(rect.left, rect.top);
        }
#endif
        return Vector2Int.zero;
    }

    public void MoveWindowTo(int x, int y)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        IntPtr zOrderTarget = keepTopMost ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(windowHandle, zOrderTarget, x, y, 0, 0, SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
        UpdateWindowMetrics();
#endif
    }

    public Vector2Int GetVisibleWindowPosition()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        Vector2Int actualPosition = GetWindowPosition();
        return new Vector2Int(
            actualPosition.x + VisibleContentRect.x,
            actualPosition.y + VisibleContentRect.y);
#else
        return GetWindowPosition();
#endif
    }

    public void MoveVisibleWindowTo(int x, int y)
    {
        MoveWindowTo(x - VisibleContentRect.x, y - VisibleContentRect.y);
    }

    public void SetWindowRegion(RectInt clientRegion)
    {
        SetWindowRegions(clientRegion);
    }

    public void SetWindowRegions(params RectInt[] clientRegions)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        RectInt[] sanitizedRegions = SanitizeRegions(clientRegions);
        if (sanitizedRegions.Length == 0)
        {
            ClearWindowRegion();
            return;
        }

        IntPtr combinedRegion = IntPtr.Zero;
        for (int i = 0; i < sanitizedRegions.Length; i++)
        {
            RectInt rect = sanitizedRegions[i];
            IntPtr region = CreateRectRgn(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
            if (region == IntPtr.Zero)
            {
                continue;
            }

            if (combinedRegion == IntPtr.Zero)
            {
                combinedRegion = region;
                continue;
            }

            IntPtr destination = CreateRectRgn(0, 0, 0, 0);
            if (destination == IntPtr.Zero)
            {
                DeleteObject(region);
                continue;
            }

            CombineRgn(destination, combinedRegion, region, RGN_OR);
            DeleteObject(combinedRegion);
            DeleteObject(region);
            combinedRegion = destination;
        }

        if (combinedRegion != IntPtr.Zero)
        {
            SetWindowRgn(windowHandle, combinedRegion, true);
            activeRegionRects = sanitizedRegions;
            VisibleContentRect = CalculateBoundingRect(sanitizedRegions);
        }
#else
        RectInt[] sanitizedRegions = SanitizeRegions(clientRegions);
        activeRegionRects = sanitizedRegions;
        VisibleContentRect = sanitizedRegions.Length > 0
            ? CalculateBoundingRect(sanitizedRegions)
            : new RectInt(0, 0, WindowSize.x, WindowSize.y);
#endif
    }

    public void ClearWindowRegion()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (windowHandle != IntPtr.Zero)
        {
            SetWindowRgn(windowHandle, IntPtr.Zero, true);
        }
#endif
        activeRegionRects = Array.Empty<RectInt>();
        VisibleContentRect = new RectInt(0, 0, WindowSize.x, WindowSize.y);
    }

    public bool TryGetCursorScreenPosition(out Vector2Int cursorPosition)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (GetCursorPos(out Point cursorPoint))
        {
            cursorPosition = new Vector2Int(cursorPoint.x, cursorPoint.y);
            return true;
        }
#endif
        cursorPosition = Vector2Int.RoundToInt(Input.mousePosition);
        return true;
    }

    public bool TryGetCursorClientPosition(out Vector2 clientPosition, out bool insideWindow)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        clientPosition = Vector2.zero;
        insideWindow = false;

        if (windowHandle == IntPtr.Zero || !GetCursorPos(out Point screenPoint))
        {
            return false;
        }

        Point clientPoint = screenPoint;
        if (!ScreenToClient(windowHandle, ref clientPoint) || !GetClientRect(windowHandle, out Rect rect))
        {
            return false;
        }

        WindowSize = new Vector2Int(rect.right - rect.left, rect.bottom - rect.top);
        insideWindow = IsPointInsideActiveRegion(clientPoint.x, clientPoint.y);

        clientPosition = new Vector2(clientPoint.x, WindowSize.y - clientPoint.y);
        return true;
#else
        clientPosition = Input.mousePosition;
        insideWindow = clientPosition.x >= 0f &&
                       clientPosition.x < Screen.width &&
                       clientPosition.y >= 0f &&
                       clientPosition.y < Screen.height;
        return true;
#endif
    }

    public bool IsPrimaryButtonDown()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        return (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
#else
        return Input.GetMouseButton(0);
#endif
    }

    public bool IsSecondaryButtonDown()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        return (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
#else
        return Input.GetMouseButton(1);
#endif
    }

    private void ApplyTransparencyMode()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (useDwmAlphaTransparency && !useColorKeyTransparency)
        {
            DwmBlurBehind blurBehind = new DwmBlurBehind
            {
                dwFlags = DWM_BB_ENABLE,
                fEnable = true,
                hRgnBlur = IntPtr.Zero,
                fTransitionOnMaximized = false
            };
            DwmEnableBlurBehindWindow(windowHandle, ref blurBehind);
            return;
        }

        if (useColorKeyTransparency)
        {
            SetLayeredWindowAttributes(windowHandle, ToColorKey(transparentBackground), 0, LWA_COLORKEY);
            return;
        }

        SetLayeredWindowAttributes(windowHandle, 0, 255, LWA_ALPHA);
#endif
    }

    private Color GetEffectiveBackgroundColor()
    {
        if (useDwmAlphaTransparency && !useColorKeyTransparency)
        {
            return new Color(0f, 0f, 0f, 0f);
        }

        return transparentBackground;
    }

    private IEnumerator MonitorWindowHandle()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        while (true)
        {
            yield return new WaitForSecondsRealtime(0.5f);

            IntPtr resolvedWindowHandle = ResolvePlayerWindowHandle();
            if (resolvedWindowHandle == IntPtr.Zero || resolvedWindowHandle == windowHandle)
            {
                continue;
            }

            windowHandle = resolvedWindowHandle;
            ApplyWindowStyles();

            Margins margins = new Margins
            {
                left = -1,
                right = 0,
                top = 0,
                bottom = 0
            };
            DwmExtendFrameIntoClientArea(windowHandle, ref margins);
            ApplyTransparencyMode();
            UpdateWindowMetrics();
        }
#else
        yield break;
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private void ApplyWindowStyles()
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        uint currentStyle = unchecked((uint)GetWindowLongPtr(windowHandle, GWL_STYLE).ToInt64());
        currentStyle &= ~WS_OVERLAPPEDWINDOW;
        currentStyle |= WS_POPUP | WS_VISIBLE;
        SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(unchecked((int)currentStyle)));

        uint currentExStyle = unchecked((uint)GetWindowLongPtr(windowHandle, GWL_EXSTYLE).ToInt64());
        currentExStyle |= WS_EX_LAYERED;

        if (hideFromAltTab)
        {
            currentExStyle |= WS_EX_TOOLWINDOW;
            currentExStyle &= ~WS_EX_APPWINDOW;
        }
        else
        {
            currentExStyle &= ~WS_EX_TOOLWINDOW;
        }

        if (clickThroughEnabled)
        {
            currentExStyle |= WS_EX_TRANSPARENT;
        }
        else
        {
            currentExStyle &= ~WS_EX_TRANSPARENT;
        }

        SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr(unchecked((int)currentExStyle)));
    }
#endif

    private static uint ToColorKey(Color color)
    {
        Color32 color32 = color;
        return (uint)(color32.r | (color32.g << 8) | (color32.b << 16));
    }

    private void UpdateWindowMetrics()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (windowHandle != IntPtr.Zero && GetClientRect(windowHandle, out Rect clientRect))
        {
            WindowSize = new Vector2Int(clientRect.right - clientRect.left, clientRect.bottom - clientRect.top);
            if (VisibleContentRect.width <= 0 || VisibleContentRect.height <= 0)
            {
                VisibleContentRect = new RectInt(0, 0, WindowSize.x, WindowSize.y);
            }
        }

        int virtualX = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int virtualY = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int virtualWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int virtualHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        if (virtualWidth > 0 && virtualHeight > 0)
        {
            VirtualDesktopBounds = new RectInt(virtualX, virtualY, virtualWidth, virtualHeight);
            WorkingArea = VirtualDesktopBounds;
        }
#else
        WindowSize = new Vector2Int(Screen.width, Screen.height);
        WorkingArea = new RectInt(0, 0, Screen.width, Screen.height);
        VirtualDesktopBounds = WorkingArea;
        if (VisibleContentRect.width <= 0 || VisibleContentRect.height <= 0)
        {
            VisibleContentRect = new RectInt(0, 0, WindowSize.x, WindowSize.y);
        }
#endif
    }

    private RectInt[] SanitizeRegions(RectInt[] clientRegions)
    {
        if (clientRegions == null || clientRegions.Length == 0)
        {
            return Array.Empty<RectInt>();
        }

        int maxWidth = Mathf.Max(1, WindowSize.x);
        int maxHeight = Mathf.Max(1, WindowSize.y);
        RectInt[] sanitized = new RectInt[clientRegions.Length];
        int count = 0;

        for (int i = 0; i < clientRegions.Length; i++)
        {
            RectInt region = clientRegions[i];
            int x = Mathf.Clamp(region.x, 0, maxWidth - 1);
            int y = Mathf.Clamp(region.y, 0, maxHeight - 1);
            int width = Mathf.Clamp(region.width, 1, maxWidth - x);
            int height = Mathf.Clamp(region.height, 1, maxHeight - y);
            if (width <= 0 || height <= 0)
            {
                continue;
            }

            sanitized[count++] = new RectInt(x, y, width, height);
        }

        if (count == sanitized.Length)
        {
            return sanitized;
        }

        RectInt[] trimmed = new RectInt[count];
        Array.Copy(sanitized, trimmed, count);
        return trimmed;
    }

    private static RectInt CalculateBoundingRect(RectInt[] rects)
    {
        if (rects == null || rects.Length == 0)
        {
            return default;
        }

        int xMin = rects[0].xMin;
        int yMin = rects[0].yMin;
        int xMax = rects[0].xMax;
        int yMax = rects[0].yMax;

        for (int i = 1; i < rects.Length; i++)
        {
            xMin = Mathf.Min(xMin, rects[i].xMin);
            yMin = Mathf.Min(yMin, rects[i].yMin);
            xMax = Mathf.Max(xMax, rects[i].xMax);
            yMax = Mathf.Max(yMax, rects[i].yMax);
        }

        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private bool IsPointInsideActiveRegion(int x, int y)
    {
        if (activeRegionRects == null || activeRegionRects.Length == 0)
        {
            return x >= VisibleContentRect.x &&
                   x < VisibleContentRect.xMax &&
                   y >= VisibleContentRect.y &&
                   y < VisibleContentRect.yMax;
        }

        for (int i = 0; i < activeRegionRects.Length; i++)
        {
            if (activeRegionRects[i].Contains(new Vector2Int(x, y)))
            {
                return true;
            }
        }

        return false;
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private IntPtr ResolvePlayerWindowHandle()
    {
        IntPtr bestWindow = IntPtr.Zero;
        int bestScore = int.MinValue;

        ConsiderPlayerWindow(GetActiveWindow(), ref bestWindow, ref bestScore);
        ConsiderPlayerWindow(GetForegroundWindow(), ref bestWindow, ref bestScore);

        EnumWindows((hWnd, _) =>
        {
            ConsiderPlayerWindow(hWnd, ref bestWindow, ref bestScore);
            return true;
        }, IntPtr.Zero);

        return bestWindow;
    }

    private void ConsiderPlayerWindow(IntPtr hWnd, ref IntPtr bestWindow, ref int bestScore)
    {
        int candidateScore = ScorePlayerWindow(hWnd);
        if (candidateScore <= bestScore)
        {
            return;
        }

        bestWindow = hWnd;
        bestScore = candidateScore;
    }

    private int ScorePlayerWindow(IntPtr hWnd)
    {
        if (!IsCurrentProcessWindow(hWnd) || !GetClientRect(hWnd, out Rect rect))
        {
            return int.MinValue;
        }

        int width = rect.right - rect.left;
        int height = rect.bottom - rect.top;
        if (width <= 0 || height <= 0)
        {
            return int.MinValue;
        }

        int score = Mathf.Min(width * height, 1_000_000);
        if (MatchesExpectedWindowSize(width, height))
        {
            score += 2_000_000;
        }

        if (MatchesProductName(hWnd))
        {
            score += 500_000;
        }

        if (hWnd == GetForegroundWindow())
        {
            score += 100_000;
        }

        if (hWnd == GetActiveWindow())
        {
            score += 50_000;
        }

        return score;
    }

    private bool MatchesExpectedWindowSize(int width, int height)
    {
        const int sizeTolerance = 16;
        int expectedWidth = Mathf.Max(1, Screen.width);
        int expectedHeight = Mathf.Max(1, Screen.height);

        return Mathf.Abs(width - expectedWidth) <= sizeTolerance &&
               Mathf.Abs(height - expectedHeight) <= sizeTolerance;
    }

    private static bool MatchesProductName(IntPtr hWnd)
    {
        string productName = Application.productName;
        if (string.IsNullOrWhiteSpace(productName))
        {
            return false;
        }

        string windowTitle = GetWindowTitle(hWnd);
        return windowTitle.IndexOf(productName, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        int titleLength = GetWindowTextLength(hWnd);
        if (titleLength <= 0)
        {
            return string.Empty;
        }

        StringBuilder titleBuilder = new StringBuilder(titleLength + 1);
        GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
        return titleBuilder.ToString();
    }

    private static bool IsCurrentProcessWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero || !IsWindowVisible(hWnd))
        {
            return false;
        }

        GetWindowThreadProcessId(hWnd, out uint ownerProcessId);
        return ownerProcessId == GetCurrentProcessId();
    }
#endif
}
