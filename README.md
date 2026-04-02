# 小浣熊桌宠

一个基于 Unity 的Windows 桌面宠物项目。
全部代码由codex大人编写
它现在已经是一只可以常驻桌面、支持动画切换、文字聊天、语音输入和语音播报的小浣熊。

## 预览

- 透明桌宠窗口，常驻桌面
- 右键菜单切换动作
- 对话面板支持文字输入
- 支持 `Enter` 发送，`Alt+Enter` 换行
- 支持 Gemini 语音输入
- 支持 GPT-SoVITS 语音播报
- 支持聊天气泡、聊天历史和本地 UI Canvas

## 功能特性

### 桌宠本体

- 透明底桌面显示
- 鼠标交互、拖动、缩放
- 多个待机 / 说话 / 互动动作
- 对话时自动切换到合适动画

### 聊天系统

- 文字输入聊天
- 语音输入聊天
- 回复气泡展示
- 历史记录面板
- 动画时长和回复展示时长联动

### AI 链路

- `Gemini` 负责文字聊天
- `Gemini` 也可直接接收音频做语音理解
- `GPT-SoVITS` 负责把回复播报出来

## 技术栈

- Unity
- C#
- Win32 透明窗口
- Gemini API
- GPT-SoVITS

## 当前交互流程

### 文字聊天

1. 打开桌宠对话框
2. 输入文字
3. 发送给 Gemini
4. 显示回复气泡
5. 如果启用了 GPT-SoVITS，就同步播报语音

### 语音聊天

1. 点击麦克风按钮开始录音
2. 再点一次结束录音
3. Unity 直接把录下来的 `wav` 发给 Gemini
4. Gemini 返回识别文本
5. 桌宠继续沿用现有聊天链路生成回复

## 项目结构

```text
Assets/
  Scenes/
    SampleScene.unity
  Scripts/
    桌宠控制器.cs
    透明窗口.cs
    大模型客户端.cs
    GPTSoVITS客户端.cs
    Gemini语音输入客户端.cs
  Resources/
    麦克风图标.png
```

## 快速开始

### 1. 打开项目

用 Unity 打开这个仓库，主场景是：

- `Assets/Scenes/SampleScene.unity`

### 2. 配置 Gemini 密钥

推荐两种方式：

- 在编辑器环境里，把 `秘钥.txt` 放在项目里
- 在打包后的环境里，把 `秘钥.txt` 放在 `exe` 同目录

文件内容只需要一行：

```text
你的_GEMINI_API_KEY
```

当前代码会自动查找：

- 环境变量 `GEMINI_API_KEY`
- `exe` 同目录
- 当前工作目录
- `Application.persistentDataPath`
- 项目目录内的 `秘钥.txt / 密钥.txt`

### 3. 配置 GPT-SoVITS

如果你希望桌宠把回复念出来：

1. 先启动本地 GPT-SoVITS 服务
2. 在 Unity 场景里给 `GPTSoVITS客户端` 配好接口地址
3. 运行后，桌宠回复就会自动播报

## 主要脚本说明

### `Assets/Scripts/桌宠控制器.cs`

项目主控制器，负责：

- 动画切换
- 菜单逻辑
- 聊天面板
- 气泡显示
- 文字 / 语音输入总流程

### `Assets/Scripts/透明窗口.cs`

负责 Windows 桌宠透明窗口能力。

### `Assets/Scripts/大模型客户端.cs`

负责文本聊天请求，当前接 Gemini 文本接口。

### `Assets/Scripts/Gemini语音输入客户端.cs`

负责：

- 麦克风录音
- 本地静音拦截
- 把 `wav` 直接提交给 Gemini 做语音理解

### `Assets/Scripts/GPTSoVITS客户端.cs`

负责把回复文字转换成桌宠播报语音。

## 适合继续扩展的方向

- 更完整的桌宠状态机
- 语音输入自动停录
- 音量驱动嘴型 / 表情
- 更丰富的动作和情绪反馈
- 本地模型 / 本地知识库接入

## 说明

这个项目目前偏 MVP + 持续迭代风格，很多交互已经能跑通，但仍然适合继续打磨。

如果你想把它继续做成更完整的 AI 桌面助手，这个仓库已经把最核心的几条链路接起来了：

- 桌宠显示
- 输入
- 大模型回复
- 语音理解
- 语音播报
