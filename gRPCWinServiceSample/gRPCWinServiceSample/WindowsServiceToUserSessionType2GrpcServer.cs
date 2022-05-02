using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NLog;
using ServiceToUserSessionType2;

namespace gRPCWinServiceSample
{
    internal class WindowsServiceToUserSessionType2GrpcServer : WindowsServiceToUserSessionType2GrpcService.WindowsServiceToUserSessionType2GrpcServiceBase
    {
        class SubscribeData
        {
            public IAsyncStreamReader<UserSessionToServiceType2Request> RequestStream;
            public IServerStreamWriter<ServiceToUserSessionType2Response> ResponseStream;
            public ServerCallContext Context;
        }

        private ConcurrentDictionary<int, SubscribeData> m_UserSessions = new ConcurrentDictionary<int, SubscribeData>();

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override async Task Subscribe(IAsyncStreamReader<UserSessionToServiceType2Request> requestStream, IServerStreamWriter<ServiceToUserSessionType2Response> responseStream, ServerCallContext context)
        {
            logger.Info($"Start, ServerCallContext={context},{context.GetHashCode()}");
            try
            {
                await RequestWaitAsync(new SubscribeData
                {
                    ResponseStream = responseStream,
                    Context = context,
                    RequestStream = requestStream
                }, context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.Info($"Connection Canceled, {context},{context.GetHashCode()}");
            }
            catch (Exception e)
            {
                logger.Warn($"{e}");
            }

            logger.Info($"Connection End, {context},{context.GetHashCode()}");
        }

        private Timer ResponseTimer;

        private int m_CountForTestException;
        private bool m_IsEnableTestException;// = true;

        async Task RequestWaitAsync(SubscribeData subscribe, CancellationToken cancellationToken)
        {
            logger.Info($"RequestWaitAsync Start");

            try
            {

                await foreach (var req in subscribe.RequestStream.ReadAllAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    logger.Info($"Case {req.ActionCase}");
                    switch (req.ActionCase)
                    {
                        case UserSessionToServiceType2Request.ActionOneofCase.None:
                            break;
                        case UserSessionToServiceType2Request.ActionOneofCase.RegisterUserSession:
                            {
                                var val = req.RegisterUserSession;
                                logger.Info($"RegisterUserSession Call, {val.SessionId}");
                                m_UserSessions.TryAdd(val.SessionId, subscribe);

                                ResponseTimer = new Timer(_ => subscribe.ResponseStream.WriteAsync(
                                        new ServiceToUserSessionType2Response
                                        {
                                            SendDataRequest = new SendDataRequestParam()
                                            {
                                                Data2 = @"datastr2"
                                            }
                                        }), null,
                                    new TimeSpan(0, 0, 1),
                                    new TimeSpan(0, 0, 5));

                            }
                            break;
                        case UserSessionToServiceType2Request.ActionOneofCase.GetDataRequest:
                            {
                                var val = req.GetDataRequest;
                                logger.Info($"GetDataRequest Call, {val.Number2}");
                                m_CountForTestException++;
                                if (m_IsEnableTestException && 2 < m_CountForTestException)
                                {
                                    throw new Exception("over");
                                }

                                await subscribe.ResponseStream.WriteAsync(
                                    new ServiceToUserSessionType2Response
                                    {
                                        GetDataResponse = new GetDataResponseParam
                                        {
                                            Data2 = "ResData2"
                                        }
                                    });
                            }
                            break;
                        case UserSessionToServiceType2Request.ActionOneofCase.SendDataResponse:
                            {
                                var val = req.SendDataResponse;
                                logger.Info($"SendDataResponse Call, {val.Result2}");
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            finally
            {
                ResponseTimer?.Dispose();
            }
        }

    }
}
