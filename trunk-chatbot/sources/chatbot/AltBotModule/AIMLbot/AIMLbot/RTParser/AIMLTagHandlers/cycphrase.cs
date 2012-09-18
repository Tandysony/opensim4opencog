using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using AltAIMLbot;
using AltAIMLbot.Utils;
using AltAIMLParser;

namespace RTParser.AIMLTagHandlers
{
    /// <summary>
    /// &lt;cycphrase&gt; translates a Cyc symbol into an English word/phrase
    /// </summary>
    public class cycphrase : RTParser.Database.CycTagHandler
    {
        /// <summary>                    s
        /// Ctor
        /// </summary>
        /// <param name="bot">The bot involved in this request</param>
        /// <param name="user">The user making the request</param>
        /// <param name="query">The query that originated this node</param>
        /// <param name="request">The request inputted into the system</param>
        /// <param name="result">The result to be passed to the user</param>
        /// <param name="templateNode">The node to be Processed</param>
        public cycphrase(RTParser.AltBot bot,
                        User user,
                        SubQuery query,
                        Request request,
                        Result result,
                        XmlNode templateNode)
            : base(bot, user, query, request, result, templateNode)
        {
        }


        /// <summary>
        /// The method that does the actual Processing of the text.
        /// </summary>
        /// <returns>The resulting Processed text</returns>
        protected override Unifiable ProcessChange()
        {
            if (CheckNode("cycphrase"))
            {
                return TheCyc.Paraphrase(TransformAtomically(null, false));
            }
            return Unifiable.Empty;
        }
    }
}
