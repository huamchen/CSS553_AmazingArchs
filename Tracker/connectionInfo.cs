using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tracker
{
    public partial class connectionInfo : Form
    {
        Moniter mMoniter;
        public connectionInfo(Moniter Moniter)
        {
            InitializeComponent();
            mMoniter = Moniter;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (mMoniter.createConnection(dbUrl.Text,dbName.Text, username.Text, password.Text, keyword.Text))
            {
                MessageBox.Show("Registe Success!");
                this.Close();
            }
            else
            {
                MessageBox.Show("Registe Fail!");
            }
        }
        private void setInfo(String dbUrl,String dbName, String dbUsername, String dbPassword, String keyword)
        {
            this.dbUrl.Text = dbUrl;
            this.dbName.Text = dbName;
            this.username.Text = dbUsername;
            this.password.Text = dbPassword;
            this.keyword.Text = keyword;
        }
    }
}
