using System.Diagnostics;
using System.IO;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class Core : ModSystem
{
	private ICoreServerAPI sapi;

	public override double ExecuteOrder()
	{
		return 0.001;
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void StartPre(ICoreAPI api)
	{
		api.Assets.AddModOrigin("game", Path.Combine(GamePaths.AssetsPath, "creative"), (api.Side == EnumAppSide.Client) ? "textures" : null);
	}

	public override void Start(ICoreAPI api)
	{
		GameVersion.EnsureEqualVersionOrKillExecutable(api, FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion, "1.20.7", "VSCreativeMod");
		base.Start(api);
		api.RegisterItemClass("ItemMagicWand", typeof(ItemMagicWand));
		api.RegisterEntity("EntityTestShip", typeof(EntityTestShip));
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.Event.SaveGameCreated += Event_SaveGameCreated;
		api.Event.PlayerCreate += Event_PlayerCreate;
	}

	private void Event_PlayerCreate(IServerPlayer byPlayer)
	{
		if (sapi.WorldManager.SaveGame.WorldConfiguration.GetString("gameMode") == "creative")
		{
			byPlayer.WorldData.CurrentGameMode = EnumGameMode.Creative;
			byPlayer.WorldData.PickingRange = 100f;
			byPlayer.BroadcastPlayerData();
		}
	}

	private void Event_SaveGameCreated()
	{
		if (sapi.WorldManager.SaveGame.PlayStyle == "creativebuilding")
		{
			sapi.WorldManager.SaveGame.EntitySpawning = false;
		}
	}
}
