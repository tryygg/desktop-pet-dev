using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Networking;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class 大模型客户端 : MonoBehaviour
{
    [Header("界面")]
    [SerializeField, FormerlySerializedAs("userInputField"), InspectorName("用户输入框")]
    private InputField 用户输入框;
    [SerializeField, FormerlySerializedAs("responseText"), InspectorName("回复文本")]
    private Text 回复文本;

    public InputField userInputField { get => 用户输入框; set => 用户输入框 = value; }
    public Text responseText { get => 回复文本; set => 回复文本 = value; }

    [Header("桌宠反馈")]
    [SerializeField, FormerlySerializedAs("petController"), InspectorName("桌宠控制器")]
    private 桌宠控制器 桌宠控制器引用;
    [SerializeField, FormerlySerializedAs("petAnimator"), InspectorName("桌宠动画器")]
    private Animator 桌宠动画器;
    [SerializeField, FormerlySerializedAs("thinkingState"), InspectorName("思考状态名")]
    private string 思考状态名 = "\u6c89\u601d";
    [SerializeField, FormerlySerializedAs("talkingStates"), InspectorName("说话状态列表")]
    private string[] 说话状态列表 = { "\u8bf4\u8bdd1", "\u8bf4\u8bdd2", "\u8bf4\u8bdd3" };
    [SerializeField, FormerlySerializedAs("thinkingTrigger"), InspectorName("思考触发器")]
    private string 思考触发器 = string.Empty;
    [SerializeField, FormerlySerializedAs("talkingTrigger"), InspectorName("说话触发器")]
    private string 说话触发器 = string.Empty;

    public 桌宠控制器 petController { get => 桌宠控制器引用; set => 桌宠控制器引用 = value; }
    public Animator petAnimator { get => 桌宠动画器; set => 桌宠动画器 = value; }
    public string thinkingState { get => 思考状态名; set => 思考状态名 = value; }
    public string[] talkingStates { get => 说话状态列表; set => 说话状态列表 = value; }
    public string thinkingTrigger { get => 思考触发器; set => 思考触发器 = value; }
    public string talkingTrigger { get => 说话触发器; set => 说话触发器 = value; }

    [Header("接口")]
    [SerializeField, FormerlySerializedAs("apiUrl"), InspectorName("接口地址")]
    private string 接口地址 = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    [SerializeField, FormerlySerializedAs("apiKey"), InspectorName("接口密钥")]
    private string 接口密钥 = string.Empty;
    [SerializeField, FormerlySerializedAs("model"), InspectorName("模型名称")]
    private string 模型名称 = "gemini-3-flash-preview";

    public string apiUrl { get => 接口地址; set => 接口地址 = value; }
    public string apiKey { get => 接口密钥; set => 接口密钥 = value; }
    public string model { get => 模型名称; set => 模型名称 = value; }

    [TextArea(2, 5)]
    [SerializeField, FormerlySerializedAs("systemPrompt"), InspectorName("系统提示词")]
    private string 系统提示词 = "\u4f60\u662f\u4e00\u4e2a\u684c\u9762 AI \u52a9\u624b\uff0c\u56de\u7b54\u8bf7\u7b80\u77ed\u3001\u53e3\u8bed\u5316\u3002";

    public string systemPrompt { get => 系统提示词; set => 系统提示词 = value; }

    private bool requestInFlight;

    [Serializable]
    private class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class ChatCompletionRequest
    {
        public string model;
        public ChatMessage[] messages;
    }

    [Serializable]
    private class ChatChoice
    {
        public ChatMessage message;
    }

    [Serializable]
    private class ChatCompletionResponse
    {
        public ChatChoice[] choices;
    }

    [Serializable]
    private class ApiErrorEnvelope
    {
        public ApiErrorDetail error;
    }

    [Serializable]
    private class ApiErrorDetail
    {
        public string message;
    }

    private void Reset()
    {
        自动补齐引用();
    }

    private void Awake()
    {
        自动补齐引用();
    }

    private void OnValidate()
    {
        自动补齐引用();
    }

    private void 自动补齐引用()
    {
        if (桌宠控制器引用 == null)
        {
            桌宠控制器引用 = GetComponent<桌宠控制器>();
        }

        if (桌宠控制器引用 == null)
        {
            桌宠控制器引用 = GetComponentInParent<桌宠控制器>();
        }

        if (桌宠控制器引用 == null)
        {
            桌宠控制器引用 = FindObjectOfType<桌宠控制器>();
        }

        if (桌宠动画器 == null && 桌宠控制器引用 != null)
        {
            桌宠动画器 = 桌宠控制器引用.characterAnimator;
        }

        if (桌宠动画器 == null)
        {
            桌宠动画器 = GetComponent<Animator>();
        }

        if (桌宠动画器 == null)
        {
            桌宠动画器 = GetComponentInChildren<Animator>(true);
        }
    }

    public bool 已完成接口配置()
    {
        return !string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey);
    }

    public void 请求聊天回复(string userText, Action<string> onSuccess, Action<string> onError, bool 播放桌宠反馈 = true)
    {
        自动补齐引用();

        if (requestInFlight)
        {
            onError?.Invoke("我还在思考上一条消息，稍等一下哦。");
            return;
        }

        string trimmedMessage = userText != null ? userText.Trim() : string.Empty;
        if (string.IsNullOrEmpty(trimmedMessage))
        {
            onError?.Invoke("消息不能为空。");
            return;
        }

        if (!已完成接口配置())
        {
            onError?.Invoke("请先配置 API 地址和密钥。");
            return;
        }

        if (播放桌宠反馈)
        {
            PlayThinkingFeedback();
        }

        StartCoroutine(PostRequest(trimmedMessage, onSuccess, onError, 播放桌宠反馈));
    }

    public void SendMessageToAI()
    {
        自动补齐引用();

        if (requestInFlight)
        {
            return;
        }

        string userText = userInputField != null ? userInputField.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(userText))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            SetResponseText("\u8bf7\u5148\u914d\u7f6e API \u5730\u5740\u548c\u5bc6\u94a5\u3002");
            return;
        }

        if (userInputField != null)
        {
            userInputField.text = string.Empty;
        }

        SetResponseText("\u601d\u8003\u4e2d...");
        请求聊天回复(
            userText,
            content => SetResponseText(content),
            error => SetResponseText(error),
            true);
    }

    private IEnumerator PostRequest(string message, Action<string> onSuccess, Action<string> onError, bool 播放桌宠反馈)
    {
        requestInFlight = true;

        ChatCompletionRequest payload = new ChatCompletionRequest
        {
            model = model,
            messages = new[]
            {
                new ChatMessage
                {
                    role = "system",
                    content = systemPrompt
                },
                new ChatMessage
                {
                    role = "user",
                    content = message
                }
            }
        };

        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 45;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = ExtractErrorMessage(request.downloadHandler.text);
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = "\u7f51\u7edc\u8fde\u63a5\u5931\u8d25\u3002";
                }

                SetResponseText(errorMessage);
                requestInFlight = false;
                if (播放桌宠反馈)
                {
                    ResetPetFeedback();
                }

                onError?.Invoke(errorMessage);
                yield break;
            }

            string content = ExtractContentFromJson(request.downloadHandler.text);
            if (string.IsNullOrEmpty(content))
            {
                SetResponseText("\u89e3\u6790\u5931\u8d25\u3002");
                requestInFlight = false;
                if (播放桌宠反馈)
                {
                    ResetPetFeedback();
                }

                onError?.Invoke("\u89e3\u6790\u5931\u8d25\u3002");
                yield break;
            }

            SetResponseText(content);
            if (播放桌宠反馈)
            {
                PlayTalkingFeedback();
            }

            requestInFlight = false;
            onSuccess?.Invoke(content);
            yield break;
        }
    }

    private void PlayThinkingFeedback()
    {
        if (petController != null && petController.TryPlayAnimationState(thinkingState))
        {
            return;
        }

        if (petAnimator != null && !string.IsNullOrWhiteSpace(thinkingTrigger))
        {
            petAnimator.SetTrigger(thinkingTrigger);
        }
    }

    private void PlayTalkingFeedback()
    {
        if (petController != null && petController.TryPlayFirstAvailableState(talkingStates))
        {
            return;
        }

        if (petAnimator != null && !string.IsNullOrWhiteSpace(talkingTrigger))
        {
            petAnimator.SetTrigger(talkingTrigger);
        }
    }

    private void ResetPetFeedback()
    {
        if (petController != null)
        {
            petController.PlayInitialState();
        }
    }

    private void SetResponseText(string text)
    {
        if (responseText != null)
        {
            responseText.text = text;
        }
    }

    private static string ExtractContentFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        ChatCompletionResponse response = JsonUtility.FromJson<ChatCompletionResponse>(json);
        if (response?.choices == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < response.choices.Length; i++)
        {
            string content = response.choices[i]?.message?.content;
            if (!string.IsNullOrWhiteSpace(content))
            {
                return content.Trim();
            }
        }

        return string.Empty;
    }

    private static string ExtractErrorMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        ApiErrorEnvelope errorEnvelope = JsonUtility.FromJson<ApiErrorEnvelope>(json);
        return errorEnvelope?.error?.message?.Trim() ?? string.Empty;
    }
}
