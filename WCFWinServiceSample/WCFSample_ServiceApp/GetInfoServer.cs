using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WCFIPCSample_Lib
{


    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class GetInfoServer : IGetInfo
    {
        public string GetData(int value)
        {
            Task.Delay(1000).ContinueWith(task => SendData($"Callback, {DateTime.Now}"));
            return $"You entered: {value}, {DateTime.Now}";
        }


        static Logger log = LogManager.GetCurrentClassLogger();

        private static ConcurrentDictionary<string, OperationContext> _SessionId_Operation_Dic =
            new ConcurrentDictionary<string, OperationContext>();

        void SendData(string value)
        {
            Callback.SendData(value);
        }
        public static void SendDataAllSession(string value)
        {

            foreach (var session in _SessionId_Operation_Dic)
            {
                try
                {
                    session.Value.GetCallbackChannel<IGetInfoCallback>().SendData($"{session.Key}, {value}");

                }
                catch (Exception ex)
                {
                    log.Trace($"{session.Key}, {ex}");
                    _SessionId_Operation_Dic.TryRemove(session.Key, out OperationContext temp);
                }
            }

        }

        public void SessionDisconnect()
        {
            log.Trace($"{nameof(SessionDisconnect)}, {OperationContext.Current.SessionId}");
            _SessionId_Operation_Dic.TryRemove(OperationContext.Current.SessionId, out OperationContext temp);
        }

        public void SessionConnect()
        {
            _SessionId_Operation_Dic[OperationContext.Current.SessionId] = OperationContext.Current;
            log.Trace($"{nameof(SessionConnect)}, {OperationContext.Current.SessionId}");
        }

        private IGetInfoCallback Callback { get; } = OperationContext.Current.GetCallbackChannel<IGetInfoCallback>();
    }
}
