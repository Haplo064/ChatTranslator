using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using IvanAkcheurov.NTextCat.Lib;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Num = System.Numerics;
using System.Reflection;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;


namespace ChatTranslator
{
    public class UI
    {
        public ChatTranslator trn;
        public Handy handy;
        public void TranslatorConfigUI()
        {
            if (trn.config)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(500, 500), new Num.Vector2(1920, 1080));
                ImGui.Begin("Chat Translator Config", ref trn.config);

                if (ImGui.Combo("Language", ref trn.languageInt, trn.languages, trn.languages.Length))
                {
                    handy.SaveConfig();
                }
                if (ImGui.Combo("Mode", ref trn.tranMode, trn.tranModeOptions, trn.tranModeOptions.Length))
                {
                    handy.SaveConfig();
                }
                var txtclr = BitConverter.GetBytes(trn.textColour[0].Choice);
                if (ImGui.ColorButton($"Translated Text Colour", new Num.Vector4(
                    (float)txtclr[3] / 255,
                    (float)txtclr[2] / 255,
                    (float)txtclr[1] / 255,
                    (float)txtclr[0] / 255)))
                {
                    trn.chooser = trn.textColour[0];
                    trn.picker = true;
                }
                //ImGui.Text($"{languageInt}");
                int i = 0;
                ImGui.Text("Enabled channels:");
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Which chat channels to translate."); }
                ImGui.Columns(2);



                foreach (var e in (XivChatType[])Enum.GetValues(typeof(XivChatType)))
                {
                    if (trn.yesno[i])
                    {
                        var enabled = trn._channels.Contains(e);
                        if (ImGui.Checkbox($"{e}", ref enabled))
                        {
                            if (enabled) trn._channels.Add(e);
                            else trn._channels.Remove(e);
                            handy.SaveConfig();
                        }

                        ImGui.NextColumn();
                    }

                    i++;
                }

                ImGui.Columns(1);

                if (ImGui.Button("Save and Close Config"))
                {
                    handy.SaveConfig();
                    trn.config = false;
                }

                ImGui.End();
            }

            if (trn.picker)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(320, 440), new Num.Vector2(640, 880));
                ImGui.Begin("UIColor Picker", ref trn.picker);
                ImGui.Columns(10, "##columnsID", false);
                foreach (var z in trn.uiColours)
                {
                    var temp = BitConverter.GetBytes(z.UIForeground);
                    if (ImGui.ColorButton(z.RowId.ToString(), new Num.Vector4(
                        (float)temp[3] / 255,
                        (float)temp[2] / 255,
                        (float)temp[1] / 255,
                        (float)temp[0] / 255)))
                    {
                        trn.chooser.Choice = z.UIForeground;
                        trn.chooser.Option = z.RowId;
                        trn.picker = false;
                        handy.SaveConfig();
                    }

                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
                ImGui.End();
            }
        }
    }
}