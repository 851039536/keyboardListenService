using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// 全局按键监听工具类（支持控制台和GUI应用）
/// </summary>
public static class M_GlobalKeyListener
{
    #region Windows API 声明

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode,IntPtr wParam,IntPtr lParam);

    [DllImport("user32.dll",CharSet = CharSet.Auto,SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,LowLevelKeyboardProc lpfn,IntPtr hMod,uint dwThreadId);

    [DllImport("user32.dll",CharSet = CharSet.Auto,SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll",CharSet = CharSet.Auto,SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk,int nCode,IntPtr wParam,IntPtr lParam);

    [DllImport("kernel32.dll",CharSet = CharSet.Auto,SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    #endregion

    #region 事件定义

    /// <summary>
    /// 按键按下事件
    /// </summary>
    public static event EventHandler<KeyEventArgs> KeyDown;

    /// <summary>
    /// 按键释放事件
    /// </summary>
    public static event EventHandler<KeyEventArgs> KeyUp;

    #endregion

    #region 私有字段

    private static LowLevelKeyboardProc _proc;
    private static IntPtr _hookID = IntPtr.Zero;
    private static bool _isRunning = false;
    private static Thread _messageLoopThread;

    #endregion

    #region 公共方法

    /// <summary>
    /// 开始监听键盘事件
    /// </summary>
    public static void Start()
    {
        if(_isRunning)
            return;

        _isRunning = true;
        _proc = HookCallback;

        // 在控制台应用中需要创建消息循环线程
        if(Environment.UserInteractive && Console.OpenStandardInput(1) != null)
        {
            _messageLoopThread = new Thread(() =>
            {
                _hookID = SetHook(_proc);
                Application.Run(); // 启动消息循环
            });
            _messageLoopThread.IsBackground = true;
            _messageLoopThread.Start();
        } else
        {
            _hookID = SetHook(_proc);
        }
    }

    /// <summary>
    /// 停止监听键盘事件
    /// </summary>
    public static void Stop()
    {
        if(!_isRunning)
            return;

        _isRunning = false;

        if(_hookID != IntPtr.Zero)
        {
            _=UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }

        if(_messageLoopThread != null && _messageLoopThread.IsAlive)
        {
            Application.Exit();
            _messageLoopThread.Join(500);
        }
    }

    #endregion

    #region 私有方法

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using(Process curProcess = Process.GetCurrentProcess())
        using(ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL,proc,GetModuleHandle(curModule.ModuleName),0);
        }
    }

    private static IntPtr HookCallback(int nCode,IntPtr wParam,IntPtr lParam)
    {
        if(nCode >= 0 && _isRunning)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Keys key = (Keys)vkCode;

            if(wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                KeyDown?.Invoke(null,new KeyEventArgs(key));
            } else if(wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
            {
                KeyUp?.Invoke(null,new KeyEventArgs(key));
            }
        }
        return CallNextHookEx(_hookID,nCode,wParam,lParam);
    }

    #endregion
}
