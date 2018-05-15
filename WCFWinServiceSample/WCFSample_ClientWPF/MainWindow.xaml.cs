using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ServiceModel;
using WCFIPCSample_Lib;
using NLog;

namespace WCFSample_ClientWPF
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            InstanceContext instanceContext = new InstanceContext(new GetInfoClient());
            var uri = new Uri("net.pipe://localhost/WCFSample/DuplexService");
            var address = new EndpointAddress(uri);
            var binding = new NetNamedPipeBinding();

            channelFactory = new DuplexChannelFactory<IGetInfo>(instanceContext, binding, address);

        }

        ~MainWindow()
        {
            WCFFinalize();
        }

        private DuplexChannelFactory<IGetInfo> channelFactory;
        private IGetInfo channel;

        private Logger log = LogManager.GetCurrentClassLogger();

        private void OnBtnChannelOpen(object sender, RoutedEventArgs e)
        {
            channelFactory.Open();
            channel = channelFactory.CreateChannel();
            MessageBox.Show("OK");
        }

        private void OnBtnChannelClose(object sender, RoutedEventArgs e)
        {
            WCFFinalize();
            MessageBox.Show("OK");
        }

        private void WCFFinalize()
        {
            channelFactory.Close();
        }


        private void OnBtnGetData(object sender, RoutedEventArgs e)
        {
            var ret = channel.GetData(2);
            log.Trace($"{nameof(OnBtnGetData)}, {ret}");
            MessageBox.Show("OK");
        }
    }
}
