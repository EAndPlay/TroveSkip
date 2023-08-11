using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TroveSkip.Models;

namespace TroveSkip
{
    public class PatchCollection : ICloneable
    {
        private List<Patch> _patches;

        public PatchCollection()
        {
            _patches = new();
        }
        
        public void Add(Patch patch)
        {
            patch = patch.Clone() as Patch;
            _patches.Add(patch);
        }

        public void Initialize()
        {
            new Thread(() =>
            {
                _patches.ForEach(x => x.Initialize());
            }) {IsBackground = true}.Start();
        }

        //no remove!

        public void Patch(PatchName name) => _patches.Find(x => x.Name == name).Activate();

        public void Unpatch(PatchName name) => _patches.Find(x => x.Name == name).Deactivate();

        public bool IsPatched(PatchName name) => _patches.Find(x => x.Name == name).IsActivated;

        public void ActivateAll() => _patches.ForEach(x => x.Activate());

        public void DeactivateAll() => _patches.ForEach(x => x.Deactivate());

        public void SetOwner(HookModel owner) => _patches.ForEach(x => x.SetOwner(owner));

        public List<Patch> GetPatches() => _patches;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}