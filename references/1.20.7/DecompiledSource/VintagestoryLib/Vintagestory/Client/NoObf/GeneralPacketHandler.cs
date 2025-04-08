using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class GeneralPacketHandler : ClientSystem
{
	public override string Name => "gph";

	public GeneralPacketHandler(ClientMain game)
		: base(game)
	{
		game.PacketHandlers[2] = HandlePing;
		game.PacketHandlers[3] = HandlePlayerPing;
		game.PacketHandlers[58] = HandleExchangeBlock;
		game.PacketHandlers[46] = HandleModeChange;
		game.PacketHandlers[45] = HandlePlayerDeath;
		game.PacketHandlers[8] = HandleChatLine;
		game.PacketHandlers[9] = HandleDisconnectPlayer;
		game.PacketHandlers[18] = HandleSound;
		game.PacketHandlers[29] = HandleServerRedirect;
		game.PacketHandlers[41] = HandlePlayerData;
		game.PacketHandlers[30] = HandleInventoryContents;
		game.PacketHandlers[31] = HandleInventoryUpdate;
		game.PacketHandlers[32] = HandleInventoryDoubleUpdate;
		game.PacketHandlers[66] = HandleNotifyItemSlot;
		game.PacketHandlers[7] = HandleSetBlock;
		game.PacketHandlers[48] = HandleBlockEntities;
		game.PacketHandlers[44] = HandleBlockEntityMessage;
		game.PacketHandlers[51] = HandleSpawnPosition;
		game.PacketHandlers[53] = HandleSelectedHotbarSlot;
		game.PacketHandlers[59] = HandleStopMovement;
		game.PacketHandlers[61] = HandleSpawnParticles;
		game.PacketHandlers[64] = HandleBlockDamage;
		game.PacketHandlers[65] = HandleAmbient;
		game.PacketHandlers[68] = HandleIngameError;
		game.PacketHandlers[69] = HandleIngameDiscovery;
		game.PacketHandlers[72] = RemoveBlockLight;
		game.PacketHandlers[75] = HandleLandClaims;
		game.PacketHandlers[76] = HandleRoles;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}

	private void HandlePing(Packet_Server packet)
	{
		game.SendPingReply();
		game.ServerInfo.ServerPing.OnSend(game.Platform.EllapsedMs);
	}

	private void HandlePlayerPing(Packet_Server packet)
	{
		game.ServerInfo.ServerPing.OnReceive(game.Platform.EllapsedMs);
		Packet_ServerPlayerPing p = packet.PlayerPing;
		Dictionary<int, float> pings = new Dictionary<int, float>();
		for (int i = 0; i < packet.PlayerPing.ClientIdsCount; i++)
		{
			int clientid = p.ClientIds[i];
			pings[clientid] = (float)p.Pings[i] / 1000f;
		}
		foreach (KeyValuePair<string, ClientPlayer> plr in game.PlayersByUid)
		{
			if (pings.TryGetValue(plr.Value.ClientId, out var val))
			{
				plr.Value.Ping = val;
			}
		}
	}

	private void HandleSetBlock(Packet_Server packet)
	{
		BlockPos pos = new BlockPos(packet.SetBlock.X, packet.SetBlock.Y, packet.SetBlock.Z);
		int blockId = packet.SetBlock.BlockType;
		if (blockId < 0)
		{
			int oldLiquidBlockId = game.WorldMap.RelaxedBlockAccess.GetBlock(pos, 2).BlockId;
			blockId = -(blockId + 1);
			if (blockId != oldLiquidBlockId)
			{
				game.WorldMap.RelaxedBlockAccess.SetBlock(blockId, pos, 2);
			}
		}
		else
		{
			int oldBlockId = game.WorldMap.RelaxedBlockAccess.GetBlockId(pos);
			if (blockId != oldBlockId)
			{
				game.WorldMap.RelaxedBlockAccess.SetBlock(blockId, pos);
				game.eventManager?.TriggerBlockChanged(game, pos, game.WorldMap.Blocks[oldBlockId]);
			}
		}
	}

	private void HandleExchangeBlock(Packet_Server packet)
	{
		BlockPos pos = new BlockPos(packet.ExchangeBlock.X, packet.ExchangeBlock.Y, packet.ExchangeBlock.Z);
		int oldBlockId = game.WorldMap.RelaxedBlockAccess.GetBlockId(pos);
		int blockId = packet.ExchangeBlock.BlockType;
		game.WorldMap.RelaxedBlockAccess.ExchangeBlock(blockId, pos);
		game.eventManager?.TriggerBlockChanged(game, pos, game.WorldMap.Blocks[oldBlockId]);
	}

	private void HandleModeChange(Packet_Server packet)
	{
		game.PlayersByUid.TryGetValue(packet.ModeChange.PlayerUID, out var player);
		player?.UpdateFromPacket(game, packet.ModeChange);
	}

	private void HandlePlayerDeath(Packet_Server packet)
	{
		if (game.EntityPlayer != null)
		{
			game.EntityPlayer.TryStopHandAction(forceStop: true, EnumItemUseCancelReason.Death);
			game.eventManager?.TriggerPlayerDeath(packet.PlayerDeath.ClientId, packet.PlayerDeath.LivesLeft);
		}
	}

	private void HandleChatLine(Packet_Server packet)
	{
		game.eventManager?.TriggerNewServerChatLine(packet.Chatline.Groupid, packet.Chatline.Message, (EnumChatType)packet.Chatline.ChatType, packet.Chatline.Data);
		game.Logger.Chat("{0} @ {1}", packet.Chatline.Message, packet.Chatline.Groupid);
	}

	private void HandleDisconnectPlayer(Packet_Server packet)
	{
		game.Logger.Notification("Disconnected by the server ({0})", packet.DisconnectPlayer.DisconnectReason);
		string reason = packet.DisconnectPlayer.DisconnectReason;
		game.exitReason = "exit command by server";
		if ((reason != null && reason.Contains("Bad game session")) || reason == Lang.Get("Bad game session, try relogging"))
		{
			reason += "\n\nThis error can be caused when trying to connect to a server on version 1.18.3 or older. Please ask the server owner to update.";
		}
		game.disconnectReason = reason;
		game.DestroyGameSession(gotDisconnected: true);
	}

	private void HandleSound(Packet_Server packet)
	{
		game.PlaySoundAt(new AssetLocation(packet.Sound.Name), CollectibleNet.DeserializeFloat(packet.Sound.X), CollectibleNet.DeserializeFloat(packet.Sound.Y), CollectibleNet.DeserializeFloat(packet.Sound.Z), null, (EnumSoundType)packet.Sound.SoundType, CollectibleNet.DeserializeFloatPrecise(packet.Sound.Pitch), CollectibleNet.DeserializeFloat(packet.Sound.Range), CollectibleNet.DeserializeFloatPrecise(packet.Sound.Volume));
	}

	private void HandleServerRedirect(Packet_Server packet)
	{
		game.Logger.Notification("Received server redirect");
		game.SendLeave(0);
		game.ExitAndSwitchServer(new MultiplayerServerEntry
		{
			host = packet.Redirect.Host,
			name = packet.Redirect.Name
		});
		game.Logger.VerboseDebug("Received server redirect packet");
	}

	private void HandlePlayerData(Packet_Server packet)
	{
		if (!game.BlocksReceivedAndLoaded)
		{
			game.Logger.VerboseDebug("Startup sequence wrong, playerdata packet handled before BlocksReceivedAndLoaded; player may be null");
			return;
		}
		string uid = packet.PlayerData.PlayerUID;
		if (packet.PlayerData.ClientId <= -99)
		{
			game.Logger.VerboseDebug("Received player data deletion for playeruid " + uid);
			if (game.PlayersByUid.TryGetValue(uid, out var plr))
			{
				game.api.eventapi.TriggerPlayerEntityDespawn(plr);
				plr.worlddata.EntityPlayer = null;
				game.api.eventapi.TriggerPlayerLeave(plr);
				game.PlayersByUid.Remove(uid);
			}
			return;
		}
		game.Logger.VerboseDebug("Received player data for playeruid " + uid);
		ClientPlayer clientPlayer;
		bool isNew = !game.PlayersByUid.TryGetValue(uid, out clientPlayer);
		if (isNew)
		{
			clientPlayer = (game.PlayersByUid[uid] = new ClientPlayer(game));
		}
		else
		{
			clientPlayer.WarnIfEntityChanged(packet.PlayerData.EntityId, "playerData");
		}
		clientPlayer.UpdateFromPacket(game, packet.PlayerData);
		if (ClientSettings.PlayerUID == uid && !game.Spawned)
		{
			game.player = clientPlayer;
			game.mouseYaw = game.EntityPlayer.SidedPos.Yaw;
			game.mousePitch = game.EntityPlayer.SidedPos.Pitch;
			game.Logger.VerboseDebug("Informing clientsystems playerdata received");
			game.OnOwnPlayerDataReceived();
			game.Spawned = true;
			game.SendPacketClient(new Packet_Client
			{
				Id = 26
			});
		}
		if (packet.PlayerData.Privileges != null)
		{
			string[] privileges = packet.PlayerData.Privileges;
			int count = packet.PlayerData.PrivilegesCount;
			string[] array = (clientPlayer.Privileges = new string[count]);
			for (int i = 0; i < count; i++)
			{
				array[i] = privileges[i];
			}
		}
		if (packet.PlayerData.RoleCode != null)
		{
			clientPlayer.RoleCode = packet.PlayerData.RoleCode;
		}
		if (isNew)
		{
			game.api.eventapi.TriggerPlayerJoin(clientPlayer);
			if (clientPlayer.Entity != null)
			{
				game.api.eventapi.TriggerPlayerEntitySpawn(clientPlayer);
			}
		}
		if (ClientSettings.PlayerUID == uid)
		{
			game.eventManager?.TriggerPlayerModeChange();
			if (game.player.worlddata.CurrentGameMode != EnumGameMode.Creative)
			{
				ClientSettings.RenderMetaBlocks = false;
			}
			if (game.player.worlddata.CurrentGameMode == EnumGameMode.Spectator)
			{
				game.MainCamera.SetMode(EnumCameraMode.FirstPerson);
			}
			if (!game.clientPlayingFired && game.api.eventapi.TriggerIsPlayerReady())
			{
				game.clientPlayingFired = true;
				game.SendPacketClient(new Packet_Client
				{
					Id = 29
				});
			}
		}
		game.Logger.VerboseDebug("Done handling playerdata packet");
	}

	private void HandleInventoryContents(Packet_Server packet)
	{
		string invId = packet.InventoryContents.InventoryId;
		game.Logger.VerboseDebug("Received inventory contents " + invId);
		ClientPlayer player = game.GetPlayerFromClientId(packet.InventoryContents.ClientId);
		if (player == null)
		{
			game.Logger.Error("Server sent me inventory contents for a player that i don't have? Ignoring. Clientid was " + packet.InventoryContents.ClientId);
			return;
		}
		if (!player.inventoryMgr.Inventories.ContainsKey(invId))
		{
			if (!ClientMain.ClassRegistry.inventoryClassToTypeMapping.ContainsKey(packet.InventoryContents.InventoryClass))
			{
				game.Logger.Error("Server sent me inventory contents from with an inventory class name '{0}' - no idea how to instantiate that. Ignoring.", packet.InventoryContents.InventoryClass);
				return;
			}
			player.inventoryMgr.Inventories[invId] = ClientMain.ClassRegistry.CreateInventory(packet.InventoryContents.InventoryClass, packet.InventoryContents.InventoryId, game.api);
			player.inventoryMgr.Inventories[invId].AfterBlocksLoaded(game);
		}
		(player.inventoryMgr.Inventories[invId].InvNetworkUtil as InventoryNetworkUtil).UpdateFromPacket(game, packet.InventoryContents);
	}

	private void HandleInventoryUpdate(Packet_Server packet)
	{
		string invId = packet.InventoryUpdate.InventoryId;
		ClientPlayer player = game.GetPlayerFromClientId(packet.InventoryUpdate.ClientId);
		if (player != null && player.inventoryMgr.Inventories.ContainsKey(invId))
		{
			(player.inventoryMgr.Inventories[invId].InvNetworkUtil as InventoryNetworkUtil).UpdateFromPacket(game, packet.InventoryUpdate);
		}
	}

	private void HandleNotifyItemSlot(Packet_Server packet)
	{
		string invId = packet.NotifySlot.InventoryId;
		if (game.player?.inventoryMgr?.Inventories != null && game.player.inventoryMgr.Inventories.ContainsKey(invId))
		{
			game.player.inventoryMgr.Inventories[invId]?.PerformNotifySlot(packet.NotifySlot.SlotId);
		}
	}

	private void HandleInventoryDoubleUpdate(Packet_Server packet)
	{
		if (packet?.InventoryDoubleUpdate == null)
		{
			game.Logger.Warning("Received inventory double update with packet set to null?");
			return;
		}
		string invId1 = packet.InventoryDoubleUpdate.InventoryId1;
		string invId2 = packet.InventoryDoubleUpdate.InventoryId2;
		ClientPlayerInventoryManager inventoryMgr = game.GetPlayerFromClientId(packet.InventoryDoubleUpdate.ClientId)?.inventoryMgr;
		if (inventoryMgr == null)
		{
			game.Logger.Warning("Received inventory double update for a client whose inventory i dont have? for clientid " + packet?.InventoryContents?.ClientId);
			return;
		}
		if (inventoryMgr.GetInventory(invId1, out var invFound))
		{
			(invFound.InvNetworkUtil as InventoryNetworkUtil)?.UpdateFromPacket(game, packet.InventoryDoubleUpdate);
		}
		if (invId1 != invId2 && inventoryMgr.GetInventory(invId1, out var invFound2))
		{
			(invFound2.InvNetworkUtil as InventoryNetworkUtil)?.UpdateFromPacket(game, packet.InventoryDoubleUpdate);
		}
	}

	private void HandleBlockEntities(Packet_Server packet)
	{
		Packet_BlockEntity[] blockentities = packet.BlockEntities.BlockEntitites;
		for (int i = 0; i < packet.BlockEntities.BlockEntititesCount; i++)
		{
			Packet_BlockEntity p = blockentities[i];
			ClientChunk chunk = game.WorldMap.GetChunkAtBlockPos(p.PosX, p.PosY, p.PosZ);
			if (chunk != null)
			{
				chunk.AddOrUpdateBlockEntityFromPacket(p, game);
				BlockPos pos = new BlockPos(p.PosX, p.PosY, p.PosZ);
				game.eventManager?.TriggerBlockChanged(game, pos, game.BlockAccessor.GetBlock(pos));
			}
		}
	}

	private void HandleBlockEntityMessage(Packet_Server packet)
	{
		Packet_BlockEntityMessage p = packet.BlockEntityMessage;
		game.WorldMap.GetBlockEntity(new BlockPos(p.X, p.Y, p.Z))?.OnReceivedServerPacket(p.PacketId, p.Data);
	}

	private void HandleSpawnPosition(Packet_Server packet)
	{
		EntityPos spawnpos = ClientSystemEntities.entityPosFromPacket(packet.EntityPosition);
		game.SpawnPosition = spawnpos;
	}

	private void HandleSelectedHotbarSlot(Packet_Server packet)
	{
		int clientid = packet.SelectedHotbarSlot.ClientId;
		try
		{
			foreach (ClientPlayer player in game.PlayersByUid.Values)
			{
				if (player.ClientId == clientid)
				{
					Packet_SelectedHotbarSlot shpPacket = packet.SelectedHotbarSlot;
					player.inventoryMgr.SetActiveHotbarSlotNumberFromServer(shpPacket.SlotNumber);
					ItemStack stack = null;
					if (shpPacket.Itemstack != null && shpPacket.Itemstack.ItemClass != -1 && shpPacket.Itemstack.ItemId != 0)
					{
						stack = StackConverter.FromPacket(shpPacket.Itemstack, game);
					}
					ItemStack offstack = null;
					if (shpPacket.OffhandStack != null && shpPacket.OffhandStack.ItemClass != -1 && shpPacket.OffhandStack.ItemId != 0)
					{
						offstack = StackConverter.FromPacket(shpPacket.OffhandStack, game);
					}
					player.inventoryMgr.ActiveHotbarSlot.Itemstack = stack;
					if (player.Entity?.LeftHandItemSlot != null)
					{
						player.Entity.LeftHandItemSlot.Itemstack = offstack;
					}
					break;
				}
			}
		}
		catch (Exception e)
		{
			string msg = "Handling server packet HandleSelectedHotbarSlot threw an exception while trying to update the slot of clientid " + clientid + " with itemstack " + packet.SelectedHotbarSlot.Itemstack;
			msg += "Exception thrown: ";
			game.Logger.Fatal(msg);
			game.Logger.Fatal(e);
		}
	}

	private void HandleStopMovement(Packet_Server packet)
	{
		if (game.EntityPlayer?.Controls != null)
		{
			game.EntityPlayer.Controls.StopAllMovement();
		}
	}

	private void HandleSpawnParticles(Packet_Server packet)
	{
		Packet_SpawnParticles p = packet.SpawnParticles;
		IParticlePropertiesProvider propprovider = ClientMain.ClassRegistry.CreateParticlePropertyProvider(p.ParticlePropertyProviderClassName);
		using (MemoryStream ms = new MemoryStream(p.Data))
		{
			BinaryReader reader = new BinaryReader(ms);
			propprovider.FromBytes(reader, game);
		}
		game.SpawnParticles(propprovider);
	}

	private void HandleBlockDamage(Packet_Server packet)
	{
		BlockPos pos = new BlockPos(packet.BlockDamage.PosX, packet.BlockDamage.PosY, packet.BlockDamage.PosZ);
		game.WorldMap.DamageBlock(pos, BlockFacing.ALLFACES[packet.BlockDamage.Facing], CollectibleNet.DeserializeFloat(packet.BlockDamage.Damage));
	}

	private void HandleAmbient(Packet_Server packet)
	{
		using MemoryStream ms = new MemoryStream(packet.Ambient.Data);
		AmbientModifier s = new AmbientModifier().EnsurePopulated();
		s.FromBytes(new BinaryReader(ms));
		game.AmbientManager.CurrentModifiers["serverambient"] = s.EnsurePopulated();
	}

	private void HandleIngameError(Packet_Server packet)
	{
		Packet_IngameError p = packet.IngameError;
		string message = p.Message;
		if (message == null)
		{
			if (p.LangParams == null)
			{
				message = Lang.Get("ingameerror-" + p.Code);
			}
			else
			{
				string key = "ingameerror-" + p.Code;
				object[] langParams = p.LangParams;
				message = Lang.Get(key, langParams);
			}
		}
		game.eventManager?.TriggerIngameError(this, p.Code, message);
	}

	private void HandleIngameDiscovery(Packet_Server packet)
	{
		Packet_IngameDiscovery p = packet.IngameDiscovery;
		string message = p.Message;
		if (message == null)
		{
			string key = "ingamediscovery-" + p.Code;
			object[] langParams = p.LangParams;
			message = Lang.Get(key, langParams);
		}
		game.eventManager?.TriggerIngameDiscovery(this, p.Code, message);
	}

	private void RemoveBlockLight(Packet_Server packet)
	{
		Packet_RemoveBlockLight pkt = packet.RemoveBlockLight;
		game.BlockAccessor.RemoveBlockLight(new byte[3]
		{
			(byte)pkt.LightH,
			(byte)pkt.LightS,
			(byte)pkt.LightV
		}, new BlockPos(pkt.PosX, pkt.PosY, pkt.PosZ));
	}

	private void HandleLandClaims(Packet_Server packet)
	{
		Packet_LandClaims pkt = packet.LandClaims;
		if (pkt.Allclaims != null && pkt.Allclaims.Length != 0)
		{
			game.WorldMap.LandClaims = (from b in pkt.Allclaims
				where b != null
				select b into claim
				select SerializerUtil.Deserialize<LandClaim>(claim.Data)).ToList();
		}
		else if (pkt.Addclaims != null)
		{
			game.WorldMap.LandClaims.AddRange(from b in pkt.Addclaims
				where b != null
				select b into claim
				select SerializerUtil.Deserialize<LandClaim>(claim.Data));
		}
		game.WorldMap.RebuildLandClaimPartitions();
	}

	private void HandleRoles(Packet_Server packet)
	{
		Packet_Roles pkt = packet.Roles;
		game.WorldMap.RolesByCode = new Dictionary<string, PlayerRole>();
		for (int i = 0; i < pkt.RolesCount; i++)
		{
			Packet_Role rolePkt = pkt.Roles[i];
			game.WorldMap.RolesByCode[rolePkt.Code] = new PlayerRole
			{
				Code = rolePkt.Code,
				PrivilegeLevel = rolePkt.PrivilegeLevel
			};
		}
	}
}
