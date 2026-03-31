using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[AddComponentMenu("桌宠/GPT-SoVITS客户端")]
[RequireComponent(typeof(AudioSource))]
public class GPTSoVITS客户端 : MonoBehaviour
{
    [Header("接口")]
    [SerializeField, FormerlySerializedAs("apiUrl"), InspectorName("接口地址")]
    private string 接口地址 = "http://127.0.0.1:9880/tts";
    [SerializeField, FormerlySerializedAs("requestTimeoutSeconds"), InspectorName("请求超时秒数")]
    private int 请求超时秒数 = 90;

    [Header("参考音频")]
    [SerializeField, FormerlySerializedAs("referenceAudioPath"), InspectorName("参考音频路径")]
    private string 参考音频路径 = string.Empty;
    [TextArea(2, 4)]
    [SerializeField, FormerlySerializedAs("promptText"), InspectorName("参考文本")]
    private string 参考文本 = string.Empty;
    [SerializeField, FormerlySerializedAs("promptLanguage"), InspectorName("参考语言")]
    private string 参考语言 = "zh";
    [SerializeField, FormerlySerializedAs("textLanguage"), InspectorName("合成语言")]
    private string 合成语言 = "zh";

    [Header("语音参数")]
    [SerializeField, FormerlySerializedAs("textSplitMethod"), InspectorName("文本切分方式")]
    private string 文本切分方式 = "cut5";
    [SerializeField, FormerlySerializedAs("speedFactor"), InspectorName("语速倍率")]
    private float 语速倍率 = 1f;
    [SerializeField, FormerlySerializedAs("topK"), InspectorName("Top K")]
    private int topK = 15;
    [SerializeField, FormerlySerializedAs("topP"), InspectorName("Top P")]
    private float topP = 1f;
    [SerializeField, FormerlySerializedAs("temperature"), InspectorName("Temperature")]
    private float temperature = 1f;

    [Header("播放")]
    [SerializeField, FormerlySerializedAs("audioSource"), InspectorName("语音音频源")]
    private AudioSource 语音音频源;
    [SerializeField, FormerlySerializedAs("autoPlayOnSynthesis"), InspectorName("合成后自动播放")]
    private bool 合成后自动播放 = true;

    public string apiUrl { get => 接口地址; set => 接口地址 = value; }
    public string referenceAudioPath { get => 参考音频路径; set => 参考音频路径 = value; }
    public string promptText { get => 参考文本; set => 参考文本 = value; }
    public string promptLanguage { get => 参考语言; set => 参考语言 = value; }
    public string textLanguage { get => 合成语言; set => 合成语言 = value; }
    public AudioSource audioSource { get => 语音音频源; set => 语音音频源 = value; }

    private bool requestInFlight;
    private AudioClip 当前运行时音频;

    [Serializable]
    private class TtsRequest
    {
        public string text;
        public string text_lang;
        public string ref_audio_path;
        public string prompt_lang;
        public string prompt_text;
        public string text_split_method;
        public int batch_size;
        public float speed_factor;
        public int top_k;
        public float top_p;
        public float temperature;
        public string media_type;
        public bool streaming_mode;
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

    public bool 已完成接口配置()
    {
        return !string.IsNullOrWhiteSpace(接口地址);
    }

    public void 请求语音播放(string 文本, Action<float> onSuccess, Action<string> onError)
    {
        自动补齐引用();

        if (requestInFlight)
        {
            onError?.Invoke("语音还在生成中，请稍等一下。");
            return;
        }

        string cleanText = string.IsNullOrWhiteSpace(文本) ? string.Empty : 文本.Trim();
        if (string.IsNullOrEmpty(cleanText))
        {
            onError?.Invoke("要合成的文本不能为空。");
            return;
        }

        if (!已完成接口配置())
        {
            onError?.Invoke("请先配置 GPT-SoVITS 的接口地址。");
            return;
        }

        if (语音音频源 != null && 语音音频源.isPlaying)
        {
            语音音频源.Stop();
        }

        StartCoroutine(请求语音播放协程(cleanText, onSuccess, onError));
    }

    private void 自动补齐引用()
    {
        if (语音音频源 == null)
        {
            语音音频源 = GetComponent<AudioSource>();
        }

        if (语音音频源 == null)
        {
            语音音频源 = gameObject.AddComponent<AudioSource>();
        }

        if (语音音频源 != null)
        {
            语音音频源.playOnAwake = false;
            语音音频源.loop = false;
            语音音频源.spatialBlend = 0f;
        }
    }

    private IEnumerator 请求语音播放协程(string 文本, Action<float> onSuccess, Action<string> onError)
    {
        requestInFlight = true;

        string 可用参考音频路径 = string.Empty;
        if (!尝试准备参考音频路径(out 可用参考音频路径, out string 准备错误))
        {
            requestInFlight = false;
            onError?.Invoke(准备错误);
            yield break;
        }

        string 有效参考文本 = 获取有效参考文本();
        byte[] bodyRaw = Encoding.UTF8.GetBytes(构建请求体Json(文本, 可用参考音频路径, 有效参考文本));

        using (UnityWebRequest request = new UnityWebRequest(接口地址.Trim(), UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(接口地址.Trim(), AudioType.WAV);
            request.timeout = Mathf.Max(5, 请求超时秒数);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            requestInFlight = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorText = 读取错误响应(request);
                onError?.Invoke(string.IsNullOrWhiteSpace(errorText) ? "语音合成失败。" : errorText);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                onError?.Invoke("语音合成成功了，但没有拿到音频数据。");
                yield break;
            }

            if (当前运行时音频 != null && 当前运行时音频 != clip)
            {
                Destroy(当前运行时音频);
            }

            当前运行时音频 = clip;

            if (语音音频源 != null)
            {
                语音音频源.clip = clip;
                if (合成后自动播放)
                {
                    语音音频源.Play();
                }
            }

            float duration = clip.length > 0.01f ? clip.length : 0.01f;
            onSuccess?.Invoke(duration);
        }
    }

    private string 获取有效参考文本()
    {
        if (!string.IsNullOrWhiteSpace(参考文本))
        {
            return 参考文本.Trim();
        }

        if (string.IsNullOrWhiteSpace(参考音频路径))
        {
            return string.Empty;
        }

        string fileName = Path.GetFileNameWithoutExtension(参考音频路径.Trim());
        return string.IsNullOrWhiteSpace(fileName) ? string.Empty : fileName.Trim();
    }

    private bool 尝试准备参考音频路径(out string 可用路径, out string 错误信息)
    {
        可用路径 = string.Empty;
        错误信息 = string.Empty;

        string sourcePath = string.IsNullOrWhiteSpace(参考音频路径) ? string.Empty : 参考音频路径.Trim();
        if (string.IsNullOrEmpty(sourcePath))
        {
            return true;
        }

        try
        {
            sourcePath = Path.GetFullPath(sourcePath);
        }
        catch (Exception exception)
        {
            错误信息 = "参考音频路径无效：" + exception.Message;
            return false;
        }

        if (!File.Exists(sourcePath))
        {
            错误信息 = "参考音频不存在：" + sourcePath;
            return false;
        }

        string extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".wav";
        }

        string tempDirectory = Path.Combine(Path.GetTempPath(), "codex-desktop-pet-tts");
        string fingerprint = 生成参考音频指纹(sourcePath);
        string tempPath = Path.Combine(tempDirectory, "reference-audio-" + fingerprint + extension.ToLowerInvariant());

        try
        {
            Directory.CreateDirectory(tempDirectory);
            File.Copy(sourcePath, tempPath, true);
            可用路径 = tempPath;
            return true;
        }
        catch (Exception exception)
        {
            错误信息 = "复制参考音频到临时目录失败：" + exception.Message;
            return false;
        }
    }

    private string 构建请求体Json(string 文本, string 可用参考音频路径, string 有效参考文本)
    {
        StringBuilder builder = new StringBuilder(512);
        builder.Append('{');
        bool hasField = false;

        添加Json字段(builder, ref hasField, "text", 文本);
        添加Json字段(builder, ref hasField, "text_lang", 规范化语言代码(合成语言));
        添加Json字段(builder, ref hasField, "text_split_method", string.IsNullOrWhiteSpace(文本切分方式) ? "cut5" : 文本切分方式.Trim());
        添加Json数字字段(builder, ref hasField, "batch_size", 1);
        添加Json数字字段(builder, ref hasField, "speed_factor", Mathf.Max(0.4f, 语速倍率));
        添加Json数字字段(builder, ref hasField, "top_k", Mathf.Max(1, topK));
        添加Json数字字段(builder, ref hasField, "top_p", Mathf.Clamp(topP, 0.05f, 1f));
        添加Json数字字段(builder, ref hasField, "temperature", Mathf.Clamp(temperature, 0.05f, 2f));
        添加Json字段(builder, ref hasField, "media_type", "wav");
        添加Json布尔字段(builder, ref hasField, "streaming_mode", false);

        添加Json字段(
            builder,
            ref hasField,
            "ref_audio_path",
            string.IsNullOrWhiteSpace(可用参考音频路径) ? string.Empty : 可用参考音频路径);

        if (!string.IsNullOrWhiteSpace(有效参考文本))
        {
            添加Json字段(builder, ref hasField, "prompt_lang", 规范化语言代码(参考语言));
            添加Json字段(builder, ref hasField, "prompt_text", 有效参考文本);
        }
        else
        {
            添加Json字段(builder, ref hasField, "prompt_text", string.Empty);
        }

        builder.Append('}');
        return builder.ToString();
    }

    private static void 添加Json字段(StringBuilder builder, ref bool hasField, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || value == null)
        {
            return;
        }

        if (hasField)
        {
            builder.Append(',');
        }

        builder.Append('"').Append(EscapeJson(key)).Append("\":");
        builder.Append('"').Append(EscapeJson(value)).Append('"');
        hasField = true;
    }

    private static void 添加Json数字字段(StringBuilder builder, ref bool hasField, string key, int value)
    {
        if (hasField)
        {
            builder.Append(',');
        }

        builder.Append('"').Append(EscapeJson(key)).Append("\":").Append(value);
        hasField = true;
    }

    private static void 添加Json数字字段(StringBuilder builder, ref bool hasField, string key, float value)
    {
        if (hasField)
        {
            builder.Append(',');
        }

        builder.Append('"').Append(EscapeJson(key)).Append("\":").Append(value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        hasField = true;
    }

    private static void 添加Json布尔字段(StringBuilder builder, ref bool hasField, string key, bool value)
    {
        if (hasField)
        {
            builder.Append(',');
        }

        builder.Append('"').Append(EscapeJson(key)).Append("\":").Append(value ? "true" : "false");
        hasField = true;
    }

    private static string 读取错误响应(UnityWebRequest request)
    {
        if (request == null)
        {
            return "request is null";
        }

        string code = $"HTTP {request.responseCode}";
        string error = string.IsNullOrWhiteSpace(request.error) ? "Unknown error" : request.error.Trim();
        string contentType = request.GetResponseHeader("Content-Type") ?? string.Empty;

        try
        {
            byte[] data = request.downloadHandler?.data;
            if (data != null && data.Length > 0)
            {
                if (contentType.IndexOf("json", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    contentType.IndexOf("text", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string body = Encoding.UTF8.GetString(data).Trim('\uFEFF', '\0', ' ', '\r', '\n', '\t');
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        return $"{code} | {error} | {body}";
                    }
                }

                return $"{code} | {error} | Content-Type={contentType} | {data.Length} bytes";
            }
        }
        catch
        {
        }

        return $"{code} | {error}";
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(value.Length + 16);
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
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
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
                default:
                    if (char.IsControl(c))
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4"));
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

    private static string 生成参考音频指纹(string sourcePath)
    {
        FileInfo info = new FileInfo(sourcePath);
        string fingerprintSource = sourcePath + "|" + info.Length + "|" + info.LastWriteTimeUtc.Ticks;
        byte[] bytes = Encoding.UTF8.GetBytes(fingerprintSource);
        byte[] hash;
        using (SHA1 sha1 = SHA1.Create())
        {
            hash = sha1.ComputeHash(bytes);
        }
        StringBuilder builder = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
        {
            builder.Append(hash[i].ToString("x2"));
        }

        return builder.ToString();
    }

    private static string 规范化语言代码(string 原始值)
    {
        string normalized = string.IsNullOrWhiteSpace(原始值) ? "zh" : 原始值.Trim().ToLowerInvariant();
        return normalized;
    }
}
