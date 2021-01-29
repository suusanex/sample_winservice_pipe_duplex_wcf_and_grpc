using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public class BindingSource : INotifyPropertyChanged
        {
            #region INotifyPropertyChanged実装 
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName = null));
            }
            #endregion

            public BindingSource()
            {
            }


            string _Output;
            public string Output
            {
                get => _Output;
                set { _Output = value; OnPropertyChanged(); }
            }

            //string _DUMMY;
            //public string DUMMY
            //{
            //    get => _DUMMY;
            //    set { _DUMMY = value; OnPropertyChanged(); }
            //}

        }

        public BindingSource m_Bind;

        public MainWindow()
        {
            InitializeComponent();

            m_Bind = new BindingSource();
            DataContext = m_Bind;
            CreateFactory();

        }

        private const int m_WriteLineMaxLines = 1000;
        private int m_WriteLineLines;

        void WriteLine(string msg)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (m_WriteLineMaxLines <= m_WriteLineLines)
                    {
                        var deleteLineCount = m_WriteLineMaxLines / 2;
                        Output.Text = string.Join(Environment.NewLine,
                            Output.Text.Split(Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries).Skip(deleteLineCount)) + Environment.NewLine;
                        m_WriteLineLines -= deleteLineCount;
                        Output.AppendText($"最大行数{m_WriteLineMaxLines}を超えたため、テキストの半分を削除しました" + Environment.NewLine);
                        m_WriteLineLines++;
                    }
                    Output.AppendText($"{m_WriteLineLines}:{msg}{Environment.NewLine}");
                    m_WriteLineLines++;
                    Output.ScrollToEnd();
                });
            }
            catch (Exception e)
            {
                log.Warn($"Window Text Write Fail, {msg}, {e}");
            }
        }

        private void CreateFactory()
        {
            if (channelFactory == null)
            {
                var client = new ServiceToUserSessionClient();
                client.OnSendDataRequest += value =>
                {
                    var message = $"OnSendDataRequest, {value}";
                    WriteLine(message);
                    log.Trace(message);
                    return true;
                };
                InstanceContext instanceContext = new InstanceContext(client);
                var uri = new Uri("net.pipe://localhost/WCFSample/DuplexService");
                var address = new EndpointAddress(uri);
                var binding = new NetNamedPipeBinding();

                channelFactory = new DuplexChannelFactory<IUserSessionToService>(instanceContext, binding, address);
            }
        }

        ~MainWindow()
        {
            WCFFinalize();
        }

        private DuplexChannelFactory<IUserSessionToService> channelFactory;
        private IUserSessionToService channel;

        private Logger log = LogManager.GetCurrentClassLogger();

        Timer channelReconnectTimer;

        private void OnBtnChannelOpen(object sender, RoutedEventArgs e)
        {
            CreateFactory();

            channelFactory.Open();

            channelReconnectTimer = new Timer(ChannelAndSessionReconnect, null, ReconnectTimeSpan, new TimeSpan(0, 0, 0, 0, -1));

            channel = channelFactory.CreateChannel();


            WriteLine("OK");
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
            WriteLine("OK");
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
            WriteLine("OK");
        }
        private void OnBtnSessionConnect(object sender, RoutedEventArgs e)
        {
            channel.SessionConnect();
            log.Trace($"{nameof(OnBtnSessionConnect)}");
            WriteLine("OK");
        }
        private void OnBtnSessionDisconnect(object sender, RoutedEventArgs e)
        {
            channel.SessionDisconnect();
            log.Trace($"{nameof(OnBtnSessionDisconnect)}");
            WriteLine("OK");
        }
        private void OnBtnStopWatch(object sender, RoutedEventArgs e)
        {
            var msg = new StringBuilder();

            var stop = new Stopwatch();
            stop.Start();

            OneCall();

            stop.Stop();
            msg.AppendLine($"1 time = {stop.Elapsed}");

            stop = new Stopwatch();
            stop.Start();

            int loop = 10;
            for (int i = 0; i < loop; i++)
            {
                OneCall();
            }

            stop.Stop();

            msg.AppendLine($"{loop} time = {stop.Elapsed}");

            WriteLine(msg.ToString());

            void OneCall()
            {

                CreateFactory();

                channelFactory.Open();

                channel = channelFactory.CreateChannel();
                channel.SessionConnect();
                var ret = channel.GetData(2);
                channel.SessionDisconnect();

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
        }

    }
}
