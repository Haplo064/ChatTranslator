using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
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
                    //PluginLog.Log("Caught loop A");
                    run = false;
                }
                if (!run) return;

                
                var messageString = message.TextValue;
                //PluginLog.Log($"READING IN: {messageString}");
                var predictedLanguage = Lang(messageString);
                //PluginLog.Log($"PRED LANG: {predictedLanguage}");

                var yes = true;
                var pos = Array.IndexOf(_codes, predictedLanguage);
                //Check for whitelist settings
                if (_whitelist && !_chosenLanguages.Contains(pos))
                { yes = false; }
                //Check for notSelf setting
                //PluginLog.Log($"MY NAME: {ClientState.LocalPlayer.Name} vs: {pName}");
                if (ClientState.LocalPlayer is not null && _notSelf && ClientState.LocalPlayer.Name.TextValue == pName.TextValue)
                { yes = false; }
                //Check for blacklist settings
                if (_blacklist.Contains(messageString))
                { yes = false; }
                if (predictedLanguage == _codes[_languageInt] || !yes) return;
                
                //Checking if any rawtext to translate exists
                var rawExists = message.Payloads.Any(payload => payload.Type == PayloadType.RawText);
                if (!rawExists) return;
                

                
                if (_oneChan && _tranMode == 2)
                {
                    type = _order[_oneInt];
                }

                if (_tranMode == 0 || _tranMode == 1) isHandled = true;

                // is it Append (0), Replace (1), or additional (2)
                //PluginLog.Log($"ADDING MESSAGE TO CHAT TRAN QUEUE.");
                _chatters.Enqueue(new Chatter{Message = message, mode = _tranMode, Sender = sender, Type = type, Sent = false, SenderId = senderId});
             

            }
            catch (Exception e)
            {
                PluginLog.LogError($"Chat Translator Error: {e}");
            }
        }

        public SeString getName(SeString sender, XivChatType type, SeString message)
        {
            var playerPayload = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
            if (type is XivChatType.StandardEmote) playerPayload = message.Payloads.FirstOrDefault(x => x is PlayerPayload) as PlayerPayload;
            var pName = playerPayload == default(PlayerPayload) ? ClientState.LocalPlayer.Name.TextValue : playerPayload.PlayerName;
            PluginLog.Log(pName);
            return pName;
        }

        public class Chatter
        {
            public int mode { get; set; }
            public SeString Sender { get; set; }
            public XivChatType Type { get; set; }
            public SeString Message { get; set; }
            public uint SenderId { get; set; }
            public bool Sent { get; set; }
        }
        
    }
}
