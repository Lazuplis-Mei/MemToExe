namespace MsgMonitor
{
    partial class MemoryBrowser
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
            this.AddressUpDown = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.AddressUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // AddressUpDown
            // 
            this.AddressUpDown.Hexadecimal = true;
            this.AddressUpDown.Increment = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.AddressUpDown.Location = new System.Drawing.Point(59, 12);
            this.AddressUpDown.Maximum = new decimal(new int[] {
            -1,
            0,
            0,
            0});
            this.AddressUpDown.Name = "AddressUpDown";
            this.AddressUpDown.Size = new System.Drawing.Size(120, 21);
            this.AddressUpDown.TabIndex = 0;
            this.AddressUpDown.ValueChanged += new System.EventHandler(this.AddressUpDown_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "首地址";
            // 
            // MemoryBrowser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 261);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.AddressUpDown);
            this.Name = "MemoryBrowser";
            this.Text = "浏览内存";
            this.Resize += new System.EventHandler(this.MemoryBrowser_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.AddressUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown AddressUpDown;
        private System.Windows.Forms.Label label1;
    }
}