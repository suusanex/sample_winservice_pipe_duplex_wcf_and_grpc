using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;
using NLog;
using ServiceToUserSession;

namespace gRPCFrameworkLib
{
    class UserSessionToServiceGRpc : IUserSessionToService
    {

        private static Logger log = LogManager.GetCurrentClassLogger();

        private Channel m_Channel;
        private AsyncDuplexStreamingCall<UserSessionToServiceRequest, ServiceToUserSessionResponse> m_DuplexStream;
        private CancellationTokenSource m_ResponseWaitCancel;

        Timer channelReconnectTimer;


        public void Subscribe()
        {

            m_Channel = new Channel("localhost:51232", ChannelCredentials.Insecure);
            m_ResponseWaitCancel = new CancellationTokenSource();
            var client = new WindowsServiceToUserSessionGrpcService.WindowsServiceToUserSessionGrpcServiceClient(m_Channel);
            m_DuplexStream = client.Subscribe(cancellationToken: m_ResponseWaitCancel.Token);

            log.Info($"Subscribe End, Stream={m_DuplexStream},{m_DuplexStream.GetHashCode()}");


            //TODO:一定時間での再接続タイマー
            //channelReconnectTimer = new Timer(ChannelAndSessionReconnect, null, ReconnectTimeSpan, new TimeSpan(0, 0, 0, 0, -1));


            var stream = m_DuplexStream.ResponseStream;
            _ = stream.ForEachAsync(command =>
            {
                return Task.Run(async () =>
                {
                    log.Debug($"Read, {command.ActionCase}");

                    m_CountForTestException++;
                    if (m_IsEnableTestException && 2 < m_CountForTestException)
                    {
                        throw new Exception("over");
                    }

                    switch (command.ActionCase)
                    {
                        case ServiceToUserSessionResponse.ActionOneofCase.None:
                            break;
                        case ServiceToUserSessionResponse.ActionOneofCase.GetDataResponse:
                        {
                            var val = command.GetDataResponse;
                            OnGetDataResponse?.Invoke(val.Data);
                        }
                            break;
                        case ServiceToUserSessionResponse.ActionOneofCase.SendDataRequest:
                        {
                            var val = command.SendDataRequest;
                            var ret = OnSendDataRequest?.Invoke(val.Data);
                            await m_DuplexStream.RequestStream.WriteAsync(new UserSessionToServiceRequest
                            {
                                SendDataResponse = new SendDataResponseParam
                                {
                                    Result = ret ?? false,
                                }
                            });
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });

            });


        }

        public async Task SessionConnectAsync()
        {
            await m_DuplexStream.RequestStream.WriteAsync(new UserSessionToServiceRequest
            {
                RegisterUserSession = new RegisterUserSessionRequest
                {
                    SessionId = Process.GetCurrentProcess().SessionId
                }
            });
        }



        public event Action<string> OnGetDataResponse;
        public event Func<string, bool> OnSendDataRequest;

        private int m_CountForTestException;
        private bool m_IsEnableTestException;// = true;


        public async Task<string> GetDataRequestAsync()
        {
            var task = new TaskCompletionSource<string>();

            await m_DuplexStream.RequestStream.WriteAsync(new UserSessionToServiceRequest
            {
                GetDataRequest = new GetDataRequestParam
                {
                    Number = 2
                }
            });

            void GetDataResponseFunc(string result)
            {
                OnGetDataResponse -= GetDataResponseFunc;
                task.SetResult(result);
            }

            OnGetDataResponse += GetDataResponseFunc;


            log.Debug("GetDataRequest End");

            return await task.Task;
        }

        private TimeSpan ReconnectTimeSpan = new TimeSpan(0, 9, 0);//サーバー側のReceiveTimeoutより短くする必要がある


        void ChannelAndSessionReconnect(object NullObj)
        {
            //log.Debug($"{nameof(ChannelAndSessionReconnect)}");
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
            //    log.Debug($"{e}");
            //}
        }


        readonly TimeSpan m_DisposeTimeout = new TimeSpan(0, 0, 10);

        public void Dispose()
        {
            channelReconnectTimer?.Dispose();
            channelReconnectTimer = null;
            m_ResponseWaitCancel?.Cancel();
            m_ResponseWaitCancel = null;

            if (m_DuplexStream != null)
            {
                try
                {
                    if (!m_DuplexStream.RequestStream.CompleteAsync().Wait(m_DisposeTimeout))
                    {
                        log.Warn($"Dispose CompleteAsync Timeout");
                    }
                }
                catch (Exception e)
                {
                    //Completeの例外発生はすでにComplete不可能な状態であることを示しているので、そのままDisposeへ進む
                    log.Warn($"{nameof(m_DuplexStream)} Complete Exception, {e}");
                }
                finally
                {
                    m_DuplexStream.Dispose();
                    m_DuplexStream = null;
                }
            }

            if (m_Channel != null)
            {
                if (!m_Channel.ShutdownAsync().Wait(m_DisposeTimeout))
                {
                    log.Warn($"Dispose ShutdownAsync Timeout");
                }

                m_Channel = null;
            }

        }
    }
}
