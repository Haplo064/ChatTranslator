using System;
using Dalamud.Game.Chat;
using ImGuiNET;
using Num = System.Numerics;

namespace ChatTranslator
{
    public partial class ChatTranslator
    {
        public void TranslatorConfigUI()
        {
            if (config)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(500, 500), new Num.Vector2(1920, 1080));
                ImGui.Begin("Chat Translator Config", ref config);

                if (ImGui.Combo("Language", ref languageInt, languages, languages.Length))
                {
                    SaveConfig();
                }
                if (ImGui.Combo("Mode", ref tranMode, tranModeOptions, tranModeOptions.Length))
                {
                    SaveConfig();
                }
                var txtclr = BitConverter.GetBytes(textColour[0].Choice);
                if (ImGui.ColorButton($"Translated Text Colour", new Num.Vector4(
                    (float)txtclr[3] / 255,
                    (float)txtclr[2] / 255,
                    (float)txtclr[1] / 255,
                    (float)txtclr[0] / 255)))
                {
                    chooser = textColour[0];
                    picker = true;
                }
                int i = 0;
                ImGui.Text("Enabled channels:");
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Which chat channels to translate."); }
                ImGui.Columns(2);

                foreach (var e in (XivChatType[])Enum.GetValues(typeof(XivChatType)))
                {
                    if (yesno[i])
                    {
                        var enabled = _channels.Contains(e);
                        if (ImGui.Checkbox($"{e}", ref enabled))
                        {
                            if (enabled) _channels.Add(e);
                            else _channels.Remove(e);
                            SaveConfig();
                        }
                        ImGui.NextColumn();
                    }
                    i++;
                }

                ImGui.Columns(1);
                if (ImGui.Button("Save and Close Config"))
                {
                    SaveConfig();
                    config = false;
                }
                ImGui.End();
            }

            if (picker)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(320, 440), new Num.Vector2(640, 880));
                ImGui.Begin("UIColor Picker", ref picker);
                ImGui.Columns(10, "##columnsID", false);
                foreach (var z in uiColours)
                {
                    var temp = BitConverter.GetBytes(z.UIForeground);
                    if (ImGui.ColorButton(z.RowId.ToString(), new Num.Vector4(
                        (float)temp[3] / 255,
                        (float)temp[2] / 255,
                        (float)temp[1] / 255,
                        (float)temp[0] / 255)))
                    {
                        chooser.Choice = z.UIForeground;
                        chooser.Option = z.RowId;
                        picker = false;
                        SaveConfig();
                    }
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
                ImGui.End();
            }
        }
    }
}