using System;
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


        void SendData(string value)
        {
            Callback.SendData(value);
        }

        private IGetInfoCallback Callback { get; } = OperationContext.Current.GetCallbackChannel<IGetInfoCallback>();
    }
}
