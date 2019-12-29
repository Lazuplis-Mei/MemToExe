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

namespace MsgMoniter
{
    [StructLayout(LayoutKind.Sequential)]
    struct CopyDataStruct
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }

    enum MemoryDataType
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

    struct WrittenMemoryInfo
    {
        public static bool FormatHex = false;
        private MemoryDataType type;
        public Process Target { get; }
        public uint Address { get; }
        public MemoryDataType Type
        {
            get => type;
            set
            {
                if (value == MemoryDataType.Float || value == MemoryDataType.Integer)
                {
                    if (type == MemoryDataType.Float || type == MemoryDataType.Integer)
                    {
                        type = value;
                    }
                    return;
                }
                if (value == MemoryDataType.Double || value == MemoryDataType.Long)
                {
                    if (type == MemoryDataType.Double || type == MemoryDataType.Long)
                    {
                        type = value;
                    }
                    return;
                }
                if (value == MemoryDataType.String || value == MemoryDataType.ByteArray)
                {
                    if (type == MemoryDataType.String || type == MemoryDataType.ByteArray)
                    {
                        type = value;
                    }
                    return;
                }

            }
        }
        public byte[] ByteArray { get; }
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
                    type = MemoryDataType.Byte;
                    break;
                case 2:
                    type = MemoryDataType.Short;
                    break;
                case 4:
                    type = MemoryDataType.Integer;
                    break;
                case 8:
                    type = MemoryDataType.Long;
                    break;
                default:
                    type = MemoryDataType.ByteArray;
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
        private ToolStripMenuItem 修改类型ToolStripMenuItem;
        private ToolStripMenuItem 浮点数_整数ToolStripMenuItem;
        private ToolStripMenuItem 字符串_字节数组ToolStripMenuItem;
        private ToolStripMenuItem 保存到可执行文件ToolStripMenuItem;
        private ToolStripMenuItem 清空所有项ToolStripMenuItem;
        private ColumnHeader ColumnTarget;
        private MenuStrip MainMenuStrip;
        private ToolStripMenuItem 文件ToolStripMenuItem;
        private ToolStripMenuItem 打开ToolStripMenuItem;
        private ToolStripMenuItem 附加ToolStripMenuItem;
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

        [STAThread]
        public static void Main()
        {
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
            saveFileDialog.Title = "打开程序";
            saveFileDialog.DefaultExt = "exe";
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.MsgList = new System.Windows.Forms.ListView();
            this.ColumnTarget = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ColumnAddress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ColumnValueType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ColumnValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.MainPopMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.删除项目ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.复制ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.地址ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.数值ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.修改类型ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.浮点数_整数ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.字符串_字节数组ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.保存到可执行文件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.清空所有项ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.文件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.打开ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.附加ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.MainPopMenu.SuspendLayout();
            this.MainMenuStrip.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // MsgList
            // 
            this.MsgList.CheckBoxes = true;
            this.MsgList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColumnTarget,
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
            this.MsgList.Size = new System.Drawing.Size(608, 493);
            this.MsgList.TabIndex = 0;
            this.MsgList.UseCompatibleStateImageBehavior = false;
            this.MsgList.View = System.Windows.Forms.View.Details;
            // 
            // ColumnTarget
            // 
            this.ColumnTarget.Text = "进程";
            this.ColumnTarget.Width = 150;
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
            this.ColumnValue.Width = 250;
            // 
            // MainPopMenu
            // 
            this.MainPopMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.MainPopMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.删除项目ToolStripMenuItem,
            this.复制ToolStripMenuItem,
            this.修改类型ToolStripMenuItem,
            this.保存到可执行文件ToolStripMenuItem,
            this.清空所有项ToolStripMenuItem});
            this.MainPopMenu.Name = "MainPopMenu";
            this.MainPopMenu.Size = new System.Drawing.Size(254, 124);
            // 
            // 删除项目ToolStripMenuItem
            // 
            this.删除项目ToolStripMenuItem.Name = "删除项目ToolStripMenuItem";
            this.删除项目ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.删除项目ToolStripMenuItem.Size = new System.Drawing.Size(253, 24);
            this.删除项目ToolStripMenuItem.Text = "删除项目";
            this.删除项目ToolStripMenuItem.Click += new System.EventHandler(this.删除项目ToolStripMenuItem_Click);
            // 
            // 复制ToolStripMenuItem
            // 
            this.复制ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.地址ToolStripMenuItem,
            this.数值ToolStripMenuItem});
            this.复制ToolStripMenuItem.Name = "复制ToolStripMenuItem";
            this.复制ToolStripMenuItem.Size = new System.Drawing.Size(253, 24);
            this.复制ToolStripMenuItem.Text = "复制信息";
            // 
            // 地址ToolStripMenuItem
            // 
            this.地址ToolStripMenuItem.Name = "地址ToolStripMenuItem";
            this.地址ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.地址ToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            this.地址ToolStripMenuItem.Text = "地址";
            this.地址ToolStripMenuItem.Click += new System.EventHandler(this.地址ToolStripMenuItem_Click);
            // 
            // 数值ToolStripMenuItem
            // 
            this.数值ToolStripMenuItem.Name = "数值ToolStripMenuItem";
            this.数值ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.C)));
            this.数值ToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            this.数值ToolStripMenuItem.Text = "数值";
            this.数值ToolStripMenuItem.Click += new System.EventHandler(this.数值ToolStripMenuItem_Click);
            // 
            // 修改类型ToolStripMenuItem
            // 
            this.修改类型ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.浮点数_整数ToolStripMenuItem,
            this.字符串_字节数组ToolStripMenuItem});
            this.修改类型ToolStripMenuItem.Name = "修改类型ToolStripMenuItem";
            this.修改类型ToolStripMenuItem.Size = new System.Drawing.Size(253, 24);
            this.修改类型ToolStripMenuItem.Text = "显示类型";
            // 
            // 浮点数_整数ToolStripMenuItem
            // 
            this.浮点数_整数ToolStripMenuItem.Name = "浮点数_整数ToolStripMenuItem";
            this.浮点数_整数ToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            this.浮点数_整数ToolStripMenuItem.Text = "浮点数<->整数";
            this.浮点数_整数ToolStripMenuItem.Click += new System.EventHandler(this.浮点数_整数ToolStripMenuItem_Click);
            // 
            // 字符串_字节数组ToolStripMenuItem
            // 
            this.字符串_字节数组ToolStripMenuItem.Name = "字符串_字节数组ToolStripMenuItem";
            this.字符串_字节数组ToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            this.字符串_字节数组ToolStripMenuItem.Text = "字符串<->字节数组";
            this.字符串_字节数组ToolStripMenuItem.Click += new System.EventHandler(this.字符串_字节数组ToolStripMenuItem_Click);
            // 
            // 保存到可执行文件ToolStripMenuItem
            // 
            this.保存到可执行文件ToolStripMenuItem.Name = "保存到可执行文件ToolStripMenuItem";
            this.保存到可执行文件ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.保存到可执行文件ToolStripMenuItem.Size = new System.Drawing.Size(253, 24);
            this.保存到可执行文件ToolStripMenuItem.Text = "保存到可执行文件";
            this.保存到可执行文件ToolStripMenuItem.Click += new System.EventHandler(this.保存到可执行文件ToolStripMenuItem_Click);
            // 
            // 清空所有项ToolStripMenuItem
            // 
            this.清空所有项ToolStripMenuItem.Name = "清空所有项ToolStripMenuItem";
            this.清空所有项ToolStripMenuItem.Size = new System.Drawing.Size(253, 24);
            this.清空所有项ToolStripMenuItem.Text = "清空所有项";
            this.清空所有项ToolStripMenuItem.Click += new System.EventHandler(this.清空所有项ToolStripMenuItem_Click);
            // 
            // MainMenuStrip
            // 
            this.MainMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.MainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.文件ToolStripMenuItem,
            this.全局设置ToolStripMenuItem,
            this.保存写入ToolStripMenuItem,
            this.帮助ToolStripMenuItem});
            this.MainMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MainMenuStrip.Name = "MainMenuStrip";
            this.MainMenuStrip.Size = new System.Drawing.Size(632, 28);
            this.MainMenuStrip.TabIndex = 1;
            this.MainMenuStrip.Text = "menuStrip1";
            // 
            // 文件ToolStripMenuItem
            // 
            this.文件ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.打开ToolStripMenuItem,
            this.附加ToolStripMenuItem,
            this.toolStripMenuItemSep1,
            this.退出ToolStripMenuItem});
            this.文件ToolStripMenuItem.Name = "文件ToolStripMenuItem";
            this.文件ToolStripMenuItem.Size = new System.Drawing.Size(53, 24);
            this.文件ToolStripMenuItem.Text = "文件";
            // 
            // 打开ToolStripMenuItem
            // 
            this.打开ToolStripMenuItem.Name = "打开ToolStripMenuItem";
            this.打开ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.打开ToolStripMenuItem.Size = new System.Drawing.Size(148, 26);
            this.打开ToolStripMenuItem.Text = "打开";
            this.打开ToolStripMenuItem.Click += new System.EventHandler(this.打开ToolStripMenuItem_Click);
            // 
            // 附加ToolStripMenuItem
            // 
            this.附加ToolStripMenuItem.Name = "附加ToolStripMenuItem";
            this.附加ToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.附加ToolStripMenuItem.Size = new System.Drawing.Size(148, 26);
            this.附加ToolStripMenuItem.Text = "附加";
            this.附加ToolStripMenuItem.Click += new System.EventHandler(this.附加ToolStripMenuItem_Click);
            // 
            // toolStripMenuItemSep1
            // 
            this.toolStripMenuItemSep1.Name = "toolStripMenuItemSep1";
            this.toolStripMenuItemSep1.Size = new System.Drawing.Size(145, 6);
            // 
            // 退出ToolStripMenuItem
            // 
            this.退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            this.退出ToolStripMenuItem.Size = new System.Drawing.Size(148, 26);
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
            this.全局设置ToolStripMenuItem.Size = new System.Drawing.Size(83, 24);
            this.全局设置ToolStripMenuItem.Text = "全局设置";
            // 
            // 仅显示主模块地址ToolStripMenuItem
            // 
            this.仅显示主模块地址ToolStripMenuItem.CheckOnClick = true;
            this.仅显示主模块地址ToolStripMenuItem.Name = "仅显示主模块地址ToolStripMenuItem";
            this.仅显示主模块地址ToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            this.仅显示主模块地址ToolStripMenuItem.Text = "仅显示主模块地址";
            // 
            // 显示十六进制ToolStripMenuItem
            // 
            this.显示十六进制ToolStripMenuItem.CheckOnClick = true;
            this.显示十六进制ToolStripMenuItem.Name = "显示十六进制ToolStripMenuItem";
            this.显示十六进制ToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            this.显示十六进制ToolStripMenuItem.Text = "显示十六进制";
            this.显示十六进制ToolStripMenuItem.Click += new System.EventHandler(this.显示十六进制ToolStripMenuItem_Click);
            // 
            // 暂停拦截ToolStripMenuItem
            // 
            this.暂停拦截ToolStripMenuItem.CheckOnClick = true;
            this.暂停拦截ToolStripMenuItem.Name = "暂停拦截ToolStripMenuItem";
            this.暂停拦截ToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            this.暂停拦截ToolStripMenuItem.Text = "暂停拦截";
            // 
            // 保存写入ToolStripMenuItem
            // 
            this.保存写入ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.所有勾选的项ToolStripMenuItem});
            this.保存写入ToolStripMenuItem.Name = "保存写入ToolStripMenuItem";
            this.保存写入ToolStripMenuItem.Size = new System.Drawing.Size(83, 24);
            this.保存写入ToolStripMenuItem.Text = "保存写入";
            // 
            // 所有勾选的项ToolStripMenuItem
            // 
            this.所有勾选的项ToolStripMenuItem.Name = "所有勾选的项ToolStripMenuItem";
            this.所有勾选的项ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.S)));
            this.所有勾选的项ToolStripMenuItem.Size = new System.Drawing.Size(232, 26);
            this.所有勾选的项ToolStripMenuItem.Text = "所有勾选的项";
            this.所有勾选的项ToolStripMenuItem.Click += new System.EventHandler(this.所有勾选的项ToolStripMenuItem_Click);
            // 
            // 帮助ToolStripMenuItem
            // 
            this.帮助ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.使用说明ToolStripMenuItem,
            this.关于ToolStripMenuItem});
            this.帮助ToolStripMenuItem.Name = "帮助ToolStripMenuItem";
            this.帮助ToolStripMenuItem.Size = new System.Drawing.Size(53, 24);
            this.帮助ToolStripMenuItem.Text = "帮助";
            // 
            // 使用说明ToolStripMenuItem
            // 
            this.使用说明ToolStripMenuItem.Name = "使用说明ToolStripMenuItem";
            this.使用说明ToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.使用说明ToolStripMenuItem.Text = "使用说明";
            this.使用说明ToolStripMenuItem.Click += new System.EventHandler(this.使用说明ToolStripMenuItem_Click);
            // 
            // 关于ToolStripMenuItem
            // 
            this.关于ToolStripMenuItem.Name = "关于ToolStripMenuItem";
            this.关于ToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.关于ToolStripMenuItem.Text = "关于";
            this.关于ToolStripMenuItem.Click += new System.EventHandler(this.关于ToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 527);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.statusStrip1.Size = new System.Drawing.Size(632, 26);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.IsLink = true;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(249, 20);
            this.toolStripStatusLabel1.Text = "本程序由冥谷川恋制作，不要滥用哦";
            this.toolStripStatusLabel1.Click += new System.EventHandler(this.toolStripStatusLabel1_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(632, 553);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.MainMenuStrip);
            this.Controls.Add(this.MsgList);
            this.MinimumSize = new System.Drawing.Size(250, 150);
            this.Name = "MainForm";
            this.Text = "MsgMonitor";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.MainPopMenu.ResumeLayout(false);
            this.MainMenuStrip.ResumeLayout(false);
            this.MainMenuStrip.PerformLayout();
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
                GetCopyData(m);
            }
            base.WndProc(ref m);
        }

        private void GetCopyData(Message m)
        {
            if (暂停拦截ToolStripMenuItem.Checked)
            {
                return;
            }
            CopyDataStruct copyData = (CopyDataStruct)m.GetLParam(typeof(CopyDataStruct));
            WrittenMemoryInfo wmInfo = new WrittenMemoryInfo(copyData);
            uint address = wmInfo.Address;
            if (仅显示主模块地址ToolStripMenuItem.Checked)
            {
                int baseAddress = wmInfo.Target.MainModule.BaseAddress.ToInt32();
                int size = wmInfo.Target.MainModule.ModuleMemorySize;
                if (address < baseAddress || address > baseAddress + size)
                {
                    return;
                }
            }
            ListViewItem item = new ListViewItem();
            item.Tag = wmInfo;
            item.Text = wmInfo.Target.ProcessName;
            item.SubItems.Add("0x" + address.ToString("X8"));
            item.SubItems.Add(wmInfo.Type.GetDescription());
            item.SubItems.Add(wmInfo.GetFormattedValue());
            MsgList.Items.Add(item);
        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog()== DialogResult.OK)
            {
                Process process = new Process();
                process.StartInfo.FileName = "MemToExe.exe";
                process.StartInfo.Arguments = openFileDialog.FileName;
                process.Start();
            }
        }

        private void 附加ToolStripMenuItem_Click(object sender, EventArgs e)
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
                try
                { 
                    WrittenMemoryInfo uwmInfo = (WrittenMemoryInfo)MsgList.SelectedItems[0].Tag;
                    ProcessModule module = uwmInfo.Target.MainModule;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using (FileStream stream = File.Create(saveFileDialog.FileName))
                        {
                            FileStream tempStream = File.OpenRead(module.FileName);
                            tempStream.CopyTo(stream);
                            tempStream.Close();
                            int anotherProcessCount = 0;
                            int inVaildCount = 0;
                            foreach (ListViewItem item in MsgList.SelectedItems)
                            {
                                WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)item.Tag;
                                if (wmInfo.Target.Id != uwmInfo.Target.Id)
                                {
                                    anotherProcessCount++;
                                }
                                int pos = (int)wmInfo.Address - module.BaseAddress.ToInt32();
                                if (pos < 0 || pos > module.ModuleMemorySize)
                                {
                                    inVaildCount++;
                                }
                                stream.Seek(pos, SeekOrigin.Begin);
                                stream.Write(wmInfo.ByteArray, 0, wmInfo.ByteArray.Length);
                            }
                            if (anotherProcessCount > 0 || inVaildCount > 0)
                            {
                                MessageBox.Show($"有{anotherProcessCount}项与首项进程不符，{inVaildCount}项不在主模块内，未进行修改", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void 所有勾选的项ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MsgList.CheckedItems.Count > 0)
            {
                try
                {
                    WrittenMemoryInfo uwmInfo = (WrittenMemoryInfo)MsgList.CheckedItems[0].Tag;
                    ProcessModule module = uwmInfo.Target.MainModule;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using (FileStream stream = File.Create(saveFileDialog.FileName))
                        {
                            FileStream tempStream = File.OpenRead(module.FileName);
                            tempStream.CopyTo(stream);
                            tempStream.Close();
                            int anotherProcessCount = 0;
                            int inVaildCount = 0;
                            foreach (ListViewItem item in MsgList.CheckedItems)
                            {
                                WrittenMemoryInfo wmInfo = (WrittenMemoryInfo)item.Tag;
                                if (wmInfo.Target.Id != uwmInfo.Target.Id)
                                {
                                    anotherProcessCount++;
                                }
                                int pos = (int)wmInfo.Address - module.BaseAddress.ToInt32();
                                if (pos < 0 || pos > module.ModuleMemorySize)
                                {
                                    inVaildCount++;
                                }
                                stream.Seek(pos, SeekOrigin.Begin);
                                stream.Write(wmInfo.ByteArray, 0, wmInfo.ByteArray.Length);
                            }
                            if (anotherProcessCount > 0 || inVaildCount > 0)
                            {
                                MessageBox.Show($"有{anotherProcessCount}项与首项进程不符，{inVaildCount}项不在主模块内，未进行修改", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }

    static class ObjectExtension
    {
        public static string GetDescription(this Enum @enum)
        {
            FieldInfo fieldInfo = @enum?.GetType()?.GetField(@enum.ToString());
            object[] attrs = fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attrs?[0] as DescriptionAttribute).Description;
        }
    }

}
