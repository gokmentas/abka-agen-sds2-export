using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Agen_SDS2_Export_Module_v0._2
{
    public partial class Setup : Form
    {
        public Setup()
        {
            InitializeComponent();
        }

        public static string folderPath = "";

        private void Setup_Load(object sender, EventArgs e)
        {
            folderPath = Form1.path;
            bool isSettingsExist = File.Exists(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");

            if (isSettingsExist && txt_Path.Text == @"C:\Agenexport")
            {
                StreamReader sr = new StreamReader(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");
                txt_Path.Text = sr.ReadLine();
                cmbx_Units.Text = sr.ReadLine();
                sr.Close();
            }
            else if (isSettingsExist)
            {
                File.Delete(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");
                FileStream fs = File.Create(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");
                fs.Close();
            }
            else
            {
                FileStream fs = File.Create(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");
                fs.Close();
            }
        }

        private void btn_Browse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                folderPath = dialog.SelectedPath;
                txt_Path.Text = folderPath;
            }
        }

        private void btn_ok_Click(object sender, EventArgs e)
        {
            Form1.path = folderPath;
            bool isSettingsExist = File.Exists(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");

            if (isSettingsExist)
            {
                File.Delete(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");
                FileStream fs = File.Create(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");
                fs.Close();
            }
            else
            {
                FileStream fs = File.Create(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");
                fs.Close();
            }

            StreamWriter sw = new StreamWriter(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");
            sw.WriteLine(txt_Path.Text);
            sw.WriteLine(cmbx_Units.Text);
            sw.Close();

            this.Close();
        }
    }
}
