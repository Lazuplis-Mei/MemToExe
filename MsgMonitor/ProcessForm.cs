using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MsgMonitor
{
    public partial class ProcessForm : Form
    {
        public string ProcessId { get; set; }

        public ProcessForm()
        {
            InitializeComponent();
            listView1.ListViewItemSorter = new ListViewItemComparer();
        }

        private void ProcessForm_Load(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            foreach (var process in Process.GetProcesses())
            {
                if (process.MainWindowTitle.Length == 0)
                {
                    ListViewItem listViewItem = new ListViewItem(process.Id.ToString());
                    listViewItem.SubItems.Add(process.ProcessName);
                    listViewItem.SubItems.Add(process.MainWindowTitle);
                    listView1.Items.Add(listViewItem);
                }
                else
                {
                    ListViewItem listViewItem = new ListViewItem(process.Id.ToString());
                    listViewItem.SubItems.Add(process.ProcessName);
                    listViewItem.SubItems.Add(process.MainWindowTitle);
                    listView1.Items.Insert(0, listViewItem);
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                ProcessId = listView1.SelectedItems[0].SubItems[0].Text;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            ProcessForm_Load(null, null);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Button1_Click(null, null);
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewItemComparer comparer = (ListViewItemComparer)listView1.ListViewItemSorter;
            comparer.Reverse = !comparer.Reverse;
            comparer.Column = e.Column;
            listView1.Sort();
        }
    }

    public class ListViewItemComparer : IComparer
    {
        public int Column { get; set; } = -1;
        public bool Reverse { get; set; } = true;
        public int Compare(object i1, object i2)
        {
            if (Column == -1)
                return 0;
            int result = string.Compare(((ListViewItem)i1).SubItems[Column].Text,
                ((ListViewItem)i2).SubItems[Column].Text);
            if (Reverse)
                result = -result;
            return result;
        }
    }
}
