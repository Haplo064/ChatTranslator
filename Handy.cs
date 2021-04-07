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
    public class Handy
    {
        public ChatTranslator trn;

        public void SaveConfig()
        {
            trn.Configuration.Lang = trn.languageInt;
            trn.Configuration.Channels = trn._channels;
            trn.pluginInterface.SavePluginConfig(trn.Configuration);
        }

        public void PrintChat(XivChatType type, string senderName, SeString messageSeString)
        {
            var chat = new XivChatEntry
            {
                Type = type,
                Name = senderName,
                MessageBytes = messageSeString.Encode()
            };

            trn.pluginInterface.Framework.Gui.Chat.PrintChat(chat);
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
