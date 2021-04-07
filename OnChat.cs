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
    public class OnChat
    {
        public ChatTranslator trn;
        public Translater translater;
        public Handy handy;
        public void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            try
            {
                bool tempHandled = isHandled;
                if (!isHandled)
                {
                    if (trn._channels.Contains(type))
                    {
                        string PName = trn.pluginInterface.ClientState.LocalPlayer.Name;
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

                        if (trn.enable)
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
                                String predictedLanguage = translater.Lang(messageString);
                                PluginLog.Log($"PRED LANG: {predictedLanguage}");
                                var fmessage = new SeString(new List<Payload>());
                                fmessage.Append(message);

                                if (predictedLanguage != trn.codes[trn.languageInt])
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
                                        if (trn.tranMode != 2) // is it Replace (1) or append (0)
                                        {
                                            isHandled = true;
                                        }
                                        var t = Task.Run(() =>
                                        {
                                            if (!translater.Tran(type, messageString, fmessage, PName))
                                            {
                                                PluginLog.Log("FALSE so printChat");
                                                fmessage.Payloads.Insert(0, new UIForegroundPayload(trn.pluginInterface.Data, 0));
                                                fmessage.Payloads.Insert(0, new UIForegroundPayload(trn.pluginInterface.Data, 48));
                                                handy.PrintChat(type, PName, fmessage);
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
    }
}
