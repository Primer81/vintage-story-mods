using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerPackets
{
	public static Packet_Server IngameError(string code, string text, params object[] langargs)
	{
		Packet_Server p = new Packet_Server();
		p.Id = 68;
		p.IngameError = new Packet_IngameError
		{
			Message = text,
			Code = code
		};
		if (langargs != null)
		{
			p.IngameError.SetLangParams(langargs.Select((object e) => (e != null) ? e.ToString() : "").ToArray());
		}
		return p;
	}

	public static Packet_Server IngameDiscovery(string code, string text, params object[] langargs)
	{
		Packet_Server p = new Packet_Server();
		p.Id = 69;
		p.IngameDiscovery = new Packet_IngameDiscovery
		{
			Message = text,
			Code = code
		};
		if (langargs != null)
		{
			p.IngameDiscovery.SetLangParams(langargs.Select((object e) => (e != null) ? e.ToString() : "").ToArray());
		}
		return p;
	}

	public static Packet_Server ChatLine(int groupid, string text, EnumChatType chatType, string data)
	{
		return new Packet_Server
		{
			Id = 8,
			Chatline = new Packet_ChatLine
			{
				Message = text,
				Groupid = groupid,
				ChatType = (int)chatType,
				Data = data
			}
		};
	}

	public static Packet_Server LevelInitialize(int maxViewDistance)
	{
		return new Packet_Server
		{
			Id = 4,
			LevelInitialize = new Packet_ServerLevelInitialize
			{
				ServerChunkSize = MagicNum.ServerChunkSize,
				ServerMapChunkSize = MagicNum.ServerChunkSize,
				ServerMapRegionSize = MagicNum.MapRegionSize,
				MaxViewDistance = maxViewDistance
			}
		};
	}

	public static Packet_Server LevelFinalize()
	{
		return new Packet_Server
		{
			Id = 6,
			LevelFinalize = new Packet_ServerLevelFinalize()
		};
	}

	public static byte[] Serialize(Packet_Server packet, IntRef retLength)
	{
		CitoMemoryStream ms = new CitoMemoryStream();
		Packet_ServerSerializer.Serialize(ms, packet);
		byte[] result = ms.ToArray();
		retLength.value = ms.Position();
		return result;
	}

	internal static Packet_Server Ping()
	{
		return new Packet_Server
		{
			Id = 2,
			Ping = new Packet_ServerPing()
		};
	}

	internal static Packet_Server DisconnectPlayer(string disconnectReason)
	{
		Packet_Server packet_Server = new Packet_Server();
		packet_Server.Id = 9;
		packet_Server.DisconnectPlayer = new Packet_ServerDisconnectPlayer();
		packet_Server.DisconnectPlayer.DisconnectReason = disconnectReason;
		return packet_Server;
	}

	internal static Packet_Server AnswerQuery(Packet_ServerQueryAnswer answer)
	{
		return new Packet_Server
		{
			Id = 28,
			QueryAnswer = answer
		};
	}

	public static Packet_EntityAttributes GetEntityPacket(FastMemoryStream ms, Entity entity)
	{
		BinaryWriter writer = new BinaryWriter(ms);
		Packet_EntityAttributes obj = new Packet_EntityAttributes
		{
			EntityId = entity.EntityId
		};
		entity.ToBytes(writer, forClient: true);
		obj.Data = ms.ToArray();
		return obj;
	}

	public static Packet_EntityAttributes GetEntityDebugAttributePacket(FastMemoryStream ms, Entity entity)
	{
		BinaryWriter writer = new BinaryWriter(ms);
		Packet_EntityAttributes obj = new Packet_EntityAttributes
		{
			EntityId = entity.EntityId
		};
		entity.DebugAttributes.ToBytes(writer);
		obj.Data = ms.ToArray();
		return obj;
	}

	public static Packet_EntityAttributeUpdate GetEntityPartialAttributePacket(FastMemoryStream ms, Entity entity)
	{
		Packet_EntityAttributeUpdate p = new Packet_EntityAttributeUpdate();
		entity.WatchedAttributes.GetDirtyPathData(ms, out var paths, out var datas);
		Packet_PartialAttribute[] attrs = new Packet_PartialAttribute[paths.Length];
		for (int i = 0; i < attrs.Length; i++)
		{
			attrs[i] = new Packet_PartialAttribute();
			attrs[i].Path = paths[i];
			attrs[i].SetData(datas[i]);
		}
		p.SetAttributes(attrs);
		p.EntityId = entity.EntityId;
		return p;
	}

	public static Packet_Server GetBulkEntityAttributesPacket(List<Packet_EntityAttributes> fullPackets, List<Packet_EntityAttributeUpdate> partialPackets)
	{
		Packet_BulkEntityAttributes bulkpacket = new Packet_BulkEntityAttributes();
		bulkpacket.SetFullUpdates(fullPackets.ToArray());
		bulkpacket.SetPartialUpdates(partialPackets.ToArray());
		return new Packet_Server
		{
			Id = 60,
			BulkEntityAttributes = bulkpacket
		};
	}

	public static Packet_Server GetBulkEntityDebugAttributesPacket(List<Packet_EntityAttributes> fullPackets)
	{
		Packet_BulkEntityDebugAttributes bulkpacket = new Packet_BulkEntityDebugAttributes();
		bulkpacket.SetFullUpdates(fullPackets.ToArray());
		return new Packet_Server
		{
			Id = 62,
			BulkEntityDebugAttributes = bulkpacket
		};
	}

	public static Packet_Server GetEntityAttributesPacket(Entity entity)
	{
		Packet_EntityAttributes p = new Packet_EntityAttributes();
		MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		entity.ToBytes(writer, forClient: true);
		p.SetData(ms.ToArray());
		p.EntityId = entity.EntityId;
		return new Packet_Server
		{
			Id = 37,
			EntityAttributes = p
		};
	}

	public static Packet_Server GetEntityAttributesUpdatePacket(Entity entity)
	{
		Packet_EntityAttributeUpdate p = new Packet_EntityAttributeUpdate();
		entity.WatchedAttributes.GetDirtyPathData(out var paths, out var datas);
		Packet_PartialAttribute[] attrs = new Packet_PartialAttribute[paths.Length];
		for (int i = 0; i < attrs.Length; i++)
		{
			attrs[i] = new Packet_PartialAttribute();
			attrs[i].Path = paths[i];
			attrs[i].SetData(datas[i]);
		}
		p.SetAttributes(attrs);
		p.EntityId = entity.EntityId;
		return new Packet_Server
		{
			Id = 38,
			EntityAttributeUpdate = p
		};
	}

	public static Packet_Server GetFullEntityPacket(Entity entity, FastMemoryStream ms, BinaryWriter writer)
	{
		return new Packet_Server
		{
			Id = 33,
			Entity = GetEntityPacket(entity, ms, writer)
		};
	}

	public static Packet_Server GetEntityDespawnPacket(List<EntityDespawn> despawns)
	{
		long[] entityIds = despawns.Select((EntityDespawn item) => item.Entity.EntityId).ToArray();
		int[] despawnReasons = despawns.Select((EntityDespawn item) => (int)((item.DespawnData != null) ? item.DespawnData.Reason : EnumDespawnReason.Death)).ToArray();
		int[] damageSource = despawns.Select((EntityDespawn item) => (int)((item.DespawnData != null) ? ((item.DespawnData.DamageSourceForDeath != null) ? item.DespawnData.DamageSourceForDeath.Source : EnumDamageSource.Unknown) : EnumDamageSource.Block)).ToArray();
		Packet_EntityDespawn p = new Packet_EntityDespawn();
		p.SetEntityId(entityIds);
		p.SetDeathDamageSource(damageSource);
		p.SetDespawnReason(despawnReasons);
		return new Packet_Server
		{
			Id = 36,
			EntityDespawn = p
		};
	}

	public static Packet_Server GetEntitySpawnPacket(List<Entity> spawns)
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return GetEntitySpawnPacket(spawns, ms);
	}

	public static Packet_Server GetEntitySpawnPacket(List<Entity> spawns, FastMemoryStream ms)
	{
		ms.Reset();
		BinaryWriter writer = new BinaryWriter(ms);
		Packet_Entity[] packets = (from entity in spawns
			where entity.Alive
			select entity into item
			select (Packet_Entity)(item.packet ?? (item.packet = GetEntityPacket(item, ms, writer)))).ToArray();
		return new Packet_Server
		{
			Id = 34,
			EntitySpawn = new Packet_EntitySpawn
			{
				Entity = packets,
				EntityCount = packets.Length,
				EntityLength = packets.Length
			}
		};
	}

	public static Packet_Entity GetEntityPacket(Entity entity)
	{
		using FastMemoryStream ms = new FastMemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		return new Packet_Entity
		{
			EntityId = entity.EntityId,
			Data = getEntityDataForClient(entity, ms, writer),
			EntityType = entity.Code.ToShortString(),
			SimulationRange = entity.SimulationRange
		};
	}

	public static Packet_Entity GetEntityPacket(Entity entity, FastMemoryStream ms, BinaryWriter writer)
	{
		return new Packet_Entity
		{
			EntityId = entity.EntityId,
			Data = getEntityDataForClient(entity, ms, writer),
			EntityType = entity.Code.ToShortString(),
			SimulationRange = entity.SimulationRange
		};
	}

	public static Packet_EntityPosition getEntityPositionPacket(EntityPos pos, Entity entity, int tick)
	{
		Packet_EntityPosition packet = new Packet_EntityPosition
		{
			EntityId = entity.EntityId,
			X = CollectibleNet.SerializeDoublePrecise(pos.X),
			Y = CollectibleNet.SerializeDoublePrecise(pos.Y),
			Z = CollectibleNet.SerializeDoublePrecise(pos.Z),
			Yaw = CollectibleNet.SerializeFloatPrecise(pos.Yaw),
			Pitch = CollectibleNet.SerializeFloatPrecise(pos.Pitch),
			Roll = CollectibleNet.SerializeFloatPrecise(pos.Roll),
			MotionX = CollectibleNet.SerializeDoublePrecise(pos.Motion.X),
			MotionY = CollectibleNet.SerializeDoublePrecise(pos.Motion.Y),
			MotionZ = CollectibleNet.SerializeDoublePrecise(pos.Motion.Z),
			Teleport = entity.IsTeleport,
			HeadYaw = CollectibleNet.SerializeFloatPrecise(pos.HeadYaw),
			HeadPitch = CollectibleNet.SerializeFloatPrecise(pos.HeadPitch),
			PositionVersion = entity.WatchedAttributes.GetInt("positionVersionNumber"),
			Tick = tick
		};
		if (entity is EntityAgent agent)
		{
			packet.BodyYaw = CollectibleNet.SerializeFloatPrecise(agent.BodyYaw);
			packet.Controls = agent.Controls.ToInt();
		}
		return packet;
	}

	public static byte[] getEntityDataForClient(Entity entity, FastMemoryStream ms, BinaryWriter writer)
	{
		ms.Reset();
		entity.ToBytes(writer, forClient: true);
		return ms.ToArray();
	}

	internal static Packet_BlockEntity getBlockEntityPacket(BlockEntity blockEntity, MemoryStream ms, BinaryWriter writer)
	{
		return new Packet_BlockEntity
		{
			Classname = ServerMain.ClassRegistry.blockEntityTypeToClassnameMapping[blockEntity.GetType()],
			Data = getBlockEntityData(blockEntity, ms, writer),
			PosX = blockEntity.Pos.X,
			PosY = blockEntity.Pos.InternalY,
			PosZ = blockEntity.Pos.Z
		};
	}

	private static byte[] getBlockEntityData(BlockEntity blockEntity, MemoryStream ms, BinaryWriter writer)
	{
		ms.Clear();
		TreeAttribute tree = new TreeAttribute();
		blockEntity.ToTreeAttributes(tree);
		tree.ToBytes(writer);
		return ms.ToArray();
	}
}
