using System;
using UnityEngine;

public class 说明文档 : ScriptableObject
{
    [InspectorName("图标")]
    public Texture2D icon;
    [InspectorName("标题")]
    public string title;
    [InspectorName("章节列表")]
    public 章节[] sections;
    [InspectorName("已加载布局")]
    public bool loadedLayout;

    [Serializable]
    public class 章节
    {
        [InspectorName("章节标题")]
        public string heading;
        [InspectorName("正文内容")]
        public string text;
        [InspectorName("链接文字")]
        public string linkText;
        [InspectorName("链接地址")]
        public string url;
    }
}
