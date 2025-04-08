using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdChunk
{
	private ServerMain server;

	public CmdChunk(ServerMain server)
	{
		this.server = server;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.commandapi.GetOrCreate("chunk").WithDescription("Commands affecting chunks.").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("forceload")
			.WithDescription("Force the server to preload all chunk columns in given area.")
			.WithAdditionalInformation("These chunks will not be unloaded until the server is restarted.  Area is given by specifying block x,z coordinates for two opposite corners (both corners will be included).  Coordinates can be relative to the player using (~) prefix")
			.WithArgs(parsers.WorldPosition2D("position1"), parsers.WorldPosition2D("position2"))
			.HandleWith(handleForceLoadChunks)
			.EndSubCommand();
	}

	private TextCommandResult handleForceLoadChunks(TextCommandCallingArgs args)
	{
		Vec2i obj = args[0] as Vec2i;
		Vec2i pos2 = args[1] as Vec2i;
		int mincx = Math.Min(obj.X, pos2.X) / 32;
		int maxcx = Math.Max(obj.X, pos2.X) / 32;
		int mincz = Math.Min(obj.Y, pos2.Y) / 32;
		int maxcz = Math.Max(obj.Y, pos2.Y) / 32;
		int forceLoaded = 0;
		for (int cx = mincx; cx <= maxcx; cx++)
		{
			for (int cz = mincz; cz <= maxcz; cz++)
			{
				forceLoaded++;
				server.LoadChunkColumnFast(cx, cz, new ChunkLoadOptions
				{
					KeepLoaded = true
				});
			}
		}
		return TextCommandResult.Success("Ok, will force load " + forceLoaded + " chunk columns");
	}
}
