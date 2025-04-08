using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common.Database;
using Vintagestory.Server;

namespace Vintagestory.Common;

public class BlockAccessorMovable : BlockAccessorBase, IMiniDimension, IBlockAccessor
{
	protected double totalMass;

	private BlockAccessorBase parent;

	private Dictionary<long, IWorldChunk> chunks = new Dictionary<long, IWorldChunk>();

	private FastSetOfLongs dirtychunks = new FastSetOfLongs();

	public const int subDimensionSize = 16384;

	public const int subDimensionIndexZMultiplier = 4096;

	public const int originOffset = 8192;

	public const int MaxMiniDimensions = 16777216;

	public const int subDimensionSizeInChunks = 512;

	public const int dimensionSizeY = 32768;

	public const int dimensionId = 1;

	public const int DefaultLightLevel = 18;

	public EntityPos CurrentPos { get; set; }

	public bool Dirty { get; set; }

	public Vec3d CenterOfMass { get; set; }

	public bool TrackSelection { get; set; }

	public int BlocksPreviewSubDimension_Server { get; set; }

	public BlockPos selectionTrackingOriginalPos { get; set; }

	public int subDimensionId { get; set; }

	public BlockAccessorMovable(BlockAccessorBase parent, Vec3d pos)
		: base(parent.worldmap, parent.worldAccessor)
	{
		CurrentPos = new EntityPos(pos.X, pos.Y, pos.Z);
		this.parent = parent;
		BlocksPreviewSubDimension_Server = -1;
	}

	public virtual void SetSubDimensionId(int subId)
	{
		subDimensionId = subId;
	}

	public void SetSelectionTrackingSubId_Server(int subId)
	{
		BlocksPreviewSubDimension_Server = subId;
	}

	public virtual void ClearChunks()
	{
		if (parent.worldAccessor is IServerWorldAccessor worldAccessor)
		{
			foreach (KeyValuePair<long, IWorldChunk> chunk in chunks)
			{
				((ServerChunk)chunk.Value).ClearAll(worldAccessor);
			}
		}
		else
		{
			ChunkRenderer cr = ((ClientMain)parent.worldAccessor).chunkRenderer;
			foreach (KeyValuePair<long, IWorldChunk> chunk2 in chunks)
			{
				((ClientChunk)chunk2.Value).RemoveDataPoolLocations(cr);
			}
		}
		dirtychunks.Clear();
		if (CenterOfMass == null)
		{
			CenterOfMass = new Vec3d(0.0, 0.0, 0.0);
		}
		else
		{
			CenterOfMass.Set(0.0, 0.0, 0.0);
		}
		totalMass = 0.0;
	}

	public virtual void UnloadUnusedServerChunks()
	{
		List<long> toRemove = new List<long>();
		foreach (KeyValuePair<long, IWorldChunk> val in chunks)
		{
			if (val.Value.Empty)
			{
				toRemove.Add(val.Key);
				ChunkPos cpos = parent.worldmap.ChunkPosFromChunkIndex3D(val.Key);
				ServerSystemUnloadChunks.TryUnloadChunk(val.Key, cpos, (ServerChunk)val.Value, new List<ServerChunkWithCoord>(), (ServerMain)parent.worldAccessor);
			}
		}
		foreach (long key in toRemove)
		{
			chunks.Remove(key);
		}
	}

	public static bool ChunkCoordsInSameDimension(int cyA, int cyB)
	{
		return cyA / 1024 == cyB / 1024;
	}

	protected virtual IWorldChunk GetChunkAt(int posX, int posY, int posZ)
	{
		chunks.TryGetValue(ChunkIndex(posX, posY, posZ), out var chunk);
		return chunk;
	}

	protected virtual long ChunkIndex(int posX, int posY, int posZ)
	{
		int cx = posX / 32 % 512 + subDimensionId % 4096 * 512;
		int num = posY / 32 + 1024;
		int cz = posZ / 32 % 512 + subDimensionId / 4096 * 512;
		return ((long)num * (long)worldmap.index3dMulZ + cz) * worldmap.index3dMulX + cx;
	}

	public virtual void AdjustPosForSubDimension(BlockPos pos)
	{
		pos.X += subDimensionId % 4096 * 16384 + 8192;
		pos.Y += 8192;
		pos.Z += subDimensionId / 4096 * 16384 + 8192;
	}

	protected virtual IWorldChunk CreateChunkAt(int posX, int posY, int posZ)
	{
		long cindex = ChunkIndex(posX, posY, posZ);
		IWorldChunk chunk;
		if (worldAccessor.Side == EnumAppSide.Server)
		{
			ServerMain server = (ServerMain)worldAccessor;
			chunk = ServerChunk.CreateNew(server.serverChunkDataPool);
			chunk.Lighting.FloodWithSunlight(18);
			server.loadedChunksLock.AcquireWriteLock();
			try
			{
				if (server.loadedChunks.TryGetValue(cindex, out var oldchunk))
				{
					oldchunk.Dispose();
				}
				server.loadedChunks[cindex] = (ServerChunk)chunk;
			}
			finally
			{
				server.loadedChunksLock.ReleaseWriteLock();
			}
		}
		else
		{
			chunk = ClientChunk.CreateNew(((ClientWorldMap)worldmap).chunkDataPool);
		}
		chunks[cindex] = chunk;
		return chunk;
	}

	public virtual void MarkChunkDirty(int x, int y, int z)
	{
		dirtychunks.Add(ChunkIndex(x, y, z));
		Dirty = true;
	}

	public virtual void CollectChunksForSending(IPlayer[] players)
	{
		foreach (long index in dirtychunks)
		{
			if (chunks.TryGetValue(index, out var chunk))
			{
				((ServerChunk)chunk).MarkToPack();
				foreach (IPlayer player in players)
				{
					MarkChunkForSendingToPlayersInRange(chunk, index, player);
				}
			}
		}
		dirtychunks.Clear();
	}

	public virtual void MarkChunkForSendingToPlayersInRange(IWorldChunk chunk, long index, IPlayer player)
	{
		ServerPlayer plr = player as ServerPlayer;
		if (plr?.Entity != null && plr?.client != null)
		{
			ConnectedClient client = plr.client;
			float viewDist = client.WorldData.Viewdistance + 16;
			if (client.Entityplayer.ServerPos.InHorizontalRangeOf((int)CurrentPos.X, (int)CurrentPos.Z, viewDist * viewDist) || subDimensionId == BlocksPreviewSubDimension_Server)
			{
				client.forceSendChunks.Add(index);
			}
		}
	}

	protected virtual int Index3d(int posX, int posY, int posZ)
	{
		return worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F);
	}

	protected virtual bool SetBlock(int blockId, BlockPos pos, int layer, ItemStack byItemstack)
	{
		pos.SetDimension(1);
		IWorldChunk chunk = GetChunkAt(pos.X, pos.Y, pos.Z);
		if (chunk == null)
		{
			if (blockId == 0)
			{
				return false;
			}
			chunk = CreateChunkAt(pos.X, pos.Y, pos.Z);
		}
		else
		{
			chunk.Unpack();
			if (chunk.Empty)
			{
				chunk.Lighting.FloodWithSunlight(18);
			}
		}
		Block newBlock = worldmap.Blocks[blockId];
		if (layer == 2 || (layer == 0 && newBlock.ForFluidsLayer))
		{
			if (layer == 0)
			{
				SetSolidBlock(0, pos, chunk, byItemstack);
			}
			SetFluidBlock(blockId, pos, chunk);
			return true;
		}
		if (layer != 0 && layer != 1)
		{
			throw new ArgumentException("Layer must be solid or fluid");
		}
		return SetSolidBlock(blockId, pos, chunk, byItemstack);
	}

	protected virtual bool SetSolidBlock(int blockId, BlockPos pos, IWorldChunk chunk, ItemStack byItemstack)
	{
		int index3d = Index3d(pos.X, pos.Y, pos.Z);
		int oldblockid = (chunk.Data as ChunkData).GetSolidBlock(index3d);
		if (blockId == oldblockid)
		{
			return false;
		}
		Block newBlock = worldmap.Blocks[blockId];
		Block oldBlock = worldmap.Blocks[oldblockid];
		if (oldblockid > 0)
		{
			AddToCenterOfMass(oldBlock, pos, -1);
		}
		if (blockId > 0)
		{
			AddToCenterOfMass(newBlock, pos, 1);
		}
		chunk.Data[index3d] = blockId;
		if (blockId != 0)
		{
			chunk.Empty = false;
		}
		chunk.BreakAllDecorFast(worldAccessor, pos, index3d);
		oldBlock.OnBlockRemoved(worldmap.World, pos);
		newBlock.OnBlockPlaced(worldmap.World, pos, byItemstack);
		if (newBlock.DisplacesLiquids(this, pos))
		{
			chunk.Data.SetFluid(index3d, 0);
		}
		else
		{
			int liqId = chunk.Data.GetFluid(index3d);
			if (liqId != 0)
			{
				worldAccessor.GetBlock(liqId);
			}
		}
		return true;
	}

	protected virtual bool SetFluidBlock(int fluidBlockid, BlockPos pos, IWorldChunk chunk)
	{
		int index3d = Index3d(pos.X, pos.Y, pos.Z);
		int oldblockid = chunk.Data.GetFluid(index3d);
		if (fluidBlockid == oldblockid)
		{
			return false;
		}
		chunk.Data.SetFluid(index3d, fluidBlockid);
		if (fluidBlockid != 0)
		{
			chunk.Empty = false;
		}
		return true;
	}

	protected virtual void AddToCenterOfMass(Block block, BlockPos pos, int sign)
	{
		double mass = (double)Math.Max(block.MaterialDensity, 1) / 1000.0;
		double px = (double)pos.X + 0.5 - 8192.0;
		double py = (double)pos.Y + 0.5 - 8192.0;
		double pz = (double)pos.Z + 0.5 - 8192.0;
		if (CenterOfMass == null)
		{
			CenterOfMass = new Vec3d(px, py, pz);
		}
		else
		{
			CenterOfMass.X = (CenterOfMass.X * totalMass + px * mass * (double)sign) / (totalMass + mass * (double)sign);
			CenterOfMass.Y = (CenterOfMass.Y * totalMass + py * mass * (double)sign) / (totalMass + mass * (double)sign);
			CenterOfMass.Z = (CenterOfMass.Z * totalMass + pz * mass * (double)sign) / (totalMass + mass * (double)sign);
		}
		totalMass += mass * (double)sign;
	}

	public virtual FastVec3d GetRenderOffset(float dt)
	{
		FastVec3d result = new FastVec3d(-(subDimensionId % 4096) * 16384, 0.0, -(subDimensionId / 4096) * 16384).Add(-8192.0);
		if (TrackSelection)
		{
			BlockSelection selection = ((ClientMain)parent.worldAccessor).BlockSelection;
			if (selection != null && selection.Position != null)
			{
				return result.Add(selection.Position).Add(selection.Face.Normali);
			}
		}
		return result.Add(selectionTrackingOriginalPos.X, selectionTrackingOriginalPos.InternalY, selectionTrackingOriginalPos.Z);
	}

	public virtual void SetRenderOffsetY(int offset)
	{
		selectionTrackingOriginalPos.Y = offset;
	}

	public virtual float[] GetRenderTransformMatrix(float[] currentModelViewMatrix, Vec3d playerPos)
	{
		if (CurrentPos.Yaw == 0f && CurrentPos.Pitch == 0f && CurrentPos.Roll == 0f)
		{
			return currentModelViewMatrix;
		}
		float[] result = new float[currentModelViewMatrix.Length];
		float dx = (float)(CurrentPos.X + CenterOfMass.X - playerPos.X);
		float dy = (float)(CurrentPos.Y + CenterOfMass.Y - playerPos.Y);
		float dz = (float)(CurrentPos.Z + CenterOfMass.Z - playerPos.Z);
		Mat4f.Translate(result, currentModelViewMatrix, dx, dy, dz);
		ApplyCurrentRotation(result);
		return Mat4f.Translate(result, result, 0f - dx, 0f - dy, 0f - dz);
	}

	public virtual void ApplyCurrentRotation(float[] result)
	{
		Mat4f.RotateY(result, result, CurrentPos.Yaw);
		Mat4f.RotateZ(result, result, CurrentPos.Pitch);
		Mat4f.RotateX(result, result, CurrentPos.Roll);
	}

	public override int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0)
		{
			return 0;
		}
		if (posY >= 32768)
		{
			posX %= 16384;
			posY %= 32768;
			posZ %= 16384;
		}
		return GetChunkAt(posX, posY, posZ)?.UnpackAndReadBlock(Index3d(posX, posY, posZ), layer) ?? 0;
	}

	public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
	{
		if ((posX | posY | posZ) < 0)
		{
			return null;
		}
		if (posY >= 32768)
		{
			posX %= 16384;
			posY %= 32768;
			posZ %= 16384;
		}
		IWorldChunk chunk = GetChunkAt(posX, posY, posZ);
		if (chunk == null)
		{
			return null;
		}
		return worldmap.Blocks[chunk.UnpackAndReadBlock(Index3d(posX, posY, posZ), layer)];
	}

	public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack)
	{
		if (SetBlock(blockId, pos, 0, byItemstack))
		{
			MarkChunkDirty(pos.X, pos.Y, pos.Z);
		}
	}

	public override void SetBlock(int blockId, BlockPos pos, int layer)
	{
		if (SetBlock(blockId, pos, 0, null))
		{
			MarkChunkDirty(pos.X, pos.Y, pos.Z);
		}
	}

	public override void ExchangeBlock(int blockId, BlockPos pos)
	{
	}

	public virtual void ReceiveClientChunk(long cindex, IWorldChunk chunk, IWorldAccessor world)
	{
		chunks[cindex] = chunk;
		RecalculateCenterOfMass(world);
	}

	public virtual void RecalculateCenterOfMass(IWorldAccessor world)
	{
		CenterOfMass = new Vec3d(0.0, 0.0, 0.0);
		totalMass = 0.0;
		BlockPos tmp = new BlockPos();
		foreach (KeyValuePair<long, IWorldChunk> entry in chunks)
		{
			ChunkPos chunkPos = worldmap.ChunkPosFromChunkIndex3D(entry.Key);
			int cx = chunkPos.X * 32 % 16384;
			int cy = chunkPos.Y * 32 % 16384;
			int cz = chunkPos.Z * 32 % 16384;
			IWorldChunk value = entry.Value;
			value.Unpack_ReadOnly();
			IChunkBlocks blocks = value.Data;
			for (int i = 0; i < 32768; i++)
			{
				int blockId = blocks.GetBlockId(i, 1);
				if (blockId > 0)
				{
					tmp.X = cx + i % 32;
					tmp.Y = cy + i / 1024;
					tmp.Z = cz + i / 32 % 32;
					AddToCenterOfMass(world.GetBlock(blockId), tmp, 1);
				}
			}
		}
	}

	internal static int CalcSubDimensionId(int cx, int cz)
	{
		return cx / 512 + cz / 512 * 4096;
	}

	internal static int CalcSubDimensionId(Vec3i vec)
	{
		return CalcSubDimensionId(vec.X / 32, vec.Z / 32);
	}

	internal static bool IsTransparent(Vec3i chunkOrigin)
	{
		return CalcSubDimensionId(chunkOrigin) == Dimensions.BlocksPreviewSubDimension_Client;
	}
}
