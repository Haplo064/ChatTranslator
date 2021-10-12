// TODOS
// Publish

using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Configuration;
using Dalamud.Data;
using Dalamud.IoC;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Concurrent;
using System.IO;
using System;

namespace ChatTranslator
{
    public partial class ChatTranslator : IDalamudPlugin
    {
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static DataManager Data { get; private set; }
        [PluginService] public static ChatGui Chat { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; private set; }
        
        public string Name => "Chat Translator";
        private Config _configuration;
        private bool _config;
        private int _languageInt = 16;
        private int _languageInt2;
        private UiColorPick[] _textColour;
        private UiColorPick _chooser;
        private bool _picker;
        private int _tranMode;
        private int _oneInt;
        private bool _oneChan;
        private readonly string[] _tranModeOptions = { "Append", "Replace", "Additional" };
        private Lumina.Excel.ExcelSheet<UIColor> _uiColours;
        private bool _notSelf;
        private bool _whitelist;
        private List<string> _blacklist;
        private List<int> _chosenLanguages;
        private List<XivChatType> _channels = new List<XivChatType>();
        private readonly List<string> _lastTranslations = new List<string>();
        private BlockingCollection<Chatter> _chatters = new BlockingCollection<Chatter>();
        private bool _chatEngine = true;

        private readonly List<XivChatType> _order = new List<XivChatType>
        {
            XivChatType.None,
            XivChatType.Debug,
            XivChatType.Urgent,
            XivChatType.Notice,
            XivChatType.Say,
            XivChatType.Shout,
            XivChatType.TellOutgoing,
            XivChatType.TellIncoming,
            XivChatType.Party,
            XivChatType.Alliance,
            XivChatType.Ls1,
            XivChatType.Ls2,
            XivChatType.Ls3,
            XivChatType.Ls4,
            XivChatType.Ls5,
            XivChatType.Ls6,
            XivChatType.Ls7,
            XivChatType.Ls8,
            XivChatType.FreeCompany,
            XivChatType.NoviceNetwork,
            XivChatType.CustomEmote,
            XivChatType.StandardEmote,
            XivChatType.Yell,
            XivChatType.CrossParty,
            XivChatType.PvPTeam,
            XivChatType.CrossLinkShell1,
            XivChatType.Echo,
            XivChatType.SystemMessage,
            XivChatType.SystemError,
            XivChatType.GatheringSystemMessage,
            XivChatType.ErrorMessage,
            XivChatType.RetainerSale,
            XivChatType.CrossLinkShell2,
            XivChatType.CrossLinkShell3,
            XivChatType.CrossLinkShell4,
            XivChatType.CrossLinkShell5,
            XivChatType.CrossLinkShell6,
            XivChatType.CrossLinkShell7,
            XivChatType.CrossLinkShell8
        };

        private readonly string[] _orderString =
        {
            "None",
            "Debug",
            "Urgent",
            "Notice",
            "Say",
            "Shout",
            "TellOutgoing",
            "TellIncoming",
            "Party",
            "Alliance",
            "Ls1",
            "Ls2",
            "Ls3",
            "Ls4",
            "Ls5",
            "Ls6",
            "Ls7",
            "Ls8",
            "FreeCompany",
            "NoviceNetwork",
            "CustomEmote",
            "StandardEmote",
            "Yell",
            "CrossParty",
            "PvPTeam",
            "CrossLinkShell1",
            "Echo",
            "SystemMessage",
            "SystemError",
            "GatheringSystemMessage",
            "ErrorMessage",
            "RetainerSale",
            "CrossLinkShell2",
            "CrossLinkShell3",
            "CrossLinkShell4",
            "CrossLinkShell5",
            "CrossLinkShell6",
            "CrossLinkShell7",
            "CrossLinkShell8"
        };

        private readonly bool[] _yesNo = {
            false, false, false, false, true,
            true, false, true, true, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, false, true, true, true,
            true, true, false, false, false,
            false, false, true, true, true,
            true, true, true, true
        };

        private readonly string[] _codes = {
            "af", "an", "ar", "az", "be_x_old",
            "bg", "bn", "br", "bs",
            "ca", "ceb", "cs", "cy", "da",
            "de", "el", "en", "eo", "es",
            "et", "eu", "fa", "fi", "fr",
            "gl", "he", "hi", "hr", "ht",
            "hu", "hy", "id", "is", "it",
            "ja", "jv", "ka", "kk", "ko",
            "la", "lb", "lt", "lv",
            "mg", "mk", "ml", "mr", "ms",
            "new", "nl", "nn", "no", "oc",
            "pl", "pt", "ro", "roa_rup",
            "ru", "sk", "sl",
            "sq", "sr", "sv", "sw", "ta",
            "te", "th", "tl", "tr", "uk",
            "ur", "vi", "vo", "war", "zh",
            "zh_classical", "zh_yue"
        };

        private readonly string[] _languages = {
            "Afrikaans", "Aragonese", "Arabic", "Azerbaijani", "Belarusian",
            "Bulgarian", "Bengali", "Breton", "Bosnian",
            "Catalan; Valencian", "Cebuano", "Czech", "Welsh", "Danish",
            "German", "Greek, Modern", "English", "Esperanto", "Spanish; Castilian",
            "Estonian", "Basque", "Persian", "Finnish", "French",
            "Galician", "Hebrew", "Hindi", "Croatian", "Haitian; Haitian Creole",
            "Hungarian", "Armenian", "Indonesian", "Icelandic", "Italian",
            "Japanese", "Javanese", "Georgian", "Kazakh", "Korean",
            "Latin", "Luxembourgish; Letzeburgesch", "Lithuanian", "Latvian",
            "Malagasy", "Macedonian", "Malayalam", "Marathi", "Malay",
            "Nepal Bhasa; Newari", "Dutch; Flemish", "Norwegian Nynorsk; Nynorsk, Norwegian", "Norwegian", "Occitan (post 1500)",
            "Polish", "Portuguese", "Romanian; Moldavian; Moldovan", "Romance languages",
            "Russian", "Slovak", "Slovenian",
            "Albanian", "Serbian", "Swedish", "Swahili", "Tamil",
            "Telugu", "Thai", "Tagalog", "Turkish", "Ukrainian",
            "Urdu", "Vietnamese", "Volapük", "Waray", "Chinese",
            "Chinese Classical", "Chinese yue"
        };

        public ChatTranslator()
        {
            Identifier = Factory.Load(Path.Combine(PluginInterface.AssemblyLocation.DirectoryName, "Wiki82.profile.xml"));
            PluginLog.Information($"XML file path: {Identifier}");

            _configuration = PluginInterface.GetPluginConfig() as Config ?? new Config();
            Chat.ChatMessage += Chat_OnChatMessage;
            
            PluginInterface.UiBuilder.Draw += TranslatorConfigUi;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
            CommandManager.AddHandler("/trn", new CommandInfo(Command)
            {
                HelpMessage = "Opens the Chat Translator config menu"
            });
            _uiColours = Data.Excel.GetSheet<UIColor>();
            _channels = _configuration.Channels;
            _textColour = _configuration.TextColour;
            _tranMode = _configuration.TranMode;
            _languageInt = _configuration.Lang;
            _whitelist = _configuration.Whitelist;
            _notSelf = _configuration.NotSelf;
            _oneChan = _configuration.OneChan;
            _oneInt = _configuration.OneInt;
            _chosenLanguages = _configuration.ChosenLanguages;
            _blacklist = _configuration.Blacklist;
            
            Thread t = new Thread(new ThreadStart(ThreadProc));
            t.Start();
        }

        public void ThreadProc()
        {
            PluginLog.Log("Translation Engine Started");
            while (_chatEngine)
            {
                try
                {
                    var chats = _chatters.Take();
                    PluginLog.Information("Dequeued: " + chats.Message);
                    var tranSeString = Tran(chats.Message);
                    Chat.PrintChat(new XivChatEntry { Message = tranSeString, Name = chats.Sender, Type = chats.Type, SenderId = chats.SenderId });
                }
                catch (Exception ex)
                {
                    PluginLog.Information($"Exception in thread loop: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        public void Dispose()
        {
            Chat.ChatMessage -= Chat_OnChatMessage;
            PluginInterface.UiBuilder.Draw -= TranslatorConfigUi;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
            CommandManager.RemoveHandler("/trn");
            _chatEngine = false;
        }

        private void Command(string command, string arguments) => _config = true;

        private void OpenConfigUi()
        {
            _config = true;
        }
    }

    public class UiColorPick
    {
        public uint Choice { get; set; }
        public uint Option { get; set; }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public List<XivChatType> Channels { get; set; } = new List<XivChatType>();
        public int Lang { get; set; } = 16;
        public UiColorPick[] TextColour { get; set; } =
        {
            new UiColorPick { Choice = 0, Option =0 }
        };
        public bool NotSelf { get; set; }
        public bool Whitelist { get; set; }
        public List<int> ChosenLanguages { get; set; } = new List<int>();
        public bool OneChan { get; set; }
        public int OneInt { get; set; }
        public List<string> Blacklist { get; set; } = new List<string>();
        public int TranMode { get; set; }
    }
}
