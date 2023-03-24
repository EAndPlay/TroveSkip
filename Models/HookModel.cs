using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TroveSkip.Memory;
using TroveSkip.Properties;
using TroveSkip.ViewModels;

namespace TroveSkip.Models
{
    public class HookModel : INotifyPropertyChanged
    {
        public int Id { get; }
        public Process Process { get; } //was readonly @field
        public IntPtr Handle { get; }
        public IntPtr WindowHandle { get; }
        public ProcessModule Module { get; }
        public int ModuleAddress { get; }
        public bool IsPrimary { get; set; }
        public int WorldId { get; set; }
        // public int NoClipAddress { get; set; }
        // public bool NoClipEnabled { get; set; }
        public bool Notified { get; set; }
        public int LocalPlayerPointer { get; set; }
        public int WorldPointer { get; set; }
        public int SettingsPointer { get; set; }
        public Dictionary<SettingOffset, float> Settings { get; }

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

        public ICommand IsBotChangedCommand => MainWindowViewModel.Instance.IsBotChangedCommand;

        public HookModel(Process process, string name)
        {
            Process = process;
            Name = name ?? string.Empty;
            Id = process.Id;
            Handle = process.Handle;
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

        // public HookModel(HookModel hookModel, string name) : this(hookModel.Process, name) {}
        // public HookModel(HookModel hookModel, string name) : this(hookModel.Process, name)
        // {
        //     MapCheck = hookModel.MapCheck;
        //     ZoomCheck = hookModel.ZoomCheck;
        //     FovCheck = hookModel.FovCheck;
        //     ChamsCheck = hookModel.ChamsCheck;
        //     MiningCheck = hookModel.MiningCheck;
        // }
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}