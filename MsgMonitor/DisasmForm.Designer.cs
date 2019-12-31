namespace MsgMonitor
{
    partial class DisasmForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DisAsmListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.AddressUpDown = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.LengthUpDown = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.AddressUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LengthUpDown)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // DisAsmListView
            // 
            this.DisAsmListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.DisAsmListView.FullRowSelect = true;
            this.DisAsmListView.GridLines = true;
            this.DisAsmListView.HideSelection = false;
            this.DisAsmListView.Location = new System.Drawing.Point(9, 37);
            this.DisAsmListView.Margin = new System.Windows.Forms.Padding(2);
            this.DisAsmListView.Name = "DisAsmListView";
            this.DisAsmListView.Size = new System.Drawing.Size(564, 300);
            this.DisAsmListView.TabIndex = 0;
            this.DisAsmListView.UseCompatibleStateImageBehavior = false;
            this.DisAsmListView.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "地址";
            this.columnHeader1.Width = 120;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "字节";
            this.columnHeader2.Width = 200;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "汇编代码";
            this.columnHeader3.Width = 200;
            // 
            // AddressUpDown
            // 
            this.AddressUpDown.Hexadecimal = true;
            this.AddressUpDown.Location = new System.Drawing.Point(104, 10);
            this.AddressUpDown.Margin = new System.Windows.Forms.Padding(2);
            this.AddressUpDown.Maximum = new decimal(new int[] {
            -1,
            0,
            0,
            0});
            this.AddressUpDown.Name = "AddressUpDown";
            this.AddressUpDown.Size = new System.Drawing.Size(112, 21);
            this.AddressUpDown.TabIndex = 1;
            this.AddressUpDown.ValueChanged += new System.EventHandler(this.AddressUpDown_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 14);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "反汇编首地址";
            // 
            // LengthUpDown
            // 
            this.LengthUpDown.Location = new System.Drawing.Point(313, 10);
            this.LengthUpDown.Margin = new System.Windows.Forms.Padding(2);
            this.LengthUpDown.Maximum = new decimal(new int[] {
            800,
            0,
            0,
            0});
            this.LengthUpDown.Name = "LengthUpDown";
            this.LengthUpDown.Size = new System.Drawing.Size(112, 21);
            this.LengthUpDown.TabIndex = 3;
            this.LengthUpDown.ValueChanged += new System.EventHandler(this.LengthUpDown_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(220, 14);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 12);
            this.label2.TabIndex = 4;
            this.label2.Text = "反汇编代码长度";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 339);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.statusStrip1.Size = new System.Drawing.Size(584, 22);
            this.statusStrip1.TabIndex = 5;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(208, 17);
            this.toolStripStatusLabel1.Text = "按住Ctrl加滚轮可以调节反汇编首地址";
            // 
            // DisasmForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.LengthUpDown);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.AddressUpDown);
            this.Controls.Add(this.DisAsmListView);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(249, 149);
            this.Name = "DisasmForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "反汇编窗口";
            this.Resize += new System.EventHandler(this.DisasmForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.AddressUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LengthUpDown)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.ListView DisAsmListView;
        public System.Windows.Forms.NumericUpDown AddressUpDown;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        public System.Windows.Forms.NumericUpDown LengthUpDown;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
    }
}