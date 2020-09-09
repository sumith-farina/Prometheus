###############################################################################
#                                                                             #
#                     Prometheus CSV出力用スクリプト                           #
# 前提　                                                                      #
#   PowerShellの実行環境が整っていること、かつ下記ライブラリの                 #
#   インストールがなされていること                                            #
# 　 ・csv,datetime,dateutil,io,time,logging,math                              #
# 使い方(powershellでの使用法です)                                            #
#    python prom_csv_diff_file.py [Prometheusのファイル名]                     #
#                                                                             #
###############################################################################


import csv
from datetime import datetime
from datetime import timedelta
from dateutil.relativedelta import relativedelta
import requests
import sys
import io
import getopt
import time
import logging
import os
import math
def GetMetrixNames(url):
    response = requests.get('{0}/api/v1/label/__name__/values'.format(url))
    names = response.json()['data']
    #Return metrix names
    return names

PROMETHEUS_URL = ''

#範囲指定のquery
RANGE_QUERY_API = '/api/v1/query_range'

#値の取得span(1h)
RESOLUTION = '3600s'

"""
Prometheus duration data as csv.
"""

#現在日付取得
today=datetime.now()
today_ts=today.timestamp()

#1日前取得 #テスト用
yesterday = today - timedelta(days=1)
yesterday_ts = yesterday.timestamp()

#7日前取得 #テスト用
week_ago = today - timedelta(days=7)
week_ago_ts = week_ago.timestamp()

#1か月前取得
month_ago = today - relativedelta(months=1)
month_ago_ts = month_ago.timestamp()

#Prometheusのサーバ指定チェック
if len(sys.argv) != 2:
    print('Usage: {0} http://localhost:9090'.format(sys.argv[0]))
    sys.exit(1)

#PrometheusからMetricsの名前一覧を取得
metrixNames=GetMetrixNames(sys.argv[1])
writeHeader=True
PROMETHEUS_URL=sys.argv[1]

#データの取得スクリプト
for metrixName in metrixNames:
    #test：upのみを取得する(ここで取得したいデータを制限する)
     if metrixName != 'up':
       continue
     #期間指定のqueryの結果をPrometheuから取得(APIの詳細はhttps://prometheus.io/docs/prometheus/latest/querying/api/#range-vectors)
     response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
      params={'query': metrixName, 'start': week_ago_ts, 'end': today_ts, 'step': RESOLUTION})

     #応答が返ってきているか確認
     status = response.json()['status']
     if status == "error":
        logging.error(response.json())
        sys.exit(2)

     #Prometheusの応答から「data」列の「result」を取得
     results = response.json()['data']['result']

     #応答の内容を識別
     for result in results:
          #「result」の「metric」内「__name__」情報を取得(query名)
          metricName = result['metric'].get('__name__')

          #「result」の「metric」内「instance」情報を取得(instance名)
          metricInst = result['metric'].get('instance')

          #「result」の「metric」内「job」情報を取得(job名)
          metricJob = result['metric'].get('job')

          #「result」の「metric」内「values」情報を取得(timestamp、値のList)
          metricValues = result['values']

          #csvファイルへの書き込みオブジェクト作成(取得するresult(job)毎にファイルを作成)
          fileName = "C:\\csv\\" + metricName + "-" + metricJob + ".csv"

          #csv出力先指定
          with open(fileName, "w", newline="") as csvFile:
            writer = csv.writer(csvFile)

          #ヘッダにquery名、instance名、job名を記載
            writer.writerow([metricName, metricInst, metricJob])

          #タイムスタンプと値を記載
            for metricValue in metricValues:
             timeDate = datetime.fromtimestamp(metricValue[0])
             writer.writerow([timeDate, metricValue[1]])
