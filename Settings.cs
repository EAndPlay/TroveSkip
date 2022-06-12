using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;

namespace TroveSkipFramework
{
    [Serializable]
    public class Settings
    {
        public string BaseAddress;
        public string ChatBaseAddress;
        
        public string SkipButton;
        public string SprintButton;
        public string SprintToggleButton;
        public string JumpButton;
        public string JumpToggleButton;
        public string SpeedHackToggle;

        public float SkipValue;
        public float SprintValue;
        public float JumpForceValue;
        public int SpeedHackValue;

        public bool SearchOnLoad;
        public bool FollowApp;

        public Settings()
        {
            BaseAddress = ChatBaseAddress = new string('0', 8);
            SprintButton = Key.LeftShift.ToString();
            JumpButton = Key.Space.ToString();
            SkipButton = Key.D3.ToString();
            SprintToggleButton = Key.D4.ToString();
            JumpToggleButton = Key.None.ToString();
            SpeedHackToggle = Key.None.ToString();

            SkipValue = 4;
            SprintValue = 35;
            JumpForceValue = 4;
            SpeedHackValue = 350;
        }

        [NonSerialized] internal static string Path = "settings.json";

        public void Save() => File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));

        public static Settings Load()
        {
            if (File.Exists(Path) && File.ReadAllText(Path) != string.Empty)
            {
                try
                {
                    return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Path));
                }
                catch
                {
                    var settings = new Settings();
                    MessageBox.Show("Loaded settings file are broken. Some settings could switch to default");
                    foreach (var line in File.ReadLines(Path))
                    {
                        if (Regex.IsMatch(line, "\"BaseAddress\":.*\".{8}\""))
                        {
                            settings.BaseAddress = Regex.Match(line, "\"BaseAddress\":.*\"(.{8})\"").Groups[1].Value;
                        }
                        else if (Regex.IsMatch(line, "\"ChatBaseAddress\":.*\".{8}\""))
                        {
                            settings.ChatBaseAddress = Regex.Match(line, "\"ChatBaseAddress\":.*\"(.{8})\"").Groups[1].Value;
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
                            settings.SprintToggleButton = Regex.Match(line, "\"SprintToggleButton\":.*\"(.+)\"").Groups[1].Value;
                        }
                        else if (Regex.IsMatch(line, "\"JumpButton\":.*\".+\""))
                        {
                            settings.JumpButton = Regex.Match(line, "\"JumpButton\":.*\"(.+)\"").Groups[1].Value;
                        }
                        else if (Regex.IsMatch(line, "\"JumpToggleButton\":.*\".+\""))
                        {
                            settings.JumpToggleButton = Regex.Match(line, "\"JumpToggleButton\":.*\"(.+)\"").Groups[1].Value;
                        }
                        else if (Regex.IsMatch(line, "\"SpeedHackToggle\":.*\".+\""))
                        {
                            settings.SpeedHackToggle = Regex.Match(line, "\"SpeedHackToggle\":.*\"(.+)\"").Groups[1].Value;
                        }
                        else if (Regex.IsMatch(line, "\"SprintValue\":.*[0-9]*[.][0-9]+"))
                        {
                            settings.SprintValue = float.Parse(Regex.Match(line, "\"SprintValue\":.*([0-9]*[.][0-9]+)").Groups[1].Value.Replace(".", ","));
                        }
                        else if (Regex.IsMatch(line, "\"SkipValue\":.*[0-9]*[.][0-9]+"))
                        {
                            settings.SkipValue = float.Parse(Regex.Match(line, "\"SkipValue\":.*([0-9]*[.][0-9]+)").Groups[1].Value.Replace(".", ","));
                        }
                        else if (Regex.IsMatch(line, "\"JumpForceValue\":.*[0-9]*[.][0-9]+"))
                        {
                            settings.JumpForceValue = float.Parse(Regex.Match(line, "\"JumpForceValue\":.*([0-9]*[.][0-9]+)").Groups[1].Value.Replace(".", ","));
                        }
                        else if (Regex.IsMatch(line, "\"SpeedHackValue\":.*\\d+"))
                        {
                            settings.SpeedHackValue = int.Parse(Regex.Match(line, "\"SpeedHackValue\":.*(\\d+)").Groups[1].Value.Replace(".", ","));
                        }
                        else if (Regex.IsMatch(line, "\"FollowApp\":\\s*false") || Regex.IsMatch(line, "\"FollowApp\":\\s*true"))
                        {
                            settings.FollowApp = bool.Parse(Regex.Match(line, "\"FollowApp\":\\s*([a-z]+)").Groups[1].Value);
                        }
                    }
                    return settings;
                }
            }
            return new Settings();
        }
    }
}
