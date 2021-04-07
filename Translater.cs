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
using ChatTranslator;

namespace ChatTranslator
{
    public class Translater
    {
        public ChatTranslator trn;
        public Handy handy;
        public UI ui;
        // NCat
        public static RankedLanguageIdentifierFactory factory = new RankedLanguageIdentifierFactory();
        public static RankedLanguageIdentifier identifier = factory.Load(Path.Combine(AssemblyDirectory, "Wiki82.profile.xml"));

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

        public string Lang(string message)
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

        public string Translate(string text)
        {
            //PluginLog.Log($"Translate: {text}");

            string lang = trn.codes[trn.languageInt];
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

        public bool Tran(XivChatType type, string messageString, SeString messageSeString, string senderName)
        {
            var fmessage = new SeString(new List<Payload>());
            fmessage.Append(messageSeString);

            var run = true;
            if (messageSeString.Payloads.Count < 2) { }
            else if (messageSeString.Payloads[0] == new UIForegroundPayload(trn.pluginInterface.Data, 48) && messageSeString.Payloads[1] == new UIForegroundPayload(trn.pluginInterface.Data, 0))
            {
                PluginLog.Log("Caught loop B");
                run = false;
            }

            if (run)
            {
                bool tdone = false;
                uint currentColour = 0;
                for (int i = 0; i < messageSeString.Payloads.Count; i++)
                {
                    if (messageSeString.Payloads[i].Type == PayloadType.UIForeground)
                    {
                        var pl = (UIForegroundPayload)messageSeString.Payloads[i];
                        currentColour = pl.UIColor.UIForeground;
                    }
                    //PluginLog.Log($"TYPE: {messageSeString.Payloads[i].Type}");
                    if (messageSeString.Payloads[i].Type == PayloadType.MapLink || messageSeString.Payloads[i].Type == PayloadType.Item || messageSeString.Payloads[i].Type == PayloadType.Quest)
                    {
                        i += 7;
                        currentColour = 0;
                        continue;
                    }
                    if (messageSeString.Payloads[i].Type == PayloadType.Player)
                    {
                        i += 2;
                        currentColour = 0;
                        continue;
                    }
                    if (messageSeString.Payloads[i].Type == PayloadType.Status)
                    {
                        i += 10;
                        currentColour = 0;
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
                            messageSeString.Payloads.Insert(i, new UIForegroundPayload(trn.pluginInterface.Data, (ushort)trn.textColour[0].Option));
                            messageSeString.Payloads.Insert(i, new UIForegroundPayload(trn.pluginInterface.Data, 0));

                            if (i + 3 < messageSeString.Payloads.Count)
                            {
                                messageSeString.Payloads.Insert(i + 3, new UIForegroundPayload(trn.pluginInterface.Data, 0));
                            }
                            else
                            {
                                messageSeString.Payloads.Append(new UIForegroundPayload(trn.pluginInterface.Data, 0));
                            }
                            i += 2;
                            tdone = true;
                        }

                    }
                }
                if (tdone)
                {
                    if (trn.tranMode == 0) // Append
                    {
                        var tmessage = new SeString(new List<Payload>());

                        tmessage.Payloads.Add(new TextPayload(" | "));
                        fmessage.Payloads.Insert(0, new UIForegroundPayload(trn.pluginInterface.Data, 0));
                        fmessage.Payloads.Insert(0, new UIForegroundPayload(trn.pluginInterface.Data, 48));
                        fmessage.Append(tmessage);
                        fmessage.Append(messageSeString);
                        handy.PrintChat(type, senderName, fmessage);


                        return true;
                    }
                    else // Replace or Additional
                    {
                        messageSeString.Payloads.Insert(0, new UIForegroundPayload(trn.pluginInterface.Data, 0));
                        messageSeString.Payloads.Insert(0, new UIForegroundPayload(trn.pluginInterface.Data, 48));
                        handy.PrintChat(type, senderName, messageSeString);
                        return true;
                    }
                }
                return false;


            }
            return false;
        }
    }
}
