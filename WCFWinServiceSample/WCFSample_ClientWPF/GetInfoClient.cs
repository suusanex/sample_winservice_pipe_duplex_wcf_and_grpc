using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using NLog;
using WCFIPCSample_Lib;

namespace WCFSample_ClientWPF
{
    public class ServiceToUserSessionClient : IServiceToUserSessionCallback
    {
        private int count;
        private Logger log = LogManager.GetCurrentClassLogger();
        public bool SendData(string value)
        {
            log.Info($"Start, {value}");
            count++;
            if (count % 4 == 0)
            {
                log.Info($"throw Exception, {value}");
                throw new Exception($"{count}");
            }

            Thread.Sleep(TimeSpan.FromSeconds(2));

            log.Info($"End, {value}");
            if (OnSendDataRequest != null)
            {
                return OnSendDataRequest(value);
            }
            else
            {
                return false;
            }
        }

        public bool SendData2(string value)
        {
            return SendData(value);
        }

        public event Func<string, bool> OnSendDataRequest;

    }
}
