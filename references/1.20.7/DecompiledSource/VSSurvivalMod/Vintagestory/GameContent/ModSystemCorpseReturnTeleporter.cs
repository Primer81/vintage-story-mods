using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemCorpseReturnTeleporter : ModSystem
{
	public Dictionary<string, Vec3d> lastDeathLocations = new Dictionary<string, Vec3d>();

	private ICoreServerAPI sapi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.Event.PlayerDeath += Event_PlayerDeath;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		sapi = api;
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("lastDeathLocations", lastDeathLocations);
	}

	private void Event_SaveGameLoaded()
	{
		lastDeathLocations = sapi.WorldManager.SaveGame.GetData("lastDeathLocations", new Dictionary<string, Vec3d>());
	}

	private void Event_PlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
	{
		lastDeathLocations[byPlayer.PlayerUID] = byPlayer.Entity.Pos.XYZ;
	}
}
