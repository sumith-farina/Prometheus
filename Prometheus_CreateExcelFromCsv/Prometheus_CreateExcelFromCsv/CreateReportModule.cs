/*******************************************************************************************************************/
/*                                                                                                                 */
/*   Prometheus Excelグラフ掃き出しツール                                                                          */
/*    本ツールは「prom_csv_diff_file.py」により出力されたCSVファイルを基に                                         */
/*    以下のレポートを出力するツールです。                                                                         */
/*     ・基本レポート項目(Memory使用率、CPU使用率、Disk使用率、送受信トラフィック)                                 */
/*     ・オプションレポート項目                                                                                    */
/*                                                                                                                 */
/*                                                                                                                 */
/*                                                                                                                 */
/*                                                                                                                 */
/*******************************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using Prometheus_CreateExcelFromCsv;
using System.Drawing;


// 「※」マークは基本検証時の確認コードのため、本番時は不要となるので削除もしくはコメントアウトすること

public class CreateExcelFromCsv
{
    public const int MaxLength = 30;

    public const int RHeight = 3;

    public String selectCsv()
	{
        //OpenFileDialogクラスのインスタンスを作成
        OpenFileDialog ofd = new OpenFileDialog();

        //はじめのファイル名を指定する
        //はじめに「ファイル名」で表示される文字列を指定する
        ofd.FileName = "";
        //はじめに表示されるフォルダを指定する
        //指定しない（空の文字列）の時は、現在のディレクトリが表示される
        ofd.InitialDirectory = @"C:\";
        //[ファイルの種類]に表示される選択肢を指定する
        //指定しないとすべてのファイルが表示される
        ofd.Filter = "csvファイル (*.csv;*.txt)|*.csv;*.txt|すべてのファイル(*.*)|*.*";
        //[ファイルの種類]ではじめに選択されるものを指定する
        //2番目の「すべてのファイル」が選択されているようにする
        ofd.FilterIndex = 2;
        //タイトルを設定する
        ofd.Title = "開くファイルを選択してください";
        //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
        ofd.RestoreDirectory = true;
        //存在しないファイルの名前が指定されたとき警告を表示する
        //デフォルトでTrueなので指定する必要はない
        ofd.CheckFileExists = true;
        //存在しないパスが指定されたとき警告を表示する
        //デフォルトでTrueなので指定する必要はない
        ofd.CheckPathExists = true;

        //ダイアログを表示する
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            //OKボタンがクリックされたとき、選択されたファイル名を表示する
            // Console.WriteLine(ofd.FileName);
            return (ofd.FileName);
        }
        else
        {
            return ("");
        }
    }

    public bool createExcelReport(string csvFilePath)
    {
        bool bRet = false;

        // ファイルの最低限のチェック
        if(csvFilePath == "")
        {
            MessageBox.Show("ファイルが選択されていません。");
            goto FINISH;
        }

        if (Path.GetExtension(csvFilePath) != ".csv" && Path.GetExtension(csvFilePath) != ".txt")
        {
            MessageBox.Show("csvファイルが選択されていません。");
            goto FINISH;
        }

        // ファイルのディレクトリ名を取得
        string folderPath = System.IO.Path.GetDirectoryName(csvFilePath);

        // csvの内容をListに格納
        List<Dictionary<string,string>> csvList = CsvToList(csvFilePath);

        /*
         * 
         * Listの構成は以下の通り
         * csvList
         *   ┝ [0] Dictionary<[header(0),value],[header(1),value],...,[header(n),value]>
         *   ┝ [1] Dictionary<[header(0),value],[header(1),value],...,[header(n),value]>
         *   ┝ [2] Dictionary<[header(0),value],[header(1),value],...,[header(n),value]>
         *   ┝ [3] Dictionary<[header(0),value],[header(1),value],...,[header(n),value]>
         *   ・
         *   ・
         *   ・
         *   ・
         *   └ [m] Dictionary<[header(0),value],[header(1),value],...,[header(n),value]>
         *   
         *   headerは4つの想定であり、「"IP"」「"DataName"」「"Time"」「"Value"」
         */

        // 実際のグラフ作成処理
        createReportCore(csvList, folderPath);        

        bRet = true;

        FINISH:
            return bRet;
    }

    private static List<Dictionary<string, string>> CsvToList(string path)
    {
        // Shift-JISでcsvファイルを読み込み、「,」で分割
        var parser = new TextFieldParser(path, Encoding.GetEncoding("Shift-JIS"))
        {
            TextFieldType = FieldType.Delimited,
            Delimiters = new string[] { "," }
        };
        // 読み込み(1行読み込みを終了まで実施)
        var rows = new List<string[]>();
        while (!parser.EndOfData)
        {
            rows.Add(parser.ReadFields());
        }

        // 列名設定(1行目のヘッダを「head」に追加
        var header = new List<string>();
        foreach (var head in rows.First())
        {
            header.Add(head);
        }

        // 行追加(1行ずつdicListに「List((ヘッダ,値),(ヘッダ,値),...,(ヘッダ,値))」を追加
        // 返り値の定義
        var dicList = new List<Dictionary<string, string>>();

        // 1行目をスキップした状態で読み込み
        foreach (var row in rows.Skip(1))
        {
            // Listに追加するDictionaryを定義
            var dic = new Dictionary<string, string>();

            // 1行の要素数分Dictionaryに(ヘッダ,値)の組み合わせを追加
            foreach (var i in Enumerable.Range(0, row.Length))
            {
                dic.Add(header[i], row[i]);
            }
            // 1行分のDictionaryをListに追加
            dicList.Add(dic);
        }

        // DictionaryのListを返す
        return dicList;

    }

    private bool createReportCore(List<Dictionary<string, string>> csvDicList, string folderPath)
    {
        bool bRet = false;

        Form1 f = new Form1();
        f = Form1.Form1Instance;

        int rdoButtonValue = f.getRadioButtonValue();

        // 各Excelオブジェクトの初期化
        Microsoft.Office.Interop.Excel.Application ExcelApp = null;
        Microsoft.Office.Interop.Excel.Workbooks wbs = null;
        Microsoft.Office.Interop.Excel.Workbook wb = null;
        Microsoft.Office.Interop.Excel.Sheets shs = null;
        Microsoft.Office.Interop.Excel.Worksheet ws = null;
        Range hiddenRange;

        // Excelワークブックの作成
        ExcelApp = new Microsoft.Office.Interop.Excel.Application();
        //ExcelApp.Visible = true; // ※検証用　本番時はコメントアウトもしくは削除するコード
        wbs = ExcelApp.Workbooks;
        wb = wbs.Add();

        // Excelシートの作成
        int count = 1; // sheet用カウント変数
        shs = wb.Sheets; // ワークブックの全てのシートオブジェクト
        List<string> dataNameList = new List<string>(); // dataNameを格納するList
        List<string> optionList = new List<string>();
        List<string> ipList = new List<string>(); // ipアドレスを格納するList
        List<string> dateList = new List<string>(); // 日付情報を格納するList

        List<Dictionary<string, string>> dataDicList = new List<Dictionary<string, string>>(); // 特定のDataName の値を格納するList(Dictionary形式のデータを保持)

        // 特定データの数をCountするためのカウント変数
        int cnt = 0;

        // 各種変数の初期化
        string dName = "";
        string ipAddress = "";
        string dateTime = "";

        // ワークシートオブジェクトの取得
        ws = shs[count] as Microsoft.Office.Interop.Excel.Worksheet;
        // 一番最初のワークシートを選択
        ws.Select(Type.Missing);
        //cellの初期設定(ワークシートのセルオブジェクト取得)
        var cells = ws.Cells;

        // IPアドレスリストを作成
        for (int i = 0; i < csvDicList.Count(); i++)
        {
            ipAddress = csvDicList[i]["IP"];
            if (ipList.Contains(ipAddress) != true)
            {
                ipList.Add(ipAddress);
                if (count != 1)
                {
                    wb.Sheets.Add();
                }
                count = count + 1;
            }
        }
        ipList.Sort();

        // IPアドレスもしくはホスト名でシート作成
        for (int i = 1; i < count; i++)
        {
            ws = shs[i];
            // 31文字以上の場合、エクセルのシート名に使用できないため、31文字以降を削除
            if (ipList[i - 1].Length > MaxLength)
            {
                ipAddress = ipList[i - 1].Substring(0, MaxLength);
            }
            else
            {
                ipAddress = ipList[i - 1];
            }
            // ワークシートの名前を変更
            ws.Name = ipAddress;
        }

        // ipListシートごとのデータでグラフを作成
        for (int i = 0; i < ipList.Count(); i++)
        {
            // シート名からシート番号を取得し、シートオブジェクトを取得
            ws = wb.Sheets[getSheetIndex(ipList[i], wb.Sheets)];
            cells = ws.Cells;

            // Excelファイルの目盛線を非表示に変更
            ws.Activate();
            ExcelApp.ActiveWindow.DisplayGridlines = false;

            // ※検証用 「MemoryUsage(%)」のシートだけで確認
            /*
            if (i > 0)
            {
                break;
            }
            */            

            // シート名に応じたデータを取得
            dataDicList = getSpecifiedDataDictionaryList(ws.Name,csvDicList);

            // dataNameの分別(表の縦列になる)
            for (int j = 0; j < dataDicList.Count(); j++)
            {
                // dName にcsvの全ての「DataName」キーの値を検索し、dataNameに含まれていない場合はその「DataName」を追加する
                dName = dataDicList[j]["DataName"];
                if (dataNameList.Contains(dName) != true)
                {
                    if (dName.Contains("Option"))
                    {
                        if (optionList.Contains(dName) != true)
                        {
                            optionList.Add(dName);
                        }
                    }
                    else
                    {
                        dataNameList.Add(dName);
                    }
                }
            }
            dataNameList.Sort();
            optionList.Sort();

            // 日付の分別(表の横列になる)
            for (int j = 0; j < dataDicList.Count(); j++)
            {
                dateTime = dataDicList[j]["Time"];
                if (dateList.Contains(dateTime) != true)
                {
                    dateList.Add(dateTime);
                }
            }
            dateList.Sort();

            // 日付情報書込み(x列)
            for (int j = 0; j < dateList.Count; j++)
            {
                cells[1, j + 2].Value = dateList[j];
            }

            // 日付書式変更(日付のみ)
            ws.Range[cells[1,2],cells[1,dateList.Count()+1]].NumberFormat = "d";

            // DataName情報書込み(Y列)
            for (int j = 0; j < dataNameList.Count; j++)
            {
                cells[j+2, 1].Value = dataNameList[j];

            }

            // 表の作成
            cnt = 0;
            while(cnt < dataDicList.Count())
            {
                for(int j = 0; j< dataNameList.Count(); j++)
                {
                    string celldataName = dataNameList[j];
                    for(int k = 0; k < dateList.Count(); k++)
                    {
                        //string celltm = cells[1, k + 2].Value.ToString();
                        //celltm = celltm.Substring(0, 10);
                        string celltm = dateList[k];
                        if (celldataName == dataDicList[cnt]["DataName"] && celltm == dataDicList[cnt]["Time"])
                        {
                            cells[j + 2, k + 2].Value = dataDicList[cnt]["Value"];
                            goto NEXT;
                        }
                    }
                }
                NEXT:
                cnt = cnt+1;
            }

            // cell高さ・幅調整
            for (int j=1; j <= dataNameList.Count() +1; j++)
            {
                ws.Rows[j].RowHeight = 3;
            }

            for (int j=1; j <= dateList.Count()+1; j++)
            {
                ws.Columns[j].AutoFit();
            }

            // グラフ作成
            var chartObjs = ws.ChartObjects() as ChartObjects;
            double chartTop = (RHeight * (dataNameList.Count() + 2));
            double chartLeft = 10;
            double chartWidth = 550;
            double chartHeight = 350;
            var chartObj = chartObjs.Add(chartLeft, chartTop, chartWidth, chartHeight);
            var chart = chartObj.Chart;
            chart.HasTitle = true;
            ChartTitle chtTitle = chart.ChartTitle;
            chtTitle.Text = ws.Name;
            var chartType = chart.ChartType;

            //グラフタイプ選択
            if (f.getRadioButtonValue() == 1)
            {
                chart.ChartType = XlChartType.xlLine;
                chartType = XlChartType.xlLine;
            }
            else if (f.getRadioButtonValue() == 2)
            {
                chart.ChartType = XlChartType.xlColumnStacked;
                chartType = XlChartType.xlColumnStacked;
            }
            else if (f.getRadioButtonValue() == 3)
            {
                chart.ChartType = XlChartType.xlBarStacked;
                chartType = XlChartType.xlBarStacked;
            }
            else
            {
                chart.ChartType = XlChartType.xlLine;
                chartType = XlChartType.xlLine;
            }

            // グラフ描画
            for (int j=0; j<dataNameList.Count;j++)
            {
                chartObj = chartObjs.Add(chartLeft+j*(chartWidth+chartLeft), chartTop, chartWidth, chartHeight);
                chart = chartObj.Chart;
                chart.ChartType = chartType;
                chart.HasTitle = true;
                getChartTitle(chart, dataNameList[j]);
                Range chartRange1 = ws.Range[ws.Cells[1, 1], ws.Cells[1, dateList.Count() + 1]];
                Range chartRange2 = ws.Range[ws.Cells[j + 2, 1], ws.Cells[j + 2, dateList.Count() + 1]];
                Range cRange = ExcelApp.Union(chartRange1, chartRange2);
                //Microsoft.Office.Interop.Excel.Ranges chartRanges = ws.Ranges[chartRange1, chartRange2];
                //chart.SetSourceData(chartRange);
                chart.SetSourceData(cRange);                
            }

            hiddenRange = ws.Range[ws.Cells[1, 1], ws.Cells[dataNameList.Count + 1, dateList.Count + 1]];
            hiddenRange.Font.Color = Color.FromArgb(255,255,255);

//            Microsoft.Office.Interop.Excel.Range dataRange = ws.Range[ws.Cells[2, 1], ws.Cells[2, dataNameList.Count() + 1]];
//            dataRange.Style.Numberformat = "G/標準";
            //日付書式変更
//            Microsoft.Office.Interop.Excel.Range dateRange = ws.Range[ws.Cells[1, 2], ws.Cells[1, dateList.Count() + 1]];
//            dateRange.Style.Numberformat = "dd";

            //hiddenRange.EntireRow.Hidden = true;
        }

        //excelファイルの保存
        ws = wb.Sheets[getSheetIndex(ipList[0], wb.Sheets)];
        ws.Activate();
//        string timeName = DateTime.Now.ToString("yyyy年MM月dd日");
        string timeName = DateTime.Now.ToString("yyyy年MM月");
        wb.SaveAs(folderPath + @"\" + timeName + @"_MonthlyReport.xlsx");
        wb.Close(false); //※検証時はコメントアウト
        ExcelApp.Quit(); //※検証時はコメントアウト

        //Excelのオブジェクトを開放し忘れているとプロセスが落ちないため注意
        Marshal.ReleaseComObject(ws);
        Marshal.ReleaseComObject(shs);
        Marshal.ReleaseComObject(wb);
        Marshal.ReleaseComObject(wbs);
        Marshal.ReleaseComObject(ExcelApp);
        ws = null;
        shs = null;
        wb = null;
        wbs = null;
        ExcelApp = null;

        GC.Collect();

        // オプション項目描画
        if (optionList.Count() > 0)
        {
            bRet = createOptionReport(csvDicList,optionList,folderPath);
            if (bRet != true)
            {
                MessageBox.Show("オプションレポートの作成に失敗しました。");
                goto FIN;
            }
        }

        bRet = true;

        FIN:
        return(bRet);

    }

    private static bool createOptionReport(List<Dictionary<string, string>> csvDicList, List<string> optionList, string folderPath)
    {
        bool bRet = false;

        Form1 f = new Form1();
        f = Form1.Form1Instance;

        int rdoButtonValue = f.getRadioButtonValue();

        // 各Excelオブジェクトの初期化
        Microsoft.Office.Interop.Excel.Application ExcelApp = null;
        Microsoft.Office.Interop.Excel.Workbooks wbs = null;
        Microsoft.Office.Interop.Excel.Workbook wb = null;
        Microsoft.Office.Interop.Excel.Sheets shs = null;
        Microsoft.Office.Interop.Excel.Worksheet ws = null;
        Range hiddenRange;

        // Excelワークブックの作成
        ExcelApp = new Microsoft.Office.Interop.Excel.Application();
        //ExcelApp.Visible = true; // ※検証用　本番時はコメントアウトもしくは削除するコード
        wbs = ExcelApp.Workbooks;
        wb = wbs.Add();

        // Excelシートの作成
        int count = 1; // sheet用カウント変数
        shs = wb.Sheets; // ワークブックの全てのシートオブジェクト
        List<string> ipList = new List<string>(); // ipアドレスを格納するList
        List<string> dateList = new List<string>(); // 日付情報を格納するList
        List<string> ipOptionList = new List<string>();
        List<Dictionary<string, string>> optionDataDicList = new List<Dictionary<string, string>>();
        List<string> optionDataNameList = new List<string>();

        List<Dictionary<string, string>> dataDicList = new List<Dictionary<string, string>>(); // 特定のDataName の値を格納するList(Dictionary形式のデータを保持)

        // 特定データの数をCountするためのカウント変数
        int cnt = 0;

        // 各種変数の初期化
        string ipAddress = "";
        string dateTime = "";
        string dName = "";

        // ワークシートオブジェクトの取得
        ws = shs[count] as Microsoft.Office.Interop.Excel.Worksheet;
        // 一番最初のワークシートを選択
        ws.Select(Type.Missing);
        //cellの初期設定(ワークシートのセルオブジェクト取得)
        var cells = ws.Cells;

        // OptionのDataListを作成
        optionDataDicList = getSpecifiedOptionDictionaryList(csvDicList);

        // IPアドレスリストを作成
        for (int i = 0; i < optionDataDicList.Count(); i++)
        {
            ipAddress = optionDataDicList[i]["IP"];
            if (ipList.Contains(ipAddress) != true)
            {
                ipList.Add(ipAddress);
                if (count != 1)
                {
                    wb.Sheets.Add();
                }
                count = count + 1;
            }
        }
        ipList.Sort();

        // IPアドレスもしくはホスト名でシート作成
        for (int i = 1; i < count; i++)
        {
            ws = shs[i];
            // 31文字以上の場合、エクセルのシート名に使用できないため、31文字以降を削除
            if (ipList[i - 1].Length > MaxLength)
            {
                ipAddress = ipList[i - 1].Substring(0, MaxLength);
            }
            else
            {
                ipAddress = ipList[i - 1];
            }
            // ワークシートの名前を変更
            ws.Name = ipAddress;
        }

        // ipListシートごとのデータでグラフを作成
        for (int i = 0; i < ipList.Count(); i++)
        {
            //OptionDataNameListの初期化
            optionDataNameList.Clear();
            
            // シート名からシート番号を取得し、シートオブジェクトを取得
            ws = wb.Sheets[getSheetIndex(ipList[i], wb.Sheets)];
            cells = ws.Cells;

            // 目盛線の削除
            ws.Activate();
            ExcelApp.ActiveWindow.DisplayGridlines = false;

            // ※検証用 「MemoryUsage(%)」のシートだけで確認
            /*
            if (i > 0)
            {
                break;
            }
            */

            // シート名に応じたデータを取得
            dataDicList = getSpecifiedOptionDictionaryList(csvDicList, ws.Name);

            // OptionListの分別(表の縦列になる)
            optionList.Sort();

            // 日付の分別(表の横列になる)
            for (int j = 0; j < dataDicList.Count(); j++)
            {
                dateTime = dataDicList[j]["Time"];
                if (dateList.Contains(dateTime) != true)
                {
                    dateList.Add(dateTime);
                }
            }
            dateList.Sort();

            // 日付情報書込み(x列)
            for (int j = 0; j < dateList.Count; j++)
            {
                cells[1, j + 2].Value = dateList[j];
            }

            // 日付書式変更(日付のみに変更)
            ws.Range[cells[1, 2], cells[1, dateList.Count() + 1]].NumberFormat = "d";

            // dataNameの分別(表の縦列になる)
            for (int j = 0; j < dataDicList.Count(); j++)
            {
                // dName にcsvの全ての「DataName」キーの値を検索し、dataNameに含まれていない場合はその「DataName」を追加する
                dName = dataDicList[j]["DataName"];
                if (optionDataNameList.Contains(dName) != true)
                {
                        optionDataNameList.Add(dName);
                }
            }
            optionDataNameList.Sort();

            // DataName情報書込み(Y列)
            for (int j = 0; j < optionDataNameList.Count; j++)
            {
                cells[j + 2, 1].Value = optionDataNameList[j];

            }

            // 表の作成
            cnt = 0;
            while (cnt < dataDicList.Count())
            {
                for (int j = 0; j < optionDataNameList.Count(); j++)
                {
                    string celldataName = optionDataNameList[j];
                    for (int k = 0; k < dateList.Count(); k++)
                    {
                        //string celltm = cells[1, k + 2].Value.ToString();
                        //celltm = celltm.Substring(0, 10);
                        string celltm = dateList[k];
                        if (celldataName == dataDicList[cnt]["DataName"] && celltm == dataDicList[cnt]["Time"])
                        {
                            cells[j + 2, k + 2].Value = dataDicList[cnt]["Value"];
                            goto NEXT;
                        }
                    }
                }
                NEXT:
                cnt = cnt + 1;
            }

            // cell高さ・幅調整
            for (int j = 1; j <= optionDataNameList.Count() + 1; j++)
            {
                ws.Rows[j].RowHeight = 3;
            }

            for (int j = 1; j <= dateList.Count() + 1; j++)
            {
                ws.Columns[j].AutoFit();
            }

            // グラフ作成
            var chartObjs = ws.ChartObjects() as ChartObjects;
            double chartTop = (RHeight * (optionList.Count() + 2));
            double chartLeft = 10;
            double chartWidth = 550;
            double chartHeight = 350;
            var chartObj = chartObjs.Add(chartLeft, chartTop, chartWidth, chartHeight);
            var chart = chartObj.Chart;
            chart.HasTitle = true;
            ChartTitle chtTitle = chart.ChartTitle;
            chtTitle.Text = ws.Name;
            var chartType = chart.ChartType;

            //グラフタイプ選択
            if (f.getRadioButtonValue() == 1)
            {
                chart.ChartType = XlChartType.xlLine;
                chartType = XlChartType.xlLine;
            }
            else if (f.getRadioButtonValue() == 2)
            {
                chart.ChartType = XlChartType.xlColumnStacked;
                chartType = XlChartType.xlColumnStacked;
            }
            else if (f.getRadioButtonValue() == 3)
            {
                chart.ChartType = XlChartType.xlBarStacked;
                chartType = XlChartType.xlBarStacked;
            }
            else
            {
                chart.ChartType = XlChartType.xlLine;
                chartType = XlChartType.xlLine;
            }

            // グラフ描画
            for (int j = 0; j < optionDataNameList.Count; j++)
            {
                chartObj = chartObjs.Add(chartLeft + j * (chartWidth + chartLeft), chartTop, chartWidth, chartHeight);
                chart = chartObj.Chart;
                chart.ChartType = chartType;
                chart.HasTitle = true;
                //オプション項目にタイトルが必要な場合は以下のメソッドを使用すること
                //getChartTitleOption(chart, optionDataNameList[j]);
                Range chartRange1 = ws.Range[ws.Cells[1, 1], ws.Cells[1, dateList.Count() + 1]];
                Range chartRange2 = ws.Range[ws.Cells[j + 2, 1], ws.Cells[j + 2, dateList.Count() + 1]];
                Range test = ws.Range[chartRange1, chartRange2];
                Range cRange = ExcelApp.Union(chartRange1, chartRange2);
                //Microsoft.Office.Interop.Excel.Ranges chartRanges = ws.Ranges[chartRange1, chartRange2];
                //chart.SetSourceData(chartRange);
                chart.SetSourceData(cRange);

            }
            // データの文字列の色を「白」に変更し、背景画面で見えないようにする
            hiddenRange = ws.Range[ws.Cells[1, 1], ws.Cells[optionDataNameList.Count + 1, dateList.Count + 1]];
            hiddenRange.Font.Color = Color.FromArgb(255, 255, 255);

        }

        //excelファイルの保存
        ws = wb.Sheets[getSheetIndex(ipList[0], wb.Sheets)];
        ws.Activate();
        string timeName = DateTime.Now.ToString("yyyy年MM月");
        wb.SaveAs(folderPath + @"\" + timeName + @"_MonthlyOptionReport.xlsx");
        wb.Close(false); //※検証時はコメントアウト
        ExcelApp.Quit(); //※検証時はコメントアウト

        //Excelのオブジェクトを開放し忘れているとプロセスが落ちないため注意
        Marshal.ReleaseComObject(ws);
        Marshal.ReleaseComObject(shs);
        Marshal.ReleaseComObject(wb);
        Marshal.ReleaseComObject(wbs);
        Marshal.ReleaseComObject(ExcelApp);
        ws = null;
        shs = null;
        wb = null;
        wbs = null;
        ExcelApp = null;

        GC.Collect();

        bRet = true;

        return (bRet);

    }


    // 指定されたワークシート名のインデックスを返すメソッド
    private static int getSheetIndex(string sheetName, Microsoft.Office.Interop.Excel.Sheets shs)
    {
        int i = 0;
        foreach (Microsoft.Office.Interop.Excel.Worksheet sh in shs)
        {
            if (sheetName == sh.Name)
            {
                return i + 1;
            }
            i += 1;
        }
        return 0;
    }


    // シート名と一致した値をList型でまとめて返すメソッド
    private static List<Dictionary<string, string>> getSpecifiedDataDictionaryList(string sheetName, List<Dictionary<string, string>> csvList)
    {
        List<Dictionary<string, string>> retDicList = new List<Dictionary<string, string>>();
        
        // データ取得
        for (int j = 0; j < csvList.Count(); j++)
        {
            var dic = new Dictionary<string, string>();

            // 「IPアドレス」とシート名が等しいときにretDicListにdictionaryを追加する
            if(csvList[j]["IP"] == sheetName)
            {
                dic.Add("IP", csvList[j]["IP"]);
                dic.Add("DataName", csvList[j]["DataName"]);
                dic.Add("Time", csvList[j]["Time"]);
                dic.Add("Value", csvList[j]["Value"]);
                retDicList.Add(dic);
            }
        }

        // retDicListが空の場合、返す値が存在しないため、エラーを出力してしまうので、その対策
        if(retDicList == null && retDicList.Count() == 0)
        {
            var errorDic = new Dictionary<string, string>();
            errorDic.Add("empty", "empty");
            retDicList.Add(errorDic);
        }

        // DictionaryのList(DataNameでフィルターをかけられた)を返す
        return retDicList;
    }

    // 全てのデータからオプションだけのデータを返すメソッド
    private static List<Dictionary<string, string>> getSpecifiedOptionDictionaryList(List<Dictionary<string, string>> csvList)
    {
        List<Dictionary<string, string>> retDicList = new List<Dictionary<string, string>>();

        // データ取得
        for (int j = 0; j < csvList.Count(); j++)
        {
            var dic = new Dictionary<string, string>();

            // 「IPアドレス」とシート名が等しいときにretDicListにdictionaryを追加する
            if (csvList[j]["DataName"].Contains("Option"))
            {
                dic.Add("IP", csvList[j]["IP"]);
                dic.Add("DataName", csvList[j]["DataName"]);
                dic.Add("Time", csvList[j]["Time"]);
                dic.Add("Value", csvList[j]["Value"]);
                retDicList.Add(dic);
            }
        }

        // retDicListが空の場合、返す値が存在しないため、エラーを出力してしまうので、その対策
        if (retDicList == null && retDicList.Count() == 0)
        {
            var errorDic = new Dictionary<string, string>();
            errorDic.Add("empty", "empty");
            retDicList.Add(errorDic);
        }

        // DictionaryのList(DataNameでフィルターをかけられた)を返す
        return retDicList;
    }

    // 全てのデータから指定されたIPアドレスのデータだけをList型で返すメソッド
    private static List<Dictionary<string, string>> getSpecifiedOptionDictionaryList(List<Dictionary<string, string>> csvList, string ipAddress)
    {
        List<Dictionary<string, string>> retDicList = new List<Dictionary<string, string>>();

        // データ取得
        for (int j = 0; j < csvList.Count(); j++)
        {
            var dic = new Dictionary<string, string>();

            // 「IPアドレス」とシート名が等しいときにretDicListにdictionaryを追加する
            if (csvList[j]["DataName"].Contains("Option") && csvList[j]["IP"] == ipAddress)
            {
                dic.Add("IP", csvList[j]["IP"]);
                dic.Add("DataName", csvList[j]["DataName"]);
                dic.Add("Time", csvList[j]["Time"]);
                dic.Add("Value", csvList[j]["Value"]);
                retDicList.Add(dic);
            }
        }

        // retDicListが空の場合、返す値が存在しないため、エラーを出力してしまうので、その対策
        if (retDicList == null && retDicList.Count() == 0)
        {
            var errorDic = new Dictionary<string, string>();
            errorDic.Add("empty", "empty");
            retDicList.Add(errorDic);
        }

        // DictionaryのList(DataNameでフィルターをかけられた)を返す
        return retDicList;
    }

    private static void getChartTitle(Chart chart, String dataName)
    {
        DateTime dt = DateTime.Now;
        string result = dt.ToString("MM月");

        ChartTitle chartTitle = chart.ChartTitle;
        var yAxis = (Microsoft.Office.Interop.Excel.Axis)chart.Axes(Microsoft.Office.Interop.Excel.XlAxisType.xlValue, Microsoft.Office.Interop.Excel.XlAxisGroup.xlPrimary);
        yAxis.HasTitle = true;
        var xAxis = (Microsoft.Office.Interop.Excel.Axis)chart.Axes(Microsoft.Office.Interop.Excel.XlAxisType.xlCategory, Microsoft.Office.Interop.Excel.XlAxisGroup.xlPrimary);
        //xAxis.HasTitle = true;
        //xAxis.AxisTitle.Text = result;

        if (dataName.Contains("MemoryUsage"))
        {
            chartTitle.Text = "メモリ使用率(%)";
            yAxis.AxisTitle.Text = "%";    
        }
        else if (dataName.Contains("CpuUsage"))
        {
            chartTitle.Text = "CPU使用率(%)";
            yAxis.AxisTitle.Text = "%";
        }
        else if (dataName.Contains("DiskUsage"))
        {
            chartTitle.Text = "Disk使用率(%)";
            yAxis.AxisTitle.Text = "%";
        }
        else if (dataName.Contains("Outbound"))
        {
            chartTitle.Text = "送信トラフィック";
            yAxis.AxisTitle.Text = "bps";
        }
        else if (dataName.Contains("Incoming"))
        {
            chartTitle.Text = "受信トラフィック";
            yAxis.AxisTitle.Text = "bps";
        }

    }

    //オプション項目については、顧客別対応となるため以下の内容を更新して対応する
    private static void getChartTitleOption(Chart chart, String dataName)
    {
        DateTime dt = DateTime.Now;
        string result = dt.ToString("MM月");

        ChartTitle chartTitle = chart.ChartTitle;
        var yAxis = (Microsoft.Office.Interop.Excel.Axis)chart.Axes(Microsoft.Office.Interop.Excel.XlAxisType.xlValue, Microsoft.Office.Interop.Excel.XlAxisGroup.xlPrimary);
        yAxis.HasTitle = true;
        var xAxis = (Microsoft.Office.Interop.Excel.Axis)chart.Axes(Microsoft.Office.Interop.Excel.XlAxisType.xlCategory, Microsoft.Office.Interop.Excel.XlAxisGroup.xlPrimary);
        xAxis.HasTitle = true;
        xAxis.AxisTitle.Text = result;
    }

}
