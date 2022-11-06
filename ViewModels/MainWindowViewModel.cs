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
    delegate Task TaskHandler(int millisecondsDelay);
    
    public partial class MainWindowViewModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        //TODO: d3d hook
        private readonly Dispatcher _dispatcher;

        private static TaskHandler _wait = Task.Delay;

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
                        // int id;
                        if (_hookModel != null)
                        {
                            _hookModel.IsPrimary = false;
                            if (FollowPrimary)
                            {
                                OverwriteBytes(_noClip, _noClipEnabled);
                                WriteFloat(GravityOffsets, 0);
                            }

                            // id = _hookModel.Id;
                            // if (FollowPrimary)
                            // {
                            //     _lastSettings.Add(id, ReadSettings(_settingsBaseAddress));
                            //     WriteSettings(ref _hookModel, _nullSettings);
                            // }
                        }

                        _hookModel = value;
                        _handle = _hookModel.Handle;
                        try
                        {
                            _currentModuleAddress = (int) value.Module.BaseAddress;
                        }
                        catch
                        {
                            _hookModel = null;
                            _currentModuleAddress = 0;
                            return;
                        }

                        _currentPlayerBaseAddress = _currentModuleAddress + _playerBaseAddress;
                        // fixed (int* pointer = &_currentBaseAddress)
                        //     ReadProcessMemory(value.Handle, _currentBaseAddress, pointer, 4, out _);
                        _currentSettingsAddress = _currentModuleAddress + _settingsBaseAddress;
                        _currentGameGlobalsBaseAddress = _currentModuleAddress + _gameGlobalsBaseAddress;
                        
                        unsafe
                        {
                            _currentChatStateAddress = _currentModuleAddress + _chatBaseAddress;
                            var buffer = stackalloc byte[4];
                            ReadMemory(_currentChatStateAddress, buffer);
                            _currentChatStateAddress = *(int*) buffer + ChatOpenedOffsets[0];

                            // _currentWorldIdAddress = _currentModuleAddress + _gameGlobalsBaseAddress;
                            // var intBuffer = (int*) buffer;
                            // foreach (var offset in WorldIdStableOffsets)
                            // {
                            //     ReadMemory(_currentWorldIdAddress, buffer);
                            //     _currentWorldIdAddress = *intBuffer + offset;
                            // }
                            //
                            // if (_currentWorldIdAddress != WorldIdStableOffsets[WorldIdStableOffsets.Length - 1])
                            // {
                            //     _currentWorldId = ReadInt(_currentWorldIdAddress);
                            // }
                            // else
                            // {
                            //     _currentWorldIdAddress = 0;
                            // }
                        }
                        
                        _encryptionKey = ReadUInt(StatsEncKeyOffsets);
                        if (_encryptionKey != 0)
                        {
                            var bytes = BitConverter.GetBytes((float) SpeedHackValue);
                            _encryptedSpeed = BitConverter.ToUInt32(bytes, 0) ^ _encryptionKey;
                        }

                        // MapCheck = _hookModel.MapCheck;
                        // ZoomCheck = _hookModel.ZoomCheck;
                        // FovCheck = _hookModel.FovCheck;
                        // ChamsCheck = _hookModel.ChamsCheck;
                        // MiningCheck = _hookModel.MiningCheck;
                        _hookModel.IsPrimary = true;
                        // if (FollowPrimary)
                        // {
                        //     id = _hookModel.Id;
                        //     WriteSettings(ref _hookModel, _lastSettings[id]);
                        //     _lastSettings.Remove(id);
                        // }
                        if (FollowPrimary)
                        {
                            OverwriteBytes(_noClipEnabled, _noClip);
                            WriteFloat(GravityOffsets, Gravity);
                        }

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

        #region Constants

        private const float Gravity = -29;
        
        #endregion
        //private readonly Dictionary<int, (float, float, float)> _lastSettings = new();

        //private readonly (float, float, float) _nullSettings = (0, 0, 0);
        //private readonly byte[] _nullBytes = new byte[12];

        private Settings _settings;
        private readonly UserActivityHook _activityHook = new(false, true);

        private string _currentButton;
        private Button _currentButtonElement;

        private int _playerBaseAddress;
        private int _chatBaseAddress;
        private int _settingsBaseAddress;
        private int _gameGlobalsBaseAddress;
        private int _currentPlayerBaseAddress;
        private int _currentSettingsAddress;
        private int _currentGameGlobalsBaseAddress;
        private int _currentChatStateAddress;
        private int _currentWorldIdAddress;
        private int _currentModuleAddress;

        private int _currentWorldId;

        private float _sprintValue;
        private float _skipValue;
        private float _jumpForceValue;

        private float _followSpeedValue;

        private bool _followApp;
        private bool _sprintCheck;
        private bool _speedCheck;
        private bool _jumpCheck;

        private uint _lastSpeed;
        private int _speedHackValue;
        private uint _encryptedSpeed;

        private uint _encryptionKey;

        private int _xCoordinate;
        private int _worldId;

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

        private bool _followPrimary;
        
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

        public string PlayerBaseAddress
        {
            get => _playerBaseAddress.ToString("X8");

            set
            {
                _playerBaseAddress = int.Parse(value, NumberStyles.HexNumber);
                if (HookModel != null)
                    _currentPlayerBaseAddress = _currentModuleAddress + _playerBaseAddress;

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
        
        public float FollowSpeedValue
        {
            get => _followSpeedValue;
            set
            {
                _followSpeedValue = value;
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
                    _encryptedSpeed = BitConverter.ToUInt32(BitConverter.GetBytes((float)value), 0) ^ _encryptionKey;
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
        
        public long WorldId
        {
            get => _worldId;
            set
            {
                if (value < 0)
                {
                    _worldId = (int) value;
                }
                else unsafe
                {
                    _worldId = *(int*) &value;
                }
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
        
        // public bool FollowPrimary
        // {
        //     get => _followPrimary;
        //     set
        //     {
        //         _followPrimary = value;
        //         if (value)
        //         {
        //             for (int i = 0; i < Hooks.Count; i++)
        //             {
        //                 var hook = Hooks[i];
        //                 if (hook.IsPrimary) continue;
        //
        //                 _lastSettings.Add(hook.Id, ReadSettings(ref hook));
        //                 WriteSettings(ref hook, (0, 0, 0));
        //             }
        //         }
        //         else
        //         {
        //             var newSettings = _lastSettings.Where(x => Hooks.Any(y => x.Key == y.Id))
        //                 .ToDictionary(x => x.Key, y => y.Value);
        //             _lastSettings.Clear();
        //             for (int i = 0; i < Hooks.Count; i++)
        //             {
        //                 var hook = Hooks[i];
        //                 // if (hook.IsPrimary) continue;
        //                 var settings = newSettings[hook.Id];
        //                 
        //                 WriteSettings(ref hook, settings);
        //             }
        //         }
        //         OnPropertyChanged();
        //     }
        // }
        
        public bool FollowPrimary
        {
            get => _followPrimary;
            set
            {
                _followPrimary = value;
                if (value)
                {
                    foreach (var hook in Hooks)
                    {
                        if (hook.IsPrimary) continue;
                        
                        OverwriteBytes(hook, _noClip, _noClipEnabled);
                        WriteFloat(hook.Handle, GravityOffsets, 0);
                    }
                }
                else
                {
                    foreach (var hook in Hooks)
                    {
                        if (hook.IsPrimary) continue;
                        
                        OverwriteBytes(hook, _noClipEnabled, _noClip);
                        WriteFloat(hook.Handle, GravityOffsets, Gravity);
                    }
                }
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
        public ICommand MiningCheckCommand => _miningCheckCommand ??= new(x => InjectCheckChanged(x, _miningSlow, _miningSlowEnabled));
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

        public unsafe MainWindowViewModel()
        {
            _dispatcher = Application.Current.MainWindow.Dispatcher;
            SearchWindowVisibility = Visibility.Hidden;
            MainPageVisibility = Visibility.Visible;
            _dispatcher.ShutdownStarted += (_, _) => CloseWindow();
            _dispatcher.InvokeAsync(LoadSettings);

            _activityHook.KeyDown += OnKeyDown;
            _activityHook.KeyUp += key => _pressedKeys[key] = false;
            //_activityHook.OnMouseActivity += MouseCheck;

            _dispatcher.InvokeAsync(UpdateCurrent);
            _dispatcher.InvokeAsync(FocusUpdate);
            _dispatcher.InvokeAsync(ForceSprint);
            _dispatcher.InvokeAsync(ForceSpeed);
            _dispatcher.InvokeAsync(HooksUpdate);
            _dispatcher.InvokeAsync(FollowUpdate);
        }

        private void HideWindow()
        {
            if (Application.Current.MainWindow != null)
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void CloseWindow()
        {
            if (FollowPrimary)
            {
                foreach (var hook in Hooks)
                {
                    if (hook.IsPrimary) continue;
                    OverwriteBytes(hook, _noClipEnabled, _noClip);
                    WriteFloat(hook.Handle, GravityOffsets, Gravity);
                }
            }
            SaveCurrent();
            _settings.Save();
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
            _dispatcher.InvokeAsync(() =>
            {
                process ??= HookModel.Process;
                if (_antiAfkList.Contains(process.Id)) return;
                _antiAfkList.Add(process.Id);
                var address = FindSignature(_antiAfk, process);
                if (address == 0) return;

                var handle = process.Handle;
                var caveLength = _antiAfkCave.Length + 5;
                var hAlloc = VirtualAllocEx(handle, 0, caveLength, AllocationType.Commit,
                    MemoryProtection.ExecuteRead);

                WriteMemory(handle, hAlloc, AsmJump((ulong) address + 6, (ulong) hAlloc, _antiAfkCave));
                WriteMemory(handle, address, AsmJump((ulong) hAlloc, (ulong) address));
                process.Exited += (_, _) => _antiAfkList.Remove(process.Id);
            });
        }

        private void InjectCheckChanged(ToggleButton checkBox, int[] find, int[] change)
        {
            var isChecked = checkBox.IsChecked ?? false;
            if (isChecked && GameClosed())
            {
                checkBox.IsChecked = false;
                return;
            }
            
            // switch (checkBox.Name)
            // {
            //     case "MapCheck":
            //         HookModel.MapCheck = isChecked;
            //         break;
            //     case "ZoomCheck":
            //         HookModel.ZoomCheck = isChecked;
            //         break;
            //     case "FovCheck":
            //         HookModel.FovCheck = isChecked;
            //         break;
            //     case "ChamsCheck":
            //         HookModel.ChamsCheck = isChecked;
            //         break;
            //     case "MiningCheck":
            //         HookModel.MiningCheck = isChecked;
            //         //OverwriteBytes(isChecked ? _geodeTool : _geodeToolEnabled, isChecked ? _geodeToolEnabled : _geodeTool);
            //         var geodeFrom = isChecked ? _geodeTool : _geodeToolEnabled;
            //         var geodeTo = isChecked ? _geodeToolEnabled : _geodeTool;
            //         foreach (var hook in Hooks)
            //         {
            //             OverwriteBytes(hook, geodeFrom, geodeTo);
            //         }
            //         break;
            //     default:
            //         MessageBox.Show("Non-existing CheckBox : " + checkBox.Name);
            //         CloseWindow();
            //         break;
            // }

            int[] from, to;
            if (checkBox.Name == "MiningCheck")
            {
                from = isChecked ? _geodeTool : _geodeToolEnabled;
                to = isChecked ? _geodeToolEnabled : _geodeTool;
                foreach (var hook in Hooks)
                {
                    OverwriteBytes(hook, from, to);
                }
            }

            from = isChecked ? find : change;
            to = isChecked ? change : find;
            //OverwriteBytes(from, to);
            foreach (var hook in Hooks)
            {
                OverwriteBytes(hook, from, to);
            }
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

            if (MessageBox.Show(
                    "Open chat and Press ok\nDONT close chat till address found\n(dialogue with info will appear)") !=
                MessageBoxResult.OK)
            {
                return;
            }
            _playerBaseAddress = _chatBaseAddress = _settingsBaseAddress = _gameGlobalsBaseAddress = 0;
            
            for (int i = 16_150_000; i < 19_000_000; i++)
            {
                var found = false;
                _dispatcher.Invoke(() => { found = FindBaseAddresses(i); });
                
                if (found)
                    break;
            }

            for (int i = _chatBaseAddress - 4096; i < _chatBaseAddress + 4096; i++)
            {
                if (FindCharacterBaseAddress(i))
                    break;
            }

            SearchWindowVisibility = Visibility.Hidden;
            MessageBox.Show(new StringBuilder()
                .Append("Base address: ").AppendLine(PlayerBaseAddress)
                .Append("Chat address: ").AppendLine(_chatBaseAddress.ToString("X8"))
                .Append("Settings address: ").AppendLine(_settingsBaseAddress.ToString("X8"))
                .Append("Game globals address: ").AppendLine(_gameGlobalsBaseAddress.ToString("X8")).ToString());
        }

        private readonly Type _keyType = typeof(Key);
        private Key ParseKey(string key) => (Key)Enum.Parse(_keyType, key);
        
        private void LoadSettings()
        {
            _settings = Settings.Load();
            
            _binds.Add(nameof(SkipButton), ParseKey(_settings.SkipButton));
            _binds.Add(nameof(SprintButton), ParseKey(_settings.SprintButton));
            _binds.Add(nameof(SprintToggleButton), ParseKey(_settings.SprintToggleButton));
            _binds.Add(nameof(JumpButton), ParseKey(_settings.JumpButton));
            _binds.Add(nameof(JumpToggleButton), ParseKey(_settings.JumpToggleButton));
            _binds.Add(nameof(SpeedHackToggle), ParseKey(_settings.SpeedHackToggle));
            
            SkipButton = GetKey(nameof(SkipButton)).ToString();
            SprintButton = GetKey(nameof(SprintButton)).ToString();
            SprintToggleButton = GetKey(nameof(SprintToggleButton)).ToString();
            JumpButton = GetKey(nameof(JumpButton)).ToString();
            JumpToggleButton = GetKey(nameof(JumpToggleButton)).ToString();
            SpeedHackToggle = GetKey(nameof(SpeedHackToggle)).ToString();

            PlayerBaseAddress = _settings.PlayerBaseAddress;
            _chatBaseAddress = Convert.ToInt32(_settings.ChatBaseAddress, 16);
            _settingsBaseAddress = Convert.ToInt32(_settings.SettingsBaseAddress, 16);
            _gameGlobalsBaseAddress = Convert.ToInt32(_settings.GameGlobalsBaseAddress, 16);
            SprintValue = _settings.SprintValue;
            SkipValue = _settings.SkipValue;
            JumpForceValue = _settings.JumpForceValue;
            SpeedHackValue = _settings.SpeedHackValue;
            FollowSpeedValue = _settings.FollowSpeedValue;
            FollowApp = _settings.FollowApp;
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
                try
                {
                    var copy = Hooks.FirstOrDefault(x => x.Id == process.Id);
                    if (copy != null)
                    {
                        if (copy.Name.Length == 0 && name.Length > 0)
                        {
                            var index = Hooks.IndexOf(copy);
                            hook = new HookModel(copy.Process, name);
                            Hooks[index] = hook;
                        }
                    }
                    else
                    {
                        AddHook(hook);
                        
                        // if (FollowPrimary)
                        // {
                        //     _lastSettings.Add(hook.Id, ReadSettings(ref hook));
                        //     WriteSettings(ref hook, _nullSettings);
                        // }
                    }

                    if (!_antiAfkList.Contains(process.Id))
                        EnableAntiAfk(process);
                }
                catch{}
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
            else if (change && HookModel == null)
            {
                HookModel = Hooks.First();
            }
        }
        
        private void Skip()
        {
            var xposAdd = GetPlayerAddress(XPosition);
            var xviewAdd = GetPlayerAddress(XView);

            WriteFloat(xposAdd, ReadFloat(xviewAdd) * SkipValue + ReadFloat(xposAdd));
            WriteFloat(xposAdd + 4, ReadFloat(xviewAdd + 4) * SkipValue + ReadFloat(xposAdd + 4));
            WriteFloat(xposAdd + 8, ReadFloat(xviewAdd + 8) * SkipValue + ReadFloat(xposAdd + 8));
        }

        private void SuperJump()
        {
            if (!JumpCheck || GameClosed() || NotFocused()) return;
            WriteFloat(YPosition, ReadFloat(YPosition) + JumpForceValue);
        }
        
        private async void ForceSprint()
        {
            while (true)
            {
                await _wait(10);
                while (!SprintCheck || !IsPressed(_binds[nameof(SprintButton)]) || GameClosed() || NotFocused())
                    await _wait(10);
                
                var xviewAdd = GetPlayerAddress(XView);
                var velocityAdd = GetPlayerAddress(XVelocity);
                
                WriteFloat(velocityAdd, ReadFloat(xviewAdd) * SprintValue);
                WriteFloat(velocityAdd + 4, ReadFloat(xviewAdd + 4) * SprintValue);
                WriteFloat(velocityAdd + 8, ReadFloat(xviewAdd + 8) * SprintValue);
            }
        }

        private async void ForceSpeed()
        {
            while (true)
            {
                await _wait(10);
                while (!SpeedCheck || GameClosed() || FollowApp && NotFocused())
                    await _wait(100);
                WriteUInt(SpeedOffsets, _encryptedSpeed);
            }
        }

        private void AddHook(HookModel hook)
        {
            Hooks.Add(hook);
            _dispatcher.InvokeAsync(() =>
            {
                if (MapCheck)
                {
                    OverwriteBytes(hook, _mapHack, _mapHack);
                }

                if (ZoomCheck)
                {
                    OverwriteBytes(hook, _zoomHack, _zoomHackEnabled);
                }

                if (FovCheck)
                {
                    OverwriteBytes(hook, _fovHack, _fovHackEnabled);
                }

                if (ChamsCheck)
                {
                    OverwriteBytes(hook, _chamsMonsters, _chamsMonstersEnabled);
                }

                if (MiningCheck)
                {
                    OverwriteBytes(hook, _geodeTool, _geodeToolEnabled);
                    OverwriteBytes(hook, _miningSlow, _miningSlowEnabled);
                }
            });
        }
        
        private async void FocusUpdate()
        {
            while (true)
            {
                while (!FollowApp)
                {
                    await _wait(50);
                }

                var handle = GetForegroundWindow();
                GetWindowThreadProcessId(handle, out var procId);
                
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
                    HookModel = null;
                }

                await _wait(50);
            }
        }

        private async void HooksUpdate()
        {
            while (true)
            {
                await _wait(750); // 60000
                var processList = Process.GetProcessesByName("Trove");
                foreach (var process in processList)
                {
                    if (!_antiAfkList.Contains(process.Id))
                        EnableAntiAfk(process);
                    
                    var hook = Hooks.FirstOrDefault(x => x.Id == process.Id);
                    if (hook == null)
                    {
                        var name = GetName(process.Handle);
                        hook = new HookModel(process, name);
                        AddHook(hook);
                    }
                    else if (hook.Name.Length == 0)
                    {
                        var name = GetName(process.Handle);
                        if (name != null)
                        {
                            var index = Hooks.IndexOf(hook);
                            hook = new HookModel(hook.Process, name);
                            Hooks[index] = hook;
                        }
                    }
                }

                foreach (var hook in Hooks)
                {
                    if (hook.IsPrimary) continue;

                    var address = GetAddress(hook.Handle, hook.ModuleAddress + _gameGlobalsBaseAddress, WorldIdStableOffsets);
                    unsafe
                    {
                        fixed (byte* pointer = GetBuffer(hook.Handle, address))
                            hook.WorldId = *(int*) pointer;
                    }
                }

                _currentWorldId = ReadInt(GetAddress(_currentGameGlobalsBaseAddress, WorldIdStableOffsets));
                // if (_currentWorldIdAddress == 0)
                // {
                //     var address = GetAddress(_currentGameGlobalsBaseAddress, WorldIdStableOffsets);
                //     if (address != WorldIdStableOffsets[WorldIdStableOffsets.Length - 1])
                //     {
                //         _currentWorldIdAddress = address;
                //         _currentWorldId = ReadInt(address);
                //     }
                // }
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
            return (HookModel?.Id ?? 0) != procId;
        }

        private unsafe bool ChatOpened()
        {
            if (GameClosed() || _chatBaseAddress == 0) return true;
            var @byte = stackalloc byte[1];
            ReadProcessMemory(_handle, _currentChatStateAddress, @byte, 1, out _);
            return *@byte != 0;
        }

        private void OnKeyDown(Key key)
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
                    var speed = ReadUInt(SpeedOffsets);
                    if (SpeedCheck)
                        _lastSpeed = speed;
                    else
                        WriteUInt(SpeedOffsets, _lastSpeed);
                }
            }

            _pressedKeys[key] = true;
        }

        private async void UpdateCurrent()
        {
            while (true)
            {
                await _wait(100);

                if (!FollowApp)
                {
                    if (HookModel != null && HookModel.HasExited)
                    {
                        if (HookModel != null)
                            Hooks.Remove(HookModel);
                        HookModel = null;
                    }
                    while (HookModel == null)
                    {
                        if (Hooks.Count > 0)
                        {
                            HookModel = Hooks.First();
                            break;
                        }
                        
                        await _wait(750);
                        RefreshHooks(true);

                        await _wait(750);
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
                        var hook = new HookModel(copy.Process, name);
                        Hooks[index] = hook;
                        HookModel = hook;
                    }
                }
            }
        }

        private async void FollowUpdate()
        {
            while (true)
            {
                await _wait(1);
                if (!FollowPrimary)
                {
                    await _wait(50);
                    continue;
                }

                var xVelAdd = GetPlayerAddress(XVelocity);
                var xVelocity = ReadFloat(xVelAdd);
                var yVelocity = ReadFloat(xVelAdd + 4);
                var zVelocity = ReadFloat(xVelAdd + 8); 
                var length = (float) Math.Sqrt(xVelocity * xVelocity + yVelocity * yVelocity + zVelocity * zVelocity) / 1.5f;
                if (length == 0)
                    length = 1;
                
                var xPosAdd = GetPlayerAddress(XPosition);
                var sourceX = ReadFloat(xPosAdd) + xVelocity / length;
                var sourceY = ReadFloat(xPosAdd + 4) + yVelocity / length;
                var sourceZ = ReadFloat(xPosAdd + 8) + zVelocity / length;
                
                foreach (var hook in Hooks)
                {
                    if (hook.IsPrimary || _currentWorldId != hook.WorldId) continue;

                    var handle = hook.Handle;
                    xPosAdd = GetPlayerAddress(handle, XPosition);
                    var xDiff = sourceX - ReadFloat(handle, xPosAdd);
                    var yDiff = sourceY - ReadFloat(handle, xPosAdd + 4);
                    var zDiff = sourceZ - ReadFloat(handle, xPosAdd + 8);

                    xVelAdd = GetPlayerAddress(handle, XVelocity);
                    length = (float) Math.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff);
                    if (Math.Abs(xDiff) < 1 && Math.Abs(yDiff) < 1 && Math.Abs(zDiff) < 1)
                    {
                        length *= 5;
                    }
                    length /= FollowSpeedValue;

                    WriteFloat(handle, xVelAdd, xDiff / length);
                    WriteFloat(handle, xVelAdd + 4, yDiff / length);
                    WriteFloat(handle, xVelAdd + 8, zDiff / length);
                }
            }
        }

        private unsafe string GetName(IntPtr handle)
        {
            var buffer = new byte[28];
            if (handle == IntPtr.Zero)
                ReadMemory(GetPlayerAddress(NameOffests), buffer);
            else
                ReadMemory(handle, GetPlayerAddress(handle, NameOffests), buffer);

            fixed (byte* p = buffer)
            {
                for (byte i = 0; i < buffer.Length; i++)
                {
                    if (*(p + i) != '\0') continue;
                    if (i == 0)
                        break;
                    
                    return Encoding.ASCII.GetString(buffer).Substring(0, i);
                }
            
                return string.Empty;
            }
        }

        private unsafe string GetPowerRank()
        {
            fixed (byte* p = BitConverter.GetBytes(ReadUInt(PowerRankOffsets) ^ _encryptionKey))
            {
                return (*(int*)p).ToString();
            }
        }
        
        public void SaveCurrent()
        {
            _settings.PlayerBaseAddress = PlayerBaseAddress;
            _settings.ChatBaseAddress = _chatBaseAddress.ToString("X8");
            _settings.SettingsBaseAddress = _settingsBaseAddress.ToString("X8");
            _settings.GameGlobalsBaseAddress = _gameGlobalsBaseAddress.ToString("X8");
            _settings.SkipValue = SkipValue;
            _settings.SprintValue = SprintValue;
            _settings.JumpForceValue = JumpForceValue;
            _settings.SpeedHackValue = SpeedHackValue;
            _settings.FollowSpeedValue = FollowSpeedValue;
            _settings.SkipButton = _binds[nameof(SkipButton)].ToString();
            _settings.SprintButton = _binds[nameof(SprintButton)].ToString();
            _settings.SprintToggleButton = _binds[nameof(SprintToggleButton)].ToString();
            _settings.JumpButton = _binds[nameof(JumpButton)].ToString();
            _settings.JumpToggleButton = _binds[nameof(JumpToggleButton)].ToString();
            _settings.SpeedHackToggle = _binds[nameof(SpeedHackToggle)].ToString();
            _settings.FollowApp = FollowApp;
        }

        private bool IsPressed(Key key)
        {
            if (!_pressedKeys.TryGetValue(key, out _))
                _pressedKeys.Add(key, false);
            return _pressedKeys[key];
        }

        private unsafe (float, float, float) ReadSettings(int baseAddress)
        {
            var buffer = stackalloc byte[4];
            var intBuffer = (int*) buffer;
            var floatBuffer = (float*) buffer;
            ReadMemory(baseAddress, buffer);
            baseAddress = *intBuffer;
            
            var address = baseAddress + IdkObject[0];
            ReadMemory(address, buffer);
            var idkObject = *floatBuffer;
            
            if (idkObject == 150)
            {
                address = baseAddress + DrawDistance[0];
                ReadMemory(address, buffer);
                var drawDistance = *floatBuffer;
                
                if (drawDistance >= 32 && drawDistance <= 210)
                {
                    address = baseAddress + HalfDrawDistance[0];
                    ReadMemory(address, buffer);
                    var halfDrawDistance = *floatBuffer;
                    
                    if (halfDrawDistance == Math.Min(96, drawDistance / 2))
                    {
                        return (drawDistance, halfDrawDistance, idkObject);
                    }
                }

            }

            return (0, 0, 0);
        }

        private unsafe (float, float, float) ReadSettings(ref HookModel hook, int baseAddress)
        {
            var handle = hook.Handle;
            var buffer = stackalloc byte[4];
            var intBuffer = (int*) buffer;
            var floatBuffer = (float*) buffer;
            ReadMemory(handle, baseAddress, buffer);
            var address = *intBuffer + IdkObject[0];
            ReadMemory(handle, address, buffer);
            var idkObject = *floatBuffer;
            if (idkObject == 150)
            {
                ReadMemory(handle, baseAddress, buffer);
                address = *intBuffer + DrawDistance[0];
                ReadMemory(handle, address, buffer);
                var drawDistance = *floatBuffer;

                if (drawDistance >= 32 && drawDistance <= 210)
                {
                    ReadMemory(handle, baseAddress, buffer);
                    address = *intBuffer + DrawDistance[0];
                    ReadMemory(handle, address, buffer);
                    var halfDrawDistance = *floatBuffer;

                    if (halfDrawDistance == Math.Min(96, drawDistance / 2))
                    {
                        return (drawDistance, halfDrawDistance, idkObject);
                    }
                }
            }

            return (0, 0, 0);
        }

        private (float, float, float) ReadSettings(ref HookModel hook) => ReadSettings(ref hook, hook.ModuleAddress + _settingsBaseAddress);

        private void WriteSettings(ref HookModel hook, (float, float, float) settings)
        {
            var handle = hook.Handle;
            var halfDrawDistanceAdd = GetPlayerAddress(handle, HalfDrawDistance);
            WriteFloat(handle, halfDrawDistanceAdd, settings.Item2);
            WriteFloat(handle, halfDrawDistanceAdd + 4, settings.Item3);
            WriteFloat(handle, halfDrawDistanceAdd + 0x24, settings.Item1);
        }

        private unsafe bool FindBaseAddresses(int i)
        {
            var buffer = stackalloc byte[4];
            var intBuffer = (int*) buffer;
            var address = _currentModuleAddress + i;
            ReadMemory(address, buffer);
            var source = *intBuffer;
            
            if (_settingsBaseAddress == 0)
            {
                var settings = ReadSettings(address);
                if (settings.Item1 != 0)
                {
                    _settingsBaseAddress = i;
                    return _chatBaseAddress != 0 && _gameGlobalsBaseAddress != 0;
                }
            }
            
            if (_chatBaseAddress == 0)
            {
                address = source + ChatOpenedOffsets[0];
                ReadMemory(address, buffer);
                var opened = *buffer == 1;
                var valid = opened || *buffer == 0;

                if (valid && opened)
                {
                    address = source + ChatOpenedOffsets[1];
                    ReadMemory(address, buffer);
                    if (*intBuffer == 841)
                    {
                        _chatBaseAddress = i;
                        return _settingsBaseAddress != 0 && _gameGlobalsBaseAddress != 0;
                    }
                }
            }

            if (_gameGlobalsBaseAddress == 0)
            {
                *intBuffer = source;
                foreach (var offset in WorldIdStableOffsets)
                {
                    address = *intBuffer + offset;
                    ReadMemory(address, buffer);
                }

                if (i == 0x114AC7C)
                    MessageBox.Show((*intBuffer) + "\n" + ReadInt(GetAddress(_currentModuleAddress + 0x114AC7C, WorldIdStableOffsets)));
                
                if (_worldId == *intBuffer)
                {
                    _gameGlobalsBaseAddress = i;
                    return _chatBaseAddress != 0 && _settingsBaseAddress != 0;
                }
            }

            return false;
        }

        private unsafe bool FindCharacterBaseAddress(int i)
        {
            if (_playerBaseAddress != 0) return false;
            
            var buffer = stackalloc byte[4];
            var intBuffer = (int*) buffer;
            var address = _currentModuleAddress + i;
            ReadMemory(address, buffer);
                    
            foreach (var offset in XPosition)
            {
                address = *intBuffer + offset;
                ReadMemory(address, buffer);
            }

            var value = *(float*) buffer;
            
            if (value > XCoordinate - 1 && value < XCoordinate + 1)
            {
                PlayerBaseAddress = i.ToString("X8");
                return true;
            }

            return false;
        }

        private void WindowMouseDown(object sender, EventArgs args) => WindowDeactivated(sender, args);

        public void BindKeyDown(Key key)
        {
            if (_currentButton == null) return;
            _binds[_currentButton] = key;
            _currentButtonElement.Content = _binds[_currentButton];
            _currentButton = null;
            _currentButtonElement = null;
            _activityHook.KeyDown -= BindKeyDown;
        }
        
        public void WindowDeactivated(object _, EventArgs __)
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
            Regex regex = sender.Text.Contains(".") ? new("^[a-zA-Z-.]+$") : new("^[a-zA-Z]+$");
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