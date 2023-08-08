using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TroveSkip.Memory;
using TroveSkip.Properties;
using TroveSkip.ViewModels;

namespace TroveSkip.Models
{
    public class HookModel : INotifyPropertyChanged
    {
        private const DarkSide.ProcessAccessFlags HookAccess =
            DarkSide.ProcessAccessFlags.CreateThread |
            DarkSide.ProcessAccessFlags.VirtualMemoryOperation |
            DarkSide.ProcessAccessFlags.VirtualMemoryRead |
            DarkSide.ProcessAccessFlags.VirtualMemoryWrite;
        
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
        public Dictionary<int, float> Settings;
        //public Dictionary<PatchName, Patch> Patches;
        public PatchCollection Patches;

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
            Handle = DarkSide.OpenProcess(HookAccess, false, Id);
            //Handle = process.Handle;
            
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

            Patches = new PatchCollection(this);
            Patches.Add(TroveSkip.Patches.AutoLoot);
            Patches.Add(TroveSkip.Patches.AutoAttack);
            Patches.Add(TroveSkip.Patches.InstaMining);
            Patches.Add(TroveSkip.Patches.NoClip);
            Patches.Add(TroveSkip.Patches.MapHack);
            Patches.Add(TroveSkip.Patches.ZoomHack);
            Patches.Initialize();

            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                while (!IsInWorld()) await Task.Delay(50);
                
                WindowHandle = process.MainWindowHandle;
            });
        }

        ~HookModel()
        {
            DarkSide.CloseHandle(Handle);
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

        public bool IsInWorld() =>
            DarkSide.GetAddress(Handle, LocalPlayerPointer, Offsets.LocalPlayer.CharacterSelf) != 0;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}