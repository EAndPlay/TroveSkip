using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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
            Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            
            //for invoke from other path
            if (e.Args.Length > 0)
                //Settings.path = e.Args[0] + "/" + Settings.path;
            {
                File.Delete(e.Args[0]);
            }
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message + '\n' + e.Exception.StackTrace);
        }
    }
}
