using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NLog;
using gRPCCoreLib;

namespace gRPCCoreClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
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

        }

        private Logger log = LogManager.GetCurrentClassLogger();

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

        private UserSessionToServiceGrpcClient _grpcClient;

        private async void OnBtnChannelOpen(object sender, RoutedEventArgs e)
        {
            if (_grpcClient != null)
            {
                await _grpcClient.DisposeAsync();
            }

            _grpcClient = new UserSessionToServiceGrpcClient();
            _grpcClient.OnGetDataResponse += OnGetDataResponse;
            _grpcClient.OnSendDataRequest += OnSendDataRequest;
            _grpcClient.Subscribe();
            WriteLine($"{nameof(OnBtnChannelOpen)} OK");
        }

        private bool OnSendDataRequest(string data)
        {
            WriteLine($"{nameof(OnSendDataRequest)} {data}");
            return true;
        }

        private void OnGetDataResponse(string data)
        {
            WriteLine($"{nameof(OnGetDataResponse)} {data}");
        }

        private async void OnBtnChannelClose(object sender, RoutedEventArgs e)
        {
            if (_grpcClient != null)
            {
                await _grpcClient.DisposeAsync();
                _grpcClient = null;
            }
            WriteLine($"{nameof(OnBtnChannelClose)} OK");
        }


        private async void OnBtnGetData(object sender, RoutedEventArgs e)
        {
            var ret = await _grpcClient.GetDataRequestAsync();

            log.Trace($"{nameof(OnBtnGetData)}, {ret}");
            WriteLine($"{nameof(OnBtnGetData)} OK");
        }

        private async void OnBtnSessionConnect(object sender, RoutedEventArgs e)
        {
            await _grpcClient.SessionConnectAsync();


            log.Trace("RegisterUserSessionRequest End");
            log.Trace($"{nameof(OnBtnSessionConnect)}");
            WriteLine($"{nameof(OnBtnSessionConnect)} OK");
        }

        private async void OnBtnSessionDisconnect(object sender, RoutedEventArgs e)
        {
            if (_grpcClient != null)
            {
                await _grpcClient.DisposeAsync();
                _grpcClient = null;
            }

            log.Trace($"{nameof(OnBtnSessionDisconnect)}");
            WriteLine($"{nameof(OnBtnSessionDisconnect)} OK");
        }

        private async void OnBtnStopWatch(object sender, RoutedEventArgs e)
        {
            if (_grpcClient != null)
            {
                await _grpcClient.DisposeAsync();
            }

            var stop = new Stopwatch();
            stop.Start();

            await OneCall().ConfigureAwait(false);

            async Task OneCall()
            {
                _grpcClient = new UserSessionToServiceGrpcClient();
                _grpcClient.Subscribe();
                await _grpcClient.GetDataRequestAsync().ConfigureAwait(false);

                await _grpcClient.DisposeAsync().ConfigureAwait(false);
                _grpcClient = null;
            }

            stop.Stop();
            Dispatcher.Invoke(()=> WriteLine($"1 time = {stop.Elapsed}"));

            stop = new Stopwatch();
            stop.Start();

            int loop = 10;
            for (int i = 0; i < loop; i++)
            {
                await OneCall().ConfigureAwait(false);
            }

            stop.Stop();

            Dispatcher.Invoke(() => WriteLine($"{loop} time = {stop.Elapsed}"));
        }
    }
}
