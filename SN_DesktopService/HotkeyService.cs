namespace SN_DesktopService;

/// <summary>
/// 快捷键服务：订阅全局键盘事件，处理组合键逻辑
/// </summary>
public static class HotkeyService
{
    private static Form1? _form1;
    private static bool _started;

    /// <summary>
    /// 启动快捷服务：注册键盘事件并开始监听
    /// </summary>
    public static void Start()
    {
        if(_started)
            return;
        _started = true;

        Console.WriteLine("键盘监听服务启动中...");
        M_GlobalKeyListener.KeyDown += OnKeyDown;
        M_GlobalKeyListener.KeyUp += OnKeyUp;
        M_GlobalKeyListener.Start();
    }

    private static void OnKeyDown(object? sender,KeyEventArgs e)
    {
        Console.WriteLine($"按键按下: {e.KeyCode}");

        // 检测 Ctrl+K 组合键
        if(Control.ModifierKeys == Keys.Control && e.KeyCode == Keys.K)
        {
            Console.WriteLine("检测到Ctrl+K组合键");

            if(_form1 is { IsDisposed: false })
                return;

            _form1 = new Form1();
            _form1.FormClosed += (_, _) => _form1 = null;
            _form1.Show();
        }
    }

    private static void OnKeyUp(object? sender,KeyEventArgs e)
    {
        Console.WriteLine($"按键释放: {e.KeyCode}");
    }
}
