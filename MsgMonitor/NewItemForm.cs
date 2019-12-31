using MsgMoniter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MsgMonitor
{
    public partial class NewItemForm : Form
    {
        public NewItemForm()
        {
            InitializeComponent();
            comboBoxType.SelectedIndex = 2;
        }

        public WrittenMemoryInfo WMInfo;

        public string Description { get; set; }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Description = textBoxDesc.Text;
                WMInfo = new WrittenMemoryInfo();
                WMInfo.Address = Convert.ToUInt32(textBoxAddress.Text, 16);
                WMInfo.Type = (MemoryDataType)comboBoxType.SelectedIndex;
                switch (WMInfo.Type)
                {
                    case MemoryDataType.Byte:
                        WMInfo.ByteArray = new byte[] { byte.Parse(textBoxValue.Text) };
                        break;
                    case MemoryDataType.Short:
                        WMInfo.ByteArray = BitConverter.GetBytes(short.Parse(textBoxValue.Text));
                        break;
                    case MemoryDataType.Integer:
                        WMInfo.ByteArray = BitConverter.GetBytes(int.Parse(textBoxValue.Text));
                        break;
                    case MemoryDataType.Float:
                        WMInfo.ByteArray = BitConverter.GetBytes(float.Parse(textBoxValue.Text));
                        break;
                    case MemoryDataType.Long:
                        WMInfo.ByteArray = BitConverter.GetBytes(long.Parse(textBoxValue.Text));
                        break;
                    case MemoryDataType.Double:
                        WMInfo.ByteArray = BitConverter.GetBytes(double.Parse(textBoxValue.Text));
                        break;
                    case MemoryDataType.String:
                        WMInfo.ByteArray = Encoding.Default.GetBytes(textBoxValue.Text);
                        break;
                    case MemoryDataType.ByteArray:
                        string[] bytes = textBoxValue.Text.Split(',');
                        WMInfo.ByteArray = new byte[bytes.Length];
                        for (int i = 0; i < bytes.Length; i++)
                            WMInfo.ByteArray[i] = Convert.ToByte(bytes[i], 16);
                        break;
                    default:
                        WMInfo.ByteArray = null;
                        break;
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
