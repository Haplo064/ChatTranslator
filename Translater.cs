using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Newtonsoft.Json.Linq;
using NTextCat;

namespace ChatTranslator
{
    public partial class ChatTranslator
    {
        // NTextCat
        private static readonly RankedLanguageIdentifierFactory Factory = new();
        private static readonly RankedLanguageIdentifier Identifier = Factory.Load(Path.Combine(AssemblyDirectory, "Wiki82.profile.xml"));

        private static string AssemblyDirectory
        {
            get
            {
                var codeBase = typeof(ChatTranslator).Assembly.Location;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        private static string Lang(string message)
        {
            //PluginLog.Log("LANG FUNC");
            var languages = Identifier.Identify(message);
            //PluginLog.Log("L1");
            var mostCertainLanguage = languages.FirstOrDefault();
            //PluginLog.Log("L2");
            return mostCertainLanguage != null ? mostCertainLanguage.Item1.Iso639_2T : "The language could not be identified with an acceptable degree of certainty";
        }

        private string Translate(string text)
        {
            var lang = _codes[_languageInt];
            var url = "https://clients5.google.com/translate_a/t?client=dict-chrome-ex&sl=auto&tl=" + lang + "&q=" + text;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36";
            //PluginLog.Log("SENDING");
            var requestResult = request.GetResponse();
            //PluginLog.Log("READING");
            var reader = new StreamReader(requestResult.GetResponseStream() ?? throw new Exception());
            var read = reader.ReadToEnd();
            var parsed = JObject.Parse(read);
            var sentences = (JArray) parsed["sentences"];
            string trans = "";
            //PluginLog.Log($"PARSE LOOP ({sentences.Count})");

            var take = 0;
            try
            {
                //PluginLog.Log(sentences[sentences.Count-1]["src_translit"].ToString());
                take++;
                //PluginLog.Log("Translit");
            }
            catch
            {
                //PluginLog.Log("No translit");
            }
            for (int i = 0; i < sentences.Count-take; i++)
            {
                //PluginLog.Log(sentences[i]["trans"].ToString());
                trans += sentences[i]["trans"].ToString();
            }
            //PluginLog.Log("PARSE LOOP DONE");

            JValue src = null;
            try
            {
                src = ((JValue)parsed["src"]);
                //PluginLog.Log(src.ToString());
            }
            catch
            {
                //PluginLog.Log("No src");
            }
            
            Debug.Assert(trans != null, nameof(trans) + " != null");
            if (src != null && (src.ToString(CultureInfo.InvariantCulture) == lang || trans.ToString() == text))
            {
                //PluginLog.Log("LOOP XXX");
                return "LOOP";
            }
            //PluginLog.Log("RETURN");
            return trans;
        }

        private SeString Tran(SeString messageSeString)
        {
            var cleanMessage = new SeString(new List<Payload>());
            cleanMessage.Append(messageSeString);
            var originalMessage = new SeString(new List<Payload>());
            originalMessage.Append(messageSeString);

            var run = true;
            if (messageSeString.Payloads.Count < 2) { }
            else if (messageSeString.Payloads[0] == new UIForegroundPayload(48) && messageSeString.Payloads[1] == new UIForegroundPayload(0))
            {
                //PluginLog.Log("Caught loop B");
                run = false;
            }

            if (!run) return messageSeString;
            var tranDone = false;

            for (var i = 0; i < messageSeString.Payloads.Count; i++)
            {
                if (messageSeString.Payloads[i].Type == PayloadType.MapLink ||
                    messageSeString.Payloads[i].Type == PayloadType.Item ||
                    messageSeString.Payloads[i].Type == PayloadType.Quest)
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

                if (messageSeString.Payloads[i].Type != PayloadType.RawText) continue;
                //PluginLog.Log("Type PASS");
                var text = (TextPayload)messageSeString.Payloads[i];
                var translatedText = Translate(text.Text);
                if (translatedText == "LOOP") continue;
                messageSeString.Payloads[i] = new TextPayload(translatedText);
                messageSeString.Payloads.Insert(i, new UIForegroundPayload((ushort)_textColour[0].Option));
                messageSeString.Payloads.Insert(i, new UIForegroundPayload(0));

                if (i + 3 < messageSeString.Payloads.Count)
                    messageSeString.Payloads.Insert(i + 3, new UIForegroundPayload(0));
                else
                    messageSeString.Payloads.Append(new UIForegroundPayload(0));
                i += 2;
                tranDone = true;
            }

            if (!tranDone)
            {
                messageSeString.Payloads.Insert(0, new UIForegroundPayload(0));
                messageSeString.Payloads.Insert(0, new UIForegroundPayload(48));
                return messageSeString;
            }
            // Adding to the rolling "last translation" list
            _lastTranslations.Insert(0,cleanMessage.TextValue);
            if(_lastTranslations.Count > 10) _lastTranslations.RemoveAt(10);
            
            if (_tranMode == 0) // Append
            {
                var tranMessage = new SeString(new List<Payload>());
                tranMessage.Payloads.Add(new TextPayload(" | "));
                originalMessage.Payloads.Insert(0, new UIForegroundPayload(0));
                originalMessage.Payloads.Insert(0, new UIForegroundPayload(48));
                originalMessage.Append(tranMessage);
                originalMessage.Append(messageSeString);
                return originalMessage;
                //PrintChat(type, senderName, originalMessage);
            }
            return messageSeString;

        }
    }
}
