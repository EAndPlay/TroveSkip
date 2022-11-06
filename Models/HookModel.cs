using System;
using System.Diagnostics;

namespace TroveSkip.Models
{
    public class HookModel
    {
        public Process Process { get; private set; } //was readonly @field
        public int Id { get; }
        public IntPtr Handle { get; }
        public ProcessModule Module { get; }
        public int ModuleAddress { get; }
        public bool IsPrimary { get; set; }
        public int WorldId { get; set; }

        // public bool MapCheck;
        // public bool ZoomCheck;
        // public bool FovCheck;
        // public bool ChamsCheck;
        // public bool MiningCheck;

        public bool HasExited => Process == null;
        // {
        //     get
        //     {
        //         Process?.Refresh();
        //         return Process == null || Process.HasExited;
        //     }
        // }
        public string Name { get; }

        public HookModel(Process process, string name)
        {
            Process = process;
            Name = name;
            Id = process.Id;
            Handle = process.Handle;
            Module = process.MainModule;
            ModuleAddress = (int) Module.BaseAddress;
            Process.EnableRaisingEvents = true;
            Process.Exited += (_, _) => Process = null;
            IsPrimary = false;
        }

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