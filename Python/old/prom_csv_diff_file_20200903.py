###############################################################################
#                                                                             #
#                     Prometheus CSV出力用スクリプト                           #
# 　前提　                                                                    #
#   対象OS：Windows 7/8/8.1/10 or LinuxOS                                     #
#   PowerShellもしくはコマンドにてPythonが実行できる環境が整っていること、        #
#   かつ下記ライブラリのインストールがなされていること                            #
# 　 ・csv,datetime,dateutil,io,time,logging,math,requests,sys,getopt,        #
#      json,dict                                                             #
#                                                                            #
# 　※本説明は基本的にWindowsで使用する前提で記載しております。                   #
#   　Linuxでの実装の場合、ファイルの格納先を変更する必要があります。              #
#                                                                             #
# 　使い方                                                                     #
#    [Windowsの場合(PowerShellにて実行)]                                       #
#    python prom_csv_diff_file.py [PrometheusのIPアドレス]:9090                #
#    [LinuxOSの場合]                                                          #
#    python3 ./prom_csv_diff_file.py [PrometheusのIPアドレス]:9090             #
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

#############################################################################
#                                                                           #
# 　　　　　　　　　　　　　        各種定義                                   #
#                                                                           #
############################################################################# 

PROMETHEUS_URL = ''

#範囲指定のquery
RANGE_QUERY_API = '/api/v1/query_range'

#値の取得スパン
#RESOLUTION = '3600s'
#RESOLUTION = '1800s'
RESOLUTION = '24h'

###########################################################################################################
#     C:\Prometheus\ScrapedDataBetweenMonthAgo.txt                                                        #
#      ファイルの仕様:                                                                                     #
#         本スクリプトによるレポートをcsv形式で出力する                                                       #
#         例) 「IPｱﾄﾞﾚｽ,調査対象名,日時,値」                                                                 #
#             192.168.1.211,MemoryUsage(%),2020-08-25 11:02:29.334000,92.73865851276214                   #
#             ・                                                                                          #
#             ・                                                                                          #
#             ・                                                                                          #
#             (以下基本項目以外はOption*:[metric名]で表記される)                                             #
#             192.168.1.200:9182,Option1:windows_cpu_time_total,2020-08-27 11:02:29.334000,1342.34375     #
# #########################################################################################################
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

#取得スパン(ここで取得スパンを変更する)
span = month_ago_ts

#############################################################################
#                                                                           #
# fileExistCheck(string filepath)                                           #
#   「filepath」で指定されたfilepathに対象ファイルが存在するか確認              #
#     存在しない場合は、プリント文を返し、プログラムを終了する                   #
#                                                                           #
############################################################################# 

def fileExistCheck(filepath):
    fileChk = os.path.exists(filepath) > 0
    if not fileChk:
        print("Specified file is not exist.")
        sys.exit(0)
    return fileChk

#############################################################################
#                                                                           #
# GetMatrixNames(string url)                                                #
#   「url」で指定されたPrometheusのWebクライアントへアクセスし、                #
#     応答時の「data」全てを返す関数                                          #
#                                                                           #
############################################################################# 

def GetMetrixNames(url):
    response = requests.get('{0}/api/v1/label/__name__/values'.format(url))
    names = response.json()['data']
    #Return metrix names
    return names

#############################################################################
#                                                                           #
# openIpAddressList(string filepath)                                        #
#   「filepath」で指定されたfilepathのIPアドレスリストを開き、                  #
#     アドレスリストを配列に格納して配列を返す関数                              #
#                                                                           #
############################################################################# 

def openIpAddressList(filepath):
    ipAddressFile = open(filepath,"r")
    ipAddressList = ipAddressFile.read()
    ipAddressFile.close()
    ipAddressListSplit = ipAddressList.split()
    return ipAddressListSplit

#############################################################################
#                                                                           #
# WindowsMemoryUsageToCsv()                                             　　#
#   windows_Exporterが動作している対象のWindowsシステムにおいて、          　　#
#   IPアドレスを利用してメトリックス情報を取得しメモリの使用率をCSV出力する関数　 #
#                                                                           #
############################################################################# 

def WindowsMemoryUsageToCsv():
    winIpLists = openIpAddressList(WINIPADDRESSLIST)

    csMem = 'windows_cs_physical_memory_bytes'
    osMem = 'windows_os_physical_memory_free_bytes'
    #Memory使用率算出
    for winIp in winIpLists:
        metrixName =  '(avg_over_time(' + csMem + '{instance="' + winIp + ':9182"}[1d])-avg_over_time(' \
            + osMem + '{instance="' + winIp + ':9182"}[1d]))/' + csMem + '{instance="' + winIp + ':9182"} * 100'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
                    writer.writerow([winIp,"MemoryUsage(%)",timeDate, metricValue[1]])

    return

#############################################################################
#                                                                           #
# WindowsCpuUsageToCsv()                                                　　#
#   windows_Exporterが動作している対象のWindowsシステムにおいて、          　　#
#   IPアドレスを利用してメトリックス情報を取得しCPUの使用率をCSV出力する関数　   #
#                                                                           #
############################################################################# 

def WindowsCpuUsageToCsv():
    winIpLists = openIpAddressList(WINIPADDRESSLIST)

    cpuTimeTotal = 'windows_cpu_time_total'
    #CPU使用率を抽出(全コアの合計値を算出しているため、後程コア数で平均を取得する)
    for winIp in winIpLists:
        metrixName =  'sum(1-rate(' + cpuTimeTotal + '{instance="' + winIp + ':9182",mode="idle"}[1d]))* 100'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
                    #コア数で除算し、平均値を抽出
                    cpuUsage = float(metricValue[1])/coreAmount
                    writer.writerow([winIp,"CpuUsage(%)",timeDate, cpuUsage])
    return

#############################################################################
#                                                                           #
# WindowsDiskUsageToCsv()                                               　　#
#   windows_Exporterが動作している対象のWindowsシステムにおいて、          　　#
#   IPアドレスを利用してメトリックス情報を取得しdiskの使用率をCSV出力する関数    #
#                                                                           #
############################################################################# 

#Diskの使用率はどのドライブをレポートとして提出するのか、確認すること
def WindowsDiskUsageToCsv():
    winIpLists = openIpAddressList(WINIPADDRESSLIST)

    diskFreeBytes = 'windows_logical_disk_free_bytes'
    diskSizeBytes = 'windows_logical_disk_size_bytes'
    #Disk使用率算出(Cドライブ)
    for winIp in winIpLists:
        metrixName =  '(1-' + diskFreeBytes + '{instance="' + winIp + ':9182"}/' + diskSizeBytes + '{instance="' + winIp + ':9182",volume="C:"}) * 100'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
                    writer.writerow([winIp,"DiskUsage(%)",timeDate, metricValue[1]])

#   Disk使用率算出(Dドライブ)※追加のドライブが必要な場合はコメントアウトしている部分を解除する
#   また、ドライブ名については確認すること
#        metrixName =  '(1-' + diskFreeBytes + '{instance="' + winIp + ':9182"}/' + diskSizeBytes + \
#            '{instance="' + winIp + ':9182",volume="D:"}) * 100'
#        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
#        params={'query': metrixName, 'start': week_ago_ts, 'end': today_ts, 'step': RESOLUTION})
#        status = response.json()['status']
#        if status == "error":
#            logging.error(response.json())
#            sys.exit(2)
#
#        results = response.json()['data']['result']
#        for result in results:
#            metricValues = result['values']
#
#            with open(RESULT_FILE, "a", newline="") as csvFile:
#                writer = csv.writer(csvFile)
#                for metricValue in metricValues:
#                    timeDate = datetime.fromtimestamp(metricValue[0])
#                    writer.writerow([winIp,"DiskUsage(D)",timeDate, metricValue[1]])
    return


#############################################################################
#                                                                           #
# WindowsNetworkResourceUsageToCsv()                                    　　#
#   windows_Exporterが動作している対象のWindowsシステムにおいて、          　　#
#   IPアドレスを利用してメトリックス情報を取得しネットワークトラフィックの        #
# 　平均使用byteをcsv出力する関数                                        　   #
#                                                                           #
############################################################################# 

#NIC名指定が必要(顧客によって変更が必要)
def WindowsNetworkResourceUsageToCsv():
    winIpLists = openIpAddressList(WINIPADDRESSLIST)

    sendTraffic = 'windows_net_bytes_sent_total'
    receiveTraffic = 'windows_net_bytes_received_total'

    for winIp in winIpLists:
        #送信トラフィック算出
        metrixName =  'rate(' + sendTraffic + '{instance="' + winIp + ':9182"}[1d])>0'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    writeInfo = 'SendingNetworkTraffic(' + nicName + ')'
                    writer.writerow([winIp,writeInfo,timeDate, metricValue[1]])
        
        #受信トラフィック算出
        metrixName =  'rate(' + receiveTraffic + '{instance="' + winIp + ':9182"}[1d])>0'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    writeInfo = 'ReceivingNetworkTraffic(' + nicName + ')'
                    writer.writerow([winIp,writeInfo,timeDate, metricValue[1]])
    return

#############################################################################
#                                                                           #
# NodeMemoryUsageToCsv()                                                　　#
#   node_Exporterにて監視している対象のLinuxOSについて、                   　　#
#   IPアドレスを利用してメトリックス情報を取得しメモリの使用率をCSV出力する関数   #
#                                                                           #
############################################################################# 

def NodeMemoryUsageToCsv():
    nodeIpLists = openIpAddressList(NODEIPADDRESSLIST)

    nodeMem = 'node_memory_MemTotal_bytes'
    nodeAvailMem = 'node_memory_MemAvailable_bytes'
    #Memory使用率算出
    for nodeIp in nodeIpLists:
        metrixName =  '(avg_over_time(' + nodeMem + '{instance="' + nodeIp + ':9100"}[1d])-avg_over_time(' + nodeAvailMem + '{instance="' + nodeIp + ':9100"}[1d]))/' \
             + nodeMem + '{instance="' + nodeIp + ':9100"} * 100'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
                    writer.writerow([nodeIp,"MemoryUsage(%)",timeDate, metricValue[1]])

    return


#############################################################################
#                                                                           #
# NodeCpuUsageToCsv()                                                   　　#
#   node_Exporterにて監視している対象のLinuxOSについて、                   　　#
#   IPアドレスを利用してメトリックス情報を取得しCPUの使用率をCSV出力する関数　    #
#                                                                           #
############################################################################# 

def NodeCpuUsageToCsv():
    nodeIpLists = openIpAddressList(NODEIPADDRESSLIST)

    nodeCpuSeconds = 'node_cpu_seconds_total'
    #CPU使用率算出
    for nodeIp in nodeIpLists:
        metrixName =  'sum((1-rate(' + nodeCpuSeconds + '{instance="' + nodeIp + ':9100",mode="idle"}[1d])))*100'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
                    writer.writerow([nodeIp,"CpuUsage(%)",timeDate, cpuUsage])

    return

#############################################################################
#                                                                           #
# NodeDiskUsageToCsv()                                                   　 #
#   node_Exporterにて監視している対象のLinuxOSについて、                   　　#
#   IPアドレスを利用してメトリックス情報を取得しDiskの使用率をCSV出力する関数　   #
#                                                                           #
############################################################################# 

def NodeDiskUsageToCsv():
    nodeIpLists = openIpAddressList(NODEIPADDRESSLIST)

    nodeDiskFree = 'node_filesystem_free_bytes'
    nodeDiskSize = 'node_filesystem_size_bytes'
    #Disk使用率算出
    for nodeIp in nodeIpLists:
        metrixName =  '(1-(avg_over_time(' + nodeDiskFree + '{instance="' + nodeIp + ':9100",fstype=~"ext4|xfs",mountpoint="/"}[1d])/avg_over_time('+ \
            nodeDiskSize + '{instance="localhost:9100"}[1d])))*100'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
                    writer.writerow([nodeIp,"DiskUsage(%)",timeDate, metricValue[1]])

    return


#############################################################################
#                                                                           #
# NodeNetworkResourceUsageToCsv()                                    　　   #
#   node_Exporterにて監視している対象のLinuxOSについて、                   　　#
#   IPアドレスを利用してメトリックス情報を取得しネットワークトラフィックの        #
# 　平均使用byteをcsv出力する関数                                             #
#                                         　                                #
############################################################################# 

def NodeNetworkResourceUsageToCsv():
    nodeIpLists = openIpAddressList(NODEIPADDRESSLIST)

    sendTraffic = 'node_network_transmit_bytes_total'
    receiveTraffic = 'node_network_receive_bytes_total'
    #送信トラフィック算出
    for nodeIp in nodeIpLists:
        metrixName =  'irate(' + sendTraffic + '{instance="' + nodeIp + ':9100",device!~"tap.*|veth.*|br.*|docker.*|virbr*|lo*"}[1d])*8>0'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
            nicName = result['metric'].get('device')
            #csv出力先指定
            with open(RESULT_FILE, "a", newline="") as csvFile:
                writer = csv.writer(csvFile)
                #タイムスタンプと値を記載
                for metricValue in metricValues:
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    writeInfo = 'SendingTraffic(' + nicName + ')'
                    writer.writerow([nodeIp,writeInfo,timeDate, metricValue[1]])

        #受信トラフィック算出
        metrixName =  'irate(' + receiveTraffic + '{instance="' + nodeIp + ':9100",device!~"tap.*|veth.*|br.*|docker.*|virbr*|lo*"}[1d])*8>0'
        response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
        params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})
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
            nicName = result['metric'].get('device')
            #csv出力先指定
            with open(RESULT_FILE, "a", newline="") as csvFile:
                writer = csv.writer(csvFile)
                #タイムスタンプと値を記載
                for metricValue in metricValues:
                    timeDate = datetime.fromtimestamp(metricValue[0])
                    writeInfo = 'ReceivingTraffic(' + nicName + ')'
                    writer.writerow([nodeIp,writeInfo,timeDate, metricValue[1]])

    return


#以下メイン関数
"""
Prometheus duration data as csv.
"""

#########################################################################
#     C:\Prometheus\metricsList.txt                                     #
#      ファイルの仕様:                                                   #
#         1行毎にレポートに追加したい項目の名前を追加する                   #
#         例)                                                           #
#             windows_cpu_time_total                                    #
#             node_cpu_total                                            #
#             hrStorageAllocationUnit                                   #
# #######################################################################  

#(オプション)レポート追加要素
METRICSLISTPATH='c:\\Prometheus\\metricsList.txt'


#########################################################################
#     C:\Prometheus\WinIpList.txt                                       #
#      ファイルの仕様:                                                   #
#         1行毎に監視対象であるWindowsのIPアドレスを追加する                #
#         例)                                                           #
#             192.168.1.200                                             #
#             192.168.1.211                                             #
# #######################################################################  

#WindowsIPアドレスリスト
WINIPADDRESSLIST = 'c:\\Prometheus\\WinIpList.txt'
bWinIpFileChk = fileExistCheck(WINIPADDRESSLIST)


#########################################################################
#     C:\Prometheus\NodeIpList.txt                                      #
#      ファイルの仕様:                                                   #
#         1行毎に監視対象であるWindows以外のIPアドレスを追加する            #
#         例)                                                           #
#             192.168.1.212                                             #
#             192.168.1.215                                             #
# #######################################################################  

#NodeIPアドレスリスト
NODEIPADDRESSLIST = 'c:\\Prometheus\\NodeIpList.txt'
nodeIpFileChk = fileExistCheck(NODEIPADDRESSLIST)


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

#ヘッダ書込み処理
with open(RESULT_FILE, "a", newline="") as csvFile:
    writer = csv.writer(csvFile)
    writer.writerow(["IP","DataName","Time","Value"])

#基本レポート情報の取得
#Windowsのメモリ使用率を取得しcsvに出力
WindowsMemoryUsageToCsv()
#WindowsのCPU使用率を取得しcsvに出力
WindowsCpuUsageToCsv()
#WindowsのDisk使用率を取得しcsvに出力
WindowsDiskUsageToCsv()
#Windowsのネットワークトラフィックを取得しcsvに出力
WindowsNetworkResourceUsageToCsv()

#LinuxOSのメモリ使用率を取得しcsvに出力
NodeMemoryUsageToCsv()
#LinuxOSのCPU使用率を取得しcsvに出力
NodeCpuUsageToCsv()
#LinuxOSのDisk使用率を取得しcsvに出力
NodeDiskUsageToCsv()
#LinuxOSのネットワークトラフィックを取得しcsvに出力
NodeNetworkResourceUsageToCsv()

#(オプション)取得するメトリックスの情報を記載
#ファイル存在チェック
fileChk = os.path.exists(METRICSLISTPATH) > 0
if not fileChk:
    print("Optional function is not addapted.")
    sys.exit(0)

#オプションで調査するメトリックスを取得
metricsFile = open('c:\\Prometheus\\metricsList.txt',"r")
metricsList = metricsFile.read()
metricsFile.close()

#ファイルは存在するけどリストが空の場合
if not metricsList:
    print("Optional resource list is empty.")
    sys.exit(0)

#lines1 = metricsList.split('\n') # 改行で区切る(改行文字そのものは戻り値のデータには含まれない)
#1行ごとにリストを読み込む(最後の改行は読み込まない)
metricsListNames = metricsList.splitlines()
count = 0

#変数の初期化
metricsName = ''

#以下オプションのメトリックス取得用スクリプト
#データの取得スクリプト
for metricsName in metricsListNames:
 #オプション用の管理番号
 count = count + 1
 for metrixName in metrixNames:
    #取得したいメトリックスではない場合、次のメトリックスへ移動
     if metrixName != metricsName:
       continue
 
     #期間指定のqueryの結果をPrometheuから取得(APIの詳細はhttps://prometheus.io/docs/prometheus/latest/querying/api/#range-vectors)
     response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
      params={'query': metrixName, 'start': span, 'end': today_ts, 'step': RESOLUTION})

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
          with open(RESULT_FILE, "a", newline="") as csvFile:
            writer = csv.writer(csvFile)
          #どのオプションの値を追加したか記載
            writeOptionInfo = "Option" + str(count) + "-" + metricName
          #タイムスタンプと値を記載
            for metricValue in metricValues:
             timeDate = datetime.fromtimestamp(metricValue[0])
             writer.writerow([metricInst,writeOptionInfo,timeDate, metricValue[1]])

 