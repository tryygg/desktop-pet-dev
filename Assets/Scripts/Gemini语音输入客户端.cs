using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[AddComponentMenu("桌宠/Gemini语音输入客户端")]
public class Gemini语音输入客户端 : MonoBehaviour
{
    [Header("接口")]
    [SerializeField, FormerlySerializedAs("serviceUrl"), InspectorName("接口地址")]
    private string 接口地址 = "https://generativelanguage.googleapis.com/v1beta";
    [SerializeField, InspectorName("秘钥文件路径")]
    private string 秘钥文件路径 = "Assets/New Folder/秘钥.txt";
    [SerializeField, FormerlySerializedAs("requestTimeoutSeconds"), InspectorName("请求超时秒数")]
    private int 请求超时秒数 = 120;
    [SerializeField, FormerlySerializedAs("modelName"), InspectorName("识别模型名称")]
    private string 识别模型名称 = "gemini-3-flash-preview";
    [TextArea(2, 4)]
    [SerializeField, FormerlySerializedAs("transcribePrompt"), InspectorName("转写提示词")]
    private string 转写提示词 = "请直接输出这段音频里的原文，不要加解释，不要总结，不要说多余的话。";

    [Header("录音")]
    [SerializeField, FormerlySerializedAs("microphoneDeviceName"), InspectorName("麦克风设备名")]
    private string 麦克风设备名 = string.Empty;
    [SerializeField, FormerlySerializedAs("sampleRate"), InspectorName("录音采样率")]
    private int 录音采样率 = 16000;
    [SerializeField, FormerlySerializedAs("maxRecordSeconds"), InspectorName("最长录音秒数")]
    private int 最长录音秒数 = 20;
    [SerializeField, FormerlySerializedAs("minimumRecordSeconds"), InspectorName("最短录音秒数")]
    private float 最短录音秒数 = 0.35f;
    [SerializeField, InspectorName("静音均方根阈值")]
    private float 静音均方根阈值 = 0.008f;
    [SerializeField, InspectorName("静音峰值阈值")]
    private float 静音峰值阈值 = 0.045f;
    [SerializeField, InspectorName("有效人声采样阈值")]
    private float 有效人声采样阈值 = 0.02f;
    [SerializeField, InspectorName("有效人声占比阈值")]
    private float 有效人声占比阈值 = 0.003f;

    public bool 正在录音 => 正在录音中;
    public bool 正在转写 => 转写请求进行中;

    private string 当前麦克风设备 = string.Empty;
    private AudioClip 当前录音片段;
    private float 开始录音时间;
    private bool 正在录音中;
    private bool 转写请求进行中;

    [Serializable]
    private class GeminiCandidate
    {
        public GeminiResponseContent content;
    }

    [Serializable]
    private class GeminiResponseContent
    {
        public GeminiResponsePart[] parts;
    }

    [Serializable]
    private class GeminiResponsePart
    {
        public string text;
    }

    [Serializable]
    private class GeminiError
    {
        public string message;
    }

    [Serializable]
    private class GeminiGenerateResponse
    {
        public GeminiCandidate[] candidates;
        public GeminiError error;
    }

    private void Awake()
    {
        自动选择麦克风设备();
    }

    private void OnDisable()
    {
        if (正在录音中)
        {
            停止麦克风(true);
        }
    }

    public bool 已完成接口配置()
    {
        return !string.IsNullOrWhiteSpace(接口地址) && !string.IsNullOrWhiteSpace(读取ApiKey());
    }

    public bool 开始录音(out string error)
    {
        error = string.Empty;

        if (转写请求进行中)
        {
            error = "语音还在识别中，请稍等一下。";
            return false;
        }

        if (正在录音中)
        {
            error = "已经在录音了。";
            return false;
        }

        if (!已完成接口配置())
        {
            error = "Gemini 语音识别配置还没准备好。";
            return false;
        }

        if (!自动选择麦克风设备())
        {
            error = "没有找到可用的麦克风。";
            return false;
        }

        int sampleRate = Mathf.Max(8000, 录音采样率);
        int durationSeconds = Mathf.Max(1, 最长录音秒数);

        当前录音片段 = Microphone.Start(当前麦克风设备, false, durationSeconds, sampleRate);
        if (当前录音片段 == null)
        {
            error = "麦克风启动失败。";
            return false;
        }

        开始录音时间 = Time.realtimeSinceStartup;
        正在录音中 = true;
        return true;
    }

    public void 停止录音并转写(Action<string> onSuccess, Action<string> onError)
    {
        if (!正在录音中)
        {
            onError?.Invoke("现在没有正在进行的录音。");
            return;
        }

        转写请求进行中 = true;
        StartCoroutine(停止录音并转写协程(onSuccess, onError));
    }

    public void 取消录音()
    {
        停止麦克风(true);
    }

    private IEnumerator 停止录音并转写协程(Action<string> onSuccess, Action<string> onError)
    {
        yield return null;

        int sampleCount = 获取已录制样本数();
        float recordedSeconds = Time.realtimeSinceStartup - 开始录音时间;

        AudioClip trimmedClip = 提取录音片段(sampleCount);
        停止麦克风(false);

        if (trimmedClip == null || sampleCount <= 0 || recordedSeconds < 最短录音秒数)
        {
            转写请求进行中 = false;
            if (trimmedClip != null)
            {
                Destroy(trimmedClip);
            }

            onError?.Invoke("这次录音太短了，你再说一遍吧。");
            yield break;
        }

        if (!检测到有效人声(trimmedClip, sampleCount))
        {
            转写请求进行中 = false;
            Destroy(trimmedClip);
            onError?.Invoke("我没听到你说话。");
            yield break;
        }

        byte[] wavBytes = 编码为Wav(trimmedClip, sampleCount);
        Destroy(trimmedClip);
        if (wavBytes == null || wavBytes.Length == 0)
        {
            转写请求进行中 = false;
            onError?.Invoke("录音保存失败了。");
            yield break;
        }

        string apiKey = 读取ApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            转写请求进行中 = false;
            onError?.Invoke("Gemini API Key 没有找到。");
            yield break;
        }

        using (UnityWebRequest request = 创建Gemini请求(wavBytes, apiKey))
        {
            yield return request.SendWebRequest();
            转写请求进行中 = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(读取错误响应(request));
                yield break;
            }

            string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (string.IsNullOrWhiteSpace(body))
            {
                onError?.Invoke("Gemini 没有返回内容。");
                yield break;
            }

            GeminiGenerateResponse response = null;
            try
            {
                response = JsonUtility.FromJson<GeminiGenerateResponse>(body);
            }
            catch (Exception exception)
            {
                onError?.Invoke("Gemini 返回格式解析失败：" + exception.Message);
                yield break;
            }

            string text = 提取回复文本(response);
            if (string.IsNullOrWhiteSpace(text))
            {
                string errorMessage = response != null && response.error != null && !string.IsNullOrWhiteSpace(response.error.message)
                    ? response.error.message.Trim()
                    : "我这次没听清，你再说一遍吧。";
                onError?.Invoke(errorMessage);
                yield break;
            }

            onSuccess?.Invoke(text.Trim());
        }
    }

    private UnityWebRequest 创建Gemini请求(byte[] wavBytes, string apiKey)
    {
        string model = string.IsNullOrWhiteSpace(识别模型名称) ? "gemini-3-flash-preview" : 识别模型名称.Trim();
        string prompt = string.IsNullOrWhiteSpace(转写提示词)
            ? "请直接输出这段音频里的原文，不要加解释，不要总结，不要说多余的话。"
            : 转写提示词.Trim();

        string url = 接口地址.TrimEnd('/') + "/models/" + UnityWebRequest.EscapeURL(model) + ":generateContent?key=" + UnityWebRequest.EscapeURL(apiKey);
        string bodyJson =
            "{\"contents\":[{\"parts\":["
            + "{\"text\":\"" + EscapeJson(prompt) + "\"},"
            + "{\"inline_data\":{\"mime_type\":\"audio/wav\",\"data\":\"" + Convert.ToBase64String(wavBytes) + "\"}}"
            + "]}]}";
        byte[] body = Encoding.UTF8.GetBytes(bodyJson);

        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.Max(10, 请求超时秒数);
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            switch (c)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                default:
                    if (c < 32)
                    {
                        builder.Append("\\u");
                        builder.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    private string 读取ApiKey()
    {
        string key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
        {
            return key.Trim();
        }

        string keyFilePath = 解析秘钥文件路径();
        if (string.IsNullOrWhiteSpace(keyFilePath) || !File.Exists(keyFilePath))
        {
            return string.Empty;
        }

        try
        {
            string[] lines = File.ReadAllLines(keyFilePath, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = string.IsNullOrWhiteSpace(lines[i]) ? string.Empty : lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    return line;
                }
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private string 解析秘钥文件路径()
    {
        if (string.IsNullOrWhiteSpace(秘钥文件路径))
        {
            return 查找默认密钥文件();
        }

        string configuredPath = 秘钥文件路径.Trim();
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
        List<string> searchRoots = new List<string>();

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (!string.IsNullOrWhiteSpace(projectRoot) && Directory.Exists(projectRoot))
        {
            searchRoots.Add(projectRoot);
        }

        string currentDirectory = Environment.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(currentDirectory) && Directory.Exists(currentDirectory) && !searchRoots.Contains(currentDirectory))
        {
            searchRoots.Add(currentDirectory);
        }

        string executableDirectory = 获取可执行文件目录();
        if (!string.IsNullOrWhiteSpace(executableDirectory) && Directory.Exists(executableDirectory) && !searchRoots.Contains(executableDirectory))
        {
            searchRoots.Add(executableDirectory);
        }

        string persistentDirectory = Application.persistentDataPath;
        if (!string.IsNullOrWhiteSpace(persistentDirectory) && Directory.Exists(persistentDirectory) && !searchRoots.Contains(persistentDirectory))
        {
            searchRoots.Add(persistentDirectory);
        }

        for (int i = 0; i < searchRoots.Count; i++)
        {
            string root = searchRoots[i];
            string[] secretFiles = Directory.GetFiles(root, "秘钥.txt", SearchOption.AllDirectories);
            if (secretFiles.Length > 0)
            {
                return secretFiles[0];
            }

            secretFiles = Directory.GetFiles(root, "密钥.txt", SearchOption.AllDirectories);
            if (secretFiles.Length > 0)
            {
                return secretFiles[0];
            }
        }

        return string.Empty;
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

    private static string 提取回复文本(GeminiGenerateResponse response)
    {
        if (response == null || response.candidates == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < response.candidates.Length; i++)
        {
            GeminiCandidate candidate = response.candidates[i];
            if (candidate == null || candidate.content == null || candidate.content.parts == null)
            {
                continue;
            }

            for (int j = 0; j < candidate.content.parts.Length; j++)
            {
                string text = candidate.content.parts[j] != null ? candidate.content.parts[j].text : null;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (builder.Length > 0)
                    {
                        builder.Append('\n');
                    }

                    builder.Append(text.Trim());
                }
            }
        }

        return builder.ToString().Trim();
    }

    private bool 自动选择麦克风设备()
    {
        if (!string.IsNullOrWhiteSpace(麦克风设备名))
        {
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                if (string.Equals(Microphone.devices[i], 麦克风设备名, StringComparison.OrdinalIgnoreCase))
                {
                    当前麦克风设备 = Microphone.devices[i];
                    return true;
                }
            }
        }

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            当前麦克风设备 = string.Empty;
            return false;
        }

        当前麦克风设备 = Microphone.devices[0];
        return true;
    }

    private int 获取已录制样本数()
    {
        if (!正在录音中 || 当前录音片段 == null)
        {
            return 0;
        }

        int position = Microphone.GetPosition(当前麦克风设备);
        if (position < 0)
        {
            position = 0;
        }

        return Mathf.Clamp(position, 0, 当前录音片段.samples);
    }

    private AudioClip 提取录音片段(int sampleCount)
    {
        if (当前录音片段 == null || sampleCount <= 0)
        {
            return null;
        }

        int channelCount = Mathf.Max(1, 当前录音片段.channels);
        float[] samples = new float[sampleCount * channelCount];
        当前录音片段.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create(
            "GeminiVoiceInput",
            sampleCount,
            channelCount,
            当前录音片段.frequency,
            false);
        trimmedClip.SetData(samples, 0);
        return trimmedClip;
    }

    private bool 检测到有效人声(AudioClip clip, int sampleCount)
    {
        if (clip == null || sampleCount <= 0)
        {
            return false;
        }

        int channelCount = Mathf.Max(1, clip.channels);
        float[] samples = new float[sampleCount * channelCount];
        clip.GetData(samples, 0);

        double energy = 0d;
        float peak = 0f;
        int voicedCount = 0;
        float gate = Mathf.Max(0.001f, 有效人声采样阈值);

        for (int i = 0; i < samples.Length; i++)
        {
            float absolute = Mathf.Abs(samples[i]);
            energy += absolute * absolute;
            if (absolute > peak)
            {
                peak = absolute;
            }

            if (absolute >= gate)
            {
                voicedCount++;
            }
        }

        float rms = samples.Length > 0 ? Mathf.Sqrt((float)(energy / samples.Length)) : 0f;
        float voicedRatio = samples.Length > 0 ? voicedCount / (float)samples.Length : 0f;

        bool clearlySilent = peak < Mathf.Max(0.001f, 静音峰值阈值)
                             && rms < Mathf.Max(0.0005f, 静音均方根阈值)
                             && voicedRatio < Mathf.Max(0.0005f, 有效人声占比阈值);

        return !clearlySilent;
    }

    private void 停止麦克风(bool destroyClip)
    {
        if (!string.IsNullOrWhiteSpace(当前麦克风设备))
        {
            try
            {
                if (Microphone.IsRecording(当前麦克风设备))
                {
                    Microphone.End(当前麦克风设备);
                }
            }
            catch
            {
            }
        }

        正在录音中 = false;
        if (destroyClip && 当前录音片段 != null)
        {
            Destroy(当前录音片段);
        }

        当前录音片段 = null;
    }

    private static byte[] 编码为Wav(AudioClip clip, int sampleCount)
    {
        if (clip == null || sampleCount <= 0)
        {
            return null;
        }

        int channelCount = Mathf.Max(1, clip.channels);
        int sampleRate = Mathf.Max(8000, clip.frequency);
        float[] samples = new float[sampleCount * channelCount];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];
        const float rescaleFactor = 32767f;

        for (int i = 0; i < samples.Length; i++)
        {
            float clamped = Mathf.Clamp(samples[i], -1f, 1f);
            intData[i] = (short)Mathf.RoundToInt(clamped * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        using (MemoryStream stream = new MemoryStream(44 + bytesData.Length))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + bytesData.Length);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channelCount);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channelCount * 2);
            writer.Write((short)(channelCount * 2));
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(bytesData.Length);
            writer.Write(bytesData);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static string 读取错误响应(UnityWebRequest request)
    {
        if (request == null)
        {
            return "request is null";
        }

        string code = $"HTTP {request.responseCode}";
        string error = string.IsNullOrWhiteSpace(request.error) ? "Unknown error" : request.error.Trim();

        try
        {
            byte[] data = request.downloadHandler?.data;
            if (data != null && data.Length > 0)
            {
                string body = Encoding.UTF8.GetString(data).Trim('\uFEFF', '\0', ' ', '\r', '\n', '\t');
                if (!string.IsNullOrWhiteSpace(body))
                {
                    return $"{code} | {error} | {body}";
                }
            }
        }
        catch
        {
        }

        return $"{code} | {error}";
    }
}
