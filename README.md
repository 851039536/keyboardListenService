# SN_DesktopService 使用文档

## 项目概述

一个基于 **.NET 9.0** 的 Windows 桌面应用程序，实现了**全局键盘监听**功能。
该应用能够在后台持续监听系统级别的键盘事件，并根据特定的按键组合触发相应的操作。

主要特性：
  • 全局键盘监听（支持控制台和 GUI 应用）
  • 支持快捷键组合检测（如 `Ctrl+K`）
  • 动态显示 Windows 窗体（右上角弹出）
  • 后台常驻运行，CPU 占用极低
  • 零外部依赖，仅使用 .NET 框架内置 API

---

## 系统要求

操作系统：
  • Windows 7 或更高版本（Windows 10 /11）

开发环境：
  • .NET 9.0 SDK
  • Visual Studio 2022 或其他兼容 C# 的 IDE

---

## 项目结构

```
keyboardListenService/
├── keyboardListenService.sln          # 解决方案文件（VS 2022）
└── SN_DesktopService/
    ├── keyboardListenService.csproj    # 项目文件（net9.0-windows）
    ├── Program.cs                      # 程序入口（顶级语句）
    ├── M_GlobalKeyListener.cs          # 全局键盘钩子引擎（核心）
    ├── TestService.cs                  # 业务服务：事件处理器 + 快捷键逻辑
    ├── Form1.cs                        # 窗体逻辑（定位到右上角）
    ├── Form1.Designer.cs               # 窗体设计器代码（386×312）
    ├── Form1.resx                      # 窗体资源
    ├── Properties/
    │   └── PublishProfiles/            # 发布配置文件
    └── README.md
```

### 各文件职责

| 文件 | 命名空间 | 职责 |
|------|---------|------|
| `Program.cs` | —（顶级语句） | 创建 TestService，启动服务，主线程保活循环 |
| `M_GlobalKeyListener.cs` | 全局命名空间 | P/Invoke 封装，WH_KEYBOARD_LL 钩子安装/卸载/回调 |
| `TestService.cs` | `SN_DesktopService` | 订阅键盘事件，Ctrl+K 组合键检测，Form1 单例管理 |
| `Form1.cs` / `Form1.Designer.cs` | `SN_DesktopService` | Windows Forms 空窗体，定位到主屏幕右上角 |

---

## 架构设计

### 线程模型（双线程架构）

```
主线程 (Program.cs)                后台消息线程 (M_GlobalKeyListener)
┌──────────────────┐               ┌──────────────────────────────┐
│ while(true)      │               │ SetHook(WH_KEYBOARD_LL)      │
│   Thread.Sleep(20)│              │ Application.Run() ← 消息循环  │
│                  │               │     ↓                        │
│ 职责：保活进程    │               │ HookCallback()               │
│ CPU 占用 ~0%     │               │     ↓                        │
└──────────────────┘               │ KeyDown / KeyUp 事件触发      │
                                   │     ↓                        │
                                   │ TestService.OnKeyDown()      │
                                   │     └→ Ctrl+K → Form1.Show() │
                                   └──────────────────────────────┘
```

- **主线程**：`while(true) { Thread.Sleep(20); }` 每 20ms 轮询一次，CPU 占用可忽略不计
- **消息泵线程**：后台线程（`IsBackground = true`），承载 Windows 消息循环和全局钩子回调

**为什么需要双线程？** 控制台应用没有默认消息循环，而 `WH_KEYBOARD_LL` 低级钩子要求消息泵才能接收回调。`M_GlobalKeyListener.Start()` 通过 `Console.OpenStandardInput()` 自动判断运行环境并创建消息线程。

### 数据流

```
用户按键 → Windows 内核 → WH_KEYBOARD_LL 钩子回调
    → HookCallback() 读取 vkCode
    → 触发 KeyDown/KeyUp C# 事件
    → TestService.OnKeyDown()
    → 检测 Ctrl+K → 创建/显示 Form1 窗体
```

---

## 功能说明

### 全局键盘监听

`M_GlobalKeyListener` 提供以下静态事件：

| 事件 | 触发时机 | 参数 |
|------|---------|------|
| `KeyDown` | 键盘按键按下 | `KeyEventArgs` 含 `KeyCode` |
| `KeyUp` | 键盘按键释放 | `KeyEventArgs` 含 `KeyCode` |

使用示例：
```csharp
M_GlobalKeyListener.KeyDown += OnKeyDown;
M_GlobalKeyListener.KeyUp += OnKeyUp;
M_GlobalKeyListener.Start();
```

### 快捷键组合检测

当前应用监听 `Ctrl+K` 组合键：
  • 在任何窗口按下 `Ctrl+K`，自动弹出 Form1 窗体
  • 窗体显示在主屏幕右上角（`Screen.PrimaryScreen.WorkingArea`，避开任务栏）
  • 已打开的窗体会复用，不会重复创建（单例模式）

> **注意**：快捷键检测使用 `Control.ModifierKeys` 而非检查 `Keys.ControlKey`，因为前者即使在全局钩子回调线程上也能正确读取当前 Ctrl 状态。

代码位置：`TestService.cs` → `OnKeyDown` 方法

### 后台运行机制

应用使用线程循环保持后台运行：
```csharp
while(true) { Thread.Sleep(20); }
```
每隔 20 毫秒检查一次，确保进程不退出的同时几乎不消耗 CPU 资源。

---

## 编译和运行

### 编译项目

```bash
# Debug 配置
dotnet build -c Debug

# Release 配置
dotnet build -c Release
```

或在 Visual Studio 2022 中按 `Ctrl+Shift+B` 直接构建。

### 运行应用

```bash
dotnet run
```

应用启动后将在后台运行，监听全局键盘事件。控制台会持续输出按键日志。

### 调试

按 `Ctrl+K` 组合键测试功能：
  1. 打开命令行/终端，`dotnet run` 启动应用
  2. 在**任意窗口**按下 `Ctrl+K`
  3. 观察控制台输出"检测到Ctrl+K组合键"
  4. 观察 Form1 窗体是否在屏幕右上角弹出

> **提示**：如果按键无响应，尝试以管理员身份运行。部分 Windows 安全策略限制低级键盘钩子的注册。

---

## API 参考

### M_GlobalKeyListener 类

全局命名空间中的静态类，所有成员为 `public static`。

**事件**：

```csharp
public static event EventHandler<KeyEventArgs> KeyDown;  // 按键按下
public static event EventHandler<KeyEventArgs> KeyUp;    // 按键释放
```

**方法**：

```csharp
public static void Start();  // 安装 WH_KEYBOARD_LL 钩子，启动消息循环
public static void Stop();   // 卸载钩子，退出消息循环（等待最多 500ms）
```

**内部实现**：

| 组件 | 细节 |
|------|------|
| 钩子类型 | `WH_KEYBOARD_LL`（值 13），全局低级键盘钩子 |
| P/Invoke DLL | `user32.dll`（SetWindowsHookEx/UnhookWindowsHookEx/CallNextHookEx）、`kernel32.dll`（GetModuleHandle） |
| 消息常量 | `WM_KEYDOWN`(0x0100)、`WM_KEYUP`(0x0101)、`WM_SYSKEYDOWN`(0x0104)、`WM_SYSKEYUP`(0x0105) |
| 钩子链 | 回调始终调用 `CallNextHookEx` 传递事件给下一个钩子 |

### TestService 类

命名空间 `SN_DesktopService`。

```csharp
public void Start()  // 订阅键盘事件 + 启动 M_GlobalKeyListener
```

事件处理器：

```csharp
private static void OnKeyDown(object sender, KeyEventArgs e)
    // 输出按键名 + 检测 Ctrl+K 组合键 → 弹出 Form1 窗体

private static void OnKeyUp(object sender, KeyEventArgs e)
    // 输出释放的按键名
```

### Form1 类

命名空间 `SN_DesktopService`，继承自 `Form`。

- 客户端大小：386 × 312 像素
- 默认启动位置：`CenterScreen`（设计器默认值）
- 实际位置：`Form1_Load` 中手动定位到主屏幕右上角（`WorkingArea.Width - this.Width, 0`）
- 非模态显示：`Show()` 而非 `ShowDialog()`

---

## 常见问题

**Q: 应用无法监听到按键**

A: 请检查以下几点：
  1. 以管理员身份运行（右键终端 → 以管理员身份运行）
  2. 检查杀毒软件/安全软件是否拦截了键盘钩子
  3. 确认 Windows 版本 ≥ 7

**Q: 如何修改快捷键组合**

A: 编辑 `TestService.cs` 中的 `OnKeyDown` 方法：
```csharp
// 原代码
if(Control.ModifierKeys == Keys.Control && e.KeyCode == Keys.K)

// 示例：改为 Ctrl+Shift+A
if(Control.ModifierKeys == (Keys.Control | Keys.Shift) && e.KeyCode == Keys.A)
```
`ModifierKeys` 支持按位或组合多个修饰键。

**Q: 如何自定义窗体位置**

A: 编辑 `Form1.cs` 中的 `Form1_Load` 方法：
```csharp
this.Location = new Point(x, y);  // x、y 为屏幕坐标
```
可删除多余的 `this.Location = new Point(0, 0);` 行（该行被后续代码覆盖，无实际效果）。

**Q: 如何停止应用监听**

A: 在代码中调用 `M_GlobalKeyListener.Stop();` 或在终端按 `Ctrl+C` 终止进程。

---

## 发布应用

### 自包含发布

生成独立可执行文件，目标系统无需安装 .NET Runtime：
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### 框架依赖发布

目标系统需安装 .NET 9 Runtime：
```bash
dotnet publish -c Release -r win-x64
```

发布输出目录：`bin/Release/net9.0-windows/publish/`

---

## 技术细节

### 按键监听实现原理

使用 Windows **低级键盘钩子（WH_KEYBOARD_LL）** 实现：

1. **注册**：通过 `SetWindowsHookEx` 将回调函数注册到操作系统钩子链
2. **拦截**：所有键盘事件在到达目标窗口之前先经过钩子链
3. **处理**：在 `HookCallback` 中读取 `vkCode` 虚拟键码，转为 `Keys` 枚举
4. **传递**：调用 `CallNextHookEx` 将事件传递给钩子链中的下一个钩子，确保其他应用也能正常工作
5. **事件**：通过 C# `event` 机制将按键信息传递给订阅者（TestService）

### P/Invoke 技术

项目使用 .NET 平台调用（Platform Invoke）调用 4 个 Windows 原生 API，封装在 `M_GlobalKeyListener` 中：

| API | DLL | 签名 |
|-----|-----|------|
| `SetWindowsHookEx` | user32.dll | `(int idHook, delegate lpfn, IntPtr hMod, uint dwThreadId) → IntPtr` |
| `UnhookWindowsHookEx` | user32.dll | `(IntPtr hhk) → bool` |
| `CallNextHookEx` | user32.dll | `(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam) → IntPtr` |
| `GetModuleHandle` | kernel32.dll | `(string lpModuleName) → IntPtr` |

### 跨平台兼容性

该应用**仅支持 Windows**，核心依赖：
  • Windows Forms（`UseWindowsForms=true`）
  • Windows API P/Invoke 调用（user32.dll、kernel32.dll）

如需跨平台支持，需要将键盘监听模块替换为各平台的本地实现（如 Linux 的 `libinput`/`evdev`，macOS 的 `CGEvent`）。

### 性能考虑

  • 低级键盘钩子性能开销极小，操作系统层面优化
  • 后台循环 20ms 睡眠间隔，CPU 占用 < 0.1%
  • 控制台输出频繁按键日志可能影响性能，生产环境建议移除或限流

---

## 许可证和声明

本项目使用 Windows API 钩子实现全局键盘监听。在生产环境中使用前，请确保了解相关的法律和安全影响。

---

## 联系和反馈

如有问题或建议，请提出 Issue 或 Pull Request。

更新日期：2026 年 6 月 18 日
