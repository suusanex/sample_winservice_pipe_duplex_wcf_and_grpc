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
using MathNet.Numerics.Statistics;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

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
            await ChannelOpen(GrpcConnectType.TCP);
            WriteLine($"{nameof(OnBtnChannelOpen)} OK");
        }

        private async Task ChannelOpen(GrpcConnectType connectType)
        {
            if (_grpcClient != null)
            {
                await _grpcClient.DisposeAsync();
            }

            _grpcClient = new UserSessionToServiceGrpcClient();
            _grpcClient.OnGetDataResponse += OnGetDataResponse;
            _grpcClient.OnSendDataRequest += OnSendDataRequest;
            _grpcClient.OnHighFrequencyResponse += OnHighFrequencyResponse;
            _grpcClient.Subscribe(connectType);
        }


        private async void OnBtnChannelOpenPipe(object sender, RoutedEventArgs e)
        {
            await ChannelOpen(GrpcConnectType.Pipe);
            WriteLine($"{nameof(OnBtnChannelOpenPipe)} OK");
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

            log.Debug($"{nameof(OnBtnGetData)}, {ret}");
            WriteLine($"{nameof(OnBtnGetData)} OK");
        }

        private async void OnBtnSessionConnect(object sender, RoutedEventArgs e)
        {
            await _grpcClient.SessionConnectAsync();


            log.Debug("RegisterUserSessionRequest End");
            log.Debug($"{nameof(OnBtnSessionConnect)}");
            WriteLine($"{nameof(OnBtnSessionConnect)} OK");
        }

        private async void OnBtnSessionDisconnect(object sender, RoutedEventArgs e)
        {
            if (_grpcClient != null)
            {
                await _grpcClient.DisposeAsync();
                _grpcClient = null;
            }

            log.Debug($"{nameof(OnBtnSessionDisconnect)}");
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
                _grpcClient.Subscribe(GrpcConnectType.TCP);
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

        private readonly TimeSpan m_HighFrequencyInterval = TimeSpan.FromMilliseconds(100);

        private async void OnHighFrequencyResponseTestStart(object sender, RoutedEventArgs e)
        {
            m_HighFrequencyResponseTestDelayTimes.Clear();
            m_HighFrequencyResponseSw.Restart();

            await _grpcClient.HighFrequencyResponseTestStartAsync(m_HighFrequencyInterval);


            log.Debug("HighFrequencyResponseTestStartAsync End");
            WriteLine($"{nameof(OnHighFrequencyResponseTestStart)} OK");

        }

        private async void OnHighFrequencyResponseTestEnd(object sender, RoutedEventArgs e)
        {
            await _grpcClient.HighFrequencyResponseTestEndAsync();

            WriteLine($"{nameof(OnHighFrequencyResponseTestEnd)} OK, ResCount={m_HighFrequencyResponseTestDelayTimes.Count}");

        }

        private readonly List<double> m_HighFrequencyResponseTestDelayTimes = new();
        private readonly List<double> m_HighFrequencyResponseTestIntervalTimes = new();

        DateTime m_LastResponseDateTime;

        readonly Stopwatch m_HighFrequencyResponseSw = new ();

        private void OnHighFrequencyResponse(long msgFileTime)
        {
            var nowDateTime = DateTime.Now;
            var responseDateTime = DateTime.FromFileTimeUtc(msgFileTime).ToLocalTime();
            var diff = nowDateTime - responseDateTime;
            m_HighFrequencyResponseTestDelayTimes.Add(diff.TotalMilliseconds);

            if (m_LastResponseDateTime != default)
            {
                m_HighFrequencyResponseTestIntervalTimes.Add((responseDateTime - m_LastResponseDateTime).TotalMilliseconds);
            }
            m_LastResponseDateTime = responseDateTime;

            if (TimeSpan.FromSeconds(5) < m_HighFrequencyResponseSw.Elapsed)
            {
                m_HighFrequencyResponseSw.Restart();
                var median = m_HighFrequencyResponseTestDelayTimes.Median();
                var ave = m_HighFrequencyResponseTestDelayTimes.Average();

                log.Debug("HighFrequencyResponseTestEndAsync End");
                WriteLine($"{nameof(OnHighFrequencyResponse)}, Delay, Median={m_HighFrequencyResponseTestDelayTimes.Median()}[ms], Average={m_HighFrequencyResponseTestDelayTimes.Average()}[ms]");
                WriteLine($"{nameof(OnHighFrequencyResponse)}, Interval, Expected={m_HighFrequencyInterval.TotalMilliseconds}[ms], Median={m_HighFrequencyResponseTestIntervalTimes.Median()}[ms], Average={m_HighFrequencyResponseTestIntervalTimes.Average()}[ms]");
                m_HighFrequencyResponseTestDelayTimes.Clear();
            }
        }

        private int m_GetDataRequestAsyncNumber;

        private async void OnNoStreamRepeatTestStart(object sender, RoutedEventArgs e)
        {
            log.Info("OnNoStreamRepeatTestStart Start");

            await NoStreamRepeatTestStart(GrpcConnectType.TCP);
        }

        private async Task NoStreamRepeatTestStart(GrpcConnectType connectType)
        {
            if (m_NoStreamRepeatTestCancel != null) await m_NoStreamRepeatTestCancel?.CancelAsync();
            m_NoStreamRepeatTestCancel = new();
            var cancelToken = m_NoStreamRepeatTestCancel.Token;

            var client = new UserSessionToServiceGrpcNoStreamClient(connectType);
            var sw = new Stopwatch();

            var viewSw = new Stopwatch();
            viewSw.Start();

            try
            {
                await Task.Run(async () =>
                {
                    while (!cancelToken.IsCancellationRequested)
                    {
                        m_GetDataRequestAsyncNumber++;
                        sw.Restart();
                        var res = await client.GetDataRequestAsync(m_GetDataRequestAsyncNumber);
                        sw.Stop();
                        var interval = sw.ElapsedMilliseconds;
                        log.Debug($"{nameof(OnNoStreamRepeatTestStart)}, {interval}[ms] {res.Data}");
                        m_NoStreamRepeatTestResponseTimes.Add(interval);

                        if (TimeSpan.FromSeconds(10) < viewSw.Elapsed)
                        {
                            viewSw.Restart();
                            log.Info("HighFrequencyResponseTestEndAsync End");
                            WriteLine(
                                $"{nameof(OnNoStreamRepeatTestStart)}, Response, Median={m_NoStreamRepeatTestResponseTimes.Median()}[ms], Average={m_NoStreamRepeatTestResponseTimes.Average()}[ms]");
                            m_NoStreamRepeatTestResponseTimes.Clear();
                        }

                        await Task.Delay(m_NoStreamRepeatTestInterval, cancelToken);
                    }
                }, cancelToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private async void OnNoStreamRepeatTestStartPipe(object sender, RoutedEventArgs e)
        {
            log.Info($"{nameof(OnNoStreamRepeatTestStartPipe)} Start");

            await NoStreamRepeatTestStart(GrpcConnectType.Pipe);

        }

        private void OnNoStreamRepeatTestEnd(object sender, RoutedEventArgs e)
        {
            m_NoStreamRepeatTestCancel?.Cancel();
            m_NoStreamRepeatTestCancel = null;

        }

        CancellationTokenSource? m_NoStreamRepeatTestCancel;
        private readonly TimeSpan m_NoStreamRepeatTestInterval = TimeSpan.FromMilliseconds(100);
        private readonly List<double> m_NoStreamRepeatTestResponseTimes = new();

        private void OnLoaded(object sender, RoutedEventArgs e)
        {

            log.Info("OnLoaded MainWindow");

        }

    }
}
