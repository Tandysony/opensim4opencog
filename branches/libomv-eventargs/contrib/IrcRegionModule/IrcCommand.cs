using System;
using System.Collections.Generic;
using System.Threading;
using cogbot;
using cogbot.Actions;
using Meebey.SmartIrc4net;
using OpenMetaverse;

namespace IrcRegionModule
{
    public class IrcCommand : Command, SystemApplicationCommand

    {
        private string _ircNick = "cogbotIrcBridge";

        public string IrcNick
        {
            get
            {
                return _ircNick;
            }
            set
            {
                _ircNick = value;
                if (ircClient.IsConnected)
                {
                    ircClient.RfcNick(IrcNick);
                }
            }
        }

        private string _ircServer = "irc.freenode.net";
        public string IrcServer
        {
            get
            {
                return _ircServer;
            }
            set
            {
                _ircServer = value;
                if (!ircClient.IsConnected)
                {
                    ircClient.Connect(IrcServer, 6667);
                    ircClient.Login(IrcNick, IrcNick);
                    ircClient.RfcNick(IrcNick);
                    ircClient.RfcJoin(RegionChannel);
                    ircClient.OnChannelMessage += IRC_OnChannelMessage;
                    if (ListenerThreaThread == null)
                    {
                        ListenerThreaThread = new Thread(ListenerThread);
                        ListenerThreaThread.Start();
                    }
                }
                _ircServer = value;
            }
        }

        public void ListenerThread()
        {
            while ((ircClient != null && ircClient.IsConnected))
            {
                ircClient.ListenOnce();
            }   
        }

        Dictionary<String,UUID> IrcUUIDs = new Dictionary<string, UUID>();
        protected internal void IRC_OnChannelMessage(object sender, IrcEventArgs e)
        {
            IrcMessageData data = e.Data;
            if (ircClient.IsMe(data.Nick)) return;
            if (data.Message.Contains("[off]")) return;
            string nick = data.Nick + " " + data.Channel.Substring(1);
            Client.Self.Chat(string.Format("{0}: {1}", nick, data.Message), 0, ChatType.Normal);
            UUID id;
            if (!IrcUUIDs.TryGetValue(nick,out id))
            {
                id = IrcUUIDs[nick] = UUID.Random();
            }
            Client.FakeEvent(Client.Self, "OnChat", data.Message, ChatAudibleLevel.Fully, ChatType.Normal,
                             ChatSourceType.Agent, nick,
                             id, id, Vector3.Zero);
        }

        private string _regionChannel = "#logicmoo";
        public string RegionChannel
        {
            get
            {
                return _regionChannel;
            }
            set
            {
                _regionChannel = value;
                if (ircClient.IsConnected)
                {
                    if (ircClient.IsJoined(_regionChannel))
                    {
                        ircClient.RfcPart(_regionChannel);
                    }
                    ircClient.RfcJoin(RegionChannel);
                }
            }
        }

        private static Meebey.SmartIrc4net.IrcClient ircClient = new IrcClient();
        private Thread ListenerThreaThread;

        public IrcCommand(BotClient bc) 
        {
            Name = "irc";
            Description = "connects to IRC. Usage: irc channel nick server";
            Category = CommandCategory.Other;
            IrcRegionModule.IrcBotModule.IrcCommand = this;
            
        }
        public override CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
        {
            if (args.Length > 0)
            {
                if (!args[0].StartsWith("#"))
                {
                    string putsrv = String.Join(" ", args);
                    ircClient.WriteLine(putsrv);
                    return Success("IRC SERVER: " + putsrv);

                }
                RegionChannel = args[0];
            }
            if (args.Length > 1)
            {
                IrcNick = args[1];
            }
            if (args.Length > 2)
            {
                IrcServer = args[2];
            }
            return Success("irc connected as " + ircClient.Nickname + " on " + RegionChannel);
        }

        public void IrcSend(string msg)
        {
            if (ircClient.IsConnected)
            ircClient.SendMessage(SendType.Message, RegionChannel, msg);
        }

        public bool IsChannelAgent(string fromname)
        {
            return fromname.EndsWith(RegionChannel.Substring(1));            
        }
    }
}