using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using NLog;
using ServiceToUserSessionType2;

namespace gRPCCoreLib
{
    public class UserSessionToServiceType2GrpcClient
    {
        private Logger log = LogManager.GetCurrentClassLogger();

        private GrpcChannel m_Channel;
        private AsyncDuplexStreamingCall<UserSessionToServiceType2Request, ServiceToUserSessionType2Response> m_DuplexStream;
        private CancellationTokenSource m_ResponseWaitCancel;

        Timer channelReconnectTimer;

        public async ValueTask DisposeAsync()
        {
            channelReconnectTimer?.DisposeAsync().ConfigureAwait(false);
            channelReconnectTimer = null;

            if (m_DuplexStream != null)
            {
                try
                {
                    await m_DuplexStream.RequestStream.CompleteAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    //Completeの例外発生はすでにComplete不可能な状態であることを示しているので、そのままDisposeへ進む
                    log.Trace($"{nameof(m_DuplexStream)} Complete Exception, {e}");
                }
                finally
                {
                    m_DuplexStream.Dispose();
                    m_DuplexStream = null;
                }
            }

            m_Channel?.Dispose();
            m_Channel = null;

        }

        public event Action<string> OnGetDataResponse;

        public void Subscribe()
        {

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            m_Channel = GrpcChannel.ForAddress("http://localhost:50100/Connect2");

            m_ResponseWaitCancel = new CancellationTokenSource();
            var client = new WindowsServiceToUserSessionType2GrpcService.WindowsServiceToUserSessionType2GrpcServiceClient(m_Channel);
            m_DuplexStream = client.Subscribe(cancellationToken: m_ResponseWaitCancel.Token);

            log.Trace($"Subscribe End, Stream={m_DuplexStream},{m_DuplexStream.GetHashCode()}");

            //TODO:一定時間での再接続タイマー
            //channelReconnectTimer = new Timer(ChannelAndSessionReconnect, null, ReconnectTimeSpan, new TimeSpan(0, 0, 0, 0, -1));

            Task.Run(async () =>
            {

                var stream = m_DuplexStream.ResponseStream;

                await foreach (var command in stream.ReadAllAsync(m_ResponseWaitCancel.Token))
                {
                    log.Trace($"Read, {command.ActionCase}");

                    m_CountForTestException++;
                    if (m_IsEnableTestException && 2 < m_CountForTestException)
                    {
                        throw new Exception("over");
                    }

                    switch (command.ActionCase)
                    {
                        case ServiceToUserSessionType2Response.ActionOneofCase.None:
                            break;
                        case ServiceToUserSessionType2Response.ActionOneofCase.GetDataResponse:
                        {
                            var val = command.GetDataResponse;
                            OnGetDataResponse?.Invoke(val.Data2);
                        }
                            break;
                        case ServiceToUserSessionType2Response.ActionOneofCase.SendDataRequest:
                        {
                            var val = command.SendDataRequest;
                            var ret = OnSendDataRequest?.Invoke(val.Data2);
                            await m_DuplexStream.RequestStream.WriteAsync(new UserSessionToServiceType2Request
                            {
                                SendDataResponse = new SendDataResponseParam
                                {
                                    Result2 = ret ?? false,
                                }
                            });
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });

        }

        public event Func<string, bool> OnSendDataRequest;

        private int m_CountForTestException;
        private bool m_IsEnableTestException;// = true;


        public async Task<string> GetDataRequestAsync()
        {
            var task = new TaskCompletionSource<string>();

            await m_DuplexStream.RequestStream.WriteAsync(new UserSessionToServiceType2Request
            {
                GetDataRequest = new GetDataRequestParam
                {
                    Number2 = 2
                }
            });

            void GetDataResponseFunc(string result)
            {
                OnGetDataResponse -= GetDataResponseFunc;
                task.SetResult(result);
            }

            OnGetDataResponse += GetDataResponseFunc;


            log.Trace("GetDataRequest End");

            return await task.Task;
        }


        private TimeSpan ReconnectTimeSpan = new TimeSpan(0, 9, 0);//サーバー側のReceiveTimeoutより短くする必要がある

        public async Task SessionConnectAsync()
        {

            await m_DuplexStream.RequestStream.WriteAsync(new UserSessionToServiceType2Request
            {
                RegisterUserSession = new RegisterUserSessionRequest
                {
                    SessionId = Process.GetCurrentProcess().SessionId
                }
            });

        }

        void ChannelAndSessionReconnect(object NullObj)
        {
            //log.Trace($"{nameof(ChannelAndSessionReconnect)}");
            //try
            //{

            //    try
            //    {
            //        channelFactory.Close();
            //    }
            //    catch
            //    {
            //        channelFactory.Abort();
            //    }
            //    channelFactory = null;


            //    CreateFactory();

            //    channelFactory.Open();
            //    channelReconnectTimer.Change(ReconnectTimeSpan, new TimeSpan(0, 0, 0, 0, -1));

            //    channel = channelFactory.CreateChannel();

            //    channel.SessionConnect();

            //}
            //catch (Exception e)
            //{
            //    log.Trace($"{e}");
            //}
        }

    }
}
