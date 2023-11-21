using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NLog;
using ServiceToUserSession;

namespace gRPCWinServiceSample
{
    public class WindowsServiceToUserSessionGrpcServer : WindowsServiceToUserSessionGrpcService.WindowsServiceToUserSessionGrpcServiceBase
    {
        class SubscribeData
        {
            public IAsyncStreamReader<UserSessionToServiceRequest> RequestStream;
            public IServerStreamWriter<ServiceToUserSessionResponse> ResponseStream;
            public ServerCallContext Context;
        }

        private ConcurrentDictionary<int, SubscribeData> m_UserSessions = new ConcurrentDictionary<int, SubscribeData>();

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override async Task Subscribe(IAsyncStreamReader<UserSessionToServiceRequest> requestStream, IServerStreamWriter<ServiceToUserSessionResponse> responseStream, ServerCallContext context)
        {
            logger.Info($"Start, ServerCallContext={context},{context.GetHashCode()}, this={this.GetHashCode()}");
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

        private Timer? ResponseTimer;

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
                        case UserSessionToServiceRequest.ActionOneofCase.None:
                            break;
                        case UserSessionToServiceRequest.ActionOneofCase.RegisterUserSession:
                            {
                                var val = req.RegisterUserSession;
                                logger.Info($"RegisterUserSession Call, {val.SessionId}");
                                m_UserSessions.TryAdd(val.SessionId, subscribe);

                                ResponseTimer = new Timer(_ => subscribe.ResponseStream.WriteAsync(
                                        new ServiceToUserSessionResponse
                                        {
                                            SendDataRequest = new SendDataRequestParam()
                                            {
                                                Data = @"datastr"
                                            }
                                        }), null,
                                    new TimeSpan(0, 0, 1),
                                    new TimeSpan(0, 0, 5));

                            }
                            break;
                        case UserSessionToServiceRequest.ActionOneofCase.GetDataRequest:
                            {
                                var val = req.GetDataRequest;
                                logger.Info($"GetDataRequest Call, {val.Number}");
                                m_CountForTestException++;
                                if (m_IsEnableTestException && 2 < m_CountForTestException)
                                {
                                    throw new Exception("over");
                                }

                                await subscribe.ResponseStream.WriteAsync(
                                    new ServiceToUserSessionResponse
                                    {
                                        GetDataResponse = new GetDataResponseParam
                                        {
                                            Data = "ResData"
                                        }
                                    });
                            }
                            break;
                        case UserSessionToServiceRequest.ActionOneofCase.SendDataResponse:
                            {
                                var val = req.SendDataResponse;
                                logger.Info($"SendDataResponse Call, {val.Result}");
                            }
                            break;
                        case UserSessionToServiceRequest.ActionOneofCase.HighFrequencyResponseTestStart:
                            if (ResponseTimer != null)
                            {
                                await ResponseTimer.DisposeAsync();
                                ResponseTimer = null;
                            }
                            m_HighFrequencyResponseTestCancel?.Dispose();
                            m_HighFrequencyResponseTestCancel = new();
                            var cancelToken = m_HighFrequencyResponseTestCancel.Token;
                            var interval = TimeSpan.FromMilliseconds(req.HighFrequencyResponseTestStart.IntervalMs);
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    //レスポンスに現実的な負荷を与えるために、適当な100文字の文字列を追加
                                    while (!cancelToken.IsCancellationRequested)
                                    {
                                        await subscribe.ResponseStream.WriteAsync(
                                            new ServiceToUserSessionResponse
                                            {
                                                HighFrequencyResponse = new ()
                                                {
                                                    MsgFileTime = DateTime.Now.ToFileTimeUtc(),
                                                    DataBuf = m_ResponseBufStr,
                                                }
                                            }, cancelToken);

                                        await Task.Delay(interval, cancelToken);
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    logger.Info($"HighFrequencyResponseTest Canceled");
                                }
                                catch (Exception e)
                                {
                                    logger.Warn($"{e}");
                                }
                            }, cancelToken);

                            break;
                        case UserSessionToServiceRequest.ActionOneofCase.HighFrequencyResponseTestEnd:
                            if (m_HighFrequencyResponseTestCancel != null)
                            {
                                await m_HighFrequencyResponseTestCancel.CancelAsync();
                                m_HighFrequencyResponseTestCancel = null;
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

        private CancellationTokenSource? m_HighFrequencyResponseTestCancel;

        private readonly string m_ResponseBufStr = string.Join("", Enumerable.Repeat("0123456789", 10));
    }
}
