using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientPlayer : IClientPlayer, IPlayer
{
	internal ClientWorldPlayerData worlddata;

	internal ClientPlayerInventoryManager inventoryMgr;

	private ClientMain game;

	public string[] Privileges;

	public string RoleCode;

	public float Ping;

	public EnumCameraMode? OverrideCameraMode;

	public IPlayerRole Role
	{
		get
		{
			return game.WorldMap.GetRole(RoleCode);
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public string PlayerUID => worlddata?.PlayerUID;

	public int ClientId => worlddata.ClientId;

	public EntityPlayer Entity => worlddata.EntityPlayer;

	public IPlayerInventoryManager InventoryManager => inventoryMgr;

	public string PlayerName
	{
		get
		{
			string name = Entity?.GetName();
			if (name != null)
			{
				return name;
			}
			return worlddata.PlayerName;
		}
	}

	BlockSelection IPlayer.CurrentBlockSelection => game.BlockSelection;

	EntitySelection IPlayer.CurrentEntitySelection => game.EntitySelection;

	public IWorldPlayerData WorldData => worlddata;

	public BlockPos SpawnPosition { get; set; }

	public float CameraYaw
	{
		get
		{
			return game.mouseYaw;
		}
		set
		{
			game.mouseYaw = value;
		}
	}

	public float CameraPitch
	{
		get
		{
			return game.mousePitch;
		}
		set
		{
			game.mousePitch = value;
		}
	}

	public float CameraRoll
	{
		get
		{
			return (float)game.MainCamera.Roll;
		}
		set
		{
			game.MainCamera.Roll = value;
		}
	}

	public EnumCameraMode CameraMode => OverrideCameraMode ?? game.MainCamera.CameraMode;

	string[] IPlayer.Privileges => Privileges;

	public List<Entitlement> Entitlements { get; set; } = new List<Entitlement>();


	public bool ImmersiveFpMode
	{
		get
		{
			if (!ClientSettings.ImmersiveFpMode)
			{
				EntityPlayer entity = Entity;
				if (entity == null)
				{
					return false;
				}
				return !entity.Alive;
			}
			return true;
		}
	}

	public PlayerGroupMembership[] Groups => game.OwnPlayerGroupMemembershipsById.Values.ToArray();

	public ClientPlayer(ClientMain game)
	{
		worlddata = ClientWorldPlayerData.CreateNew();
		inventoryMgr = new ClientPlayerInventoryManager(new OrderedDictionary<string, InventoryBase>(), this, game);
		this.game = game;
	}

	private void AddOrUpdateInventory(ClientMain game, Packet_InventoryContents packet)
	{
		string invId = packet.InventoryId;
		ScreenManager.Platform.Logger.VerboseDebug("Received inventory contents " + invId);
		if (packet.InventoryClass == null)
		{
			throw new Exception("Illegal inventory contents packet, classname is null! " + packet.InventoryId);
		}
		if (!inventoryMgr.Inventories.ContainsKey(invId))
		{
			inventoryMgr.Inventories[invId] = (InventoryBasePlayer)ClientMain.ClassRegistry.CreateInventory(packet.InventoryClass, packet.InventoryId, game.api);
		}
		(inventoryMgr.Inventories[invId].InvNetworkUtil as InventoryNetworkUtil).UpdateFromPacket(game, packet);
	}

	public void UpdateFromPacket(ClientMain game, Packet_PlayerData packet)
	{
		if (packet.Entitlements != null)
		{
			Entitlements.Clear();
			string[] array = packet.Entitlements.Split(',');
			foreach (string entitlement in array)
			{
				Entitlements.Add(new Entitlement
				{
					Code = entitlement,
					Name = Lang.Get("entitlement-" + entitlement)
				});
			}
		}
		InventoryManager.ActiveHotbarSlotNumber = packet.HotbarSlotId;
		worlddata.UpdateFromPacket(game, packet);
		for (int i = 0; i < packet.InventoryContentsCount; i++)
		{
			AddOrUpdateInventory(game, packet.InventoryContents[i]);
		}
		SpawnPosition = new BlockPos(packet.Spawnx, packet.Spawny, packet.Spawnz);
	}

	public void UpdateFromPacket(ClientMain game, Packet_PlayerMode mode)
	{
		worlddata.UpdateFromPacket(game, mode);
	}

	public void ShowChatNotification(string message)
	{
		game.ShowChatMessage(message);
	}

	public void TriggerFpAnimation(EnumHandInteract anim)
	{
		if (anim == EnumHandInteract.HeldItemInteract)
		{
			game.HandSetAttackBuild = true;
		}
		if (anim == EnumHandInteract.HeldItemAttack)
		{
			game.HandSetAttackDestroy = true;
		}
	}

	public PlayerGroupMembership[] GetGroups()
	{
		if (game.player.PlayerUID != PlayerUID)
		{
			throw new NotImplementedException("On the client side you can only query the current players group, not those of other players");
		}
		return game.OwnPlayerGroupMemembershipsById.Values.ToArray();
	}

	public PlayerGroupMembership GetGroup(int groupId)
	{
		if (game.player.PlayerUID != PlayerUID)
		{
			throw new NotImplementedException("On the client side you can only query the current players group, not those of other players");
		}
		game.OwnPlayerGroupMemembershipsById.TryGetValue(groupId, out var mems);
		return mems;
	}

	public bool HasPrivilege(string privilegeCode)
	{
		return Privileges.Contains(privilegeCode);
	}

	internal void WarnIfEntityChanged(long newId, string packetName)
	{
		if (Entity == null || Entity.EntityId == newId)
		{
			return;
		}
		game.Logger.Warning("ClientPlayer entityId change detected in {0} packet for {1}", packetName, PlayerName);
		long oldId = Entity.EntityId;
		Entity oldEntity;
		bool hasOld = game.LoadedEntities.TryGetValue(oldId, out oldEntity);
		game.Logger.Warning("Old entityID {0} loaded: {1}.  New entityID {2} loaded: {3}.", oldId, hasOld, newId, game.LoadedEntities.ContainsKey(newId));
		if (hasOld && oldEntity != null)
		{
			game.Logger.Warning("Attempting to despawn the old entityID");
			try
			{
				EntityDespawnData despawnReason = new EntityDespawnData
				{
					Reason = EnumDespawnReason.Unload,
					DamageSourceForDeath = new DamageSource
					{
						Source = EnumDamageSource.Unknown
					}
				};
				oldEntity.OnEntityDespawn(despawnReason);
				game.RemoveEntityRenderer(oldEntity);
				oldEntity.Properties.Client.Renderer = null;
				game.WorldMap.GetClientChunk(oldEntity.InChunkIndex3d)?.RemoveEntity(oldId);
				game.LoadedEntities.Remove(oldId);
			}
			catch (Exception e)
			{
				game.Logger.Error(e);
			}
		}
	}
}
