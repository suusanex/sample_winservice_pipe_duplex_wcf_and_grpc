using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WCFIPCSample_Lib
{

    [ServiceContract(SessionMode = SessionMode.Required,
        CallbackContract = typeof(IServiceToUserSessionCallback))]
    public interface IUserSessionToService
    {
        [OperationContract(IsInitiating = true)]
        void SessionConnect();

        [OperationContract(IsInitiating = false)]
        void SessionDisconnect();


        [OperationContract(IsInitiating = false)]
        string GetData(int value);

    }

    public interface IServiceToUserSessionCallback
    {
        [OperationContract]
        bool SendData(string value);

        [OperationContract]
        bool SendData2(string value);

    }
}
