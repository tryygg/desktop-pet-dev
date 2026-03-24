using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LLMClient : MonoBehaviour
{
    [Header("UI")]
    public InputField userInputField;
    public Text responseText;

    [Header("Pet Feedback")]
    public DesktopPetController petController;
    public Animator petAnimator;
    public string thinkingState = "\u6c89\u601d";
    public string[] talkingStates = { "\u8bf4\u8bdd1", "\u8bf4\u8bdd2", "\u8bf4\u8bdd3" };
    public string thinkingTrigger = string.Empty;
    public string talkingTrigger = string.Empty;

    [Header("API")]
    public string apiUrl = "https://api.deepseek.com/v1/chat/completions";
    public string apiKey = string.Empty;
    public string model = "deepseek-chat";

    [TextArea(2, 5)]
    public string systemPrompt = "\u4f60\u662f\u4e00\u4e2a\u684c\u9762 AI \u52a9\u624b\uff0c\u56de\u7b54\u8bf7\u7b80\u77ed\u3001\u53e3\u8bed\u5316\u3002";

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

    public void SendMessageToAI()
    {
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

        SetResponseText("\u601d\u8003\u4e2d...");
        PlayThinkingFeedback();
        StartCoroutine(PostRequest(userText));

        if (userInputField != null)
        {
            userInputField.text = string.Empty;
        }
    }

    private IEnumerator PostRequest(string message)
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
                ResetPetFeedback();
                requestInFlight = false;
                yield break;
            }

            string content = ExtractContentFromJson(request.downloadHandler.text);
            if (string.IsNullOrEmpty(content))
            {
                SetResponseText("\u89e3\u6790\u5931\u8d25\u3002");
                ResetPetFeedback();
                requestInFlight = false;
                yield break;
            }

            SetResponseText(content);
            PlayTalkingFeedback();
        }

        requestInFlight = false;
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
