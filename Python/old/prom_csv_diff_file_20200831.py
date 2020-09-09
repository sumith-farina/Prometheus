###############################################################################
#                                                                             #
#                     Prometheus CSV出力用スクリプト                           #
# 前提　                                                                      #
#   対象OS：Windows 7/8/8.1/10                                                 #
#   PowerShellにてPythonが実行できる環境が整っていること、                    #
#   かつ下記ライブラリのインストールがなされていること                         #
# 　 ・csv,datetime,dateutil,io,time,logging,math                              #
# 使い方(powershellでの使用法です)                                            #
#    python prom_csv_diff_file.py [PrometheusのIPアドレス]:9090               #
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
import json
import dict

PROMETHEUS_URL = ''

#範囲指定のquery
RANGE_QUERY_API = '/api/v1/query_range'

#値の取得スパン
#RESOLUTION = '3600s'
#RESOLUTION = '1800s'
RESOLUTION = '24h'

#ファイル存在チェック
def fileExistCheck(filepath):
    fileChk = os.path.exists(filepath) > 0
    if not fileChk:
        print("Specified file is not exist.")
        sys.exit(0)
    return

metricsFile = open('c:\\Prometheus\\metricsList.txt',"r")
metricsList = metricsFile.read()
metricsFile.close()

def GetMetrixNames(url):
    response = requests.get('{0}/api/v1/label/__name__/values'.format(url))
    names = response.json()['data']
    #Return metrix names
    return names

def openIpAddressList(filepath):
    ipAddressFile = open(filepath,"r")
    ipAddressList = ipAddressFile.read()
    ipAddressFile.close()
    ipAddressListSplit = ipAddressList.split()
    return ipAddressListSplit

def WindowsMemoryUsageToCsv():
    winIpLists = openIpAddressList(WINIPADDRESSLIST)

    csMem = 'windows_cs_physical_memory_bytes'
    osMem = 'windows_os_physical_memory_free_bytes'
    #Memory使用率算出
    for winIp in winIpLists:
        metrixName =  '(avg_over_time(' + csMem + '{instance="' + winIp + ':9182"}[1d])-avg_over_time(' + osMem + '{instance="' + winIp + ':9182"}[1d]))/' + csMem + '{instance="' + winIp + ':9182"} * 100'
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
            #「result」の「metric」内「values」情報を取得(timestamp、値のList)
            metricValues = result['values']

            #csv出力先指定
            with open(RESULT_FILE, "a", newline="") as csvFile:
                writer = csv.writer(csvFile)
                #タイムスタンプと値を記載
                for metricValue in metricValues:
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    writer.writerow([winIp,"MemoryUsage",timeDate, metricValue[1]])

    return


def WindowsCpuUsageToCsv():
    winIpLists = openIpAddressList(WINIPADDRESSLIST)

    cpuTimeTotal = 'windows_cpu_time_total'
    #CPU使用率算出用の値(1)を抽出
    for winIp in winIpLists:
        metrixName =  'sum(1-rate(' + cpuTimeTotal + '{instance="' + winIp + ':9182",mode="idle"}[1d]))* 100'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': week_ago_ts, 'end': today_ts, 'step': RESOLUTION})
        #応答が返ってきているか確認
        status = response.json()['status']
        if status == "error":
            logging.error(response.json())
            sys.exit(2)

        #Prometheusの応答から「data」列の「result」を取得
        results = response.json()['data']['result']

        #コア数取得
        metrixName =  '1-rate(' + cpuTimeTotal + '{instance="' + winIp + ':9182",mode="idle"}[1d])'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': week_ago_ts, 'end': today_ts, 'step': RESOLUTION})
        #応答が返ってきているか確認
        status = response.json()['status']
        if status == "error":
            logging.error(response.json())
            sys.exit(2)
        
        coreAmounts = response.json()['data']['result']
        coreAmount = len(coreAmounts)

         #応答の内容を識別
        for result in results:
            #「result」の「metric」内「values」情報を取得(timestamp、値のList)
            metricValues = result['values']

            #csv出力先指定
            with open(RESULT_FILE, "a", newline="") as csvFile:
                writer = csv.writer(csvFile)
                #タイムスタンプと値を記載
                for metricValue in metricValues:
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    cpuUsage = float(metricValue[1])/coreAmount
                    writer.writerow([winIp,"CpuUsage",timeDate, cpuUsage])
    return

#Diskの使用率はどのドライブをレポートとして提出するのか、確認すること
def WindowsDiskUsageToCsv():
    winIpLists = openIpAddressList(WINIPADDRESSLIST)

    diskFreeBytes = 'windows_logical_disk_free_bytes'
    diskSizeBytes = 'windows_logical_disk_size_bytes'
    #Disk使用率算出(Cドライブ)
    for winIp in winIpLists:
        metrixName =  '(1-' + diskFreeBytes + '{instance="' + winIp + ':9182"}/' + diskSizeBytes + '{instance="' + winIp + ':9182",volume="C:"}) * 100'
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
            #「result」の「metric」内「values」情報を取得(timestamp、値のList)
            metricValues = result['values']

            #csv出力先指定
            with open(RESULT_FILE, "a", newline="") as csvFile:
                writer = csv.writer(csvFile)
                #タイムスタンプと値を記載
                for metricValue in metricValues:
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    writer.writerow([winIp,"DiskUsage(C)",timeDate, metricValue[1]])

#   Disk使用率算出(Dドライブ)※追加のドライブが必要な場合はコメントアウトしている部分を解除する
#   また、ドライブ名については確認すること
#    for winIp in winIpLists:
#        metrixName =  '(1-' + diskFreeBytes + '{instance="' + winIp + ':9182"}/' + diskSizeBytes + '{instance="' + winIp + ':9182",volume="D:"}) * 100'
#        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
#        params={'query': metrixName, 'start': week_ago_ts, 'end': today_ts, 'step': RESOLUTION})
#        #応答が返ってきているか確認
#        status = response.json()['status']
#        if status == "error":
#            logging.error(response.json())
#            sys.exit(2)
#
#        #Prometheusの応答から「data」列の「result」を取得
#        results = response.json()['data']['result']
#
#         #応答の内容を識別
#        for result in results:
#            #「result」の「metric」内「values」情報を取得(timestamp、値のList)
#            metricValues = result['values']
#
#            #csv出力先指定
#            with open(RESULT_FILE, "a", newline="") as csvFile:
#                writer = csv.writer(csvFile)
#                #タイムスタンプと値を記載
#                for metricValue in metricValues:
#                    timeDate = datetime.fromtimestamp(metricValue[0])
#                    writer.writerow([winIp,"DiskUsage(D)",timeDate, metricValue[1]])
    return

#NIC名指定が必要(顧客によって変更が必要)
def WindowsNetworkResourceUsageToCsv():
    winIpLists = openIpAddressList(WINIPADDRESSLIST)

    sendTraffic = 'windows_net_bytes_sent_total'
    recieveTraffic = 'windows_net_bytes_received_total'

    for winIp in winIpLists:
        #Send使用率算出
        metrixName =  'rate(' + sendTraffic + '{instance="' + winIp + ':9182"}[1d])'
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
            #「result」の「metric」内「values」情報を取得(timestamp、値のList)
            metricValues = result['values']
            nicName = result['metric'].get('nic')

            #csv出力先指定
            with open(RESULT_FILE, "a", newline="") as csvFile:
                writer = csv.writer(csvFile)
                #タイムスタンプと値を記載
                for metricValue in metricValues:
                    if metricValue[1] == '0':
                        continue
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    writeInfo = 'SendingNetworkTraffic(' + nicName + ')'
                    writer.writerow([winIp,writeInfo,timeDate, metricValue[1]])
        
        #Receive使用率算出
        metrixName =  'rate(' + recieveTraffic + '{instance="' + winIp + ':9182"}[1d])'
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
            #「result」の「metric」内「values」情報を取得(timestamp、値のList)
            metricValues = result['values']
            nicName = result['metric'].get('nic')

            #csv出力先指定
            with open(RESULT_FILE, "a", newline="") as csvFile:
                writer = csv.writer(csvFile)
                #タイムスタンプと値を記載
                for metricValue in metricValues:
                    if metricValue[1] == '0':
                        continue
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    writeInfo = 'RecievingNetworkTraffic(' + nicName + ')'
                    writer.writerow([winIp,writeInfo,timeDate, metricValue[1]])
    return

def NodeMemoryUsageToCsv():
    nodeIpLists = openIpAddressList(NODEIPADDRESSLIST)

    nodeMem = 'node_memory_MemTotal_bytes'
    nodeAvailMem = 'node_memory_MemAvailable_bytes'
    #Memory使用率算出
    for nodeIp in nodeIpLists:
        metrixName =  '(avg_over_time(' + nodeMem + '{instance="' + nodeIp + ':9100"}[1d])-avg_over_time(' + nodeAvailMem + '{instance="' + nodeIp + ':9100"}[1d]))/' \
             + nodeMem + '{instance="' + nodeIp + ':9100"} * 100'
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
            #「result」の「metric」内「values」情報を取得(timestamp、値のList)
            metricValues = result['values']

            #csv出力先指定
            with open(RESULT_FILE, "a", newline="") as csvFile:
                writer = csv.writer(csvFile)
                #タイムスタンプと値を記載
                for metricValue in metricValues:
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    writer.writerow([nodeIp,"MemoryUsage",timeDate, metricValue[1]])

    return


def NodeCpuUsageToCsv():
    nodeIpLists = openIpAddressList(NODEIPADDRESSLIST)

    nodeCpuSeconds = 'node_cpu_seconds_total'
    #CPU使用率算出
    for nodeIp in nodeIpLists:
        metrixName =  'sum((1-rate(' + nodeCpuSeconds + '{instance="' + nodeIp + ':9100",mode="idle"}[1d])))*100'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': week_ago_ts, 'end': today_ts, 'step': RESOLUTION})
        #応答が返ってきているか確認
        status = response.json()['status']
        if status == "error":
            logging.error(response.json())
            sys.exit(2)

        #Prometheusの応答から「data」列の「result」を取得
        results = response.json()['data']['result']

        #コア数取得
        metrixName =  '(1-rate(' + nodeCpuSeconds + '{instance="' + nodeIp + ':9100",mode="idle"}[1d]))*100'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': week_ago_ts, 'end': today_ts, 'step': RESOLUTION})
        #応答が返ってきているか確認
        status = response.json()['status']
        if status == "error":
            logging.error(response.json())
            sys.exit(2)
        
        coreAmounts = response.json()['data']['result']
        coreAmount = len(coreAmounts)

         #応答の内容を識別
        for result in results:
            #「result」の「metric」内「values」情報を取得(timestamp、値のList)
            metricValues = result['values']

            #csv出力先指定
            with open(RESULT_FILE, "a", newline="") as csvFile:
                writer = csv.writer(csvFile)
                #タイムスタンプと値を記載
                for metricValue in metricValues:
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    cpuUsage = float(metricValue[1])/coreAmount
                    writer.writerow([nodeIp,"CpuUsage",timeDate, metricValue[1]])

    return



"""
Prometheus duration data as csv.
"""

#(オプション)レポート追加要素
METRICSLISTPATH='c:\\Prometheus\\metricsList.txt'

#WindowsIPアドレスリスト
WINIPADDRESSLIST = 'c:\\Prometheus\\WinIpList.txt'
fileExistCheck(WINIPADDRESSLIST)

#OtherIPアドレスリスト
NODEIPADDRESSLIST = 'c:\\Prometheus\\NodeIpList.txt'
fileExistCheck(NODEIPADDRESSLIST)

#結果保存用ファイル
RESULT_FILE = 'c:\\Prometheus\\ScrapedDataBetweenMonthAgo.txt'

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
#if len(sys.argv) != 2:
#    print('Usage: {0} http://localhost:9090'.format(sys.argv[0]))
#    sys.exit(1)

#PrometheusからMetricsの名前一覧を取得
#metrixNames=GetMetrixNames(sys.argv[1])
metrixNames=GetMetrixNames("http://192.168.1.212:9090")
writeHeader=True
#PROMETHEUS_URL=sys.argv[1]
PROMETHEUS_URL="http://192.168.1.212:9090"

#基本レポート情報の取得
#WindowsMemoryUsageToCsv()
#WindowsCpuUsageToCsv()
#WindowsDiskUsageToCsv()
#WindowsNetworkResourceUsageToCsv()
#NodeMemoryUsageToCsv()
NodeCpuUsageToCsv()
#NodeDiskUsageToCsv()
#NodeNetworkResourceUsageToCsv()

#(オプション)取得するメトリックスの情報を記載
metricsName = ''

#ファイル存在チェック
fileChk = os.path.exists(METRICSLISTPATH) > 0
if not fileChk:
    print("Optional function is not addapted.")
    sys.exit(0)

metricsFile = open('c:\\Prometheus\\metricsList.txt',"r")
metricsList = metricsFile.read()
metricsFile.close()

if not metricsList:
    print("Optional resource list is empty.")
    sys.exit(0)

#lines1 = metricsList.split('\n') # 改行で区切る(改行文字そのものは戻り値のデータには含まれない)
#1行ごとにリストを読み込む(最後の改行は読み込まない)
metricsListNames = metricsList.splitlines()

#データの取得スクリプト
for metricsName in metricsListNames:
 for metrixName in metrixNames:
    #test：upのみを取得する(ここで取得したいデータを制限する)
     if metrixName != metricsName:
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
          fileName = "C:\\Prometheus\\" + metricName + "-" + metricJob + ".csv"

          #csv出力先指定
#          with open(fileName, "a", newline="") as csvFile:
#            writer = csv.writer(csvFile)

          #ヘッダにquery名、instance名、job名を記載
#            writer.writerow([metricName, metricInst, metricJob])

          #タイムスタンプと値を記載
#            for metricValue in metricValues:
#             timeDate = datetime.fromtimestamp(metricValue[0])
#             writer.writerow([timeDate, metricValue[1]])
