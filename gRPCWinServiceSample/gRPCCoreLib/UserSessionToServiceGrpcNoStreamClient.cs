using Grpc.Net.Client.Configuration;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceToUserSession;

namespace gRPCCoreLib
{
    public class UserSessionToServiceGrpcNoStreamClient
    {


        private GrpcChannel CreateChannel()
        {
            return GrpcChannel.ForAddress("http://localhost:50100/Connect1NoStream");
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
