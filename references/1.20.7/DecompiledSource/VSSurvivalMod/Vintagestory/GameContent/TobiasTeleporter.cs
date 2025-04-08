using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class TobiasTeleporter : ModSystem
{
	private ICoreServerAPI sapi;

	private ICoreClientAPI capi;

	public TobiasTeleporterData TeleporterData = new TobiasTeleporterData();

	private bool needsSaving;

	private IClientNetworkChannel clientChannel;

	private IServerNetworkChannel serverChannel;

	public double OwnLastUsage;

	public int TpCooldownInMonths { get; set; } = 6;


	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		clientChannel = api.Network.RegisterChannel("tobiasteleporter");
		clientChannel.RegisterMessageType(typeof(TobiasLastUsage));
		clientChannel.SetMessageHandler<TobiasLastUsage>(OnLastUsage);
	}

	private void OnLastUsage(TobiasLastUsage lastUsage)
	{
		OwnLastUsage = lastUsage.LastUsage;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		sapi.Event.SaveGameLoaded += Event_SaveGameLoaded;
		sapi.Event.GameWorldSave += Event_GameWorldSave;
		sapi.Event.PlayerJoin += OnPlayerJoin;
		serverChannel = api.Network.RegisterChannel("tobiasteleporter");
		serverChannel.RegisterMessageType(typeof(TobiasLastUsage));
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		sapi.ChatCommands.GetOrCreate("dev").BeginSubCommand("tobias-teleporter").WithAlias("tobt")
			.WithDescription("Set tobias teleporter at the specified location. Only one per world can exist.")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"))
			.HandleWith(OnSetTp)
			.EndSubCommand();
	}

	private void OnPlayerJoin(IServerPlayer byplayer)
	{
		SendLastUsageToPlayer(byplayer);
	}

	private void SendLastUsageToPlayer(IServerPlayer byplayer)
	{
		if (TeleporterData.PlayerLocations.TryGetValue(byplayer.PlayerUID, out var playerLocation))
		{
			TobiasLastUsage message = new TobiasLastUsage
			{
				LastUsage = playerLocation.TotalDaysSinceLastTeleport
			};
			serverChannel.SendPacket(message, byplayer);
		}
	}

	private TextCommandResult OnSetTp(TextCommandCallingArgs args)
	{
		Vec3d posVec = args[0] as Vec3d;
		BlockPos pos = posVec.AsBlockPos;
		Block block = sapi.World.BlockAccessor.GetBlock(pos);
		BlockEntityTobiasTeleporter bett = block.GetBlockEntity<BlockEntityTobiasTeleporter>(pos);
		if (bett == null)
		{
			return TextCommandResult.Success("Target block not a Tobias Translocator");
		}
		bett.IsAtTobiasCave = true;
		bett.OwnerPlayerUid = null;
		string side = block.Variant["side"];
		Vec3d tpPos = posVec + BlockTobiasTeleporter.GetTeleportOffset(side);
		TeleporterData.TobiasTeleporterLocation = tpPos;
		needsSaving = true;
		return TextCommandResult.Success("Tobias teleporter set to Tobias Cave");
	}

	private void Event_GameWorldSave()
	{
		if (needsSaving)
		{
			needsSaving = false;
			sapi.WorldManager.SaveGame.StoreData("tobiasTeleporterData", TeleporterData);
		}
	}

	private void Event_SaveGameLoaded()
	{
		TeleporterData = sapi.WorldManager.SaveGame.GetData("tobiasTeleporterData", new TobiasTeleporterData());
	}

	public void UpdatePlayerLastTeleport(Entity entity)
	{
		if (entity is EntityPlayer player && TeleporterData.PlayerLocations.TryGetValue(player.PlayerUID, out var location))
		{
			location.TotalDaysSinceLastTeleport = sapi.World.Calendar.TotalDays;
			SendLastUsageToPlayer(player.Player as IServerPlayer);
		}
	}

	public bool IsAllowedToTeleport(string playerUid, out Vec3d location)
	{
		if (TeleporterData.PlayerLocations.TryGetValue(playerUid, out var data))
		{
			int sixMonths = sapi.World.Calendar.DaysPerMonth * TpCooldownInMonths;
			if (data.TotalDaysSinceLastTeleport + (double)sixMonths < sapi.World.Calendar.TotalDays)
			{
				location = data.Position;
				return true;
			}
		}
		location = null;
		return false;
	}

	public bool TryGetPlayerLocation(string playerUid, out Vec3d location)
	{
		if (TeleporterData.PlayerLocations.TryGetValue(playerUid, out var data))
		{
			location = data.Position;
			return true;
		}
		location = null;
		return false;
	}

	public void AddPlayerLocation(string playerUid, BlockPos position)
	{
		string side = sapi.World.BlockAccessor.GetBlock(position).Variant["side"];
		Vec3d tpPos = position.ToVec3d() + BlockTobiasTeleporter.GetTeleportOffset(side);
		TeleporterData.PlayerLocations[playerUid] = new PlayerLocationData
		{
			Position = tpPos,
			TotalDaysSinceLastTeleport = sapi.World.Calendar.TotalDays - (double)(sapi.World.Calendar.DaysPerMonth * TpCooldownInMonths)
		};
		IServerPlayer player = sapi.World.PlayerByUid(playerUid) as IServerPlayer;
		SendLastUsageToPlayer(player);
		needsSaving = true;
	}

	public void RemovePlayerTeleporter(string ownerPlayerUid)
	{
		TeleporterData.PlayerLocations.Remove(ownerPlayerUid);
		IPlayer player = sapi.World.PlayerByUid(ownerPlayerUid);
		TobiasLastUsage message = new TobiasLastUsage
		{
			LastUsage = 0.0
		};
		serverChannel.SendPacket(message, player as IServerPlayer);
		needsSaving = true;
	}

	public double GetNextUsage()
	{
		int sixMonths = capi.World.Calendar.DaysPerMonth * TpCooldownInMonths;
		return Math.Max(0.0, (double)sixMonths + OwnLastUsage - capi.World.Calendar.TotalDays);
	}
}
