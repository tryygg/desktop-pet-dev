using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

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
    [SerializeField, InspectorName("密钥文件路径")]
    private string 密钥文件路径 = "Assets/New Folder/秘钥.txt";

    public string apiUrl { get => 接口地址; set => 接口地址 = value; }
    public string apiKey { get => 接口密钥; set => 接口密钥 = value; }
    public string model { get => 模型名称; set => 模型名称 = value; }

    [TextArea(2, 5)]
    [SerializeField, FormerlySerializedAs("systemPrompt"), InspectorName("系统提示词")]
    private string 系统提示词 = "\u4f60\u662f\u4e00\u4e2a\u684c\u9762 AI \u52a9\u624b\uff0c\u56de\u7b54\u8bf7\u7b80\u77ed\u3001\u53e3\u8bed\u5316\u3002";

    public string systemPrompt { get => 系统提示词; set => 系统提示词 = value; }

    private bool requestInFlight;
    private sealed class CurlRequestResult
    {
        public bool success;
        public long statusCode;
        public string body;
        public string error;
    }

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
        尝试从密钥文件加载接口密钥();
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
        尝试从密钥文件加载接口密钥();
        return !string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey);
    }

    public void 请求聊天回复(string userText, Action<string> onSuccess, Action<string> onError, bool 播放桌宠反馈 = true)
    {
        自动补齐引用();
        尝试从密钥文件加载接口密钥();

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
        尝试从密钥文件加载接口密钥();

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
        string requestUrl = BuildChatCompletionsUrl();

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

        using (UnityWebRequest request = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 45;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (ShouldUseCurlFallback(request))
                {
                    yield return PostRequestWithCurl(requestUrl, JsonUtility.ToJson(payload), result =>
                    {
                        if (result == null)
                        {
                            return;
                        }

                        if (result.success)
                        {
                            string curlContent = ExtractContentFromJson(result.body);
                            if (!string.IsNullOrEmpty(curlContent))
                            {
                                SetResponseText(curlContent);
                                if (播放桌宠反馈)
                                {
                                    PlayTalkingFeedback();
                                }

                                requestInFlight = false;
                                onSuccess?.Invoke(curlContent);
                                return;
                            }
                        }

                        string curlError = ExtractErrorMessage(result.body);
                        if (string.IsNullOrEmpty(curlError))
                        {
                            curlError = string.IsNullOrWhiteSpace(result.error)
                                ? "网络连接失败。"
                                : result.error;
                        }

                        SetResponseText(curlError);
                        requestInFlight = false;
                        if (播放桌宠反馈)
                        {
                            ResetPetFeedback();
                        }

                        onError?.Invoke(curlError);
                    });

                    if (!requestInFlight)
                    {
                        yield break;
                    }
                }

                string errorMessage = ExtractErrorMessage(request.downloadHandler.text);
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = string.IsNullOrWhiteSpace(request.error)
                        ? "\u7f51\u7edc\u8fde\u63a5\u5931\u8d25\u3002"
                        : "Error: " + request.error;
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

    private static bool ShouldUseCurlFallback(UnityWebRequest request)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (request == null)
        {
            return false;
        }

        string error = request.error ?? string.Empty;
        if (request.responseCode > 0)
        {
            return false;
        }

        return error.IndexOf("SSL", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("TLS", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("SecureChannel", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("transport stream", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("Authentication failed", StringComparison.OrdinalIgnoreCase) >= 0;
#else
        return false;
#endif
    }

    private IEnumerator PostRequestWithCurl(string requestUrl, string payloadJson, Action<CurlRequestResult> onComplete)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        string tempJsonPath = Path.Combine(Path.GetTempPath(), "desktop-pet-gemini-request.json");
        try
        {
            File.WriteAllText(tempJsonPath, payloadJson, new UTF8Encoding(false));
        }
        catch (Exception exception)
        {
            onComplete?.Invoke(new CurlRequestResult
            {
                success = false,
                statusCode = 0,
                body = string.Empty,
                error = "写入临时请求文件失败: " + exception.Message
            });
            yield break;
        }

        Process process = null;
        string stdout = string.Empty;
        string stderr = string.Empty;
        CurlRequestResult failureResult = null;
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "curl.exe",
                Arguments =
                    "-k -sS -X POST " +
                    BuildCurlProxyArguments(requestUrl) +
                    QuoteArgument(requestUrl) + " " +
                    "-H " + QuoteArgument("Authorization: Bearer " + apiKey.Trim()) + " " +
                    "-H " + QuoteArgument("Content-Type: application/json") + " " +
                    "--data-binary " + QuoteArgument("@" + tempJsonPath) + " " +
                    "-w " + QuoteArgument("\\n__HTTP_CODE__:%{http_code}"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            ApplyProxyEnvironment(startInfo);

            process = new Process { StartInfo = startInfo };
            process.Start();
        }
        catch (Exception exception)
        {
            failureResult = new CurlRequestResult
            {
                success = false,
                statusCode = 0,
                body = string.Empty,
                error = "curl 回退失败: " + exception.Message
            };
        }

        if (failureResult != null)
        {
            if (process != null)
            {
                process.Dispose();
            }

            try
            {
                if (File.Exists(tempJsonPath))
                {
                    File.Delete(tempJsonPath);
                }
            }
            catch
            {
            }

            onComplete?.Invoke(failureResult);
            yield break;
        }

        while (process != null && !process.HasExited)
        {
            yield return null;
        }

        try
        {
            if (process != null)
            {
                stdout = process.StandardOutput.ReadToEnd();
                stderr = process.StandardError.ReadToEnd();
            }
        }
        catch (Exception exception)
        {
            onComplete?.Invoke(new CurlRequestResult
            {
                success = false,
                statusCode = 0,
                body = string.Empty,
                error = "读取 curl 结果失败: " + exception.Message
            });
            yield break;
        }
        finally
        {
            if (process != null)
            {
                process.Dispose();
            }

            try
            {
                if (File.Exists(tempJsonPath))
                {
                    File.Delete(tempJsonPath);
                }
            }
            catch
            {
            }
        }

        CurlRequestResult result = ParseCurlResult(stdout, stderr);
        if (result.success)
        {
            onComplete?.Invoke(result);
            yield break;
        }
        if (!result.success)
        {
            Debug.LogWarning("Gemini 请求已回退到 curl.exe，但仍失败。 " + (result.error ?? string.Empty));
        }
        else
        {
            Debug.LogWarning("Gemini 请求已回退到 curl.exe 并成功返回。");
        }

        onComplete?.Invoke(result);
#else
        onComplete?.Invoke(new CurlRequestResult
        {
            success = false,
            statusCode = 0,
            body = string.Empty,
            error = "当前平台不支持 curl 回退。"
        });
        yield break;
#endif
    }

    private static CurlRequestResult ParseCurlResult(string stdout, string stderr)
    {
        string output = stdout ?? string.Empty;
        const string marker = "__HTTP_CODE__:";
        int markerIndex = output.LastIndexOf(marker, StringComparison.Ordinal);
        long statusCode = 0;
        string body = output;
        if (markerIndex >= 0)
        {
            string statusText = output.Substring(markerIndex + marker.Length).Trim();
            long.TryParse(statusText, out statusCode);
            body = output.Substring(0, markerIndex).Trim();
        }

        bool success = statusCode >= 200 && statusCode < 300;
        string error = string.Empty;
        if (!success)
        {
            error = !string.IsNullOrWhiteSpace(stderr)
                ? stderr.Trim()
                : (statusCode > 0 ? "HTTP " + statusCode : "curl 请求失败。");
        }

        return new CurlRequestResult
        {
            success = success,
            statusCode = statusCode,
            body = body,
            error = error
        };
    }

    private static string QuoteArgument(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private static string BuildCurlProxyArguments(string requestUrl)
    {
        string proxy = GetProxyForRequest(requestUrl);
        if (string.IsNullOrWhiteSpace(proxy))
        {
            return string.Empty;
        }

        return "--proxy " + QuoteArgument(proxy.Trim()) + " ";
    }

    private static void ApplyProxyEnvironment(ProcessStartInfo startInfo)
    {
        if (startInfo == null)
        {
            return;
        }

        CopyEnvironmentVariable(startInfo, "HTTPS_PROXY");
        CopyEnvironmentVariable(startInfo, "HTTP_PROXY");
        CopyEnvironmentVariable(startInfo, "ALL_PROXY");
        CopyEnvironmentVariable(startInfo, "https_proxy");
        CopyEnvironmentVariable(startInfo, "http_proxy");
        CopyEnvironmentVariable(startInfo, "all_proxy");
    }

    private static void CopyEnvironmentVariable(ProcessStartInfo startInfo, string name)
    {
        string value = ReadEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            startInfo.EnvironmentVariables[name] = value.Trim();
        }
    }

    private static string GetProxyForRequest(string requestUrl)
    {
        bool isHttps = !string.IsNullOrWhiteSpace(requestUrl) &&
                       requestUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        if (isHttps)
        {
            string httpsProxy = ReadEnvironmentVariable("HTTPS_PROXY", "https_proxy");
            if (!string.IsNullOrWhiteSpace(httpsProxy))
            {
                return httpsProxy;
            }
        }

        string httpProxy = ReadEnvironmentVariable("HTTP_PROXY", "http_proxy");
        if (!string.IsNullOrWhiteSpace(httpProxy))
        {
            return httpProxy;
        }

        return ReadEnvironmentVariable("ALL_PROXY", "all_proxy");
    }

    private static string ReadEnvironmentVariable(params string[] names)
    {
        if (names == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            string processValue = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(processValue))
            {
                return processValue.Trim();
            }

            string userValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
            if (!string.IsNullOrWhiteSpace(userValue))
            {
                return userValue.Trim();
            }

            string machineValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
            if (!string.IsNullOrWhiteSpace(machineValue))
            {
                return machineValue.Trim();
            }
        }

        return string.Empty;
    }

    private string BuildChatCompletionsUrl()
    {
        string configuredUrl = string.IsNullOrWhiteSpace(apiUrl) ? string.Empty : apiUrl.Trim();
        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            return "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
        }

        if (configuredUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return configuredUrl;
        }

        if (configuredUrl.EndsWith("/openai", StringComparison.OrdinalIgnoreCase))
        {
            return configuredUrl + "/chat/completions";
        }

        if (configuredUrl.EndsWith("/openai/", StringComparison.OrdinalIgnoreCase))
        {
            return configuredUrl + "chat/completions";
        }

        if (configuredUrl.EndsWith("/v1beta", StringComparison.OrdinalIgnoreCase) ||
            configuredUrl.EndsWith("/v1beta/", StringComparison.OrdinalIgnoreCase))
        {
            return configuredUrl.TrimEnd('/') + "/openai/chat/completions";
        }

        return configuredUrl;
    }

    private void 尝试从密钥文件加载接口密钥()
    {
        string resolvedPath = 解析密钥文件路径();
        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(resolvedPath, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                string candidate = lines[i] != null ? lines[i].Trim() : string.Empty;
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    接口密钥 = candidate;
                    return;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("读取本地密钥文件失败: " + exception.Message);
        }
    }

    private string 解析密钥文件路径()
    {
        string configuredPath = string.IsNullOrWhiteSpace(密钥文件路径) ? string.Empty : 密钥文件路径.Trim();
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return 查找默认密钥文件();
        }

        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        string normalizedRelativePath = configuredPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        List<string> candidates = new List<string>();

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (!string.IsNullOrWhiteSpace(projectRoot))
        {
            candidates.Add(Path.GetFullPath(Path.Combine(projectRoot, normalizedRelativePath)));
        }

        string currentDirectory = Environment.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(currentDirectory))
        {
            candidates.Add(Path.GetFullPath(Path.Combine(currentDirectory, normalizedRelativePath)));
        }

        string executableDirectory = 获取可执行文件目录();
        if (!string.IsNullOrWhiteSpace(executableDirectory))
        {
            candidates.Add(Path.GetFullPath(Path.Combine(executableDirectory, normalizedRelativePath)));
        }

        string persistentDirectory = Application.persistentDataPath;
        if (!string.IsNullOrWhiteSpace(persistentDirectory))
        {
            candidates.Add(Path.GetFullPath(Path.Combine(persistentDirectory, normalizedRelativePath)));
        }

        string fileName = Path.GetFileName(normalizedRelativePath);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            if (!string.IsNullOrWhiteSpace(currentDirectory))
            {
                candidates.Add(Path.Combine(currentDirectory, fileName));
            }

            if (!string.IsNullOrWhiteSpace(executableDirectory))
            {
                candidates.Add(Path.Combine(executableDirectory, fileName));
            }

            if (!string.IsNullOrWhiteSpace(persistentDirectory))
            {
                candidates.Add(Path.Combine(persistentDirectory, fileName));
            }
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            if (File.Exists(candidates[i]))
            {
                return candidates[i];
            }
        }

        string fallback = 查找默认密钥文件();
        return string.IsNullOrWhiteSpace(fallback) ? (candidates.Count > 0 ? candidates[0] : normalizedRelativePath) : fallback;
    }

    private string 查找默认密钥文件()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
        {
            return string.Empty;
        }

        string[] candidateFiles = Directory.GetFiles(projectRoot, "秘钥.txt", SearchOption.AllDirectories);
        if (candidateFiles.Length > 0)
        {
            return candidateFiles[0];
        }

        candidateFiles = Directory.GetFiles(projectRoot, "密钥.txt", SearchOption.AllDirectories);
        return candidateFiles.Length > 0 ? candidateFiles[0] : string.Empty;
    }

    private static string 获取可执行文件目录()
    {
        try
        {
            string executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule != null
                ? System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
                : string.Empty;
            return string.IsNullOrWhiteSpace(executablePath) ? string.Empty : Path.GetDirectoryName(executablePath);
        }
        catch
        {
            return string.Empty;
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

        try
        {
            ApiErrorEnvelope errorEnvelope = JsonUtility.FromJson<ApiErrorEnvelope>(json);
            if (!string.IsNullOrWhiteSpace(errorEnvelope?.error?.message))
            {
                return errorEnvelope.error.message.Trim();
            }
        }
        catch
        {
        }

        Match match = Regex.Match(json, "\"message\"\\s*:\\s*\"(?<message>(?:\\\\.|[^\"])*)\"", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return string.Empty;
        }

        string value = match.Groups["message"].Value;
        return value.Replace("\\n", "\n").Replace("\\\"", "\"").Trim();
    }
}
