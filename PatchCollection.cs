using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TroveSkip.Models;

namespace TroveSkip
{
    public class PatchCollection : ICloneable
    {
        private HookModel _owner;
        private List<Patch> _patches;

        public PatchCollection(HookModel owner)
        {
            _owner = owner;
            _patches = new();
        }
        
        public void Add(Patch patch)
        {
            patch = patch.Clone() as Patch;
            patch.SetOwner(_owner);
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

        public void Activate(PatchName name) => _patches.Find(x => x.Name == name).Activate();

        public void Deactivate(PatchName name) => _patches.Find(x => x.Name == name).Deactivate();

        public bool IsActivated(PatchName name) => _patches.Find(x => x.Name == name).IsActivated;

        public void ActivateAll() => _patches.ForEach(x => x.Activate());
        
        public void DeactivateAll() => _patches.ForEach(x => x.Deactivate());

        public List<Patch> GetPatches() => _patches;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}