# CODEBUDDY.md This file provides guidance to CodeBuddy when working with code in this repository.

## 常用命令

### 编译项目
```bash
dotnet build          # Debug 配置编译
dotnet build -c Release  # Release 配置编译
```

### 运行项目
```bash
dotnet run            # 以控制台模式运行，启动全局键盘监听
```

### 发布项目
```bash
# 自包含发布（目标系统无需安装 .NET Runtime）
dotnet publish -c Release -r win-x64 --self-contained

# 框架依赖发布（目标系统需安装 .NET 9 Runtime）
dotnet publish -c Release -r win-x64
```
发布输出位于 `SN_DesktopService/bin/Release/net9.0-windows/publish/`。

### 调试
本项目无单元测试。调试方式：运行 `dotnet run` 后在任意窗口按 `Ctrl+K`，观察控制台按键日志输出及 Form1 窗体是否弹出。需要管理员权限运行以确保全局键盘钩子正常工作。

---

## 项目架构

### 整体定位

SN_DesktopService 是一个 **Windows 全局键盘监听控制台应用**，目标框架 `.NET 9.0-windows`（仅支持 Windows 7+），输出类型为 `Exe`，启用 Windows Forms。项目的核心能力是通过 Windows API 低级键盘钩子（`WH_KEYBOARD_LL`）在操作系统层面拦截所有键盘按键事件，并在检测到特定组合键时触发预设操作。

项目无任何外部 NuGet 包依赖，仅使用 `Microsoft.NETCore.App` 和 `Microsoft.WindowsDesktop.App.WindowsForms` 框架库。

### 线程模型（关键设计）

整个应用运行在全双线程架构下：

1. **主线程**：`Program.cs` 中执行 `while(true) { Thread.Sleep(20); }` 无限循环，唯一职责是**保持进程不退出**。每 20ms 轮询一次，CPU 占用极低。
2. **消息泵线程**：由 `M_GlobalKeyListener.Start()` 创建的后台线程（`IsBackground = true`），在此线程中安装 Windows 钩子并调用 `Application.Run()` 启动 WinForms 消息循环。钩子回调事件在该线程上触发。

**为什么需要两个线程？** 控制台应用默认没有 Windows 消息循环，而 `WH_KEYBOARD_LL` 低级钩子要求消息泵才能接收回调。`M_GlobalKeyListener` 通过 `Environment.UserInteractive && Console.OpenStandardInput(1) != null` 判断是否为控制台应用，如果是则自动创建后台线程承载消息循环；如果是 GUI 应用则直接在主线程安装钩子。

### 四层架构关系

```
Program.cs（入口层）
    │  创建 TestService 实例并调用 Start()
    │  主线程进入 while(true) 保活循环
    ▼
TestService（业务逻辑层）
    │  订阅 M_GlobalKeyListener 事件
    │  实现 OnKeyDown/OnKeyUp 处理器
    │  Ctrl+K → 创建/显示 Form1（单例模式）
    ▼
M_GlobalKeyListener（键盘钩子引擎层）
    │  P/Invoke 调用 user32.dll / kernel32.dll
    │  安装 WH_KEYBOARD_LL 全局钩子
    │  后台线程启动 Application.Run() 消息循环
    │  钩子回调中触发 KeyDown/KeyUp 事件
    ▼
Form1（GUI 展现层）
    │  Windows Forms 空窗体 386×312 像素
    │  Form1_Load 中定位到主屏幕右上角
```

### M_GlobalKeyListener —— 核心引擎详解

这是整个项目中最重要的类，位于**全局命名空间**（非 `SN_DesktopService` 命名空间），修饰符为 `public static`，完全静态。

**P/Invoke 声明（4 个 Windows API）**：

| API | DLL | 用途 |
|-----|-----|------|
| `SetWindowsHookEx` | user32.dll | 安装钩子，`idHook=13`(WH_KEYBOARD_LL)，`dwThreadId=0` 表示全局 |
| `UnhookWindowsHookEx` | user32.dll | 卸载钩子 |
| `CallNextHookEx` | user32.dll | 将消息传递给钩子链中的下一个钩子 |
| `GetModuleHandle` | kernel32.dll | 获取当前进程主模块句柄 |

**Windows 消息常量**：

| 常量 | 值 | 用途 |
|------|-----|------|
| `WH_KEYBOARD_LL` | 13 | 低级键盘钩子类型号 |
| `WM_KEYDOWN` | 0x0100 | 非系统键按下 |
| `WM_KEYUP` | 0x0101 | 非系统键释放 |
| `WM_SYSKEYDOWN` | 0x0104 | 系统键按下（如 Alt） |
| `WM_SYSKEYUP` | 0x0105 | 系统键释放 |

**钩子安装流程（`SetHook` 内部方法）**：
1. `Process.GetCurrentProcess().MainModule` 获取当前进程主模块
2. `GetModuleHandle(curModule.ModuleName)` 获取模块句柄
3. `SetWindowsHookEx(WH_KEYBOARD_LL, proc, moduleHandle, 0)` 安装全局钩子

**钩子回调流程（`HookCallback`）**：
- `nCode < 0` 时不处理，直接调用 `CallNextHookEx` 传递
- `nCode >= 0` 时从 `lParam` 读取 `vkCode`（虚拟键码）
- 转为 `Keys` 枚举，根据 `wParam` 判断 KeyDown 还是 KeyUp
- 触发 `KeyDown?.Invoke()` 或 `KeyUp?.Invoke()` 事件

**停止流程（`Stop`）**：
- 调用 `UnhookWindowsHookEx` 卸载钩子
- 如果后台消息线程存活，调用 `Application.Exit()` 退出消息循环
- 等待线程最多 500ms 后返回

### TestService —— 业务逻辑层

命名空间 `SN_DesktopService`，持有静态字段 `form1` 作为 Form1 的单例引用。

**事件处理器逻辑**：
- `OnKeyDown`：用 `Control.ModifierKeys == Keys.Control` 判断 Ctrl 是否按下（区别于检查 `Keys.ControlKey`，`ModifierKeys` 工作在全局钩子线程中同样有效）
- `OnKeyUp`：仅输出日志

**Ctrl+K 单例逻辑**：如果 `form1` 不为 null 且未释放（`!IsDisposed`），直接返回不做任何操作；否则创建新实例并调用 `form1.Show()` 非模态显示。

### 潜在问题与注意事项

1. **命名空间不一致**：`M_GlobalKeyListener` 在全局命名空间，而 `TestService`、`Form1` 在 `SN_DesktopService` 命名空间。使用时无需 `using` 即可访问监听器。
2. **静态事件泄漏**：`TestService` 的方法均为 `static`，且订阅了 `M_GlobalKeyListener` 的静态事件。即使在 `TestService` 实例被 GC 回收后事件处理仍然存活——这在本项目是预期行为，因为只有唯一实例且进程运行期间不卸载。
3. **Form1_Load 冗余代码**：先设位置为 `(0, 0)` 再覆盖为右上角，前者代码无效。
4. **无优雅退出机制**：只能通过 Ctrl+C 终止进程或任务管理器结束。无退出快捷键。
5. **权限要求**：`WH_KEYBOARD_LL` 在某些 Windows 安全策略下可能需要管理员权限才能正常工作。
6. **`.csproj` 项目名 vs 文件夹名**：项目文件名为 `keyboardListenService.csproj`，但文件夹名为 `SN_DesktopService`。解决方案中的项目显示名为 `keyboardListenService`。
