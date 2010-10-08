﻿using System;
using System.Collections.Generic;
using System.Text;
using RTParser;
using RTParser.Utils;

namespace AIMLbot
{
    public class Bot : RTParser.RTPBot
    {
         
        public Bot()
            : base()
        {
        }
        public void loadAIMLFromFiles()
        {
            base.loadAIMLFromDefaults();
        }
    }
    public class MasterUser : RTParser.User
    {
        public MasterUser(string UserID, Bot bot)
            : base(UserID, bot)
        {
        }
        public MasterUser(string UserID, RTPBot bot)
            : base(UserID, bot)
        {
        }
    }

    sealed public class MasterRequest : RequestImpl, Request
    {
/*
        public Request(String rawInput, RTParser.User user, RTPBot bot)
            : this(rawInput, user, bot, null)
        {
        }
      
              public MasterRequest(String rawInput, RTParser.User user, RTPBot bot, RTParser.Request r)
            : base(rawInput, user, bot, r, null)
        {
        }
*/
        public MasterRequest(string rawInput, User user, RTPBot bot, Request parent, User targetUser)
            : base(rawInput, user, bot, parent, targetUser)
        {
        }
    }

    sealed public class MasterResult : RTParser.ResultImpl, Result ,InteractionResult 
    {
        public MasterResult(string rawInput, User user, RTPBot bot, Request parent, User targetUser)
            : base(rawInput, user, bot, parent, targetUser)
        {
        }

        #region InteractionResult Members


        public Result result
        {
            get { return this;  }
        }

        public RTParser.Variables.ISettingsDictionary RequesterChanges
        {
            get { throw new NotImplementedException(); }
        }

        public RTParser.Variables.ISettingsDictionary ResponderChanges
        {
            get { throw new NotImplementedException(); }
        }

        public InteractionResult PreviousInteraction
        {
            get { throw new NotImplementedException(); }
        }

        public InteractionResult NextInteraction
        {
            get { throw new NotImplementedException(); }
        }

        public Unifiable GetInputSentence(int sentence)
        {
            throw new NotImplementedException();
        }

        public void CollectResult()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    
    namespace Utils
    {
        public class AIMLLoader : RTParser.Utils.AIMLLoader
        {
            public AIMLLoader(RTPBot bot)
                : base(bot, bot == null ? null : bot.GetBotRequest("-AIMLLoader-"))
            {
            }
        }
    }

}
