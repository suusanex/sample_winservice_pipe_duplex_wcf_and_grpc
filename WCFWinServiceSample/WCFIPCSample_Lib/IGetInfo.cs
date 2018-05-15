using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WCFIPCSample_Lib
{

    [ServiceContract(SessionMode = SessionMode.Required,
        CallbackContract = typeof(IGetInfoCallback))]
    public interface IGetInfo
    {
        [OperationContract]
        string GetData(int value);

    }

    public interface IGetInfoCallback
    {
        [OperationContract]
        void SendData(string value);


    }
}
