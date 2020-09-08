using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Prometheus_CreateExcelFromCsv
{
    public partial class Form1 : Form
    {
        CreateExcelFromCsv reportObj = new CreateExcelFromCsv();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // テキストボックスにデフォルトで名前を記載
            //csvFilePath.Text = @"C:\Prometheus\ScrapedDataBetweenMonthAgo.txt";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            csvFilePath.Text = reportObj.selectCsv();

        }

        private void createReportButton_Click(object sender, EventArgs e)
        {
            bool bRet = false;
            bRet = reportObj.createExcelReport(csvFilePath.Text);
            if(bRet != true)
            {
                MessageBox.Show("レポート作成に失敗しました。");
            }
            else
            {
                MessageBox.Show("レポート作成に成功しました。");
                
                string message = "作成したexcelファイルのフォルダを開きますか？";
                string caption = "Open Directory";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result = MessageBox.Show(this, message, caption, buttons, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    string folderPath = System.IO.Path.GetDirectoryName(csvFilePath.Text);
                    Process.Start("EXPLORER.EXE", folderPath);
                }
            }
        }
    }
}
