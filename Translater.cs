﻿using System;
using System.Linq;
using System.Collections.Generic;
 using System.Diagnostics;
 using System.Globalization;
 using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
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
        private static readonly RankedLanguageIdentifierFactory Factory = new RankedLanguageIdentifierFactory();
        private static readonly RankedLanguageIdentifier Identifier = Factory.Load(Path.Combine(AssemblyDirectory, "Wiki82.profile.xml"));

        private static string AssemblyDirectory
        {
            get
            {
                var codeBase = typeof(ChatTranslator).Assembly.CodeBase;
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
            var trans = ((JArray)parsed["sentences"])?[0]["trans"];
            var src = ((JValue)parsed["src"]);
            Debug.Assert(trans != null, nameof(trans) + " != null");
            if (src != null && (src.ToString(CultureInfo.InvariantCulture) == lang || trans.ToString() == text))
            {
                return "LOOP";
            }
            return trans.ToString();
        }

        private bool Tran(XivChatType type, SeString messageSeString, string senderName)
        {
            var fmessage = new SeString(new List<Payload>());
            fmessage.Append(messageSeString);

            var run = true;
            if (messageSeString.Payloads.Count < 2) { }
            else if (messageSeString.Payloads[0] == new UIForegroundPayload(_pluginInterface.Data, 48) && messageSeString.Payloads[1] == new UIForegroundPayload(_pluginInterface.Data, 0))
            {
                PluginLog.Log("Caught loop B");
                run = false;
            }

            if (!run) return false;
            var tdone = false;

            for (var i = 0; i < messageSeString.Payloads.Count; i++)
            {
                if (messageSeString.Payloads[i].Type == PayloadType.MapLink ||
                    messageSeString.Payloads[i].Type == PayloadType.Item ||
                    messageSeString.Payloads[i].Type == PayloadType.Quest)
                {
                    i += 7;
                    continue;
                }
                else if (messageSeString.Payloads[i].Type == PayloadType.Player)
                {
                    i += 2;
                    continue;
                }
                else if (messageSeString.Payloads[i].Type == PayloadType.Status)
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
                messageSeString.Payloads.Insert(i, new UIForegroundPayload(_pluginInterface.Data, (ushort)_textColour[0].Option));
                messageSeString.Payloads.Insert(i, new UIForegroundPayload(_pluginInterface.Data, 0));

                if (i + 3 < messageSeString.Payloads.Count)
                    messageSeString.Payloads.Insert(i + 3, new UIForegroundPayload(_pluginInterface.Data, 0));
                else
                    messageSeString.Payloads.Append(new UIForegroundPayload(_pluginInterface.Data, 0));
                i += 2;
                tdone = true;
            }

            if (!tdone) return false;
            if (_tranMode == 0) // Append
            {
                var tmessage = new SeString(new List<Payload>());
                tmessage.Payloads.Add(new TextPayload(" | "));
                fmessage.Payloads.Insert(0, new UIForegroundPayload(_pluginInterface.Data, 0));
                fmessage.Payloads.Insert(0, new UIForegroundPayload(_pluginInterface.Data, 48));
                fmessage.Append(tmessage);
                fmessage.Append(messageSeString);
                PrintChat(type, senderName, fmessage);
                return true;
            }
            else // Replace or Additional
            {
                messageSeString.Payloads.Insert(0, new UIForegroundPayload(_pluginInterface.Data, 0));
                messageSeString.Payloads.Insert(0, new UIForegroundPayload(_pluginInterface.Data, 48));
                if (_oneChan && _tranMode == 2)
                { PrintChat(_order[_oneInt], senderName, messageSeString); }
                else { PrintChat(type, senderName, messageSeString); }

                return true;
            }
        }
    }
}
