using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Ionic.Zip;
using Microsoft.Win32;

namespace TroveSkipLauncher.ViewModels
{
    public class MainWindowViewModel
    {
        private Dispatcher _dispatcher;

        private int _progressBarValue;
        private Visibility _progressVisibility;
        private StatusType _statusType;
        private DelegateCommand _hideWindowCommand;
        private DelegateCommand _closeWindowCommand;

        private static WebClient _webClient = new();
        private const string DbUrl = "https://pastebin.com/raw/rjyuQQyv";
        private const string UpdateUrl = "https://pastebin.com/raw/ALTsfWjv";

        public static string exePath = null;

        public int ProgressBarValue
        {
            get => _progressBarValue;
            set => _progressBarValue = value;
        }
        
        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set => _progressVisibility = value;
        }

        public StatusType StatusType
        {
            get => _statusType;
            set => _statusType = value;
        }

        public string StatusText { get; set; }
        
        public ICommand HideWindowCommand => _hideWindowCommand ??= new(HideWindow);
        public ICommand CloseWindowCommand => _closeWindowCommand ??= new(CloseWindow);

        public MainWindowViewModel()
        {
            _dispatcher = Application.Current.MainWindow.Dispatcher;
            _dispatcher.Invoke(StartUpdate);
            // _dispatcher.Invoke(AnimateStatusText);
        }

        private async Task StartUpdate()
        {
            SetStatus(StatusType.Checking);
            var id = Registry.CurrentUser.GetValue(@"SOFTWARE\NCT\id");
            if (id == null)
            {
                goto AuthFailed;
            }

            _webClient.DownloadProgressChanged += (_, args) =>
            {
                ProgressBarValue = args.ProgressPercentage;
            };
            string availableIds;
            try
            {
                availableIds = await _webClient.DownloadStringTaskAsync(DbUrl);
            }
            catch
            {
                MessageBox.Show("Download failed: No access to server");
                throw;
            }
            if (!availableIds.Contains(id.ToString()))
            {
                goto AuthFailed;
            }

            string updateString;
            try
            {
                ProgressBarValue = 0;
                updateString = _webClient.DownloadString(UpdateUrl);
            }
            catch
            {
                MessageBox.Show("Download failed: No access to server");
                goto Close;
            }

            var downloadLink = updateString.Split(' ')[1];
            var tempFileName = Path.GetTempFileName();
            SetStatus(StatusType.Downloading);
            try
            {
                ProgressBarValue = 0;
                await _webClient.DownloadFileTaskAsync(downloadLink, tempFileName);
            }
            catch
            {
                File.Delete(tempFileName);
                MessageBox.Show("Download data failed: No access to server or file does not exist");
                goto Close;
            }
            SetStatus(StatusType.Installing);
            using (var zipFile = Ionic.Zip.ZipFile.Read(tempFileName))
            {
                var extractPath = exePath ?? Environment.CurrentDirectory;
                zipFile.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
            }
            SetStatus(StatusType.Done);
            return;
            AuthFailed:
            MessageBox.Show("Authentication failed.");
            Close:
            CloseWindow();
        }
        
        private void HideWindow()
        {
            if (Application.Current.MainWindow != null)
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }
        
        private void CloseWindow()
        {
            Environment.Exit(0);
        }

        private void SetStatus(StatusType statusType)
        {
            StatusType = statusType;
            StatusText = statusType.ToString();
            ProgressVisibility = Visibility.Visible;
            //ProgressVisibility = statusType == StatusType.Downloading || statusType == StatusType.Checking ? Visibility.Visible : Visibility.Hidden;
        }

        private async void AnimateStatusText()
        {
            var stringBuilder = new StringBuilder();
            while (true)
            {
                await Task.Delay(333);
                var typeString = StatusType.ToString();
                if (!typeString.EndsWith("ing")) continue;
                var dotsCount = StatusText.ToCharArray().Count(x => x == '.');
                stringBuilder.Append(typeString);
                if (dotsCount < 3)
                {
                    stringBuilder.Append('.');
                }
                
                StatusText = stringBuilder.ToString();
                //throw new Exception(stringBuilder.ToString());
                stringBuilder.Clear();
            }
        }
    }
}