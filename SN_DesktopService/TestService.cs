
namespace SN_DesktopService
{
    public class TestService
    {
        static  Form1? form1;
        private static void OnKeyDown(object sender,KeyEventArgs e)
        {
            Console.WriteLine($"按键按下: {e.KeyCode}");

            // 检测组合键
            if(Control.ModifierKeys == Keys.Control && e.KeyCode == Keys.K)
            {
                Console.WriteLine("检测到Ctrl+K组合键");

                if(form1 != null && !form1.IsDisposed)
                {
                    return;
                }
                form1 = new Form1();
                form1.Show();

            }
        }

        private static void OnKeyUp(object sender,KeyEventArgs e)
        {
            Console.WriteLine($"按键释放: {e.KeyCode}");
        }

        public void Start()
        {
            Console.WriteLine("启动");
            // 注册按键事件
            M_GlobalKeyListener.KeyDown += OnKeyDown;
            M_GlobalKeyListener.KeyUp += OnKeyUp;
            // 开始监听
            M_GlobalKeyListener.Start();
              

                // 停止监听
                //M_GlobalKeyListener.Stop();
        }

    }
}
