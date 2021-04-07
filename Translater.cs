using System;
using System.Linq;
using System.Collections.Generic;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Plugin;
using IvanAkcheurov.NTextCat.Lib;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace ChatTranslator
{
    public partial class ChatTranslator
    {
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
                return "LOOP";
            }
            return trans.ToString();
        }

        public bool Tran(XivChatType type, SeString messageSeString, string senderName)
        {
            var fmessage = new SeString(new List<Payload>());
            fmessage.Append(messageSeString);

            var run = true;
            if (messageSeString.Payloads.Count < 2) { }
            else if (messageSeString.Payloads[0] == new UIForegroundPayload(pluginInterface.Data, 48) && messageSeString.Payloads[1] == new UIForegroundPayload(pluginInterface.Data, 0))
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
                    { i += 7; continue; }
                    if (messageSeString.Payloads[i].Type == PayloadType.Player)
                    { i += 2; continue; }
                    if (messageSeString.Payloads[i].Type == PayloadType.Status)
                    { i += 10; continue; }
                    if (messageSeString.Payloads[i].Type == PayloadType.RawText)
                    {
                        //PluginLog.Log("Type PASS");
                        var text = (TextPayload)messageSeString.Payloads[i];
                        var translatedText = Translate(text.Text);
                        if (translatedText != "LOOP")
                        {
                            messageSeString.Payloads[i] = new TextPayload(translatedText);
                            messageSeString.Payloads.Insert(i, new UIForegroundPayload(pluginInterface.Data, (ushort)textColour[0].Option));
                            messageSeString.Payloads.Insert(i, new UIForegroundPayload(pluginInterface.Data, 0));

                            if (i + 3 < messageSeString.Payloads.Count)
                                messageSeString.Payloads.Insert(i + 3, new UIForegroundPayload(pluginInterface.Data, 0));
                            else
                                messageSeString.Payloads.Append(new UIForegroundPayload(pluginInterface.Data, 0));
                            i += 2;
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
                        fmessage.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 48));
                        fmessage.Append(tmessage);
                        fmessage.Append(messageSeString);
                        PrintChat(type, senderName, fmessage);
                        return true;
                    }
                    else // Replace or Additional
                    {
                        messageSeString.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 0));
                        messageSeString.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 48));
                        PrintChat(type, senderName, messageSeString);
                        return true;
                    }
                }
                return false;
            }
            return false;
        }
    }
}
