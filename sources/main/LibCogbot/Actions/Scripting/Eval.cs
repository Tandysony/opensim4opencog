﻿using System;
using System.Collections.Generic;
using System.Text;
//using OpenMetaverse; //using libsecondlife;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Scripting
{
    internal class Eval : Command, BotSystemCommand
    {
        public Eval(BotClient Client)
            : base(Client)
        {
            Name = "eval";
        }

        public override void MakeInfo()
        {
            Description = "Enqueue a lisp task in ClientMananger.";
            AddVersion(CreateParams(Rest("code", typeof(string), "what to eval")));
            Category = CommandCategory.Scripting;
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            //base.acceptInput(verb, args);
            return Success(TheBotClient.evalLispString(args.GetString("code")));
        }
    }
}