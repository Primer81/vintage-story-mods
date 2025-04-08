using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemEntityOwnership : ModSystem
{
	public Dictionary<string, Dictionary<string, EntityOwnership>> OwnerShipsByPlayerUid;

	private ICoreServerAPI sapi;

	private ICoreClientAPI capi;

	public Dictionary<string, EntityOwnership> SelfOwnerShips { get; set; } = new Dictionary<string, EntityOwnership>();


	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("entityownership").RegisterMessageType<EntityOwnershipPacket>();
		api.ModLoader.GetModSystem<WorldMapManager>().RegisterMapLayer<OwnedEntityMapLayer>("ownedcreatures", 2.0);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += Event_PlayerJoin;
		api.Event.OnEntityDeath += Event_EntityDeath;
		AiTaskRegistry.Register<AiTaskComeToOwner>("cometoowner");
	}

	private void Event_PlayerJoin(IServerPlayer player)
	{
		sendOwnerShips(player);
	}

	private void sendOwnerShips(IServerPlayer player)
	{
		if (OwnerShipsByPlayerUid.TryGetValue(player.PlayerUID, out var playerShipsByPlayerUid))
		{
			sapi.Network.GetChannel("entityownership").SendPacket(new EntityOwnershipPacket
			{
				OwnerShipByGroup = playerShipsByPlayerUid
			}, player);
		}
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("entityownership", OwnerShipsByPlayerUid);
	}

	private void Event_SaveGameLoaded()
	{
		OwnerShipsByPlayerUid = sapi.WorldManager.SaveGame.GetData("entityownership", new Dictionary<string, Dictionary<string, EntityOwnership>>());
	}

	private void Event_EntityDeath(Entity entity, DamageSource damageSource)
	{
		RemoveOwnership(entity);
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Network.GetChannel("entityownership").SetMessageHandler<EntityOwnershipPacket>(onPacket);
	}

	private void onPacket(EntityOwnershipPacket packet)
	{
		SelfOwnerShips = packet.OwnerShipByGroup ?? new Dictionary<string, EntityOwnership>();
		WorldMapManager wm = capi.ModLoader.GetModSystem<WorldMapManager>();
		if (wm != null && wm.worldMapDlg != null && wm.worldMapDlg.IsOpened())
		{
			(wm.worldMapDlg.MapLayers.FirstOrDefault((MapLayer ml) => ml is OwnedEntityMapLayer) as OwnedEntityMapLayer)?.Reload();
		}
	}

	public void ClaimOwnership(Entity toEntity, EntityAgent byEntity)
	{
		if (sapi == null)
		{
			return;
		}
		string group = toEntity.GetBehavior<EntityBehaviorOwnable>()?.Group;
		if (group != null)
		{
			IServerPlayer player = (byEntity as EntityPlayer).Player as IServerPlayer;
			OwnerShipsByPlayerUid.TryGetValue(player.PlayerUID, out var playerShipsByPlayerUid);
			if (playerShipsByPlayerUid == null)
			{
				playerShipsByPlayerUid = (OwnerShipsByPlayerUid[player.PlayerUID] = new Dictionary<string, EntityOwnership>());
			}
			if (playerShipsByPlayerUid.TryGetValue(group, out var eo))
			{
				sapi.World.GetEntityById(eo.EntityId)?.WatchedAttributes.RemoveAttribute("ownedby");
			}
			playerShipsByPlayerUid[group] = new EntityOwnership
			{
				EntityId = toEntity.EntityId,
				Pos = toEntity.ServerPos,
				Name = toEntity.GetName(),
				Color = "#0e9d51"
			};
			TreeAttribute tree = new TreeAttribute();
			tree.SetString("uid", player.PlayerUID);
			tree.SetString("name", player.PlayerName);
			toEntity.WatchedAttributes["ownedby"] = tree;
			toEntity.WatchedAttributes.MarkPathDirty("ownedby");
			sendOwnerShips(player);
		}
	}

	public void RemoveOwnership(Entity fromEntity)
	{
		ITreeAttribute tree = fromEntity.WatchedAttributes.GetTreeAttribute("ownedby");
		if (tree == null)
		{
			return;
		}
		string uid = tree.GetString("uid");
		string groupecode = fromEntity.GetBehavior<EntityBehaviorOwnable>().Group;
		if (OwnerShipsByPlayerUid.TryGetValue(uid, out var ownerships) && ownerships != null && ownerships.TryGetValue(groupecode, out var ownership) && ownership?.EntityId == fromEntity.EntityId)
		{
			ownerships.Remove(groupecode);
			IPlayer player = sapi.World.PlayerByUid(uid);
			if (player != null)
			{
				sendOwnerShips(player as IServerPlayer);
			}
			fromEntity.WatchedAttributes.RemoveAttribute("ownedby");
		}
	}
}
