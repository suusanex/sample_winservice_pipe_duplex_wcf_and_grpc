using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using WCFIPCSample_Lib;

namespace WCFSample_ClientWPF
{
    public class GetInfoClient : IGetInfoCallback
    {
        private Logger log = LogManager.GetCurrentClassLogger();
        public void SendData(string value)
        {
            log.Trace($"{nameof(SendData)}, {value}");
        }
    }
}
