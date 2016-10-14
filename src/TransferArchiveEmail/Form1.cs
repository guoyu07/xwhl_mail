using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TransferArchiveEmail
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            labelResult.Text = "Saving ...";
            txtAttach.Text = "";
            Worker w = new Worker();
            MMail mail = w.Do(this.txtUsername.Text, this.txtMail.Text.Trim());
            labelResult.Text = string.Format("Saved. {0:yyyy/MM/dd HH:mm:ss}", DateTime.Now);
            txtAttach.Text = mail.Attachments;
        }

        private void txtMail_Click(object sender, EventArgs e)
        {
            txtMail.SelectAll();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //new DA().CheckAttachments();
        }
    }
}
