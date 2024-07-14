using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game;
using Dalamud.Plugin;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using TwitchLib.Api.Helix;
using TwitchLib.Client.Models;
using Veda;
using Dalamud.Configuration;
using static Lumina.Data.Files.Pcb.PcbListFile;
using Dalamud.Interface.Utility.Raii;
using System.Reflection.Emit;
using Microsoft.Extensions.Primitives;
using static FFXIVClientStructs.FFXIV.Client.LayoutEngine.LayoutManager;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using static FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkHistory.Delegates;

namespace GraphicsConfig
{
    public class PluginUI
    {
        public bool IsVisible;
        public bool ShowSupport;
        public string CurrentSelection = "No preset";

        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("Graphics Config", ref IsVisible, ImGuiWindowFlags.AlwaysAutoResize))
                return;

            List<string> Presets = new List<string>{ "None" };
            Presets.AddRange(Plugin.GetPresets());

            ImGui.Text("Saving a preset: \"/gsave PresetName\"\nLoading a preset: \"/gload PresetName\"\nListing presets: \"/glist\"");
            ImGui.Text("The plugin will enable the specified presets\nwhen the condition begins (Like entering combat\nor a cutscene) and revert to the default preset\nafter (like killing the enemy or finishing the\ncutscene). If you don't want to use a preset for\nany of these, just select none for each condition.");
            
            ImGui.Text("Default:");
            ImGui.SameLine();
            ImGui.Indent(130);
            DrawComboBox("DefaultPreset", Plugin.PluginConfig.DefaultPreset, 150, out Plugin.PluginConfig.DefaultPreset, Presets);
            ImGui.Unindent(130);
            
            ImGui.Text("In Duty:");
            ImGui.SameLine();
            ImGui.Indent(130);
            DrawComboBox("InDutyPreset", Plugin.PluginConfig.InDutyPreset, 150, out Plugin.PluginConfig.InDutyPreset, Presets);
            ImGui.Unindent(130);
            
            ImGui.Text("Crafting:");
            ImGui.SameLine();
            ImGui.Indent(130);
            DrawComboBox("CraftingPreset", Plugin.PluginConfig.CraftingPreset, 150, out Plugin.PluginConfig.CraftingPreset, Presets);
            ImGui.Unindent(130);
            
            ImGui.Text("Editing Character:");
            ImGui.SameLine();
            ImGui.Indent(130);
            DrawComboBox("EditingCharacterPreset", Plugin.PluginConfig.EditingCharacterPreset, 150, out Plugin.PluginConfig.EditingCharacterPreset, Presets);
            ImGui.Unindent(130);
            
            ImGui.Text("Gathering:");
            ImGui.SameLine();
            ImGui.Indent(130);
            DrawComboBox("GatheringPreset", Plugin.PluginConfig.GatheringPreset, 150, out Plugin.PluginConfig.GatheringPreset, Presets);
            ImGui.Unindent(130);
            
            ImGui.Text("In Combat:");
            ImGui.SameLine();
            ImGui.Indent(130);
            DrawComboBox("CombatPreset", Plugin.PluginConfig.CombatPreset, 150, out Plugin.PluginConfig.CombatPreset, Presets);
            ImGui.Unindent(130);
            
            ImGui.Text("Bard Performance:");
            ImGui.SameLine();
            ImGui.Indent(130);
            DrawComboBox("PerformancePreset", Plugin.PluginConfig.PerformancePreset, 150, out Plugin.PluginConfig.PerformancePreset, Presets);
            ImGui.Unindent(130);
            
            ImGui.Text("Cutscene or Gpose:");
            ImGui.SameLine();
            ImGui.Indent(130);
            DrawComboBox("WatchingCutscenePreset", Plugin.PluginConfig.WatchingCutscenePreset, 150, out Plugin.PluginConfig.WatchingCutscenePreset, Presets);
            ImGui.Unindent(130);
            
            ImGui.Text("Device unplugged:");
            ImGui.SameLine();
            ImGui.Indent(130);
            DrawComboBox("UnpluggedPreset", Plugin.PluginConfig.UnpluggedPreset, 150, out Plugin.PluginConfig.UnpluggedPreset, Presets);
            ImGui.Unindent(130);

            if (ImGui.Button("Save"))
            {
                Plugin.PluginConfig.Save();
                this.IsVisible = false;
            }

            ImGui.SameLine();
            ImGui.Indent(100);

            if (ImGui.Button("Want to help support my work?"))
            {
                ShowSupport = !ShowSupport;
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Click me!"); }

            if (ShowSupport)
            {
                ImGui.Indent(-100);
                ImGui.Text("Here are the current ways you can support the work I do.\nEvery bit helps, thank you! Have a great day!");
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.19f, 0.52f, 0.27f, 1));
                if (ImGui.Button("Donate via Paypal"))
                {
                    Functions.OpenWebsite("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=QXF8EL4737HWJ");
                }
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.95f, 0.39f, 0.32f, 1));
                if (ImGui.Button("Become a Patron"))
                {
                    Functions.OpenWebsite("https://www.patreon.com/bePatron?u=5597973");
                }
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.25f, 0.67f, 0.87f, 1));
                if (ImGui.Button("Support me on Ko-Fi"))
                {
                    Functions.OpenWebsite("https://ko-fi.com/Y8Y114PMT");
                }
                ImGui.PopStyleColor();
            }
            ImGui.End();
        }

        private static unsafe void DrawClippedList(int itemCount, string preview, IReadOnlyList<string> list, out string result)
        {
            result = preview;
            var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            clipper.Begin(itemCount, ImGui.GetTextLineHeightWithSpacing());

            var clipperBreak = false;
            while (clipper.Step())
            {
                if (clipperBreak)
                {
                    break;
                }
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    if (i >= itemCount)
                    {
                        clipperBreak = true;
                        break;
                    }
                    var item = list[i];
                    if (!ImGui.Selectable(item + "##" + i.ToString(CultureInfo.CurrentCulture)))
                    {
                        continue;
                    }
                    result = item;
                    ImGui.CloseCurrentPopup();
                }
            }
            clipper.End();
            clipper.Destroy();
        }

        private void DrawComboBox<T>(string label, string current, float width, out string result, IReadOnlyCollection<T> list) where T : notnull
        {
            ImGui.SetNextItemWidth(width);
            using var combo = ImRaii.Combo("##Combo" + label, current);
            result = current;
            if (!combo)
            {
                return;
            }
            var tempList = list.Select(item => item.ToString()!).ToList();
            if (tempList.Count > 0)
            {
                tempList.Sort(2, tempList.Count - 2, StringComparer.InvariantCulture);
            }
            //if (filter)
            //{
            //    ImGui.SetNextItemWidth(width);
            //    ImGui.InputTextWithHint("##Filter" + label, "Filter..", ref filterTemp, 30);
            //    tempList = tempList.Where(item => item.Contains(filterTemp, StringComparison.OrdinalIgnoreCase)).ToList();
            //}
            var itemCount = tempList.Count;
            var height = ImGui.GetTextLineHeightWithSpacing() * Math.Min(itemCount + 1.5f, 8);
            height += itemCount > 0 ? -ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y - ImGui.GetStyle().FramePadding.Y : 0;
            using var listChild = ImRaii.Child("###child" + label, new Vector2(width, height));
            DrawClippedList(itemCount, current, tempList, out result);
        }
    }
}