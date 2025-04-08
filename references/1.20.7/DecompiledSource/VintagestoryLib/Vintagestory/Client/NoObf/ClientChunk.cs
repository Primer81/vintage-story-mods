using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientChunk : WorldChunk, IClientChunk, IWorldChunk
{
	public static Stopwatch ReadWriteStopWatch;

	internal int BlockEntitiesCount;

	private HashSet<int> lightPositions;

	public ClientMapChunk clientmapchunk;

	private Dictionary<string, byte[]> moddata;

	internal ModelDataPoolLocation[] centerModelPoolLocations;

	internal ModelDataPoolLocation[] edgeModelPoolLocations;

	internal bool queuedForUpload;

	internal long lastTesselationMs;

	internal bool loadedFromServer;

	internal int quantityDrawn;

	internal int quantityRelit;

	internal int quantityOverloads;

	internal bool enquedForRedraw;

	internal bool shouldSunRelight;

	public ushort traversability;

	public bool traversabilityFresh;

	public static int[,] traversabilityMapping;

	public Bools CullVisible = new Bools(a: true, b: true);

	public static int bufIndex;

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

	public override IMapChunk MapChunk => clientmapchunk;

	public bool LoadedFromServer => loadedFromServer;

	internal void RemoveDataPoolLocations(ChunkRenderer chunkRenderer)
	{
		RemoveCenterDataPoolLocations(chunkRenderer);
		RemoveEdgeDataPoolLocations(chunkRenderer);
	}

	internal int RemoveEdgeDataPoolLocations(ChunkRenderer chunkRenderer)
	{
		if (edgeModelPoolLocations != null)
		{
			chunkRenderer.RemoveDataPoolLocations(edgeModelPoolLocations);
			edgeModelPoolLocations = null;
			return 1;
		}
		return 0;
	}

	internal int RemoveCenterDataPoolLocations(ChunkRenderer chunkRenderer)
	{
		if (centerModelPoolLocations != null)
		{
			chunkRenderer.RemoveDataPoolLocations(centerModelPoolLocations);
			centerModelPoolLocations = null;
			return 1;
		}
		return 0;
	}

	public bool IsTraversable(BlockFacing from, BlockFacing to)
	{
		int bitIndex = traversabilityMapping[from.Index, to.Index];
		if (traversabilityFresh)
		{
			return ((traversability >> bitIndex) & 1) > 0;
		}
		return true;
	}

	public void SetTraversable(int from, int to)
	{
		int bitIndex = traversabilityMapping[from, to];
		traversability |= (ushort)(1 << bitIndex);
	}

	public void ClearTraversable()
	{
		traversability = 0;
	}

	static ClientChunk()
	{
		ReadWriteStopWatch = new Stopwatch();
		traversabilityMapping = new int[6, 6];
		ReadWriteStopWatch.Start();
		traversabilityMapping[BlockFacing.NORTH.Index, BlockFacing.EAST.Index] = 0;
		traversabilityMapping[BlockFacing.EAST.Index, BlockFacing.NORTH.Index] = 0;
		traversabilityMapping[BlockFacing.NORTH.Index, BlockFacing.WEST.Index] = 1;
		traversabilityMapping[BlockFacing.WEST.Index, BlockFacing.NORTH.Index] = 1;
		traversabilityMapping[BlockFacing.NORTH.Index, BlockFacing.SOUTH.Index] = 2;
		traversabilityMapping[BlockFacing.SOUTH.Index, BlockFacing.NORTH.Index] = 2;
		traversabilityMapping[BlockFacing.NORTH.Index, BlockFacing.UP.Index] = 3;
		traversabilityMapping[BlockFacing.UP.Index, BlockFacing.NORTH.Index] = 3;
		traversabilityMapping[BlockFacing.NORTH.Index, BlockFacing.DOWN.Index] = 4;
		traversabilityMapping[BlockFacing.DOWN.Index, BlockFacing.NORTH.Index] = 4;
		traversabilityMapping[BlockFacing.EAST.Index, BlockFacing.SOUTH.Index] = 5;
		traversabilityMapping[BlockFacing.SOUTH.Index, BlockFacing.EAST.Index] = 5;
		traversabilityMapping[BlockFacing.EAST.Index, BlockFacing.WEST.Index] = 6;
		traversabilityMapping[BlockFacing.WEST.Index, BlockFacing.EAST.Index] = 6;
		traversabilityMapping[BlockFacing.EAST.Index, BlockFacing.UP.Index] = 7;
		traversabilityMapping[BlockFacing.UP.Index, BlockFacing.EAST.Index] = 7;
		traversabilityMapping[BlockFacing.EAST.Index, BlockFacing.DOWN.Index] = 8;
		traversabilityMapping[BlockFacing.DOWN.Index, BlockFacing.EAST.Index] = 8;
		traversabilityMapping[BlockFacing.SOUTH.Index, BlockFacing.WEST.Index] = 9;
		traversabilityMapping[BlockFacing.WEST.Index, BlockFacing.SOUTH.Index] = 9;
		traversabilityMapping[BlockFacing.SOUTH.Index, BlockFacing.UP.Index] = 10;
		traversabilityMapping[BlockFacing.UP.Index, BlockFacing.SOUTH.Index] = 10;
		traversabilityMapping[BlockFacing.SOUTH.Index, BlockFacing.DOWN.Index] = 11;
		traversabilityMapping[BlockFacing.DOWN.Index, BlockFacing.SOUTH.Index] = 11;
		traversabilityMapping[BlockFacing.WEST.Index, BlockFacing.UP.Index] = 12;
		traversabilityMapping[BlockFacing.UP.Index, BlockFacing.WEST.Index] = 12;
		traversabilityMapping[BlockFacing.WEST.Index, BlockFacing.DOWN.Index] = 13;
		traversabilityMapping[BlockFacing.DOWN.Index, BlockFacing.WEST.Index] = 13;
		traversabilityMapping[BlockFacing.UP.Index, BlockFacing.DOWN.Index] = 14;
		traversabilityMapping[BlockFacing.DOWN.Index, BlockFacing.UP.Index] = 14;
	}

	public static ClientChunk CreateNew(ClientChunkDataPool datapool)
	{
		return new ClientChunk
		{
			chunkdataVersion = 2,
			PotentialBlockOrLightingChanges = true,
			chunkdata = datapool.Request(),
			datapool = datapool,
			MaybeBlocks = datapool.OnlyAirBlocksData
		};
	}

	public static ClientChunk CreateNewCompressed(ChunkDataPool datapool, byte[] blocksCompressed, byte[] lightCompressed, byte[] lightSatCompressed, byte[] fluidsCompressed, byte[] moddata, int compver)
	{
		ClientChunk chunk = new ClientChunk();
		chunk.datapool = datapool;
		chunk.chunkdataVersion = compver;
		chunk.lightCompressed = lightCompressed;
		chunk.lightSatCompressed = lightSatCompressed;
		chunk.fluidsCompressed = fluidsCompressed;
		chunk.lastReadOrWrite = Environment.TickCount;
		chunk.moddata = SerializerUtil.Deserialize<Dictionary<string, byte[]>>(moddata);
		chunk.LiveModData = new Dictionary<string, object>();
		chunk.MaybeBlocks = datapool.OnlyAirBlocksData;
		chunk.blocksCompressed = blocksCompressed;
		if (blocksCompressed == null || lightCompressed == null || lightSatCompressed == null)
		{
			chunk.Unpack_MaybeNullData();
		}
		return chunk;
	}

	private ClientChunk()
	{
	}

	public bool ChunkHasData()
	{
		ChunkData chunkData = chunkdata;
		if (chunkData == null || !chunkData.HasData())
		{
			return base.blocksCompressed != null;
		}
		return true;
	}

	internal void SetVisible(bool visible)
	{
		CullVisible[(bufIndex + 1) % 2] = visible;
	}

	internal bool IsFrustumVisible()
	{
		if (edgeModelPoolLocations == null || centerModelPoolLocations == null)
		{
			return false;
		}
		if (edgeModelPoolLocations.Length == 0 && centerModelPoolLocations.Length == 0)
		{
			return false;
		}
		if (centerModelPoolLocations.Length == 0)
		{
			return edgeModelPoolLocations[0].FrustumVisible;
		}
		return centerModelPoolLocations[0].FrustumVisible;
	}

	public virtual bool TemporaryUnpack(int[] blocks)
	{
		lock (packUnpackLock)
		{
			if (chunkdata != null)
			{
				chunkdata.CopyBlocksTo(blocks);
			}
			else
			{
				ChunkData.UnpackBlocksTo(blocks, base.blocksCompressed, base.lightSatCompressed, chunkdataVersion);
			}
		}
		return true;
	}

	internal void LoadEntitiesFromPacket(Packet_Entity[] entities, int entitiesCount, ClientMain game)
	{
		for (int i = 0; i < entitiesCount; i++)
		{
			Entity entity = ClientSystemEntities.createOrUpdateEntityFromPacket(entities[i], game);
			if (entity != null)
			{
				AddEntity(entity);
				if (!game.LoadedEntities.ContainsKey(entity.EntityId))
				{
					game.LoadedEntities[entity.EntityId] = entity;
					game.eventManager?.TriggerEntityLoaded(entity);
				}
			}
		}
	}

	internal void PreLoadBlockEntitiesFromPacket(Packet_BlockEntity[] blockEntities, int blockEntitiesCount, ClientMain game)
	{
		BlockEntities.Clear();
		for (int i = 0; i < blockEntitiesCount; i++)
		{
			Packet_BlockEntity packet = blockEntities[i];
			BlockEntity blockEntity = ClientMain.ClassRegistry.CreateBlockEntity(packet.Classname);
			using (MemoryStream ms = new MemoryStream(packet.Data))
			{
				using BinaryReader reader = new BinaryReader(ms);
				TreeAttribute tree = new TreeAttribute();
				tree.FromBytes(reader);
				Block block = GetLocalBlockAtBlockPos(game, tree.GetInt("posx"), tree.GetInt("posy"), tree.GetInt("posz"));
				try
				{
					blockEntity.CreateBehaviors(block, game);
					blockEntity.FromTreeAttributes(tree, game);
				}
				catch (Exception e)
				{
					BlockPos pos = new BlockPos(packet.PosX, packet.PosY, packet.PosZ);
					game.Logger.Error("At position " + pos?.ToString() + " with block " + block.Code.ToShortString() + ", {0} threw an error when being created:", packet.Classname);
					game.Logger.Error(e);
				}
			}
			BlockEntities[blockEntity.Pos] = blockEntity;
		}
		BlockEntitiesCount = blockEntitiesCount;
	}

	internal void InitBlockEntitiesFromPacket(ClientMain game)
	{
		foreach (BlockEntity be in BlockEntities.Values)
		{
			try
			{
				be.Initialize(game.api);
			}
			catch (Exception e)
			{
				if (be != null)
				{
					if (game.ClassRegistryInt != null)
					{
						string classname = game.ClassRegistryInt.blockEntityTypeToClassnameMapping[be.GetType()];
						game.Logger.Error("Exception thrown when initializing a block entity with classname {0}:", classname);
					}
					else
					{
						game.Logger.Error("Exception thrown when initializing a block entity {0}:", be.GetType());
					}
					game.Logger.Error(e);
				}
				else
				{
					game.Logger.Error("Exception thrown when initializing a block entity, because it's null. Seems to be a corrupt chunk.");
				}
			}
			if (ScreenManager.FrameProfiler.Enabled)
			{
				ScreenManager.FrameProfiler.Mark("initbe-" + game.ClassRegistryInt.blockEntityTypeToClassnameMapping[be.GetType()]);
			}
		}
	}

	internal void AddOrUpdateBlockEntityFromPacket(Packet_BlockEntity p, ClientMain game)
	{
		BlockPos pos = new BlockPos(p.PosX, p.PosY, p.PosZ);
		if (p.Data == null && p.Classname == null)
		{
			RemoveBlockEntity(game, pos);
			return;
		}
		if (BlockEntities.TryGetValue(pos, out var blockentity))
		{
			Type type = ClientMain.ClassRegistry.GetBlockEntityType(p.Classname);
			if (blockentity.GetType() == type)
			{
				BinaryReader reader = new BinaryReader(new MemoryStream(p.Data));
				ITreeAttribute tree = new TreeAttribute();
				tree.FromBytes(reader);
				try
				{
					blockentity.FromTreeAttributes(tree, game);
					return;
				}
				catch (Exception e)
				{
					game.Logger.Error("At position " + pos?.ToString() + ", BlockEntity {0} threw an error when being updated:", p.Classname);
					game.Logger.Error(e);
					return;
				}
			}
			RemoveBlockEntity(game, pos);
		}
		BlockEntity be = ClientSystemEntities.createBlockEntityFromPacket(p, game);
		try
		{
			be.Initialize(game.api);
		}
		catch (Exception e2)
		{
			game.Logger.Error("Exception thrown at " + pos?.ToString() + " when initializing a block entity with classname {0}:", p.Classname);
			game.Logger.Error(e2);
		}
		AddBlockEntity(be);
	}

	private void RemoveBlockEntity(ClientMain game, BlockPos pos)
	{
		game.WorldMap.GetBlockEntity(pos)?.OnBlockRemoved();
		RemoveBlockEntity(pos);
	}

	public override void FinishLightDoubleBuffering()
	{
		if (chunkdata == null)
		{
			Unpack();
		}
		((ClientChunkData)chunkdata)?.FinishLightDoubleBuffering();
	}

	public void SetVisibility(bool visible)
	{
		bool hidden = !visible;
		if (centerModelPoolLocations != null)
		{
			ModelDataPoolLocation[] array = centerModelPoolLocations;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Hide = hidden;
			}
		}
		if (edgeModelPoolLocations != null)
		{
			ModelDataPoolLocation[] array = edgeModelPoolLocations;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Hide = hidden;
			}
		}
	}

	public void SetPoolLocations(ref ModelDataPoolLocation[] target, ModelDataPoolLocation[] modelDataPoolLocations, bool hidden)
	{
		if (hidden && modelDataPoolLocations != null)
		{
			for (int i = 0; i < modelDataPoolLocations.Length; i++)
			{
				modelDataPoolLocations[i].Hide = hidden;
			}
		}
		target = modelDataPoolLocations;
	}

	public bool GetHiddenState(ref ModelDataPoolLocation[] target)
	{
		bool hidden = false;
		if (target != null && target.Length != 0)
		{
			hidden = target[0].Hide;
		}
		return hidden;
	}
}
