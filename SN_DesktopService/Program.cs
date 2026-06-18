using SN_DesktopService;

// 注册优雅退出处理
Console.CancelKeyPress += (_, e) =>
{
    Console.WriteLine("\n正在退出...");
    M_GlobalKeyListener.Stop();
    e.Cancel = true; // 阻止立即终止，留时间给 Stop()
    Environment.Exit(0);
};

HotkeyService.Start();

// 保持进程存活
while(true)
{
    Thread.Sleep(20);
}


