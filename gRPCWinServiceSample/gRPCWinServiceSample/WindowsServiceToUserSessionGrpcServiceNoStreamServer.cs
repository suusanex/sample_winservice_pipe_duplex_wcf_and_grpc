using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using NLog;
using ServiceToUserSession;

namespace gRPCWinServiceSample
{
    public class WindowsServiceToUserSessionGrpcServiceNoStreamServer : WindowsServiceToUserSessionGrpcServiceNoStream.WindowsServiceToUserSessionGrpcServiceNoStreamBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override async Task<ServiceToUserSessionResponseNoStream> Subscribe(UserSessionToServiceRequestNoStream request, ServerCallContext context)
        {
            logger.Info($"Start, ServerCallContext={context},{context.GetHashCode()}, this={this.GetHashCode()}");


            try
            {
                return await RequestWaitAsync(request, context, context.CancellationToken);
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

            return await base.Subscribe(request, context);
        }

        private async Task<ServiceToUserSessionResponseNoStream> RequestWaitAsync(UserSessionToServiceRequestNoStream req, ServerCallContext context, CancellationToken cancellationToken)
        {
            logger.Info($"RequestWaitAsync Start");

            logger.Info($"Case {req.ActionCase}");

            switch (req.ActionCase)
            {
                case UserSessionToServiceRequestNoStream.ActionOneofCase.GetDataRequest:
                {
                    var val = req.GetDataRequest;
                        logger.Info($"GetDataRequest Call, {val.Number}");
                        //レスポンスに現実的な負荷を与えるために、適当な100文字の文字列を追加
                        var res = new ServiceToUserSessionResponseNoStream
                        {
                            GetDataResponse = new GetDataResponseParam
                            {
                                Data = m_ResponseBufStr
                            }
                        };
                        return res;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


        }

        private readonly string m_ResponseBufStr = string.Join("", Enumerable.Repeat("0123456789", 10));

    }
}
