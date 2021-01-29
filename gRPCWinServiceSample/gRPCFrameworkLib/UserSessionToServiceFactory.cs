using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gRPCFrameworkLib
{
    public static class UserSessionToServiceFactory
    {
        public static IUserSessionToService CreateInstance()
        {
            return new UserSessionToServiceGRpc();
        }

    }
}
