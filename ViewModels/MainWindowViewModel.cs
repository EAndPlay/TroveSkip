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
        public static MainWindowViewModel Instance { get; set; }

        //TODO: d3d hook
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
                        // int id;
                        if (_hookModel != null)
                        {
                            _hookModel.IsPrimary = false;
                            if (FollowBotsToggle && _botsNoClipCheck)
                            {
                                //OverwriteBytes(_handle, _hookModel.NoClipAddress, _noClipEnabled);
                                //_hookModel.NoClipEnabled = true;
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

                        _currentPlayerAddress = _currentModuleAddress + _localPlayerPointer;
                        // fixed (int* pointer = &_currentBaseAddress)
                        //     ReadProcessMemory(value.Handle, _currentBaseAddress, pointer, 4, out _);
                        _currentSettingsAddress = _currentModuleAddress + _settingsPointer;
                        _currentGameGlobalsAddress = _currentModuleAddress + _gameGlobalsPointer;
                        
                        unsafe
                        {
                            _currentChatStateAddress = _currentModuleAddress + _chatPointer;
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
                        if (FollowBotsToggle && !_botsNoClipCheck && _botsFollowType == 0)
                        {
                            //OverwriteBytes(_hookModel, _noClipEnabled, _noClip);
                            // _hookModel.NoClipEnabled = false;
                            OverwriteBytes(_noClipEnabled, _noClip);
                            WriteFloat(GravityOffsets, Gravity);
                        }

                        //EnableAntiAfk();
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
        private readonly byte[] _validChars;

        #region Constants

        private const float Gravity = -29;

        //private const float BotsNoClipTriggerDistance = 2;
        
        #endregion
        //private readonly Dictionary<int, (float, float, float)> _lastSettings = new();

        //private readonly (float, float, float) _nullSettings = (0, 0, 0);
        //private readonly byte[] _nullBytes = new byte[12];

        private Settings _settings;
        private readonly UserActivityHook _activityHook = new(false, true);

        private string _currentButton;
        private Button _currentButtonElement;

        private int _localPlayerPointer;
        private int _chatPointer;
        private int _settingsPointer;
        private int _gameGlobalsPointer;
        private int _playersInWorldPointer;
        private int _currentPlayerAddress;
        private int _currentSettingsAddress;
        private int _currentGameGlobalsAddress;
        private int _currentChatStateAddress;
        private int _currentModuleAddress;

        private int _currentWorldId;

        private BotsSettings _botsSettings;
        private float _botsStopDistance;
        private byte _botsStopType;
        private float _botsStopPower; // with StopType == 1 (Slow)
        private byte _botsWarnStatus;
        private uint _botsWarnDistance;
        private byte _botsFollowType;
        private string _botsFollowTargetName;
        private int _botsFollowTargetNameLength;

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
        private string _botsNoClipToggle;

        private bool _mapCheck;
        private bool _zoomCheck;
        private bool _fovCheck;
        private bool _chamsCheck;
        private bool _miningCheck;

        private bool _followBotsToggle;
        private bool _botsNoClipCheck;

        private Visibility _searchWindowVisibility;
        private Visibility _mainPageVisibility;
        private Visibility _settingsPageVisibility;
        private Visibility _botsSettingsPageVisibility;

        private DelegateCommand<CheckBox> _mapCheckCommand;
        private DelegateCommand<CheckBox> _zoomCheckCommand;
        private DelegateCommand<CheckBox> _fovCheckCommand;
        private DelegateCommand<CheckBox> _chamsCheckCommand;
        private DelegateCommand<CheckBox> _miningCheckCommand;
        private DelegateCommand<Button> _bindClickCommand;
        private DelegateCommand<Button> _switchPageCommand;
        private DelegateCommand<string> _botsStopTypeSwitchCommand;
        private DelegateCommand<string> _botsWarnStatusSwitchCommand;
        private DelegateCommand<string> _botsFollowTypeSwitchCommand;
        private DelegateCommand _invokeSearchWindowCommand;
        private DelegateCommand _findAddressCommand;
        private DelegateCommand _hideWindowCommand;
        private DelegateCommand _closeWindowCommand;
        // private DelegateCommand _clickComboBox;

        public string PlayerBaseAddress
        {
            get => _localPlayerPointer.ToString("X8");

            set
            {
                _localPlayerPointer = int.Parse(value, NumberStyles.HexNumber);
                if (HookModel != null)
                    _currentPlayerAddress = _currentModuleAddress + _localPlayerPointer;

                OnPropertyChanged();
            }
        }

        // public int LocalPlayerPointer
        // {
        //     get => _localPlayerPointer;
        //     set
        //     {
        //         _localPlayerPointer = value;
        //         
        //         OnPropertyChanged();
        //     }
        // }
        
        public int GameGlobalsPointer
        {
            get => _gameGlobalsPointer;
            set
            {
                _gameGlobalsPointer = value;
                OnPropertyChanged();
            }
        }
        public int PlayersInWorldPointer
        {
            get => _playersInWorldPointer;
            set
            {
                _playersInWorldPointer = value;
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
                if (value == 0)
                    value = 1;
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

        public string BotsNoClipToggle
        {
            get => _botsNoClipToggle;
            set
            {
                _botsNoClipToggle = value;
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
        
        public bool FollowBotsToggle
        {
            get => _followBotsToggle;
            set
            {
                _followBotsToggle = value;
                IntPtr handle;
                if (value)
                {
                    foreach (var hook in Hooks)
                    {
                        if (hook.IsPrimary) continue;

                        handle = hook.Handle;
                        //OverwriteBytes(handle, hook.NoClipAddress, _noClipEnabled);
                        if (_botsNoClipCheck)
                            OverwriteBytes(hook, _noClip, _noClipEnabled);
                        WriteFloat(handle, GravityOffsets, 0);
                    }
                }
                else
                {
                    foreach (var hook in Hooks)
                    {
                        if (hook.IsPrimary) continue;

                        handle = hook.Handle;
                        //OverwriteBytes(handle, hook.NoClipAddress, _noClip);
                        if (_botsNoClipCheck)
                            OverwriteBytes(hook, _noClipEnabled, _noClip);
                        WriteFloat(handle, GravityOffsets, Gravity);
                    }
                }
                
                OnPropertyChanged();
            }
        }

        public bool BotsNoClipCheck
        {
            get => _botsNoClipCheck;
            set
            {
                _botsNoClipCheck = value;
                if (!FollowBotsToggle) return;
                if (value)
                {
                    foreach (var hook in Hooks)
                    {
                        if (hook.IsPrimary && _botsFollowType == 0) continue;

                        //OverwriteBytes(handle, hook.NoClipAddress, _noClipEnabled);
                        OverwriteBytes(hook, _noClip, _noClipEnabled);
                    }
                }
                else
                {
                    foreach (var hook in Hooks)
                    {
                        //if (hook.IsPrimary) continue;

                        //OverwriteBytes(handle, hook.NoClipAddress, _noClipEnabled);
                        OverwriteBytes(hook, _noClipEnabled, _noClip);
                    }
                }
            }
        }
        
        //public BotsSettings BotsSettings { get; private set; }

        public float BotsStopDistance
        {
            get => _botsStopDistance;
            set
            {
                _botsStopDistance = value;
                OnPropertyChanged();
            }
        }

        public byte BotsStopType
        {
            get => _botsStopType;
            set
            {
                _botsStopType = value;
                OnPropertyChanged();
            }
        }
        
        #region Trash
        //TODO: Rewrite

        private bool _fullStopState;
        private bool _slowStopState;

        public bool FullStopState
        {
            get => _fullStopState;
            set
            {
                _fullStopState = value;
                OnPropertyChanged();
            }
        }
        
        public bool SlowStopState
        {
            get => _slowStopState;
            set
            {
                _slowStopState = value;
                OnPropertyChanged();
            }
        }

        private bool _botsWarnOn;
        private bool _botsWarnOff;
        
        public bool BotsWarnOn
        {
            get => _botsWarnOn;
            set
            {
                _botsWarnOn = value;
                OnPropertyChanged();
            }
        }
        
        public bool BotsWarnOff
        {
            get => _botsWarnOff;
            set
            {
                _botsWarnOff = value;
                OnPropertyChanged();
            }
        }
        
        private bool _botsFollowLocal;
        private bool _botsFollowTarget;
        
        public bool BotsFollowLocal
        {
            get => _botsFollowLocal;
            set
            {
                _botsFollowLocal = value;
                OnPropertyChanged();
            }
        }
        
        public bool BotsFollowTarget
        {
            get => _botsFollowTarget;
            set
            {
                _botsFollowTarget = value;
                OnPropertyChanged();
            }
        }
        
        #endregion
        
        public float BotsStopPower
        {
            get => _botsStopPower;
            set
            {
                if (value == 0)
                    value = 1;
                _botsStopPower = value;
                OnPropertyChanged();
            }
        }
        
        public byte BotsWarnStatus
        {
            get => _botsWarnStatus;
            set
            {
                _botsWarnStatus = value;
                OnPropertyChanged();
            }
        }
        
        public uint BotsWarnDistance
        {
            get => _botsWarnDistance;
            set
            {
                _botsWarnDistance = value;
                OnPropertyChanged();
            }
        }
        
        public byte BotsFollowType
        {
            get => _botsFollowType;
            set
            {
                _botsFollowType = value;
                OnPropertyChanged();
            }
        }
        
        public string BotsFollowTargetName
        {
            get => _botsFollowTargetName;
            set
            {
                _botsFollowTargetName = value;
                _botsFollowTargetNameLength = value.Length;
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
                BotsSettingsPageVisibility = value == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                OnPropertyChanged();
            }
        }
        
        public Visibility BotsSettingsPageVisibility
        {
            get => _botsSettingsPageVisibility;
            set
            {
                _botsSettingsPageVisibility = value;
                //MainPageVisibility = value == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                OnPropertyChanged();
            }
        }

        public ICommand MapCheckCommand => _mapCheckCommand ??= new(x => InjectCheckChanged(x, _mapHack, _mapHackEnabled));
        public ICommand ZoomCheckCommand => _zoomCheckCommand ??= new(x => InjectCheckChanged(x, _zoomHack, _zoomHackEnabled));
        public ICommand FovCheckCommand => _fovCheckCommand ??= new(x => InjectCheckChanged(x, _fovHack, _fovHackEnabled));
        public ICommand ChamsCheckCommand => _chamsCheckCommand ??= new(x => InjectCheckChanged(x, _chamsMonsters, _chamsMonstersEnabled));
        public ICommand MiningCheckCommand => _miningCheckCommand ??= new(x => InjectCheckChanged(x, _miningSlow, _miningSlowEnabled));
        public ICommand BindClickCommand => _bindClickCommand ??= new(BindClick);
        public ICommand SwitchPageCommand => _switchPageCommand ??= new(SwitchPage);
        public ICommand BotsStopTypeSwitchCommand => _botsStopTypeSwitchCommand ??= new(x => SwitchStopType(byte.Parse(x)));
        public ICommand BotsWarnStatusSwitchCommand => _botsWarnStatusSwitchCommand ??= new(x => SwitchWarnStatus(byte.Parse(x)));
        public ICommand BotsFollowTypeSwitchCommand => _botsFollowTypeSwitchCommand ??= new(x => SwitchFollowType(byte.Parse(x)));
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
            Instance = this;
            _dispatcher = Application.Current.MainWindow.Dispatcher;
            SearchWindowVisibility = Visibility.Hidden;
            MainPageVisibility = Visibility.Visible;
            BotsSettingsPageVisibility = Visibility.Hidden;
            _dispatcher.ShutdownStarted += (_, _) => CloseWindow();
            _dispatcher.InvokeAsync(LoadSettings);

            _activityHook.KeyDown += OnKeyDown;
            _activityHook.KeyUp += key => _pressedKeys[key] = false;
            var charsList = new List<byte>();
            {
                for (var i = (byte) 'a'; i <= 'z'; i++)
                {
                    charsList.Add(i);
                }
                for (var i = (byte) 'A'; i <= 'Z'; i++)
                {
                    charsList.Add(i);
                }
                charsList.Add(95);
                _validChars = charsList.ToArray(); 
            }
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
            if (FollowBotsToggle)
            {
                IntPtr handle;
                foreach (var hook in Hooks)
                {
                    if (hook.IsPrimary) continue;

                    handle = hook.Handle;
                    if (_botsNoClipCheck)
                        OverwriteBytes(hook, _noClipEnabled, _noClip);
                    WriteFloat(handle, GravityOffsets, Gravity);
                }
            }
            SaveSettings();
            Environment.Exit(0);
        }

        private void SwitchPage(ContentControl button)
        {
            switch (button.Content)
            {
                case "SETS":
                    MainPageVisibility = Visibility.Hidden;
                    button.Content = "BOTS";
                    break;
                case "BOTS":
                    SettingsPageVisibility = Visibility.Hidden;
                    button.Content = "MAIN";
                    break;
                default:
                    MainPageVisibility = Visibility.Visible;
                    button.Content = "SETS";
                    BotsSettingsPageVisibility = Visibility.Hidden;
                    break;
            }
            // MainPageVisibility = MainPageVisibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            // var visible = MainPageVisibility == Visibility.Visible;
            // button.Content = visible ? "SETS" : "MAIN";
            // if (visible)
            //     SearchWindowVisibility = Visibility.Hidden;
        }

        //TODO: REWRITE!!!
        private void SwitchStopType(byte type)
        {
            BotsStopType = type;
            FullStopState = type == 0;
            SlowStopState = type == 1;
        }
        
        private void SwitchWarnStatus(byte type)
        {
            BotsWarnStatus = type;
            BotsWarnOff = type == 0;
            BotsWarnOn = type == 1;
        }

        private void SwitchFollowType(byte type)
        {
            BotsFollowType = type;
            BotsFollowLocal = type == 0;
            BotsFollowTarget = type == 1;
        }
        
        //----------------------------

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
                    "Open chat and Press ok\nDONT close chat till address found\n(dialogue with info will appear)",
                    "Before Scan",
                    MessageBoxButton.OKCancel) !=
                MessageBoxResult.OK)
            {
                return;
            }
            _localPlayerPointer = _chatPointer = _settingsPointer = _gameGlobalsPointer = 0;
            
            for (int i = 16_150_000; i < 19_000_000; i++)
            {
                var found = false;
                _dispatcher.Invoke(() => { found = FindBaseAddresses(i); });
                
                if (found)
                    break;
            }

            for (int i = _chatPointer - 2048; i < _chatPointer + 2048; i++)
            {
                if (FindCharacterBaseAddress(i))
                    break;
            }

            foreach (var hook in Hooks)
            {
                hook.ResetAddreses();
            }
            
            SearchWindowVisibility = Visibility.Hidden;
            MessageBox.Show(new StringBuilder()
                .Append("Player address: ").AppendLine(PlayerBaseAddress)
                .Append("Chat address: ").AppendLine(_chatPointer.ToString("X8"))
                .Append("Settings address: ").AppendLine(_settingsPointer.ToString("X8"))
                .Append("Game Globals address: ").AppendLine(_gameGlobalsPointer.ToString("X8")).ToString());
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

            PlayerBaseAddress = _settings.LocalPlayerPointer;
            _chatPointer = Convert.ToInt32(_settings.ChatPointer, 16);
            _settingsPointer = Convert.ToInt32(_settings.SettingsPointer, 16);
            _gameGlobalsPointer = Convert.ToInt32(_settings.GameGlobalsPointer, 16);
            _playersInWorldPointer = Convert.ToInt32(_settings.PlayersInWorldPointer, 16);
            SprintValue = _settings.SprintValue;
            SkipValue = _settings.SkipValue;
            JumpForceValue = _settings.JumpForceValue;
            SpeedHackValue = _settings.SpeedHackValue;
            FollowSpeedValue = _settings.FollowSpeedValue;
            FollowApp = _settings.FollowApp;

            _botsSettings = _settings.BotsSettings;
            _binds.Add(nameof(BotsNoClipToggle), ParseKey(_botsSettings.NoClipToggle));
            BotsNoClipToggle = GetKey(nameof(BotsNoClipToggle)).ToString();
            BotsStopDistance = _botsSettings.StopDistance;
            //BotsStopType = _botsSettings.StopType;
            SwitchStopType(_botsSettings.StopType);
            BotsStopPower = _botsSettings.StopPower;
            //BotsNotifyState = _botsSettings.NotifyState;
            SwitchWarnStatus(_botsSettings.WarnStatus);
            BotsWarnDistance = _botsSettings.WarnDistance;
            SwitchFollowType(_botsSettings.FollowType);
            BotsFollowTargetName = _botsSettings.FollowTargetName;
            BotsNoClipCheck = _botsSettings.NoClip;
        }
        
        private void SaveSettings()
        {
            _settings.LocalPlayerPointer = PlayerBaseAddress;
            _settings.ChatPointer = _chatPointer.ToString("X8");
            _settings.SettingsPointer = _settingsPointer.ToString("X8");
            _settings.GameGlobalsPointer = _gameGlobalsPointer.ToString("X8");
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
            _botsSettings.StopDistance = BotsStopDistance;
            _botsSettings.StopType = BotsStopType;
            _botsSettings.StopPower = BotsStopPower;
            _botsSettings.NoClipToggle = _binds[nameof(BotsNoClipToggle)].ToString();
            _botsSettings.WarnStatus = BotsWarnStatus;
            _botsSettings.WarnDistance = BotsWarnDistance;
            _botsSettings.FollowType = BotsFollowType;
            _botsSettings.FollowTargetName = BotsFollowTargetName;
            _botsSettings.NoClip = BotsNoClipCheck;
            
            _settings.Save();
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
                try
                {
                    var copy = Hooks.FirstOrDefault(x => x.Id == process.Id);
                    HookModel hook;
                    if (copy != null)
                    {
                        if (copy.Name.Length == 0 && name.Length > 0)
                        {
                            var index = Hooks.IndexOf(copy);
                            hook = new HookModel(copy.Process, name);
                            //hook.NetworkPlayersAddress = hook.ModuleAddress + _playersInWorldPointer;
                            Hooks[index] = hook;
                        }
                    }
                    else
                    {
                        hook = new HookModel(process, name);
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
                catch {}
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
            var xposAdd = GetAddressFromLocalPlayer(LocalXPosition);
            var xviewAdd = GetAddressFromLocalPlayer(XView);

            WriteFloat(xposAdd, ReadFloat(xviewAdd) * SkipValue + ReadFloat(xposAdd));
            WriteFloat(xposAdd + 4, ReadFloat(xviewAdd + 4) * SkipValue + ReadFloat(xposAdd + 4));
            WriteFloat(xposAdd + 8, ReadFloat(xviewAdd + 8) * SkipValue + ReadFloat(xposAdd + 8));
        }

        private void SuperJump()
        {
            if (!_jumpCheck || GameClosed() || NotFocused()) return;
            WriteFloat(LocalYPosition, ReadFloat(LocalYPosition) + JumpForceValue);
        }
        
        private async void ForceSprint()
        {
            while (true)
            {
                await Task.Delay(10);
                while (!_sprintCheck || !IsPressed(_binds[nameof(SprintButton)]) || GameClosed() || NotFocused())
                    await Task.Delay(10);
                
                var xviewAdd = GetAddressFromLocalPlayer(XView);
                var velocityAdd = GetAddressFromLocalPlayer(LocalXVelocity);
                
                WriteFloat(velocityAdd, ReadFloat(xviewAdd) * SprintValue);
                WriteFloat(velocityAdd + 4, ReadFloat(xviewAdd + 4) * SprintValue);
                WriteFloat(velocityAdd + 8, ReadFloat(xviewAdd + 8) * SprintValue);
            }
        }

        private async void ForceSpeed()
        {
            while (true)
            {
                await Task.Delay(10);
                while (!_speedCheck || GameClosed() || FollowApp && NotFocused())
                    await Task.Delay(100);
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

            hook.Process.Exited += (_, _) => Hooks.Remove(Hooks.First(x => x.Id == hook.Id));
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

                await Task.Delay(50);
            }
        }

        private async void HooksUpdate()
        {
            // Process[] allProcesses;
            // Process indexedProcess;
            Process[]/*List<Process>*/ processList;
            HookModel hook;
            while (true)
            {
                await Task.Delay(750); // 60000
                processList = Process.GetProcessesByName("Trove");

                // allProcesses = Process.GetProcesses(".");
                // for (short i = 0; i < allProcesses.Length; ++i)
                // {
                //     indexedProcess = allProcesses[i];
                //     if (indexedProcess.ProcessName == "Trove")
                //         processList.Add(indexedProcess);
                //     else
                //         indexedProcess.Dispose();
                // }
                
                foreach (var process in processList)
                {
                    if (!_antiAfkList.Contains(process.Id))
                        EnableAntiAfk(process);
                    
                    hook = Hooks.FirstOrDefault(x => x.Id == process.Id);
                    if (hook == null)
                    {
                        var name = GetName(process.Handle);
                        hook = new HookModel(process, name);
                        if (hook.ModuleAddress == 0)
                            continue;
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

                for (byte i = 0; i < Hooks.Count; i++)
                {
                    hook = Hooks[i];
                    if (hook.IsPrimary) continue;

                    var address = GetAddress(hook.Handle, hook.ModuleAddress + _gameGlobalsPointer, WorldIdStableOffsets);
                    unsafe
                    {
                        fixed (byte* pointer = GetBuffer(hook.Handle, address))
                            hook.WorldId = *(int*) pointer;
                    }
                }
                // foreach (var hook in Hooks)
                // {
                //     if (hook.IsPrimary) continue;
                //
                //     var address = GetAddress(hook.Handle, hook.ModuleAddress + _gameGlobalsBaseAddress, WorldIdStableOffsets);
                //     unsafe
                //     {
                //         fixed (byte* pointer = GetBuffer(hook.Handle, address))
                //             hook.WorldId = *(int*) pointer;
                //     }
                // }

                _currentWorldId = ReadInt(GetAddress(_currentGameGlobalsAddress, WorldIdStableOffsets));
                _hookModel.WorldId = _currentWorldId;
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
            if (GameClosed()) return true;
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

                else if (key == _binds[nameof(SprintToggleButton)])
                    SprintCheck = !SprintCheck;

                else if (key == _binds[nameof(JumpToggleButton)])
                    JumpCheck = !JumpCheck;

                else if (key == _binds[nameof(SpeedHackToggle)])
                {
                    SpeedCheck = !SpeedCheck;
                    var speed = ReadUInt(SpeedOffsets);
                    if (SpeedCheck)
                        _lastSpeed = speed;
                    else
                        WriteUInt(SpeedOffsets, _lastSpeed);
                }
                
                else if (key == _binds[nameof(BotsNoClipToggle)] && FollowBotsToggle)
                    BotsNoClipCheck = !BotsNoClipCheck;
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
                        var hook = new HookModel(copy.Process, name);
                        Hooks[index] = hook;
                        HookModel = hook;
                    }
                }
            }
        }

        private async void FollowUpdate()
        {
            int xPosAdd, xVelAdd, worldId = 0;
            float sourceX = 0, sourceY = 0, sourceZ = 0, 
                length, xDiff, yDiff, zDiff;

            var offsets = (int[]) PlayerInWorld.Clone();
            //int[] posOffsets;
            string name;
            bool playerFound;

            while (true)
            {
                await Task.Delay(1);
                while (!_followBotsToggle)
                {
                    await Task.Delay(50);
                }

                // var xVelAdd = GetPlayerAddress(XVelocity);
                // var xVelocity = ReadFloat(xVelAdd);
                // var yVelocity = ReadFloat(xVelAdd + 4);
                // var zVelocity = ReadFloat(xVelAdd + 8); 
                // var length = (float) Math.Sqrt(xVelocity * xVelocity + yVelocity * yVelocity + zVelocity * zVelocity) / 1.5f;
                // if (length == 0)
                //     length = 1;

                // var xPosAdd = GetPlayerAddress(XPosition);
                // var sourceX = ReadFloat(xPosAdd) + xVelocity / length;
                // var sourceY = ReadFloat(xPosAdd + 4) + yVelocity / length;
                // var sourceZ = ReadFloat(xPosAdd + 8) + zVelocity / length;

                if (_botsSettings.FollowType == 0)
                {
                    xPosAdd = GetAddressFromLocalPlayer(LocalXPosition);
                    sourceX = ReadFloat(xPosAdd);
                    sourceY = ReadFloat(xPosAdd + 4);
                    sourceZ = ReadFloat(xPosAdd + 8);
                    worldId = _currentWorldId;
                }
                else
                {
                    if (_botsFollowTargetNameLength == 0 || Hooks.Count == 0) continue;

                    playerFound = false;
                    foreach (var hook in Hooks)
                    {
                        var handle = hook.Handle;
                        for (byte i = 1; i < 32; i++)
                        {
                            offsets[1] = PlayersStartOffset + NetworkPlayerStructureSize * i;
                            name = GetName(hook.Handle, hook.NetworkPlayersAddress, offsets.Join(NameOffsets));
                            
                            if (name != _botsFollowTargetName)
                                continue;
    
                            xPosAdd = GetAddress(hook.NetworkPlayersAddress,
                                offsets.Join(CharacterPositionX));
                            sourceX = ReadFloat(handle, xPosAdd);
                            sourceY = ReadFloat(handle, xPosAdd + 4);
                            sourceZ = ReadFloat(handle, xPosAdd + 8);

                            worldId = ReadInt(handle, hook.GameGlobalsAddress, WorldIdStableOffsets);
                            playerFound = true;
                            break;
                        }

                        if (playerFound)
                            break;
                    }

                    if (!playerFound)
                        continue;
                }

                foreach (var hook in Hooks)
                {
                    if (_botsFollowType == 0 && hook.IsPrimary || worldId != hook.WorldId) continue;

                    var handle = hook.Handle;
                    xPosAdd = GetAddressFromLocalPlayer(handle, LocalXPosition);
                    xDiff = sourceX - ReadFloat(handle, xPosAdd);
                    yDiff = sourceY - ReadFloat(handle, xPosAdd + 4);
                    zDiff = sourceZ - ReadFloat(handle, xPosAdd + 8);

                    xVelAdd = GetAddressFromLocalPlayer(handle, LocalXVelocity);
                    length = 1;
                    if (Math.Abs(xDiff) < _botsStopDistance &&
                        Math.Abs(yDiff) < _botsStopDistance &&
                        Math.Abs(zDiff) < _botsStopDistance)
                    {
                        if (_botsStopType == 0) //BotsStopType.FullStop)
                        {
                            WriteFloat(handle, xVelAdd, 0);
                            WriteFloat(handle, xVelAdd + 4, 0);
                            WriteFloat(handle, xVelAdd + 8, 0);
                            continue;
                        }

                        length *= _botsStopPower;
                    }
                    else
                    {
                        if (_botsWarnStatus == 1)
                        {
                            if (Math.Abs(xDiff) < _botsWarnDistance &&
                                Math.Abs(yDiff) < _botsWarnDistance &&
                                Math.Abs(zDiff) < _botsWarnDistance)
                            {
                                if (hook.Notified)
                                    hook.Notified = false;
                            }
                            else if (!hook.Notified)
                            {
                                hook.Notified = true;
                                MessageBox.Show(new StringBuilder("Name: ").Append(hook.Name)
                                        .Append("\nX: ").Append((int) ReadFloat(handle, xPosAdd))
                                        .Append("\nY: ").Append((int) ReadFloat(handle, xPosAdd + 4))
                                        .Append("\nZ: ").Append((int) ReadFloat(handle, xPosAdd + 8))
                                        .Append("\nDistance: ")
                                        .Append((int) Math.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff))
                                        .ToString(),
                                    hook.Name);
                            }
                        }
                    }

                    // if (_botsNotifyState == 1 &&
                    //     !hook.Notified &&
                    //     Math.Abs(xDiff) >= _botsNotifyDistance &&
                    //     Math.Abs(yDiff) >= _botsNotifyDistance &&
                    //     Math.Abs(zDiff) >= _botsNotifyDistance)
                    // {
                    //     hook.Notified = true;
                    //     MessageBox.Show(new StringBuilder("X: ").Append(ReadFloat(handle, xPosAdd))
                    //         .Append("\nY: ").Append(ReadFloat(handle, xPosAdd + 4))
                    //         .Append("\nZ: ").Append(ReadFloat(handle, xPosAdd + 8))
                    //         .Append("\nDistance: ").Append(Math.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff)).ToString(), hook.Name);
                    // }
                    length *= Math.Max((float) Math.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff), 1);
                    // if (length == 0)
                    //     length = 1;

                    // if (xAbs < BotsNoClipTriggerDistance &&
                    //     yAbs < BotsNoClipTriggerDistance &&
                    //     zAbs < BotsNoClipTriggerDistance)
                    // {
                    //     OverwriteBytes(handle, hook.NoClipAddress, _noClipEnabled);
                    //     //hook.NoClipEnabled = false;
                    // }
                    // else
                    // {
                    //     OverwriteBytes(handle, hook.NoClipAddress, _noClipEnabled);
                    // }

                    // if (xAbs < BotsNoClipTriggerDistance &&
                    //     yAbs < BotsNoClipTriggerDistance &&
                    //     zAbs < BotsNoClipTriggerDistance)
                    // {
                    //     OverwriteBytes(hook, _noClipEnabled, _noClip);
                    // }
                    // else
                    // {
                    //     OverwriteBytes(hook, _noClip, _noClipEnabled);
                    // }
                    length /= FollowSpeedValue;

                    WriteFloat(handle, xVelAdd, xDiff / length);
                    WriteFloat(handle, xVelAdd + 4, yDiff / length);
                    WriteFloat(handle, xVelAdd + 8, zDiff / length);
                }
            }
        }

        private static int[] GetPlayerOffsets(int index)
        {
            var array = (int[]) PlayerInWorld.Clone();
            array[1] = index;
            return array;
        }
        
        private unsafe string GetName(IntPtr handle, int address, int[] offsets)
        {
            var buffer = new byte[_botsFollowTargetNameLength];
            ReadMemory(handle, GetAddress(handle, address, offsets), buffer);
            //MessageBox.Show("add:" + address.ToString("X8"));
            //MessageBox.Show(Encoding.ASCII.GetString(buffer));
            return Encoding.ASCII.GetString(buffer);
            fixed (byte* p = buffer)
            {
                for (byte i = 0; i < buffer.Length; i++)
                {
                    if (*(p + i) != 0 && _validChars.Contains(*(p + i)))
                        continue;
                    
                    if (i == 0)
                        break;
                    
                    return Encoding.ASCII.GetString(buffer).Substring(0, i);
                }
            
                return string.Empty;
            }
        }
        
        private unsafe string GetName(IntPtr handle)
        {
            var buffer = new byte[28];
            if (handle == IntPtr.Zero)
                ReadMemory(GetAddressFromLocalPlayer(LocalPlayerNameOffsets), buffer);
            else
                ReadMemory(handle, GetAddressFromLocalPlayer(handle, LocalPlayerNameOffsets), buffer);

            fixed (byte* p = buffer)
            {
                for (byte i = 0; i < buffer.Length; i++)
                {
                    if (*(p + i) != 0) continue;
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

        private (float, float, float) ReadSettings(ref HookModel hook) => ReadSettings(ref hook, hook.ModuleAddress + _settingsPointer);

        private void WriteSettings(ref HookModel hook, (float, float, float) settings)
        {
            var handle = hook.Handle;
            var halfDrawDistanceAdd = GetAddressFromLocalPlayer(handle, HalfDrawDistance);
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
            
            if (_settingsPointer == 0)
            {
                var settings = ReadSettings(address);
                if (settings.Item1 != 0)
                {
                    _settingsPointer = i;
                    return _chatPointer != 0 && _gameGlobalsPointer != 0;
                }
            }
            
            if (_chatPointer == 0)
            {
                address = source + ChatOpenedOffsets[0];
                if (ReadMemory(address, buffer))
                {
                    var opened = *buffer == 1;
                    var valid = opened || *buffer == 0;

                    if (valid && opened)
                    {
                        address = source + ChatOpenedOffsets[1];
                        if (ReadMemory(address, buffer))
                        {
                            if (*intBuffer == 841)
                            {
                                _chatPointer = i;
                                return _settingsPointer != 0 && _gameGlobalsPointer != 0;
                            }
                        }
                    }
                }
            }

            if (_gameGlobalsPointer == 0)
            {
                *intBuffer = source;
                foreach (var offset in WorldIdStableOffsets)
                {
                    address = *intBuffer + offset;
                    if (!ReadMemory(address, buffer)) return false;
                }

                if (_worldId == *intBuffer)
                {
                    _gameGlobalsPointer = i;
                    return _chatPointer != 0 && _settingsPointer != 0;
                }
            }

            return false;
        }

        private unsafe bool FindCharacterBaseAddress(int i)
        {
            if (_localPlayerPointer != 0) return false;
            
            var buffer = stackalloc byte[4];
            var intBuffer = (int*) buffer;
            var address = _currentModuleAddress + i;
            ReadMemory(address, buffer);
                    
            foreach (var offset in LocalXPosition)
            {
                address = *intBuffer + offset;
                if (!ReadMemory(address, buffer))
                {
                    return false;
                }
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