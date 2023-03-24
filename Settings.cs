using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using TroveSkip.SettingsParsers;

namespace TroveSkip
{
    [Serializable]
    public class Settings
    {
        public string LocalPlayerPointer;
        public string ChatPointer;
        public string SettingsPointer;
        public string GameGlobalsPointer;
        public string WorldPointer;

        public string SkipButton;
        public string SprintButton;
        public string SprintToggleButton;
        public string JumpButton;
        public string JumpToggleButton;
        public string SpeedHackToggleButton;
        public string MiningToggleButton;
        public string FollowBotsToggleButton;

        public float SkipValue;
        public float SprintValue;
        public float JumpForceValue;
        public int SpeedHackValue;
        public float FollowSpeedValue;

        public bool NoGraphics;
        public bool AntiAfk;

        public bool FollowApp;

        public BotsSettings BotsSettings;

        public Settings()
        {
            LocalPlayerPointer = ChatPointer = SettingsPointer = GameGlobalsPointer = WorldPointer = new string('0', 8);
            SprintButton = Key.LeftShift.ToString();
            JumpButton = Key.Space.ToString();
            SkipButton = Key.D3.ToString();
            SprintToggleButton = Key.D4.ToString();
            JumpToggleButton = Key.None.ToString();
            SpeedHackToggleButton = Key.None.ToString();
            MiningToggleButton = Key.None.ToString();
            FollowBotsToggleButton = Key.None.ToString();

            SkipValue = 4;
            SprintValue = FollowSpeedValue = 40;
            JumpForceValue = 4;
            SpeedHackValue = 200;

            BotsSettings = new();
        }

        // [NonSerialized] private static readonly Dictionary<Type, ISettingParser> Parsers = new();
        //
        // static Settings()
        // {
        //     // var fields = typeof(Settings).GetFields();
        //     // var strings = new List<string>();
        //     // var ints = new List<string>();
        //     // var floats = new List<string>();
        //     // foreach (var field in fields)
        //     // {
        //     //     if (field.FieldType == typeof(string))
        //     //     {
        //     //         strings.Add(field.Name);
        //     //     }
        //     //     else if (field.FieldType == typeof(int))
        //     //     {
        //     //         ints.Add(field.Name);
        //     //     }
        //     //     else if (field.FieldType == typeof(float))
        //     //     {
        //     //         floats.Add(field.Name);
        //     //     }
        //     // }
        //     Parsers.Add(typeof(string), new StringParser());
        //     Parsers.Add(typeof(int), new IntParser());
        //     Parsers.Add(typeof(float), new FloatParser());
        // }

        [NonSerialized] internal static string path = "settings.json";

        public void Save() => File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));

        public static Settings Load()
        {
            if (!File.Exists(path) || File.ReadAllText(path) == string.Empty) return new Settings();
            try
            {
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
            }
            catch
            {
                var settings = new Settings();
                MessageBox.Show("Loaded settings file are broken. Some settings could switch to default");
                foreach (var line in File.ReadLines(path))
                {
                    //// var it = typeof(Settings);
                    //// foreach (var field in it.GetFields())
                    //// {
                    ////     var fieldType = field.FieldType;
                    ////     if (line.Contains(field.Name) && Parsers.Keys.Contains(fieldType))
                    ////     {
                    ////         var value = Parsers[fieldType].Parse(line);
                    ////     }
                    //// }
                    // foreach (var s in Parsers)
                    // {
                    //     foreach (var b in s.Key)
                    //     {
                    //         if (line.Contains(b))
                    //         {
                    //         }
                    //     }
                    // }
                    //TODO: rewrite adequate!!!
                    if (Regex.IsMatch(line, "\"LocalPlayerPointer\":.*\".{8}\""))
                    {
                        settings.LocalPlayerPointer =
                            Regex.Match(line, "\"LocalPlayerPointer\":.*\"(.{8})\"").Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"ChatPointer\":.*\".{8}\""))
                    {
                        settings.ChatPointer =
                            Regex.Match(line, "\"ChatPointer\":.*\"(.{8})\"").Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"SettingsPointer\":.*\".{8}\""))
                    {
                        settings.SettingsPointer =
                            Regex.Match(line, "\"SettingsPointer\":.*\"(.{8})\"").Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"GameGlobalsPointer\":.*\".{8}\""))
                    {
                        settings.GameGlobalsPointer = Regex.Match(line, "\"GameGlobalsPointer\":.*\"(.{8})\"")
                            .Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"PlayersInWorldPointer\":.*\".{8}\""))
                    {
                        settings.WorldPointer = Regex.Match(line, "\"PlayersInWorldPointer\":.*\"(.{8})\"")
                            .Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"SkipButton\":.*\".+\""))
                    {
                        settings.SkipButton = Regex.Match(line, "\"SkipButton\":.*\"(.+)\"").Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"SprintButton\":.*\".+\""))
                    {
                        settings.SprintButton = Regex.Match(line, "\"SprintButton\":.*\"(.+)\"").Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"SprintToggleButton\":.*\".+\""))
                    {
                        settings.SprintToggleButton =
                            Regex.Match(line, "\"SprintToggleButton\":.*\"(.+)\"").Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"JumpButton\":.*\".+\""))
                    {
                        settings.JumpButton = Regex.Match(line, "\"JumpButton\":.*\"(.+)\"").Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"JumpToggleButton\":.*\".+\""))
                    {
                        settings.JumpToggleButton =
                            Regex.Match(line, "\"JumpToggleButton\":.*\"(.+)\"").Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"SpeedHackToggle\":.*\".+\""))
                    {
                        settings.SpeedHackToggleButton = Regex.Match(line, "\"SpeedHackToggle\":.*\"(.+)\"").Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, "\"SprintValue\":.*[0-9]*[.][0-9]+"))
                    {
                        settings.SprintValue = float.Parse(Regex.Match(line, "\"SprintValue\":.*([0-9]*[.][0-9]+)")
                            .Groups[1].Value.Replace(".", ","));
                    }
                    else if (Regex.IsMatch(line, "\"SkipValue\":.*[0-9]*[.][0-9]+"))
                    {
                        settings.SkipValue = float.Parse(Regex.Match(line, "\"SkipValue\":.*([0-9]*[.][0-9]+)")
                            .Groups[1].Value.Replace(".", ","));
                    }
                    else if (Regex.IsMatch(line, "\"JumpForceValue\":.*[0-9]*[.][0-9]+"))
                    {
                        settings.JumpForceValue = float.Parse(Regex
                            .Match(line, "\"JumpForceValue\":.*([0-9]*[.][0-9]+)").Groups[1].Value.Replace(".", ","));
                    }
                    else if (Regex.IsMatch(line, "\"SpeedHackValue\":.*\\d+"))
                    {
                        settings.SpeedHackValue = int.Parse(Regex.Match(line, "\"SpeedHackValue\":.*(\\d+)").Groups[1]
                            .Value.Replace(".", ","));
                    }
                    else if (Regex.IsMatch(line, "\"FollowApp\":\\s*false") ||
                             Regex.IsMatch(line, "\"FollowApp\":\\s*true"))
                    {
                        settings.FollowApp =
                            bool.Parse(Regex.Match(line, "\"FollowApp\":\\s*([a-z]+)").Groups[1].Value);
                    }
                }

                File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
                return settings;
            }
        }
    }
}