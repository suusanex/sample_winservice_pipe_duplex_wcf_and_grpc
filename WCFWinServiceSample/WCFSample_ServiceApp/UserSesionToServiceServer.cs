using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WCFSample_ServiceApp;

namespace WCFIPCSample_Lib
{


    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class UserSesionToServiceServer : IUserSessionToService
    {
        public string GetData(int value)
        {
            Task.Delay(1000).ContinueWith(task =>
            {
                try
                {
                    var task1 = Task.Run(() =>
                    {
                        log.Info("SendData1 Start");
                        SendDataAllSession($"Callback1, {DateTime.Now}");
                        log.Info("SendData1 End");
                    });
                    var task2 = Task.Run(() =>
                    {
                        log.Info("SendData2 Start");
                        SendData2AllSession($"Callback2, {DateTime.Now}");
                        log.Info("SendData2 End");
                    });
                    var task3 = Task.Run(() =>
                    {
                        log.Info("SendData3 Start");
                        SendDataAllSession($"Callback3, {DateTime.Now}");
                        log.Info("SendData3 End");
                    });

                    Task.WaitAll(task1, task2, task3);
                }
                catch (Exception e)
                {
                    log.Warn($"SendData, {e}");

                    Thread.Sleep(new TimeSpan(0,0,1));
                    SendData($"Callback, {DateTime.Now}");
                }
            });
            return $"You entered: {value}, {DateTime.Now}";
        }


        static Logger log = LogManager.GetCurrentClassLogger();

        private static ConcurrentDictionary<string, OperationContext> _SessionId_Operation_Dic =
            new ConcurrentDictionary<string, OperationContext>();

        void SendData(string value)
        {
            try
            {
                Callback.SendData(value);
            }
            catch (Exception e)
            {
                log.Warn($"SendData WCF, {e}");
            }
        }
        public static void SendDataAllSession(string value)
        {

            foreach (var session in _SessionId_Operation_Dic)
            {
                try
                {
                    session.Value.GetCallbackChannel<IServiceToUserSessionCallback>().SendData($"{session.Key}, {value}");

                }
                catch (Exception ex)
                {
                    log.Trace($"{session.Key}, {ex}");
                    _SessionId_Operation_Dic.TryRemove(session.Key, out OperationContext temp);
                }
            }

        }
        public static void SendData2AllSession(string value)
        {

            foreach (var session in _SessionId_Operation_Dic)
            {
                try
                {
                    session.Value.GetCallbackChannel<IServiceToUserSessionCallback>().SendData2($"{session.Key}, {value}");

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

        private IServiceToUserSessionCallback Callback => OperationContext.Current.GetCallbackChannel<IServiceToUserSessionCallback>();
    }
}
