﻿using System;
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
using System.Threading;
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

            CreateFactory();

        }

        private void CreateFactory()
        {
            if (channelFactory == null)
            {
                InstanceContext instanceContext = new InstanceContext(new GetInfoClient());
                var uri = new Uri("net.pipe://localhost/WCFSample/DuplexService");
                var address = new EndpointAddress(uri);
                var binding = new NetNamedPipeBinding();

                channelFactory = new DuplexChannelFactory<IGetInfo>(instanceContext, binding, address);
            }
        }

        ~MainWindow()
        {
            WCFFinalize();
        }

        private DuplexChannelFactory<IGetInfo> channelFactory;
        private IGetInfo channel;

        private Logger log = LogManager.GetCurrentClassLogger();

        Timer channelReconnectTimer;

        private void OnBtnChannelOpen(object sender, RoutedEventArgs e)
        {
            CreateFactory();

            channelFactory.Open();

            channelReconnectTimer = new Timer(ChannelAndSessionReconnect, null, ReconnectTimeSpan, new TimeSpan(0, 0, 0, 0, -1));

            channel = channelFactory.CreateChannel();


            MessageBox.Show("OK");
        }

        private TimeSpan ReconnectTimeSpan = new TimeSpan(0, 9, 0);//サーバー側のReceiveTimeoutより短くする必要がある

        private void OnBtnChannelClose(object sender, RoutedEventArgs e)
        {
            channelReconnectTimer?.Dispose();
            channelReconnectTimer = null;

            try
            {
                channelFactory.Close();
            }
            catch
            {
                channelFactory.Abort();
            }

            channelFactory = null;
            MessageBox.Show("OK");
        }

        void ChannelAndSessionReconnect(object NullObj)
        {
            log.Trace($"{nameof(ChannelAndSessionReconnect)}");
            try
            {

                try
                {
                    channelFactory.Close();
                }
                catch
                {
                    channelFactory.Abort();
                }
                channelFactory = null;


                CreateFactory();

                channelFactory.Open();
                channelReconnectTimer.Change(ReconnectTimeSpan, new TimeSpan(0, 0, 0, 0, -1));

                channel = channelFactory.CreateChannel();

                channel.SessionConnect();

            }
            catch (Exception e)
            {
                log.Trace($"{e}");
            }
        }

        private void WCFFinalize()
        {
            try
            {
                channel?.SessionDisconnect();
            }
            catch { }

            try
            {
                channelFactory.Close();
            }
            catch
            {
                channelFactory.Abort();
            }
            channelFactory = null;
        }


        private void OnBtnGetData(object sender, RoutedEventArgs e)
        {
            var ret = channel.GetData(2);
            log.Trace($"{nameof(OnBtnGetData)}, {ret}");
            MessageBox.Show("OK");
        }
        private void OnBtnSessionConnect(object sender, RoutedEventArgs e)
        {
            channel.SessionConnect();
            log.Trace($"{nameof(OnBtnSessionConnect)}");
            MessageBox.Show("OK");
        }
        private void OnBtnSessionDisconnect(object sender, RoutedEventArgs e)
        {
            channel.SessionDisconnect();
            log.Trace($"{nameof(OnBtnSessionDisconnect)}");
            MessageBox.Show("OK");
        }
    }
}
