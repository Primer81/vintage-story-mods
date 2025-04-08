using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf;

public class SystemChunkVisibilityCalc : ClientSystem
{
	private bool doOcclCulling;

	private uint[] visitedBlock;

	private uint iteration;

	private readonly QueueOfInt bfsQueue = new QueueOfInt();

	private const int chunksize = 32;

	private Block[] blocksFast;

	private int[] Blocks;

	public override string Name => "chunkculler";

	public SystemChunkVisibilityCalc(ClientMain game)
		: base(game)
	{
		SystemChunkVisibilityCalc systemChunkVisibilityCalc = this;
		game.eventManager.OnUpdateLighting += OnUpdateLighting;
		game.eventManager.OnChunkLoaded += OnChunkLoaded;
		game.eventManager.AddGameTickListener(onEvery500ms, 500);
		doOcclCulling = ClientSettings.Occlusionculling;
		ClientSettings.Inst.AddWatcher("occlusionculling", delegate(bool nowon)
		{
			bool num = systemChunkVisibilityCalc.doOcclCulling;
			systemChunkVisibilityCalc.doOcclCulling = nowon;
			if (!num && nowon)
			{
				lock (game.chunkPositionsLock)
				{
					foreach (KeyValuePair<long, ClientChunk> current in game.WorldMap.chunks)
					{
						current.Value.traversabilityFresh = false;
						ChunkPos item = game.WorldMap.ChunkPosFromChunkIndex3D(current.Key);
						if (item.Dimension == 0)
						{
							game.chunkPositionsForRegenTrav.Add(item);
						}
					}
				}
			}
		});
	}

	private void onEvery500ms(float dt)
	{
		if (game.extendedDebugInfo)
		{
			game.DebugScreenInfo["traversethread"] = "traverseQ: " + game.chunkPositionsForRegenTrav.Count;
		}
	}

	public override void OnBlockTexturesLoaded()
	{
		base.OnBlockTexturesLoaded();
		visitedBlock = new uint[32768];
		blocksFast = (game.Blocks as BlockList).BlocksFast;
		Blocks = new int[32768];
	}

	private void OnChunkLoaded(Vec3i chunkpos)
	{
		if (!doOcclCulling)
		{
			return;
		}
		lock (game.chunkPositionsLock)
		{
			game.chunkPositionsForRegenTrav.Add(new ChunkPos(chunkpos));
		}
	}

	private void OnUpdateLighting(int oldBlockId, int newBlockId, BlockPos pos, Dictionary<BlockPos, BlockUpdate> blockUpdatesBulk)
	{
		if (!doOcclCulling)
		{
			return;
		}
		lock (game.chunkPositionsLock)
		{
			if (blockUpdatesBulk != null)
			{
				foreach (KeyValuePair<BlockPos, BlockUpdate> val in blockUpdatesBulk)
				{
					if (val.Value.NewSolidBlockId >= 0 && RequiresRecalc(val.Value.OldBlockId, val.Value.NewSolidBlockId))
					{
						ChunkPos chunkPos = ChunkPos.FromPosition(val.Key.X, val.Key.Y, val.Key.Z, 0);
						if (!game.chunkPositionsForRegenTrav.Contains(chunkPos))
						{
							game.chunkPositionsForRegenTrav.Add(chunkPos);
							ClientChunk chunk = game.WorldMap.GetClientChunk(chunkPos.X, chunkPos.Y, chunkPos.Z);
							if (chunk != null)
							{
								chunk.traversabilityFresh = false;
							}
						}
					}
				}
				return;
			}
			if (!RequiresRecalc(oldBlockId, newBlockId))
			{
				return;
			}
			ChunkPos chunkPos2 = ChunkPos.FromPosition(pos.X, pos.Y, pos.Z);
			if (!game.chunkPositionsForRegenTrav.Contains(chunkPos2))
			{
				game.chunkPositionsForRegenTrav.Add(chunkPos2);
				ClientChunk chunk2 = game.WorldMap.GetClientChunk(chunkPos2.X, chunkPos2.Y, chunkPos2.Z);
				if (chunk2 != null)
				{
					chunk2.traversabilityFresh = false;
				}
			}
		}
	}

	private bool RequiresRecalc(int oldblockid, int newblockid)
	{
		Block oldblock = game.Blocks[oldblockid];
		Block newblock = game.Blocks[newblockid];
		if (oldblock.SideOpaque[0] == newblock.SideOpaque[0] && oldblock.SideOpaque[1] == newblock.SideOpaque[1] && oldblock.SideOpaque[2] == newblock.SideOpaque[2] && oldblock.SideOpaque[3] == newblock.SideOpaque[3] && oldblock.SideOpaque[4] == newblock.SideOpaque[4])
		{
			return oldblock.SideOpaque[5] != newblock.SideOpaque[5];
		}
		return true;
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 10;
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		if (game.chunkPositionsForRegenTrav.Count != 0)
		{
			ChunkPos chunkpos;
			lock (game.chunkPositionsLock)
			{
				chunkpos = game.chunkPositionsForRegenTrav.PopOne();
			}
			RegenTraversabilityGraph(chunkpos);
		}
	}

	private void RegenTraversabilityGraph(ChunkPos chunkpos)
	{
		ClientChunk chunk = game.WorldMap.GetClientChunk(chunkpos.X, chunkpos.Y, chunkpos.Z);
		if (chunk == null || !chunk.ChunkHasData())
		{
			return;
		}
		if (chunk.Empty)
		{
			setFullyTraversable(chunk);
			return;
		}
		chunk.ClearTraversable();
		bfsQueue.Clear();
		uint iter = ++iteration;
		chunk.TemporaryUnpack(Blocks);
		Vec3i curpos = new Vec3i();
		int validBlocks = blocksFast.Length;
		for (int i = 0; i < Blocks.Length; i++)
		{
			if (visitedBlock[i] == iter)
			{
				continue;
			}
			int exitedFaces = 0;
			bfsQueue.Enqueue(i);
			while (bfsQueue.Count > 0)
			{
				int index = bfsQueue.Dequeue();
				int blockId = Blocks[index];
				if (blockId >= validBlocks)
				{
					continue;
				}
				Block block = blocksFast[blockId];
				if (AllSidesOpaque(block))
				{
					continue;
				}
				curpos.Set(index % 32, index / 32 / 32, index / 32 % 32);
				for (int f = 0; f < 6; f++)
				{
					if (block.SideOpaque[f])
					{
						continue;
					}
					Vec3i Normali = BlockFacing.ALLNORMALI[f];
					int nx = curpos.X + Normali.X;
					int ny = curpos.Y + Normali.Y;
					int nz = curpos.Z + Normali.Z;
					if (DidWeExitChunk(nx, ny, nz))
					{
						exitedFaces |= 1 << f;
						continue;
					}
					int nindex = (ny * 32 + nz) * 32 + nx;
					if (visitedBlock[nindex] != iter)
					{
						visitedBlock[nindex] = iter;
						bfsQueue.Enqueue(nindex);
					}
				}
			}
			connectFacesAndSetTraversable(exitedFaces, chunk);
		}
		chunk.traversabilityFresh = true;
	}

	private void connectFacesAndSetTraversable(int exitedFaces, ClientChunk chunk)
	{
		for (int i = 0; i < 6; i++)
		{
			if ((exitedFaces & (1 << i)) == 0)
			{
				continue;
			}
			for (int j = i + 1; j < 6; j++)
			{
				if ((exitedFaces & (1 << j)) != 0)
				{
					chunk.SetTraversable(i, j);
				}
			}
		}
	}

	private void setFullyTraversable(ClientChunk chunk)
	{
		for (int i = 0; i < 6; i++)
		{
			for (int j = i + 1; j < 6; j++)
			{
				chunk.SetTraversable(i, j);
			}
		}
	}

	public bool AllSidesOpaque(Block block)
	{
		if (block.SideOpaque[0] && block.SideOpaque[1] && block.SideOpaque[2] && block.SideOpaque[3] && block.SideOpaque[4])
		{
			return block.SideOpaque[5];
		}
		return false;
	}

	public bool DidWeExitChunk(int posX, int posY, int posZ)
	{
		if (posX >= 0 && posX < 32 && posY >= 0 && posY < 32 && posZ >= 0)
		{
			return posZ >= 32;
		}
		return true;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
