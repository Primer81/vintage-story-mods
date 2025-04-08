using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class ProPickWorkSpace
{
	public class DummyChunk : IServerChunk, IWorldChunk
	{
		public class DummyChunkData : IChunkBlocks
		{
			public int[] blocks;

			public int this[int index3d]
			{
				get
				{
					return blocks[index3d];
				}
				set
				{
					blocks[index3d] = value;
				}
			}

			public int Length => blocks.Length;

			public DummyChunkData(int chunksize)
			{
				blocks = new int[chunksize * chunksize * chunksize];
			}

			public void ClearBlocks()
			{
				for (int i = 0; i < blocks.Length; i++)
				{
					blocks[i] = 0;
				}
			}

			public void ClearBlocksAndPrepare()
			{
				ClearBlocks();
			}

			public int GetBlockId(int index3d, int layer)
			{
				return blocks[index3d];
			}

			public int GetBlockIdUnsafe(int index3d)
			{
				return this[index3d];
			}

			public int GetFluid(int index3d)
			{
				throw new NotImplementedException();
			}

			public void SetBlockAir(int index3d)
			{
				this[index3d] = 0;
			}

			public void SetBlockBulk(int chunkIndex, int v, int mantleBlockId, int mantleBlockId1)
			{
				throw new NotImplementedException();
			}

			public void SetBlockUnsafe(int index3d, int value)
			{
				this[index3d] = value;
			}

			public void SetFluid(int index3d, int value)
			{
			}

			public void TakeBulkReadLock()
			{
			}

			public void ReleaseBulkReadLock()
			{
			}

			public bool ContainsBlock(int id)
			{
				throw new NotImplementedException();
			}

			public void FuzzyListBlockIds(List<int> reusableList)
			{
				throw new NotImplementedException();
			}
		}

		public int chunkY;

		public IChunkBlocks Blocks;

		public IMapChunk MapChunk { get; set; }

		IChunkBlocks IWorldChunk.Data => Blocks;

		IChunkLight IWorldChunk.Lighting
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public Entity[] Entities
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public int EntitiesCount
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public BlockEntity[] BlockEntities
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public HashSet<int> LightPositions
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public string GameVersionCreated
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool Disposed
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool Empty
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		Dictionary<BlockPos, BlockEntity> IWorldChunk.BlockEntities
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public IChunkBlocks MaybeBlocks
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool NotAtEdge
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		IChunkBlocks IWorldChunk.Blocks
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public int BlocksPlaced
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public int BlocksRemoved
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public Dictionary<string, object> LiveModData
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public DummyChunk(int chunksize)
		{
			Blocks = new DummyChunkData(chunksize);
		}

		public void AddEntity(Entity entity)
		{
			throw new NotImplementedException();
		}

		public byte[] GetModdata(string key)
		{
			throw new NotImplementedException();
		}

		public byte[] GetServerModdata(string key)
		{
			throw new NotImplementedException();
		}

		public void MarkModified()
		{
			throw new NotImplementedException();
		}

		public bool RemoveEntity(long entityId)
		{
			throw new NotImplementedException();
		}

		public void RemoveModdata(string key)
		{
			throw new NotImplementedException();
		}

		public void SetModdata(string key, byte[] data)
		{
			throw new NotImplementedException();
		}

		public void SetServerModdata(string key, byte[] data)
		{
			throw new NotImplementedException();
		}

		public void Unpack()
		{
			throw new NotImplementedException();
		}

		public bool Unpack_ReadOnly()
		{
			throw new NotImplementedException();
		}

		public int UnpackAndReadBlock(int index, int layer)
		{
			throw new NotImplementedException();
		}

		public ushort Unpack_AndReadLight(int index)
		{
			throw new NotImplementedException();
		}

		public ushort Unpack_AndReadLight(int index, out int lightSat)
		{
			throw new NotImplementedException();
		}

		public Block GetLocalBlockAtBlockPos(IWorldAccessor world, BlockPos position)
		{
			throw new NotImplementedException();
		}

		public void MarkFresh()
		{
			throw new NotImplementedException();
		}

		public BlockEntity GetLocalBlockEntityAtBlockPos(BlockPos pos)
		{
			throw new NotImplementedException();
		}

		public bool AddDecor(IBlockAccessor blockAccessor, BlockPos pos, int faceIndex, Block block)
		{
			throw new NotImplementedException();
		}

		public void RemoveDecor(int index3d, IWorldAccessor world, BlockPos pos)
		{
			throw new NotImplementedException();
		}

		Block[] IWorldChunk.GetDecors(IBlockAccessor blockAccessor, BlockPos pos)
		{
			throw new NotImplementedException();
		}

		public Dictionary<int, Block> GetSubDecors(IBlockAccessor blockAccessor, BlockPos position)
		{
			throw new NotImplementedException();
		}

		public bool GetDecors(IBlockAccessor blockAccessor, BlockPos pos, Block[] result)
		{
			throw new NotImplementedException();
		}

		public Cuboidf[] AdjustSelectionBoxForDecor(IBlockAccessor blockAccessor, BlockPos pos, Cuboidf[] orig)
		{
			throw new NotImplementedException();
		}

		public void FinishLightDoubleBuffering()
		{
			throw new NotImplementedException();
		}

		public bool SetDecor(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing onFace)
		{
			throw new NotImplementedException();
		}

		public bool SetDecor(IBlockAccessor blockAccessor, Block block, BlockPos pos, int subPosition)
		{
			throw new NotImplementedException();
		}

		public void BreakDecor(IWorldAccessor world, BlockPos pos, BlockFacing side = null)
		{
			throw new NotImplementedException();
		}

		public void BreakAllDecorFast(IWorldAccessor world, BlockPos pos, int index3d, bool callOnBrokenAsDecor = true)
		{
			throw new NotImplementedException();
		}

		public Dictionary<int, Block> GetDecors(IBlockAccessor blockAccessor, BlockPos pos)
		{
			throw new NotImplementedException();
		}

		public void SetDecors(Dictionary<int, Block> newDecors)
		{
			throw new NotImplementedException();
		}

		public void BreakDecorPart(IWorldAccessor world, BlockPos pos, BlockFacing side, int faceAndSubposition)
		{
			throw new NotImplementedException();
		}

		public Block GetDecor(IBlockAccessor blockAccessor, BlockPos pos, int faceAndSubposition)
		{
			throw new NotImplementedException();
		}

		public void SetModdata<T>(string key, T data)
		{
			throw new NotImplementedException();
		}

		public T GetModdata<T>(string key)
		{
			throw new NotImplementedException();
		}

		public bool BreakDecor(IWorldAccessor world, BlockPos pos, BlockFacing side = null, int? faceAndSubposition = null)
		{
			throw new NotImplementedException();
		}

		public T GetModdata<T>(string key, T defaultValue = default(T))
		{
			throw new NotImplementedException();
		}

		public int GetLightAbsorptionAt(int index3d, BlockPos blockPos, IList<Block> blockTypes)
		{
			throw new NotImplementedException();
		}

		public Block GetLocalBlockAtBlockPos(IWorldAccessor world, int posX, int posY, int posZ, int layer)
		{
			throw new NotImplementedException();
		}

		public Block GetLocalBlockAtBlockPos_LockFree(IWorldAccessor world, BlockPos pos, int layer = 0)
		{
			throw new NotImplementedException();
		}

		public bool SetDecor(Block block, int index3d, BlockFacing onFace)
		{
			throw new NotImplementedException();
		}

		public bool SetDecor(Block block, int index3d, int faceAndSubposition)
		{
			throw new NotImplementedException();
		}

		public bool RemoveBlockEntity(BlockPos pos)
		{
			throw new NotImplementedException();
		}

		public void AcquireBlockReadLock()
		{
			throw new NotImplementedException();
		}

		public void ReleaseBlockReadLock()
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
		}
	}

	public Dictionary<string, string> pageCodes = new Dictionary<string, string>();

	public Dictionary<string, DepositVariant> depositsByCode = new Dictionary<string, DepositVariant>();

	private GenRockStrataNew rockStrataGen;

	private GenDeposits depositGen;

	private ICoreServerAPI sapi;

	public virtual void OnLoaded(ICoreAPI api)
	{
		if (api.Side == EnumAppSide.Client)
		{
			return;
		}
		ICoreServerAPI sapi = api as ICoreServerAPI;
		this.sapi = sapi;
		rockStrataGen = new GenRockStrataNew();
		rockStrataGen.setApi(sapi);
		TyronThreadPool.QueueTask(delegate
		{
			rockStrataGen.initWorldGen();
			GenDeposits genDeposits = new GenDeposits();
			genDeposits.addHandbookAttributes = false;
			genDeposits.setApi(sapi);
			genDeposits.initAssets(sapi, blockCallbacks: false);
			genDeposits.initWorldGen();
			depositGen = genDeposits;
		}, "propickonloaded");
		sapi.Event.ServerRunPhase(EnumServerRunPhase.RunGame, delegate
		{
			DepositVariant[] array = api.ModLoader.GetModSystem<GenDeposits>()?.Deposits;
			if (array != null)
			{
				foreach (DepositVariant depositVariant in array)
				{
					if (depositVariant.WithOreMap)
					{
						pageCodes[depositVariant.Code] = depositVariant.HandbookPageCode;
						depositsByCode[depositVariant.Code] = depositVariant;
						if (depositVariant.HandbookPageCode == null)
						{
							api.World.Logger.Warning("Deposit " + depositVariant.Code + " has no handbook page code. Links created by the prospecting pick will not work without it.");
						}
					}
					int num = 0;
					while (depositVariant.ChildDeposits != null && num < depositVariant.ChildDeposits.Length)
					{
						DepositVariant depositVariant2 = depositVariant.ChildDeposits[num];
						if (depositVariant2.WithOreMap)
						{
							if (depositVariant2.HandbookPageCode == null)
							{
								api.World.Logger.Warning("Child Deposit " + depositVariant2.Code + " of deposit " + depositVariant.Code + " has no handbook page code. Links created by the prospecting pick will not work without it.");
							}
							pageCodes[depositVariant2.Code] = depositVariant2.HandbookPageCode;
							depositsByCode[depositVariant2.Code] = depositVariant2;
						}
						num++;
					}
				}
			}
		});
	}

	public void Dispose(ICoreAPI api)
	{
		if (sapi != null)
		{
			pageCodes = null;
			depositsByCode = null;
			rockStrataGen?.Dispose();
			rockStrataGen = null;
			depositGen?.Dispose();
			depositGen = null;
			sapi = null;
		}
	}

	public virtual int[] GetRockColumn(int posX, int posZ)
	{
		DummyChunk[] chunks = new DummyChunk[sapi.World.BlockAccessor.MapSizeY / 32];
		int chunkX = posX / 32;
		int chunkZ = posZ / 32;
		int lx = posX % 32;
		int lz = posZ % 32;
		IMapChunk mapchunk = sapi.World.BlockAccessor.GetMapChunk(new Vec2i(chunkX, chunkZ));
		for (int chunkY3 = 0; chunkY3 < chunks.Length; chunkY3++)
		{
			chunks[chunkY3] = new DummyChunk(32);
			chunks[chunkY3].MapChunk = mapchunk;
			chunks[chunkY3].chunkY = chunkY3;
		}
		int surfaceY = mapchunk.WorldGenTerrainHeightMap[lz * 32 + lx];
		for (int y2 = 0; y2 < surfaceY; y2++)
		{
			int chunkY2 = y2 / 32;
			int lY2 = y2 - chunkY2 * 32;
			int localIndex3D2 = (32 * lY2 + lz) * 32 + lx;
			chunks[chunkY2].Blocks[localIndex3D2] = rockStrataGen.rockBlockId;
		}
		GenRockStrataNew genRockStrataNew = rockStrataGen;
		IServerChunk[] chunks2 = chunks;
		genRockStrataNew.preLoad(chunks2, chunkX, chunkZ);
		GenRockStrataNew genRockStrataNew2 = rockStrataGen;
		chunks2 = chunks;
		genRockStrataNew2.genBlockColumn(chunks2, chunkX, chunkZ, lx, lz);
		if (depositGen == null)
		{
			int timeOutCount = 100;
			while (depositGen == null && timeOutCount-- > 0)
			{
				Thread.Sleep(30);
			}
			if (depositGen == null)
			{
				throw new NullReferenceException("Prospecting Pick internal ore generator was not initialised, likely due to an exception during earlier off-thread worldgen");
			}
		}
		GenDeposits genDeposits = depositGen;
		chunks2 = chunks;
		genDeposits.GeneratePartial(chunks2, chunkX, chunkZ, 0, 0);
		int[] rockColumn = new int[surfaceY];
		for (int y = 0; y < surfaceY; y++)
		{
			int chunkY = y / 32;
			int lY = y - chunkY * 32;
			int localIndex3D = (32 * lY + lz) * 32 + lx;
			rockColumn[y] = chunks[chunkY].Blocks[localIndex3D];
		}
		return rockColumn;
	}
}
