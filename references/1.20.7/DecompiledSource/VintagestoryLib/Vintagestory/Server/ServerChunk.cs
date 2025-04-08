using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

[ProtoContract]
public class ServerChunk : WorldChunk, IServerChunk, IWorldChunk
{
	public static Stopwatch ReadWriteStopWatch;

	[ProtoMember(6)]
	private List<byte[]> EntitiesSerialized;

	[ProtoMember(7)]
	public int BlockEntitiesCount;

	[ProtoMember(8)]
	private List<byte[]> BlockEntitiesSerialized;

	[ProtoMember(9)]
	private Dictionary<string, byte[]> moddata;

	[ProtoMember(10)]
	protected HashSet<int> lightPositions;

	[ProtoMember(11)]
	public Dictionary<string, byte[]> ServerSideModdata;

	[ProtoMember(12)]
	public string GameVersionCreated;

	[ProtoMember(13)]
	public bool EmptyBeforeSave;

	[ProtoMember(14)]
	public byte[] DecorsSerialized;

	[ProtoMember(15)]
	public int savedCompressionVersion;

	[ProtoMember(17)]
	public int BlocksPlaced;

	[ProtoMember(18)]
	public int BlocksRemoved;

	public ServerMapChunk serverMapChunk;

	public bool DirtyForSaving;

	[ProtoMember(1)]
	internal new byte[] blocksCompressed
	{
		get
		{
			return base.blocksCompressed;
		}
		set
		{
			base.blocksCompressed = value;
		}
	}

	[ProtoMember(2)]
	internal new byte[] lightCompressed
	{
		get
		{
			return base.lightCompressed;
		}
		set
		{
			base.lightCompressed = value;
		}
	}

	[ProtoMember(3)]
	internal new byte[] lightSatCompressed
	{
		get
		{
			return base.lightSatCompressed;
		}
		set
		{
			base.lightSatCompressed = value;
		}
	}

	[ProtoMember(16)]
	internal byte[] liquidsCompressed
	{
		get
		{
			return base.fluidsCompressed;
		}
		set
		{
			base.fluidsCompressed = value;
		}
	}

	[ProtoMember(5)]
	public new int EntitiesCount
	{
		get
		{
			return base.EntitiesCount;
		}
		set
		{
			base.EntitiesCount = value;
		}
	}

	public override IMapChunk MapChunk => serverMapChunk;

	public bool NotAtEdge
	{
		get
		{
			if (serverMapChunk != null)
			{
				return serverMapChunk.NeighboursLoaded.Value() == 511;
			}
			return false;
		}
	}

	public override Dictionary<string, byte[]> ModData
	{
		get
		{
			return moddata;
		}
		set
		{
			if (value == null)
			{
				throw new NullReferenceException("ModData must not be set to null");
			}
			moddata = value;
		}
	}

	public override HashSet<int> LightPositions
	{
		get
		{
			return lightPositions;
		}
		set
		{
			lightPositions = value;
		}
	}

	string IServerChunk.GameVersionCreated => GameVersionCreated;

	int IServerChunk.BlocksPlaced => BlocksPlaced;

	int IServerChunk.BlocksRemoved => BlocksRemoved;

	static ServerChunk()
	{
		ReadWriteStopWatch = new Stopwatch();
		ReadWriteStopWatch.Start();
	}

	private ServerChunk()
	{
	}

	public static ServerChunk CreateNew(ChunkDataPool datapool)
	{
		ServerChunk serverChunk = new ServerChunk();
		serverChunk.datapool = datapool;
		serverChunk.PotentialBlockOrLightingChanges = true;
		serverChunk.chunkdataVersion = 2;
		serverChunk.chunkdata = datapool.Request();
		serverChunk.GameVersionCreated = "1.20.7";
		serverChunk.lightPositions = new HashSet<int>();
		serverChunk.moddata = new Dictionary<string, byte[]>();
		serverChunk.ServerSideModdata = new Dictionary<string, byte[]>();
		serverChunk.LiveModData = new Dictionary<string, object>();
		serverChunk.MaybeBlocks = datapool.OnlyAirBlocksData;
		serverChunk.MarkModified();
		return serverChunk;
	}

	public void RemoveEntitiesAndBlockEntities(IServerWorldAccessor server)
	{
		if (Entities != null)
		{
			EntityDespawnData reason = new EntityDespawnData
			{
				Reason = EnumDespawnReason.Unload
			};
			for (int i = 0; i < Entities.Length; i++)
			{
				Entity entity = Entities[i];
				if (entity == null)
				{
					if (i >= EntitiesCount)
					{
						break;
					}
				}
				else if (!(entity is EntityPlayer))
				{
					server.DespawnEntity(entity, reason);
				}
			}
		}
		foreach (KeyValuePair<BlockPos, BlockEntity> blockEntity in BlockEntities)
		{
			blockEntity.Value.OnBlockUnloaded();
		}
	}

	public void ClearData()
	{
		ChunkData oldData = chunkdata;
		PotentialBlockOrLightingChanges = true;
		chunkdataVersion = 2;
		chunkdata = datapool.Request();
		GameVersionCreated = "1.20.7";
		lightPositions = new HashSet<int>();
		moddata = new Dictionary<string, byte[]>();
		ServerSideModdata = new Dictionary<string, byte[]>();
		base.MaybeBlocks = datapool.OnlyAirBlocksData;
		MarkModified();
		base.Empty = true;
		if (oldData != null)
		{
			datapool.Free(oldData);
		}
	}

	public static ServerChunk FromBytes(byte[] serializedChunk, ChunkDataPool datapool, IWorldAccessor worldForResolve)
	{
		if (datapool == null)
		{
			throw new MissingFieldException("datapool cannot be null");
		}
		ServerChunk chunk;
		using (MemoryStream ms = new MemoryStream(serializedChunk))
		{
			chunk = Serializer.Deserialize<ServerChunk>(ms);
		}
		chunk.chunkdataVersion = chunk.savedCompressionVersion;
		chunk.datapool = datapool;
		if (chunk.blocksCompressed == null || chunk.lightCompressed == null || chunk.lightSatCompressed == null)
		{
			chunk.Unpack_MaybeNullData();
		}
		chunk.AfterDeserialization(worldForResolve);
		if (chunk.lightPositions == null)
		{
			chunk.lightPositions = new HashSet<int>();
		}
		if (chunk.moddata == null)
		{
			chunk.moddata = new Dictionary<string, byte[]>();
		}
		if (chunk.ServerSideModdata == null)
		{
			chunk.ServerSideModdata = new Dictionary<string, byte[]>();
		}
		if (chunk.LiveModData == null)
		{
			chunk.LiveModData = new Dictionary<string, object>();
		}
		chunk.MaybeBlocks = datapool.OnlyAirBlocksData;
		return chunk;
	}

	public byte[] ToBytes()
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return ToBytes(ms);
	}

	public byte[] ToBytes(FastMemoryStream ms)
	{
		lock (packUnpackLock)
		{
			if (!IsPacked())
			{
				Pack();
				blocksCompressed = blocksCompressedTmp;
				lightCompressed = lightCompressedTmp;
				lightSatCompressed = lightSatCompressedTmp;
				liquidsCompressed = fluidsCompressedTmp;
				chunkdataVersion = 2;
			}
			savedCompressionVersion = chunkdataVersion;
			ms.Reset();
			Serializer.Serialize((Stream)ms, this);
			return ms.ToArray();
		}
	}

	protected override void UpdateForVersion()
	{
		chunkdata.UpdateFluids();
		chunkdataVersion = 2;
		DirtyForSaving = true;
		PotentialBlockOrLightingChanges = true;
	}

	public void MarkToPack()
	{
		PotentialBlockOrLightingChanges = true;
	}

	[ProtoBeforeSerialization]
	private void BeforeSerialization()
	{
		lock (packUnpackLock)
		{
			foreach (KeyValuePair<string, object> var in base.LiveModData)
			{
				SetModdata(var.Key, var.Value);
			}
			EmptyBeforeSave = base.Empty;
			if (Entities == null)
			{
				Entities = new Entity[0];
			}
			EntitiesSerialized = new List<byte[]>(Entities.Length);
			int entitiesCount = 0;
			BlockEntitiesSerialized = new List<byte[]>(BlockEntities.Count);
			bool hasDecors = Decors != null && Decors.Count > 0;
			if (Entities.Length != 0 || BlockEntities.Count > 0 || hasDecors)
			{
				using FastMemoryStream ms = new FastMemoryStream();
				if (Entities.Length != 0)
				{
					entitiesCount = SerializeEntities(ms);
				}
				if (BlockEntities.Count > 0)
				{
					SerializeBlockEntities(ms);
				}
				if (hasDecors)
				{
					SerializeDecors(ms);
				}
			}
			EntitiesCount = entitiesCount;
			BlockEntitiesCount = BlockEntitiesSerialized.Count;
			if (!hasDecors)
			{
				DecorsSerialized = new byte[0];
			}
		}
	}

	private int SerializeEntities(FastMemoryStream ms)
	{
		int cnt = 0;
		BinaryWriter writer = new BinaryWriter(ms);
		for (int i = 0; i < Entities.Length; i++)
		{
			Entity entity = Entities[i];
			if (entity == null)
			{
				if (i >= EntitiesCount)
				{
					break;
				}
				continue;
			}
			if (!entity.StoreWithChunk)
			{
				cnt++;
				continue;
			}
			try
			{
				ms.Reset();
				writer.Write(ServerMain.ClassRegistry.GetEntityClassName(entity.GetType()));
				entity.ToBytes(writer, forClient: false);
				EntitiesSerialized.Add(ms.ToArray());
				cnt++;
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Error thrown trying to serialize entity with code {0}, will not save, sorry!", entity?.Code);
				ServerMain.Logger.Error(e);
			}
		}
		return cnt;
	}

	private void SerializeBlockEntities(FastMemoryStream ms)
	{
		BinaryWriter writer = new BinaryWriter(ms);
		foreach (KeyValuePair<BlockPos, BlockEntity> blockEntity in BlockEntities)
		{
			BlockEntity be = blockEntity.Value;
			try
			{
				ms.Reset();
				string classsName = ServerMain.ClassRegistry.blockEntityTypeToClassnameMapping[be.GetType()];
				writer.Write(classsName);
				TreeAttribute tree = new TreeAttribute();
				be.ToTreeAttributes(tree);
				tree.ToBytes(writer);
				BlockEntitiesSerialized.Add(ms.ToArray());
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Error thrown trying to serialize block entity {0} at {1}, will not save, sorry!", be?.GetType(), be?.Pos);
				ServerMain.Logger.Error(e);
			}
		}
	}

	private void SerializeDecors(FastMemoryStream ms)
	{
		ms.Reset();
		BinaryWriter writer = new BinaryWriter(ms);
		foreach (KeyValuePair<int, Block> val in Decors)
		{
			Block de = val.Value;
			writer.Write(val.Key);
			writer.Write(de.BlockId);
		}
		DecorsSerialized = ms.ToArray();
	}

	[ProtoAfterSerialization]
	private void AfterSerialization()
	{
		EntitiesSerialized = null;
		BlockEntitiesSerialized = null;
		DecorsSerialized = null;
	}

	private void AfterDeserialization(IWorldAccessor worldAccessorForResolve)
	{
		lock (packUnpackLock)
		{
			base.Empty = EmptyBeforeSave;
			if (EntitiesSerialized == null)
			{
				Entities = new Entity[0];
				EntitiesCount = 0;
			}
			else
			{
				Entities = new Entity[EntitiesSerialized.Count];
				int cnt = 0;
				for (int i = 0; i < Entities.Length; i++)
				{
					string className2 = "unknown";
					try
					{
						using MemoryStream input = new MemoryStream(EntitiesSerialized[i]);
						BinaryReader reader2 = new BinaryReader(input);
						className2 = reader2.ReadString();
						Entity entity2 = ServerMain.ClassRegistry.CreateEntity(className2);
						entity2.FromBytes(reader2, isSync: false, ((IServerWorldAccessor)worldAccessorForResolve).RemappedEntities);
						Entities[cnt++] = entity2;
					}
					catch (Exception e2)
					{
						ServerMain.Logger.Error("Failed loading an entity (type " + className2 + ") in a chunk. Will discard, sorry. Exception logged to verbose debug.");
						ServerMain.Logger.VerboseDebug("Failed loading an entity in a chunk. Will discard, sorry. Exception: {0}", LoggerBase.CleanStackTrace(e2.ToString()));
					}
				}
				EntitiesCount = cnt;
				EntitiesSerialized = null;
			}
			if (BlockEntitiesSerialized != null)
			{
				foreach (byte[] item in BlockEntitiesSerialized)
				{
					using MemoryStream input2 = new MemoryStream(item);
					using BinaryReader binaryReader = new BinaryReader(input2);
					string className;
					try
					{
						className = binaryReader.ReadString();
					}
					catch (Exception)
					{
						ServerMain.Logger.Error("Badly corrupted BlockEntity data in a chunk. Will discard it. Sorry.");
						goto end_IL_0160;
					}
					string blockCode = null;
					try
					{
						TreeAttribute tree = new TreeAttribute();
						tree.FromBytes(binaryReader);
						BlockEntity entity = ServerMain.ClassRegistry.CreateBlockEntity(className);
						Block block = null;
						blockCode = tree.GetString("blockCode");
						if (blockCode != null)
						{
							block = worldAccessorForResolve.GetBlock(new AssetLocation(blockCode));
						}
						if (block == null)
						{
							block = GetLocalBlockAtBlockPos(worldAccessorForResolve, tree.GetInt("posx"), tree.GetInt("posy"), tree.GetInt("posz"));
							if (block?.Code != null)
							{
								tree.SetString("blockCode", block.Code.ToShortString());
							}
						}
						if (block?.Code == null)
						{
							int posx = tree.GetInt("posx");
							int posy = tree.GetInt("posy");
							int posz = tree.GetInt("posz");
							worldAccessorForResolve.Logger.Notification("Block entity with classname {3} at {0}, {1}, {2} has a block that is null or whose code is null o.O? Won't load this block entity!", posx, posy, posz, className);
						}
						else
						{
							entity.CreateBehaviors(block, worldAccessorForResolve);
							entity.FromTreeAttributes(tree, worldAccessorForResolve);
							BlockEntities[entity.Pos] = entity;
						}
					}
					catch (Exception e)
					{
						ServerMain.Logger.Error("Failed loading blockentity {0} for block {1} in a chunk. Will discard it. Sorry. Exception logged to verbose debug.", className, blockCode);
						ServerMain.Logger.VerboseDebug("Failed loading a blockentity in a chunk. Will discard it. Sorry. Exception: {0}", LoggerBase.CleanStackTrace(e.ToString()));
					}
					end_IL_0160:;
				}
				BlockEntitiesCount = BlockEntities.Count;
				BlockEntitiesSerialized = null;
			}
			if (DecorsSerialized != null && DecorsSerialized.Length != 0)
			{
				Decors = new Dictionary<int, Block>();
				using (MemoryStream ms = new MemoryStream(DecorsSerialized))
				{
					BinaryReader reader = new BinaryReader(ms);
					while (reader.BaseStream.Position < reader.BaseStream.Length)
					{
						int index3d = reader.ReadInt32();
						int blockId = reader.ReadInt32();
						Block dec = worldAccessorForResolve.GetBlock(blockId);
						Decors.Add(index3d, dec);
					}
				}
				DecorsSerialized = null;
			}
			if (LightPositions == null)
			{
				LightPositions = new HashSet<int>();
			}
			if (moddata == null)
			{
				moddata = new Dictionary<string, byte[]>();
			}
		}
	}

	public override void AddEntity(Entity entity)
	{
		base.AddEntity(entity);
		MarkModified();
	}

	public override bool RemoveEntity(long entityId)
	{
		bool num = base.RemoveEntity(entityId);
		if (num)
		{
			MarkModified();
		}
		return num;
	}

	internal Packet_ServerChunk ToPacket(int posX, int posY, int posZ, bool withEntities = false)
	{
		Packet_ServerChunk packet = new Packet_ServerChunk
		{
			X = posX,
			Y = posY,
			Z = posZ
		};
		lock (packUnpackLock)
		{
			foreach (KeyValuePair<string, object> var in base.LiveModData)
			{
				SetModdata(var.Key, var.Value);
			}
			if (chunkdata == null && chunkdataVersion < 2)
			{
				Unpack();
			}
			if (chunkdata != null)
			{
				UpdateEmptyFlag();
				if (PotentialBlockOrLightingChanges)
				{
					chunkdataVersion = 2;
					byte[] bcTmp = null;
					byte[] lcTmp = null;
					byte[] lscTmp = null;
					byte[] lqTmp = null;
					chunkdata.CompressInto(ref bcTmp, ref lcTmp, ref lscTmp, ref lqTmp, chunkdataVersion);
					base.blocksCompressed = bcTmp;
					base.lightCompressed = lcTmp;
					base.lightSatCompressed = lscTmp;
					base.fluidsCompressed = lqTmp;
					PotentialBlockOrLightingChanges = false;
				}
				if (Environment.TickCount - lastReadOrWrite > MagicNum.UncompressedChunkTTL)
				{
					datapool.Free(chunkdata);
					base.MaybeBlocks = datapool.OnlyAirBlocksData;
					chunkdata = null;
				}
			}
			packet.Empty = (base.Empty ? 1 : 0);
			packet.SetBlocks(blocksCompressed);
			packet.SetLight(lightCompressed);
			packet.SetLightSat(lightSatCompressed);
			packet.SetLiquids(liquidsCompressed);
			packet.SetCompver(chunkdataVersion);
		}
		packet.SetModdata(SerializerUtil.Serialize(moddata));
		if (withEntities)
		{
			packet.SetEntities(GetEntitiesPackets());
		}
		packet.SetBlockEntities(GetBlockEntitiesPackets());
		if (LightPositions == null)
		{
			LightPositions = new HashSet<int>();
		}
		packet.SetLightPositions(LightPositions.ToArray());
		if (Decors != null)
		{
			foreach (KeyValuePair<int, Block> val in Decors)
			{
				packet.DecorsPosAdd(val.Key);
				packet.DecorsIdsAdd(val.Value.BlockId);
			}
		}
		return packet;
	}

	internal Packet_Entity[] GetEntitiesPackets()
	{
		Packet_Entity[] packets = new Packet_Entity[EntitiesCount];
		if (Entities != null && packets.Length != 0)
		{
			using FastMemoryStream ms = new FastMemoryStream();
			BinaryWriter writer = new BinaryWriter(ms);
			int j = 0;
			for (int i = 0; i < Entities.Length; i++)
			{
				Entity entity = Entities[i];
				if (entity == null)
				{
					if (i >= EntitiesCount)
					{
						break;
					}
				}
				else
				{
					packets[j++] = ServerPackets.GetEntityPacket(entity, ms, writer);
				}
			}
		}
		return packets;
	}

	internal Packet_BlockEntity[] GetBlockEntitiesPackets()
	{
		Packet_BlockEntity[] packets = new Packet_BlockEntity[BlockEntities.Count];
		if (packets.Length == 0)
		{
			return packets;
		}
		using MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		int i = 0;
		foreach (KeyValuePair<BlockPos, BlockEntity> val in BlockEntities)
		{
			if (val.Value != null)
			{
				packets[i++] = ServerPackets.getBlockEntityPacket(val.Value, ms, writer);
			}
		}
		return packets;
	}

	public override void MarkModified()
	{
		base.MarkModified();
		DirtyForSaving = true;
	}

	public void SetServerModdata(string key, byte[] data)
	{
		ServerSideModdata[key] = data;
	}

	public byte[] GetServerModdata(string key)
	{
		ServerSideModdata.TryGetValue(key, out var data);
		return data;
	}

	public void ClearAll(IServerWorldAccessor worldAccessor)
	{
		RemoveEntitiesAndBlockEntities(worldAccessor);
		ClearData();
		BlockEntities?.Clear();
		Decors?.Clear();
		Entities = null;
	}
}
