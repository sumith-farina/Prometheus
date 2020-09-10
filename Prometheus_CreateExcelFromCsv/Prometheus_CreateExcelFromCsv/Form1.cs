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
        private static Form1 _form1Instance;

        CreateExcelFromCsv reportObj = new CreateExcelFromCsv();

        public Form1()
        {
            InitializeComponent();
        }

        public static Form1 Form1Instance
        {
            get
            {
                return _form1Instance;
            }

            set
            {
                _form1Instance = value;
            }
        }

        public int getRadioButtonValue()
        {
            if(rdoLineButton.Checked == true)
            {
                return (int)RadioButtonValue.rdoLineButton; 
            }
            else if (rdoBarButton.Checked == true)
            {
                return (int)RadioButtonValue.rdoBarButton;
            }
            else if (rdoColumnButton.Checked == true)
            {
                return (int)RadioButtonValue.rdoColumnButton;
            }
            else
            {
                return (int)RadioButtonValue.NoSelect;
            }

        }

        public int getReportButtonValue()
        {
            if (ReportBaseButton.Checked == true)
            {
                return (int)ReportOptionButtonValue.rdoBase;
            }
            else if (ReportBaseOnly.Checked == true)
            {
                return (int)ReportOptionButtonValue.rdoBaseOnly;
            }
            else if (ReportOptionOnly.Checked == true)
            {
                return (int)ReportOptionButtonValue.rdoOptionOnly;
            }
            else
            {
                return (int)ReportOptionButtonValue.NoSelect;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // テキストボックスにデフォルトで名前を記載
            //csvFilePath.Text = @"C:\Prometheus\ScrapedDataBetweenMonthAgo.txt";
            Form1.Form1Instance = this;
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
                
                // Excelファイルのフォルダを開くか確認
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

        private void csvFilePath_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            //コントロール内にドラッグされたとき実行される
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                //ドラッグされたデータ形式を調べ、ファイルのときはコピーとする
                e.Effect = DragDropEffects.Copy;
            else
                //ファイル以外は受け付けない
                e.Effect = DragDropEffects.None;
        }

        private void csvFilePath_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            //コントロール内にドロップされたとき実行される
            //ドロップされたファイル名を取得する
            string[] fileName = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            foreach (string dropItem in fileName)
            {
                csvFilePath.Clear();
                csvFilePath.Text=dropItem;
            }
        }

        public enum RadioButtonValue
        {
            rdoLineButton = 1,
            rdoColumnButton,
            rdoBarButton,
            NoSelect = -1
        }

        public enum ReportOptionButtonValue
        {
            rdoBase = 1,
            rdoBaseOnly,
            rdoOptionOnly,
            NoSelect = -1
        }
    }
}
