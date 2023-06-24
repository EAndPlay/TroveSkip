using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TroveSkip.Properties;

namespace TroveSkip
{
    public enum StopType : byte
    {
        FullStop,
        Slow
    }

    public enum FollowType : byte
    {
        Local,
        Target
    }
    
    public enum TargetCheckType : byte
    {
        AllToLeaderToTarget,
        AllToTarget
    }

    [Serializable]
    public class BotsSettings : INotifyPropertyChanged
    {
        public float StopDistance { get; set; }
        public StopType StopType { get; set; }
        public float StopPower { get; set; }
        public string NoClipToggleButton { get; set; }
        public bool WarnEnabled { get; set; }
        public uint WarnDistance { get; set; }
        private FollowType _followType;

        public FollowType FollowType
        {
            get => _followType;
            set
            {
                _followType = value;
                OnPropertyChanged();
            }
        }

        public TargetCheckType TargetCheckType { get; set; }
        public string FollowTargetName { get; set; }
        public bool NoClip { get; set; }
        public bool AutoSetBot { get; set; }

        public BotsSettings()
        {
            StopDistance = 2;
            StopType = StopType.Slow;
            StopPower = 5;
            NoClipToggleButton = Key.None.ToString();
            WarnEnabled = false;
            FollowType = FollowType.Local;
            TargetCheckType = TargetCheckType.AllToLeaderToTarget;
            WarnDistance = 15;
            FollowTargetName = string.Empty;
            NoClip = false;
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}