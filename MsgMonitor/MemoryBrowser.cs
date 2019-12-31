using Be.Windows.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MsgMonitor
{
    public partial class MemoryBrowser : Form
    {
        IntPtr hProcess;
        HexBox hexBox;
        byte[] bytes = new byte[1024];
        int lastWidth;
        int lastHeight;
        public MemoryBrowser(Process process, uint address)
        {
            hProcess = DisasmForm.OpenProcess(process, DisasmForm.ProcessAccessFlags.All);
            hexBox = new HexBox();
            hexBox.ReadOnly = true;
            hexBox.ByteProvider = new DynamicByteProvider(bytes);
            hexBox.Left = 9;
            hexBox.Top = 37;
            hexBox.Width = 464;
            hexBox.Height = 213;
            InitializeComponent();
            AddressUpDown.Value = address;
            Controls.Add(hexBox);
            lastWidth = Width;
            lastHeight = Height;
        }

        private void AddressUpDown_ValueChanged(object sender, EventArgs e)
        {
            DisasmForm.ReadProcessMemory(hProcess, (uint)AddressUpDown.Value, bytes, bytes.Length, 0);
            hexBox.ByteProvider = new DynamicByteProvider(bytes);
        }

        private void MemoryBrowser_Resize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                hexBox.Width += Width - lastWidth;
                hexBox.Height += Height - lastHeight;
                lastWidth = Width;
                lastHeight = Height;
            }
        }
    }
}
