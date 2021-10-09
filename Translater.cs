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
            var languages = Identifier.Identify(message);
            var mostCertainLanguage = languages.FirstOrDefault();
            return mostCertainLanguage != null ? mostCertainLanguage.Item1.Iso639_2T : "The language could not be identified with an acceptable degree of certainty";
        }

        private string Translate(string text)
        {
            var lang = _codes[_languageInt];
            var url = "https://clients5.google.com/translate_a/t?client=dict-chrome-ex&sl=auto&tl=" + lang + "&q=" + text;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.104 Safari/537.36";

            var requestResult = request.GetResponse();
            var reader = new StreamReader(requestResult.GetResponseStream() ?? throw new Exception());
            var read = reader.ReadToEnd();
            var parsed = JObject.Parse(read);
            var sentences = (JArray) parsed["sentences"];
            string trans = "";
            for (int i = 0; i < sentences.Count; i++)
            {
                trans += sentences[i]["trans"].ToString();
            }
            var src = ((JValue)parsed["src"]);
            Debug.Assert(trans != null, nameof(trans) + " != null");
            if (src != null && (src.ToString(CultureInfo.InvariantCulture) == lang || trans.ToString() == text))
            {
                return "LOOP";
            }
            return trans.ToString();
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
                PluginLog.Log("Caught loop B");
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

            if (!tranDone) return messageSeString;
            // Adding to the rolling "last translation" list
            _lastTranslations.Insert(0,cleanMessage.TextValue);
            if(_lastTranslations.Count > 10) _lastTranslations.RemoveAt(10);
            
            if (_tranMode == 0) // Append
            {
                var tranMessage = new SeString(new List<Payload>());
                tranMessage.Payloads.Add(new TextPayload(" | "));
                //originalMessage.Payloads.Insert(0, new UIForegroundPayload(_pluginInterface.Data, 0));
                //originalMessage.Payloads.Insert(0, new UIForegroundPayload(_pluginInterface.Data, 48));
                originalMessage.Append(tranMessage);
                originalMessage.Append(messageSeString);
                return originalMessage;
                //PrintChat(type, senderName, originalMessage);
            }
            return messageSeString;

        }
    }
}
