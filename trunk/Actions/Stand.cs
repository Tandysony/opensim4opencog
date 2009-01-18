using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse; //using libsecondlife;

namespace cogbot.Actions
{
    class Stand : Action
    {
        public Stand(BotClient Client)
            : base(Client)
        {
            helpString = "Stand up.";
            usageString = "To Stand up, type \"stand\"";
        }

        public override void acceptInput(string verb, Parser args)
        {
            //base.acceptInput(verb, args);

            Sit sit = (Sit)Client.Commands["sit"];
            if (Client.Self.SittingOn == 0 && !sit.sittingOnGround)
            {
                WriteLine("You are already standing.");
            }
            else
            {
                Client.Self.Crouch(false);
                Client.Self.Stand();
                WriteLine("You stood up.");
                sit.sittingOnGround = false;
            }

            Client.describeNext = true;
        }
    }
}
