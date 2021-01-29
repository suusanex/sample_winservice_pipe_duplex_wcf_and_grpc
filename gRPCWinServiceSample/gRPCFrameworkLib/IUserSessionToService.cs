using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gRPCFrameworkLib
{
    public interface IUserSessionToService : IDisposable
    {
        void Subscribe();

        Task SessionConnectAsync();

        Task<string> GetDataRequestAsync();

        event Action<string> OnGetDataResponse;
        event Func<string, bool> OnSendDataRequest;

    }

}
