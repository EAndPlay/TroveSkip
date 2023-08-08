using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using TroveSkip.Memory;
using TroveSkip.Models;
using TroveSkip.Properties;

namespace TroveSkip.ViewModels
{
    public partial class MainWindowViewModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        private readonly Dispatcher _dispatcher;
        
        private const int VectorSize = sizeof(float) * 3;
        private const int FloatVectorSize = 3;
        //TODO: d3d hook

        private static readonly int[] SettingsToSave =
        {
            Offsets.Settings.DrawDistance,
            Offsets.Settings.Grama,
            Offsets.Settings.ObjectsDrawDistance,
            Offsets.Settings.ShaderDetail
        };
        //{SettingOffset.DrawDistance, SettingOffset.Grama, SettingOffset.ObjectsDrawDistance, SettingOffset.ShaderDetail};

        private static readonly Key[] WASDKeys =
        {
            Key.W, Key.A, Key.S, Key.D
        };

        private static readonly Dictionary<PatchName, bool> Patches = new();

        private bool _authorized;

        public bool Authorized
        {
            get => _authorized;
            set
            {
                if (_authorized != value)
                {
                    if (value)
                    {
                        BlockShitVisibility = Visibility.Hidden;
                        StartBackground();
                    }
                    else
                    {
                        BlockShitVisibility = Visibility.Visible;
                        _activityHook.KeyDown -= OnKeyDown;
                    }
                }

                _authorized = value;
            }
        }

        private Visibility _findButtonVisibility;

        public Visibility FindButtonVisibility
        {
            get => _findButtonVisibility;
            set
            {
                _findButtonVisibility = value;
                OnPropertyChanged();
            }
        }

        private bool[] _userAccesses = new bool[(int)UserAccess.Count];
        
        public ObservableCollectionEx<HookModel> Hooks { get; } = new();
        
        private HookModel _hookModel;
        private IntPtr _handle;

        public HookModel HookModel
        {
            get => _hookModel;
            set
            {
                if (_hookModel != null && (value != null && value.Id != _hookModel.Id || value == null))
                {
                    _hookModel.IsPrimary = false;
                    if (FollowBotsToggle && _botsNoClipCheck)
                    {
                        _hookModel.Patches.Activate(PatchName.NoClip);
                        DarkSide.WriteFloat(_handle, _currentLocalPlayerPointer, Offsets.LocalPlayer.Character.Controller.Gravity, 0); //GravityOffsets, 0);
                    }
                    if (NoGraphics)
                        ChangeGraphics(_hookModel, true);
                    if (AutoAttackCheck && GetCharacterId(_hookModel) != CharacterId.CandyBarbarian)
                        _hookModel.Patches.Activate(PatchName.AutoAttack);
                }

                if (value != null)
                {
                    _hookModel = value;
                    _handle = _hookModel.Handle;
                    _currentModulePointer = value.ModuleAddress;

                    _currentLocalPlayerPointer = _currentModulePointer + _localPlayerOffset;
                    // _currentSettingsPointer = _currentModulePointer + _settingsOffset;
                    // _currentGameGlobalsPointer = _currentModulePointer + _gameGlobalsOffset;
                    // _currentWorldPointer = _currentModulePointer + _worldOffset;
                    _currentChatStatePointer = DarkSide.ReadInt(_handle, _currentModulePointer + _chatOffset) + ChatOpenedOffsets[0];

                    if (_encryptionKey == 0)
                    {
                        var address = DarkSide.FindSignatureAddress(_hookModel, Signatures.StatsEncryptionKeySignature) + 7;
                        _encryptionKey = DarkSide.ReadUInt(_handle, address);
                        var bytes = BitConverter.GetBytes((float) SpeedHackValue);
                        _encryptedSpeed = BitConverter.ToUInt32(bytes, 0) ^ _encryptionKey;
                    }
                    
                    _hookModel.IsPrimary = true;
                    if (FollowBotsToggle && !_botsNoClipCheck && BotsSettings.FollowType == FollowType.Local)
                    {
                        _hookModel.Patches.Deactivate(PatchName.NoClip);
                        DarkSide.WriteFloat(_handle, _currentLocalPlayerPointer, Offsets.LocalPlayer.Character.Controller.Gravity, DefaultGravity); //GravityOffsets, DefaultGravity);
                    }
                    if (NoGraphics)
                        ChangeGraphics(_hookModel, false);
                    if (AutoAttackCheck)
                        _hookModel.Patches.Deactivate(PatchName.AutoAttack);
                }
                else
                {
                    _hookModel = null;
                }
                OnPropertyChanged();
            }
        }
        
        //TODO: abstract to list of Key class (local)
        private readonly Dictionary<string, Key> _binds = new();
        private readonly Dictionary<Key, bool> _pressedKeys = new();
        private readonly List<int> _antiAfkList = new();

        #region Constants

        private const float DefaultGravity = -29;
        private const string UpperHexFormat = "X8";

        #endregion

        private Settings _settings;
        private readonly UserActivityHook _activityHook = new(false, true);

        private string _blockText;

        private string _currentButton;
        private Button _currentButtonElement;

        private int _localPlayerOffset;
        private int _chatOffset;
        private int _settingsOffset;
        private int _gameGlobalsOffset;
        private int _worldOffset;
        
        private int _currentModulePointer;
        
        private int _currentLocalPlayerPointer;
        // private int _currentSettingsPointer;
        // private int _currentGameGlobalsPointer;
        // private int _currentWorldPointer;
        private int _currentChatStatePointer;

        private BotsSettings _botsSettings;

        public BotsSettings BotsSettings
        {
            get => _botsSettings;
            set
            {
                _botsSettings = value;
                OnPropertyChanged();
            }
        }
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
        private bool _noGraphicsCheck;
        private bool _followCamera;
        private bool _rotateCamera;
        private bool _autoPotCheck;
        private bool _autoAttackCheck;
        private bool _stopIfNoMove;
        private bool _antiAfkCheck;

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
        private string _speedHackToggleButton;
        private string _botsNoClipToggleButton;
        private string _attackHoldButton;
        private string _followBotsToggleButton;
        private string _rotateCameraToggleButton;

        private bool _mapCheck;
        private bool _zoomCheck;
        private bool _fovCheck;
        private bool _chamsCheck;
        private bool _miningCheck;
        private bool _autoLootCheck;
        private bool _sendECHeck;

        private bool _followBotsToggle;
        private bool _botsNoClipCheck;

        private Visibility _searchWindowVisibility;
        private Visibility _mainPageVisibility;
        private Visibility _settingsPageVisibility;
        private Visibility _botsSettingsPageVisibility;
        private Visibility _blockShitVisibility;
        private Visibility _connectionGridVisibility;

        private DelegateCommand<PatchName> _patchCommand;
        private DelegateCommand<Button> _bindClickCommand;
        private DelegateCommand<Button> _switchPageCommand;
        private DelegateCommand<HookModel> _isBotChangedCommand;
        private DelegateCommand _invokeSearchWindowCommand;
        private DelegateCommand _findAddressCommand;
        private DelegateCommand _hideWindowCommand;
        private DelegateCommand _closeWindowCommand;

        public string BlockText
        {
            get => _blockText;
            set
            {
                _blockText = value; 
                OnPropertyChanged();
            }
        }

        public string LocalPlayerOffset
        {
            get => _localPlayerOffset.ToString(UpperHexFormat);

            set
            {
                _localPlayerOffset = int.Parse(value, NumberStyles.HexNumber);

                OnPropertyChanged();
            }
        }

        public string SettingsOffset
        {
            get => _settingsOffset.ToString(UpperHexFormat);
            set
            {
                _settingsOffset = int.Parse(value, NumberStyles.HexNumber);
                OnPropertyChanged();
            }
        }

        public string ChatOffset
        {
            get => _chatOffset.ToString(UpperHexFormat);
            set
            {
                _chatOffset = int.Parse(value, NumberStyles.HexNumber); 
                OnPropertyChanged();
            }
        }

        public string GameGlobalsOffset
        {
            get => _gameGlobalsOffset.ToString(UpperHexFormat);
            set
            {
                _gameGlobalsOffset = int.Parse(value, NumberStyles.HexNumber);
                OnPropertyChanged();
            }
        }
        
        public string WorldOffset
        {
            get => _worldOffset.ToString(UpperHexFormat);
            set
            {
                _worldOffset = int.Parse(value, NumberStyles.HexNumber);
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
                if (_speedCheck)
                    _dispatcher.InvokeAsync(ForceSpeedAsync);
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
                    _encryptedSpeed = BitConverter.ToUInt32(BitConverter.GetBytes((float) value), 0) ^ _encryptionKey;
                    //_encryptedSpeed = (uint) *(float*) value ^ _encryptionKey;
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

        public string SpeedHackToggleButton
        {
            get => _speedHackToggleButton;
            set
            {
                _speedHackToggleButton = value;
                OnPropertyChanged();
            }
        }

        public string BotsNoClipToggleButton
        {
            get => _botsNoClipToggleButton;
            set
            {
                _botsNoClipToggleButton = value;
                OnPropertyChanged();
            }
        }
        
        public string AttackHoldButton
        {
            get => _attackHoldButton;
            set
            {
                _attackHoldButton = value;
                OnPropertyChanged();
            }
        }

        public string FollowBotsToggleButton
        {
            get => _followBotsToggleButton;
            set
            {
                _followBotsToggleButton = value;
                OnPropertyChanged();
            }
        }

        public string RotateCameraToggleButton
        {
            get => _rotateCameraToggleButton;
            set
            {
                _rotateCameraToggleButton = value;
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
        
        public bool AutoLootCheck
        {
            get => _autoLootCheck;
            set
            {
                _autoLootCheck = value;
                OnPropertyChanged();
            }
        }
        
        public bool SendECHeck
        {
            get => _sendECHeck;
            set
            {
                _sendECHeck = value;
                OnPropertyChanged();
            }
        }
        
        public bool NoGraphics
        {
            get => _noGraphicsCheck;
            set
            {
                _noGraphicsCheck = value;
                foreach (var hook in Hooks)
                {
                    if (hook.IsBot && !hook.IsPrimary)
                        ChangeGraphics(hook, value);
                }
                OnPropertyChanged();
            }
        }
        
        public bool FollowCamera
        {
            get => _followCamera;
            set
            {
                _followCamera = value;
                if (value)
                    _dispatcher.InvokeAsync(FollowCameraAsync);
                OnPropertyChanged();
            }
        }

        public bool AutoPotCheck
        {
            get => _autoPotCheck;
            set
            {
                _autoPotCheck = value;
                if (value)
                    _dispatcher.InvokeAsync(AutoPotAsync);
                OnPropertyChanged();
            }
        }
        
        public bool AutoAttackCheck
        {
            get => _autoAttackCheck;
            set
            {
                _autoAttackCheck = value;
                OnPropertyChanged();
            }
        }
        
        public bool StopIfNoMove
        {
            get => _stopIfNoMove;
            set
            {
                _stopIfNoMove = value;
                if (value)
                    _dispatcher.InvokeAsync(StopIfNoMoveAsync);
                OnPropertyChanged();
            }
        }

        public bool RotateCamera
        {
            get => _rotateCamera;
            set
            {
                _rotateCamera = value;
                if (value)
                    _dispatcher.InvokeAsync(RotateCameraAsync);
                OnPropertyChanged();
            }
        }

        public bool AntiAfkCheck
        {
            get => _antiAfkCheck;
            set
            {
                _antiAfkCheck = value;
                if (value)
                    foreach (var hook in Hooks)
                        EnableAntiAfk(hook);
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
                if (value)
                {
                    foreach (var hook in Hooks)
                    {
                        if (BotsSettings.FollowType == FollowType.Local && hook.IsPrimary || !hook.IsBot) continue;

                        if (_botsNoClipCheck)
                            hook.Patches.Activate(PatchName.NoClip);
                        DarkSide.WriteFloat(_handle, _currentLocalPlayerPointer,
                            Offsets.LocalPlayer.Character.Controller.Gravity, 0); //GravityOffsets, 0);
                    }
                }
                else
                {
                    foreach (var hook in Hooks)
                    {
                        if (_botsNoClipCheck && BotsSettings.FollowType == FollowType.Local)
                            hook.Patches.Deactivate(PatchName.NoClip);
                        DarkSide.WriteFloat(_handle, _currentLocalPlayerPointer, Offsets.LocalPlayer.Character.Controller.Gravity, DefaultGravity);
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
                        if (hook.IsPrimary && BotsSettings.FollowType == FollowType.Local || !hook.IsBot) continue;

                        hook.Patches.Activate(PatchName.NoClip);
                    }
                }
                else
                {
                    foreach (var hook in Hooks)
                    {
                        hook.Patches.Deactivate(PatchName.NoClip);
                    }
                }
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
                OnPropertyChanged();
            }
        }
        
        public Visibility BlockShitVisibility
        {
            get => _blockShitVisibility;
            set
            {
                _blockShitVisibility = value;
                OnPropertyChanged();
            }
        }
        
        public Visibility ConnectionGridVisibility
        {
            get => _connectionGridVisibility;
            set
            {
                _connectionGridVisibility = value;
                OnPropertyChanged();
            }
        }

        public ICommand PatchCommand => _patchCommand ??= new(PatchStateChanged);
        public ICommand BindClickCommand => _bindClickCommand ??= new(BindClick);
        public ICommand SwitchPageCommand => _switchPageCommand ??= new(SwitchPage);
        public ICommand IsBotChangedCommand => _isBotChangedCommand ??= new(x =>
        {
            MessageBox.Show(x.IsBot.ToString());
        });
        public ICommand InvokeSearchWindowCommand => _invokeSearchWindowCommand ??= new(() => SetSearchWindowState());
        
        public ICommand FindAddressCommand => _findAddressCommand ??= new(//() =>
        // {
            // SetCapture(HookModel.WindowHandle);
            // SendMessage(HookModel.WindowHandle, SystemMessage.SetCursor, 0,
            //     GetCursorPositionLParam(1270, 580));
            // SendMouseClick(HookModel.WindowHandle, MouseButton.LeftButton, 1270, 580);
            //SendMessage((int) HookModel.WindowHandle, SystemMessage.MouseLeftButtonDown, KeyDownMessage.LeftButton, MAKELPARAM(345, 165));
            //SendMessage((int) HookModel.WindowHandle, SystemMessage.MouseLeftButtonUp, 0, MAKELPARAM(345, 165));
            //SendInput(1, new[] {MouseInputSettings}, Input.Size);
        //});
        FindAddress);
        public ICommand HideWindowCommand => _hideWindowCommand ??= new(HideWindow);
        public ICommand CloseWindowCommand => _closeWindowCommand ??= new(CloseWindow);

        private object GetKey(string name) => _binds[name] != Key.None ? 
            (object) (Regex.IsMatch(_binds[name].ToString(), @"D\d") ? 
                _binds[name].ToString().Replace("D", "") : _binds[name]) : "Not binded";
        
        public MainWindowViewModel()
        {
            for (int i = 0; i < Enum.GetValues(typeof(PatchName)).Length; i++)
            {
                Patches.Add((PatchName) i, false);
            }
            
            _dispatcher = Application.Current.MainWindow.Dispatcher;
            SearchWindowVisibility = Visibility.Hidden;
            MainPageVisibility = Visibility.Visible;
            BotsSettingsPageVisibility = Visibility.Hidden;
            //TODO: set "false" on release
            Authorized = true;
            // var proc = Process.GetProcessById(22496);
            // var handle = proc.Handle;
            // var baseAddress = proc.MainModule.BaseAddress;
            // unsafe
            // {
            //     var buffer = stackalloc byte[4];
            //     for (int i = 0; i < 100_000_000; i++)
            //     {
            //         ReadProcessMemory(handle, i, buffer, 4, out _);
            //         if (*(int*) buffer == 1620)
            //         {
            //             MessageBox.Show(i.ToString());
            //             break;
            //         }
            //     }
            // }
            // return;
            
            _dispatcher.ShutdownStarted += (_, _) => CloseWindow();
            _dispatcher.InvokeAsync(LoadSettings);

            //TODO: mouse buttons holding states
            //_activityHook.OnMouseActivity += (ref MouseButtonEventArgs args) =>
            //{
            //    args.ChangedButton 
            //};

            //_activityHook.OnMouseActivity += MouseCheck;
            //_dispatcher.InvokeAsync(StatusUpdate);
        }

        private void HideWindow()
        {
            var window = Application.Current.MainWindow;
            if (window != null)
                window.WindowState = WindowState.Minimized;
        }

        private void CloseWindow()
        {
            if (FollowBotsToggle)
            {
                IntPtr handle;
                foreach (var hook in Hooks)
                {
                    if (hook.IsPrimary || !hook.IsBot) continue;

                    handle = hook.Handle;
                    if (_botsNoClipCheck)
                        hook.Patches.Deactivate(PatchName.NoClip);
                    DarkSide.WriteFloat(handle, hook.LocalPlayerPointer, GravityOffsets, DefaultGravity);
                }
            }

            foreach (var pair in Patches.ToArray())
                if (pair.Value)
                    PatchStateChanged(pair.Key);
            
            SaveSettings();
            Environment.Exit(0);
        }

        // private bool IsUpdatingOrErrorCaused()
        // {
        //     var stringBuilder = new StringBuilder().AppendLine("Can't update:\n");
        //     string str;
        //     try
        //     {
        //         str = _webClient.DownloadString(UpdateUrl);
        //     }
        //     catch (Exception e)
        //     {
        //         stringBuilder.Append("Error on accessing server");
        //         goto ErrorRaised;
        //     }   
        //
        //     var updateSplit = str.Split(' ');
        //
        //     if (new Version(updateSplit[0]) > Assembly.GetExecutingAssembly().GetName().Version)
        //     {
        //         BlockText = "Downloading updater";
        //         var tempFileName = Path.GetTempFileName();
        //         try
        //         {
        //             _webClient.DownloadFile(updateSplit[1], tempFileName);
        //         }
        //         catch
        //         {
        //             stringBuilder.Append("File not available");
        //             goto ErrorRaised;
        //         }
        //
        //         BlockText = "Launching updater";
        //         Process.Start(tempFileName, Environment.CurrentDirectory);
        //     }
        //     else
        //     {
        //         return false;
        //     }
        //
        //     return false;
        //     ErrorRaised:
        //     BlockText = stringBuilder.ToString();
        //     return true;
        // }
        //
        // // rewrite this shit
        // private bool IsAuthorized()
        // {
        //     var stringBuilder = new StringBuilder();
        //     string db;
        //     try
        //     {
        //         db = _webClient.DownloadString(DbUrl);
        //     }
        //     catch
        //     {
        //         BlockText = "Download failed: No access to server";
        //         return false;
        //     }
        //     var id = (string)Registry.CurrentUser.GetValue(@"SOFTWARE\NCT\id");
        //     if (id == null)
        //     {
        //         int newId;
        //         var random = new Random();
        //         do
        //         {
        //             newId = random.Next(100_000, 999_999);
        //         } while (db.Contains(newId.ToString()));
        //
        //         id = newId.ToString();
        //     }
        //
        //     DateTime GetDayTime(string date) //dd.MM.yyyy
        //     {
        //         var dateTimeSplit = date.Split('.').ToArray().Select(int.Parse).ToArray();
        //         return new DateTime(dateTimeSplit[2], dateTimeSplit[1], dateTimeSplit[0]);
        //     }
        //     
        //     TimeSpan GetExpirationTime(string time)
        //     {
        //         int GetFromRegex(string pattern)
        //         {
        //             var regex = new Regex(pattern);
        //             var value = regex.Match(time).Groups[1].Value;
        //             return value != string.Empty ? int.Parse(value) : 0;
        //         }
        //         
        //         var months = GetFromRegex(@"(\d)M");
        //         var days = GetFromRegex(@"(\d)d");
        //         var hours = GetFromRegex(@"(\d)h");
        //         var minutes = GetFromRegex(@"(\d)m");
        //         var seconds = GetFromRegex(@"(\d)s");
        //         return new TimeSpan(months * 30 + days, hours, minutes, seconds);
        //     }
        //     
        //     foreach (var line in db.Split('\n'))
        //     {
        //         var split = line.Split(' ');
        //         if (split[0] != id) continue;
        //         stringBuilder.Append("Id :").AppendLine(id);
        //         var datesSplit = split[1].Split('-');
        //         var startDate = GetDayTime(datesSplit[0]);
        //         var expirationTime = GetExpirationTime(datesSplit[1]);
        //         var expirationDate = startDate + expirationTime + TimeSpan.FromDays(1);
        //         if (DateTime.Now > expirationDate)
        //         {
        //             var daysAgoExpired = (DateTime.Now - expirationDate).Days + 1;
        //             stringBuilder.Append("Expired ")
        //                 .AppendLine(daysAgoExpired.ToString())
        //                 .Append("day");
        //             if (daysAgoExpired > 1)
        //                 stringBuilder.Append("s");
        //             stringBuilder.Append(" ago");
        //             return false;
        //         }
        //         // var startDate = GetDayTime(datesSplit[0]); //just for nothing
        //         // var expirationDate = GetDayTime(datesSplit[1]);
        //         // var expirationPeriod;
        //         // startDate + expirationDate
        //         // if (DateTime.Now > expirationDate)
        //         return true;
        //     }
        //     return false;
        // }

        private void SwitchPage(ContentControl button)
        {
            SetSearchWindowState(Visibility.Hidden);
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
        }

        //----------------------------

        private void EnableAntiAfk(HookModel hook = null)
        {
            _dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(5);
                hook ??= HookModel;
                var id = hook.Id;
                if (_antiAfkList.Contains(id)) return;
                _antiAfkList.Add(id);
                var address = DarkSide.FindSignatureAddress(hook, Signatures.AntiAfkSignature);
                if (address == 0) return;

                var handle = hook.Handle;
                var caveLength = Signatures.AntiAfkCaveSignature.Length + 5; //5 = jmp byte + 4 bytes for address
                var caveAddress = DarkSide.VirtualAllocEx(
                    handle, 
                    0, 
                    caveLength, 
                    DarkSide.AllocationType.Commit, 
                    DarkSide.MemoryProtection.ExecuteRead);

                DarkSide.WriteMemory(handle, caveAddress, DarkSide.AsmJumpOld((ulong) address + 6, (ulong) caveAddress, Signatures.AntiAfkCaveSignature));
                DarkSide.WriteMemory(handle, address, DarkSide.AsmJumpOld((ulong) caveAddress, (ulong) address));
            });
        }

        private void PatchStateChanged(PatchName patchName)
        {
            var newState = Patches[patchName] = !Patches[patchName];
            foreach (var hook in Hooks)
            {
                if (patchName == PatchName.AutoAttack &&
                    (hook.IsPrimary || GetCharacterId(hook) == CharacterId.Revenant))
                {
                    continue;
                }

                if (newState)
                {
                    if (patchName == PatchName.AutoAttack && (hook.IsPrimary || GetCharacterId(hook) == CharacterId.Revenant))
                        continue;
                    
                    hook.Patches.Activate(patchName);
                }
                else
                {
                    var autoAttackLastState = hook.Patches.IsActivated(PatchName.AutoAttack);
                    hook.Patches.Deactivate(patchName);

                    if (autoAttackLastState)
                        DarkSide.SendMouseClick(hook.WindowHandle, DarkSide.MouseButton.LeftButton);
                }
            }
        }

        private void SetSearchWindowState(Visibility visibility = Visibility.Collapsed)
        {
            if (visibility == Visibility.Collapsed)
            {
                SearchWindowVisibility = SearchWindowVisibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            }
            else
            {
                SearchWindowVisibility = visibility;
            }
        }

        private void FindAddress()
        {
            if (Hooks.Count == 0)
            {
                MessageBox.Show("Trove not found");
                return;
            }

            // if (XCoordinate > -3 && XCoordinate < 3)
            // {
            //     MessageBox.Show("Mostly recommended be in X coordinate\nmore than 3 or less than -3");
            //     return;
            // }
            //
            // if (MessageBox.Show(
            //         "Open chat and Press ok\nDONT close chat till address found\n(dialogue with info will appear)",
            //         "Before Scan",
            //         MessageBoxButton.OKCancel) !=
            //     MessageBoxResult.OK)
            // {
            //     return;
            // }
            _localPlayerOffset = _chatOffset = _settingsOffset = _gameGlobalsOffset = _worldOffset = 0;
            
            var address = DarkSide.FindSignatureAddress(_hookModel, Signatures.PlayerPointerSignature) + Signatures.PlayerPointerSignature.Length;
            address = DarkSide.ReadInt(_handle, address);
            LocalPlayerOffset = (address - _currentModulePointer).ToString("X8");
            
            address = DarkSide.FindSignatureAddress(_hookModel, Signatures.WorldPointerSignature) + 10;
            address = DarkSide.ReadInt(_handle, address);
            WorldOffset = (address - _currentModulePointer).ToString("X8");

            address = DarkSide.FindSignatureAddress(_hookModel, Signatures.SettingsPointerSignature) + Signatures.SettingsPointerSignature.Length;
            address = DarkSide.ReadInt(_handle, address);
            SettingsOffset = (address - _currentModulePointer).ToString("X8");
            
            address = DarkSide.FindSignatureAddress(_hookModel, Signatures.ChatStateOffsetSignature) + 7;
            address = DarkSide.ReadInt(_handle, address);
            ChatOffset = (address - _currentModulePointer).ToString("X8");
            
            // for (int i = MinimalModuleOffset; i < MaximalModuleOffset; i++)
            // {
            //     var found = false;
            //     var i1 = i;
            //     _dispatcher.Invoke(() => { found = FindBaseAddresses(i1); });
            //     
            //     if (found)
            //         break;
            // }
            
            SaveSettings();
            HookModel = HookModel;
            foreach (var hook in Hooks)
                UpdateHookInfo(hook);

            MessageBox.Show(new StringBuilder()
                .Append("Player offset: ").AppendLine(LocalPlayerOffset)
                .Append("Chat offset: ").AppendLine(ChatOffset)
                .Append("Settings offset: ").AppendLine(SettingsOffset)
                //.Append("Game Globals offset: ").AppendLine(GameGlobalsOffset)
                .Append("World offset: ").AppendLine(WorldOffset).ToString());
        }

        private readonly Type _keyType = typeof(Key);
        private Key ParseKey(string key) => (Key)Enum.Parse(_keyType, key);
        
        private void LoadSettings()
        {
            _settings = Settings.Load();
            
            LocalPlayerOffset = _settings.LocalPlayerPointer;
            ChatOffset = _settings.ChatPointer;
            SettingsOffset = _settings.SettingsPointer;
            GameGlobalsOffset = _settings.GameGlobalsPointer;
            WorldOffset = _settings.WorldPointer;

            SprintValue = _settings.SprintValue;
            SkipValue = _settings.SkipValue;
            JumpForceValue = _settings.JumpForceValue;
            SpeedHackValue = _settings.SpeedHackValue;
            FollowSpeedValue = _settings.FollowSpeedValue;
            FollowApp = _settings.FollowApp;

            //NoGraphics = _settings.NoGraphics;
            AntiAfkCheck = _settings.AntiAfk;
            StopIfNoMove = _settings.StopIfNoMove;

            BotsSettings = _settings.BotsSettings;
            
            //TODO: make Button class
            {
                _binds.Add(nameof(SkipButton), ParseKey(_settings.SkipButton));
                _binds.Add(nameof(SprintButton), ParseKey(_settings.SprintButton));
                _binds.Add(nameof(SprintToggleButton), ParseKey(_settings.SprintToggleButton));
                _binds.Add(nameof(JumpButton), ParseKey(_settings.JumpButton));
                _binds.Add(nameof(JumpToggleButton), ParseKey(_settings.JumpToggleButton));
                _binds.Add(nameof(SpeedHackToggleButton), ParseKey(_settings.SpeedHackToggleButton));
                _binds.Add(nameof(AttackHoldButton), ParseKey(_settings.AttackHoldButton));
                _binds.Add(nameof(FollowBotsToggleButton), ParseKey(_settings.FollowBotsToggleButton));
                _binds.Add(nameof(BotsNoClipToggleButton), ParseKey(_botsSettings.NoClipToggleButton));
                _binds.Add(nameof(RotateCameraToggleButton), ParseKey(_settings.RotateCameraToggleButton));

                SkipButton = GetKey(nameof(SkipButton)).ToString();
                SprintButton = GetKey(nameof(SprintButton)).ToString();
                SprintToggleButton = GetKey(nameof(SprintToggleButton)).ToString();
                JumpButton = GetKey(nameof(JumpButton)).ToString();
                JumpToggleButton = GetKey(nameof(JumpToggleButton)).ToString();
                SpeedHackToggleButton = GetKey(nameof(SpeedHackToggleButton)).ToString();
                AttackHoldButton = GetKey(nameof(AttackHoldButton)).ToString();
                FollowBotsToggleButton = GetKey(nameof(FollowBotsToggleButton)).ToString();
                BotsNoClipToggleButton = GetKey(nameof(BotsNoClipToggleButton)).ToString();
                RotateCameraToggleButton = GetKey(nameof(RotateCameraToggleButton)).ToString();
            }
            
            BotsFollowTargetName = _botsSettings.FollowTargetName;
        }
        
        private void SaveSettings()
        {
            _settings.LocalPlayerPointer = LocalPlayerOffset;
            _settings.ChatPointer = ChatOffset;
            _settings.SettingsPointer = SettingsOffset;
            _settings.GameGlobalsPointer = GameGlobalsOffset;
            _settings.WorldPointer = WorldOffset;
            
            _settings.SkipValue = SkipValue;
            _settings.SprintValue = SprintValue;
            _settings.JumpForceValue = JumpForceValue;
            _settings.SpeedHackValue = SpeedHackValue;
            _settings.FollowSpeedValue = FollowSpeedValue;

            //_settings.NoGraphics = NoGraphics;
            _settings.AntiAfk = AntiAfkCheck;
            
            _settings.SkipButton = _binds[nameof(SkipButton)].ToString();
            _settings.SprintButton = _binds[nameof(SprintButton)].ToString();
            _settings.SprintToggleButton = _binds[nameof(SprintToggleButton)].ToString();
            _settings.JumpButton = _binds[nameof(JumpButton)].ToString();
            _settings.JumpToggleButton = _binds[nameof(JumpToggleButton)].ToString();
            _settings.SpeedHackToggleButton = _binds[nameof(SpeedHackToggleButton)].ToString();
            _settings.AttackHoldButton = _binds[nameof(AttackHoldButton)].ToString();
            _settings.FollowBotsToggleButton = _binds[nameof(FollowBotsToggleButton)].ToString();
            _settings.RotateCameraToggleButton = _binds[nameof(RotateCameraToggleButton)].ToString();
            
            _settings.FollowApp = FollowApp;
            _botsSettings.FollowTargetName = BotsFollowTargetName;
            _botsSettings.NoClipToggleButton = _binds[nameof(BotsNoClipToggleButton)].ToString();
            
            _settings.Save();
        }

        private static byte[] local_SkipValuesBuffer = new byte[VectorSize];
        private static byte[] local_SkipPositionBuffer = new byte[VectorSize];

        private void Skip()
        {
            var xPositionAddress = DarkSide.GetAddress(_handle, _currentLocalPlayerPointer, Offsets.LocalPlayer.Character.Controller.PositionX);//LocalXPosition);
            var xCameraRotationAddress = DarkSide.GetAddress(_handle, _currentLocalPlayerPointer, Offsets.Camera.Rotation.RotationX);//XView);

            DarkSide.ReadProcessMemory(_handle, xCameraRotationAddress, local_SkipValuesBuffer, VectorSize, out _);
            DarkSide.ReadProcessMemory(_handle, xPositionAddress, local_SkipPositionBuffer, VectorSize, out _);
            unsafe
            {
                fixed (byte* valuesBuffer = local_SkipValuesBuffer,
                    positionBuffer = local_SkipPositionBuffer)
                {
                    var valueFloatPtr = (float*) valuesBuffer;
                    var positionFloatPtr = (float*) positionBuffer;
                    float* tempPtr;
                    for (byte i = 0; i < FloatVectorSize; i++)
                    {
                        tempPtr = valueFloatPtr + i;
                        *tempPtr = *tempPtr * _skipValue + *(positionFloatPtr + i);
                    }
                }
            }

            DarkSide.WriteProcessMemory(_handle, xPositionAddress, local_SkipValuesBuffer, VectorSize, out _);
        }

        private void SuperJump()
        {
            if (!_jumpCheck || NotFocused()) return;
            DarkSide.WriteFloat(_handle, _currentLocalPlayerPointer, LocalYPosition, DarkSide.ReadFloat(_handle, _currentLocalPlayerPointer, LocalYPosition) + _jumpForceValue);
        }
        
        private async void ForceSprintAsync()
        {
            var valuesBuffer = new byte[VectorSize];
            int xViewAdd, velocityAddress;
            while (IsPressed(_binds[nameof(SprintButton)]) && !NotFocused() && _sprintCheck)
            {
                xViewAdd = DarkSide.GetAddress(_handle, _currentLocalPlayerPointer, Offsets.Camera.Rotation.RotationX);//XView);
                velocityAddress = DarkSide.GetAddress(_handle, _currentLocalPlayerPointer, Offsets.LocalPlayer.Character.Controller.VelocityX);//LocalXVelocity);
                
                DarkSide.ReadProcessMemory(_handle, xViewAdd, valuesBuffer, VectorSize, out _);
                unsafe
                {
                    fixed (byte* bufferPtr = valuesBuffer)
                    {
                        var floatPtr = (float*) bufferPtr;
                        for (byte i = 0; i < 3; i++)
                        {
                            *(floatPtr + i) *= _sprintValue;
                        }
                    }
                }
                
                DarkSide.WriteProcessMemory(_handle, velocityAddress, valuesBuffer, VectorSize, out _);
                await Task.Delay(10);
            }
        }

        private async void ForceSpeedAsync()
        {
            while (_speedCheck)
            {
                if (NotHooked() || _followApp && NotFocused())
                {
                    await Task.Delay(100);
                    continue;
                }
                
                DarkSide.WriteUInt(_handle, _currentLocalPlayerPointer, Offsets.LocalPlayer.Character.Stats.MovementSpeed, _encryptedSpeed); //SpeedOffsets
                await Task.Delay(10);
            }
        }

        private void AddHook(HookModel hook)
        {
            UpdateHookInfo(hook);
            Hooks.Add(hook);
            //if (BotsSettings.AutoSetBot)
            hook.IsBot = true;
            
            _dispatcher.InvokeAsync(() =>
                {
                    foreach (var pair in Patches)
                    {
                        if (pair.Value)
                            hook.Patches.Activate(pair.Key);    
                        else
                            hook.Patches.Deactivate(pair.Key);
                    }

                    var marketMasteryCheckAddr = DarkSide.FindSignatureAddress(hook, Signatures.MarketMasteryCheckSignature);
                    if (marketMasteryCheckAddr != 0)
                    {
                        marketMasteryCheckAddr += Signatures.MarketMasteryCheckSignature.Length - 8;
                        var nopBuffer = new byte[8] {0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90};
                        DarkSide.WriteMemory(hook.Handle, marketMasteryCheckAddr, nopBuffer);
                    }
                    //EnableAntiAfk(hook);
                }
            );

            if (NoGraphics)
                ChangeGraphics(hook, true);
        }

        private void UpdateHookInfo(HookModel hook)
        {
            // for future
            var moduleAddress = hook.ModuleAddress;
            hook.WorldPointer = moduleAddress + _worldOffset;
            hook.LocalPlayerPointer = moduleAddress + _localPlayerOffset;
            hook.SettingsPointer = moduleAddress + _settingsOffset;
            //hook.StatsEncryptionKey = ReadUInt(hook.Handle, hook.LocalPlayerPointer, StatsEncryptionKeyOffsets);
        }

        private async void HooksUpdateAsync()
        {
            const int maxNameLength = 15;
            while (true)
            {
                await Task.Delay(50);

                if (FollowApp && HookModel != null)
                {
                    if (HookModel.Id != DarkSide.GetForegroundWindowProcessId()) //== procId
                        HookModel = null;
                }

                var processes = Process.GetProcessesByName("Trove");
                foreach (var hookCopy in Hooks.ToArray())
                {
                    if (processes.All(x => x.Id != hookCopy.Id))
                    {
                        Hooks.Remove(hookCopy);
                        _antiAfkList.Remove(hookCopy.Id);
                    }
                }

                HookModel[] hooksCopy;
                HookModel hook;
                string name;
                foreach (var process in processes)
                {
                    int baseAddress;
                    try
                    {
                        ProcessModule module;
                        if ((module = process.MainModule) == null)
                            continue;

                        baseAddress = (int) module.BaseAddress;
                    }
                    catch
                    {
                        continue;
                    }

                    hooksCopy = Hooks.ToArray();
                    hook = hooksCopy.FirstOrDefault(x => x.Id == process.Id);

                    string FormattedName()
                    {
                        if (name == null) return null;

                        var nameLength = name.Length;
                        return nameLength <= maxNameLength
                            ? name
                            : new StringBuilder(name.Substring(0,
                                    maxNameLength - 3)) //Math.Min(nameLength, maxNameLength - 3)
                                .Append("...").ToString();
                    }

                    if (hook == null)
                    {
                        name = GetName(process.Handle, baseAddress);
                        hook = new HookModel(process, FormattedName());
                        if (hook.ModuleAddress == 0)
                            continue;

                        AddHook(hook);
                    }
                    else if (hook.Name == string.Empty)
                    {
                        name = GetName(hook.Handle, baseAddress);
                        if (name != null)
                            hook.Name = FormattedName();
                    }

                    if (HookModel == null)
                    {
                        if (!FollowApp || FollowApp && DarkSide.GetForegroundWindowProcessId() == hook.Id)
                            HookModel = hook;
                    }
                }

                if (FollowBotsToggle)
                {
                    foreach (var botHook in Hooks)
                    {
                        var gravity = botHook.IsPrimary && BotsSettings.FollowType == FollowType.Local || !botHook.IsBot
                            ? DefaultGravity
                            : 0;
                        DarkSide.WriteFloat(botHook.Handle, botHook.LocalPlayerPointer, GravityOffsets, gravity);
                        botHook.WorldId = DarkSide.ReadInt(botHook.Handle, botHook.WorldPointer, WorldIdOffsets);
                    }
                }
            }

            //if unauthorized
            //Hooks.Clear();
        }

        private bool NotHooked() => HookModel == null;

        private bool NotFocused()
        {
            var handle = DarkSide.GetForegroundWindow();
            if (handle == IntPtr.Zero) return true;
            
            DarkSide.GetWindowThreadProcessId(handle, out var procId);
            return (HookModel?.Id ?? 0) != procId;
        }

        private unsafe bool ChatOpened()
        {
            var boolByte = stackalloc byte[1];
            DarkSide.ReadProcessMemory(_handle, _currentChatStatePointer, boolByte, sizeof(bool), out _);
            return *boolByte == 1;
        }

        private void OnKeyDown(Key key)
        {
            if (NotHooked() || NotFocused() || ChatOpened()) return;

            if (!_pressedKeys.TryGetValue(key, out _))
                _pressedKeys.Add(key, false);

            // if (key == _binds[nameof(SkipButton)] && !IsPressed(key))
            //     _dispatcher.InvokeAsync(Skip);
            // else if (key == _binds[nameof(SprintButton)] && !IsPressed(key))
            //     _dispatcher.InvokeAsync(ForceSprintAsync);
            
            //TODO: abstract to Key.Handle
            if (!IsPressed(key))
            {
                if (key == _binds[nameof(SkipButton)])
                    _dispatcher.InvokeAsync(Skip);

                else if (key == _binds[nameof(SprintButton)])
                    _dispatcher.InvokeAsync(ForceSprintAsync);
                
                else if (key == _binds[nameof(JumpButton)])
                    _dispatcher.InvokeAsync(SuperJump);

                else if (key == _binds[nameof(SprintToggleButton)])
                    SprintCheck = !SprintCheck;

                else if (key == _binds[nameof(JumpToggleButton)])
                    JumpCheck = !JumpCheck;

                else if (key == _binds[nameof(SpeedHackToggleButton)])
                {
                    SpeedCheck = !SpeedCheck;
                    var speed = DarkSide.ReadUInt(_handle, _currentLocalPlayerPointer, SpeedOffsets);
                    if (SpeedCheck)
                    {
                        _lastSpeed = speed;
                    }
                    else
                    {
                        DarkSide.WriteUInt(_handle, _currentLocalPlayerPointer, SpeedOffsets, _lastSpeed);
                    }
                }

                else if (key == _binds[nameof(BotsNoClipToggleButton)] && FollowBotsToggle)
                    BotsNoClipCheck = !BotsNoClipCheck;
                
                //TODO: find out why it made
                // else if (key == _binds[nameof(MiningToggleButton)])
                //     foreach (var h)
                //     PatchStateChanged(new ToggleButton {Name = "MiningCheck", IsChecked = MiningCheck = !MiningCheck}, MiningSlowSignature, MiningSlowEnabledSignature);
                
                else if (key == _binds[nameof(AttackHoldButton)])
                {
                    foreach (var hook in Hooks)
                    {
                        if (hook.IsPrimary) continue;
                        
                        hook.Patches.Activate(PatchName.AutoAttack);
                    }
                }
                
                else if (key == _binds[nameof(FollowBotsToggleButton)])
                    FollowBotsToggle = !FollowBotsToggle;
                
                else if (key == _binds[nameof(RotateCameraToggleButton)])
                    RotateCamera = !RotateCamera;
                
                else if (key == Key.F1)
                {
                    foreach (var hook in Hooks)
                    {
                        if (hook.IsPrimary || GetCharacterId(hook) != CharacterId.Revenant)
                            continue;
                        DarkSide.SendKeyboardKeyPress(hook.WindowHandle, Key.D1);
                        DarkSide.SendKeyboardKeyDown(hook.WindowHandle, Key.W);
                    }
                }
                else if (key == Key.F2)
                {
                    foreach (var hook in Hooks)
                    {
                        if (hook.IsPrimary || GetCharacterId(hook) != CharacterId.Revenant)
                            continue;
                        DarkSide.SendKeyboardKeyPress(hook.WindowHandle, Key.D1);
                        DarkSide.SendKeyboardKeyUp(hook.WindowHandle, Key.W);
                    }
                }
                
                else if (key == Key.E)
                {
                    if (SendECHeck)
                    {
                        foreach (var hook in Hooks)
                        {
                            if (hook.IsPrimary) continue;
                            
                            DarkSide.SendKeyboardKeyPress(hook.WindowHandle, Key.E);
                        }
                    }
                }
            }

            _pressedKeys[key] = true;
        }

        private void OnKeyUp(Key key)
        {
            _pressedKeys[key] = false;

            if (NotHooked() || NotFocused() || ChatOpened()) return;

            if (key == _binds[nameof(AttackHoldButton)])
            {
                foreach (var hook in Hooks)
                {
                    if (hook.IsPrimary) continue;
                    _dispatcher.InvokeAsync(() =>
                    {
                        hook.Patches.Deactivate(PatchName.AutoAttack);
                        DarkSide.SendMouseClick(hook.WindowHandle, DarkSide.MouseButton.LeftButton);
                    });
                }
            }
        }

        // TODO: optimize
        private async void FollowBotsAsync()
        {
            var worldId = 0;
            byte[] sourceBuffer = new byte[VectorSize], velocityBuffer = new byte[VectorSize];
            float tempLength;
            IntPtr handle;
            int xVelAdd, xPosAdd;

            var playerOffsets = (int[]) PlayersArray.Clone();
            var playerNameOffsets = playerOffsets.Join(NameOffsets);
            var characterPositionXOffsets = playerOffsets.Join(CharacterPositionX);
            
            while (Authorized)
            {
                await Task.Delay(1);
                while (!_followBotsToggle || _botsSettings.FollowType == FollowType.Local && (Hooks.Count < 2 || HookModel == null) || _botsSettings.FollowType == FollowType.Target && Hooks.Count == 0)
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

                // int xPosAdd; last

                bool GetSourcePositionsFromTarget(HookModel hook)
                {
                    handle = hook.Handle;
                    var playersCount = DarkSide.ReadInt(handle, hook.WorldPointer, Offsets.World.PlayersList.Count);//PlayersCountInWorldOffsets);
                    for (byte i = 1; i < playersCount; i++)
                    {
                        characterPositionXOffsets[PlayerOffsetInPlayersArray] = 
                            playerNameOffsets[PlayerOffsetInPlayersArray] = 
                                playerOffsets[PlayerOffsetInPlayersArray] = i * sizeof(int);
                        var name = GetName(handle, hook.WorldPointer, playerNameOffsets);//playerOffsets.Join(NameOffsets));
                        
                        if (name != _botsFollowTargetName)
                            continue;

                        xPosAdd = DarkSide.GetAddress(handle, hook.WorldPointer, characterPositionXOffsets);

                        //sourceBuffer = GetBuffer(handle, xPosAdd, VectorSize);
                        DarkSide.ReadProcessMemory(handle, xPosAdd, sourceBuffer, VectorSize, out _);
                        // sourceX = ReadFloat(handle, xPosAdd);
                        // sourceY = ReadFloat(handle, xPosAdd + sizeof(int));
                        // sourceZ = ReadFloat(handle, xPosAdd + 2 * sizeof(int));

                        worldId = hook.WorldId;

                        return true;
                    }

                    return false;
                }

                if (_botsSettings.FollowType == FollowType.Local)
                {
                    xPosAdd = DarkSide.GetAddress(_handle, _currentLocalPlayerPointer, LocalXPosition);
                    //sourceBuffer = GetBuffer(xPosAdd, VectorSize);
                    DarkSide.ReadProcessMemory(_handle, xPosAdd, sourceBuffer, VectorSize, out _);
                    // sourceX = ReadFloat(xPosAdd);
                    // sourceY = ReadFloat(xPosAdd + sizeof(int));
                    // sourceZ = ReadFloat(xPosAdd + 2 * sizeof(int));
                    worldId = _hookModel.WorldId;
                }
                else if (_botsSettings.TargetCheckType == TargetCheckType.AllToLeaderToTarget)
                {
                    foreach (var hook in Hooks)
                    {
                        if (GetSourcePositionsFromTarget(hook))
                            break;
                    }
                }

                foreach (var hook in Hooks)
                {
                    if (!hook.IsBot) continue;
                    if (_botsSettings.FollowType == FollowType.Local)
                    {
                        if (hook.IsPrimary || worldId != hook.WorldId)
                            continue;
                    }
                    else
                    {
                        if (_botsFollowTargetNameLength == 0 || 
                            !GetSourcePositionsFromTarget(hook))
                            continue;
                    }
                    // if (!_botsFollowLocal && _botsTargetCheckType == 1)
                    // {
                    //     if (_botsFollowTargetNameLength == 0 || 
                    //         Hooks.Count == 0 ||
                    //         !GetSourcePositionsFromTarget(hook)) continue;
                    // }
                    // else if (_botsFollowLocal && (hook.IsPrimary || worldId != hook.WorldId)) continue;
                    
                    handle = hook.Handle;
                    xPosAdd = DarkSide.GetAddress(handle, hook.LocalPlayerPointer, LocalXPosition);
                    //xPosAdd = GetAddressFromLocalPlayer(handle, LocalXPosition);
                    // var floats = ReadFloats(handle, xPosAdd, 3);
                    // var xDiff = sourceX - floats[0];
                    // var yDiff = sourceY - floats[1];
                    // var zDiff = sourceZ - floats[2];
                    // xDiff = sourceX - ReadFloat(handle, xPosAdd);
                    // yDiff = sourceY - ReadFloat(handle, xPosAdd + sizeof(uint));
                    // zDiff = sourceZ - ReadFloat(handle, xPosAdd + 2 * sizeof(uint));
                    
                    //velocityBuffer = GetBuffer(handle, xPosAdd, VectorSize);
                    DarkSide.ReadProcessMemory(handle, xPosAdd, velocityBuffer, VectorSize, out _);
                    
                    float length = 1;
                    tempLength = 0;
                    var isClose = true;
                    unsafe
                    {

                        fixed (byte* sourcePtr = sourceBuffer, velocityPtr = velocityBuffer)
                        {
                            // try
                            // {
                            float* sourceFloatPtr = (float*) sourcePtr,
                                velocityFloatPtr = (float*) velocityPtr;
                            int i;
                            for (i = 0; i < FloatVectorSize; i++)
                            {
                                var currentPtr = velocityFloatPtr + i;
                                *currentPtr = *(sourceFloatPtr + i) - *currentPtr;
                                tempLength += *currentPtr * *currentPtr;
                                isClose &= *currentPtr < _botsSettings.StopDistance;
                            }
                            
                            if (isClose)
                                length *= _botsSettings.StopPower;

                            length *= Math.Max((float) Math.Sqrt(tempLength), 1) / _followSpeedValue;
                            for (i = 0; i < FloatVectorSize; i++)
                            {
                                *(velocityFloatPtr + i) /= length;
                            }

                            //MessageBox.Show(BitConverter.ToSingle(velocityBuffer, 0) + " " + BitConverter.ToSingle(velocityBuffer, 4) + " " + BitConverter.ToSingle(velocityBuffer, 8));
                            // }
                            // catch (Exception e)
                            // {
                            //     MessageBox.Show(e.Message + "\n" + e.StackTrace);
                            // }
                        }
                    }

                    /*var*/ xVelAdd = DarkSide.GetAddress(handle, hook.LocalPlayerPointer, LocalXVelocity);
                    // var isCloseNew = new []{xDiff, yDiff, zDiff}.Select(Math.Abs).All(x => x < BotsSettings.StopDistance);
                    //var xVelAdd = GetAddressFromLocalPlayer(handle, LocalXVelocity);
                    // if (Math.Abs(xDiff) < BotsSettings.StopDistance &&
                    //     Math.Abs(yDiff) < BotsSettings.StopDistance &&
                    //     Math.Abs(zDiff) < BotsSettings.StopDistance)
                    if (isClose)
                    // if (isCloseNew)
                    {
                        if (BotsSettings.StopType == StopType.FullStop)
                        {
                            var nullBuffer = new byte[12];
                            DarkSide.WriteMemory(handle, xVelAdd, nullBuffer);
                            continue;
                        }

                        // length *= _botsSettings.StopPower;
                    }
                    if (_botsSettings.WarnEnabled)
                    {
                        // if (Math.Abs(xDiff) < BotsSettings.StopDistance &&
                        //     Math.Abs(yDiff) < BotsSettings.StopDistance &&
                        //     Math.Abs(zDiff) < BotsSettings.StopDistance)
                        if (hook.Notified)
                        {
                            if (isClose)
                                hook.Notified = false;
                        }
                        else// if (!hook.Notified)
                        {
                            static float[] ReadFloats(IntPtr handle, int address, int count)
                            {
                                var buffer = DarkSide.GetBuffer(handle, address, count * sizeof(float));
                                var floats = new float[count];
                                for (short i = 0; i < count; i++)
                                    floats[i] = BitConverter.ToSingle(buffer, i * sizeof(float));
                                return floats;
                            }
                            var floats = ReadFloats(handle, xPosAdd, 3);
                            var xDiff = BitConverter.ToSingle(sourceBuffer!, 0) - floats[0];
                            var yDiff = BitConverter.ToSingle(sourceBuffer!, 4) - floats[1];
                            var zDiff = BitConverter.ToSingle(sourceBuffer!, 8) - floats[2];
                            hook.Notified = true;
                            MessageBox.Show(new StringBuilder("Name: ").Append(hook.Name)
                                    .Append("\nX: ").Append((int) DarkSide.ReadFloat(handle, xPosAdd))
                                    .Append("\nY: ").Append((int) DarkSide.ReadFloat(handle, xPosAdd + 4))
                                    .Append("\nZ: ").Append((int) DarkSide.ReadFloat(handle, xPosAdd + 8))
                                    .Append("\nDistance: ")
                                    .Append((int) Math.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff))
                                    .ToString(),
                                hook.Name);
                        }
                    }

                    // length *= Math.Max((float) Math.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff), 1)
                    //           / _followSpeedValue;

                    // length *= Math.Max((float) Math.Sqrt(tempLength), 1) / _followSpeedValue;
                    
                    // unsafe
                    // {
                    //     fixed (byte* velocityPtr = velocityBuffer)
                    //     {
                    //         var velocityFloatPtr = (float*) velocityPtr;
                    //         for (byte i = 0; i < FloatVectorSize; i++)
                    //         {
                    //             *(velocityFloatPtr + i) /= length;
                    //         }
                    //     }
                    // }

                    DarkSide.WriteProcessMemory(handle, xVelAdd, velocityBuffer, VectorSize, out _);
                }
            }
        }

        private async void FollowCameraAsync()
        {
            const int cameraRotationSize = 8;
            var cameraRotationBuffer = new byte[cameraRotationSize];
            int cameraRotationAddress;
            int hookCameraRotationAddress;
            while (_followCamera)
            {
                await Task.Delay(5);
                while (HookModel == null)
                    await Task.Delay(50);

                cameraRotationAddress = DarkSide.GetAddress(_handle, _currentLocalPlayerPointer, Offsets.Camera.VerticalMove);//CameraVerticalRotationOffsets);
                DarkSide.ReadProcessMemory(_hookModel.Handle, cameraRotationAddress, cameraRotationBuffer, cameraRotationSize,
                    out _);

                foreach (var hook in Hooks)
                {
                    if (hook.IsPrimary) continue;
                    hookCameraRotationAddress =
                        DarkSide.GetAddress(hook.Handle, hook.LocalPlayerPointer, Offsets.Camera.VerticalMove);
                    if (_rotateCamera && IsRotatable(hook))
                    {
                        DarkSide.WriteProcessMemory(hook.Handle, hookCameraRotationAddress, cameraRotationBuffer,
                            cameraRotationSize - sizeof(int), out _);

                        continue;
                    }

                    DarkSide.WriteProcessMemory(hook.Handle, hookCameraRotationAddress, cameraRotationBuffer, cameraRotationSize,
                        out _);
                }
            }
        }

        private static CharacterId[] NonRotatableCharacters = { CharacterId.ShadowHunter, CharacterId.CandyBarbarian, CharacterId.DinoTamer };
        
        private bool IsRotatable(HookModel hook) => !NonRotatableCharacters.Contains(GetCharacterId(hook));

        private async void RotateCameraAsync()
        {
            const float cameraFullXCircle = 6.3f; //game constant
            const int rotateDelay = 1000;
            const int circlesPerSecond = 4;
            const int stepsForFullCircle = 10;
            const int timePerCircleStep = rotateDelay / circlesPerSecond / stepsForFullCircle;
            
            List<HookModel> alts = new();
            while (_rotateCamera)
            {
                foreach (var hook in Hooks)
                {
                    if (hook.IsPrimary) continue;

                    if (IsRotatable(hook))
                    {
                        alts.Add(hook);
                    }
                }

                var altsCount = alts.Count;
                if (altsCount == 0)
                {
                    await Task.Delay(1000);
                    continue;
                }

                var cameraDifference = cameraFullXCircle / altsCount;

                HookModel altHook;
                for (int i = 0; i < altsCount; i++)
                {
                    altHook = alts[i];
                    DarkSide.WriteFloat(altHook.Handle, altHook.LocalPlayerPointer, Offsets.Camera.HorizontalMove,
                        cameraDifference * i);
                }

                int cameraXAddress;
                float cameraX;
                for (int i = 0; i < circlesPerSecond; i++)
                {
                    for (int j = 0; j < stepsForFullCircle; j++)
                    {
                        foreach (var alt in alts)
                        {
                            cameraXAddress = DarkSide.GetAddress(alt.Handle, alt.LocalPlayerPointer,
                                Offsets.Camera.HorizontalMove);
                            cameraX = DarkSide.ReadFloat(alt.Handle, cameraXAddress);
                            DarkSide.WriteFloat(alt.Handle, cameraXAddress, cameraX + cameraFullXCircle / stepsForFullCircle);
                        }

                        await Task.Delay(timePerCircleStep);
                    }

                    foreach (var alt in alts.ToArray())
                    {
                        if (!IsRotatable(alt))
                        {
                            alts.Remove(alt);
                        }
                    }

                    if (!_rotateCamera)
                        return;
                }

                alts.Clear();
            }
        }

        private async void AutoPotAsync()
        {
            IntPtr handle;
            int currentHealth;
            float maxHealth;
            while (_autoPotCheck)
            {
                await Task.Delay(50);
                foreach (var hook in Hooks.ToArray())
                {
                    if (hook.IsPrimary) continue;

                    handle = hook.Handle;
                    currentHealth = DarkSide.ReadInt(handle, hook.LocalPlayerPointer, Offsets.LocalPlayer.Character.CurrentHealth);
                    maxHealth = GetEncryptedFloat(hook, hook.LocalPlayerPointer, Offsets.LocalPlayer.Character.Stats.MaximumHealth);
                    
                    if (currentHealth / maxHealth < 0.5f)
                    {
                        DarkSide.SendKeyboardKeyPress(hook.WindowHandle, Key.Q);
                    }
                }
            }
        }

        private async void StopIfNoMoveAsync()
        {
            var valuesBuffer = new byte[sizeof(int)];
            int velocityAddress;

            bool NoWASD()
            {
                foreach (var key in WASDKeys)
                    if (IsPressed(key))
                        return false;
                return true;
            }

            while (_stopIfNoMove)
            {
                await Task.Delay(50);
                
                if (IsPressed(_binds[nameof(SprintButton)]) && _sprintCheck || !NoWASD())
                {
                    continue;
                }
                
                velocityAddress = DarkSide.GetAddress(_handle, _currentLocalPlayerPointer, LocalXVelocity);
                DarkSide.WriteInt(_handle, velocityAddress, 0);
                DarkSide.WriteInt(_handle, velocityAddress + 8, 0);
            }
        }

        private void StartBackground()
        {
            _activityHook.KeyDown += OnKeyDown;
            _activityHook.KeyUp += OnKeyUp;
            _dispatcher.InvokeAsync(ForceSpeedAsync);
            _dispatcher.InvokeAsync(HooksUpdateAsync);
            _dispatcher.InvokeAsync(FollowBotsAsync);
        }
        
        private async void StatusUpdate()
        {
            BlockText = "Connecting...";
            var stringBuilder = new StringBuilder();
            void OutputText() => BlockText = stringBuilder.ToString();
            
            var webClient = new WebClient();
            //webClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            
            async Task<bool> IsConnected()
            {
                const string connectionUrl = "http://google.com";
                //const string connectionUrl = "http://licensesp.exitgames.com/";
                try
                {
                    //using var stream = webClient.OpenRead(connectionUrl);
                    //webClient.DownloadString(connectionUrl);
                    await webClient.DownloadDataTaskAsync(connectionUrl);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            async Task WaitForConnection()
            {
                const int connectionCheckTime = 1000;
                if (!await IsConnected())
                {
                    // if (block)
                    // {
                    //     Authorized = false;
                    //     BlockText = "Waiting for\ninternet connection";
                    //     BlockShitVisibility = Visibility.Visible;
                    // }
                    ConnectionGridVisibility = Visibility.Visible;

                    await Task.Delay(connectionCheckTime);
                    while (!await IsConnected())
                    {
                        //MessageBox.Show("no connection");
                        await Task.Delay(connectionCheckTime);
                    }
                }

                ConnectionGridVisibility = Visibility.Hidden;
            }
            
            var tempFileName = (string) null;
            {
                const string updateUrl = "https://pastebin.com/raw/ALTsfWjv";
                try
                {
                    await WaitForConnection();
                    var updateText = await webClient.DownloadStringTaskAsync(updateUrl);
                    updateText = updateText.Split('\n')[0];
                    var spaceSplit = updateText.Split(' ');
                    var serverVersion = new Version(spaceSplit[0]);

                    var currentAssembly = Assembly.GetExecutingAssembly();
                    if (serverVersion > currentAssembly.GetName().Version)
                    {
                        var updateLink = spaceSplit[1];
                        tempFileName = Path.GetTempFileName();
                        webClient.DownloadFile(updateLink, tempFileName);
                        Process.Start(tempFileName, currentAssembly.FullName);
                        Environment.Exit(0);
                    }
                }
                catch (Exception e)
                {
                    stringBuilder.AppendLine("Error on update download:")
                        .Append(e.Message)
                        .AppendLine("Try relaunch application and report error");
                    OutputText();
                    if (tempFileName != null)
                    {
                        File.Delete(tempFileName);
                    }
                    return;
                }
            }

            async Task<string> GetDatabaseText()
            {
                const string dbUrl = "https://pastebin.com/raw/rjyuQQyv";
                try
                {
                    return await webClient.DownloadStringTaskAsync(dbUrl);
                }
                catch
                {
                    return string.Empty;
                }
            }
            
            const int authUpdateTime = 6000;
            const int authWaitTime = 2000;

            int? userId;
            {
                const string folderPath = @"SOFTWARE\\NCT";
                const string idKey = "Id";
                var notACheatForTroveFolder = Registry.CurrentUser.OpenSubKey(folderPath, true) 
                                              ?? Registry.CurrentUser.CreateSubKey(folderPath, true);

                userId = (int?) notACheatForTroveFolder.GetValue(idKey);
                if (userId == null)
                {
                    await WaitForConnection();
                    var dbText = await GetDatabaseText();
                    int newId;
                    var random = new Random();
                    do
                    {
                        newId = random.Next(100_000, 1_000_000);
                    } while (dbText.Contains(newId.ToString()));

                    userId = newId;
                    notACheatForTroveFolder.SetValue(idKey, userId, RegistryValueKind.DWord);
                }
            }

            var idString = userId.ToString();
            var idText = "Id: " + idString;
            
            void ResetText() => stringBuilder.Clear().AppendLine(idText);
            
            DateTime GetDayTime(string date) //dd.MM.yyyy
            {
                var dayTimeSepIndex = date.IndexOf(':');
                var dayTime = TimeSpan.Zero;
                //MessageBox.Show(date.Substring(dayTimeSepIndex));
                var dateTimeSplit = (dayTimeSepIndex != - 1 ? date.Substring(0, dayTimeSepIndex) : date).Split('.').ToArray().Select(int.Parse).ToArray();
                if (dayTimeSepIndex != -1)
                {
                    var timeString = date.Substring(dayTimeSepIndex + 1);
                    var dayTimeSplit = timeString.Split('.');
                    int hours = 0, minutes = 0, seconds = 0;
                    if (dateTimeSplit.Length > 0)
                    {
                        //MessageBox.Show(dayTimeSplit[0]);
                        hours = int.Parse(dayTimeSplit[0]);
                        if (dayTimeSplit.Length > 1)
                        {
                            minutes = int.Parse(dayTimeSplit[1]);
                            if (dayTimeSplit.Length > 2)
                            {
                                seconds = int.Parse(dayTimeSplit[2]);
                            }
                        }
                    }
                    dayTime = new(hours, minutes, seconds);
                }
                return new DateTime(dateTimeSplit[2], dateTimeSplit[1], dateTimeSplit[0]) + dayTime;
            }

            TimeSpan GetExpirationTime(string time)
            {
                int GetFromRegex(string pattern)
                {
                    var regex = new Regex(pattern);
                    var value = regex.Match(time).Groups[1].Value;
                    return value != string.Empty ? int.Parse(value) : 0;
                }

                var months = GetFromRegex(@"(\d+)M");
                var days = GetFromRegex(@"(\d+)d");
                var hours = GetFromRegex(@"(\d+)h");
                var minutes = GetFromRegex(@"(\d+)m");
                var seconds = GetFromRegex(@"(\d+)s");
                return new TimeSpan(months * 30 + days, hours, minutes, seconds);
            }

            // Dictionary<UserAccess, bool> ParseAcceses()
            // {
            //     var dict = new Dictionary<UserAccess, bool>();
            //     //if ()
            //     return dict;
            // }

            //var utcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            while (true)
            {
                ResetText();
                await WaitForConnection();
                var userFound = false;
                var dbText = await GetDatabaseText();
                foreach (var line in dbText.Split('\n'))
                {
                    if (line.StartsWith("#")) continue;
                    var spaceSplit = line.Split(' ');
                    if (spaceSplit.Length > 1 && spaceSplit[1] != string.Empty)
                    {
                        if (!int.TryParse(spaceSplit[0], out var id)) continue;
                        //id = int.Parse(spaceSplit[0]);
                        if (id != userId) continue;
                        userFound = true;

                        //MessageBox.Show(spaceSplit[0] + " " + spaceSplit[1]);
                        var datesSplit = spaceSplit[1].Split('-');
                        var startDate = GetDayTime(datesSplit[0]);
                        var expirationTime = GetExpirationTime(datesSplit[1]);
                        var expirationDate = startDate + expirationTime;// + utcOffset;
                        
                        if (expirationDate - DateTime.Now > TimeSpan.Zero)
                        {
                            Authorized = true;
                            //maybe TODO: loading accesses
                        }
                        else
                        {
                            stringBuilder.AppendLine("Expired");
                            Authorized = false;
                        }
                    }
                    else
                    {
                        if (!int.TryParse(line, out var id)) continue;
                        if (userId == id)
                        {
                            userFound = true;
                            Authorized = true;
                        }
                    }

                    // Authorized = true;
                }
                
                //     
        //     foreach (var line in db.Split('\n'))
        //     {
        //         var split = line.Split(' ');
        //         if (split[0] != id) continue;
        //         stringBuilder.Append("Id :").AppendLine(id);
        //         var datesSplit = split[1].Split('-');
        //         var startDate = GetDayTime(datesSplit[0]);
        //         var expirationTime = GetExpirationTime(datesSplit[1]);
        //         var expirationDate = startDate + expirationTime + TimeSpan.FromDays(1);
        //         if (DateTime.Now > expirationDate)
        //         {
        //             var daysAgoExpired = (DateTime.Now - expirationDate).Days + 1;
        //             stringBuilder.Append("Expired ")
        //                 .AppendLine(daysAgoExpired.ToString())
        //                 .Append("day");
        //             if (daysAgoExpired > 1)
        //                 stringBuilder.Append("s");
        //             stringBuilder.Append(" ago");
        //             return false;
        //         }
        //         // var startDate = GetDayTime(datesSplit[0]); //just for nothing
        //         // var expirationDate = GetDayTime(datesSplit[1]);
        //         // var expirationPeriod;
        //         // startDate + expirationDate
        //         // if (DateTime.Now > expirationDate)
        //         return true;
        //     }

                if (!userFound)
                    Authorized = false;
                
                if (!Authorized)
                {
                    BlockText = stringBuilder.AppendLine("No access").ToString();
                }
                await Task.Delay(Authorized ? authUpdateTime : authWaitTime);
            }
        }
        
        private string GetName(IntPtr handle, int baseAddress, params int[] offsets)
        {
            try
            {
                return DarkSide.ReadString(handle, DarkSide.GetAddress(handle, baseAddress, offsets), (byte) _botsFollowTargetNameLength,
                    Encoding.ASCII);
            }
            catch
            {
                return null;
            }
        }

        private string GetName(IntPtr handle, int moduleAddress)
        {
            try
            {
                return DarkSide.ReadStringToEnd(handle,
                    DarkSide.GetAddress(handle, moduleAddress + _localPlayerOffset, Offsets.LocalPlayer.Name),//LocalPlayerNameOffsets),
                    Encoding.ASCII);
            }
            catch
            {
                return null;
            }
        }
        
        private string GetName(HookModel hook) => GetName(hook.Handle, hook.ModuleAddress);

        private CharacterId GetCharacterId(HookModel hook) =>
            (CharacterId) DarkSide.ReadInt(hook.Handle, hook.LocalPlayerPointer, Offsets.LocalPlayer.Character.CharacterId);//CharacterIdOffsets);
        
        private unsafe float GetEncryptedFloat(HookModel hook, int baseAddress, int[] offsets)
        {
            fixed (byte* p = BitConverter.GetBytes(DarkSide.ReadUInt(hook.Handle, DarkSide.GetAddress(hook.Handle, baseAddress, offsets)) ^ _encryptionKey))
            {
                return *(float*)p;
            }
        }

        private float GetEncryptedFloat(IntPtr handle, int baseAddress, int[] offsets) =>
            GetEncryptedFloat(handle, DarkSide.GetAddress(handle, baseAddress, offsets));

        private unsafe float GetEncryptedFloat(IntPtr handle, int address)
        {
            fixed (byte* p = BitConverter.GetBytes(DarkSide.ReadUInt(handle, address) ^ _encryptionKey))
            {
                return *(float*)p;
            }
        }

        private bool IsPressed(Key key)
        {
            if (!_pressedKeys.TryGetValue(key, out _))
                _pressedKeys.Add(key, false);
            return _pressedKeys[key];
        }

        private unsafe float ReadSettings(int baseAddress)
        {
            var buffer = stackalloc byte[sizeof(float)];
            var intBuffer = (int*) buffer;
            var floatBuffer = (float*) buffer;
            DarkSide.ReadMemory(_handle, baseAddress, buffer);
            baseAddress = *intBuffer;
            
            var address = baseAddress + IdkObject[0];
            DarkSide.ReadMemory(_handle, address, buffer);
            var idkObject = *floatBuffer;
            
            if (idkObject == DefaultObjectsDistance)
            {
                address = baseAddress + DrawDistanceOffsets[0];
                DarkSide.ReadMemory(_handle, address, buffer);
                var drawDistance = *floatBuffer;
                
                if (drawDistance >= MinimalDrawDistance && drawDistance <= MaximalDrawDistance)
                {
                    //return drawDistance;
                    address = baseAddress + HalfDrawDistanceOffsets[0];
                    DarkSide.ReadMemory(_handle, address, buffer);
                    var halfDrawDistance = *floatBuffer;
                    
                    if (halfDrawDistance == Math.Min(MaxGrama, drawDistance / 2))
                    {
                        return drawDistance; //(drawDistance, halfDrawDistance, idkObject);
                    }
                }

            }

            return 0;
        }

        private int GetGraphicsSettings(HookModel hook) => (int) DarkSide.ReadFloat(hook.Handle, hook.SettingsPointer, DrawDistanceOffsets);

        private void ChangeGraphics(HookModel hook, bool noGraphics)
        {
            if (noGraphics)
            {
                int address;

                float GetFloat(int offset)
                {
                    address = DarkSide.GetAddress(hook.Handle, hook.SettingsPointer, (int) offset);
                    return DarkSide.ReadFloat(hook.Handle, address);
                }

                foreach (var offset in SettingsToSave)
                {
                    hook.Settings.Add(offset, GetFloat(offset));
                    DarkSide.WriteFloat(hook.Handle, address, 0);
                }
            }
            else
            {
                foreach (var pair in hook.Settings)
                {
                    DarkSide.WriteFloat(hook.Handle, hook.SettingsPointer, new[] {(int) pair.Key}, pair.Value);
                }
                hook.Settings.Clear();
            }
        }

        private unsafe bool FindBaseAddresses(int i)
        {
            var buffer = stackalloc byte[sizeof(int)];
            var intBuffer = (int*) buffer;
            var address = _currentModulePointer + i;
            DarkSide.ReadMemory(_handle, address, buffer);
            var source = *intBuffer;
            
            if (_settingsOffset == 0)
            {
                if (ReadSettings(address) != 0)
                {
                    SettingsOffset = i.ToString("X8");
                    return _localPlayerOffset != 0 && _worldOffset != 0 && _chatOffset != 0;
                }
            }
            
            if (_chatOffset == 0)
            {
                address = source + ChatOpenedOffsets[0];
                if (DarkSide.ReadMemory(_handle, address, buffer))
                {
                    var opened = *buffer == 1;
                    var valid = opened || *buffer == 0;

                    if (valid && opened)
                    {
                        address = source + ChatOpenedOffsets[1];
                        if (DarkSide.ReadMemory(_handle, address, buffer))
                        {
                            if (*intBuffer == 841)
                            {
                                ChatOffset = i.ToString("X8");
                                return _localPlayerOffset != 0 && _worldOffset != 0 && _settingsOffset != 0;
                            }
                        }
                    }
                }
            }

            // if (_gameGlobalsOffset == 0)
            // {
            //     *intBuffer = source;
            //     foreach (var offset in WorldIdStableOffsets)
            //     {
            //         address = *intBuffer + offset;
            //         if (!ReadMemory(address, buffer)) return false;
            //     }
            //
            //     if (_worldId == *intBuffer)
            //     {
            //         GameGlobalsOffset = i.ToString("X8");
            //         return _localPlayerOffset != 0 && _worldOffset != 0 && _chatOffset != 0 && _settingsOffset != 0;
            //     }
            // }
            
            if (_localPlayerOffset == 0)
            {
                *intBuffer = source;
                foreach (var offset in LocalXPosition)
                {
                    address = *intBuffer + offset;
                    if (!DarkSide.ReadMemory(_handle, address, buffer))
                    {
                        address = 0;
                        break;
                    }
                }

                if (address != 0)
                {
                    var value = *(float*) buffer;

                    if (value > _xCoordinate - 1 && value < _xCoordinate + 1)
                    {
                        LocalPlayerOffset = i.ToString("X8");
                        return _worldOffset != 0 && _chatOffset != 0 && _settingsOffset != 0;
                    }
                }
            }
 
            if (_worldOffset == 0)
            {
                // *intBuffer = source;
                // foreach (var offset in FirstPlayerXPosition)
                // {
                //     address = *intBuffer + offset;
                //     if (!ReadMemory(address, buffer))
                //         return false;
                // }
                //
                // var xPos = *(float*) buffer;
                //*intBuffer = source;
                if (!DarkSide.ReadMemory(_handle, source + WorldIdOffsets[0], buffer))
                    return false;

                if (*intBuffer == _worldId)
                {
                    WorldOffset = i.ToString("X8");
                    return _localPlayerOffset != 0 && _chatOffset != 0 && _settingsOffset != 0;
                }
                // if (xPos > _xCoordinate - 1 && xPos < _xCoordinate + 1 && *intBuffer == _worldId)
                // {
                //     WorldOffset = i.ToString("X8");
                //     return _localPlayerOffset != 0 && _chatOffset != 0 && _settingsOffset != 0 && _gameGlobalsOffset != 0;
                // }
            }

            return false;
        }

        private void WindowMouseDown(object sender, EventArgs args) => WindowDeactivated(sender, args);

        private void BindKeyDown(Key key)
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
        
        private void BindClick(Button button)
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

        // private void PreviewTextBoxInput(TextBox sender, TextCompositionEventArgs e)
        // {
        //     Regex regex = sender.Text.Contains(".") ? new("^[a-zA-Z-.]+$") : new("^[a-zA-Z]+$");
        //     e.Handled = regex.IsMatch(e.Text);
        // }

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