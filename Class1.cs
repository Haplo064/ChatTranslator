//TODO
//Allow for echo channel -- DONE
//Single colour option for translated text -- DONE
//Fix colour 'bleeding' into next line -- DONE
//Fix not loading language selection -- DONE

//Append Translation (optional) -- TEST
//Replace with Translation (optional) -- TEST
//Send all translations to ECHO (optional)

//Option to only translate one language

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
    public class ChatTranslator : IDalamudPlugin
    {
        public string Name => "Chat Translator";
        private DalamudPluginInterface pluginInterface;
        public Config Configuration;
        public bool enable = true;
        public bool config = false;
        public string language = "en";
        public int languageInt = 16;
        public UIColorPick[] textColour;
        public UIColorPick chooser;
        public bool picker;
        public int tranMode = 0;
        public string[] tranModeOptions = { "Append", "Replace", "Additional" };

        // NCat
        public static RankedLanguageIdentifierFactory factory = new RankedLanguageIdentifierFactory();
        public static RankedLanguageIdentifier identifier = factory.Load(Path.Combine(AssemblyDirectory, "Wiki82.profile.xml"));
        public Lumina.Excel.ExcelSheet<UIColor> uiColours;

        public List<XivChatType> _channels = new List<XivChatType>();

        public List<XivChatType> order = new List<XivChatType>
        {
            XivChatType.None, XivChatType.Debug, XivChatType.Urgent, XivChatType.Notice, XivChatType.Say,
            XivChatType.Shout, XivChatType.TellOutgoing, XivChatType.TellIncoming, XivChatType.Party, XivChatType.Alliance,
            XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4, XivChatType.Ls5,
            XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8, XivChatType.FreeCompany, XivChatType.NoviceNetwork,
            XivChatType.CustomEmote, XivChatType.StandardEmote, XivChatType.Yell, XivChatType.CrossParty, XivChatType.PvPTeam,
            XivChatType.CrossLinkShell1, XivChatType.Echo, XivChatType.SystemMessage, XivChatType.SystemError, XivChatType.GatheringSystemMessage,
            XivChatType.ErrorMessage, XivChatType.RetainerSale, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4,
            XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6, XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8
        };

        public bool[] yesno = {
            false, false, false, false, true,
            true, false, true, true, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, false, true, true, true,
            true, true, false, false, false,
            false, false, true, true, true,
            true, true, true, true
        };

        public string[] codes = {
            "af", "an", "ar", "az", "be_x_old",
            "bg", "bn", "br", "bs",
            "ca", "ceb", "cs", "cy", "da",
            "de", "el", "en", "eo", "es",
            "et", "eu", "fa", "fi", "fr",
            "gl", "he", "hi", "hr", "ht",
            "hu", "hy", "id", "is", "it",
            "ja", "jv", "ka", "kk", "ko",
            "la", "lb", "lt", "lv",
            "mg", "mk", "ml", "mr", "ms",
            "new", "nl", "nn", "no", "oc",
            "pl", "pt", "ro", "roa_rup",
            "ru", "sk", "sl",
            "sq", "sr", "sv", "sw", "ta",
            "te", "th", "tl", "tr", "uk",
            "ur", "vi", "vo", "war", "zh",
            "zh_classical", "zh_yue"
        };

        public string[] languages = {
            "Afrikaans", "Aragonese", "Arabic", "Azerbaijani", "Belarusian",
            "Bulgarian", "Bengali", "Breton", "Bosnian",
            "Catalan; Valencian", "Cebuano", "Czech", "Welsh", "Danish",
            "German", "Greek, Modern", "English", "Esperanto", "Spanish; Castilian",
            "Estonian", "Basque", "Persian", "Finnish", "French",
            "Galician", "Hebrew", "Hindi", "Croatian", "Haitian; Haitian Creole",
            "Hungarian", "Armenian", "Indonesian", "Icelandic", "Italian",
            "Japanese", "Javanese", "Georgian", "Kazakh", "Korean",
            "Latin", "Luxembourgish; Letzeburgesch", "Lithuanian", "Latvian",
            "Malagasy", "Macedonian", "Malayalam", "Marathi", "Malay",
            "Nepal Bhasa; Newari", "Dutch; Flemish", "Norwegian Nynorsk; Nynorsk, Norwegian", "Norwegian", "Occitan (post 1500)",
            "Polish", "Portuguese", "Romanian; Moldavian; Moldovan", "Romance languages",
            "Russian", "Slovak", "Slovenian",
            "Albanian", "Serbian", "Swedish", "Swahili", "Tamil",
            "Telugu", "Thai", "Tagalog", "Turkish", "Ukrainian",
            "Urdu", "Vietnamese", "Volapük", "Waray", "Chinese",
            "Chinese Classical", "Chinese yue"
        };

        public static string AssemblyDirectory
        {
            get
            {
                var codeBase = typeof(ChatTranslator).Assembly.CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public void SaveConfig()
        {
            Configuration.Lang = languageInt;
            Configuration.Channels = _channels;
            pluginInterface.SavePluginConfig(Configuration);
        }
        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            Configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();
            this.pluginInterface.Framework.Gui.Chat.OnChatMessage += Chat_OnChatMessage;
            this.pluginInterface.UiBuilder.OnBuildUi += TranslatorConfigUI;
            this.pluginInterface.UiBuilder.OnOpenConfigUi += TranslatorConfig;
            this.pluginInterface.CommandManager.AddHandler("/trn", new CommandInfo(Command)
            {
                HelpMessage = "Opens the Chat Translator config menu"
            });
            uiColours = pluginInterface.Data.Excel.GetSheet<UIColor>();
            _channels = Configuration.Channels;
            textColour = Configuration.TextColour;
            languageInt = Configuration.Lang;
        }
        public class PluginConfiguration : IPluginConfiguration
        {
            public int Version { get; set; } = 0;
        }
        public void Dispose()
        {
            pluginInterface.Framework.Gui.Chat.OnChatMessage -= Chat_OnChatMessage;
            pluginInterface.UiBuilder.OnBuildUi -= TranslatorConfigUI;
            pluginInterface.UiBuilder.OnOpenConfigUi -= TranslatorConfig;
            pluginInterface.CommandManager.RemoveHandler("/trn");
        }
        private void Command(string command, string arguments) => config = true;

        // What to do when plugin install config button is pressed
        private void TranslatorConfig(object Sender, EventArgs args) => config = true;
        private void TranslatorConfigUI()
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
                //ImGui.Text($"{languageInt}");
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

        private void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            try
            {
                bool tempHandled = isHandled;
                if (!isHandled)
                {
                    if (_channels.Contains(type))
                    {
                        string PName = pluginInterface.ClientState.LocalPlayer.Name;
                        if (sender.Payloads.Count > 0)
                        {
                            if (sender.Payloads[0].Type == PayloadType.Player)
                            {
                                var pPayload = (PlayerPayload)sender.Payloads[0];
                                PName = pPayload.PlayerName;
                            }
                            if (sender.Payloads[0].Type == PayloadType.Icon && sender.Payloads[1].Type == PayloadType.Player)
                            {
                                var pPayload = (PlayerPayload)sender.Payloads[1];
                                PName = pPayload.PlayerName;
                            }
                        }
                        if (type == XivChatType.StandardEmote || type == XivChatType.CustomEmote)
                        {
                            if (message.Payloads[0].Type == PayloadType.Player)
                            {
                                var pPayload = (PlayerPayload)message.Payloads[0];
                                PName = pPayload.PlayerName;
                            }
                        }

                        if (enable)
                        {
                            var run = true;
                            if (message.Payloads.Count < 2) { }
                            else if (message.Payloads[0].Type == PayloadType.UIForeground && message.Payloads[1].Type == PayloadType.UIForeground)
                            {
                                PluginLog.Log("Caught loop A");
                                run = false;
                            }

                            if (run)
                            {
                                foreach (Payload pl in message.Payloads)
                                {
                                    PluginLog.Log($"{pl.Type}");
                                    if (pl.Type == PayloadType.UIForeground)
                                    {
                                        var xx = (UIForegroundPayload)pl;
                                        PluginLog.Log($"RGB: {xx.RGB}");
                                    }
                                }
                                String messageString = message.TextValue;
                                String predictedLanguage = Lang(messageString);
                                PluginLog.Log($"PRED LANG: {predictedLanguage}");
                                var fmessage = new SeString(new List<Payload>());
                                fmessage.Append(message);

                                if (predictedLanguage != codes[languageInt])
                                {
                                    bool rawExists = false;
                                    foreach (Payload payload in message.Payloads)
                                    {
                                        if (payload.Type == PayloadType.RawText)
                                        {
                                            rawExists = true;
                                            break;
                                        }
                                    }
                                    if (rawExists)
                                    {
                                        if (tranMode != 2) // is it Replace (1) or append (0)
                                        {
                                            isHandled = true;
                                        }
                                        var t = Task.Run(() =>
                                        {
                                            if (!Tran(type, messageString, fmessage, PName))
                                            {
                                                PluginLog.Log("FALSE so printChat");
                                                fmessage.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 0));
                                                fmessage.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 999));
                                                PrintChat(type, PName, fmessage);
                                            };
                                        });

                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PluginLog.LogError($"Chat Translator Error: {e}");
            }
        }


        public bool Tran(XivChatType type, string messageString, SeString messageSeString, string senderName)
        {
            var fmessage = new SeString(new List<Payload>());
            fmessage.Append(messageSeString);

            var run = true;
            if (messageSeString.Payloads.Count < 2) { }
            else if (messageSeString.Payloads[0] == new UIForegroundPayload(pluginInterface.Data, 999) && messageSeString.Payloads[1] == new UIForegroundPayload(pluginInterface.Data, 0))
            {
                PluginLog.Log("Caught loop B");
                run = false;
            }

            if (run)
            {
                bool tdone = false;
                for (int i = 0; i < messageSeString.Payloads.Count; i++)
                {
                    //PluginLog.Log($"TYPE: {messageSeString.Payloads[i].Type}");
                    if (messageSeString.Payloads[i].Type == PayloadType.MapLink || messageSeString.Payloads[i].Type == PayloadType.Item || messageSeString.Payloads[i].Type == PayloadType.Quest)
                    {
                        i += 7;
                        continue;
                    }
                    if (messageSeString.Payloads[i].Type == PayloadType.Player)
                    {
                        i += 2;
                        continue;
                    }
                    if (messageSeString.Payloads[i].Type == PayloadType.Status)
                    {
                        i += 10;
                        continue;
                    }
                    if (messageSeString.Payloads[i].Type == PayloadType.RawText)
                    {
                        //PluginLog.Log("Type PASS");
                        var text = (TextPayload)messageSeString.Payloads[i];
                        var translatedText = Translate(text.Text);
                        if (translatedText != "LOOP")
                        {
                            messageSeString.Payloads[i] = new TextPayload(translatedText);
                            messageSeString.Payloads.Insert(i, new UIForegroundPayload(pluginInterface.Data, (ushort)textColour[0].Option));
                            if (i + 2 < messageSeString.Payloads.Count) { messageSeString.Payloads.Insert(i + 2, new UIForegroundPayload(pluginInterface.Data, 0)); }
                            else { messageSeString.Payloads.Append(new UIForegroundPayload(pluginInterface.Data, 0)); }
                            i++;
                            i++;
                            tdone = true;
                        }

                    }
                }
                if (tdone)
                {
                    if (tranMode == 0) // Append
                    {
                        var tmessage = new SeString(new List<Payload>());

                        tmessage.Payloads.Add(new TextPayload(" | "));
                        fmessage.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 0));
                        fmessage.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 999));
                        fmessage.Append(tmessage);
                        fmessage.Append(messageSeString);
                        PrintChat(type, senderName, fmessage);
                        return true;
                    }
                    else // Replace or Additional
                    {
                        messageSeString.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 0));
                        messageSeString.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 999));
                        PrintChat(type, senderName, messageSeString);
                        return true;
                    }
                }
                return false;


            }
            return false;
        }

        public void PrintChat(XivChatType type, string senderName, SeString messageSeString)
        {
            var chat = new XivChatEntry
            {
                Type = type,
                Name = senderName,
                MessageBytes = messageSeString.Encode()
            };

            pluginInterface.Framework.Gui.Chat.PrintChat(chat);
        }

        public string Translate(string text)
        {
            //PluginLog.Log($"Translate: {text}");

            string lang = codes[languageInt];
            string url = "https://clients5.google.com/translate_a/t?client=dict-chrome-ex&sl=auto&tl=" + lang + "&q=" + text;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.104 Safari/537.36";

            var request_result = request.GetResponse();
            StreamReader reader = new StreamReader(request_result.GetResponseStream());
            var read = reader.ReadToEnd();
            var parsed = JObject.Parse(read);
            var trans = ((JArray)parsed["sentences"])[0]["trans"];
            var src = ((JValue)parsed["src"]);
            if (src.ToString() == lang || trans.ToString() == text)
            {
                //PluginLog.Log("Caught a looper!");
                return "LOOP";
            }
            //PluginLog.Log($"Translate Done: {trans}");
            return trans.ToString();
        }

        public static string Lang(string message)
        {
            var languages = identifier.Identify(message);
            var mostCertainLanguage = languages.FirstOrDefault();

            if (mostCertainLanguage != null)
            {
                //PluginLog.Log("The language of the text is:" + mostCertainLanguage.Item1.Iso639_3);
                //PluginLog.Log(mostCertainLanguage.Item2.ToString());
                return mostCertainLanguage.Item1.Iso639_2T;
            }
            else
                return ("The language couldn’t be identified with an acceptable degree of certainty");
        }

    }

    public class UIColorPick
    {
        public uint Choice { get; set; }
        public uint Option { get; set; }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public List<XivChatType> Channels { get; set; } = new List<XivChatType>();
        public int Lang { get; set; } = 16;
        public UIColorPick[] TextColour { get; set; } =
        {
            new UIColorPick { Choice = 0, Option =0 }
        };
    }
}
