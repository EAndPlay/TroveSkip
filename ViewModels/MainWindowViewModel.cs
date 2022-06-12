using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using TroveSkip.Models;
using TroveSkip.Properties;

namespace TroveSkip.ViewModels
{
    public partial class MainWindowViewModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        private readonly Dispatcher _dispatcher;

        public ObservableCollection<HookModel> Hooks { get; } = new();
        
        private HookModel _hookModel;
        private IntPtr _handle;

        public HookModel HookModel
        {
            get => _hookModel;
            set
            {
                if (value != null)
                {
                    try
                    {
                        _currentBaseAddress = value.Module.BaseAddress + _baseAddress;
                        _hookModel = value;
                        _handle = _hookModel.Handle;
                        _encryptionKey = ReadUInt(StatsEncKeyOffsets);
                        if (_encryptionKey != 0)
                        {
                            var bytes = BitConverter.GetBytes((float) SpeedHackValue);
                            _encryptedSpeed = BitConverter.ToUInt32(bytes, 0) ^ _encryptionKey;
                        }
                        MapCheck = _hookModel.MapCheck;
                        ZoomCheck = _hookModel.ZoomCheck;
                        FovCheck = _hookModel.FovCheck;
                        ChamsCheck = _hookModel.ChamsCheck;
                        MiningCheck = _hookModel.MiningCheck; 
                        EnableAntiAfk();
                    }
                    catch
                    {
                        _hookModel = null;
                    }

                }
                else
                {
                    MapCheck = ZoomCheck = FovCheck = ChamsCheck = MiningCheck = false;
                    _hookModel = null;
                }
                OnPropertyChanged();
            }
        }

        private readonly Dictionary<string, Key> _binds = new();
        private readonly Dictionary<Key, bool> _pressedKeys = new();
        private readonly List<int> _antiAfkList = new();

        private readonly Regex _avaibleName = new(@"^[1-9-a-zA-Z-_]+$");

        public Settings Settings;
        private readonly UserActivityHook _activityHook = new();

        private string _currentButton;
        private Button _currentButtonElement;

        private int _baseAddress;
        private int _chatBaseAddress;
        private IntPtr _currentBaseAddress;

        private float _sprintValue;
        private float _skipValue;
        private float _jumpForceValue;

        private bool _followApp;
        private bool _sprintCheck;
        private bool _speedCheck;
        private bool _jumpCheck;

        private uint _lastSpeed;
        private int _speedHackValue;
        private uint _encryptedSpeed;

        private uint _encryptionKey;

        private int _xCoordinate;

        private string _skipButton;
        private string _sprintButton;
        private string _sprintToggleButton;
        private string _jumpButton;
        private string _jumpToggleButton;
        private string _speedHackToggle;

        private bool _mapCheck;
        private bool _zoomCheck;
        private bool _fovCheck;
        private bool _chamsCheck;
        private bool _miningCheck;
        
        private Visibility _searchWindowVisibility;
        private Visibility _mainPageVisibility;
        private Visibility _settingsPageVisibility;

        private DelegateCommand<CheckBox> _mapCheckCommand;
        private DelegateCommand<CheckBox> _zoomCheckCommand;
        private DelegateCommand<CheckBox> _fovCheckCommand;
        private DelegateCommand<CheckBox> _chamsCheckCommand;
        private DelegateCommand<CheckBox> _miningCheckCommand;
        private DelegateCommand<Button> _bindClickCommand;
        private DelegateCommand<Button> _switchPageCommand;
        private DelegateCommand _invokeSearchWindowCommand;
        private DelegateCommand _findAddressCommand;
        private DelegateCommand _hideWindowCommand;
        private DelegateCommand _closeWindowCommand;
        // private DelegateCommand _clickComboBox;

        public string BaseAddress
        {
            get
            {
                var str = _baseAddress.ToString("X8");
                return str;
            }

            set
            {
                _baseAddress = int.Parse(value, NumberStyles.HexNumber);
                if (HookModel != null)
                    _currentBaseAddress = HookModel.Module.BaseAddress + _baseAddress;

                OnPropertyChanged();
            }
        }

        public float SprintValue
        {
            get => _sprintValue;
            set
            {
                _sprintValue = value;
                OnPropertyChanged();
            }
        }

        public float SkipValue
        {
            get => _skipValue;
            set
            {
                _skipValue = value;
                OnPropertyChanged();
            }
        }

        public float JumpForceValue
        {
            get => _jumpForceValue;
            set
            {
                _jumpForceValue = value;
                OnPropertyChanged();
            }
        }

        public bool FollowApp
        {
            get => _followApp;
            set
            {
                _followApp = value;
                OnPropertyChanged();
            }
        }

        public bool SprintCheck
        {
            get => _sprintCheck;
            set
            {
                _sprintCheck = value;
                OnPropertyChanged();
            }
        }
        
        public bool SpeedCheck
        {
            get => _speedCheck;
            set
            {
                _speedCheck = value;
                OnPropertyChanged();
            }
        }

        public bool JumpCheck
        {
            get => _jumpCheck;
            set
            {
                _jumpCheck = value;
                OnPropertyChanged();
            }
        }

        public int SpeedHackValue
        {
            get => _speedHackValue;
            set
            {
                _speedHackValue = value;
                if (_encryptionKey != 0)
                {
                    var bytes = BitConverter.GetBytes((float)value);
                    _encryptedSpeed = BitConverter.ToUInt32(bytes, 0) ^ _encryptionKey;
                }
                OnPropertyChanged();
            }
        }

        public int XCoordinate
        {
            get => _xCoordinate;
            set
            {
                _xCoordinate = value;
                OnPropertyChanged();
            }
        }

        public string SkipButton
        {
            get => _skipButton;
            set
            {
                _skipButton = value;
                OnPropertyChanged();
            }
        }

        public string SprintButton
        {
            get => _sprintButton;
            set
            {
                _sprintButton = value;
                OnPropertyChanged();
            }
        }

        public string SprintToggleButton
        {
            get => _sprintToggleButton;
            set
            {
                _sprintToggleButton = value;
                OnPropertyChanged();
            }
        }

        public string JumpButton
        {
            get => _jumpButton;
            set
            {
                _jumpButton = value;
                OnPropertyChanged();
            }
        }

        public string JumpToggleButton
        {
            get => _jumpToggleButton;
            set
            {
                _jumpToggleButton = value;
                OnPropertyChanged();
            }
        }

        public string SpeedHackToggle
        {
            get => _speedHackToggle;
            set
            {
                _speedHackToggle = value;
                OnPropertyChanged();
            }
        }

        public bool MapCheck
        {
            get => _mapCheck;
            set
            {
                _mapCheck = value;
                OnPropertyChanged();
            }
        }

        public bool ZoomCheck
        {
            get => _zoomCheck;
            set
            {
                _zoomCheck = value;
                OnPropertyChanged();
            }
        }

        public bool FovCheck
        {
            get => _fovCheck;
            set
            {
                _fovCheck = value;
                OnPropertyChanged();
            }
        }

        public bool ChamsCheck
        {
            get => _chamsCheck;
            set
            {
                _chamsCheck = value;
                OnPropertyChanged();
            }
        }

        public bool MiningCheck
        {
            get => _miningCheck;
            set
            {
                _miningCheck = value;
                OnPropertyChanged();
            }
        }

        public Visibility SearchWindowVisibility
        {
            get => _searchWindowVisibility;
            set
            {
                _searchWindowVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility MainPageVisibility
        {
            get => _mainPageVisibility;
            set
            {
                _mainPageVisibility = value;
                SettingsPageVisibility = value == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                OnPropertyChanged();
            }
        }

        public Visibility SettingsPageVisibility
        {
            get => _settingsPageVisibility;
            set
            {
                _settingsPageVisibility = value;
                OnPropertyChanged();
            }
        }

        public ICommand MapCheckCommand => _mapCheckCommand ??= new(x => InjectCheckChanged(x, _mapHack, _mapHackEnabled));
        public ICommand ZoomCheckCommand => _zoomCheckCommand ??= new(x => InjectCheckChanged(x, _zoomHack, _zoomHackEnabled));
        public ICommand FovCheckCommand => _fovCheckCommand ??= new(x => InjectCheckChanged(x, _fovHack, _fovHackEnabled));
        public ICommand ChamsCheckCommand => _chamsCheckCommand ??= new(x => InjectCheckChanged(x, _chamsMonsters, _chamsMonstersEnabled));
        public ICommand MiningCheckCommand => _miningCheckCommand ??= new(x => InjectCheckChanged(x, _mining, _miningEnabled));
        public ICommand BindClickCommand => _bindClickCommand ??= new(BindClick);
        public ICommand SwitchPageCommand => _switchPageCommand ??= new(x => SwitchPage(x));
        public ICommand InvokeSearchWindowCommand => _invokeSearchWindowCommand ??= new(InvokeSearchWindow);
        public ICommand FindAddressCommand => _findAddressCommand ??= new(FindAddress);
        public ICommand HideWindowCommand => _hideWindowCommand ??= new(HideWindow);
        public ICommand CloseWindowCommand => _closeWindowCommand ??= new(CloseWindow);
        // public ICommand ClickComboBox => _clickComboBox ??= new(() => RefreshHooks(true));

        private object GetKey(string name) => _binds[name] != Key.None ? 
            (object) (Regex.IsMatch(_binds[name].ToString(), @"D\d") ? 
                _binds[name].ToString().Replace("D", "") : _binds[name]) : "Not binded";

        public MainWindowViewModel()
        {
            _dispatcher = Application.Current.MainWindow.Dispatcher;
            SearchWindowVisibility = Visibility.Hidden;
            MainPageVisibility = Visibility.Visible;
            _dispatcher.ShutdownStarted += (_, _) => CloseWindow();
            _dispatcher.InvokeAsync(LoadSettings);
            
            _activityHook.KeyDown += (_, eve) => KeyCheck(eve.Key);
            _activityHook.KeyUp += (_, eve) => _pressedKeys[eve.Key] = false;

            _dispatcher.InvokeAsync(UpdateCurrent);
            _dispatcher.InvokeAsync(ForceSprint);
            _dispatcher.InvokeAsync(ForceSpeed);
            _dispatcher.InvokeAsync(FocusUpdate);
            _dispatcher.InvokeAsync(HooksUpdate);
        }

        private void HideWindow()
        {
            if (Application.Current.MainWindow != null)
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void CloseWindow()
        {
            SaveCurrent();
            Settings.Save();
            //Application.Current.Shutdown();
            Environment.Exit(0);
        }

        private void SwitchPage(ContentControl button)
        {
            MainPageVisibility = MainPageVisibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            var visible = MainPageVisibility == Visibility.Visible;
            button.Content = visible ? "SETS" : "MAIN";
            if (visible)
                SearchWindowVisibility = Visibility.Hidden;
        }

        private void EnableAntiAfk(Process process = null)
        {
            process ??= HookModel.Process;
            if (_antiAfkList.Contains(process.Id)) return;
            _antiAfkList.Add(process.Id);
            var address = IntPtr.Zero;
            Task.Run(() => address = (IntPtr) FindSignature(_antiAfk, process)).GetAwaiter().GetResult();
            if (address == IntPtr.Zero) return;
            
            var handle = process.Handle;
            var hAlloc = VirtualAllocEx(handle, IntPtr.Zero, (uint) _antiAfkCave.Length + 5, AllocationType.Commit, MemoryProtection);
            var cave = AsmJump((ulong) address + 6, (ulong) hAlloc, _antiAfkCave);
            
            WriteMemory(handle, hAlloc, cave);

            var jumpToAllocation = AsmJump((ulong) hAlloc, (ulong) address);
            WriteMemory(handle, address, jumpToAllocation);
        }

        private void InjectCheckChanged(ToggleButton checkBox, int[] find, int[] change)
        {
            var isChecked = checkBox.IsChecked ?? false;
            if (isChecked && GameClosed())
            {
                checkBox.IsChecked = false;
                return;
            }
            
            switch (checkBox.Name)
            {
                case "MapCheck":
                    HookModel.MapCheck = isChecked;
                    break;
                case "ZoomCheck":
                    HookModel.ZoomCheck = isChecked;
                    break;
                case "FovCheck":
                    HookModel.FovCheck = isChecked;
                    break;
                case "ChamsCheck":
                    HookModel.ChamsCheck = isChecked;
                    break;
                case "MiningCheck":
                    HookModel.MiningCheck = isChecked;
                    OverwriteBytes(isChecked ? _geodeTool : _geodeToolEnabled, isChecked ? _geodeToolEnabled : _geodeTool);
                    break;
                default:
                    MessageBox.Show("Non-existing CheckBox : " + checkBox.Name);
                    CloseWindow();
                    break;
            }
            var from = isChecked ? find : change;
            var to = isChecked ? change : find;
            OverwriteBytes(from, to);
        }

        private void InvokeSearchWindow()
        {
            SearchWindowVisibility = SearchWindowVisibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            if (SearchWindowVisibility == Visibility.Hidden)
                XCoordinate = 0;
        }

        private void FindAddress()
        {
            if (GameClosed()) return;

            if (XCoordinate > -3 && XCoordinate < 3)
            {
                MessageBox.Show("Mostly recommended be in X coordinate\nmore than 3 or less than -3");
                return;
            }

            MessageBox.Show("Open chat and Press ok\nDONT close chat till address found");
            
            var moduleAddress = HookModel.Module.BaseAddress;
            _baseAddress = _chatBaseAddress = 0;
            for (int i = 16000000; i < 18500000; i++)
            {
                var found = false;
                _dispatcher.Invoke(() =>
                {
                    found = FindBaseAddress(moduleAddress, i);
                });
                if (found)
                    break;
            }
            SearchWindowVisibility = Visibility.Hidden;
            
            MessageBox.Show($"Base address: {BaseAddress}\nChat address: {_chatBaseAddress:X8}\n");
        }

        private readonly Type _keyType = typeof(Key);
        private Key ParseKey(string key) => (Key)Enum.Parse(_keyType, key);
        
        private void LoadSettings()
        {
            Settings = Settings.Load();
            
            _binds.Add(nameof(SkipButton), ParseKey(Settings.SkipButton));
            _binds.Add(nameof(SprintButton), ParseKey(Settings.SprintButton));
            _binds.Add(nameof(SprintToggleButton), ParseKey(Settings.SprintToggleButton));
            _binds.Add(nameof(JumpButton), ParseKey(Settings.JumpButton));
            _binds.Add(nameof(JumpToggleButton), ParseKey(Settings.JumpToggleButton));
            _binds.Add(nameof(SpeedHackToggle), ParseKey(Settings.SpeedHackToggle));
            
            SkipButton = GetKey(nameof(SkipButton)).ToString();
            SprintButton = GetKey(nameof(SprintButton)).ToString();
            SprintToggleButton = GetKey(nameof(SprintToggleButton)).ToString();
            JumpButton = GetKey(nameof(JumpButton)).ToString();
            JumpToggleButton = GetKey(nameof(JumpToggleButton)).ToString();
            SpeedHackToggle = GetKey(nameof(SpeedHackToggle)).ToString();

            BaseAddress = Settings.BaseAddress;
            _chatBaseAddress = Convert.ToInt32(Settings.ChatBaseAddress, 16);
            SprintValue = Settings.SprintValue;
            SkipValue = Settings.SkipValue;
            JumpForceValue = Settings.JumpForceValue;
            SpeedHackValue = Settings.SpeedHackValue;
            FollowApp = Settings.FollowApp;
        }
        
        public void RefreshHooks(bool change = false)
        {
            var processList = Process.GetProcessesByName("Trove");

            foreach (var process in processList)
            {
                try
                {
                    if (process.MainModule == null) continue;
                }
                catch
                {
                    continue;
                }

                var handle = process.Handle;
                var name = GetName(handle);
                var hook = new HookModel(process, name);
                var copy = Hooks.FirstOrDefault(x => x.Id == process.Id);
                if (copy != null)
                {
                    if (copy.Name.Length == 0 && name.Length > 0)
                    {
                        var index = Hooks.IndexOf(copy);
                        hook = new HookModel(copy, name);
                        Hooks[index] = hook;
                    }
                }
                else
                {
                    Hooks.Add(hook);
                }

                EnableAntiAfk(process);
            }

            foreach (var hook in Hooks)
            {
                if (processList.FirstOrDefault(x => x.Id == hook.Id) == null)
                    Hooks.Remove(hook);
            }
            if (Hooks.Count == 0)
            {
                HookModel = null;
            }
            else if (change && HookModel == null && Hooks.Count > 0)
            {
                HookModel = Hooks.First();
            }
        }
        
        private void Skip()
        {
            var xposAdd = GetAddress(_xPosition);
            var yposAdd = xposAdd + 4;
            var zposAdd = yposAdd + 4;
            var xviewAdd = GetAddress(_xView);
            var yviewAdd = xviewAdd + 4;
            var zviewAdd = yviewAdd + 4;

            var xPos = ReadFloat(xposAdd);
            var yPos = ReadFloat(yposAdd);
            var zPos = ReadFloat(zposAdd);
            var xPer = ReadFloat(xviewAdd);
            var yPer = ReadFloat(yviewAdd);
            var zPer = ReadFloat(zviewAdd);
            // var xPos = ReadFloat(XPosition);
            // var yPos = ReadFloat(YPosition);
            // var zPos = ReadFloat(ZPosition);
            // var xPer = ReadFloat(XView);
            // var yPer = ReadFloat(YView);
            // var zPer = ReadFloat(ZView);
            WriteFloat(xposAdd, xPer * SkipValue + xPos);
            WriteFloat(yposAdd, yPer * SkipValue + yPos);
            WriteFloat(zposAdd, zPer * SkipValue + zPos);
        }

        private void SuperJump()
        {
            if (!JumpCheck || GameClosed() || NotFocused()) return;
            WriteFloat(_yPosition, ReadFloat(_yPosition) + JumpForceValue);
        }
        
        private async void ForceSprint()
        {
            while (true)
            {
                await Task.Delay(10);
                while (!SprintCheck || !IsPressed(_binds[nameof(SprintButton)]) || GameClosed() || NotFocused())
                    await Task.Delay(100);
                
                var xviewAdd = GetAddress(_xView);
                var yviewAdd = xviewAdd + 4;
                var zviewAdd = yviewAdd + 4;
                var xView = ReadFloat(xviewAdd);
                var yView = ReadFloat(yviewAdd);
                var zView = ReadFloat(zviewAdd);
                
                var velocityX = xView * SprintValue;
                var velocityY = yView * SprintValue;
                var velocityZ = zView * SprintValue;
                
                var velocityAdd = GetAddress(_xVelocity);
                WriteFloat(velocityAdd, velocityX);
                WriteFloat(velocityAdd + 4, velocityY);
                WriteFloat(velocityAdd + 8, velocityZ);
            }
        }

        private async void ForceSpeed()
        {
            while (true)
            {
                await Task.Delay(10);
                while (!SpeedCheck || GameClosed() || (FollowApp && NotFocused()))
                    await Task.Delay(100);
                WriteUInt(_speedOffsets, _encryptedSpeed);
            }
        }

        private async void FocusUpdate()
        {
            while (true)
            {
                while (!FollowApp)
                {
                    await Task.Delay(50);
                }
                var handle = GetForegroundWindow();
                
                GetWindowThreadProcessId(handle, out var procId);
                // if (_processesExcept.Contains(procId))
                // {
                //     await Task.Delay(50);
                //     continue;
                // }
                var proc = Process.GetProcessById(procId);
                
                if (proc.ProcessName == "Trove")
                {
                    if (HookModel == null || HookModel.Id != proc.Id)
                    {
                        var copy = Hooks.FirstOrDefault(x => x.Id == proc.Id);
                        if (copy != null)
                        {
                            HookModel = copy;
                        }
                        else
                        {
                            var name = GetName(handle);
                            var hook = new HookModel(proc, name);
                            Hooks.Add(hook);
                            HookModel = hook;
                        }
                    }
                }
                else
                {
                    // _processesExcept.Add(proc.Id);
                    HookModel = null;
                }
                await Task.Delay(50);
            }
        }

        private async void HooksUpdate()
        {
            while (true)
            {
                await Task.Delay(60000);
                var processList = Process.GetProcessesByName("Trove");
                foreach (var process in processList)
                {
                    EnableAntiAfk(process);
                }
            }
        }
        
        public bool GameClosed()
        {
            if (HookModel == null || HookModel.HasExited)
            {
                if (HookModel != null)
                    Hooks.Remove(HookModel);
                HookModel = null;
                return true;
            }
            return false;
        }

        private bool NotFocused()
        {
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero) return true;

            GetWindowThreadProcessId(handle, out var procId);
            var id = HookModel?.Id ?? 0;
            var notFocused = id != procId;
            // if (FollowApp && notFocused)
            //     Hook = null;
            return notFocused;
        }

        private bool ChatOpened()
        {
            if (GameClosed() || _chatBaseAddress == 0) return true; // || _chatOffset == 0
            var bytes = new byte[4];
            var address = HookModel.Module.BaseAddress + _chatBaseAddress; //+ 0x98 ; 00FD9E20
            ReadMemory(address, bytes);
            address = (IntPtr)(BitConverter.ToInt32(bytes, 0) + ChatOpenedOffsets[0]);

            var buffer = new byte[1];
            ReadMemory(address, buffer);
            var value = buffer[0] != 0;
            
            return value;
        }

        private void KeyCheck(Key key)
        {
            if (GameClosed() || NotFocused() || ChatOpened()) return;

            if (!_pressedKeys.TryGetValue(key, out _))
                _pressedKeys.Add(key, false);

            if (key == _binds[nameof(SkipButton)] && !IsPressed(key))
                _dispatcher.InvokeAsync(Skip);

            if (!IsPressed(key))
            {
                if (key == _binds[nameof(JumpButton)])
                    _dispatcher.InvokeAsync(SuperJump);

                if (key == _binds[nameof(SprintToggleButton)])
                    SprintCheck = !SprintCheck;

                if (key == _binds[nameof(JumpToggleButton)])
                    JumpCheck = !JumpCheck;

                if (key == _binds[nameof(SpeedHackToggle)])
                {
                    SpeedCheck = !SpeedCheck;
                    var speed = ReadUInt(_speedOffsets);
                    if (SpeedCheck)
                        _lastSpeed = speed;
                    else
                        WriteUInt(_speedOffsets, _lastSpeed);
                }
            }

            _pressedKeys[key] = true;
        }

        private async void UpdateCurrent()
        {
            while (true)
            {
                await Task.Delay(100);

                if (!FollowApp)
                {
                    if (HookModel != null && HookModel.HasExited)
                    { 
                        Hooks.Remove(Hooks.First(x => x.Id == HookModel.Id));
                        HookModel = null;
                    }
                    while (HookModel == null)
                    {
                        if (Hooks.Count > 0)
                        {
                            HookModel = Hooks.First();
                            break;
                        }
                        
                        await Task.Delay(750);
                        RefreshHooks(true);

                        await Task.Delay(750);
                        if (HookModel != null || FollowApp)
                            break;
                    }
                }

                if (HookModel != null && HookModel.Name.Length == 0)
                {
                    var name = GetName(IntPtr.Zero);
                    var copy = Hooks.FirstOrDefault(x => x.Id == HookModel.Id);
                    if (copy != null && name.Length > 0)
                    {
                        var index = Hooks.IndexOf(copy);
                        var hook = new HookModel(copy, name);
                        Hooks[index] = hook;
                        HookModel = hook;
                    }
                }
            }
        }

        private string GetName(IntPtr handle)
        {
            var buffer = new byte[16];
            if (handle != IntPtr.Zero)
                ReadProcessMemory(handle, GetAddress(handle, NameOffests), buffer, buffer.Length, out _);
            else
                ReadMemory(GetAddress(NameOffests), buffer);

            var name = Encoding.ASCII.GetString(buffer);
            int last;
            for (last = 0; last < name.Length; last++)
            {
                if (!_avaibleName.IsMatch(name[last].ToString().ToLower()))
                    break;
            }

            return name.Substring(0, last);
        }
        
        private string GetPowerRank()
        {
            var pr = ReadUInt(PowerRankOffsets);
            var fl = pr ^ _encryptionKey;
            var bytes = BitConverter.GetBytes(fl);
            
            return BitConverter.ToInt32(bytes, 0).ToString();
        }
        
        public void SaveCurrent()
        {
            Settings.BaseAddress = BaseAddress;
            Settings.ChatBaseAddress = _chatBaseAddress.ToString("X8");
            Settings.SkipValue = SkipValue;
            Settings.SprintValue = SprintValue;
            Settings.JumpForceValue = JumpForceValue;
            Settings.SpeedHackValue = SpeedHackValue;
            Settings.SkipButton = _binds[nameof(SkipButton)].ToString();
            Settings.SprintButton = _binds[nameof(SprintButton)].ToString();
            Settings.SprintToggleButton = _binds[nameof(SprintToggleButton)].ToString();
            Settings.JumpButton = _binds[nameof(JumpButton)].ToString();
            Settings.JumpToggleButton = _binds[nameof(JumpToggleButton)].ToString();
            Settings.SpeedHackToggle = _binds[nameof(SpeedHackToggle)].ToString();
            Settings.FollowApp = FollowApp;
        }

        private bool IsPressed(Key key)
        {
            if (!_pressedKeys.TryGetValue(key, out _))
                _pressedKeys.Add(key, false);
            return _pressedKeys[key];
        }

        private bool FindBaseAddress(IntPtr baseAdd, int i)
        {
            var bytes = new byte[4];
            var address = baseAdd + i;
            foreach (var offset in _xPosition)
            {
                ReadMemory(address, bytes);
                address = (IntPtr)(BitConverter.ToInt32(bytes, 0) + offset);
            }
            var buffer = new byte[4];
            ReadMemory(address, buffer);
            var value = BitConverter.ToSingle(buffer, 0);

            bytes = new byte[4];
            address = baseAdd + i;
            ReadMemory(address, bytes);
            address = (IntPtr)(BitConverter.ToInt32(bytes, 0) + ChatOpenedOffsets[0]);
            buffer = new byte[1];
            ReadMemory(address, buffer);
            var opened = buffer[0] == 1;
            var valid = opened || buffer[0] == 0;
            
            bytes = new byte[4];
            address = baseAdd + i;
            foreach (var offset in new[] { ChatOpenedOffsets[1] })
            {
                ReadMemory(address, bytes);
                address = (IntPtr)(BitConverter.ToInt32(bytes, 0) + offset);
            }
            buffer = new byte[4];
            ReadMemory(address, buffer);
            var idk = BitConverter.ToInt32(buffer, 0) == 841;
            
            if (valid && opened && idk)
            {
                _chatBaseAddress = i;
                if (_baseAddress != 0)
                    return true;
            }
            
            if (value != 0 && value > XCoordinate - 1 && value < XCoordinate + 1)
            {
                BaseAddress = i.ToString("X8");
                if (_chatBaseAddress != 0)
                    return true;
            }

            return false;
        }

        private void WindowMouseDown(object sender, MouseButtonEventArgs args) => WindowDeactivated(sender, args);

        public void BindKeyDown(object sender, KeyEventArgs args)
        {
            if (_currentButton == null) return;
            _binds[_currentButton] = args.Key;
            _currentButtonElement.Content = _binds[_currentButton];
            _currentButton = null;
            _currentButtonElement = null;
            _activityHook.KeyDown -= BindKeyDown;
        }
        
        public void WindowDeactivated(object sender, EventArgs args)
        {
            if (_currentButton == null) return;
            _binds[_currentButton] = Key.None;
            _currentButtonElement.Content = GetKey(_currentButton);
            _currentButton = null;
            _currentButtonElement = null;
            _activityHook.KeyDown -= BindKeyDown;
        }
        
        public void BindClick(Button button)
        {
            var name = button.Name;

            if (_currentButton != null)
            {
                _currentButtonElement.Content = GetKey(_currentButton);
                if (_currentButton == name)
                {
                    _currentButton = null;
                    _currentButtonElement = null;
                    _activityHook.KeyDown -= BindKeyDown;
                    return;
                }
            }

            button.Content = "Press key...";
            _currentButtonElement = button;
            _currentButton = name;
            _activityHook.KeyDown += BindKeyDown;
        }

        private void PreviewTextBoxInput(TextBox sender, TextCompositionEventArgs e)
        {
            Regex regex;
            regex = sender.Text.Contains(".") ? new("^[a-zA-Z-.]+$") : new("^[a-zA-Z]+$");
            e.Handled = regex.IsMatch(e.Text);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => Hooks.CollectionChanged += value;
            remove => Hooks.CollectionChanged -= value;
        }
    }
}