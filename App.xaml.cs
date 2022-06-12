using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TroveSkip
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            if (e.Args.Length > 0)
                Settings.Path = e.Args[0] + "/" + Settings.Path;
        }

        private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message + '\n' + e.Exception.StackTrace);
        }
    }
}
