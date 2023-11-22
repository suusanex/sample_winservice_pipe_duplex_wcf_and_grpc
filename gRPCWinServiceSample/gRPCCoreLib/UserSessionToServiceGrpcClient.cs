﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using NLog;
using ServiceToUserSession;

namespace gRPCCoreLib
{
    public enum GrpcConnectType
    {
        TCP,
        Pipe,
    }

    public class UserSessionToServiceGrpcClient
    {
        private Logger log = LogManager.GetCurrentClassLogger();

        private GrpcChannel m_Channel;
        private AsyncDuplexStreamingCall<UserSessionToServiceRequest, ServiceToUserSessionResponse> m_DuplexStream;
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
                    log.Warn($"{nameof(m_DuplexStream)} Complete Exception, {e}");
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
        public event Action<long> OnHighFrequencyResponse;

        public void Subscribe(GrpcConnectType connectType)
        {

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            if (connectType == GrpcConnectType.Pipe)
            {
                var connectionFactory = new NamedPipesConnectionFactory("gRPCWinServiceSamplePipeName");
                var socketsHttpHandler = new SocketsHttpHandler
                {
                    ConnectCallback = connectionFactory.ConnectAsync
                };

                m_Channel = GrpcChannel.ForAddress("http://localhost/Connect1", new GrpcChannelOptions
                {
                    HttpHandler = socketsHttpHandler
                });
            }
            else
            {
                m_Channel = GrpcChannel.ForAddress("http://localhost:50100/Connect1");
            }

            m_ResponseWaitCancel = new CancellationTokenSource();
            var client = new WindowsServiceToUserSessionGrpcService.WindowsServiceToUserSessionGrpcServiceClient(m_Channel);
            m_DuplexStream = client.Subscribe(cancellationToken: m_ResponseWaitCancel.Token);

            log.Debug($"Subscribe End, Stream={m_DuplexStream},{m_DuplexStream.GetHashCode()}");

            //TODO:一定時間での再接続タイマー
            //channelReconnectTimer = new Timer(ChannelAndSessionReconnect, null, ReconnectTimeSpan, new TimeSpan(0, 0, 0, 0, -1));

            Task.Run(async () =>
            {

                var stream = m_DuplexStream.ResponseStream;

                await foreach (var command in stream.ReadAllAsync(m_ResponseWaitCancel.Token))
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
                        case ServiceToUserSessionResponse.ActionOneofCase.HighFrequencyResponse:
                            OnHighFrequencyResponse?.Invoke(command.HighFrequencyResponse.MsgFileTime);
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

        public async Task HighFrequencyResponseTestStartAsync(TimeSpan interval)
        {

            await m_DuplexStream.RequestStream.WriteAsync(new UserSessionToServiceRequest
            {
                HighFrequencyResponseTestStart = new ()
                {
                    IntervalMs = (int)interval.TotalMilliseconds,
                },
            });

        }

        public async Task HighFrequencyResponseTestEndAsync()
        {

            await m_DuplexStream.RequestStream.WriteAsync(new UserSessionToServiceRequest
            {
                HighFrequencyResponseTestEnd = new Empty(),
            });

        }

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

    }
}
