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
                foreach (var pl in message.Payloads)
                {
                    PluginLog.Log($"{pl.Type}");
                    if (pl.Type != PayloadType.UIForeground) continue;
                    var xx = (UIForegroundPayload)pl;
                    PluginLog.Log($"RGB: {xx.RGB}");
                }
                var messageString = message.TextValue;
                var predictedLanguage = Lang(messageString);
                PluginLog.Log($"PRED LANG: {predictedLanguage}");
                var fmessage = new SeString(new List<Payload>());
                fmessage.Append(message);

                var yes = true;
                var pos = Array.IndexOf(_codes, predictedLanguage);
                if (_whitelist && !_chosenLanguages.Contains(pos))
                { yes = false; }
                if (_notself && _pluginInterface.ClientState.LocalPlayer.Name == pName)
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
                    if (Tran(type, fmessage, pName)) return;
                    PluginLog.Log("FALSE so printChat");
                    fmessage.Payloads.Insert(0, new UIForegroundPayload(_pluginInterface.Data, 0));
                    fmessage.Payloads.Insert(0, new UIForegroundPayload(_pluginInterface.Data, 48));
                    PrintChat(type, pName, fmessage);
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
