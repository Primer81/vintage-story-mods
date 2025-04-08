using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class ChildDepositGenerator : DiscDepositGenerator
{
	[JsonProperty]
	public NatFloat RandomTries;

	public ChildDepositGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
		: base(api, variant, depositRand, noiseGen)
	{
	}

	public override void Init()
	{
	}

	public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
	{
		variant.parentDeposit.GeneratorInst.GetYMinMax(pos, out miny, out maxy);
	}

	public void ResolveAdd(Block inblock, string key, string value)
	{
		placeBlockByInBlockId[inblock.BlockId] = PlaceBlock.Resolve(variant.fromFile, Api, inblock, key, value);
		if (SurfaceBlock != null)
		{
			surfaceBlockByInBlockId[inblock.BlockId] = SurfaceBlock.Resolve(variant.fromFile, Api, inblock, key, value);
		}
	}

	public override void GenDeposit(IBlockAccessor blockAccessor, IServerChunk[] chunks, int originChunkX, int originChunkZ, BlockPos pos, ref Dictionary<BlockPos, DepositVariant> subDepositsToPlace)
	{
		IMapChunk heremapchunk = chunks[0].MapChunk;
		int radius = Math.Min(64, (int)Radius.nextFloat(1f, DepositRand));
		if (radius <= 0)
		{
			return;
		}
		radius++;
		int depositGradeIndex = ((PlaceBlock.AllowedVariants != null) ? DepositRand.NextInt(PlaceBlock.AllowedVariants.Length) : 0);
		bool shouldGenSurfaceDeposit = DepositRand.NextFloat() > 0.35f && SurfaceBlock != null;
		float tries = RandomTries.nextFloat(1f, DepositRand);
		for (int i = 0; (float)i < tries; i++)
		{
			targetPos.Set(pos.X + DepositRand.NextInt(2 * radius + 1) - radius, pos.Y + DepositRand.NextInt(2 * radius + 1) - radius, pos.Z + DepositRand.NextInt(2 * radius + 1) - radius);
			int lx = targetPos.X % 32;
			int lz = targetPos.Z % 32;
			if (targetPos.Y <= 1 || targetPos.Y >= worldheight || lx < 0 || lz < 0 || lx >= 32 || lz >= 32)
			{
				continue;
			}
			int index3d = (targetPos.Y % 32 * 32 + lz) * 32 + lx;
			int blockId = chunks[targetPos.Y / 32].Data.GetBlockIdUnsafe(index3d);
			if (!placeBlockByInBlockId.TryGetValue(blockId, out var resolvedPlaceBlock))
			{
				continue;
			}
			Block placeblock = resolvedPlaceBlock.Blocks[depositGradeIndex];
			if (variant.WithBlockCallback)
			{
				placeblock.TryPlaceBlockForWorldGen(blockAccessor, targetPos, BlockFacing.UP, DepositRand);
			}
			else
			{
				chunks[targetPos.Y / 32].Data[index3d] = placeblock.BlockId;
			}
			if (!shouldGenSurfaceDeposit)
			{
				continue;
			}
			int surfaceY = Math.Min(heremapchunk.RainHeightMap[lz * 32 + lx], Api.World.BlockAccessor.MapSizeY - 2);
			int depth = surfaceY - targetPos.Y;
			float chance = SurfaceBlockChance * Math.Max(0f, 1f - (float)depth / 8f);
			if (surfaceY < worldheight && DepositRand.NextFloat() < chance && Api.World.Blocks[chunks[surfaceY / 32].Data.GetBlockIdUnsafe((surfaceY % 32 * 32 + lz) * 32 + lx)].SideSolid[BlockFacing.UP.Index])
			{
				index3d = ((surfaceY + 1) % 32 * 32 + lz) * 32 + lx;
				IChunkBlocks chunkBlockData = chunks[(surfaceY + 1) / 32].Data;
				if (chunkBlockData.GetBlockIdUnsafe(index3d) == 0)
				{
					chunkBlockData[index3d] = surfaceBlockByInBlockId[blockId].Blocks[0].BlockId;
				}
			}
		}
	}

	protected override void beforeGenDeposit(IMapChunk heremapchunk, BlockPos pos)
	{
	}

	protected override void loadYPosAndThickness(IMapChunk heremapchunk, int lx, int lz, BlockPos targetPos, double distanceToEdge)
	{
	}
}
