# sample_wcf_winservice_pipe_duplex
WCFでWindowsサービスとパイプ通信（双方向）するサンプルコード。

## 構成

WCFSample_ServiceApp：Windowsサービス

WCFSample_ClientWPF：クライアントアプリケーション

## 使い方

1. WCFSample_ServiceAppを、installutilなどを使ってWindowsサービスとしてインストール
1. WCFSampleHostServiceというサービスを手動で開始
1. WCFSample_ClientWPFアプリを起動
1. Openボタンを押して接続を開き、SessionConnectボタンを押してセッションを開始。GetDataボタンを押してクライアントからサービスへ通信をする。1秒後に、サービスからクライアントへの通信が別スレッドで発生する。
1. サーバー側は、存在するセッション全てに対して5秒に1回SendDataの呼び出しを行う。
1. 終了する時は、SessionDisconnectボタンを押してセッションを終了し、Closeボタンを押して接続を終了。

## ログ

Windowsサービス・クライアントアプリともに、exeファイルと同じフォルダへテキストでログを出力する。通信結果も出力している。エラー時の解析や、結果の確認に使うことができる。



