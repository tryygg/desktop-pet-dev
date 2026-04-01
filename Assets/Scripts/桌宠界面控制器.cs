using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class 桌宠界面控制器 : MonoBehaviour
{
    [Header("界面宿主")]
    [SerializeField, InspectorName("界面画布")]
    private Canvas 界面画布;
    [SerializeField, InspectorName("菜单根节点")]
    private RectTransform 菜单根节点;
    [SerializeField, InspectorName("菜单项根节点")]
    private RectTransform 菜单项根节点;
    [SerializeField, InspectorName("聊天面板根节点")]
    private RectTransform 聊天面板根节点;
    [SerializeField, InspectorName("历史面板根节点")]
    private RectTransform 历史面板根节点;
    [SerializeField, InspectorName("气泡根节点")]
    private RectTransform 气泡根节点;
    [SerializeField, InspectorName("气泡主体根节点")]
    private RectTransform 气泡主体根节点;
    [SerializeField, InspectorName("气泡尾巴根节点")]
    private RectTransform 气泡尾巴根节点;
    [SerializeField, InspectorName("加载遮罩根节点")]
    private RectTransform 加载遮罩根节点;

    [Header("文本")]
    [SerializeField, InspectorName("菜单标题文本")]
    private Text 菜单标题文本;
    [SerializeField, InspectorName("聊天标题文本")]
    private Text 聊天标题文本;
    [SerializeField, InspectorName("历史标题文本")]
    private Text 历史标题文本;
    [SerializeField, InspectorName("历史内容文本")]
    private Text 历史内容文本;
    [SerializeField, InspectorName("气泡文本")]
    private Text 气泡文本;
    [SerializeField, InspectorName("加载文本")]
    private Text 加载文本;
    [SerializeField, InspectorName("输入框占位文本")]
    private Text 输入框占位文本;
    [SerializeField, InspectorName("语音按钮文本")]
    private Text 语音按钮文本;

    [Header("控件")]
    [SerializeField, InspectorName("语音按钮图标")]
    private Image 语音按钮图标;
    [SerializeField, InspectorName("输入框")]
    private InputField 输入框;
    [SerializeField, InspectorName("历史滚动视图")]
    private ScrollRect 历史滚动视图;
    [SerializeField, InspectorName("聊天面板背景")]
    private Image 聊天面板背景;
    [SerializeField, InspectorName("历史面板背景")]
    private Image 历史面板背景;
    [SerializeField, InspectorName("气泡背景")]
    private Image 气泡背景;
    [SerializeField, InspectorName("气泡尾巴背景")]
    private Image 气泡尾巴背景;
    [SerializeField, InspectorName("加载遮罩背景")]
    private Image 加载遮罩背景;
    [SerializeField, InspectorName("气泡描边")]
    private Outline 气泡描边;
    [SerializeField, InspectorName("历史按钮")]
    private Button 历史按钮;
    [SerializeField, InspectorName("发送按钮")]
    private Button 发送按钮;
    [SerializeField, InspectorName("关闭按钮")]
    private Button 关闭按钮;
    [SerializeField, InspectorName("语音按钮")]
    private Button 语音按钮;
    [SerializeField, InspectorName("历史关闭按钮")]
    private Button 历史关闭按钮;

    public Canvas 界面Canvas => 界面画布;
    public RectTransform 菜单Root => 菜单根节点;
    public RectTransform 菜单ItemsRoot => 菜单项根节点;
    public RectTransform 聊天面板Root => 聊天面板根节点;
    public RectTransform 历史面板Root => 历史面板根节点;
    public RectTransform 气泡Root => 气泡根节点;
    public RectTransform 气泡主体Root => 气泡主体根节点;
    public RectTransform 气泡尾巴Root => 气泡尾巴根节点;
    public RectTransform 加载遮罩Root => 加载遮罩根节点;
    public Text 菜单标题Text => 菜单标题文本;
    public Text 聊天标题Text => 聊天标题文本;
    public Text 历史标题Text => 历史标题文本;
    public Text 历史内容Text => 历史内容文本;
    public Text 气泡Text => 气泡文本;
    public Text 加载Text => 加载文本;
    public Text 输入框占位Text => 输入框占位文本;
    public Text 语音按钮Label => 语音按钮文本;
    public Image 语音按钮Icon => 语音按钮图标;
    public InputField 聊天输入框 => 输入框;
    public ScrollRect 历史ScrollRect => 历史滚动视图;
    public Image 聊天面板Background => 聊天面板背景;
    public Image 历史面板Background => 历史面板背景;
    public Image 气泡Background => 气泡背景;
    public Image 气泡尾巴Background => 气泡尾巴背景;
    public Image 加载遮罩Background => 加载遮罩背景;
    public Outline 气泡Outline => 气泡描边;
    public Button 历史Button => 历史按钮;
    public Button 发送Button => 发送按钮;
    public Button 关闭Button => 关闭按钮;
    public Button 语音Button => 语音按钮;
    public Button 历史关闭Button => 历史关闭按钮;

    private void Reset()
    {
        自动绑定();
    }

    private void OnValidate()
    {
        自动绑定();
    }

    public void 自动绑定()
    {
        if (界面画布 == null)
        {
            界面画布 = GetComponent<Canvas>();
        }

        if (菜单根节点 == null)
        {
            菜单根节点 = transform.Find("MenuRoot") as RectTransform;
        }

        if (菜单项根节点 == null && 菜单根节点 != null)
        {
            菜单项根节点 = 菜单根节点.Find("Items") as RectTransform;
        }

        if (聊天面板根节点 == null)
        {
            聊天面板根节点 = transform.Find("ChatPanel") as RectTransform;
        }

        if (历史面板根节点 == null)
        {
            历史面板根节点 = transform.Find("ChatHistoryPanel") as RectTransform;
        }

        if (气泡根节点 == null)
        {
            气泡根节点 = transform.Find("ChatBubble") as RectTransform;
        }

        if (气泡主体根节点 == null && 气泡根节点 != null)
        {
            气泡主体根节点 = 气泡根节点.Find("BubbleBody") as RectTransform;
        }

        if (气泡尾巴根节点 == null && 气泡根节点 != null)
        {
            气泡尾巴根节点 = 气泡根节点.Find("BubbleTail") as RectTransform;
        }

        if (加载遮罩根节点 == null)
        {
            加载遮罩根节点 = transform.Find("StartupLoadingOverlay") as RectTransform;
        }

        if (菜单标题文本 == null && 菜单根节点 != null)
        {
            菜单标题文本 = 菜单根节点.Find("Header")?.GetComponent<Text>();
        }

        if (聊天标题文本 == null && 聊天面板根节点 != null)
        {
            聊天标题文本 = 聊天面板根节点.Find("ChatHeader")?.GetComponent<Text>();
        }

        if (历史标题文本 == null && 历史面板根节点 != null)
        {
            历史标题文本 = 历史面板根节点.Find("ChatHistoryHeader")?.GetComponent<Text>();
        }

        if (历史内容文本 == null && 历史面板根节点 != null)
        {
            历史内容文本 = 历史面板根节点.Find("ChatHistoryViewport/ChatHistory")?.GetComponent<Text>();
        }

        if (气泡文本 == null && 气泡主体根节点 != null)
        {
            气泡文本 = 气泡主体根节点.Find("BubbleText")?.GetComponent<Text>();
        }

        if (加载文本 == null && 加载遮罩根节点 != null)
        {
            加载文本 = 加载遮罩根节点.Find("LoadingText")?.GetComponent<Text>();
        }

        if (聊天面板根节点 != null)
        {
            Transform 输入背景 = 聊天面板根节点.Find("ChatInputBackground");
            if (输入框 == null && 输入背景 != null)
            {
                输入框 = 输入背景.GetComponent<InputField>();
            }

            if (输入框占位文本 == null && 输入背景 != null)
            {
                输入框占位文本 = 输入背景.Find("Placeholder")?.GetComponent<Text>();
            }

            if (历史按钮 == null)
            {
                历史按钮 = 聊天面板根节点.Find("ChatHistoryButton")?.GetComponent<Button>();
            }

            if (关闭按钮 == null)
            {
                关闭按钮 = 聊天面板根节点.Find("ChatCloseButton")?.GetComponent<Button>();
            }

            if (发送按钮 == null)
            {
                发送按钮 = 聊天面板根节点.Find("ChatSendButton")?.GetComponent<Button>();
            }

            if (语音按钮 == null)
            {
                语音按钮 = 聊天面板根节点.Find("ChatVoiceButton")?.GetComponent<Button>();
            }

            if (语音按钮文本 == null)
            {
                语音按钮文本 = 聊天面板根节点.Find("ChatVoiceButton/Label")?.GetComponent<Text>();
            }

            if (语音按钮图标 == null)
            {
                语音按钮图标 = 聊天面板根节点.Find("ChatVoiceButton/Icon")?.GetComponent<Image>();
            }
        }

        if (历史面板根节点 != null)
        {
            if (历史关闭按钮 == null)
            {
                历史关闭按钮 = 历史面板根节点.Find("ChatHistoryCloseButton")?.GetComponent<Button>();
            }

            if (历史滚动视图 == null)
            {
                历史滚动视图 = 历史面板根节点.GetComponent<ScrollRect>();
            }
        }

        if (聊天面板背景 == null && 聊天面板根节点 != null)
        {
            聊天面板背景 = 聊天面板根节点.GetComponent<Image>();
        }

        if (历史面板背景 == null && 历史面板根节点 != null)
        {
            历史面板背景 = 历史面板根节点.GetComponent<Image>();
        }

        if (气泡背景 == null && 气泡主体根节点 != null)
        {
            气泡背景 = 气泡主体根节点.GetComponent<Image>();
        }

        if (气泡尾巴背景 == null && 气泡尾巴根节点 != null)
        {
            气泡尾巴背景 = 气泡尾巴根节点.GetComponent<Image>();
        }

        if (加载遮罩背景 == null && 加载遮罩根节点 != null)
        {
            加载遮罩背景 = 加载遮罩根节点.GetComponent<Image>();
        }

        if (气泡描边 == null && 气泡主体根节点 != null)
        {
            气泡描边 = 气泡主体根节点.GetComponent<Outline>();
        }
    }
}
