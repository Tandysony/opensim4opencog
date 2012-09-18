using System;
using System.Runtime;
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using AltAIMLbot;
using AltAIMLbot.Utils;
using AltAIMLParser;
using RTParser;
using RTParser.Utils;

namespace RTParser.AIMLTagHandlers
{
    public class peekstack : RTParser.Utils.AIMLTagHandler
    {

        public peekstack(RTParser.AltBot bot,
                User user,
                SubQuery query,
                Request request,
                Result result,
                XmlNode templateNode)
            : base(bot, user, query, request, result, templateNode)
        {
        }



        protected override Unifiable ProcessChange()
        {
            if (this.templateNode.Name.ToLower() == "peekstack")
            {
                // If there are any conversation memos then return them all
                // otherwise return "Nothing."

                try
                {
                    if (Proc.conversationStack.Count > 0)
                    {
                        string [] memos = Proc.conversationStack.ToArray( );
                        string totalStack = "\n";
                        foreach (string s in memos)
                        {
                            totalStack += s + "\n";
                        }
                        Unifiable converseMemo = totalStack;
                        return converseMemo;
                    }
                    else
                    {
                        Unifiable emptyStack = "Nothing.";
                        return emptyStack;
                    }

                }
                catch
                {

                }

            }
            return Unifiable.Empty;

        }
    }
}