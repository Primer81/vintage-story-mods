using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

public class GenHotSprings : ModStdWorldGen
{
	private Block[] decorBlocks;

	private Block blocksludgygravel;

	private int boilingWaterBlockId;

	private ICoreServerAPI api;

	private IWorldGenBlockAccessor wgenBlockAccessor;

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(GenChunkColumn, EnumWorldGenPass.TerrainFeatures, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
			api.Event.InitWorldGenerator(initWorldGen, "standard");
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		wgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	public void initWorldGen()
	{
		LoadGlobalConfig(api);
		decorBlocks = new Block[4]
		{
			api.World.GetBlock(new AssetLocation("hotspringbacteria-87deg")),
			api.World.GetBlock(new AssetLocation("hotspringbacteriasmooth-74deg")),
			api.World.GetBlock(new AssetLocation("hotspringbacteriasmooth-65deg")),
			api.World.GetBlock(new AssetLocation("hotspringbacteriasmooth-55deg"))
		};
		blocksludgygravel = api.World.GetBlock(new AssetLocation("sludgygravel"));
		boilingWaterBlockId = api.World.GetBlock(new AssetLocation("boilingwater-still-7")).Id;
	}

	private void GenChunkColumn(IChunkColumnGenerateRequest request)
	{
		Dictionary<Vec3i, HotSpringGenData> data = request.Chunks[0].MapChunk.GetModdata<Dictionary<Vec3i, HotSpringGenData>>("hotspringlocations");
		if (data == null || GetIntersectingStructure(request.ChunkX * 32 + 16, request.ChunkZ * 32 + 16, ModStdWorldGen.SkipHotSpringsgHashCode) != null)
		{
			return;
		}
		int baseX = request.ChunkX * 32;
		int baseZ = request.ChunkZ * 32;
		foreach (KeyValuePair<Vec3i, HotSpringGenData> keyval in data)
		{
			Vec3i centerPos = keyval.Key;
			HotSpringGenData gendata = keyval.Value;
			genHotspring(baseX, baseZ, centerPos, gendata);
		}
	}

	private void genHotspring(int baseX, int baseZ, Vec3i centerPos, HotSpringGenData gendata)
	{
		double doubleRad = 2.0 * gendata.horRadius;
		int mindx = (int)GameMath.Clamp((double)centerPos.X - doubleRad, -32.0, 63.0);
		int maxdx = (int)GameMath.Clamp((double)centerPos.X + doubleRad + 1.0, -32.0, 63.0);
		int mindz = (int)GameMath.Clamp((double)centerPos.Z - doubleRad, -32.0, 63.0);
		int maxdz = (int)GameMath.Clamp((double)centerPos.Z + doubleRad + 1.0, -32.0, 63.0);
		double hRadiusSq = doubleRad * doubleRad;
		int minSurfaceY = 99999;
		int maxSurfaceY = 0;
		int checks = 0;
		long sum = 0L;
		bool lakeHere = false;
		for (int lx2 = mindx; lx2 <= maxdx; lx2++)
		{
			double xdistRel = (double)((lx2 - centerPos.X) * (lx2 - centerPos.X)) / hRadiusSq;
			for (int lz2 = mindz; lz2 <= maxdz; lz2++)
			{
				double zdistRel = (double)((lz2 - centerPos.Z) * (lz2 - centerPos.Z)) / hRadiusSq;
				if (xdistRel + zdistRel < 1.0)
				{
					IMapChunk mc = wgenBlockAccessor.GetMapChunk((baseX + lx2) / 32, (baseZ + lz2) / 32);
					if (mc == null)
					{
						return;
					}
					int surfaceY = mc.WorldGenTerrainHeightMap[GameMath.Mod(lz2, 32) * 32 + GameMath.Mod(lx2, 32)];
					minSurfaceY = Math.Min(minSurfaceY, surfaceY);
					maxSurfaceY = Math.Max(maxSurfaceY, surfaceY);
					checks++;
					sum += surfaceY;
					Block fluidBlock = wgenBlockAccessor.GetBlock(baseX + lx2, surfaceY + 1, baseZ + lz2, 2);
					lakeHere |= fluidBlock.Id != 0 && fluidBlock.LiquidCode != "boilingwater";
				}
			}
		}
		int avgSurfaceY = (int)Math.Round((double)sum / (double)checks);
		int surfaceRoughness = maxSurfaceY - minSurfaceY;
		if (lakeHere || surfaceRoughness >= 4 || minSurfaceY < api.World.SeaLevel + 1 || (float)minSurfaceY > (float)api.WorldManager.MapSizeY * 0.88f)
		{
			return;
		}
		gendata.horRadius = Math.Min(32.0, gendata.horRadius);
		for (int lx = mindx; lx <= maxdx; lx++)
		{
			double xdistRel = (double)((lx - centerPos.X) * (lx - centerPos.X)) / hRadiusSq;
			for (int lz = mindz; lz <= maxdz; lz++)
			{
				double zdistRel = (double)((lz - centerPos.Z) * (lz - centerPos.Z)) / hRadiusSq;
				double xzdist = xdistRel + zdistRel;
				if (xzdist < 1.0)
				{
					genhotSpringColumn(baseX + lx, avgSurfaceY, baseZ + lz, xzdist);
				}
			}
		}
	}

	private void genhotSpringColumn(int posx, int posy, int posz, double xzdist)
	{
		IMapChunk mapchunk = wgenBlockAccessor.GetChunkAtBlockPos(posx, posy, posz)?.MapChunk;
		if (mapchunk == null)
		{
			return;
		}
		int lx = posx % 32;
		int lz = posz % 32;
		int surfaceY = mapchunk.WorldGenTerrainHeightMap[lz * 32 + lx];
		xzdist += (api.World.Rand.NextDouble() / 6.0 - 1.0 / 12.0) * 0.5;
		BlockPos pos = new BlockPos(posx, posy, posz);
		Block hereFluid = wgenBlockAccessor.GetBlock(pos, 2);
		Block heredecorBlock = wgenBlockAccessor.GetDecor(pos, new DecorBits(BlockFacing.UP));
		int decorBlockIndex = (int)Math.Max(1.0, xzdist * 10.0);
		Block decorBlock = ((decorBlockIndex < decorBlocks.Length) ? decorBlocks[decorBlockIndex] : null);
		for (int i = 0; i < Math.Min(decorBlocks.Length - 1, decorBlockIndex); i++)
		{
			if (decorBlocks[i] == heredecorBlock)
			{
				decorBlock = decorBlocks[i];
				break;
			}
		}
		if (hereFluid.Id != 0)
		{
			return;
		}
		bool gravelPlaced = false;
		if (api.World.Rand.NextDouble() > xzdist - 0.4)
		{
			prepareHotSpringBase(posx, posy, posz, surfaceY, preventLiquidSpill: true, decorBlock);
			wgenBlockAccessor.SetBlock(blocksludgygravel.Id, pos);
			gravelPlaced = true;
		}
		if (xzdist < 0.1)
		{
			prepareHotSpringBase(posx, posy, posz, surfaceY, preventLiquidSpill: false);
			wgenBlockAccessor.SetBlock(0, pos, 1);
			wgenBlockAccessor.SetBlock(boilingWaterBlockId, pos);
			wgenBlockAccessor.SetDecor(decorBlocks[0], pos.DownCopy(), BlockFacing.UP);
		}
		else if (decorBlock != null)
		{
			prepareHotSpringBase(posx, posy, posz, surfaceY, preventLiquidSpill: true, decorBlock);
			Block upblock = wgenBlockAccessor.GetBlockAbove(pos, 1, 1);
			if (wgenBlockAccessor.GetBlockAbove(pos, 2, 1).SideSolid[BlockFacing.UP.Index])
			{
				pos.Y += 2;
			}
			else if (upblock.SideSolid[BlockFacing.UP.Index])
			{
				pos.Y++;
			}
			wgenBlockAccessor.SetDecor(decorBlock, pos, BlockFacing.UP);
		}
		else if (xzdist < 0.8 && !gravelPlaced)
		{
			prepareHotSpringBase(posx, posy, posz, surfaceY, preventLiquidSpill: true, decorBlock);
		}
	}

	private void prepareHotSpringBase(int posx, int posy, int posz, int surfaceY, bool preventLiquidSpill = true, Block sideDecorBlock = null)
	{
		BlockPos pos = new BlockPos(posx, posy, posz);
		for (int y2 = posy + 1; y2 <= surfaceY + 1; y2++)
		{
			pos.Y = y2;
			Block block = wgenBlockAccessor.GetBlock(pos);
			Block lblock = wgenBlockAccessor.GetBlock(pos, 2);
			if (preventLiquidSpill && (block == blocksludgygravel || lblock.Id == boilingWaterBlockId))
			{
				break;
			}
			wgenBlockAccessor.SetBlock(0, pos, 1);
			wgenBlockAccessor.SetBlock(0, pos, 2);
			wgenBlockAccessor.SetDecor(api.World.Blocks[0], pos, BlockFacing.UP);
			for (int i = 0; i < Cardinal.ALL.Length; i++)
			{
				Cardinal card = Cardinal.ALL[i];
				BlockPos npos = new BlockPos(pos.X + card.Normali.X, pos.Y, pos.Z + card.Normali.Z);
				if (wgenBlockAccessor.GetBlock(npos, 2).Id != 0)
				{
					wgenBlockAccessor.SetDecor(api.World.Blocks[0], npos.DownCopy(), BlockFacing.UP);
					wgenBlockAccessor.SetBlock(blocksludgygravel.Id, npos, 1);
					if (sideDecorBlock != null)
					{
						wgenBlockAccessor.SetDecor(sideDecorBlock, npos, BlockFacing.UP);
					}
				}
			}
		}
		int lx = posx % 32;
		int lz = posz % 32;
		IMapChunk mapChunk = wgenBlockAccessor.GetMapChunk(posx / 32, posz / 32);
		mapChunk.RainHeightMap[lz * 32 + lx] = (ushort)posy;
		mapChunk.WorldGenTerrainHeightMap[lz * 32 + lx] = (ushort)posy;
		_ = mapChunk.TopRockIdMap[lz * 32 + lx];
		for (int y = posy; y >= posy - 2; y--)
		{
			pos.Y = y;
			wgenBlockAccessor.SetDecor(api.World.Blocks[0], pos, BlockFacing.UP);
			wgenBlockAccessor.SetBlock(blocksludgygravel.Id, pos);
		}
	}
}
