using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Plugin;

namespace ChatTranslator
{
    public partial class ChatTranslator
    {

        public void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
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
                                            if (!Tran(type, fmessage, PName))
                                            {
                                                PluginLog.Log("FALSE so printChat");
                                                fmessage.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 0));
                                                fmessage.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, 48));
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
    }
}
