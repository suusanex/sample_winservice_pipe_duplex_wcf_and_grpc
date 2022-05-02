using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication;
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

            HostInitRetry = () =>
            {
                log.Trace("WCF HostInitRetry Start");
                Task.Run(MainTask);
            };

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

        private UserSesionToServiceServer m_UserSesionToServiceServer = new UserSesionToServiceServer();

        internal static Action HostInitRetry;

        private ServiceHost m_WCFService;

        void MainTask()
        {
            try
            {
                var wcfSrv = new ServiceHost(m_UserSesionToServiceServer);

                var oldWcfSrv = Interlocked.Exchange(ref m_WCFService, wcfSrv);
                if (oldWcfSrv != null)
                {
                    using (oldWcfSrv)
                    {
                        try
                        {
                            oldWcfSrv.Abort();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                m_WCFService = wcfSrv;
                {
                    log.Trace("WCF Service new");

                    var uri = new Uri("net.pipe://localhost/WCFSample/DuplexService");
                    var binding = new NetNamedPipeBinding();
                    wcfSrv.AddServiceEndpoint(typeof(IUserSessionToService), binding, uri);

                    wcfSrv.Faulted += OnWCFFaulted;
                    wcfSrv.Closed += OnWCFClosed;
                    wcfSrv.Open();

                    m_CheckTimer = new Timer(o =>
                    {
                        log.Trace($"WCF State = {wcfSrv?.State}");
                    }, null, 0, 1000);

                    log.Trace("WCF Service Opened");

                    MainTaskEndEvent.WaitOne();

                    m_CheckTimer.Dispose();
                    wcfSrv.Faulted -= OnWCFFaulted;
                    wcfSrv.Close();


                    log.Trace("WCF Service Closed");
                }
            }
            catch(Exception ex){
                log.Trace("MainTask Fail, {0}", ex.ToString());
                throw;
            }
        }

        private Timer m_CheckTimer;

        private void OnWCFClosed(object sender, EventArgs e)
        {
            log.Trace("WCF Closed {0}", e.ToString());
            OnWCFFaulted(sender, e);
        }


        private void OnWCFFaulted(object sender, EventArgs e)
        {
            log.Trace("WCF Fault {0}", e.ToString());

            var IWCF = sender as IUserSessionToService;
            if (IWCF != null)
            {
                log.Trace("WCF Fault {0}", e.ToString());
			}

            MainTaskEndEvent.Set();
            MainTaskEndEvent.Reset();

            Task.Run(MainTask);
        }

    }
}

