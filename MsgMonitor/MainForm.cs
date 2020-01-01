using MsgMonitor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpDisasm;

namespace MsgMoniter
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CopyDataStruct
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }

    public enum MemoryDataType
    {
        [Description("字节")] Byte,
        [Description("短整数")] Short,
        [Description("整数")] Integer,
        [Description("浮点数")] Float,
        [Description("长整数")] Long,
        [Description("双浮点")] Double,
        [Description("字符串")] String,
        [Description("字节数组")] ByteArray,
    }

    public struct WrittenMemoryInfo
    {
        public static bool FormatHex = false;
        public Process Target { get; set; }
        public uint Address { get; set; }
        public MemoryDataType Type { get; set; }
        public byte[] ByteArray { get; set; }
        public string GetFormattedValue()
        {
            switch (Type)
            {
                case MemoryDataType.Byte:
                    return ByteArray[0].ToString(FormatHex ? "X2" : null);
                case MemoryDataType.Short:
                    return BitConverter.ToInt16(ByteArray, 0).ToString(FormatHex ? "X4" : null);
                case MemoryDataType.Integer:
                    return BitConverter.ToInt32(ByteArray, 0).ToString(FormatHex ? "X8" : null);
                case MemoryDataType.Float:
                    if (FormatHex) goto case MemoryDataType.Integer;
                    return BitConverter.ToSingle(ByteArray, 0).ToString();
                case MemoryDataType.Long:
                    return BitConverter.ToInt64(ByteArray, 0).ToString(FormatHex ? "X16" : null);
                case MemoryDataType.Double:
                    if (FormatHex) goto case MemoryDataType.Long;
                    return BitConverter.ToDouble(ByteArray, 0).ToString();
                case MemoryDataType.String:
                    return Encoding.Default.GetString(ByteArray);
                case MemoryDataType.ByteArray:
                    StringBuilder strBuilder = new StringBuilder();
                    foreach (var item in ByteArray)
                    {
                        strBuilder.Append(item.ToString("X2"));
                        strBuilder.Append(',');
                    }
                    strBuilder.Remove(strBuilder.Length - 1, 1);
                    return strBuilder.ToString();
                default:
                    return null;
            }
        }
        public WrittenMemoryInfo(CopyDataStruct copyData)
        {
            int processId = (int)copyData.dwData;
            Target = Process.GetProcessById(processId);
            byte[] buffer = new byte[copyData.cbData];
            int valueSize = buffer.Length - 4;
            Marshal.Copy(copyData.lpData, buffer, 0, buffer.Length);
            ByteArray = buffer.Take(valueSize).ToArray();
            Address = BitConverter.ToUInt32(buffer, valueSize);
            switch (valueSize)
            {
                case 1:
                    Type = MemoryDataType.Byte;
                    break;
                case 2:
                    Type = MemoryDataType.Short;
                    break;
                case 4:
                    Type = MemoryDataType.Integer;
                    break;
                case 8:
                    Type = MemoryDataType.Long;
                    break;
                default:
                    Type = MemoryDataType.ByteArray;
                    break;
            }
        }
    }

    public class MainForm : Form
    {
        #region Controls

        private ColumnHeader ColumnAddress;
        private ColumnHeader ColumnValueType;
        private ColumnHeader ColumnValue;
        private ContextMenuStrip MainPopMenu;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem 复制ToolStripMenuItem;
        private ToolStripMenuItem 地址ToolStripMenuItem;
        private ToolStripMenuItem 数值ToolStripMenuItem;
        private ToolStripMenuItem 保存到可执行文件ToolStripMenuItem;
        private ToolStripMenuItem 清空所有项ToolStripMenuItem;
        private MenuStrip MainMenu;
        private ToolStripMenuItem 文件ToolStripMenuItem;
        private ToolStripMenuItem 打开进程ToolStripMenuItem;
        private ToolStripMenuItem 附加进程ToolStripMenuItem;
        private ToolStripMenuItem 退出ToolStripMenuItem;
        private ToolStripMenuItem 全局设置ToolStripMenuItem;
        private ToolStripMenuItem 仅显示主模块地址ToolStripMenuItem;
        private ToolStripMenuItem 暂停拦截ToolStripMenuItem;
        private ToolStripMenuItem 保存写入ToolStripMenuItem;
        private ToolStripMenuItem 所有勾选的项ToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItemSep1;
        private ToolStripMenuItem 显示十六进制ToolStripMenuItem;
        private ToolStripMenuItem 帮助ToolStripMenuItem;
        private ToolStripMenuItem 使用说明ToolStripMenuItem;
        private ToolStripMenuItem 关于ToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripMenuItem 删除项目ToolStripMenuItem;
        private ToolStripMenuItem 保存项目ToolStripMenuItem;
        private ToolStripMenuItem 打开项目ToolStripMenuItem;
        private ToolStripMenuItem 添加项目ToolStripMenuItem;
        private ToolStripMenuItem 反汇编ToolStripMenuItem;
        private ToolStripMenuItem 转到反汇编ToolStripMenuItem;
        private ToolStripMenuItem 翻译汇编代码ToolStripMenuItem;
        private ToolStripMenuItem 浏览内存区域ToolStripMenuItem;
        private ColumnHeader ColumnDesc;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel toolStripStatusLabel3;
        private ToolStripMenuItem 显示类型ToolStripMenuItem;
        private ToolStripMenuItem 浮点数_整数ToolStripMenuItem;
        private ToolStripMenuItem 字符串_字节数组ToolStripMenuItem;
        private ToolStripMenuItem 项目另存为ToolStripMenuItem;
        private ListView MsgList;

        #endregion

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hwnd, int cmdshow);
        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool binvert);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hwnd);
        const int WM_COPYDATA = 0x4A;

        int lastWidth;
        int lastHeight;
        OpenFileDialog openFileDialog;
        SaveFileDialog saveFileDialog;
        OpenFileDialog openProjectDialog;
        SaveFileDialog saveProjectDialog;
        Process targetProcess;
        ProcessModule targetModule;

        [STAThread]
        public static void Main()
        {
            Disassembler.Translator.IncludeAddress = true;
            Disassembler.Translator.IncludeBinary = true;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);
            Application.Run(new MainForm());
        }

        public MainForm()
        {
            InitializeComponent();
            openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "可执行程序(*.exe)|*.exe";
            openFileDialog.Title = "打开程序";
            openFileDialog.DefaultExt = "exe";
            saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "可执行程序(*.exe)|*.exe";
            saveFileDialog.Title = "保存程序";
            saveFileDialog.DefaultExt = "exe";
            openProjectDialog = new OpenFileDialog();
            openProjectDialog.Filter = "项目文件(*.lvi)|*.lvi";
            openProjectDialog.Title = "打开项目";
            openProjectDialog.DefaultExt = "lvi";
            saveProjectDialog = new SaveFileDialog();
            saveProjectDialog.Filter = "项目文件(*.lvi)|*.lvi";
            saveProjectDialog.Title = "保存项目";
            saveProjectDialog.DefaultExt = "lvi";
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.MsgList = new System.Windows.Forms.ListView();
            this.ColumnDesc = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ColumnAddress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ColumnValueType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ColumnValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.MainPopMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.添加项目ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.删除项目ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.复制ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.地址ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.数值ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.显示类型ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.浮点数_整数ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.字符串_字节数组ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.反汇编ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.转到反汇编ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.翻译汇编代码ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.浏览内存区域ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.保存到可执行文件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.清空所有项ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MainMenu = new System.Windows.Forms.MenuStrip();
            this.文件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.打开项目ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.打开进程ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.附加进程ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.保存项目ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.退出ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.全局设置ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.仅显示主模块地址ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.显示十六进制ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.暂停拦截ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.保存写入ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.所有勾选的项ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.帮助ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.使用说明ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.关于ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.项目另存为ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MainPopMenu.SuspendLayout();
            this.MainMenu.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // MsgList
            // 
            this.MsgList.CheckBoxes = true;
            this.MsgList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColumnDesc,
            this.ColumnAddress,
            this.ColumnValueType,
            this.ColumnValue});
            this.MsgList.ContextMenuStrip = this.MainPopMenu;
            this.MsgList.FullRowSelect = true;
            this.MsgList.GridLines = true;
            this.MsgList.HideSelection = false;
            this.MsgList.LabelWrap = false;
            this.MsgList.Location = new System.Drawing.Point(12, 31);
            this.MsgList.Name = "MsgList";
            this.MsgList.Size = new System.Drawing.Size(610, 505);
            this.MsgList.TabIndex = 0;
            this.MsgList.UseCompatibleStateImageBehavior = false;
            this.MsgList.View = System.Windows.Forms.View.Details;
            this.MsgList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.MsgList_MouseDoubleClick);
            // 
            // ColumnDesc
            // 
            this.ColumnDesc.Text = "描述信息";
            this.ColumnDesc.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ColumnDesc.Width = 150;
            // 
            // ColumnAddress
            // 
            this.ColumnAddress.Text = "地址";
            this.ColumnAddress.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ColumnAddress.Width = 100;
            // 
            // ColumnValueType
            // 
            this.ColumnValueType.Text = "类型";
            this.ColumnValueType.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ColumnValueType.Width = 100;
            // 
            // ColumnValue
            // 
            this.ColumnValue.Text = "数值";
            this.ColumnValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ColumnValue.Width = 150;
            // 
            // MainPopMenu
            // 
            this.MainPopMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.MainPopMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.添加项目ToolStripMenuItem,
            this.删除项目ToolStripMenuItem,
            this.复制ToolStripMenuItem,
            this.显示类型ToolStripMenuItem,
            this.反汇编ToolStripMenuItem,
            this.保存到可执行文件ToolStripMenuItem,
            this.清空所有项ToolStripMenuItem});
            this.MainPopMenu.Name = "MainPopMenu";
            this.MainPopMenu.Size = new System.Drawing.Size(212, 158);
            // 
            // 添加项目ToolStripMenuItem
            // 
            this.添加项目ToolStripMenuItem.Name = "添加项目ToolStripMenuItem";
            this.添加项目ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.添加项目ToolStripMenuItem.Text = "添加项目";
            this.添加项目ToolStripMenuItem.Click += new System.EventHandler(this.添加项目ToolStripMenuItem_Click);
            // 
            // 删除项目ToolStripMenuItem
            // 
            this.删除项目ToolStripMenuItem.Name = "删除项目ToolStripMenuItem";
            this.删除项目ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.删除项目ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.删除项目ToolStripMenuItem.Text = "删除项目";
            this.删除项目ToolStripMenuItem.Click += new System.EventHandler(this.删除项目ToolStripMenuItem_Click);
            // 
            // 复制ToolStripMenuItem
            // 
            this.复制ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.地址ToolStripMenuItem,
            this.数值ToolStripMenuItem});
            this.复制ToolStripMenuItem.Name = "复制ToolStripMenuItem";
            this.复制ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.复制ToolStripMenuItem.Text = "复制信息";
            // 
            // 地址ToolStripMenuItem
            // 
            this.地址ToolStripMenuItem.Name = "地址ToolStripMenuItem";
            this.地址ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.地址ToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.地址ToolStripMenuItem.Text = "地址";
            this.地址ToolStripMenuItem.Click += new System.EventHandler(this.地址ToolStripMenuItem_Click);
            // 
            // 数值ToolStripMenuItem
            // 
            this.数值ToolStripMenuItem.Name = "数值ToolStripMenuItem";
            this.数值ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.C)));
            this.数值ToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.数值ToolStripMenuItem.Text = "数值";
            this.数值ToolStripMenuItem.Click += new System.EventHandler(this.数值ToolStripMenuItem_Click);
            // 
            // 显示类型ToolStripMenuItem
            // 
            this.显示类型ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.浮点数_整数ToolStripMenuItem,
            this.字符串_字节数组ToolStripMenuItem});
            this.显示类型ToolStripMenuItem.Name = "显示类型ToolStripMenuItem";
            this.显示类型ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.显示类型ToolStripMenuItem.Text = "显示类型";
            // 
            // 浮点数_整数ToolStripMenuItem
            // 
            this.浮点数_整数ToolStripMenuItem.Name = "浮点数_整数ToolStripMenuItem";
            this.浮点数_整数ToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.浮点数_整数ToolStripMenuItem.Text = "浮点数<->整数";
            this.浮点数_整数ToolStripMenuItem.Click += new System.EventHandler(this.浮点数_整数ToolStripMenuItem_Click);
            // 
            // 字符串_字节数组ToolStripMenuItem
            // 
            this.字符串_字节数组ToolStripMenuItem.Name = "字符串_字节数组ToolStripMenuItem";
            this.字符串_字节数组ToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.字符串_字节数组ToolStripMenuItem.Text = "字符串<->字节数组";
            this.字符串_字节数组ToolStripMenuItem.Click += new System.EventHandler(this.字符串_字节数组ToolStripMenuItem_Click);
            // 
            // 反汇编ToolStripMenuItem
            // 
            this.反汇编ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.转到反汇编ToolStripMenuItem,
            this.浏览内存区域ToolStripMenuItem,
            this.翻译汇编代码ToolStripMenuItem});
            this.反汇编ToolStripMenuItem.Name = "反汇编ToolStripMenuItem";
            this.反汇编ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.反汇编ToolStripMenuItem.Text = "反汇编";
            // 
            // 转到反汇编ToolStripMenuItem
            // 
            this.转到反汇编ToolStripMenuItem.Name = "转到反汇编ToolStripMenuItem";
            this.转到反汇编ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.转到反汇编ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.转到反汇编ToolStripMenuItem.Text = "转到反汇编";
            this.转到反汇编ToolStripMenuItem.Click += new System.EventHandler(this.转到反汇编ToolStripMenuItem_Click);
            // 
            // 翻译汇编代码ToolStripMenuItem
            // 
            this.翻译汇编代码ToolStripMenuItem.Name = "翻译汇编代码ToolStripMenuItem";
            this.翻译汇编代码ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F7;
            this.翻译汇编代码ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.翻译汇编代码ToolStripMenuItem.Text = "翻译汇编代码";
            this.翻译汇编代码ToolStripMenuItem.Click += new System.EventHandler(this.翻译汇编代码ToolStripMenuItem_Click);
            // 
            // 浏览内存区域ToolStripMenuItem
            // 
            this.浏览内存区域ToolStripMenuItem.Name = "浏览内存区域ToolStripMenuItem";
            this.浏览内存区域ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F6;
            this.浏览内存区域ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.浏览内存区域ToolStripMenuItem.Text = "浏览内存区域";
            this.浏览内存区域ToolStripMenuItem.Click += new System.EventHandler(this.浏览内存区域ToolStripMenuItem_Click);
            // 
            // 保存到可执行文件ToolStripMenuItem
            // 
            this.保存到可执行文件ToolStripMenuItem.Name = "保存到可执行文件ToolStripMenuItem";
            this.保存到可执行文件ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.S)));
            this.保存到可执行文件ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.保存到可执行文件ToolStripMenuItem.Text = "保存到可执行文件";
            this.保存到可执行文件ToolStripMenuItem.Click += new System.EventHandler(this.保存到可执行文件ToolStripMenuItem_Click);
            // 
            // 清空所有项ToolStripMenuItem
            // 
            this.清空所有项ToolStripMenuItem.Name = "清空所有项ToolStripMenuItem";
            this.清空所有项ToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.清空所有项ToolStripMenuItem.Text = "清空所有项";
            this.清空所有项ToolStripMenuItem.Click += new System.EventHandler(this.清空所有项ToolStripMenuItem_Click);
            // 
            // MainMenu
            // 
            this.MainMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.文件ToolStripMenuItem,
            this.全局设置ToolStripMenuItem,
            this.保存写入ToolStripMenuItem,
            this.帮助ToolStripMenuItem});
            this.MainMenu.Location = new System.Drawing.Point(0, 0);
            this.MainMenu.Name = "MainMenu";
            this.MainMenu.Size = new System.Drawing.Size(634, 25);
            this.MainMenu.TabIndex = 1;
            this.MainMenu.Text = "MainMenu";
            // 
            // 文件ToolStripMenuItem
            // 
            this.文件ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.打开项目ToolStripMenuItem,
            this.打开进程ToolStripMenuItem,
            this.附加进程ToolStripMenuItem,
            this.保存项目ToolStripMenuItem,
            this.项目另存为ToolStripMenuItem,
            this.toolStripMenuItemSep1,
            this.退出ToolStripMenuItem});
            this.文件ToolStripMenuItem.Name = "文件ToolStripMenuItem";
            this.文件ToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.文件ToolStripMenuItem.Text = "文件";
            // 
            // 打开项目ToolStripMenuItem
            // 
            this.打开项目ToolStripMenuItem.Name = "打开项目ToolStripMenuItem";
            this.打开项目ToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.打开项目ToolStripMenuItem.Text = "打开项目";
            this.打开项目ToolStripMenuItem.Click += new System.EventHandler(this.打开项目ToolStripMenuItem_Click);
            // 
            // 打开进程ToolStripMenuItem
            // 
            this.打开进程ToolStripMenuItem.Name = "打开进程ToolStripMenuItem";
            this.打开进程ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.打开进程ToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.打开进程ToolStripMenuItem.Text = "打开程序";
            this.打开进程ToolStripMenuItem.Click += new System.EventHandler(this.打开程序ToolStripMenuItem_Click);
            // 
            // 附加进程ToolStripMenuItem
            // 
            this.附加进程ToolStripMenuItem.Name = "附加进程ToolStripMenuItem";
            this.附加进程ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.附加进程ToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.附加进程ToolStripMenuItem.Text = "附加进程";
            this.附加进程ToolStripMenuItem.Click += new System.EventHandler(this.附加进程ToolStripMenuItem_Click);
            // 
            // 保存项目ToolStripMenuItem
            // 
            this.保存项目ToolStripMenuItem.Name = "保存项目ToolStripMenuItem";
            this.保存项目ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.保存项目ToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.保存项目ToolStripMenuItem.Text = "保存项目";
            this.保存项目ToolStripMenuItem.Click += new System.EventHandler(this.保存项目ToolStripMenuItem_Click);
            // 
            // toolStripMenuItemSep1
            // 
            this.toolStripMenuItemSep1.Name = "toolStripMenuItemSep1";
            this.toolStripMenuItemSep1.Size = new System.Drawing.Size(165, 6);
            // 
            // 退出ToolStripMenuItem
            // 
            this.退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            this.退出ToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.退出ToolStripMenuItem.Text = "退出";
            this.退出ToolStripMenuItem.Click += new System.EventHandler(this.退出ToolStripMenuItem_Click);
            // 
            // 全局设置ToolStripMenuItem
            // 
            this.全局设置ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.仅显示主模块地址ToolStripMenuItem,
            this.显示十六进制ToolStripMenuItem,
            this.暂停拦截ToolStripMenuItem});
            this.全局设置ToolStripMenuItem.Name = "全局设置ToolStripMenuItem";
            this.全局设置ToolStripMenuItem.Size = new System.Drawing.Size(68, 21);
            this.全局设置ToolStripMenuItem.Text = "全局设置";
            // 
            // 仅显示主模块地址ToolStripMenuItem
            // 
            this.仅显示主模块地址ToolStripMenuItem.CheckOnClick = true;
            this.仅显示主模块地址ToolStripMenuItem.Name = "仅显示主模块地址ToolStripMenuItem";
            this.仅显示主模块地址ToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.仅显示主模块地址ToolStripMenuItem.Text = "仅显示主模块地址";
            // 
            // 显示十六进制ToolStripMenuItem
            // 
            this.显示十六进制ToolStripMenuItem.CheckOnClick = true;
            this.显示十六进制ToolStripMenuItem.Name = "显示十六进制ToolStripMenuItem";
            this.显示十六进制ToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.显示十六进制ToolStripMenuItem.Text = "显示十六进制";
            this.显示十六进制ToolStripMenuItem.Click += new System.EventHandler(this.显示十六进制ToolStripMenuItem_Click);
            // 
            // 暂停拦截ToolStripMenuItem
            // 
            this.暂停拦截ToolStripMenuItem.CheckOnClick = true;
            this.暂停拦截ToolStripMenuItem.Name = "暂停拦截ToolStripMenuItem";
            this.暂停拦截ToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.暂停拦截ToolStripMenuItem.Text = "暂停拦截";
            // 
            // 保存写入ToolStripMenuItem
            // 
            this.保存写入ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.所有勾选的项ToolStripMenuItem});
            this.保存写入ToolStripMenuItem.Name = "保存写入ToolStripMenuItem";
            this.保存写入ToolStripMenuItem.Size = new System.Drawing.Size(68, 21);
            this.保存写入ToolStripMenuItem.Text = "保存写入";
            // 
            // 所有勾选的项ToolStripMenuItem
            // 
            this.所有勾选的项ToolStripMenuItem.Name = "所有勾选的项ToolStripMenuItem";
            this.所有勾选的项ToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.所有勾选的项ToolStripMenuItem.Text = "所有勾选的项";
            this.所有勾选的项ToolStripMenuItem.Click += new System.EventHandler(this.所有勾选的项ToolStripMenuItem_Click);
            // 
            // 帮助ToolStripMenuItem
            // 
            this.帮助ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.使用说明ToolStripMenuItem,
            this.关于ToolStripMenuItem});
            this.帮助ToolStripMenuItem.Name = "帮助ToolStripMenuItem";
            this.帮助ToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.帮助ToolStripMenuItem.Text = "帮助";
            // 
            // 使用说明ToolStripMenuItem
            // 
            this.使用说明ToolStripMenuItem.Name = "使用说明ToolStripMenuItem";
            this.使用说明ToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.使用说明ToolStripMenuItem.Text = "使用说明";
            this.使用说明ToolStripMenuItem.Click += new System.EventHandler(this.使用说明ToolStripMenuItem_Click);
            // 
            // 关于ToolStripMenuItem
            // 
            this.关于ToolStripMenuItem.Name = "关于ToolStripMenuItem";
            this.关于ToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.关于ToolStripMenuItem.Text = "关于";
            this.关于ToolStripMenuItem.Click += new System.EventHandler(this.关于ToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2,
            this.toolStripStatusLabel3});
            this.statusStrip1.Location = new System.Drawing.Point(0, 539);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.statusStrip1.Size = new System.Drawing.Size(634, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.IsLink = true;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(200, 17);
            this.toolStripStatusLabel1.Text = "本程序由冥谷川恋制作，不要滥用哦";
            this.toolStripStatusLabel1.Click += new System.EventHandler(this.toolStripStatusLabel1_Click);
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.toolStripStatusLabel2.IsLink = true;
            this.toolStripStatusLabel2.LinkColor = System.Drawing.Color.Red;
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(62, 17);
            this.toolStripStatusLabel2.Text = "<无进程>";
            this.toolStripStatusLabel2.Click += new System.EventHandler(this.toolStripStatusLabel2_Click);
            // 
            // toolStripStatusLabel3
            // 
            this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            this.toolStripStatusLabel3.Size = new System.Drawing.Size(56, 17);
            this.toolStripStatusLabel3.Text = "当前进程";
            // 
            // 项目另存为ToolStripMenuItem
            // 
            this.项目另存为ToolStripMenuItem.Name = "项目另存为ToolStripMenuItem";
            this.项目另存为ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.项目另存为ToolStripMenuItem.Size = new System.Drawing.Size(214, 22);
            this.项目另存为ToolStripMenuItem.Text = "项目另存为";
            this.项目另存为ToolStripMenuItem.Click += new System.EventHandler(this.项目另存为ToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(634, 561);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.MainMenu);
            this.Controls.Add(this.MsgList);
            this.MinimumSize = new System.Drawing.Size(250, 150);
            this.Name = "MainForm";
            this.Text = "MsgMonitor";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.MainPopMenu.ResumeLayout(false);
            this.MainMenu.ResumeLayout(false);
            this.MainMenu.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            CheckForSimpleInstance();
            lastWidth = Width;
            lastHeight = Height;
        }

        private void CheckForSimpleInstance()
        {
            Process[] processes = Process.GetProcessesByName("MsgMonitor");
            if (processes.Length > 1)
            {
                IntPtr hwnd = processes[0].MainWindowHandle;
                ShowWindow(hwnd, 1);
                FlashWindow(hwnd, true);
                SetForegroundWindow(hwnd);
                BringWindowToTop(hwnd);
                Close();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                MsgList.Width += Width - lastWidth;
                MsgList.Height += Height - lastHeight;
                lastWidth = Width;
                lastHeight = Height;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_COPYDATA)
            {
                try
                {
                    GetCopyData(m);
                }
                catch{}
            }
            base.WndProc(ref m);
        }

        private void GetCopyData(Message m)
        {
            if (暂停拦截ToolStripMenuItem.Checked) return;
            CopyDataStruct copyData = (CopyDataStruct)m.GetLParam(typeof(CopyDataStruct));
            WrittenMemoryInfo wmInfo = new WrittenMemoryInfo(copyData);
            if (targetProcess == null || targetProcess.Id == 0)
            {
                targetProcess = wmInfo.Target;
                if (targetProcess.Id != 0)
                    targetModule = targetProcess.MainModule;
            }
            else if (targetProcess.Id != wmInfo.Target.Id && wmInfo.Target.Id != 0)
            {
                if (MessageBox.Show($"被拦截的内存写入的目标进程{wmInfo.Target.ProcessName}\n" +
                    $"与当前进程{targetProcess.ProcessName}不同\n" +
                    "你希望重新指定进程对象吗", "提问", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    == DialogResult.Yes)
                {
                    targetProcess = wmInfo.Target;
                    if (targetProcess.Id != 0)
                        targetModule = targetProcess.MainModule;
                }
            }
            toolStripStatusLabel2.Text = targetProcess == null ? "<无进程>" : targetProcess.ProcessName;
            if (仅显示主模块地址ToolStripMenuItem.Checked && targetModule != null)
            {
                int baseAddress = targetModule.BaseAddress.ToInt32();
                if (wmInfo.Address < baseAddress || 
                    wmInfo.Address > baseAddress + targetModule.ModuleMemorySize) return;
            }
            ListViewItem item = new ListViewItem();
            item.Tag = wmInfo;
            item.Text = "<无描述>";
            item.SubItems.Add("0x" + wmInfo.Address.ToString("X8"));
            item.SubItems.Add(wmInfo.Type.GetDescription());
            item.SubItems.Add(wmInfo.GetFormattedValue());
            MsgList.Items.Add(item);
        }

        private void 打开程序ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog()== DialogResult.OK)
            {
                Process process = new Process();
                process.StartInfo.FileName = "MemToExe.exe";
                process.StartInfo.Arguments = openFileDialog.FileName;
                process.Start();
            }
        }

        private void 附加进程ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process process = new Process();
            process.StartInfo.FileName = "MemToExe.exe";
            ProcessForm processForm = new ProcessForm();
            if (processForm.ShowDialog() == DialogResult.OK)
            {
                process.StartInfo.Arguments = processForm.ProcessId;
                process.Start();
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void 清空所有项ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MsgList.Items.Clear();
        }

        private void 地址ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder strBuilder = new StringBuilder();
            foreach (ListViewItem item in MsgList.SelectedItems)
            {
                strBuilder.AppendLine(item.SubItems[1].Text);
            }
            Clipboard.SetText(strBuilder.ToString());
        }

        private void 数值ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder strBuilder = new StringBuilder();
            foreach (ListViewItem item in MsgList.SelectedItems)
            {
                strBuilder.AppendLine(item.SubItems[3].Text);
            }
            Clipboard.SetText(strBuilder.ToString());
        }
        
        private void 浮点数_整数ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in MsgList.SelectedItems)
            {
                WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)item.Tag;
                if (wmInfo.Type == MemoryDataType.Integer)
                    wmInfo.Type = MemoryDataType.Float;
                else if (wmInfo.Type == MemoryDataType.Float)
                    wmInfo.Type = MemoryDataType.Integer;
                item.SubItems[2].Text = wmInfo.Type.GetDescription();
                item.SubItems[3].Text = wmInfo.GetFormattedValue();
                item.Tag = wmInfo;
            }
        }

        private void 字符串_字节数组ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in MsgList.SelectedItems)
            {
                WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)item.Tag;
                if (wmInfo.Type == MemoryDataType.String)
                    wmInfo.Type = MemoryDataType.ByteArray;
                else if (wmInfo.Type == MemoryDataType.ByteArray)
                    wmInfo.Type = MemoryDataType.String;
                item.SubItems[2].Text = wmInfo.Type.GetDescription();
                item.SubItems[3].Text = wmInfo.GetFormattedValue();
                item.Tag = wmInfo;
            }
        }

        private void 显示十六进制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WrittenMemoryInfo.FormatHex = 显示十六进制ToolStripMenuItem.Checked;
            foreach (ListViewItem item in MsgList.Items)
            {
                WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)item.Tag;
                item.SubItems[3].Text = wmInfo.GetFormattedValue();
            }
        }

        private void 保存到可执行文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MsgList.SelectedItems.Count > 0)
            {
                if (targetModule == null)
                {
                    MessageBox.Show("无法获取进程模块", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                saveFileDialog.FileName = targetProcess.ProcessName;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (FileStream stream = File.Create(saveFileDialog.FileName))
                    {
                        FileStream tempStream = File.OpenRead(targetModule.FileName);
                        tempStream.CopyTo(stream);
                        tempStream.Close();
                        int inVaildCount = 0;
                        foreach (ListViewItem item in MsgList.SelectedItems)
                        {
                            WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)item.Tag;
                            int pos = (int)wmInfo.Address - targetModule.BaseAddress.ToInt32();
                            if (pos < 0 || pos > targetModule.ModuleMemorySize)
                                inVaildCount++;
                            else
                            {
                                stream.Seek(pos, SeekOrigin.Begin);
                                stream.Write(wmInfo.ByteArray, 0, wmInfo.ByteArray.Length);
                            }
                        }
                        if (inVaildCount > 0)
                        {
                            MessageBox.Show($"有{inVaildCount}项不在主模块内，未进行修改", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
        }

        private void 所有勾选的项ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MsgList.CheckedItems.Count > 0)
            {
                if (targetModule == null)
                {
                    MessageBox.Show("无法获取进程模块", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                saveFileDialog.FileName = targetProcess.ProcessName;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (FileStream stream = File.Create(saveFileDialog.FileName))
                    {
                        FileStream tempStream = File.OpenRead(targetModule.FileName);
                        tempStream.CopyTo(stream);
                        tempStream.Close();
                        int inVaildCount = 0;
                        foreach (ListViewItem item in MsgList.CheckedItems)
                        {
                            WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)item.Tag;
                            int pos = (int)wmInfo.Address - targetModule.BaseAddress.ToInt32();
                            if (pos < 0 || pos > targetModule.ModuleMemorySize)
                                inVaildCount++;
                            else
                            {
                                stream.Seek(pos, SeekOrigin.Begin);
                                stream.Write(wmInfo.ByteArray, 0, wmInfo.ByteArray.Length);
                            }
                        }
                        if (inVaildCount > 0)
                        {
                            MessageBox.Show($"有{inVaildCount}项不在主模块内，未进行修改", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/Lazuplis-Mei/MemToExe");
        }

        private void 删除项目ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in MsgList.SelectedItems)
            {
                MsgList.Items.Remove(item);
            }
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("本程序由冥谷川恋制作，可用于拦截修改器的内存写入，并将内存修改保存到目标程序本身", "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void 使用说明ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("选择[文件]菜单-[打开]你需要监视的修改器\n" +
                "如果修改器无法正常启动可以选择[附加]修改器进程\n" +
                "然后程序将自动记录下修改器的内存写入\n" +
                "选择你希望永久保存到目标程序的项目右键[保存到可执行文件]\n" +
                "并不保证对所有修改器有效，且会使得修改器使用受影响\n" +
                "如果修改器是以管理员身份运行，本程序也需要管理员身份", "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void 翻译汇编代码ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MsgList.SelectedItems.Count > 0)
            {
                WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)MsgList.SelectedItems[0].Tag;
                DisasmForm disasmForm = new DisasmForm(wmInfo);
                disasmForm.ShowDialog();
            }
        }

        private void 添加项目ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewItemForm newItemForm = new NewItemForm();
            if (newItemForm.ShowDialog() == DialogResult.OK)
            {
                var wmInfo = newItemForm.WMInfo;
                ListViewItem item = new ListViewItem();
                item.Tag = wmInfo;
                item.Text = newItemForm.Description;
                item.SubItems.Add("0x" + wmInfo.Address.ToString("X8"));
                item.SubItems.Add(wmInfo.Type.GetDescription());
                item.SubItems.Add(wmInfo.GetFormattedValue());
                MsgList.Items.Add(item);
            }
        }

        private void 转到反汇编ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MsgList.SelectedItems.Count > 0)
            {
                WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)MsgList.SelectedItems[0].Tag;
                if (wmInfo.Target != null)
                {
                    DisasmForm disasmForm = new DisasmForm(wmInfo, true);
                    disasmForm.ShowDialog();
                }
            }
        }

        private void MsgList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (MsgList.SelectedItems.Count > 0)
            {
                ListViewItem item = MsgList.SelectedItems[0];
                WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)item.Tag;
                NewItemForm newItemForm = new NewItemForm();
                newItemForm.textBoxDesc.Text = item.Text;
                newItemForm.textBoxAddress.Text = item.SubItems[1].Text;
                newItemForm.comboBoxType.SelectedIndex = (int)wmInfo.Type;
                newItemForm.textBoxValue.Text = item.SubItems[3].Text;
                if (newItemForm.ShowDialog() == DialogResult.OK)
                {
                    wmInfo = newItemForm.WMInfo;
                    item.Tag = wmInfo;
                    item.Text = newItemForm.Description;
                    item.SubItems[1].Text = ("0x" + wmInfo.Address.ToString("X8"));
                    item.SubItems[2].Text = (wmInfo.Type.GetDescription());
                    item.SubItems[3].Text = (wmInfo.GetFormattedValue());
                }
            }
        }

        private void 打开项目ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openProjectDialog.ShowDialog() == DialogResult.OK)
            {
                using (FileStream fileStream = File.OpenRead(openProjectDialog.FileName))
                {
                    if (fileStream.ReadString() == ".lvi")
                    {
                        MsgList.Items.Clear();
                        int count = fileStream.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            ListViewItem item = new ListViewItem();
                            item.Checked = fileStream.ReadBoolean();
                            item.Text = fileStream.ReadString();
                            WrittenMemoryInfo wmInfo = new WrittenMemoryInfo();
                            wmInfo.Address = (uint)fileStream.ReadInt32();
                            wmInfo.Type = (MemoryDataType)fileStream.ReadInt32();
                            wmInfo.ByteArray = fileStream.ReadBytes(fileStream.ReadInt32());
                            item.Tag = wmInfo;
                            item.SubItems.Add("0x" + wmInfo.Address.ToString("X8"));
                            item.SubItems.Add(wmInfo.Type.GetDescription());
                            item.SubItems.Add(wmInfo.GetFormattedValue());
                            MsgList.Items.Add(item);
                        }
                    }
                    else
                        MessageBox.Show("不是合法的项目文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void 保存项目ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(saveProjectDialog.FileName))
                SaveProject(saveProjectDialog.FileName);
            else
                项目另存为ToolStripMenuItem_Click(null, null);
        }

        private void 浏览内存区域ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MsgList.SelectedItems.Count > 0 && targetProcess != null)
            {
                WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)MsgList.SelectedItems[0].Tag;
                MemoryBrowser memoryBrowser = new MemoryBrowser(targetProcess, wmInfo.Address);
                memoryBrowser.ShowDialog();
            }
        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            ProcessForm processForm = new ProcessForm();
            if (processForm.ShowDialog()== DialogResult.OK)
            {
                targetProcess = Process.GetProcessById(int.Parse(processForm.ProcessId));
                toolStripStatusLabel2.Text = targetProcess.ProcessName;
                if (targetProcess.Id != 0)
                    targetModule = targetProcess.MainModule;
            }
        }

        private void 项目另存为ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveProjectDialog.ShowDialog() == DialogResult.OK)
                SaveProject(saveProjectDialog.FileName);
        }

        private void SaveProject(string filePath)
        {
            using (FileStream writer = File.Open(filePath, FileMode.Create, FileAccess.Write))
            {
                writer.Write(".lvi");
                writer.Write(MsgList.Items.Count);
                foreach (ListViewItem item in MsgList.Items)
                {
                    WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)item.Tag;
                    writer.Write(item.Checked);
                    writer.Write(item.Text);
                    writer.Write((int)wmInfo.Address);
                    writer.Write((int)wmInfo.Type);
                    writer.Write(wmInfo.ByteArray.Length);
                    writer.Write(wmInfo.ByteArray);
                }
            }
        }
    }

    static class EnumExtension
    {
        public static string GetDescription(this Enum @enum)
        {
            FieldInfo fieldInfo = @enum?.GetType()?.GetField(@enum.ToString());
            object[] attrs = fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attrs?[0] as DescriptionAttribute).Description;
        }
    }

    static class FileStreamExtension
    {

        public static void Write(this FileStream fileStream, int i)
        {
            fileStream.Write(BitConverter.GetBytes(i));
        }

        public static int ReadInt32(this FileStream fileStream)
        {
            return BitConverter.ToInt32(fileStream.ReadBytes(4), 0);
        }

        public static void Write(this FileStream fileStream, bool b)
        {
            fileStream.Write(BitConverter.GetBytes(b));
        }

        public static bool ReadBoolean(this FileStream fileStream)
        {
            return BitConverter.ToBoolean(fileStream.ReadBytes(1), 0);
        }

        public static void Write(this FileStream fileStream, byte[] bytes)
        {
            fileStream.Write(bytes, 0, bytes.Length);
        }

        public static byte[] ReadBytes(this FileStream fileStream, int length)
        {
            byte[] buffer = new byte[length];
            fileStream.Read(buffer, 0, length);
            return buffer;
        }

        public static void Write(this FileStream fileStream, string str)
        {
            byte[] buffer = Encoding.Default.GetBytes(str);
            fileStream.Write(buffer.Length);
            fileStream.Write(buffer);
        }

        public static string ReadString(this FileStream fileStream)
        {
            int length = fileStream.ReadInt32();
            return Encoding.Default.GetString(fileStream.ReadBytes(length));
        }
    }
}
