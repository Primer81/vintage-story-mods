using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ChunkTesselator : IMeshPoolSupplier
{
	public const int LODPOOLS = 4;

	internal int[] TextureIdToReturnNum;

	private const int chunkSize = 32;

	internal ClientMain game;

	internal readonly Block[] currentChunkBlocksExt;

	internal readonly Block[] currentChunkFluidBlocksExt;

	internal readonly int[] currentChunkRgbsExt;

	internal byte[] currentChunkDraw32;

	internal byte[] currentChunkDrawFluids;

	internal int[] currentClimateRegionMap;

	internal bool started;

	internal int mapsizex;

	internal int mapsizey;

	internal int mapsizez;

	internal int mapsizeChunksx;

	internal int mapsizeChunksy;

	internal int mapsizeChunksz;

	private int quantityAtlasses;

	internal bool[] isPartiallyTransparent;

	internal bool[] isLiquidBlock;

	internal MeshData[][][] currentModeldataByRenderPassByLodLevel;

	internal MeshData[][][] centerModeldataByRenderPassByLodLevel;

	internal MeshData[][][] edgeModeldataByRenderPassByLodLevel;

	private int[][] fastBlockTextureSubidsByBlockAndFace;

	private TesselatedChunkPart[] ret;

	private TesselatedChunkPart[] emptyParts = new TesselatedChunkPart[0];

	internal static readonly float[] waterLevels = new float[9] { 0f, 0.125f, 0.25f, 0.375f, 0.5f, 0.625f, 0.75f, 0.875f, 1f };

	private int seaLevel;

	internal int regionSize;

	internal const int extChunkSize = 34;

	internal const int maxX = 31;

	internal bool AoAndSmoothShadows;

	internal Block[] blocksFast;

	internal readonly TCTCache vars;

	private ColorUtil.LightUtil lightConverter;

	private readonly IBlockTesselator[] blockTesselators = new IBlockTesselator[40];

	public JsonTesselator jsonTesselator;

	internal ITesselatorAPI offthreadTesselator;

	internal readonly ClientChunk[] chunksNearby;

	internal readonly ClientChunkData[] chunkdatasNearby;

	public object ReloadLock = new object();

	private ColorMapData defaultColorMapData;

	private float[][] decorRotationMatrices = new float[24][];

	private bool lightsGo;

	private bool blockTexturesGo;

	private EnumChunkRenderPass[] passes = (EnumChunkRenderPass[])Enum.GetValues(typeof(EnumChunkRenderPass));

	private BlockPos tmpPos = new BlockPos();

	public ChunkTesselator(ClientMain game)
	{
		this.game = game;
		vars = new TCTCache(this);
		int extendedCube = 39304;
		currentChunkRgbsExt = new int[extendedCube];
		currentChunkBlocksExt = new Block[extendedCube];
		currentChunkFluidBlocksExt = new Block[extendedCube];
		chunksNearby = new ClientChunk[27];
		chunkdatasNearby = new ClientChunkData[27];
		blockTesselators[1] = new CubeTesselator(0.125f);
		blockTesselators[2] = new CubeTesselator(0.25f);
		blockTesselators[3] = new CubeTesselator(0.375f);
		blockTesselators[4] = new CubeTesselator(0.5f);
		blockTesselators[5] = new CubeTesselator(0.625f);
		blockTesselators[6] = new CubeTesselator(0.75f);
		blockTesselators[7] = new CubeTesselator(0.875f);
		blockTesselators[8] = (jsonTesselator = new JsonTesselator());
		blockTesselators[10] = new CubeTesselator(1f);
		blockTesselators[11] = new CrossTesselator();
		blockTesselators[12] = new CubeTesselator(1f);
		blockTesselators[13] = new LiquidTesselator(this);
		blockTesselators[14] = new TopsoilTesselator();
		blockTesselators[15] = new CrossAndSnowlayerTesselator(0.125f);
		blockTesselators[18] = new CrossAndSnowlayerTesselator(0.25f);
		blockTesselators[19] = new CrossAndSnowlayerTesselator(0.375f);
		blockTesselators[20] = new CrossAndSnowlayerTesselator(0.5f);
		blockTesselators[21] = new SurfaceLayerTesselator();
		blockTesselators[16] = new JsonAndLiquidTesselator(this);
		blockTesselators[17] = new JsonAndSnowLayerTesselator();
		ClientSettings.Inst.AddWatcher<bool>("smoothShadows", OnSmoothShadowsChanged);
		AoAndSmoothShadows = ClientSettings.SmoothShadows;
		SetUpDecorRotationMatrices();
	}

	private void OnSmoothShadowsChanged(bool newValue)
	{
		AoAndSmoothShadows = ClientSettings.SmoothShadows;
	}

	public void LightlevelsReceived()
	{
		lightsGo = true;
		Start();
	}

	public void BlockTexturesLoaded()
	{
		blockTexturesGo = true;
		Start();
	}

	public void Start()
	{
		if (!lightsGo || !blockTexturesGo)
		{
			return;
		}
		lightConverter = new ColorUtil.LightUtil(game.WorldMap.BlockLightLevels, game.WorldMap.SunLightLevels, game.WorldMap.hueLevels, game.WorldMap.satLevels);
		regionSize = game.WorldMap.RegionSize;
		seaLevel = ClientWorldMap.seaLevel;
		vars.Start(game);
		blocksFast = (game.Blocks as BlockList).BlocksFast;
		for (int j = 0; j < blocksFast.Length; j++)
		{
			if (blocksFast[j] == null)
			{
				game.Logger.Debug("BlockList null at position " + j);
				blocksFast[j] = blocksFast[0];
			}
		}
		offthreadTesselator = game.TesselatorManager.GetNewTesselator();
		TileSideEnum.MoveIndex[0] = -34;
		TileSideEnum.MoveIndex[1] = 1;
		TileSideEnum.MoveIndex[2] = 34;
		TileSideEnum.MoveIndex[3] = -1;
		TileSideEnum.MoveIndex[4] = 1156;
		TileSideEnum.MoveIndex[5] = -1156;
		currentChunkDraw32 = new byte[32768];
		currentChunkDrawFluids = new byte[32768];
		mapsizex = game.WorldMap.MapSizeX;
		mapsizey = game.WorldMap.MapSizeY;
		mapsizez = game.WorldMap.MapSizeZ;
		mapsizeChunksx = mapsizex / 32;
		mapsizeChunksy = mapsizey / 32;
		mapsizeChunksz = mapsizez / 32;
		Array passes = Enum.GetValues(typeof(EnumChunkRenderPass));
		centerModeldataByRenderPassByLodLevel = new MeshData[4][][];
		edgeModeldataByRenderPassByLodLevel = new MeshData[4][][];
		for (int i = 0; i < 4; i++)
		{
			centerModeldataByRenderPassByLodLevel[i] = new MeshData[passes.Length][];
			edgeModeldataByRenderPassByLodLevel[i] = new MeshData[passes.Length][];
		}
		ReloadTextures();
		int maxBlockId = game.Blocks.Count;
		isPartiallyTransparent = new bool[maxBlockId];
		isLiquidBlock = new bool[maxBlockId];
		for (int blockId = 0; blockId < maxBlockId; blockId++)
		{
			Block block = game.Blocks[blockId];
			isPartiallyTransparent[blockId] = !block.AllSidesOpaque;
			isLiquidBlock[blockId] = block.MatterState == EnumMatterState.Liquid;
		}
		ClientEventManager em = game.eventManager;
		if (em != null)
		{
			em.OnReloadTextures += ReloadTextures;
		}
		started = true;
	}

	public void ReloadTextures()
	{
		List<LoadedTexture> atlasTextures = game.BlockAtlasManager.AtlasTextures;
		quantityAtlasses = atlasTextures.Count;
		int[] textureIDs = new int[quantityAtlasses];
		for (int i = 0; i < quantityAtlasses; i++)
		{
			textureIDs[i] = atlasTextures[i].TextureId;
		}
		lock (ReloadLock)
		{
			TextureIdToReturnNum = textureIDs;
			fastBlockTextureSubidsByBlockAndFace = game.FastBlockTextureSubidsByBlockAndFace;
			Array passes = Enum.GetValues(typeof(EnumChunkRenderPass));
			ret = new TesselatedChunkPart[passes.Length * quantityAtlasses];
			foreach (EnumChunkRenderPass pass in passes)
			{
				for (int lod = 0; lod < 4; lod++)
				{
					MeshData[][] chunkModeldataByRenderPass = centerModeldataByRenderPassByLodLevel[lod];
					MeshData[][] edgeModeldataByRenderPass = edgeModeldataByRenderPassByLodLevel[lod];
					chunkModeldataByRenderPass[(int)pass] = new MeshData[quantityAtlasses];
					edgeModeldataByRenderPass[(int)pass] = new MeshData[quantityAtlasses];
					InitialiseRenderPassPools(chunkModeldataByRenderPass[(int)pass], pass, 1024);
					InitialiseRenderPassPools(edgeModeldataByRenderPass[(int)pass], pass, 1024);
				}
			}
		}
	}

	private void InitialiseRenderPassPools(MeshData[] renderPassModeldata, EnumChunkRenderPass pass, int startCapacity)
	{
		for (int i = 0; i < quantityAtlasses; i++)
		{
			renderPassModeldata[i] = new MeshData();
			renderPassModeldata[i].xyz = new float[startCapacity * 3];
			renderPassModeldata[i].Uv = new float[startCapacity * 2];
			renderPassModeldata[i].Rgba = new byte[startCapacity * 4];
			renderPassModeldata[i].Flags = new int[startCapacity];
			renderPassModeldata[i].Indices = new int[startCapacity];
			renderPassModeldata[i].VerticesMax = startCapacity;
			renderPassModeldata[i].IndicesMax = startCapacity;
			if (pass == EnumChunkRenderPass.Liquid)
			{
				renderPassModeldata[i].CustomFloats = new CustomMeshDataPartFloat(startCapacity * 2);
				renderPassModeldata[i].CustomInts = new CustomMeshDataPartInt(startCapacity * 2);
			}
			else
			{
				renderPassModeldata[i].CustomInts = new CustomMeshDataPartInt(startCapacity);
			}
			if (pass == EnumChunkRenderPass.TopSoil)
			{
				renderPassModeldata[i].CustomFloats = new CustomMeshDataPartFloat(startCapacity * 2);
			}
		}
	}

	public bool BeginProcessChunk(int chunkX, int chunkY, int chunkZ, ClientChunk chunk, bool skipChunkCenter)
	{
		if (!started)
		{
			throw new Exception("not started");
		}
		vars.aoAndSmoothShadows = AoAndSmoothShadows;
		vars.xMin = 32f;
		vars.xMax = 0f;
		vars.yMin = 32f;
		vars.yMax = 0f;
		vars.zMin = 32f;
		vars.zMax = 0f;
		vars.SetDimension(chunkY / 1024);
		try
		{
			BuildExtendedChunkData(chunk, chunkX, chunkY, chunkZ, chunkX < 1 || chunkZ < 1 || chunkX >= game.WorldMap.ChunkMapSizeX - 1 || chunkZ >= game.WorldMap.ChunkMapSizeZ - 1, skipChunkCenter);
		}
		catch (ThreadAbortException)
		{
			throw;
		}
		catch (Exception e)
		{
			if (game.Platform.IsShuttingDown)
			{
				return false;
			}
			throw new Exception($"Exception thrown when trying to tesselate chunk {chunkX}/{chunkY}/{chunkZ}. Exception: {e}");
		}
		currentClimateRegionMap = game.WorldMap.LoadOrCreateLerpedClimateMap(chunkX, chunkZ);
		return CalculateVisibleFaces_Fluids(skipChunkCenter, chunkX * 32, chunkY * 32, chunkZ * 32) | CalculateVisibleFaces(skipChunkCenter, chunkX * 32, chunkY * 32, chunkZ * 32);
	}

	public int NowProcessChunk(int chunkX, int chunkY, int chunkZ, TesselatedChunk tessChunk, bool skipChunkCenter)
	{
		if (chunkX < 0 || chunkY < 0 || chunkZ < 0 || (chunkY < 1024 && (chunkX >= mapsizeChunksx || chunkZ >= mapsizeChunksz)))
		{
			return 0;
		}
		if (!BeginProcessChunk(chunkX, chunkY, chunkZ, tessChunk.chunk, skipChunkCenter))
		{
			if (!skipChunkCenter)
			{
				tessChunk.centerParts = emptyParts;
			}
			tessChunk.edgeParts = emptyParts;
			return 0;
		}
		tmpPos.dimension = chunkY / 1024;
		Dictionary<int, Block> decors = null;
		if (tessChunk.chunk.Decors != null)
		{
			decors = new Dictionary<int, Block>();
			lock (tessChunk.chunk.Decors)
			{
				CullVisibleFacesWithDecor(tessChunk.chunk.Decors, decors);
			}
		}
		lock (ReloadLock)
		{
			vars.textureAtlasPositionsByTextureSubId = game.BlockAtlasManager.TextureAtlasPositionsByTextureSubId;
			EnumChunkRenderPass[] array = passes;
			foreach (EnumChunkRenderPass renderPass in array)
			{
				for (int i = 0; i < quantityAtlasses; i++)
				{
					for (int j = 0; j < 4; j++)
					{
						edgeModeldataByRenderPassByLodLevel[j][(int)renderPass][i].Clear();
						if (!skipChunkCenter)
						{
							centerModeldataByRenderPassByLodLevel[j][(int)renderPass][i].Clear();
						}
					}
				}
			}
			try
			{
				if (skipChunkCenter)
				{
					BuildBlockPolygons_EdgeOnly(chunkX, chunkY, chunkZ);
				}
				else
				{
					BuildBlockPolygons(chunkX, chunkY, chunkZ);
				}
			}
			catch (Exception ex)
			{
				game.Logger.Error(ex);
			}
			if (decors != null)
			{
				vars.blockEntitiesOfChunk = null;
				BuildDecorPolygons(chunkX, chunkY, chunkZ, decors, skipChunkCenter);
			}
			int verticesCount = 0;
			if (!skipChunkCenter)
			{
				verticesCount += populateTesselatedChunkPart(centerModeldataByRenderPassByLodLevel, out tessChunk.centerParts);
			}
			verticesCount += populateTesselatedChunkPart(edgeModeldataByRenderPassByLodLevel, out tessChunk.edgeParts);
			tessChunk.SetBounds(vars.xMin, vars.xMax, vars.yMin, vars.yMax, vars.zMin, vars.zMax);
			return verticesCount;
		}
	}

	private int populateTesselatedChunkPart(MeshData[][][] modeldataByRenderPassByLodLevel, out TesselatedChunkPart[] tessChunkParts)
	{
		int retCount = 0;
		int verticesCount = 0;
		MeshData.Recycler.DoRecycling();
		EnumChunkRenderPass[] array = passes;
		foreach (EnumChunkRenderPass renderpass in array)
		{
			for (int j = 0; j < quantityAtlasses; j++)
			{
				MeshData chunkModeldataLod0 = modeldataByRenderPassByLodLevel[0][(int)renderpass][j];
				MeshData chunkModeldataLod1 = modeldataByRenderPassByLodLevel[1][(int)renderpass][j];
				MeshData chunkModeldataLod2Near = modeldataByRenderPassByLodLevel[2][(int)renderpass][j];
				MeshData chunkModeldataLod2Far = modeldataByRenderPassByLodLevel[3][(int)renderpass][j];
				int count0 = chunkModeldataLod0.VerticesCount;
				int count1 = chunkModeldataLod1.VerticesCount;
				int count2 = chunkModeldataLod2Near.VerticesCount;
				int count3 = chunkModeldataLod2Far.VerticesCount;
				if (count0 + count1 + count2 + count3 > 0)
				{
					ret[retCount++] = new TesselatedChunkPart
					{
						atlasNumber = j,
						modelDataLod0 = ((count0 == 0) ? null : chunkModeldataLod0.CloneUsingRecycler()),
						modelDataLod1 = ((count1 == 0) ? null : chunkModeldataLod1.CloneUsingRecycler()),
						modelDataNotLod2Far = ((count2 == 0) ? null : chunkModeldataLod2Near.CloneUsingRecycler()),
						modelDataLod2Far = ((count3 == 0) ? null : chunkModeldataLod2Far.CloneUsingRecycler()),
						pass = renderpass
					};
					verticesCount += count0 + count1;
				}
			}
		}
		if (retCount > 0)
		{
			Array.Copy(ret, tessChunkParts = new TesselatedChunkPart[retCount], retCount);
			for (int i = 0; i < retCount; i++)
			{
				ret[i] = null;
			}
		}
		else
		{
			tessChunkParts = emptyParts;
		}
		return verticesCount;
	}

	public bool CalculateVisibleFaces(bool skipChunkCenter, int baseX, int baseY, int baseZ)
	{
		byte[] currentChunkDraw32 = this.currentChunkDraw32;
		int extIndex3d = 0;
		Block blockAir = blocksFast[0];
		for (int y = 0; y < 32; y++)
		{
			int index3d = y * 32 * 32;
			for (int z = 0; z < 32; z++)
			{
				int extIndex3dBase = (y * 34 + z) * 34 + 1191;
				int zeroIfYZEdge = y * (y ^ 0x1F) * z * (z ^ 0x1F);
				for (int x = 0; x < 32; x++)
				{
					Block curBlock;
					if ((curBlock = currentChunkBlocksExt[extIndex3dBase + x]) == blockAir)
					{
						currentChunkDraw32[index3d + x] = 0;
					}
					else
					{
						if (skipChunkCenter && x * (x ^ 0x1F) * zeroIfYZEdge != 0)
						{
							continue;
						}
						extIndex3d = extIndex3dBase + x;
						int faceVisibilityFlags = 0;
						EnumFaceCullMode cullMode = curBlock.FaceCullMode;
						bool[] curBlock_SideOpaque = curBlock.SideOpaque;
						int tileSide = 5;
						do
						{
							faceVisibilityFlags <<= 1;
							Block nBlock = currentChunkBlocksExt[extIndex3d + TileSideEnum.MoveIndex[tileSide]];
							int tileSideOpposite = TileSideEnum.GetOpposite(tileSide);
							bool neighbourOpaque = nBlock.SideOpaque[tileSideOpposite];
							if (tileSide == 4 && nBlock.DrawType == EnumDrawType.JSONAndSnowLayer && neighbourOpaque && !curBlock.AllowSnowCoverage(game, tmpPos.Set(baseX + x, baseY + y, baseZ + z)))
							{
								neighbourOpaque = false;
							}
							switch (cullMode)
							{
							case EnumFaceCullMode.Default:
								if (!neighbourOpaque || (!curBlock_SideOpaque[tileSide] && curBlock.DrawType != EnumDrawType.JSON && curBlock.DrawType != EnumDrawType.JSONAndSnowLayer))
								{
									faceVisibilityFlags++;
								}
								break;
							case EnumFaceCullMode.NeverCull:
								faceVisibilityFlags++;
								break;
							case EnumFaceCullMode.Merge:
								if (nBlock != curBlock && (!curBlock_SideOpaque[tileSide] || !neighbourOpaque))
								{
									faceVisibilityFlags++;
								}
								break;
							case EnumFaceCullMode.Collapse:
								if ((nBlock == curBlock && (tileSide == 4 || tileSide == 0 || tileSide == 3)) || (nBlock != curBlock && (!curBlock_SideOpaque[tileSide] || !neighbourOpaque)))
								{
									faceVisibilityFlags++;
								}
								break;
							case EnumFaceCullMode.MergeMaterial:
								if (!curBlock.SideSolid[tileSide] || (nBlock.BlockMaterial != curBlock.BlockMaterial && (!curBlock_SideOpaque[tileSide] || !neighbourOpaque)) || !nBlock.SideSolid[tileSideOpposite])
								{
									faceVisibilityFlags++;
								}
								break;
							case EnumFaceCullMode.CollapseMaterial:
								if (nBlock.BlockMaterial == curBlock.BlockMaterial)
								{
									if (tileSide == 0 || tileSide == 3)
									{
										faceVisibilityFlags++;
									}
								}
								else if (!neighbourOpaque || (tileSide < 4 && !curBlock_SideOpaque[tileSide]))
								{
									faceVisibilityFlags++;
								}
								break;
							case EnumFaceCullMode.Liquid:
							{
								if (nBlock.BlockMaterial == curBlock.BlockMaterial)
								{
									break;
								}
								if (tileSide == 4)
								{
									faceVisibilityFlags++;
									break;
								}
								FastVec3i offset = TileSideEnum.OffsetByTileSide[tileSide];
								if (!nBlock.SideIsSolid(tmpPos.Set(baseX + x + offset.X, baseY + y + offset.Y, baseZ + z + offset.Z), tileSideOpposite))
								{
									faceVisibilityFlags++;
								}
								break;
							}
							case EnumFaceCullMode.Callback:
								if (!curBlock.ShouldMergeFace(tileSide, nBlock, index3d + x))
								{
									faceVisibilityFlags++;
								}
								break;
							case EnumFaceCullMode.MergeSnowLayer:
							{
								int indexBelowNeighbour = extIndex3d + TileSideEnum.MoveIndex[tileSide] - 1156;
								if (tileSide == 4 || (!neighbourOpaque && (tileSide == 5 || nBlock.GetSnowLevel(null) < curBlock.GetSnowLevel(null))) || (nBlock.DrawType == EnumDrawType.JSONAndSnowLayer && indexBelowNeighbour >= 0 && indexBelowNeighbour < currentChunkBlocksExt.Length && !currentChunkBlocksExt[indexBelowNeighbour].AllowSnowCoverage(game, tmpPos.Set(baseX + x, baseY + y, baseZ + z))))
								{
									faceVisibilityFlags++;
								}
								break;
							}
							case EnumFaceCullMode.FlushExceptTop:
								if (tileSide == 4 || ((tileSide == 5 || nBlock != curBlock) && !neighbourOpaque))
								{
									faceVisibilityFlags++;
								}
								break;
							case EnumFaceCullMode.Stairs:
								if ((!neighbourOpaque && (nBlock != curBlock || curBlock.SideOpaque[tileSide])) || tileSide == 4)
								{
									faceVisibilityFlags++;
								}
								break;
							}
						}
						while (tileSide-- != 0);
						if (curBlock.DrawType == EnumDrawType.JSONAndWater)
						{
							faceVisibilityFlags |= 0x40;
						}
						currentChunkDraw32[index3d + x] = (byte)faceVisibilityFlags;
					}
				}
				index3d += 32;
			}
		}
		return extIndex3d > 0;
	}

	public bool CalculateVisibleFaces_Fluids(bool skipChunkCenter, int baseX, int baseY, int baseZ)
	{
		byte[] currentChunkDraw32 = currentChunkDrawFluids;
		int extIndex3d = 0;
		Block blockAir = blocksFast[0];
		for (int y = 0; y < 32; y++)
		{
			int index3d = y * 32 * 32;
			for (int z = 0; z < 32; z++)
			{
				int extIndex3dBase = (y * 34 + z) * 34 + 1191;
				int zeroIfYZEdge = y * (y ^ 0x1F) * z * (z ^ 0x1F);
				for (int x = 0; x < 32; x++)
				{
					Block curBlock;
					if ((curBlock = currentChunkFluidBlocksExt[extIndex3dBase + x]) == blockAir)
					{
						currentChunkDraw32[index3d + x] = 0;
					}
					else
					{
						if (skipChunkCenter && x * (x ^ 0x1F) * zeroIfYZEdge != 0)
						{
							continue;
						}
						extIndex3d = extIndex3dBase + x;
						int faceVisibilityFlags = 0;
						EnumFaceCullMode faceCullMode = curBlock.FaceCullMode;
						int tileSide = 5;
						if (faceCullMode == EnumFaceCullMode.Liquid)
						{
							do
							{
								faceVisibilityFlags <<= 1;
								if (currentChunkFluidBlocksExt[extIndex3d + TileSideEnum.MoveIndex[tileSide]].BlockMaterial == curBlock.BlockMaterial)
								{
									continue;
								}
								if (tileSide == 4)
								{
									faceVisibilityFlags++;
									continue;
								}
								Block obj = currentChunkBlocksExt[extIndex3d + TileSideEnum.MoveIndex[tileSide]];
								FastVec3i offset = TileSideEnum.OffsetByTileSide[tileSide];
								if (!obj.SideIsSolid(tmpPos.Set(baseX + x + offset.X, baseY + y + offset.Y, baseZ + z + offset.Z), TileSideEnum.GetOpposite(tileSide)))
								{
									faceVisibilityFlags++;
								}
							}
							while (tileSide-- != 0);
						}
						else
						{
							do
							{
								faceVisibilityFlags <<= 1;
								Block nLiquid = currentChunkFluidBlocksExt[extIndex3d + TileSideEnum.MoveIndex[tileSide]];
								if (!curBlock.ShouldMergeFace(tileSide, nLiquid, index3d + x))
								{
									faceVisibilityFlags++;
								}
							}
							while (tileSide-- != 0);
						}
						currentChunkDraw32[index3d + x] = (byte)faceVisibilityFlags;
					}
				}
				index3d += 32;
			}
		}
		return extIndex3d > 0;
	}

	public void BuildBlockPolygons(int chunkX, int chunkY, int chunkZ)
	{
		int baseX = chunkX * 32;
		int baseY = chunkY * 32 % 32768;
		int baseZ = chunkZ * 32;
		if (baseY == 0 && chunkY / 1024 != 1)
		{
			int layerSize = 1024;
			for (int i = 0; i < layerSize; i++)
			{
				currentChunkDraw32[i] &= 223;
				currentChunkDrawFluids[i] &= 223;
			}
		}
		TCTCache vars = this.vars;
		currentModeldataByRenderPassByLodLevel = edgeModeldataByRenderPassByLodLevel;
		int index3d = -1;
		for (int lY = 0; lY < 32; lY++)
		{
			int extLzBase = (lY + 1) * 34 + 1;
			vars.posY = baseY + lY;
			vars.finalY = lY;
			vars.ly = lY;
			int zeroIfYEdge = lY * (lY ^ 0x1F);
			int lZ = 0;
			do
			{
				vars.lz = lZ;
				int posZ = (vars.posZ = baseZ + lZ);
				MeshData[][][] centerXModeldataByRenderPassByLodLevel = ((zeroIfYEdge * lZ * (lZ ^ 0x1F) != 0) ? centerModeldataByRenderPassByLodLevel : edgeModeldataByRenderPassByLodLevel);
				int extIndex3dBase = (extLzBase + lZ) * 34 + 1;
				TesselateBlock(++index3d, extIndex3dBase, 0, baseX, posZ);
				currentModeldataByRenderPassByLodLevel = centerXModeldataByRenderPassByLodLevel;
				int lX = 1;
				do
				{
					TesselateBlock(++index3d, extIndex3dBase, lX, baseX, posZ);
				}
				while (++lX < 31);
				currentModeldataByRenderPassByLodLevel = edgeModeldataByRenderPassByLodLevel;
				TesselateBlock(++index3d, extIndex3dBase, lX, baseX, posZ);
			}
			while (++lZ < 32);
		}
	}

	public void BuildBlockPolygons_EdgeOnly(int chunkX, int chunkY, int chunkZ)
	{
		int baseX = chunkX * 32;
		int baseY = chunkY * 32 % 32768;
		int baseZ = chunkZ * 32;
		if (baseY == 0)
		{
			int layerSize = 1024;
			for (int i = 0; i < layerSize; i++)
			{
				currentChunkDraw32[i] &= 223;
			}
		}
		currentModeldataByRenderPassByLodLevel = edgeModeldataByRenderPassByLodLevel;
		TCTCache vars = this.vars;
		int index3d = -1;
		for (int lY = 0; lY < 32; lY++)
		{
			int extLzBase = (lY + 1) * 34 + 1;
			vars.posY = baseY + lY;
			vars.finalY = lY;
			vars.ly = lY;
			int zeroIfYEdge = lY * (lY ^ 0x1F);
			int lZ = 0;
			do
			{
				vars.lz = lZ;
				int posZ = (vars.posZ = baseZ + lZ);
				int extIndex3dBase = (extLzBase + lZ) * 34 + 1;
				if (zeroIfYEdge * lZ * (lZ ^ 0x1F) == 0)
				{
					int lX = 0;
					do
					{
						TesselateBlock(++index3d, extIndex3dBase, lX, baseX, posZ);
					}
					while (++lX < 32);
				}
				else
				{
					TesselateBlock(++index3d, extIndex3dBase, 0, baseX, posZ);
					index3d += 31;
					TesselateBlock(index3d, extIndex3dBase, 31, baseX, posZ);
				}
			}
			while (++lZ < 32);
		}
	}

	private void CullVisibleFacesWithDecor(Dictionary<int, Block> decors, Dictionary<int, Block> drawnDecors)
	{
		foreach (KeyValuePair<int, Block> val in decors)
		{
			Block block = val.Value;
			if (block == null)
			{
				continue;
			}
			int decorFlags = (block.IsMissing ? 1 : block.decorBehaviorFlags);
			if (((uint)decorFlags & (true ? 1u : 0u)) != 0)
			{
				int indexAndFace = val.Key;
				int index3d = DecorBits.Index3dFromIndex(indexAndFace);
				BlockFacing face = DecorBits.FacingFromIndex(indexAndFace);
				if ((currentChunkDraw32[index3d] & face.Flag) != 0 || ((uint)decorFlags & 2u) != 0)
				{
					drawnDecors[indexAndFace] = block;
				}
			}
		}
	}

	private void BuildDecorPolygons(int chunkX, int chunkY, int chunkZ, Dictionary<int, Block> decors, bool edgeonly)
	{
		int chunkSizeMask = 31;
		int baseX = chunkX * 32;
		int baseY = chunkY * 32 % 32768;
		int baseZ = chunkZ * 32;
		TCTCache vars = this.vars;
		foreach (KeyValuePair<int, Block> val in decors)
		{
			int packedIndex = val.Key;
			Block block = val.Value;
			BlockFacing face = DecorBits.FacingFromIndex(packedIndex);
			int index3d = DecorBits.Index3dFromIndex(packedIndex);
			int lX = index3d % 32;
			int lY = index3d / 32 / 32;
			int lZ = index3d / 32 % 32;
			if (lX * (lX ^ chunkSizeMask) * lY * (lY ^ chunkSizeMask) * lZ * (lZ ^ chunkSizeMask) == 0)
			{
				currentModeldataByRenderPassByLodLevel = edgeModeldataByRenderPassByLodLevel;
			}
			else
			{
				if (edgeonly)
				{
					continue;
				}
				currentModeldataByRenderPassByLodLevel = centerModeldataByRenderPassByLodLevel;
			}
			vars.extIndex3d = ((lY + 1) * 34 + lZ + 1) * 34 + lX + 1;
			vars.index3d = index3d;
			Vec3i delta = face.Normali;
			lY += delta.Y;
			lZ += delta.Z;
			lX += delta.X;
			vars.posX = baseX + lX;
			vars.posY = baseY + lY;
			vars.posZ = baseZ + lZ;
			vars.finalY = lY;
			if (block is IDrawYAdjustable idya)
			{
				vars.finalY += idya.AdjustYPosition(new BlockPos(vars.posX, vars.posY, vars.posZ), currentChunkBlocksExt, vars.extIndex3d);
			}
			vars.ly = lY;
			vars.lz = lZ;
			int facesToDrawFlag = 63 - face.Opposite.Flag;
			vars.decorSubPosition = DecorBits.SubpositionFromIndex(packedIndex);
			vars.decorRotationData = DecorBits.RotationFromIndex(packedIndex);
			int drawType = (int)(block.IsMissing ? EnumDrawType.SurfaceLayer : block.DrawType);
			if (drawType == 8)
			{
				int i = face.Index;
				int rot = vars.decorRotationData % 4;
				if ((block.decorBehaviorFlags & 0x20u) != 0)
				{
					if (rot > 0)
					{
						switch (face.Index)
						{
						case 0:
							i = (rot * 2 + 1) % 6;
							break;
						case 1:
							if (rot == 2)
							{
								rot = 0;
								i = 5;
							}
							else
							{
								rot--;
								i = rot;
							}
							break;
						case 2:
							rot = 4 - rot;
							i = (rot * 2 + 1) % 6;
							break;
						case 3:
							rot = 4 - rot;
							if (rot == 2)
							{
								rot = 0;
								i = 5;
							}
							else
							{
								rot--;
								i = rot;
							}
							break;
						case 5:
							i = 4;
							break;
						}
					}
					else
					{
						i = 4;
					}
				}
				vars.preRotationMatrix = decorRotationMatrices[i + rot * 6];
			}
			else
			{
				vars.preRotationMatrix = null;
			}
			if ((block.decorBehaviorFlags & 4u) != 0 && ((lZ & 1) ^ (lX & 1)) == 1)
			{
				byte zOffsetSave = block.VertexFlags.ZOffset;
				block.VertexFlags.ZOffset = (byte)(zOffsetSave + 2);
				TesselateBlock(block, lX, facesToDrawFlag, baseX + lX, baseZ + lZ, drawType);
				block.VertexFlags.ZOffset = zOffsetSave;
			}
			else
			{
				TesselateBlock(block, lX, facesToDrawFlag, baseX + lX, baseZ + lZ, drawType);
			}
			vars.decorSubPosition = 0;
			vars.decorRotationData = 0;
			vars.preRotationMatrix = null;
		}
	}

	private void SetUpDecorRotationMatrices()
	{
		for (int rot = 0; rot < 4; rot++)
		{
			float[] matrix = Mat4f.Create();
			Mat4f.Translate(matrix, matrix, 0f, 0.5f, 0.5f);
			Mat4f.RotateX(matrix, matrix, 4.712389f);
			Mat4f.Translate(matrix, matrix, 0f, -0.5f, -0.5f);
			SetDecorRotationMatrix(matrix, rot, 0);
			for (int i = 1; i < 4; i++)
			{
				matrix = Mat4f.Create();
				Mat4f.Translate(matrix, matrix, 0.5f, 0.5f, 0.5f);
				Mat4f.RotateY(matrix, matrix, (float)Math.PI / 2f * (float)(4 - i));
				Mat4f.RotateX(matrix, matrix, 4.712389f);
				Mat4f.Translate(matrix, matrix, -0.5f, -0.5f, -0.5f);
				SetDecorRotationMatrix(matrix, rot, i);
			}
			SetDecorRotationMatrix((rot == 0) ? null : Mat4f.Create(), rot, 4);
			matrix = Mat4f.Create();
			Mat4f.Translate(matrix, matrix, 0f, 0.5f, 0.5f);
			Mat4f.RotateX(matrix, matrix, (float)Math.PI);
			Mat4f.Translate(matrix, matrix, 0f, -0.5f, -0.5f);
			SetDecorRotationMatrix(matrix, rot, 5);
		}
	}

	private void SetDecorRotationMatrix(float[] matrix, int rot, int i)
	{
		if (rot > 0)
		{
			Mat4f.Translate(matrix, matrix, 0.5f, 0f, 0.5f);
			Mat4f.RotateY(matrix, matrix, (float)(4 - rot) * ((float)Math.PI / 2f));
			Mat4f.Translate(matrix, matrix, -0.5f, 0f, -0.5f);
		}
		decorRotationMatrices[rot * 6 + i] = matrix;
	}

	private void TesselateBlock(int index3d, int extIndex3dBase, int lX, int baseX, int posZ)
	{
		int flags;
		if ((flags = currentChunkDraw32[index3d]) != 0)
		{
			vars.index3d = index3d;
			Block block2 = currentChunkBlocksExt[vars.extIndex3d = extIndex3dBase + lX];
			TesselateBlock(block2, lX, flags, baseX + lX, posZ, (int)block2.DrawType);
		}
		if ((flags = currentChunkDrawFluids[index3d]) != 0)
		{
			vars.index3d = index3d;
			Block block = currentChunkFluidBlocksExt[vars.extIndex3d = extIndex3dBase + lX];
			TesselateBlock(block, lX, flags, baseX + lX, posZ, (int)block.DrawType);
		}
	}

	private void TesselateBlock(Block block, int lX, int faceflags, int posX, int posZ, int drawType)
	{
		if (block.DrawType != EnumDrawType.Empty)
		{
			vars.block = block;
			vars.drawFaceFlags = faceflags;
			vars.posX = posX;
			vars.lx = lX;
			vars.finalX = lX;
			vars.finalY = vars.ly;
			if (block is IDrawYAdjustable idya)
			{
				vars.finalY += idya.AdjustYPosition(new BlockPos(vars.posX, vars.posY, vars.posZ), currentChunkBlocksExt, vars.extIndex3d);
			}
			vars.finalZ = vars.lz;
			int id = (vars.blockId = block.BlockId);
			vars.textureSubId = 0;
			vars.VertexFlags = block.VertexFlags.All;
			vars.RenderPass = block.RenderPass;
			vars.fastBlockTextureSubidsByFace = fastBlockTextureSubidsByBlockAndFace[id];
			if (block.RandomDrawOffset != 0)
			{
				vars.finalX += (float)(GameMath.oaatHash(posX, 0, posZ) % 12) / (24f + 12f * (float)block.RandomDrawOffset);
				vars.finalZ += (float)(GameMath.oaatHash(posX, 1, posZ) % 12) / (24f + 12f * (float)block.RandomDrawOffset);
			}
			if (block.ShapeUsesColormap || block.LoadColorMapAnyway || block.Frostable)
			{
				int x = posX + GameMath.MurmurHash3Mod(posX, 0, posZ, 5) - 2;
				int z = posZ + GameMath.MurmurHash3Mod(posX, 1, posZ, 5) - 2;
				int regionx = posX / regionSize;
				int regionz = posZ / regionSize;
				int climate = currentClimateRegionMap[GameMath.Clamp(z - regionz * regionSize, 0, regionSize - 1) * regionSize + GameMath.Clamp(x - regionx * regionSize, 0, regionSize - 1)];
				TCTCache tCTCache = vars;
				ColorMap seasonColorMapResolved = block.SeasonColorMapResolved;
				int seasonMapIndex = ((seasonColorMapResolved != null) ? (seasonColorMapResolved.RectIndex + 1) : 0);
				ColorMap climateColorMapResolved = block.ClimateColorMapResolved;
				tCTCache.ColorMapData = new ColorMapData(seasonMapIndex, (climateColorMapResolved != null) ? (climateColorMapResolved.RectIndex + 1) : 0, Climate.GetAdjustedTemperature((climate >> 16) & 0xFF, vars.posY - seaLevel), Climate.GetRainFall((climate >> 8) & 0xFF, vars.posY), block.Frostable);
			}
			else
			{
				vars.ColorMapData = defaultColorMapData;
			}
			vars.textureVOffset = ((block.alternatingVOffset && (((block.alternatingVOffsetFaces & 0xA) > 0 && posX % 2 == 1) || ((block.alternatingVOffsetFaces & 0x30) > 0 && vars.posY % 2 == 1) || ((block.alternatingVOffsetFaces & 5) > 0 && posZ % 2 == 1))) ? 1f : 0f);
			blockTesselators[drawType].Tesselate(vars);
		}
	}

	private void BuildExtendedChunkData(ClientChunk curChunk, int chunkX, int chunkY, int chunkZ, bool atMapEdge, bool skipChunkCenter)
	{
		int extendedChunkSize = 34;
		int validBlocks = game.Blocks.Count;
		game.WorldMap.GetNeighbouringChunks(chunksNearby, chunkX, chunkY, chunkZ);
		for (int j = 26; j >= 0; j--)
		{
			chunksNearby[j].Unpack();
			chunkdatasNearby[j] = (ClientChunkData)chunksNearby[j].Data;
			chunkdatasNearby[j].blocksLayer?.ClearPaletteOutsideMaxValue(validBlocks);
		}
		chunkdatasNearby[13].BuildFastBlockAccessArray(blocksFast);
		int maxEdge = extendedChunkSize - 1;
		ClientChunkData chunkdata = (ClientChunkData)curChunk.Data;
		int index3d = 0;
		int constOffset = 1190;
		int extIndex3d;
		if (skipChunkCenter)
		{
			for (int y2 = 0; y2 < 32; y2++)
			{
				for (int z2 = 0; z2 < 32; z2++)
				{
					extIndex3d = (y2 * 34 + z2) * 34 + constOffset;
					if ((y2 + 2) % 32 <= 3 || (z2 + 2) % 32 <= 3)
					{
						chunkdata.GetRange_Faster(currentChunkBlocksExt, currentChunkFluidBlocksExt, currentChunkRgbsExt, extIndex3d, index3d, index3d + 32, blocksFast, lightConverter);
						index3d += 32;
						continue;
					}
					chunkdata.GetRange_Faster(currentChunkBlocksExt, currentChunkFluidBlocksExt, currentChunkRgbsExt, extIndex3d, index3d, index3d + 2, blocksFast, lightConverter);
					extIndex3d += 30;
					index3d += 30;
					chunkdata.GetRange_Faster(currentChunkBlocksExt, currentChunkFluidBlocksExt, currentChunkRgbsExt, extIndex3d, index3d, index3d + 2, blocksFast, lightConverter);
					index3d += 2;
				}
			}
		}
		else
		{
			for (int y = 0; y < 32; y++)
			{
				for (int z3 = 0; z3 < 32; z3++)
				{
					extIndex3d = (y * 34 + z3) * 34 + constOffset;
					chunkdata.GetRange_Faster(currentChunkBlocksExt, currentChunkFluidBlocksExt, currentChunkRgbsExt, extIndex3d, index3d, index3d + 32, blocksFast, lightConverter);
					index3d += 32;
				}
			}
		}
		extIndex3d = -1;
		for (int extendedLY = 0; extendedLY < extendedChunkSize; extendedLY++)
		{
			bool edgeY = extendedLY == 0 || extendedLY == maxEdge;
			for (int extendedLZ = 0; extendedLZ < extendedChunkSize; extendedLZ++)
			{
				bool num = extendedLZ == 0 || extendedLZ == maxEdge;
				int iy = ((!edgeY) ? 1 : ((extendedLY != 0) ? 2 : 0));
				int iz = ((!num) ? 1 : ((extendedLZ != 0) ? 2 : 0));
				int num2 = (extendedLY - 1) & 0x1F;
				int z = (extendedLZ - 1) & 0x1F;
				index3d = (num2 * 32 + z) * 32;
				int cqaIndex = iy * 3 + iz;
				chunkdata = chunkdatasNearby[cqaIndex];
				int blockId = chunkdata.GetOne(out var light, out var lightSat, out var fluidId, index3d + 31);
				currentChunkBlocksExt[++extIndex3d] = blocksFast[blockId];
				currentChunkFluidBlocksExt[extIndex3d] = blocksFast[fluidId];
				currentChunkRgbsExt[extIndex3d] = lightConverter.ToRgba(light, lightSat);
				cqaIndex += 9;
				if (cqaIndex == 13)
				{
					extIndex3d += 32;
				}
				else
				{
					chunkdata = chunkdatasNearby[cqaIndex];
					chunkdata.GetRange(currentChunkBlocksExt, currentChunkFluidBlocksExt, currentChunkRgbsExt, extIndex3d, index3d, index3d + 32, blocksFast, lightConverter);
					extIndex3d += 32;
				}
				cqaIndex += 9;
				chunkdata = chunkdatasNearby[cqaIndex];
				blockId = chunkdata.GetOne(out light, out lightSat, out fluidId, index3d);
				currentChunkBlocksExt[++extIndex3d] = blocksFast[blockId];
				currentChunkFluidBlocksExt[extIndex3d] = blocksFast[fluidId];
				currentChunkRgbsExt[extIndex3d] = lightConverter.ToRgba(light, lightSat);
			}
		}
		for (int i = 0; i < currentChunkBlocksExt.Length; i++)
		{
			if (currentChunkBlocksExt[i] == null)
			{
				currentChunkBlocksExt[i] = blocksFast[0];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshData GetMeshPoolForPass(int textureid, EnumChunkRenderPass renderPass, int lodLevel)
	{
		int atlasNum = 0;
		do
		{
			if (TextureIdToReturnNum[atlasNum] == textureid)
			{
				return currentModeldataByRenderPassByLodLevel[lodLevel][(int)renderPass][atlasNum];
			}
		}
		while (++atlasNum < quantityAtlasses);
		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshData[] GetPoolForPass(EnumChunkRenderPass renderPass, int lodLevel)
	{
		return currentModeldataByRenderPassByLodLevel[lodLevel][(int)renderPass];
	}
}
