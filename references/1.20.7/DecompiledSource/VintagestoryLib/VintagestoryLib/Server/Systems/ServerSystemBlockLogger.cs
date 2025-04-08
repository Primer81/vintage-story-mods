using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace VintagestoryLib.Server.Systems;

public class ServerSystemBlockLogger : ServerSystem
{
	private ICoreServerAPI sapi;

	public ServerSystemBlockLogger(ServerMain server)
		: base(server)
	{
		sapi = (ICoreServerAPI)server.Api;
		sapi.Event.ServerRunPhase(EnumServerRunPhase.LoadGamePre, OnConfigReady);
	}

	private void OnConfigReady()
	{
		if (sapi.Server.Config.LogBlockBreakPlace)
		{
			sapi.Event.DidBreakBlock += DidBreakBock;
			sapi.Event.DidPlaceBlock += DidPlaceBLock;
		}
	}

	private void DidPlaceBLock(IServerPlayer byplayer, int oldblockid, BlockSelection blocksel, ItemStack withitemstack)
	{
		if (oldblockid != 0)
		{
			string oldBlock = sapi.World.GetBlock(oldblockid).Code.ToString();
			sapi.Logger.Build("{0} placed {1} [pre: {2}] at {3}", byplayer.PlayerName, withitemstack.Collectible.Code.ToString(), oldBlock, blocksel.Position);
		}
		else
		{
			sapi.Logger.Build("{0} placed {1} at {2}", byplayer.PlayerName, withitemstack.Collectible.Code.ToString(), blocksel.Position);
		}
	}

	private void DidBreakBock(IServerPlayer byplayer, int oldblockid, BlockSelection blocksel)
	{
		string oldBlock = ((oldblockid != 0) ? sapi.World.GetBlock(oldblockid).Code.ToString() : "Air");
		sapi.Logger.Build("{0} removed {1} at {2}", byplayer.PlayerName, oldBlock, blocksel.Position);
	}

	public override void Dispose()
	{
		if (sapi.Server.Config.LogBlockBreakPlace)
		{
			sapi.Event.DidBreakBlock -= DidBreakBock;
			sapi.Event.DidPlaceBlock -= DidPlaceBLock;
		}
	}
}
