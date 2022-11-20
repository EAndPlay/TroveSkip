using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TroveSkip.Properties;

namespace TroveSkip
{
    public enum BotsStopType : byte
    {
        FullStop,
        Slow
    }

    public enum OnOff : byte
    {
        On, Off
    }
    
    [Serializable]
    public class BotsSettings : INotifyPropertyChanged
    {
        public float StopDistance;
        public byte StopType;
        public float StopPower;
        public string NoClipToggle;
        public byte WarnStatus;
        public uint WarnDistance;
        public byte FollowType;
        public byte TargetCheckType;
        public string FollowTargetName;
        public bool NoClip;

        public BotsSettings()
        {
            StopDistance = 2;
            StopType = (byte) BotsStopType.Slow;
            StopPower = 5;
            NoClipToggle = Key.V.ToString();
            WarnStatus = FollowType = TargetCheckType = 0;
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