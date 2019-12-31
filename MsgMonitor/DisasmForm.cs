using MsgMoniter;
using SharpDisasm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MsgMonitor
{
    public partial class DisasmForm : Form
    {
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            uint lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            int lpNumberOfBytesRead);
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
             ProcessAccessFlags processAccess,
             bool bInheritHandle,
             int processId
        );
        public static IntPtr OpenProcess(Process proc, ProcessAccessFlags flags)
        {
            return OpenProcess(flags, false, proc.Id);
        }

        WrittenMemoryInfo wmInfo;
        bool extend;
        IntPtr hProcess;
        public DisasmForm(WrittenMemoryInfo wmInfo, bool extend = false)
        {
            InitializeComponent();
            this.wmInfo = wmInfo;
            this.extend = extend;
            if (extend)
            {
                hProcess = OpenProcess(this.wmInfo.Target, ProcessAccessFlags.All);
                AddressUpDown.Enabled = true;
                LengthUpDown.Enabled = true;
                DisAsmListView.MouseWheel += DisAsmListView_MouseWheel;
                this.wmInfo.ByteArray = new byte[this.wmInfo.ByteArray.Length + 50];
                AddressUpDown.Value = this.wmInfo.Address - 25;
            }
            else
            {
                AddressUpDown.Enabled = false;
                LengthUpDown.Enabled = false;
                AddressUpDown.Value = this.wmInfo.Address;
            }
            LengthUpDown.Value = this.wmInfo.ByteArray.Length;
            lastWidth = Width;
            lastHeight = Height;
        }

        int lastWidth;
        int lastHeight;

        private void DisasmForm_Resize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                DisAsmListView.Width += Width - lastWidth;
                DisAsmListView.Height += Height - lastHeight;
                lastWidth = Width;
                lastHeight = Height;
            }
        }

        private void AddressUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (extend) ReadProcessMemory(hProcess, (uint)AddressUpDown.Value, wmInfo.ByteArray, wmInfo.ByteArray.Length, 0);
            var disasm = new Disassembler(wmInfo.ByteArray, ArchitectureMode.x86_32, (uint)AddressUpDown.Value, true);
            DisAsmListView.Items.Clear();
            int index = -1;
            foreach (var item in disasm.Disassemble())
            {
                ListViewItem listViewItem = new ListViewItem("0x" + item.Offset.ToString("X8"));
                listViewItem.SubItems.Add(Disassembler.Translator.TranslateBytes(item).ToUpper());
                listViewItem.SubItems.Add(Disassembler.Translator.TranslateMnemonic(item));
                DisAsmListView.Items.Add(listViewItem);
                if (extend && item.Offset == wmInfo.Address)
                    index = DisAsmListView.Items.Count - 1;
            }
            if (extend && index >= 0) 
            {
                DisAsmListView.SelectedIndices.Add(index);
                DisAsmListView.EnsureVisible(index);
            }
        }

        private void LengthUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (LengthUpDown.Value != wmInfo.ByteArray.Length)
            {
                wmInfo.ByteArray = new byte[(int)LengthUpDown.Value];
                AddressUpDown_ValueChanged(null, null);
            }
        }
        private void DisAsmListView_MouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                if (e.Delta > 0)
                    AddressUpDown.DownButton();
                else
                    AddressUpDown.UpButton();
            }
        }
    }
}
