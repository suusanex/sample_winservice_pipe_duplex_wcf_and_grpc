using Grpc.Net.Client.Configuration;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceToUserSession;
using System.Net.Http;
using System.Threading.Channels;

namespace gRPCCoreLib
{
    public class UserSessionToServiceGrpcNoStreamClient(GrpcConnectType m_ConnectType)
    {
        private GrpcChannel CreateChannel()
        {
            if (m_ConnectType == GrpcConnectType.Pipe)
            {
                var connectionFactory = new NamedPipesConnectionFactory("gRPCWinServiceSamplePipeName");
                var socketsHttpHandler = new SocketsHttpHandler
                {
                    ConnectCallback = connectionFactory.ConnectAsync
                };

                return GrpcChannel.ForAddress("http://localhost/Connect1NoStream", new GrpcChannelOptions
                {
                    HttpHandler = socketsHttpHandler
                });
            }
            else
            {
                return GrpcChannel.ForAddress("http://localhost:50100/Connect1NoStream");
            }
        }

        public async Task<GetDataResponseParam> GetDataRequestAsync(int number)
        {

            using var channel = CreateChannel();
            var client = new WindowsServiceToUserSessionGrpcServiceNoStream.WindowsServiceToUserSessionGrpcServiceNoStreamClient(channel);

            return (await client.SubscribeAsync(new()
            {
                GetDataRequest = new()
                {
                    Number = number,
                }
            })).GetDataResponse;
        }

    }
}
