using System;
using System.Collections.Generic;
using System.Text;
using cogbot.TheOpenSims;
using OpenMetaverse;
using OpenMetaverse.Packets;
using PathSystem3D.Navigation;

namespace cogbot.Actions
{
    public class GotoCommand: Command
    {
        public GotoCommand(BotClient testClient)
		{
			Name = "goto";
			Description = "Teleport to a location (e.g. \"goto Hooper/100/100/30\")";
            Category = CommandCategory.Movement;
            Parameters = new[] {  new NamedParam(typeof(SimPosition), typeof(SimPosition)) };
		}

        public override string Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
		{
			if (args.Length < 1)
                return "Usage: goto sim/x/y/z";

            int argsUsed;
            SimPosition position = WorldSystem.GetVector(args, out argsUsed);
            if (position == null) return "Teleport - Cannot resolve to a location: " + string.Join(" ", args);
            SimPathStore ps = position.PathStore;
            ulong handle = SimRegion.GetRegionHandle(ps);
            if (Client.Self.Teleport(handle, position.SimPosition))
                return "Teleported to " + Client.Network.CurrentSim;
            else
                return "Teleport failed: " + Client.Self.TeleportMessage;

		}
    }
}
