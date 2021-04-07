using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Plugin;

namespace ChatTranslator
{
    public partial class ChatTranslator
    {
        public void SaveConfig()
        {
            Configuration.Lang = languageInt;
            Configuration.Channels = _channels;
            Configuration.NotSelf = notself;
            Configuration.Whitelist = whitelist;
            Configuration.ChosenLanguages = chosenLanguages;
            pluginInterface.SavePluginConfig(Configuration);
        }

        public void PrintChat(XivChatType type, string senderName, SeString messageSeString)
        {
            var chat = new XivChatEntry
            {
                Type = type,
                Name = senderName,
                MessageBytes = messageSeString.Encode()
            };

            pluginInterface.Framework.Gui.Chat.PrintChat(chat);
        }

        public void PrintChatToLog(SeString debugMe)
        {
            PluginLog.Log("=================");
            PluginLog.Log($"{debugMe.TextValue}");
            foreach (Payload pl in debugMe.Payloads)
            {
                PluginLog.Log($"TYPE: {pl.Type}");
                if (pl.Type == PayloadType.UIForeground)
                {
                    var pl2 = (UIForegroundPayload)pl;
                    PluginLog.Log($"--COL:{pl2.UIColor.UIForeground}");
                }

            }

        }

    }
}
