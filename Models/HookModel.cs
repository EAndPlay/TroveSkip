using System;
using System.ComponentModel;
using System.Diagnostics;
using TroveSkip.ViewModels;

namespace TroveSkip.Models
{
    public class HookModel
    {
        public Process Process { get; private set; } //was readonly @field
        public int Id { get; }
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

        // public bool MapCheck;
        // public bool ZoomCheck;
        // public bool FovCheck;
        // public bool ChamsCheck;
        // public bool MiningCheck;

        public bool HasExited => Process == null || Process.HasExited;
        
        public string Name { get; set; }

        public HookModel(Process process, string name)
        {
            Process = process;
            Name = name;
            Id = process.Id;
            Handle = process.Handle;
            WindowHandle = process.MainWindowHandle;
            try
            {
                Module = process.MainModule;
            }
            catch (Win32Exception)
            {
                return;
            }
            ModuleAddress = (int) Module.BaseAddress;
            Process.EnableRaisingEvents = true;
            Process.Exited += (_, _) => Process = null;
            IsPrimary = false;
        }

        public HookModel(HookModel hookModel, string name) : this(hookModel.Process, name) {}
        // public HookModel(HookModel hookModel, string name) : this(hookModel.Process, name)
        // {
        //     MapCheck = hookModel.MapCheck;
        //     ZoomCheck = hookModel.ZoomCheck;
        //     FovCheck = hookModel.FovCheck;
        //     ChamsCheck = hookModel.ChamsCheck;
        //     MiningCheck = hookModel.MiningCheck;
        // }
    }
}