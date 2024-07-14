using BatteryGauge.Battery;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Utility.Raii;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GraphicsConfig.Classes;
using ImGuiNET;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Veda;
using static Veda.Functions;

namespace GraphicsConfig
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "GraphicsConfig";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static ICommandManager Commands { get; set; }
        [PluginService] public static IFramework Framework { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static IPluginLog PluginLog { get; set; }
        [PluginService] public static IGameConfig GameConfig { get; set; }
        [PluginService] public static ICondition Condition { get; set; }

        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> CommandManager;
        private PluginUI ui;

        public static bool FirstRun = true;
        public static bool BreakLoop = true;

        public static readonly Queue<Func<bool>> actionQueue = new();
        private readonly Stopwatch sw = new();
        private static uint Delay = 0;
        public static bool IsDebug = true;
        public static readonly CancellationTokenSource BatteryCheckingTask = new();
        public static bool PreviouslyCharging = false;

        public Plugin(IDalamudPluginInterface pluginInterface, IChatGui chat, IPartyList partyList, ICommandManager commands, ICondition conditions)
        {
            PluginInterface = pluginInterface;
            Chat = chat;

            // Get or create a configuration object
            PluginConfig = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            PluginConfig.Initialize(PluginInterface);

            ui = new PluginUI();
            PluginInterface.UiBuilder.Draw += new System.Action(ui.Draw);
            PluginInterface.UiBuilder.OpenConfigUi += () =>
            {
                PluginUI ui = this.ui;
                ui.IsVisible = !ui.IsVisible;
            };

            ui.IsVisible = true;
            
            if (!Directory.Exists("graphical-presets")) { Directory.CreateDirectory("graphical-presets"); }

            // Load all of our commands
            CommandManager = new PluginCommandManager<Plugin>(this, commands);

            Framework.Update += OnFrameworkUpdate;

            conditions.ConditionChange += ConditionChanged;

            PreviouslyCharging = SystemPower.IsCharging;

            if (PluginConfig.UnpluggedPreset != "None")
            {
                Task.Run(async () =>
                {
                    while (!BatteryCheckingTask.Token.IsCancellationRequested)
                    {
                        CheckBattery();

                        await Task.Delay(5000, BatteryCheckingTask.Token);
                    }
                }, BatteryCheckingTask.Token);
            }
        }

        public static void CheckBattery()
        {
            if (SystemPower.IsCharging)
            {
                if(!PreviouslyCharging)
                {
                    //It is now charging, it was not before
                    if (PluginConfig.DefaultPreset != "None")
                    {
                        if (IsDebug) { Print("Default preset loaded because you plugged your device in"); }
                        ApplyConfig(PluginConfig.DefaultPreset);
                    }
                    PreviouslyCharging = SystemPower.IsCharging;
                }

                //if (SystemPower.ChargePercentage == 100 && PluginConfig.HideWhenFull)
                //{
                //    _barEntry.Text = "";
                //    _barEntry.Tooltip = "Battery fully charged.";
                //    return;
                //}

                //_barEntry.Text = PluginConfig.ChargingDisplayMode switch
                //{
                //    ChargingDisplayMode.Hide => "",
                //    ChargingDisplayMode.PercentageOnly => $"{SystemPower.ChargePercentage}%",
                //    ChargingDisplayMode.TextOnly => "Charging",
                //    ChargingDisplayMode.TextPercentage => $"Charging ({SystemPower.ChargePercentage}%)",
                //    _ => throw new ArgumentOutOfRangeException()
                //};

                //_barEntry.Tooltip = $"Battery is charging.\nCurrent percentage: {SystemPower.ChargePercentage}%";
            }
            else
            {
                if (PreviouslyCharging)
                {
                    //It is now unplugged, it was not before
                    if (PluginConfig.UnpluggedPreset != "None")
                    {
                        if (IsDebug) { Print("Unplugged preset loaded because you unplugged your device"); }
                        ApplyConfig(PluginConfig.UnpluggedPreset);
                    }
                    PreviouslyCharging = SystemPower.IsCharging;
                }
                //System is not plugged in

                //var lifetime = TimeUtil.GetPrettyTimeFormat(SystemPower.LifetimeSeconds);

                //_barEntry.Text = _pluginConfig.DischargingDisplayMode switch
                //{
                //    DischargingDisplayMode.Hide => "",
                //    DischargingDisplayMode.PercentageOnly => $"{SystemPower.ChargePercentage}%",
                //    DischargingDisplayMode.RuntimeOnly => lifetime,
                //    DischargingDisplayMode.PercentageRuntime => $"{SystemPower.ChargePercentage}% ({lifetime})",
                //    _ => throw new ArgumentOutOfRangeException()
                //};

                //_barEntry.Tooltip = $"Battery is discharging.\n" +
                //                         $"Current percentage: {SystemPower.ChargePercentage}%\n" +
                //                         $"Remaining life: {lifetime}";
            }
        }

        public void ConditionChanged(ConditionFlag flag, bool value)
        {
            //return
            //Have a string option that stores the name of the preset to use in each case (or "none" for don't change it)
            //Have a dropbox next to each condition that lists the current presets
            switch (flag)
            {
                //case ConditionFlag.BetweenAreas:
                //    if (value)
                //    {
                //        if (IsDebug) Print("Cutscene started");
                //        ApplyConfig("min");
                //    }
                //    else
                //    {
                //        if (IsDebug) Print("Cutscene ended");
                //        ApplyConfig("max");
                //    }
                //    break;
                case ConditionFlag.BoundByDuty:
                case ConditionFlag.BoundByDuty56:
                case ConditionFlag.BoundByDuty95:
                case ConditionFlag.BoundToDuty97:
                    if (value)
                    {
                        if (IsDebug) Print("Flag started");
                        if (PluginConfig.InDutyPreset != "None")
                        {
                            ApplyConfig(PluginConfig.InDutyPreset);
                        }
                    }
                    else
                    {
                        if (IsDebug) Print("Flag ended");
                        if (PluginConfig.DefaultPreset != "None")
                        {
                            ApplyConfig(PluginConfig.DefaultPreset);
                        }
                    }
                    break;

                //case ConditionFlag.ChocoboRacing:
                //    if (value)
                //    {
                //        if (IsDebug) Print("Flag started");
                //        ApplyConfig("FlagStarted");
                //    }
                //    else
                //    {
                //        if (IsDebug) Print("Flag ended");
                //        ApplyConfig(PluginConfig.DefaultPreset);
                //    }
                //    break;

                case ConditionFlag.Crafting:
                case ConditionFlag.Crafting40:
                case ConditionFlag.PreparingToCraft:

                    if (value)
                    {
                        if (IsDebug) Print("Flag started");
                        if (PluginConfig.CraftingPreset != "None")
                        {
                            ApplyConfig(PluginConfig.CraftingPreset);
                        }
                    }
                    else
                    {
                        if (IsDebug) Print("Flag ended");
                        if (PluginConfig.DefaultPreset != "None")
                        {
                            ApplyConfig(PluginConfig.DefaultPreset);
                        }
                    }
                    break;

                case ConditionFlag.CreatingCharacter: //Does this include with Fantasia?
                    if (value)
                    {
                        if (IsDebug) Print("Flag started");
                        if (PluginConfig.EditingCharacterPreset != "None")
                        {
                            ApplyConfig(PluginConfig.EditingCharacterPreset);
                        }
                    }
                    else
                    {
                        if (IsDebug) Print("Flag ended");
                        if (PluginConfig.DefaultPreset != "None")
                        {
                            ApplyConfig(PluginConfig.DefaultPreset);
                        }
                    }
                    break;

                //case ConditionFlag.EditingPortrait:
                //    if (value)
                //    {
                //        if (IsDebug) Print("Flag started");
                //        ApplyConfig("FlagStarted");
                //    }
                //    else
                //    {
                //        if (IsDebug) Print("Flag ended");
                //        ApplyConfig(PluginConfig.DefaultPreset);
                //    }
                //    break;

                case ConditionFlag.Fishing:
                case ConditionFlag.Gathering:
                case ConditionFlag.Gathering42:
                    if (value)
                    {
                        if (IsDebug) Print("Flag started");
                        if (PluginConfig.GatheringPreset != "None")
                        {
                            ApplyConfig(PluginConfig.GatheringPreset);
                        }
                    }
                    else
                    {
                        if (IsDebug) Print("Flag ended");
                        if (PluginConfig.DefaultPreset != "None")
                        {
                            ApplyConfig(PluginConfig.DefaultPreset);
                        }
                    }
                    break;

                case ConditionFlag.InCombat:
                    if (!Condition[ConditionFlag.BoundByDuty] && !Condition[ConditionFlag.BoundByDuty56] && !Condition[ConditionFlag.BoundByDuty95])
                    {
                        if (value)
                        {
                            if (IsDebug) Print("Flag started");
                            if (PluginConfig.CombatPreset != "None")
                            {
                                ApplyConfig(PluginConfig.CombatPreset);
                            }
                        }
                        else
                        {
                            if (IsDebug) Print("Flag ended");
                            if (PluginConfig.DefaultPreset != "None")
                            {
                                ApplyConfig(PluginConfig.DefaultPreset);
                            }
                        }
                    }
                    break;

                //case ConditionFlag.InDeepDungeon:
                //    if (value)
                //    {
                //        if (IsDebug) Print("Flag started");
                //        ApplyConfig("FlagStarted");
                //    }
                //    else
                //    {
                //        if (IsDebug) Print("Flag ended");
                //        ApplyConfig(PluginConfig.DefaultPreset);
                //    }
                //    break;

                case ConditionFlag.Performing: //Bard Performance
                    if (value)
                    {
                        if (IsDebug) Print("Flag started");
                        if (PluginConfig.PerformancePreset != "None")
                        {
                            ApplyConfig(PluginConfig.PerformancePreset);
                        }
                    }
                    else
                    {
                        if (IsDebug) Print("Flag ended");
                        if (PluginConfig.DefaultPreset != "None")
                        {
                            ApplyConfig(PluginConfig.DefaultPreset);
                        }
                    }
                    break;

                case ConditionFlag.WatchingCutscene:
                case ConditionFlag.WatchingCutscene78:
                case ConditionFlag.OccupiedInCutSceneEvent:
                    if (value)
                    {
                        if (IsDebug) Print("Cutscene started");
                        if (PluginConfig.WatchingCutscenePreset != "None")
                        {
                            ApplyConfig(PluginConfig.WatchingCutscenePreset);
                        }
                    }
                    else
                    {
                        if (IsDebug) Print("Cutscene ended");
                        if (PluginConfig.DefaultPreset != "None")
                        {
                            ApplyConfig(PluginConfig.DefaultPreset);
                        }
                    }
                    break;
            }
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (actionQueue.Count == 0)
            {
                if (sw.IsRunning) sw.Stop();
                return;
            }
            if (!sw.IsRunning) sw.Restart();

            if (Delay > 0)
            {
                Delay -= 1;
                return;
            }

            if (sw.ElapsedMilliseconds > 3000)
            {
                actionQueue.Clear();
                return;
            }

            try
            {
                var hasNext = actionQueue.TryPeek(out var next);
                if (hasNext)
                {
                    if (next())
                    {
                        actionQueue.Dequeue();
                        sw.Reset();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed: {ex.ToString()}");
            }
        }

        [Command("/gconfig")]
        [HelpMessage("Shows Graphics Config configuration options")]
        public void ShowTwitchOptions(string command, string args)
        {
            ui.IsVisible = !ui.IsVisible;
        }

        [Command("/glist")]
        [HelpMessage("List all graphical presets")]
        public unsafe void ListPresets(string command, string args)
        {
            string FinalMessage = "Available presets saved on your device:" + Environment.NewLine;
            if (GetPresets().Count() == 0)
            {
                Print("You don't have any presets! Type \"/gsave PresetName\" to save one.", ColorType.Info);
                return;
            }
            foreach (string PresetName in GetPresets())
            {
                FinalMessage += "\"" + PresetName + "\", ";
            }
            Print(FinalMessage.Remove(FinalMessage.Length - 2, 2), ColorType.Info);
        }

        [Command("/gsave")]
        [HelpMessage("Save current settings as a graphical preset")]
        public unsafe void SavePreset(string command, string args)
        {
            if (args.ToLower() == "none") { Print("You cannot name a preset that. Stop trying to break the plugin. >:|", ColorType.Error); return; }
            args = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(args.ToLower());
            if (String.IsNullOrWhiteSpace(args)) { Chat.PrintError("Error: Please provide a filename for the preset."); return; }
            //Get the current settings and save it to a JSON, overwriting if they specify /y or something
            WriteGraphicalPreset(args);
            Print("Saved the \"" + args + "\" graphical preset.", ColorType.Success);
        }

        [Command("/gload")]
        [HelpMessage("Load previously saved graphical preset")]
        public unsafe void LoadPreset(string command, string args)
        {
            if (args.ToLower() == "none") { Print("This is not a valid preset name. Stop trying to break the plugin. >:|", ColorType.Error); return; }
            args = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(args.ToLower());
            if (String.IsNullOrWhiteSpace(args)) { Chat.PrintError("Error: Please provide a filename for the preset."); return; }
            //Make a new configuration object, load the JSON object from the file as a <GraphicalConfiguration>
            //Then apply it to the current thing
            ApplyConfig(args);
            Print("Loaded the \"" + args + "\" graphical preset.", ColorType.Success);
        }

        public static List<string> GetPresets()
        {
            FileInfo[] Files = new DirectoryInfo(@"graphical-presets").GetFiles("*.json", SearchOption.TopDirectoryOnly); //Assuming Test is your Folder
            List<string> presets = new List<string>();
            foreach (FileInfo File in Files)
            {
                presets.Add(File.Name.Replace(".json", ""));
            }
            return presets;
        }

        public static void ApplySetting(string SettingToModify, uint NewValue)
        {
            GameConfig.System.Set(SettingToModify, NewValue);
        }

        public static uint GetSetting(string SettingToGet)
        {
            return GameConfig.System.GetUInt(SettingToGet);
        }

        public static GraphicalConfiguration ReadGraphicalPreset(string PresetName)
        {
            try
            {
                if (!Directory.Exists("graphical-presets")) { Directory.CreateDirectory("graphical-presets"); }
                PresetName = "graphical-presets\\" + System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(PresetName.ToLower()) + ".json";
                if (!File.Exists(PresetName))
                {
                    Print("Couldn't find a graphical preset named \"" + PresetName + "\".", ColorType.Warn);
                    return null;
                }

                string JSONString = File.ReadAllText(PresetName);

                GraphicalConfiguration RequestedPreset = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphicalConfiguration>(JSONString);

                return RequestedPreset;
            }
            catch (Exception f)
            {
                Chat.PrintError("Something went wrong with reading graphical setting data for " + PresetName + " - " + f.ToString());
                return null;
            }
        }

        public static bool WriteGraphicalPreset(string PresetName, bool Overwrite = false)
        {
            try
            {
                if (!Directory.Exists("graphical-presets")) { Directory.CreateDirectory("graphical-presets"); }
                PresetName = "graphical-presets\\" + System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(PresetName.ToLower()) + ".json";

                GraphicalConfiguration CurrentConfig = GetCurrentConfig();
                string NewJSON = JsonConvert.SerializeObject(CurrentConfig, Newtonsoft.Json.Formatting.Indented);

                System.IO.File.WriteAllText(PresetName, NewJSON);
                //Chat.Print("[Graphics Config] Preset saved as \"" + PresetName + "\".");
                return true;
            }
            catch (Exception f)
            {
                Chat.PrintError("Something went wrong with writing graphical setting data for " + PresetName + " - " + f.ToString());
                return false;
            }
        }

        public static GraphicalConfiguration GetCurrentConfig()
        {
            try
            {
                GraphicalConfiguration CurrentConfig = new GraphicalConfiguration();

                PropertyInfo[] properties = typeof(GraphicalConfiguration).GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    property.SetValue(CurrentConfig, GetSetting(property.Name));
                }
                return CurrentConfig;
            }
            catch (Exception e)
            {
                Print(e.ToString(), ColorType.Error);
                return null;
            }
        }

        public static void Print(string Message, ushort ColorType = 0)
        {
            Chat.Print(BuildSeString("Graphics Config", Message, ColorType));
        }

        public static bool ApplyConfig(string PresetName)
        {
            try
            {
                GraphicalConfiguration CurrentConfig = ReadGraphicalPreset(PresetName);

                if (CurrentConfig == null) { return false; }

                PropertyInfo[] properties = typeof(GraphicalConfiguration).GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    //Print("Loading/Setting " + property.Name);
                    ApplySetting(property.Name, (uint)property.GetValue(CurrentConfig));
                }
                Print("Loaded " + PresetName);
                return true;
            }
            catch (Exception e)
            {
                Print(e.ToString(), ColorType.Error);
                return false;
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            CommandManager.Dispose();

            PluginInterface.SavePluginConfig(PluginConfig);

            PluginInterface.UiBuilder.Draw -= ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= () =>
            {
                PluginUI ui = this.ui;
                ui.IsVisible = !ui.IsVisible;
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}