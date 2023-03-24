using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        public static MainWindowViewModel Instance { get; }
        private const int VectorSize = sizeof(float) * 3;
        private const int FloatVectorSize = 3;
        //TODO: d3d hook
        
        private static readonly SettingOffset[] SettingsToSave = {SettingOffset.DrawDistance, SettingOffset.Grama, SettingOffset.ObjectsDrawDistance, SettingOffset.ShaderDetail};

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
        

        private readonly Dispatcher _dispatcher;

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
                        //OverwriteBytes(_handle, _hookModel.NoClipAddress, _noClipEnabled);
                        //_hookModel.NoClipEnabled = true;
                        OverwriteBytes(_noClip, _noClipEnabled);
                        WriteFloatToLocalPlayer(GravityOffsets, 0);
                    }
                    if (NoGraphics)
                        ChangeGraphics(_hookModel, true);
                }

                if (value != null)
                {
                    // try
                    // {
                    _hookModel = value;
                    _handle = _hookModel.Handle;
                    _currentModulePointer = value.ModuleAddress;

                    _currentLocalPlayerPointer = _currentModulePointer + _localPlayerOffset;
                    // _currentSettingsPointer = _currentModulePointer + _settingsOffset;
                    // _currentGameGlobalsPointer = _currentModulePointer + _gameGlobalsOffset;
                    // _currentWorldPointer = _currentModulePointer + _worldOffset;
                    _currentChatStatePointer = ReadInt(_currentModulePointer + _chatOffset) + ChatOpenedOffsets[0];

                    _encryptionKey = ReadUInt(_currentLocalPlayerPointer, StatsEncryptionKeyOffsets);
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
                    if (FollowBotsToggle && !_botsNoClipCheck && BotsSettings.FollowType == FollowType.Local)
                    {
                        //OverwriteBytes(_hookModel, _noClipEnabled, _noClip);
                        // _hookModel.NoClipEnabled = false;
                        OverwriteBytes(_noClipEnabled, _noClip);
                        WriteFloatToLocalPlayer(GravityOffsets, DefaultGravity);
                    }
                    if (NoGraphics)
                        ChangeGraphics(_hookModel, false);

                    // }
                    // catch (Exception e)
                    // {
                    //     _hookModel = null;
                    //     MessageBox.Show(e.Message + "\n" + e.StackTrace, "HookModel");
                    // }
                }
                else
                {
                    //MapCheck = ZoomCheck = FovCheck = ChamsCheck = MiningCheck = false;
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
        //private readonly Dictionary<int, (float, float, float)> _lastSettings = new();

        //private readonly (float, float, float) _nullSettings = (0, 0, 0);
        //private readonly byte[] _nullBytes = new byte[12];

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
        private string _miningToggleButton;
        private string _followBotsToggleButton;

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
        private Visibility _blockShitVisibility;
        private Visibility _connectionGridVisibility;

        private DelegateCommand<CheckBox> _mapCheckCommand;
        private DelegateCommand<CheckBox> _zoomCheckCommand;
        private DelegateCommand<CheckBox> _fovCheckCommand;
        private DelegateCommand<CheckBox> _chamsCheckCommand;
        private DelegateCommand<CheckBox> _miningCheckCommand;
        private DelegateCommand<Button> _bindClickCommand;
        private DelegateCommand<Button> _switchPageCommand;
        private DelegateCommand<HookModel> _isBotChangedCommand;
        private DelegateCommand _invokeSearchWindowCommand;
        private DelegateCommand _findAddressCommand;
        private DelegateCommand _hideWindowCommand;
        private DelegateCommand _closeWindowCommand;
        // private DelegateCommand _clickComboBox;

        public string BlockText
        {
            get => _blockText;
            set
            {
                _blockText = value; 
                OnPropertyChanged();
            }
        }

        // public int SettingsSliderValue
        // {
        //     get => _settingsSliderValue;
        //     set
        //     {
        //         _settingsSliderValue = value;
        //         if (CustomDrawDistance)
        //         {
        //             Hooks.ForEachBot(hook =>
        //             {
        //                 if (_botsFollowType != 0 || !hook.IsPrimary)
        //                 {
        //                     ApplySettings(hook);
        //                 }
        //             });
        //         }
        //
        //         OnPropertyChanged();
        //     }
        // }

        public string LocalPlayerOffset
        {
            get => _localPlayerOffset.ToString(UpperHexFormat);

            set
            {
                _localPlayerOffset = int.Parse(value, NumberStyles.HexNumber);
                // if (HookModel != null)
                //     _currentLocalPlayerPointer = _currentModulePointer + _localPlayerOffset;

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
        
        public string MiningToggleButton
        {
            get => _miningToggleButton;
            set
            {
                _miningToggleButton = value;
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

                // if (value)
                // {
                //     Hooks.ForEachBot(hook =>
                //     {
                //         _lastSettings.Add(hook, ReadDrawDistance(hook));
                //     });
                // }
                // else
                // {
                //     foreach (var pair in _lastSettings)
                //     {
                //         WriteSettings(pair.Key, pair.Value);
                //     }
                //     _lastSettings.Clear();
                // }
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

                        //OverwriteBytes(handle, hook.NoClipAddress, _noClipEnabled);
                        if (_botsNoClipCheck)
                            OverwriteBytes(hook, _noClip, _noClipEnabled);
                        WriteFloatToLocalPlayer(hook.Handle, GravityOffsets, 0);
                    }
                }
                else
                {
                    foreach (var hook in Hooks)
                    {
                        //OverwriteBytes(handle, hook.NoClipAddress, _noClip);
                        if (_botsNoClipCheck && BotsSettings.FollowType == FollowType.Local)
                            OverwriteBytes(hook, _noClipEnabled, _noClip);
                        WriteFloatToLocalPlayer(hook.Handle, GravityOffsets, DefaultGravity);
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

                        //OverwriteBytes(handle, hook.NoClipAddress, _noClipEnabled);
                        OverwriteBytes(hook, _noClip, _noClipEnabled);
                    }
                }
                else
                {
                    foreach (var hook in Hooks)
                    {
                        OverwriteBytes(hook, _noClipEnabled, _noClip);
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
                //MainPageVisibility = value == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
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

        public ICommand MapCheckCommand => _mapCheckCommand ??= new(x => InjectCheckChanged(x, _mapHack, _mapHackEnabled));
        public ICommand ZoomCheckCommand => _zoomCheckCommand ??= new(x => InjectCheckChanged(x, _zoomHack, _zoomHackEnabled));
        public ICommand FovCheckCommand => _fovCheckCommand ??= new(x => InjectCheckChanged(x, _fovHack, _fovHackEnabled));
        public ICommand ChamsCheckCommand => _chamsCheckCommand ??= new(x => InjectCheckChanged(x, _chamsMonsters, _chamsMonstersEnabled));
        public ICommand MiningCheckCommand => _miningCheckCommand ??= new(x => InjectCheckChanged(x, _miningSlow, _miningSlowEnabled));
        public ICommand BindClickCommand => _bindClickCommand ??= new(BindClick);
        public ICommand SwitchPageCommand => _switchPageCommand ??= new(SwitchPage);
        public ICommand IsBotChangedCommand => _isBotChangedCommand ??= new(x =>
        {
            MessageBox.Show(x.IsBot.ToString());
        });
        public ICommand InvokeSearchWindowCommand => _invokeSearchWindowCommand ??= new(() => SetSearchWindowState());
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
            BotsSettingsPageVisibility = Visibility.Hidden;
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

            _activityHook.KeyUp += key => _pressedKeys[key] = false;
            //_activityHook.OnMouseActivity += MouseCheck;
            //_dispatcher.InvokeAsync(StatusUpdate);
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
                    if (hook.IsPrimary || !hook.IsBot) continue;

                    handle = hook.Handle;
                    if (_botsNoClipCheck)
                        OverwriteBytes(hook, _noClipEnabled, _noClip);
                    WriteFloatToLocalPlayer(handle, GravityOffsets, DefaultGravity);
                }
            }
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
            _dispatcher.InvokeAsync(() =>
            {
                hook ??= HookModel;
                var id = hook.Id;
                if (_antiAfkList.Contains(id)) return;
                _antiAfkList.Add(id);
                var address = FindSignature(_antiAfk, hook);
                if (address == 0) return;

                var handle = hook.Handle;
                var caveLength = _antiAfkCave.Length + 5;
                var caveAddress = VirtualAllocEx(
                    handle, 
                    0, 
                    caveLength, 
                    AllocationType.Commit, 
                    MemoryProtection.ExecuteRead);

                WriteMemory(handle, caveAddress, AsmJumpOld((ulong) address + 6, (ulong) caveAddress, _antiAfkCave));
                WriteMemory(handle, address, AsmJumpOld((ulong) caveAddress, (ulong) address));
            });
        }

        private void InjectCheckChanged(ToggleButton checkBox, int[] find, int[] change)
        {
            var isChecked = checkBox.IsChecked ?? false;
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
            
            foreach (var hook in Hooks)
            {
                OverwriteBytes(hook, from, to);
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
            if (SearchWindowVisibility == Visibility.Hidden)
                XCoordinate = 0;
        }

        private void FindAddress()
        {
            if (NotHooked()) return;

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
            _localPlayerOffset = _chatOffset = _settingsOffset = _gameGlobalsOffset = _worldOffset = 0;
            
            for (int i = MinimalModuleOffset; i < MaximalModuleOffset; i++)
            {
                var found = false;
                var i1 = i;
                _dispatcher.Invoke(() => { found = FindBaseAddresses(i1); });
                
                if (found)
                    break;
            }

            // for (int i = _chatOffset - PlayerPointerDifference; i < _chatOffset + PlayerPointerDifference; i++)
            // {
            //     if (FindPlayersAddresses(i))
            //         break;
            // }

            HookModel = HookModel;
            foreach (var hook in Hooks)
                UpdateHookAddresses(hook);

            SearchWindowVisibility = Visibility.Hidden;
            MessageBox.Show(new StringBuilder()
                .Append("Player offset: ").AppendLine(LocalPlayerOffset)
                .Append("Chat offset: ").AppendLine(ChatOffset)
                .Append("Settings offset: ").AppendLine(SettingsOffset)
                .Append("Game Globals offset: ").AppendLine(GameGlobalsOffset)
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

            NoGraphics = _settings.NoGraphics;
            AntiAfkCheck = _settings.AntiAfk;

            BotsSettings = _settings.BotsSettings;
            
            //TODO: make Button class
            {
                _binds.Add(nameof(SkipButton), ParseKey(_settings.SkipButton));
                _binds.Add(nameof(SprintButton), ParseKey(_settings.SprintButton));
                _binds.Add(nameof(SprintToggleButton), ParseKey(_settings.SprintToggleButton));
                _binds.Add(nameof(JumpButton), ParseKey(_settings.JumpButton));
                _binds.Add(nameof(JumpToggleButton), ParseKey(_settings.JumpToggleButton));
                _binds.Add(nameof(SpeedHackToggleButton), ParseKey(_settings.SpeedHackToggleButton));
                _binds.Add(nameof(MiningToggleButton), ParseKey(_settings.MiningToggleButton));
                _binds.Add(nameof(FollowBotsToggleButton), ParseKey(_settings.FollowBotsToggleButton));
                _binds.Add(nameof(BotsNoClipToggleButton), ParseKey(_botsSettings.NoClipToggleButton));

                SkipButton = GetKey(nameof(SkipButton)).ToString();
                SprintButton = GetKey(nameof(SprintButton)).ToString();
                SprintToggleButton = GetKey(nameof(SprintToggleButton)).ToString();
                JumpButton = GetKey(nameof(JumpButton)).ToString();
                JumpToggleButton = GetKey(nameof(JumpToggleButton)).ToString();
                SpeedHackToggleButton = GetKey(nameof(SpeedHackToggleButton)).ToString();
                MiningToggleButton = GetKey(nameof(MiningToggleButton)).ToString();
                FollowBotsToggleButton = GetKey(nameof(FollowBotsToggleButton)).ToString();
                BotsNoClipToggleButton = GetKey(nameof(BotsNoClipToggleButton)).ToString();
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

            _settings.NoGraphics = NoGraphics;
            _settings.AntiAfk = AntiAfkCheck;
            
            _settings.SkipButton = _binds[nameof(SkipButton)].ToString();
            _settings.SprintButton = _binds[nameof(SprintButton)].ToString();
            _settings.SprintToggleButton = _binds[nameof(SprintToggleButton)].ToString();
            _settings.JumpButton = _binds[nameof(JumpButton)].ToString();
            _settings.JumpToggleButton = _binds[nameof(JumpToggleButton)].ToString();
            _settings.SpeedHackToggleButton = _binds[nameof(SpeedHackToggleButton)].ToString();
            _settings.MiningToggleButton = _binds[nameof(MiningToggleButton)].ToString();
            _settings.FollowBotsToggleButton = _binds[nameof(FollowBotsToggleButton)].ToString();
            
            _settings.FollowApp = FollowApp;
            _botsSettings.FollowTargetName = BotsFollowTargetName;
            
            _settings.Save();
        }

        private static byte[] local_SkipValuesBuffer = new byte[VectorSize];
        private static byte[] local_SkipPositionBuffer = new byte[VectorSize];
        private unsafe void Skip()
        {
            var xPositionAddress = GetAddressFromLocalPlayer(LocalXPosition);
            var xCameraRotationAddress = GetAddressFromLocalPlayer(XView);

            // var valuesBuffer = GetBuffer(xCameraRotationAddress, VectorSize);
            // var positionBuffer = GetBuffer(xPositionAddress, VectorSize);
            //var valuesBuffer = stackalloc byte[VectorSize];
            //var positionBuffer = stackalloc byte[VectorSize];
            ReadProcessMemory(_handle, xCameraRotationAddress, local_SkipValuesBuffer, VectorSize, out _);
            ReadProcessMemory(_handle, xPositionAddress, local_SkipPositionBuffer, VectorSize, out _);
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

            //MessageBox.Show(BitConverter.ToSingle(valuesBuffer, 0) + " " + BitConverter.ToSingle(valuesBuffer, 4) + " " + BitConverter.ToSingle(valuesBuffer, 8));
            //WriteMemory(xPositionAddress, valuesBuffer);
            WriteProcessMemory(_handle, xPositionAddress, local_SkipValuesBuffer, VectorSize, out _);

            // WriteFloat(xPositionAddress, ReadFloat(xCameraRotationAddress) * _skipValue + ReadFloat(xPositionAddress));
            // WriteFloat(xPositionAddress + sizeof(float), ReadFloat(xCameraRotationAddress + sizeof(float)) * _skipValue + ReadFloat(xPositionAddress + sizeof(float)));
            // WriteFloat(xPositionAddress + 2 * sizeof(float), ReadFloat(xCameraRotationAddress + 2 * sizeof(float)) * _skipValue + ReadFloat(xPositionAddress + 2 * sizeof(float)));
        }

        private void SuperJump()
        {
            if (!_jumpCheck || NotFocused()) return;
            WriteFloatToLocalPlayer(LocalYPosition, ReadFloatFromLocalPlayer(LocalYPosition) + _jumpForceValue);
        }
        
        private async void ForceSprint()
        {
            var valuesBuffer = new byte[VectorSize];
            while (Authorized)
            {
                await Task.Delay(10);
                while (!_sprintCheck || !IsPressed(_binds[nameof(SprintButton)]) || NotFocused())
                    await Task.Delay(10);

                var xviewAdd = GetAddressFromLocalPlayer(XView);
                var velocityAdd = GetAddressFromLocalPlayer(LocalXVelocity);

                //var valuesBuffer = GetBuffer(xviewAdd, VectorSize);
                ReadProcessMemory(_handle, xviewAdd, valuesBuffer, VectorSize, out _);
                unsafe
                {
                    fixed (byte* bufferPtr = valuesBuffer)
                    {
                        var floatPtr = (float*) bufferPtr;
                        for (byte i = 0; i < 3; i++)
                        {
                            *(floatPtr + i) = *(floatPtr + i) * _sprintValue;
                        }
                    }
                }
                //WriteMemory(velocityAdd, valuesBuffer);
                WriteProcessMemory(_handle, velocityAdd, valuesBuffer, VectorSize, out _);

                // WriteFloat(velocityAdd, ReadFloat(xviewAdd) * _sprintValue);
                // WriteFloat(velocityAdd + 4, ReadFloat(xviewAdd + 4) * _sprintValue);
                // WriteFloat(velocityAdd + 8, ReadFloat(xviewAdd + 8) * _sprintValue);
            }
        }

        private async void ForceSpeed()
        {
            while (Authorized)
            {
                await Task.Delay(10);
                while (!_speedCheck || NotHooked() || _followApp && NotFocused() || ChatOpened())
                    await Task.Delay(100);
                WriteUIntToLocalPlayer(SpeedOffsets, _encryptedSpeed);
            }
        }

        private void AddHook(HookModel hook)
        {
            Hooks.Add(hook);
            if (BotsSettings.AutoSetBot)
                hook.IsBot = true;
            
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

                    if (AntiAfkCheck)
                        EnableAntiAfk(hook);
                }
            );

            // if (HookModel == null)
            //     HookModel = hook;
            UpdateHookAddresses(hook);
            // if (NoGraphics)
            //     _lastSettings.Add(hook, GetGraphicsSettings(hook));
            // if (NoGraphics && hook.IsBot)
            // {
            //     SetGraphicsSettings(hook);
            // }
            if (NoGraphics && hook.IsBot)
                ChangeGraphics(hook, true);
            // hook.Process.Exited += (_, _) =>
            // {
            //     Hooks.Remove(Hooks.First(x => x.Id == hook.Id));
            //     _antiAfkList.Remove(hook.Id);
            // }; 
        }

        private void UpdateHookAddresses(HookModel hook)
        {
            // for future
            var moduleAddress = hook.ModuleAddress;
            hook.WorldPointer = moduleAddress + _worldOffset;
            hook.LocalPlayerPointer = moduleAddress + _localPlayerOffset;
            hook.SettingsPointer = moduleAddress + _settingsOffset;
        }

        private async void HooksUpdate()
        {
            while (true)
            {
                const int hooksUpdateTime = 50;
                const int maxNameLength = 15;
                await Task.Delay(hooksUpdateTime);
                
                if (FollowApp && HookModel != null)
                {
                    // var handle = GetForegroundWindow();
                    // GetWindowThreadProcessId(handle, out var procId);
                    
                    if (HookModel.Id != GetForegroundWindowProcessId()) //== procId
                        HookModel = null;
                }
                
                var processes = Process.GetProcessesByName("Trove");

                foreach (var hook in Hooks.ToArray())
                {
                    if (processes.All(x => x.Id != hook.Id))
                    {
                        Hooks.Remove(hook);
                        _antiAfkList.Remove(hook.Id);
                    }
                }
                
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

                    var hooksCopy = Hooks.ToList();
                    var hook = hooksCopy.FirstOrDefault(x => x.Id == process.Id);
                    string name;

                    string FormattedName()
                    {
                        if (name == null) return null;
                        
                        var nameLength = name.Length;
                        return nameLength <= maxNameLength ? name : new StringBuilder(name.Substring(0, Math.Min(nameLength, maxNameLength))).Append("...").ToString();
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
                        if (!FollowApp ||
                            FollowApp && GetForegroundWindowProcessId() == hook.Id)
                            HookModel = hook;
                    }
                }

                if (FollowBotsToggle)
                {
                    foreach (var hook in Hooks)
                    {
                        var gravity = hook.IsPrimary && BotsSettings.FollowType == FollowType.Local || !hook.IsBot ? DefaultGravity : 0;
                        WriteFloat(hook.Handle, hook.LocalPlayerPointer, GravityOffsets, gravity);
                        hook.WorldId = ReadInt(hook.Handle, hook.WorldPointer, WorldIdOffsets);
                    }
                }
            }
        }

        private bool NotHooked() => HookModel == null;

        private static IntPtr local_NotFocusedHandle;
        private bool NotFocused()
        {
            local_NotFocusedHandle = GetForegroundWindow();
            if (local_NotFocusedHandle == IntPtr.Zero) return true;

            GetWindowThreadProcessId(local_NotFocusedHandle, out var procId);
            return (HookModel?.Id ?? 0) != procId;
        }

        private static byte[] local_ChatOpenedBuffer = new byte[1];
        private unsafe bool ChatOpened()
        {
            //ReadBool(_currentChatStatePointer);
            //var buffer = stackalloc byte[sizeof(bool)];
            ReadProcessMemory(_handle, _currentChatStatePointer, local_ChatOpenedBuffer, sizeof(bool), out _);
            return local_ChatOpenedBuffer[0] == 1;
        }
        // {
        //     //var @byte = stackalloc byte[1];
        //     //ReadProcessMemory(_handle, _currentChatStatePointer, @byte, 1, out _);
        //     return ReadBool(_currentChatStatePointer);
        // }

        private void OnKeyDown(Key key)
        {
            if (NotHooked() || NotFocused() || ChatOpened()) return;

            if (!_pressedKeys.TryGetValue(key, out _))
                _pressedKeys.Add(key, false);

            if (key == _binds[nameof(SkipButton)] && !IsPressed(key))
                _dispatcher.InvokeAsync(Skip);
            
            //TODO: abstract to Key.Handle
            if (!IsPressed(key))
            {
                if (key == _binds[nameof(JumpButton)])
                    _dispatcher.InvokeAsync(SuperJump);

                else if (key == _binds[nameof(SprintToggleButton)])
                    SprintCheck = !SprintCheck;

                else if (key == _binds[nameof(JumpToggleButton)])
                    JumpCheck = !JumpCheck;

                else if (key == _binds[nameof(SpeedHackToggleButton)])
                {
                    SpeedCheck = !SpeedCheck;
                    var speed = ReadUInt(_currentLocalPlayerPointer, SpeedOffsets);
                    if (SpeedCheck)
                        _lastSpeed = speed;
                    else
                        WriteUIntToLocalPlayer(SpeedOffsets, _lastSpeed);
                }
                
                else if (key == _binds[nameof(BotsNoClipToggleButton)] && FollowBotsToggle)
                    BotsNoClipCheck = !BotsNoClipCheck;
                
                else if (key == _binds[nameof(MiningToggleButton)])
                    InjectCheckChanged(new ToggleButton {Name = "MiningCheck", IsChecked = MiningCheck = !MiningCheck}, _miningSlow, _miningSlowEnabled);
                
                else if (key == _binds[nameof(FollowBotsToggleButton)])
                    FollowBotsToggle = !FollowBotsToggle;
            }

            _pressedKeys[key] = true;
        }
        
        // TODO: optimize
        private async void FollowUpdate()
        {
            var worldId = 0;
            //float sourceX = 0, sourceY = 0, sourceZ = 0;
            byte[] sourceBuffer = new byte[VectorSize], velocityBuffer = new byte[VectorSize];
            float tempLength;
            IntPtr handle;
            int xVelAdd, xPosAdd;

            var offsets = (int[]) PlayersArray.Clone();
            
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
                    var handle = hook.Handle;
                    var playersCount = ReadInt(handle, hook.WorldPointer, PlayersCountInWorldOffsets);
                    for (byte i = 1; i < playersCount; i++)
                    {
                        offsets[PlayerOffsetInPlayersArray] = i * sizeof(int);
                        var name = GetName(handle, hook.WorldPointer, offsets.Join(NameOffsets));
                        
                        if (name != _botsFollowTargetName)
                            continue;

                        xPosAdd = GetAddress(handle, hook.WorldPointer,
                            offsets.Join(CharacterPositionX));

                        //sourceBuffer = GetBuffer(handle, xPosAdd, VectorSize);
                        ReadProcessMemory(handle, xPosAdd, sourceBuffer, VectorSize, out _);
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
                    xPosAdd = GetAddressFromLocalPlayer(LocalXPosition);
                    //sourceBuffer = GetBuffer(xPosAdd, VectorSize);
                    ReadProcessMemory(_handle, xPosAdd, sourceBuffer, VectorSize, out _);
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
                    xPosAdd = GetAddress(handle, hook.LocalPlayerPointer, LocalXPosition);
                    //xPosAdd = GetAddressFromLocalPlayer(handle, LocalXPosition);
                    // var floats = ReadFloats(handle, xPosAdd, 3);
                    // var xDiff = sourceX - floats[0];
                    // var yDiff = sourceY - floats[1];
                    // var zDiff = sourceZ - floats[2];
                    // xDiff = sourceX - ReadFloat(handle, xPosAdd);
                    // yDiff = sourceY - ReadFloat(handle, xPosAdd + sizeof(uint));
                    // zDiff = sourceZ - ReadFloat(handle, xPosAdd + 2 * sizeof(uint));
                    
                    //velocityBuffer = GetBuffer(handle, xPosAdd, VectorSize);
                    ReadProcessMemory(handle, xPosAdd, velocityBuffer, VectorSize, out _);
                    
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

                    /*var*/ xVelAdd = GetAddress(handle, hook.LocalPlayerPointer, LocalXVelocity);
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
                            WriteMemory(handle, xVelAdd, nullBuffer);
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
                            var floats = ReadFloats(handle, xPosAdd, 3);
                            var xDiff = BitConverter.ToSingle(sourceBuffer!, 0) - floats[0];
                            var yDiff = BitConverter.ToSingle(sourceBuffer!, 4) - floats[1];
                            var zDiff = BitConverter.ToSingle(sourceBuffer!, 8) - floats[2];
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

                    //WriteMemory(handle, xVelAdd, velocityBuffer);
                    WriteProcessMemory(handle, xVelAdd, velocityBuffer, VectorSize, out _);
                    // WriteFloat(handle, xVelAdd, xDiff / length);
                    // WriteFloat(handle, xVelAdd + 4, yDiff / length);
                    // WriteFloat(handle, xVelAdd + 8, zDiff / length);
                }
            }
        }

        private void StartBackground()
        {
            _activityHook.KeyDown += OnKeyDown;
            _dispatcher.InvokeAsync(ForceSprint);
            _dispatcher.InvokeAsync(ForceSpeed);
            _dispatcher.InvokeAsync(HooksUpdate);
            _dispatcher.InvokeAsync(FollowUpdate);
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
                    
                    if (serverVersion > Assembly.GetExecutingAssembly().GetName().Version)
                    {
                        var updateLink = spaceSplit[1];
                        tempFileName = Path.GetTempFileName();
                        webClient.DownloadFile(updateLink, tempFileName);
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
            tempFileName = null;

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
            
            StringBuilder ResetText() => stringBuilder.Clear().AppendLine(idText);
            ResetText();
            while (true)
            {
                await WaitForConnection();
                var userFound = false;
                var dbText = await GetDatabaseText();
                foreach (var line in dbText.Split('\n'))
                {
                    var spaceSplit = line.Split(' ');
                    var id = int.Parse(spaceSplit[0]);
                    if (id != userId) continue;
                    userFound = true;
                }

                if (!userFound)
                    Authorized = false;
                Authorized = dbText.Contains(idString);
                if (!Authorized)
                {
                    BlockText = ResetText().Append("Not authorized").ToString();
                }
                await Task.Delay(Authorized ? authUpdateTime : authWaitTime);
            }
        }
        
        private string GetName(IntPtr handle, int baseAddress, params int[] offsets)
        {
            try
            {
                return ReadString(handle, GetAddress(handle, baseAddress, offsets), (byte) _botsFollowTargetNameLength,
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
                return ReadStringToEnd(handle,
                    GetAddress(handle, moduleAddress + _localPlayerOffset, LocalPlayerNameOffsets),
                    Encoding.ASCII);
            }
            catch
            {
                return null;
            }
        }
            

        private string GetName(HookModel hook) => GetName(hook.Handle, hook.ModuleAddress);

        private float GetEncryptedFloat(IntPtr handle, int baseAddress, int[] offsets) =>
            GetEncryptedFloat(handle, GetAddress(handle, baseAddress, offsets));

        private unsafe float GetEncryptedFloat(IntPtr handle, int address)
        {
            fixed (byte* p = BitConverter.GetBytes(ReadUInt(handle, address) ^ _encryptionKey))
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
            ReadMemory(baseAddress, buffer);
            baseAddress = *intBuffer;
            
            var address = baseAddress + IdkObject[0];
            ReadMemory(address, buffer);
            var idkObject = *floatBuffer;
            
            if (Math.Abs(idkObject - DefaultObjectsDistance) < 2)
            {
                address = baseAddress + DrawDistanceOffsets[0];
                ReadMemory(address, buffer);
                var drawDistance = *floatBuffer;
                
                if (drawDistance >= MinimalDrawDistance && drawDistance <= MaximalDrawDistance)
                {
                    return drawDistance;
                    // address = baseAddress + HalfDrawDistanceOffsets[0];
                    // ReadMemory(address, buffer);
                    // var halfDrawDistance = *floatBuffer;
                    //
                    // if (halfDrawDistance == Math.Min(96, drawDistance / 2))
                    // {
                    //     return (drawDistance, halfDrawDistance, idkObject);
                    // }
                }

            }

            return 0;
        }

        private int GetGraphicsSettings(HookModel hook) => (int)ReadFloat(hook.Handle, hook.SettingsPointer, DrawDistanceOffsets);

        private void ChangeGraphics(HookModel hook, bool noGraphics)
        {
            if (noGraphics)
            {
                int address;

                float GetFloat(SettingOffset offset)
                {
                    address = GetAddress(hook.Handle, hook.SettingsPointer, (int) offset);
                    return ReadFloat(hook.Handle, address);
                }

                foreach (var offset in SettingsToSave)
                {
                    hook.Settings.Add(offset, GetFloat(offset));
                    WriteFloat(hook.Handle, address, 0);
                }
            }
            else
            {
                foreach (var pair in hook.Settings)
                {
                    WriteFloat(hook.Handle, hook.SettingsPointer, new[] {(int) pair.Key}, pair.Value);
                }
                hook.Settings.Clear();
            }
        }

        private void ApplyGraphics()
        {
            if (NoGraphics)
            {
                foreach (var hook in Hooks)
                {
                    ChangeGraphics(hook, true);
                }
            }
            else
            {
                foreach (var hook in Hooks)
                {
                    ChangeGraphics(hook, false);
                }
            }
        }

        // private void SetGraphicsSettings(HookModel hook, int value = NoGraphicsValue)
        // {
        //     var handle = hook.Handle;
        //     var halfDrawDistanceAdd = GetAddress(handle, hook.SettingsPointer, HalfDrawDistanceOffsets);
        //     if (value == NoGraphicsValue)
        //     {
        //         WriteFloat(handle, halfDrawDistanceAdd, value);
        //         WriteFloat(handle, halfDrawDistanceAdd + SettingOffset.ObjectsDrawDistance - SettingOffset.Grama, value);
        //         WriteFloat(handle, halfDrawDistanceAdd + SettingOffset.DrawDistance - SettingOffset.Grama, value);
        //     }
        //     else
        //     {
        //         WriteFloat(handle, halfDrawDistanceAdd, Math.Min(MaxGrama, value / 2));
        //         WriteFloat(handle, halfDrawDistanceAdd + SettingOffset.ObjectsDrawDistance - SettingOffset.Grama, DefaultObjectsDistance);//Math.Min(DefaultObjectValue, value));
        //         WriteFloat(handle, halfDrawDistanceAdd + SettingOffset.DrawDistance - SettingOffset.Grama, value);
        //     }
        // }

        private unsafe bool FindBaseAddresses(int i)
        {
            var buffer = stackalloc byte[sizeof(int)];
            var intBuffer = (int*) buffer;
            var address = _currentModulePointer + i;
            ReadMemory(address, buffer);
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
                    if (!ReadMemory(address, buffer))
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
                if (!ReadMemory(source + WorldIdOffsets[0], buffer))
                    return false;
                
                // foreach (var offset in WorldIdOffsets)
                // {
                //     address = *intBuffer + offset;
                //     if (!ReadMemory(address, buffer))
                //         return false;
                // }

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
            SetSearchWindowState(Visibility.Hidden);
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