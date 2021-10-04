﻿using System;
using System.Threading.Tasks;
using System.Collections.Generic;
 using System.Linq;
 using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Dalamud.Logging;

namespace ChatTranslator
{
    public partial class ChatTranslator
    {
        private void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            try
            {
                if (isHandled) return;
                if (!_channels.Contains(type)) return;
                var pName = getName(sender, type, message);
                var run = true;
                
                //Catch already translated messages
                if (message.Payloads.Count < 2) { }
                else if (message.Payloads[0].Type == PayloadType.UIForeground && message.Payloads[1].Type == PayloadType.UIForeground)
                {
                    PluginLog.Log("Caught loop A");
                    run = false;
                }
                if (!run) return;

                
                var messageString = message.TextValue;
                var predictedLanguage = Lang(messageString);
                PluginLog.Log($"PRED LANG: {predictedLanguage}");

                var yes = true;
                var pos = Array.IndexOf(_codes, predictedLanguage);
                //Check for whitelist settings
                if (_whitelist && !_chosenLanguages.Contains(pos))
                { yes = false; }
                //Check for notSelf setting
                if (_notSelf && State.LocalPlayer.Name.TextValue == pName)
                { yes = false; }
                //Check for blacklist settings
                if (_blacklist.Contains(messageString))
                { yes = false; }
                if (predictedLanguage == _codes[_languageInt] || !yes) return;
                
                //Checking if any rawtext to translate exists
                var rawExists = message.Payloads.Any(payload => payload.Type == PayloadType.RawText);
                if (!rawExists) return;
                
                var originalMessage = new SeString(new List<Payload>());
                originalMessage.Append(message);

                var tranSeString = Task.Run(() => Tran(originalMessage));
                
                if (_oneChan && _tranMode == 2)
                {
                    type = _order[_oneInt];
                }
                
                if (_tranMode == 2) // is it Append (0), Replace (1), or additional (2)
                {
                    PrintChat(type, pName, tranSeString.Result);
                }

                message = tranSeString.Result;

            }
            catch (Exception e)
            {
                PluginLog.LogError($"Chat Translator Error: {e}");
            }
        }

        public string getName(SeString sender, XivChatType type, SeString message)
        {
            var pName = State.LocalPlayer.Name.TextValue;
            if (sender.Payloads.Count > 0)
            {
                if (sender.Payloads[0].Type == PayloadType.Player)
                {
                    var pPayload = (PlayerPayload) sender.Payloads[0];
                    pName = pPayload.PlayerName;
                }

                if (sender.Payloads[0].Type == PayloadType.Icon && sender.Payloads[1].Type == PayloadType.Player)
                {
                    var pPayload = (PlayerPayload) sender.Payloads[1];
                    pName = pPayload.PlayerName;
                }
            }

            if (type == XivChatType.StandardEmote || type == XivChatType.CustomEmote)
            {
                if (message.Payloads[0].Type == PayloadType.Player)
                {
                    var pPayload = (PlayerPayload) message.Payloads[0];
                    pName = pPayload.PlayerName;
                }
            }

            return pName;
        }
    }
}
