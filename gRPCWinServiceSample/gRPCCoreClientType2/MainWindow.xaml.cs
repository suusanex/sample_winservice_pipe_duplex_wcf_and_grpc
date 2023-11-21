using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
using gRPCCoreLib;
using Microsoft.Extensions.Logging;
using NLog;

namespace gRPCCoreClientType2
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

        private UserSessionToServiceType2GrpcClient _grpcClient;

        private async void OnBtnChannelOpen(object sender, RoutedEventArgs e)
        {
            if (_grpcClient != null)
            {
                await _grpcClient.DisposeAsync();
            }

            _grpcClient = new UserSessionToServiceType2GrpcClient();
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


        private async void OnBtnGetData2(object sender, RoutedEventArgs e)
        {
            var ret = await _grpcClient.GetDataRequestAsync();

            log.Debug($"{nameof(OnBtnGetData2)}, {ret}");
            WriteLine($"{nameof(OnBtnGetData2)} OK");
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

    }
}
