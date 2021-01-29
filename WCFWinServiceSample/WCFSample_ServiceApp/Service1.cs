using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Threading;
using NLog;
using WCFIPCSample_Lib;

namespace WCFSample_ServiceApp
{
    public partial class WCFSampleHostService : ServiceBase
    {
        public WCFSampleHostService()
        {
            InitializeComponent();
            MainTaskEndEvent = new AutoResetEvent(false);
        }

        Logger log = LogManager.GetCurrentClassLogger();

        protected override void OnStart(string[] args)
        {
            log.Trace("OnStart");

            var MainTaskObj = new Task(MainTask);

            MainTaskObj.Start();

            new Thread(() =>
            {
                while (true)
                {
                    UserSesionToServiceServer.SendDataAllSession($"AllSession, {DateTime.Now}");
                    Thread.Sleep(5000);
                }
            }).Start();
        }

        protected override void OnStop()
        {
            MainTaskEndEvent.Set();
        }

        AutoResetEvent MainTaskEndEvent;

        void MainTask()
        {
            try
            {
				using (var wcfSrv = new ServiceHost(typeof(UserSesionToServiceServer)))
                {
                    log.Trace("WCF Service new");

                    var uri = new Uri("net.pipe://localhost/WCFSample/DuplexService");
                    var binding = new NetNamedPipeBinding();
                    wcfSrv.AddServiceEndpoint(typeof(IUserSessionToService), binding, uri);

                    wcfSrv.Faulted += OnWCFFaulted;
                    wcfSrv.Open();

                    log.Trace("WCF Service Opened");

                    MainTaskEndEvent.WaitOne();
                    wcfSrv.Close();


                    log.Trace("WCF Service Closed");
                }
            }
            catch(Exception ex){
                log.Trace("MainTask Fail, {0}", ex.ToString());
                throw;
            }
        }


        private void OnWCFFaulted(object sender, EventArgs e)
        {
            log.Trace("WCF Fault {0}", e.ToString());

            var IWCF = sender as IUserSessionToService;
            if (IWCF != null)
            {
                log.Trace("WCF Fault {0}", e.ToString());
			}
        }

    }
}

