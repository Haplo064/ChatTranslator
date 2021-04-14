﻿using System;
using System.Threading.Tasks;
using System.Collections.Generic;
 using System.Linq;
 using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;

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
                var pName = _pluginInterface.ClientState.LocalPlayer.Name;
                if (sender.Payloads.Count > 0)
                {
                    if (sender.Payloads[0].Type == PayloadType.Player)
                    {
                        var pPayload = (PlayerPayload)sender.Payloads[0];
                        pName = pPayload.PlayerName;
                    }
                    if (sender.Payloads[0].Type == PayloadType.Icon && sender.Payloads[1].Type == PayloadType.Player)
                    {
                        var pPayload = (PlayerPayload)sender.Payloads[1];
                        pName = pPayload.PlayerName;
                    }
                }
                if (type == XivChatType.StandardEmote || type == XivChatType.CustomEmote)
                {
                    if (message.Payloads[0].Type == PayloadType.Player)
                    {
                        var pPayload = (PlayerPayload)message.Payloads[0];
                        pName = pPayload.PlayerName;
                    }
                }

                
                var run = true;
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
                var originalMessage = new SeString(new List<Payload>());
                originalMessage.Append(message);

                var yes = true;
                var pos = Array.IndexOf(_codes, predictedLanguage);
                //Check for whitelist settings
                if (_whitelist && !_chosenLanguages.Contains(pos))
                { yes = false; }
                //Check for notSelf setting
                if (_notSelf && _pluginInterface.ClientState.LocalPlayer.Name == pName)
                { yes = false; }
                //Check for blacklist settings
                if (_blacklist.Contains(messageString))
                { yes = false; }
                
                if (predictedLanguage == _codes[_languageInt] || !yes) return;
                var rawExists = message.Payloads.Any(payload => payload.Type == PayloadType.RawText);
                if (!rawExists) return;
                if (_tranMode != 2) // is it Replace (1) or append (0)
                {
                    isHandled = true;
                }

                void Function()
                {
                    if (Tran(type, originalMessage, pName)) return;
                    PluginLog.Log("FALSE so printChat");
                    originalMessage.Payloads.Insert(0, new UIForegroundPayload(_pluginInterface.Data, 0));
                    originalMessage.Payloads.Insert(0, new UIForegroundPayload(_pluginInterface.Data, 48));
                    PrintChat(type, pName, originalMessage);
                }

                Task.Run(Function);
            }
            catch (Exception e)
            {
                PluginLog.LogError($"Chat Translator Error: {e}");
            }
        }
    }
}
