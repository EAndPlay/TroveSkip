using System;
using System.Collections.Generic;
using System.Linq;
using TroveSkip.Models;

namespace TroveSkip
{
    public class Patch : ICloneable
    {
        public PatchName Name { get; }
        public bool IsActivated { get; private set; }

        private readonly PatchPair[] _patchPairs;

        private readonly int[] _addresses;
        private HookModel _owner;
        private IntPtr _handle;
        
        public Patch(PatchName name, params PatchPair[] patchPairs)
        {
            Name = name;
            _patchPairs = patchPairs;
            _addresses = new int[patchPairs.Length];
        }

        public void Initialize()
        {
            for (int i = 0; i < _addresses.Length; i++)
            {
                _addresses[i] = DarkSide.FindSignatureAddress(_owner, _patchPairs[i].DisabledSignature);
                if (_addresses[i] == 0)
                {
                    _addresses[i] = DarkSide.FindSignatureAddress(_owner, _patchPairs[i].EnabledSignature);
                    //kostil:
                    IsActivated = true;
                }
            }
        }
        
        public void Activate()
        {
            if (IsActivated) return;
            
            for (int i = 0; i < _addresses.Length; i++)
            {
                DarkSide.OverwriteBytes(_handle, _addresses[i], _patchPairs[i].EnabledSignature);
            }

            IsActivated = true;
        }

        public void Deactivate()
        {
            if (!IsActivated) return;

            for (int i = 0; i < _addresses.Length; i++)
            {
                DarkSide.OverwriteBytes(_handle, _addresses[i], _patchPairs[i].DisabledSignature);
            }

            IsActivated = false;
        }

        public void SetOwner(HookModel hookModel)
        {
            _owner = hookModel;
            _handle = hookModel.Handle;
        }
        
        public class PatchPair
        {
            public int[] DisabledSignature;
            public int[] EnabledSignature;

            public PatchPair(int[] disabledSignature, int[] enabledSignature)
            {
                DisabledSignature = disabledSignature.ToArray();
                EnabledSignature = enabledSignature.ToArray();
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}