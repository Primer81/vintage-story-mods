using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientSystemEntities : ClientSystem
{
	public override string Name => "sce";

	public ClientSystemEntities(ClientMain game)
		: base(game)
	{
		game.PacketHandlers[40] = HandleEntitiesPacket;
		game.PacketHandlers[34] = HandleEntitySpawnPacket;
		game.PacketHandlers[36] = HandleEntityDespawnPacket;
		game.PacketHandlers[37] = HandleEntityAttributesPacket;
		game.PacketHandlers[38] = HandleEntityAttributeUpdatePacket;
		game.PacketHandlers[67] = HandleEntityPacket;
		game.PacketHandlers[33] = HandleEntityLoadedPacket;
		game.PacketHandlers[60] = HandleEntityBulkAttributesPacket;
		game.PacketHandlers[62] = HandleEntityBulkDebugAttributesPacket;
		game.RegisterGameTickListener(OnGameTick, 20);
		game.RegisterGameTickListener(UpdateEvery1000ms, 1000);
		game.eventManager.OnEntitySpawn.Add(delegate(Entity e)
		{
			OnEntitySpawnOrLoaded(e, loaded: false);
		});
		game.eventManager.OnEntityLoaded.Add(delegate(Entity e)
		{
			OnEntitySpawnOrLoaded(e, loaded: true);
		});
		game.eventManager.OnEntityDespawn.Add(OnEntityDespawn);
		game.eventManager.OnReloadShapes += EventManager_OnReloadShapes;
	}

	private void EventManager_OnReloadShapes()
	{
		AnimationCache.ClearCache(game.api);
		game.TesselatorManager.LoadEntityShapesAsync(game.EntityTypes, game.api);
		List<EntityProperties> entityTypes = game.EntityTypes;
		foreach (KeyValuePair<long, Entity> val in game.LoadedEntities)
		{
			if (val.Value?.Properties?.Client != null)
			{
				val.Value.Properties.Client.LoadedShapeForEntity = entityTypes.FirstOrDefault((EntityProperties et) => et.Code.Equals(val.Value.Properties.Code))?.Client?.LoadedShape;
				if (val.Value.AnimManager != null)
				{
					val.Value.AnimManager.Dispose();
					val.Value.AnimManager.LoadAnimator(game.api, val.Value, val.Value.Properties.Client.LoadedShapeForEntity, val.Value.AnimManager.Animator?.Animations, val.Value.requirePosesOnServer, "head");
				}
			}
		}
	}

	private void OnGameTick(float dt)
	{
		if (game.IsPaused)
		{
			return;
		}
		lock (game.EntityLoadQueueLock)
		{
			while (game.EntityLoadQueue.Count > 0)
			{
				Entity entity = game.EntityLoadQueue.Pop();
				if (!game.LoadedEntities.ContainsKey(entity.EntityId))
				{
					game.LoadedEntities[entity.EntityId] = entity;
					game.eventManager?.TriggerEntityLoaded(entity);
				}
			}
		}
		game.api.World.FrameProfiler.Mark("loadedEntityQueue-lockcontention");
		foreach (Entity item in (IEnumerable<Entity>)game.LoadedEntities.Values)
		{
			item.OnGameTick(dt);
		}
	}

	private void UpdateEvery1000ms(float dt)
	{
		foreach (Entity entity in game.LoadedEntities.Values)
		{
			entity.minHorRangeToClient = (float)entity.ServerPos.HorDistanceTo(game.EntityPlayer.Pos);
			long chunkindex3d = game.WorldMap.ChunkIndex3D(entity.Pos);
			if (entity.InChunkIndex3d != chunkindex3d)
			{
				game.UpdateEntityChunk(entity, chunkindex3d);
			}
		}
	}

	private void OnEntitySpawnOrLoaded(Entity entity, bool loaded)
	{
		if (game.EntityRenderers.ContainsKey(entity))
		{
			return;
		}
		if (ClientMain.ClassRegistry.EntityRendererClassNameToTypeMapping.ContainsKey(entity.Properties.Client.RendererName))
		{
			try
			{
				entity.Properties.Client.Renderer = ClientMain.ClassRegistry.CreateEntityRenderer(entity.Properties.Client.RendererName, entity, game.api);
				game.EntityRenderers.Add(entity, entity.Properties.Client.Renderer);
			}
			catch (Exception e)
			{
				game.Platform.Logger.Error("Exception while loading entity " + entity.GetType()?.ToString() + " and creating renderer, entity will be invisible!");
				if (entity is EntityItem ei)
				{
					game.Platform.Logger.Error("Was EntityItem with slot:" + ((ei.Slot == null) ? "null" : ei.Slot.ToString()));
				}
				game.Platform.Logger.Error(e);
			}
		}
		else
		{
			game.Platform.Logger.Error("Couldn't find renderer for entity " + entity.GetType()?.ToString() + ", entity will be invisible!");
		}
		if (loaded)
		{
			entity.OnEntityLoaded();
		}
		else
		{
			entity.OnEntitySpawn();
		}
		if (entity is EntityPlayer)
		{
			entity.InChunkIndex3d = 0L;
			if (game.PlayerByUid((entity as EntityPlayer).PlayerUID) is ClientPlayer clientPlayer)
			{
				clientPlayer.WarnIfEntityChanged(entity.EntityId, "spawn/loaded");
				clientPlayer.worlddata.EntityPlayer = entity as EntityPlayer;
				game.api.eventapi.TriggerPlayerEntitySpawn(clientPlayer);
			}
		}
	}

	private void OnEntityDespawn(Entity entity, EntityDespawnData despawnReason)
	{
		entity.OnEntityDespawn(despawnReason);
		game.RemoveEntityRenderer(entity);
		entity.Properties.Client.Renderer = null;
		if (entity is EntityPlayer)
		{
			IPlayer plr = game.PlayerByUid((entity as EntityPlayer).PlayerUID);
			if (plr != null)
			{
				game.api.eventapi.TriggerPlayerEntityDespawn(plr as IClientPlayer);
				(plr as ClientPlayer).worlddata.EntityPlayer = null;
			}
		}
	}

	private void HandleEntityLoadedPacket(Packet_Server serverpacket)
	{
		Packet_Entity packet = serverpacket.Entity;
		if (packet != null)
		{
			game.EnqueueMainThreadTask(delegate
			{
				Entity entity = createOrUpdateEntityFromPacket(packet, game);
				game.LoadedEntities[entity.EntityId] = entity;
				game.eventManager?.TriggerEntityLoaded(entity);
			}, "entityloadedpacket");
		}
	}

	private void HandleEntitiesPacket(Packet_Server serverpacket)
	{
		Packet_Entity[] packet = serverpacket.Entities.Entities;
		if (game.ClassRegistryInt.entityClassNameToTypeMapping.Count == 0)
		{
			game.Logger.Error($"Server sent me one or emore entity packets, but I cannot instantiate/update it, I don't have the entity class to type mapping (yet). Maybe server sent a packet too early? Will ignore.");
			return;
		}
		for (int i = 0; i < packet.Length && packet[i] != null; i++)
		{
			Entity entity = createOrUpdateEntityFromPacket(packet[i], game);
			if (entity == null)
			{
				throw new InvalidOperationException($"Server sent me an entity packet for entity {packet[i].EntityType}, but I cannot instantiate/update it, entity is null");
			}
			game.LoadedEntities[entity.EntityId] = entity;
			game.eventManager?.TriggerEntityLoaded(entity);
		}
	}

	private void HandleEntitySpawnPacket(Packet_Server serverpacket)
	{
		Packet_EntitySpawn packet = serverpacket.EntitySpawn;
		for (int i = 0; i < packet.EntityCount; i++)
		{
			game.LoadedEntities.TryGetValue(packet.Entity[i].EntityId, out var entity);
			if (entity == null)
			{
				entity = entityFromPacket(packet.Entity[i], game);
				if (entity != null)
				{
					game.LoadedEntities[entity.EntityId] = entity;
					game.eventManager?.TriggerEntitySpawn(entity);
				}
			}
			else
			{
				updateEntityFromPacket(packet.Entity[i], entity);
			}
		}
	}

	private void HandleEntityDespawnPacket(Packet_Server serverpacket)
	{
		for (int i = 0; i < serverpacket.EntityDespawn.EntityIdCount; i++)
		{
			long entityId = serverpacket.EntityDespawn.EntityId[i];
			if (game.LoadedEntities.TryGetValue(entityId, out var entity))
			{
				EntityDespawnData despawnReason = new EntityDespawnData
				{
					Reason = (EnumDespawnReason)serverpacket.EntityDespawn.DespawnReason[i],
					DamageSourceForDeath = new DamageSource
					{
						Source = (EnumDamageSource)serverpacket.EntityDespawn.DeathDamageSource[i]
					}
				};
				game.eventManager?.TriggerEntityDespawn(entity, despawnReason);
				game.WorldMap.GetClientChunk(entity.InChunkIndex3d)?.RemoveEntity(entityId);
				game.RemoveEntityRenderer(entity);
				entity.OnEntityDespawn(despawnReason);
				game.LoadedEntities.Remove(entityId);
			}
		}
	}

	private void HandleEntityBulkAttributesPacket(Packet_Server packet)
	{
		Packet_BulkEntityAttributes p = packet.BulkEntityAttributes;
		for (int j = 0; j < p.FullUpdatesCount; j++)
		{
			HandleEntityAttributesPacket(p.FullUpdates[j]);
		}
		for (int i = 0; i < p.PartialUpdatesCount; i++)
		{
			HandleEntityAttributeUpdatePacket(p.PartialUpdates[i]);
		}
	}

	private void HandleEntityBulkDebugAttributesPacket(Packet_Server packet)
	{
		Packet_BulkEntityDebugAttributes p = packet.BulkEntityDebugAttributes;
		for (int i = 0; i < p.FullUpdatesCount; i++)
		{
			game.LoadedEntities.TryGetValue(p.FullUpdates[i].EntityId, out var entity);
			if (entity != null)
			{
				BinaryReader reader = new BinaryReader(new MemoryStream(p.FullUpdates[i].Data));
				entity.DebugAttributes.FromBytes(reader);
				entity.DebugAttributes.MarkAllDirty();
			}
		}
	}

	private void HandleEntityAttributesPacket(Packet_Server serverpacket)
	{
		HandleEntityAttributesPacket(serverpacket.EntityAttributes);
	}

	private void HandleEntityAttributesPacket(Packet_EntityAttributes packet)
	{
		game.LoadedEntities.TryGetValue(packet.EntityId, out var entity);
		if (entity != null)
		{
			BinaryReader reader = new BinaryReader(new MemoryStream(packet.Data));
			entity.FromBytes(reader, isSync: true);
		}
	}

	private void HandleEntityAttributeUpdatePacket(Packet_Server serverpacket)
	{
		HandleEntityAttributeUpdatePacket(serverpacket.EntityAttributeUpdate);
	}

	private void HandleEntityAttributeUpdatePacket(Packet_EntityAttributeUpdate p)
	{
		game.LoadedEntities.TryGetValue(p.EntityId, out var entity);
		if (entity != null)
		{
			for (int i = 0; i < p.AttributesCount; i++)
			{
				Packet_PartialAttribute pkt = p.Attributes[i];
				entity.WatchedAttributes.PartialUpdate(pkt.Path, pkt.Data);
			}
		}
	}

	private void HandleEntityPacket(Packet_Server serverpacket)
	{
		Packet_EntityPacket p = serverpacket.EntityPacket;
		game.LoadedEntities.TryGetValue(p.EntityId, out var entity);
		entity?.OnReceivedServerPacket(p.Packetid, p.Data);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}

	public static Entity createOrUpdateEntityFromPacket(Packet_Entity entitypacket, ClientMain game, bool addToLoadQueue = false)
	{
		game.LoadedEntities.TryGetValue(entitypacket.EntityId, out var entity);
		if (entity == null)
		{
			entity = entityFromPacket(entitypacket, game);
			if (entity != null && addToLoadQueue)
			{
				lock (game.EntityLoadQueueLock)
				{
					game.EntityLoadQueue.Push(entity);
				}
			}
		}
		else
		{
			updateEntityFromPacket(entitypacket, entity);
		}
		return entity;
	}

	private static void updateEntityFromPacket(Packet_Entity entitypacket, Entity entity)
	{
		BinaryReader reader = new BinaryReader(new MemoryStream(entitypacket.Data));
		entity.FromBytes(reader, isSync: true);
	}

	private static Entity entityFromPacket(Packet_Entity entitypacket, ClientMain game)
	{
		EntityProperties entityType = game.GetEntityType(new AssetLocation(entitypacket.EntityType));
		if (entityType == null)
		{
			game.Logger.Error("Server sent a create entity packet for entity code '{0}', but no such entity exists?. Ignoring", entitypacket.EntityType);
			return null;
		}
		Entity entity = game.Api.ClassRegistry.CreateEntity(entityType);
		entity.SimulationRange = entitypacket.SimulationRange;
		entity.Api = game.Api;
		updateEntityFromPacket(entitypacket, entity);
		long index3d = game.WorldMap.ChunkIndex3D(entity.Pos);
		entity.Initialize(entityType.Clone(), game.api, index3d);
		entity.AfterInitialized(onFirstSpawn: false);
		game.WorldMap.GetClientChunk(index3d)?.AddEntity(entity);
		return entity;
	}

	public static EntityPos entityPosFromPacket(Packet_EntityPosition packet)
	{
		return new EntityPos
		{
			X = CollectibleNet.DeserializeDoublePrecise(packet.X),
			Y = CollectibleNet.DeserializeDoublePrecise(packet.Y),
			Z = CollectibleNet.DeserializeDoublePrecise(packet.Z),
			Yaw = CollectibleNet.DeserializeFloatPrecise(packet.Yaw),
			Pitch = CollectibleNet.DeserializeFloatPrecise(packet.Pitch),
			Roll = CollectibleNet.DeserializeFloatPrecise(packet.Roll),
			HeadYaw = CollectibleNet.DeserializeFloatPrecise(packet.HeadYaw),
			HeadPitch = CollectibleNet.DeserializeFloatPrecise(packet.HeadPitch)
		};
	}

	internal static BlockEntity createBlockEntityFromPacket(Packet_BlockEntity packet, ClientMain game)
	{
		BlockEntity blockEntity = ClientMain.ClassRegistry.CreateBlockEntity(packet.Classname);
		UpdateBlockEntityData(blockEntity, packet.Data, game, isNew: true);
		return blockEntity;
	}

	internal static void UpdateBlockEntityData(BlockEntity entity, byte[] data, ClientMain game, bool isNew)
	{
		BinaryReader reader = new BinaryReader(new MemoryStream(data));
		TreeAttribute tree = new TreeAttribute();
		tree.FromBytes(reader);
		if (isNew)
		{
			Block block = game.BlockAccessor.GetBlockRaw(tree.GetInt("posx"), tree.GetInt("posy"), tree.GetInt("posz"), 1);
			entity.CreateBehaviors(block, game);
		}
		entity.FromTreeAttributes(tree, game);
	}
}
