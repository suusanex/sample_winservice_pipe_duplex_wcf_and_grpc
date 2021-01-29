# sample_winservice_pipe_duplex_wcf_and_grpc

WCFもしくはgRPCで、Windowsサービスとパイプ通信（双方向）するサンプルコード。

WCFとgRPCを似たような構成で作成してあるため、WCF->gRPCの移植の資料としても使える。

## 構成

### WCFWinServiceSample

WCFでパイプ通信（双方向）を行う。

 * WCFSample_ServiceApp
   * WCFのサーバー（Windowsサービス）
 * WCFSample_ClientWPF
   * WCFのクライアント
 * WCFIPCSample_Lib
   * 通信定義のinterfaceを持つ共有クラスライブラリ

### gRPCWinServiceSample

gRPCでパイプ通信（双方向）を行う。

 * gRPCWinServiceSample
    * gRPCのサーバー（Windowsサービス）
 * gRPCCoreClient
   * gRPCのクライアント（.NET Core版）
 * gRPCCoreLib
   * gRPCのクライアント側の通信定義を持つクラスライブラリ（.NET Core版）
 * gRPCFrameworkClient
   * gRPCのクライアント（.NET Framework版）
 * gRPCFrameworkLib
   * gRPCのクライアント側の通信定義を持つクラスライブラリ（.NET Framework版）


## 使い方

### WCFWinServiceSample

1. WCFSample_ServiceAppを、installutilなどを使ってWindowsサービスとしてインストール
1. WCFSampleHostServiceというサービスを手動で開始
1. WCFSample_ClientWPFアプリを起動
1. Openボタンを押して接続を開き、SessionConnectボタンを押してセッションを開始。GetDataボタンを押してクライアントからサービスへ通信をする。1秒後に、サービスからクライアントへの通信が別スレッドで発生する。
1. サーバー側は、存在するセッション全てに対して5秒に1回SendDataの呼び出しを行う。
1. 終了する時は、SessionDisconnectボタンを押してセッションを終了し、Closeボタンを押して接続を終了。


### gRPCWinServiceSample

1. gRPCWinServiceSampleを実行（Windowsサービスとしてインストールしても良い）
1. gRPCCoreClientもしくはgRPCFrameworkClientを起動
1. Openボタンを押して接続を開き、SessionConnectボタンを押してセッションを開始。GetDataボタンを押してクライアントからサービスへ通信をする。1秒後に、サービスからクライアントへの通信が別スレッドで発生する。
1. サーバー側は、存在するセッション全てに対して5秒に1回SendDataの呼び出しを行う。
1. 終了する時は、SessionDisconnectボタンを押してセッションを終了し、Closeボタンを押して接続を終了。

## ログ

### WCFWinServiceSample

主要な動作ログは、画面上に出力する。

その他の詳細なログを、exeファイルと同じフォルダへテキストで出力する。通信結果も出力する。エラー時の解析や、結果の確認に使うことができる。NLogで実現していて、NLogの設定はapp.config内に持つ。


### gRPCWinServiceSample

主要な動作ログは、画面上に出力する。

その他の詳細なログはNLogで出力する。NLog.configで設定可能（デフォルトでは出力無し）。



