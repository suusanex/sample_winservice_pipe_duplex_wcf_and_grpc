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
        [OperationContract(IsInitiating = true)]
        void SessionConnect();

        [OperationContract(IsInitiating = false)]
        void SessionDisconnect();


        [OperationContract(IsInitiating = false)]
        string GetData(int value);

    }

    public interface IGetInfoCallback
    {
        [OperationContract]
        void SendData(string value);


    }
}
