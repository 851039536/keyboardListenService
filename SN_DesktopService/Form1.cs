using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SN_DesktopService
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender,EventArgs e)
        {
            // 显示在顶部左侧，紧贴边缘
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0,0);

            // 显示在顶部右侧，紧贴边缘
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width,0);



        }
    }
}
