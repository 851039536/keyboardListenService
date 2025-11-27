# SN_DesktopService 使用文档

## 项目概述

SN_DesktopService 是一个基于 .NET 9.0 的 Windows 桌面应用程序，实现了全局键盘监听功能。
该应用能够在后台持续监听系统级别的键盘事件，并根据特定的按键组合触发相应的操作。

主要特性：
  • 全局键盘监听（支持控制台和 GUI 应用）
  • 支持快捷键组合检测（如 Ctrl+K）
  • 动态显示 Windows 窗体
  • 后台常驻运行

---

## 系统要求

操作系统：
  • Windows 7 或更高版本（包括 Windows 11 等）

开发环境：
  • .NET 9.0 SDK
  • Visual Studio 2022 或 Qoder IDE（或其他支持 C# 的编辑器）

---

## 项目结构

项目主要文件说明：

Program.cs
  程序入口点，初始化 TestService 并启动服务

Form1.cs / Form1.Designer.cs
  Windows 窗体类，定义了窗口的位置和外观
  默认显示在屏幕右上角

M_GlobalKeyListener.cs
  全局键盘监听工具类，使用 Windows API 钩子实现
  提供 Start() 和 Stop() 方法控制监听

TestService.cs
  服务类，注册按键事件处理器，管理应用逻辑

SN_DesktopService.csproj
  项目文件，定义编译配置和目标框架

---

## 功能说明

### 全局键盘监听

M_GlobalKeyListener 提供以下事件：

  KeyDown 事件
    当键盘按键被按下时触发
    参数：KeyEventArgs 包含 KeyCode（按键代码）

  KeyUp 事件
    当键盘按键被释放时触发
    参数：KeyEventArgs 包含 KeyCode（按键代码）

使用示例：
  M_GlobalKeyListener.KeyDown += OnKeyDown;
  M_GlobalKeyListener.KeyUp += OnKeyUp;
  M_GlobalKeyListener.Start();

### 快捷键组合检测

当前应用监听 Ctrl+K 组合键：
  • 按下 Ctrl+K 时，自动弹出 Form1 窗体
  • 窗体显示在屏幕右上角

代码位置：TestService.cs 中的 OnKeyDown 方法

### 后台运行机制

应用使用线程循环保持后台运行：
  while(true) { Thread.Sleep(20); }

每隔 20 毫秒检查一次，确保监听不中断

---

## 编译和运行

### 编译项目

使用命令行编译：
  dotnet build -c Debug

或在 Visual Studio 中直接构建

### 运行应用

使用命令行运行：
  dotnet run

应用将在后台运行，监听全局键盘事件

### 调试

按 Ctrl+K 组合键测试功能：
  1. 打开命令行/终端
  2. 运行应用
  3. 在任何窗口按下 Ctrl+K
  4. 查看控制台输出和 Form1 窗体显示

---

## API 参考

### M_GlobalKeyListener 类

静态事件：

  public static event EventHandler<KeyEventArgs> KeyDown
    按键按下事件

  public static event EventHandler<KeyEventArgs> KeyUp
    按键释放事件

静态方法：

  public static void Start()
    开始监听键盘事件
    需要在使用前调用

  public static void Stop()
    停止监听键盘事件

### TestService 类

方法：

  public void Start()
    启动服务，注册键盘事件处理器并开始监听

事件处理器：

  private static void OnKeyDown(object sender, KeyEventArgs e)
    处理按键按下事件
    检测 Ctrl+K 组合键并显示窗体

  private static void OnKeyUp(object sender, KeyEventArgs e)
    处理按键释放事件

### Form1 类

窗体初始化：

  private void Form1_Load(object sender, EventArgs e)
    设置窗体位置到屏幕右上角
    设置窗体为手动定位

---

## 常见问题

Q: 应用无法监听到按键
A: 请检查以下几点：
  1. 确保应用以正确的权限运行
  2. 某些系统权限可能限制键盘钩子，尝试以管理员身份运行
  3. 检查防火墙和安全软件是否阻止了应用

Q: 如何修改快捷键组合
A: 编辑 TestService.cs 中的 OnKeyDown 方法：
  if(Control.ModifierKeys == Keys.Control && e.KeyCode == Keys.K)
  将 Keys.Control 和 Keys.K 改为所需的按键

Q: 如何自定义窗体位置
A: 编辑 Form1.cs 中的 Form1_Load 方法：
  this.Location = new Point(x, y);
  其中 x 和 y 为屏幕坐标

Q: 如何停止应用监听
A: 在 TestService.cs 中调用：
  M_GlobalKeyListener.Stop();

---

## 发布应用

### 自包含发布

发布为独立可执行文件（不需要安装 .NET Runtime）：
  dotnet publish -c Release -r win-x64 --self-contained

### 框架依赖发布

发布需要目标系统安装 .NET Runtime：
  dotnet publish -c Release -r win-x64

发布输出将在 bin/Release/net9.0-windows/publish 目录中

---

## 许可证和声明

本项目使用 Windows API 钩子实现全局键盘监听。
在生产环境中使用前，请确保了解相关的法律和安全影响。

---

## 技术细节

### 按键监听实现原理

使用 Windows Low-Level Keyboard Hook (WH_KEYBOARD_LL) 实现：
  • 注册全局键盘钩子到操作系统
  • 应用收到所有键盘事件（即使窗口未获焦点）
  • 通过委托回调处理按键事件

### 跨平台兼容性

该应用仅支持 Windows 系统，使用了：
  • Windows Forms（GUI 框架）
  • Windows API（P/Invoke 调用）

如需跨平台支持，需要重新设计键盘监听模块

### 性能考虑

  • 使用低级键盘钩子，性能开销较小
  • 后台循环采用 20ms 睡眠间隔，不会过度占用 CPU
  • 建议在实际应用中添加日志控制，避免频繁输出

---

## 联系和反馈

如有问题或建议，请提出 Issue 或 Pull Request。

更新日期：2025 年 11 月 27 日
