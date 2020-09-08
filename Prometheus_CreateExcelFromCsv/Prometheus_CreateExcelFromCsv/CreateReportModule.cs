﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;


// 「※」マークは基本検証時の確認コードのため、本番時は不要となるので削除もしくはコメントアウトすること

public class CreateExcelFromCsv
{
    public const int MaxLength = 30;

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
        ofd.Filter = "EXCELファイル (*.csv;*.txt)|*.csv,*.txt|すべてのファイル(*.*)|*.*";
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

        // 各Excelオブジェクトの初期化
        Microsoft.Office.Interop.Excel.Application ExcelApp = null;
        Microsoft.Office.Interop.Excel.Workbooks wbs = null;
        Microsoft.Office.Interop.Excel.Workbook wb = null;
        Microsoft.Office.Interop.Excel.Sheets shs = null;
        Microsoft.Office.Interop.Excel.Worksheet ws = null;

        // Excelワークブックの作成
        ExcelApp = new Microsoft.Office.Interop.Excel.Application();
        //ExcelApp.Visible = true; // ※検証用　本番時はコメントアウトもしくは削除するコード
        wbs = ExcelApp.Workbooks;
        wb = wbs.Add();

        // Excelシートの作成
        int count = 1; // sheet用カウント変数
        shs = wb.Sheets; // ワークブックの全てのシートオブジェクト
        List<string> dataName = new List<string>(); // dataNameを格納するList
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

        // dataNameを取得し、dataName(リスト)へ格納
        for (int i = 0; i < csvDicList.Count(); i++)
        {
            // dName にcsvの全ての「DataName」キーの値を検索し、dataNameに含まれていない場合はその「DataName」を追加する
            dName = csvDicList[i]["DataName"];
            if (dataName.Contains(dName) != true)
            {
                dataName.Add(dName);
                if (dName.Length > MaxLength)
                {
                    dName = dName.Substring(0, MaxLength);
                }
                // 一番最初のシート以外はシートを追加する
                if (count != 1)
                {
                    wb.Sheets.Add();
                }
                count = count + 1;
            }
        }

        // 「DataName」で作成されたリスト「dataName」から各シートの名前を一意に決定する
        for (int i = 1; i < count; i++ )
        {
            ws = shs[i];
            // 31文字以上の場合、エクセルのシート名に使用できないため、31文字以降を削除
            if (dataName[i-1].Length > MaxLength)
            {
                dName = dataName[i-1].Substring(0, MaxLength);
            }
            else
            {
                dName = dataName[i-1];
            }
            // ワークシートの名前を変更
            ws.Name = dName;
        }

        // dataNameシートごとのデータでグラフを作成
        for (int i = 0; i < dataName.Count(); i++)
        {
            // シート名からシート番号を取得し、シートオブジェクトを取得
            ws = wb.Sheets[getSheetIndex(dataName[i], wb.Sheets)];
            // ws.Activate();  // ※検証用 ワークシートオブジェクトの取得を確認する
            cells = ws.Cells;

            // ※検証用 「MemoryUsage(%)」のシートだけで確認
            /*
            if (i > 0)
            {
                break;
            }
            */            

            // シート名に応じたデータを取得
            dataDicList = GetSpecifiedDataDictionaryList(ws.Name,csvDicList);

            // IPアドレスの分別(表の縦列になる)
            for (int j = 0; j < dataDicList.Count(); j++)
            {
                ipAddress = dataDicList[j]["IP"];
                if (ipList.Contains(ipAddress) != true)
                {
                    ipList.Add(ipAddress);
                }
            }
            ipList.Sort();

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
                cells[1, j+2].Value = dateList[j];
            }

            // IP情報書込み(Y列)
            for (int j = 0; j < ipList.Count; j++)
            {
                cells[j+2, 1].Value = ipList[j];

            }

            // 表の作成
            cnt = 0;
            while(cnt < dataDicList.Count())
            {
                for(int j = 0; j< ipList.Count(); j++)
                {
                    string cellip = ipList[j];
                    for(int k = 0; k < dateList.Count(); k++)
                    {
                        //string celltm = cells[1, k + 2].Value.ToString();
                        //celltm = celltm.Substring(0, 10);
                        string celltm = dateList[k];
                        if (cellip == dataDicList[cnt]["IP"] && celltm == dataDicList[cnt]["Time"])
                        {
                            cells[j + 2, k + 2].Value = dataDicList[cnt]["Value"];
                            goto NEXT;
                        }
                    }
                }
                NEXT:
                cnt = cnt+1;
            }

            // cell幅調整
            for (int j=1; j <= dateList.Count()+1; j++)
            {
                ws.Columns[j].AutoFit();
            }

            // グラフ作成
            var chartObjs = ws.ChartObjects() as ChartObjects;
            double chartTop = (18.75 * (ipList.Count() + 2));
            double chartLeft = 10;
            double chartWidth = 750;
            double chartHeight = 500;
            var chartObj = chartObjs.Add(chartLeft,chartTop,chartWidth,chartHeight);
            var chart = chartObj.Chart;
            chart.HasTitle = true;
            ChartTitle chtTitle = chart.ChartTitle;
            chtTitle.Text = ws.Name;

            chart.ChartType = XlChartType.xlLine;
            Range chartRange = ws.Range[ws.Cells[1, 1], ws.Cells[ipList.Count() + 1, dateList.Count() + 1]];
            chart.SetSourceData(chartRange);

        }

        //excelファイルの保存
        ws = wb.Sheets[getSheetIndex(dataName[0], wb.Sheets)];
        ws.Activate();
        string timeName = DateTime.Now.ToString("yyyy年MM月dd日");
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

        bRet = true;

        return(bRet);

    }

    // 指定されたワークシート名のインデックスを返すメソッド
    private int getSheetIndex(string sheetName, Microsoft.Office.Interop.Excel.Sheets shs)
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

    private static List<Dictionary<string, string>> GetSpecifiedDataDictionaryList(string sheetName, List<Dictionary<string, string>> csvList)
    {
        List<Dictionary<string, string>> retDicList = new List<Dictionary<string, string>>();
        
        // データ取得
        for (int j = 0; j < csvList.Count(); j++)
        {
            var dic = new Dictionary<string, string>();

            // 「DataName」とシート名が等しいときにretDicListにdictionaryを追加する
            if(csvList[j]["DataName"] == sheetName)
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

        return retDicList;
    }

}
