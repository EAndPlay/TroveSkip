using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using TroveSkip.Memory;
using TroveSkip.Properties;
using TroveSkip.ViewModels;

namespace TroveSkip.Models
{
    public class HookModel : INotifyPropertyChanged
    {
        private const MainWindowViewModel.ProcessAccessFlags HookAccess =
            MainWindowViewModel.ProcessAccessFlags.CreateThread |
            MainWindowViewModel.ProcessAccessFlags.VirtualMemoryOperation |
            MainWindowViewModel.ProcessAccessFlags.VirtualMemoryRead |
            MainWindowViewModel.ProcessAccessFlags.VirtualMemoryWrite;
        
        public int Id { get; }
        public Process Process { get; } //was readonly @field
        public IntPtr Handle;
        public IntPtr WindowHandle;
        public ProcessModule Module { get; }
        public int ModuleAddress;
        public bool IsPrimary;

        public int WorldId;
        // public int NoClipAddress { get; set; }
        // public bool NoClipEnabled { get; set; }
        public bool Notified;
        public int LocalPlayerPointer;
        public int WorldPointer;
        public int SettingsPointer;
        public Dictionary<SettingOffset, float> Settings;

        // public bool MapCheck;
        // public bool ZoomCheck;
        // public bool FovCheck;
        // public bool ChamsCheck;
        // public bool MiningCheck;

        //public bool HasExited => Process == null || Process.HasExited;
        
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private bool _isBot;

        public bool IsBot
        {
            get => _isBot;
            set
            {
                _isBot = value;
                OnPropertyChanged();
            }
        }

        //public ICommand IsBotChangedCommand => MainWindowViewModel.Instance.IsBotChangedCommand;

        public HookModel(Process process, string name)
        {
            Process = process;
            Name = name ?? string.Empty;
            Id = process.Id;
            Handle = OpenProcess(HookAccess, false, Id);
            //Handle = process.Handle;
            WindowHandle = process.MainWindowHandle;
            ModuleAddress = 0;
            try
            {
                Module = process.MainModule;
            }
            catch (Win32Exception)
            {
                return;
            }
            ModuleAddress = (int) Module.BaseAddress;
            IsPrimary = false;
            Settings = new();
        }

        ~HookModel()
        {
            CloseHandle(Handle);
        }
        // public HookModel(HookModel hookModel, string name) : this(hookModel.Process, name) {}
        // public HookModel(HookModel hookModel, string name) : this(hookModel.Process, name)
        // {
        //     MapCheck = hookModel.MapCheck;
        //     ZoomCheck = hookModel.ZoomCheck;
        //     FovCheck = hookModel.FovCheck;
        //     ChamsCheck = hookModel.ChamsCheck;
        //     MiningCheck = hookModel.MiningCheck;
        // }
        
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(
            MainWindowViewModel.ProcessAccessFlags processAccess,
            bool bInheritHandle,
            int processId
        );
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}