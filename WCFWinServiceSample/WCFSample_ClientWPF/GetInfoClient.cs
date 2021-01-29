using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using WCFIPCSample_Lib;

namespace WCFSample_ClientWPF
{
    public class ServiceToUserSessionClient : IServiceToUserSessionCallback
    {
        private Logger log = LogManager.GetCurrentClassLogger();
        public bool SendData(string value)
        {
            if (OnSendDataRequest != null)
            {
                return OnSendDataRequest(value);
            }
            else
            {
                return false;
            }
        }

        public event Func<string, bool> OnSendDataRequest;

    }
}
