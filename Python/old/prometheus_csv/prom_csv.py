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
def GetMetrixNames(url):
    response = requests.get('{0}/api/v1/label/__name__/values'.format(url))
    names = response.json()['data']
    #Return metrix names
    return names

PROMETHEUS_URL = ''
RANGE_QUERY_API = '/api/v1/query_range'
RESOLUTION = '3600s'

"""
Prometheus hourly data as csv.
"""

#現在日付取得
today=datetime.now()
today_ts=today.timestamp()
#10日前取得
tenDaysBefore = today - timedelta(days=10)
tenDaysBefore_ts = tenDaysBefore.timestamp()
#1か月前取得
month_ago = today - relativedelta(months=1)
month_ago_ts = month_ago.timestamp()

#csvファイルへの書き込みオブジェクト作成
writer = csv.writer(sys.stdout)

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
     #now its hardcoded for hourly
     #response = requests.get('{0}/api/v1/query'.format(sys.argv[1]),
     #params={'query': metrixName})
     response = requests.get(PROMETHEUS_URL + RANGE_QUERY_API,
      params={'query': metrixName, 'start': tenDaysBefore_ts, 'end': today_ts, 'step': RESOLUTION})

     #応答が返ってきているか確認
     status = response.json()['status']
     if status == "error":
        logging.error(response.json())
        sys.exit(2)

     results = response.json()['data']['result']
      # Build a list of all labelnames used.
      #gets all keys and discard __name__
     labelnames = set()
     for result in results:
          labelnames.update(result['metric'].keys())
      # Canonicalize
     labelnames.discard('__name__')
     labelnames = sorted(labelnames)
      # Write the samples.
     if writeHeader:
          writer.writerow(['name', 'timestamp', 'value'] + labelnames)
          writeHeader=False
     for result in results:
          l = [result['metric'].get('__name__', '')] + result['values']
          metricName = [result['metric'].get('__name__'), '']
          metricValue = result['values']
          #writer.writerow(metricName)
          #writer.writerow(metricValue)
          for label in labelnames:
              l.append(result['metric'].get(label, ''))
              #writer.writerow((u"l").encode('utf-8'))
              writer.writerow(l)