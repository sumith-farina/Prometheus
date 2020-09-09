###############################################################################
#                                                                             #
#                     Prometheus CSV出力用スクリプト                          #
# 前提　                                                                      #
#  ・対象OS：LinuxベースOS                                                    #
#  ・PrometheusがインストールされていてかつAlertManagerが導入されていること   #
#  ・Python3 がインストールされていること                                     #
#  ・Python3において、以下のライブラリが導入されていること                    #
# 　 ・sys,io,getopt,time,logging,subprocess,os,math,csv,datetime             #
#  ・実行ディレクトリ内に「amtool-stack.sh」が存在すること                    #
#  ・/prometheus/alertmanager/配下に「amtool」が存在すること                  #
#                                                                             #
# 使い方(CentOSでの使用法です)                                                #
#    python3 ./log_check_script.py                                            #
#                                                                             #
###############################################################################

import requests
import sys
import io
import getopt
import time
import logging
import subprocess
import os
import math
import csv
from datetime import datetime
from datetime import timedelta
from dateutil.relativedelta import relativedelta

#Prometheusへ接続し、メトリックス情報全てを収集する
def GetMetricsNames(url):
    response = requests.get('{0}/api/v1/label/__name__/values'.format(url))
    names = response.json()['data']
    return names

#カウントファイルが存在しない場合にカウントファイルを作成する(基本一番最初の起動以外実施しない)
def writeTxt(filename, myCheck):
    if not myCheck:
        with open(filename, "w") as f:
            f.write("0")
            f.close()

#連続エラー回数
MAX_ERROR_COUNT = 3

#バックアップジョブ停止回数
MAX_HOLD_COUNT = 5

#file名(full path)
ErrorFileName = 'ErrorCount.txt'

#本日の実行日時を取得
today=datetime.now()
today_ts=today.timestamp()

#昨日の日時を取得(実行タイミングから1日前)
yesterday=today - timedelta(days=1)
yesterday_ts = yesterday.timestamp()

#PrometheusサーバのURLを指定(Prometheusサーバで実行する場合はlocalhostを選択)
PROMETHEUS_URL = 'http://192.168.1.212:9090'
#PROMETHEUS_URL = 'http://localhost:9090'

#範囲指定の場合のAPI
RANGE_QUERY_API = '/api/v1/query_range'

#メトリックス収集用の時間定義
RESOLUTION = '12h'

#メトリックス収集対象のクエリ
specifiedMetrics = 'error_message'

#エラーカウント書き込みフラグ
chkFlag = 0

#ファイル作成フラグ
newErrorFileFlag = 0

#カウントファイル存在✓
errorfileChk = os.path.exists(ErrorFileName)
writeTxt(ErrorFileName, errorfileChk)

#新規ファイル作成フラグ
if not errorfileChk:
    newErrorFileFlag = 1

#writer = csv.writer(sys.stdout)

#Prometheusからメトリックス情報を取得
metricsNames=GetMetricsNames(PROMETHEUS_URL)

for metricsName in metricsNames:
    # 対象のメトリックス情報ではない場合、以下を実行しない
     if metricsName != specifiedMetrics:
        continue
    # 範囲指定のAPIで確認すると、step時間区切りのデータを指定した過去から今まででのデータを確認する
    # response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API, params={'query': metricsName, 'start': yesterday_ts, 'end': today_ts, 'step': RESOLUTION})
    # 通常のAPIで確認すると現在からRESOULUTION時間前までのデータを遡り確認を行う
    # 今回の場合、count=0の時はmetricsが収集されないため、範囲指定では確認ができない
     response = requests.get('{0}/api/v1/query'.format(PROMETHEUS_URL), params={'query': metricsName+'[' + RESOLUTION + ']'})
     results = response.json()['data']['result']

    # 対象のメトリックス名
     metricName=''

    # 対象のインスタンス名
     metricInst=''

    # 対象のジョブ名
     metricJob=''

    # メトリックス情報の値
     metricValues=''

    # Prometheusへのアクセス結果から情報を取得
     for result in results:
         metricName = result['metric'].get('__name__')
         metricInst = result['metric'].get('instance')
         metricJob = result['metric'].get('job')
         metricValues = result['values']

#     writer.writerow([metricName, metricInst, metricJob])

    # 取得した値が1ならカウントが追加されるため、エラーカウント書き込みフラグを立てる
     for metricValue in metricValues:
         timeDate = datetime.fromtimestamp(metricValue[0])
          #        writer.writerow([timeDate, metricValue[1]])
         metricValueInt = int(metricValue[1])
         if metricValueInt > 0:
            chkFlag = 1
            break


# エラーカウントに記載された値を読み込み
with open(ErrorFileName, "r") as errorCount:
 count = errorCount.read()
 if count == '':
    count = '0'
 countNo = int(count)
errorCount.close()

# 書き込みフラグが立っている時、カウントを追加する、フラグが立っていない場合はカウントを0に初期化する
if chkFlag == 1:
    countNo = countNo + 1
else:
    countNo = 0

#ファイル更新日付取得(タイムスタンプではない形)
updateTime = os.path.getmtime(ErrorFileName)
updateDate = datetime.fromtimestamp(updateTime)
comDate = updateDate.strftime('%Y%m%d')

#今日の日付取得(タイムスタンプではない形)
todayDate = today.fromtimestamp(today_ts)
todayDate = todayDate.strftime('%Y%m%d')


#更新日付と今日日付が同じ場合はファイルの更新を行わず終了
if newErrorFileFlag != 1:
   if (todayDate == comDate) and newErrorFileFlag != 1:
      print("本日、既にログを確認済みです")
      sys.exit(0)

#連続エラー回数チェック、連続エラー回数が指定された値を超えている場合はアラート発報
if countNo >= MAX_ERROR_COUNT:
    #amtool呼び出しシェルスクリプト実行
#    returncode = subprocess.call(['/usr/bin/bash ./amtool-stack.sh'],shell=True)
    countNo = 0

#数値→文字列変換
countTxt = str(countNo)

#エラーカウント数の更新
with open(ErrorFileName, "w") as writeFile:
    writeFile.write(countTxt)
writeFile.close()
