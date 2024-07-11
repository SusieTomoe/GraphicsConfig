using Dalamud.Game;
using Dalamud.Game.Config;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using Veda;
using GraphicsConfig.Classes;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.IO;
using FFXIVClientStructs.FFXIV.Common.Lua;
using System.Reflection;
using static Veda.Functions;

namespace GraphicsConfig
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "GraphicsConfig";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static ICommandManager Commands { get; set; }
        [PluginService] public static ICondition Conditions { get; set; }
        [PluginService] public static IDataManager Data { get; set; }
        [PluginService] public static IFramework Framework { get; set; }
        [PluginService] public static IGameGui GameGui { get; set; }
        [PluginService] public static ISigScanner SigScanner { get; set; }
        [PluginService] public static IKeyState KeyState { get; set; }
        [PluginService] public static IChatGui Chat { get; set; }
        [PluginService] public static IClientState ClientState { get; set; }
        [PluginService] public static IPartyList PartyList { get; set; }
        [PluginService] public static IPluginLog PluginLog { get; set; }
        [PluginService] public static IGameConfig GameConfig { get; set; }

        public static Configuration PluginConfig { get; set; }
        private PluginCommandManager<Plugin> CommandManager;
        private PluginUI ui;

        public static bool FirstRun = true;
        public static bool BreakLoop = true;

        public static readonly Queue<Func<bool>> actionQueue = new();
        private readonly Stopwatch sw = new();
        private static uint Delay = 0;
        public static bool IsDebug = true;

        public Plugin(IDalamudPluginInterface pluginInterface, IChatGui chat, IPartyList partyList, ICommandManager commands, ISigScanner sigScanner)
        {
            PluginInterface = pluginInterface;
            PartyList = partyList;
            Chat = chat;
            SigScanner = sigScanner;

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

            if (!Directory.Exists("graphical-presets")) { Directory.CreateDirectory("graphical-presets"); }

            // Load all of our commands
            CommandManager = new PluginCommandManager<Plugin>(this, commands);

            Functions.GetChatSignatures(sigScanner);

            Framework.Update += OnFrameworkUpdate;

            GameConfig.Changed += ConfigChange;
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

        public void ConfigChange(object? sender, ConfigChangeEvent e)
        {
            if (e.Option.ToString() != "CameraZoom" && e.Option.ToString() != "ChatType")
            {
                Chat.Print("Setting: " + e.Option.ToString());
                GameConfig.System.TryGetUInt(e.Option.ToString(), out uint TestVariable);
                Chat.Print("Changed to: " + TestVariable.ToString());
            }
        }

        [Command("/gconfig")]
        [HelpMessage("Shows GraphicsConfig configuration options")]
        public void ShowTwitchOptions(string command, string args)
        {
            ui.IsVisible = !ui.IsVisible;
        }

        [Command("/glist")]
        [HelpMessage("List all graphical presets")]
        public unsafe void ListPresets(string command, string args)
        {
            //Get the current settings and save it to a JSON, overwriting if they specify /y or something
            
        }

        [Command("/gsave")]
        [HelpMessage("Save current settings as a graphical preset")]
        public unsafe void SavePreset(string command, string args)
        {
            if(String.IsNullOrWhiteSpace(args)) { Chat.PrintError("Error: Please provide a filename for the preset."); return; }
            //Get the current settings and save it to a JSON, overwriting if they specify /y or something
            //if(args.Contains(" ")) { Chat.PrintError("Error: Presets cannot have spaces in their names."); return; }
            //if (args.Contains(" /y"))
            WriteGraphicalPreset(args);
        }

        [Command("/gload")]
        [HelpMessage("Load previously saved graphical preset")]
        public unsafe void LoadPreset(string command, string args)
        {
            //Make a new configuration object, load the JSON object from the file as a <GraphicalConfiguration>
            //Then apply it to the current thing
            ApplyConfig(args);
        }

        [Command("/gtest")]
        [HelpMessage("Does stuff")]
        public async void TestSettings(string command, string args)
        {
            //switch (GetSetting(GraphicalConfigurationStrings.GraphicsRezoUpscaleType))
            //{
            //    case GraphicsUpscaling.AMD_FSR:
            //        SaveSetting(GraphicalConfigurationStrings.GraphicsRezoUpscaleType, GraphicsUpscaling.NVIDIA_DLSS);
            //        break;
            //    case GraphicsUpscaling.NVIDIA_DLSS:
            //        SaveSetting(GraphicalConfigurationStrings.GraphicsRezoUpscaleType, GraphicsUpscaling.AMD_FSR);
            //        break;
            //}
            GetCurrentConfig();
        }

        public static void ApplySetting(string SettingToModify, uint NewValue)
        {
            //if (IsDebug) Chat.Print("Changing " + SettingToModify + " to " + NewValue);
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
                PresetName = "graphical-presets\\" + PresetName.ToLower() + ".json";
                if (!File.Exists(PresetName))
                {
                    Chat.PrintError("Couldn't find a graphical preset named \"" + PresetName + "\".");
                    return null;
                    //GraphicalConfiguration NewData = new GraphicalConfiguration();
                    //string NewJSON = JsonConvert.SerializeObject(NewData, Newtonsoft.Json.Formatting.Indented);

                    ////write string to file
                    //System.IO.File.WriteAllText(PresetName, NewJSON);
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
                PresetName = "graphical-presets\\" + PresetName.ToLower() + ".json";
                //if (!File.Exists(PresetName))
                //{
                    GraphicalConfiguration CurrentConfig = GetCurrentConfig();
                    string NewJSON = JsonConvert.SerializeObject(CurrentConfig, Newtonsoft.Json.Formatting.Indented);

                    //write string to file
                    System.IO.File.WriteAllText(PresetName, NewJSON);
                    Chat.Print("[Graphics Config] Preset saved as \"" + PresetName + "\".");
                    return true;
                //}
                //else if(File.Exists(PresetName) && !Overwrite)
                //{
                //    Chat.PrintError("[Graphics Config] There is already a preset with the name \"" + PresetName + "\". If you want to overwrite it, make sure to include /y at the end of the command.");
                //    return false;
                //}
                //else if (File.Exists(PresetName) && Overwrite)
                //{
                //    GraphicalConfiguration CurrentConfig = GetCurrentConfig();
                //    string NewJSON = JsonConvert.SerializeObject(CurrentConfig, Newtonsoft.Json.Formatting.Indented);

                //    //write string to file
                //    System.IO.File.WriteAllText(PresetName, NewJSON);
                //    Chat.Print("[Graphics Config] Preset saved as \"" + PresetName + "\".");
                //    return true;
                //}
                //return false;
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
                    //Print("Saving " + property.Name);
                    property.SetValue(CurrentConfig, GetSetting(property.Name));
                }
                //foreach (var prop in CurrentConfig.GetType().GetProperties())
                //{
                //    Chat.Print("Saving " + prop.Name);
                //    prop.SetValue(CurrentConfig, 0);
                //}
                //Chat.Print("1");
                //foreach (var prop in CurrentConfig.GetType().GetProperties())
                //{
                //    Print(prop.Name + ": " + prop.GetValue(CurrentConfig).ToString());
                //}
                return CurrentConfig;
            }
            catch(Exception e)
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

                PropertyInfo[] properties = typeof(GraphicalConfiguration).GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    //Print("Loading/Setting " + property.Name);
                    ApplySetting(property.Name, (uint)property.GetValue(CurrentConfig));
                }

                

                //foreach (var prop in CurrentConfig.GetType().GetProperties())
                //{
                //    Chat.Print(prop.Name + ": " + prop.GetValue(CurrentConfig).ToString());
                //}
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