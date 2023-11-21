using NLog;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace gRPCCoreClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Logger log = LogManager.GetCurrentClassLogger();
        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            log.Error(e.Exception.ToString);
            MessageBox.Show(e.Exception.ToString());
            e.Handled = true;
        }
    }
}
