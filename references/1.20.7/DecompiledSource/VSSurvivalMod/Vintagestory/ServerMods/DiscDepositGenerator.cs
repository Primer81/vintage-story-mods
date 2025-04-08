using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

[JsonObject(MemberSerialization.OptIn)]
public abstract class DiscDepositGenerator : DepositGeneratorBase
{
	[JsonProperty]
	public DepositBlock InBlock;

	[JsonProperty]
	public DepositBlock PlaceBlock;

	[JsonProperty]
	public DepositBlock SurfaceBlock;

	[JsonProperty]
	public NatFloat Radius;

	[JsonProperty]
	public NatFloat Thickness;

	[JsonProperty]
	public NatFloat Depth;

	[JsonProperty]
	public float SurfaceBlockChance = 0.05f;

	[JsonProperty]
	public float GenSurfaceBlockChance = 1f;

	[JsonProperty]
	public bool IgnoreParentTestPerBlock;

	[JsonProperty]
	public int MaxYRoughness = 999;

	[JsonProperty]
	public bool WithLastLayerBlockCallback;

	[JsonProperty]
	public EnumGradeDistribution GradeDistribution;

	protected float currentRelativeDepth;

	protected Dictionary<int, ResolvedDepositBlock> placeBlockByInBlockId = new Dictionary<int, ResolvedDepositBlock>();

	protected Dictionary<int, ResolvedDepositBlock> surfaceBlockByInBlockId = new Dictionary<int, ResolvedDepositBlock>();

	public MapLayerBase OreMap;

	protected int worldheight;

	protected int regionChunkSize;

	protected int noiseSizeClimate;

	protected int noiseSizeOre;

	protected int regionSize;

	protected BlockPos targetPos = new BlockPos();

	protected int radiusX;

	protected int radiusZ;

	protected float ypos;

	protected int posyi;

	protected int depoitThickness;

	protected int hereThickness;

	public double absAvgQuantity;

	protected DiscDepositGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
		: base(api, variant, depositRand, noiseGen)
	{
		worldheight = api.World.BlockAccessor.MapSizeY;
		regionSize = api.WorldManager.RegionSize;
		regionChunkSize = api.WorldManager.RegionSize / 32;
		noiseSizeClimate = regionSize / TerraGenConfig.climateMapScale;
		noiseSizeOre = regionSize / TerraGenConfig.oreMapScale;
	}

	public override void Init()
	{
		if (Radius == null)
		{
			Api.Server.LogWarning("Deposit {0} has no radius property defined. Defaulting to uniform radius 10", variant.fromFile);
			Radius = NatFloat.createUniform(10f, 0f);
		}
		if (variant.Climate != null && Radius.avg + Radius.var >= 32f)
		{
			Api.Server.LogWarning("Deposit {0} has CheckClimate=true and radius > 32 blocks - this is not supported, sorry. Defaulting to uniform radius 10", variant.fromFile);
			Radius = NatFloat.createUniform(10f, 0f);
		}
		if (InBlock != null)
		{
			Block[] blocks = Api.World.SearchBlocks(InBlock.Code);
			if (blocks.Length == 0)
			{
				Api.Server.LogWarning("Deposit in file {0}, no such blocks found by code/wildcard '{1}'. Deposit will never spawn.", variant.fromFile, InBlock.Code);
			}
			Block[] array = blocks;
			foreach (Block block in array)
			{
				if ((InBlock.AllowedVariants != null && !WildcardUtil.Match(InBlock.Code, block.Code, InBlock.AllowedVariants)) || (InBlock.AllowedVariantsByInBlock != null && !InBlock.AllowedVariantsByInBlock.ContainsKey(block.Code)))
				{
					continue;
				}
				string key = InBlock.Name;
				string value = WildcardUtil.GetWildcardValue(InBlock.Code, block.Code);
				ResolvedDepositBlock resolvedDepositBlock2 = (placeBlockByInBlockId[block.BlockId] = PlaceBlock.Resolve(variant.fromFile, Api, block, key, value));
				if (SurfaceBlock != null)
				{
					surfaceBlockByInBlockId[block.BlockId] = SurfaceBlock.Resolve(variant.fromFile, Api, block, key, value);
				}
				Block[] placeBlocks = resolvedDepositBlock2.Blocks;
				if (variant.ChildDeposits != null)
				{
					DepositVariant[] childDeposits = variant.ChildDeposits;
					foreach (DepositVariant val in childDeposits)
					{
						if (val.GeneratorInst == null)
						{
							val.InitWithoutGenerator(Api);
							val.GeneratorInst = new ChildDepositGenerator(Api, val, DepositRand, DistortNoiseGen);
							val.Attributes.Token.Populate(val.GeneratorInst);
						}
						Block[] array2 = placeBlocks;
						foreach (Block depositblock in array2)
						{
							(val.GeneratorInst as ChildDepositGenerator).ResolveAdd(depositblock, key, value);
						}
					}
				}
				if (block.Id == 0 || !variant.addHandbookAttributes)
				{
					continue;
				}
				if (block.Attributes == null)
				{
					block.Attributes = new JsonObject(JToken.Parse("{}"));
				}
				int[] oreIds = block.Attributes["hostRockFor"].AsArray(new int[0]);
				oreIds = oreIds.Append(placeBlocks.Select((Block b) => b.BlockId).ToArray());
				block.Attributes.Token["hostRockFor"] = JToken.FromObject(oreIds);
				foreach (Block pblock in placeBlocks)
				{
					if (pblock.Attributes == null)
					{
						pblock.Attributes = new JsonObject(JToken.Parse("{}"));
					}
					oreIds = pblock.Attributes["hostRock"].AsArray(new int[0]);
					oreIds = oreIds.Append(block.BlockId);
					pblock.Attributes.Token["hostRock"] = JToken.FromObject(oreIds);
				}
			}
		}
		else
		{
			Api.Server.LogWarning("Deposit in file {0} has no inblock defined, it will never spawn.", variant.fromFile);
		}
		LCGRandom rnd = new LCGRandom(Api.World.Seed);
		absAvgQuantity = GetAbsAvgQuantity(rnd);
	}

	public override void GenDeposit(IBlockAccessor blockAccessor, IServerChunk[] chunks, int chunkX, int chunkZ, BlockPos depoCenterPos, ref Dictionary<BlockPos, DepositVariant> subDepositsToPlace)
	{
		int radius = Math.Min(64, (int)Radius.nextFloat(1f, DepositRand));
		if (radius <= 0)
		{
			return;
		}
		float deform = GameMath.Clamp(DepositRand.NextFloat() - 0.5f, -0.25f, 0.25f);
		radiusX = radius - (int)((float)radius * deform);
		radiusZ = radius + (int)((float)radius * deform);
		int baseX = chunkX * 32;
		int baseZ = chunkZ * 32;
		if (depoCenterPos.X + radiusX < baseX - 6 || depoCenterPos.Z + radiusZ < baseZ - 6 || depoCenterPos.X - radiusX >= baseX + 32 + 6 || depoCenterPos.Z - radiusZ >= baseZ + 32 + 6)
		{
			return;
		}
		IMapChunk heremapchunk = chunks[0].MapChunk;
		beforeGenDeposit(heremapchunk, depoCenterPos);
		if (!shouldGenDepositHere(depoCenterPos))
		{
			return;
		}
		int extraGrade = ((GradeDistribution == EnumGradeDistribution.RandomPlusDepthBonus) ? GameMath.RoundRandom(DepositRand, GameMath.Clamp(1f - currentRelativeDepth, 0f, 1f)) : 0);
		int depositGradeIndex = ((PlaceBlock.MaxGrade != 0) ? Math.Min(PlaceBlock.MaxGrade - 1, DepositRand.NextInt(PlaceBlock.MaxGrade) + extraGrade) : 0);
		float th = Thickness.nextFloat(1f, DepositRand);
		depoitThickness = (int)th + ((DepositRand.NextFloat() < th - (float)(int)th) ? 1 : 0);
		float xRadSqInv = 1f / (float)(radiusX * radiusX);
		float zRadSqInv = 1f / (float)(radiusZ * radiusZ);
		bool parentBlockOk = false;
		ResolvedDepositBlock resolvedPlaceBlock = null;
		bool shouldGenSurfaceDeposit = DepositRand.NextFloat() <= GenSurfaceBlockChance && SurfaceBlock != null;
		int minx = baseX - 6;
		int maxx = baseX + 32 + 6;
		int minz = baseZ - 6;
		int maxz = baseZ + 32 + 6;
		minx = GameMath.Clamp(depoCenterPos.X - radiusX, minx, maxx);
		maxx = GameMath.Clamp(depoCenterPos.X + radiusX, minx, maxx);
		minz = GameMath.Clamp(depoCenterPos.Z - radiusZ, minz, maxz);
		maxz = GameMath.Clamp(depoCenterPos.Z + radiusZ, minz, maxz);
		if (minx < baseX)
		{
			minx = baseX;
		}
		if (maxx > baseX + 32)
		{
			maxx = baseX + 32;
		}
		if (minz < baseZ)
		{
			minz = baseZ;
		}
		if (maxz > baseZ + 32)
		{
			maxz = baseZ + 32;
		}
		float invChunkAreaSize = 0.0009765625f;
		for (int posx = minx; posx < maxx; posx++)
		{
			int lx = posx - baseX;
			int num = posx - depoCenterPos.X;
			float xSq = (float)(num * num) * xRadSqInv;
			for (int posz = minz; posz < maxz; posz++)
			{
				int posy = depoCenterPos.Y;
				int lz = posz - baseZ;
				int distz = posz - depoCenterPos.Z;
				double distanceToEdge = 1.0 - ((radius > 3) ? (DistortNoiseGen.Noise((double)posx / 3.0, (double)posz / 3.0) * 0.2) : 0.0) - (double)(xSq + (float)(distz * distz) * zRadSqInv);
				if (distanceToEdge < 0.0)
				{
					continue;
				}
				targetPos.Set(posx, posy, posz);
				loadYPosAndThickness(heremapchunk, lx, lz, targetPos, distanceToEdge);
				posy = targetPos.Y;
				if (posy >= worldheight || Math.Abs(depoCenterPos.Y - posy) > MaxYRoughness)
				{
					continue;
				}
				for (int y = 0; y < hereThickness; y++)
				{
					if (posy <= 1)
					{
						continue;
					}
					int index3d = (posy % 32 * 32 + lz) * 32 + lx;
					int blockId = chunks[posy / 32].Data.GetBlockIdUnsafe(index3d);
					if (!IgnoreParentTestPerBlock || !parentBlockOk)
					{
						parentBlockOk = placeBlockByInBlockId.TryGetValue(blockId, out resolvedPlaceBlock);
					}
					if (parentBlockOk && resolvedPlaceBlock.Blocks.Length != 0)
					{
						int gradeIndex = Math.Min(resolvedPlaceBlock.Blocks.Length - 1, depositGradeIndex);
						Block placeblock = resolvedPlaceBlock.Blocks[gradeIndex];
						if (variant.WithBlockCallback || (WithLastLayerBlockCallback && y == hereThickness - 1))
						{
							targetPos.Y = posy;
							placeblock.TryPlaceBlockForWorldGen(blockAccessor, targetPos, BlockFacing.UP, DepositRand);
						}
						else
						{
							IChunkBlocks data = chunks[posy / 32].Data;
							data.SetBlockUnsafe(index3d, placeblock.BlockId);
							data.SetFluid(index3d, 0);
						}
						DepositVariant[] childDeposits = variant.ChildDeposits;
						if (childDeposits != null)
						{
							for (int i = 0; i < childDeposits.Length; i++)
							{
								float rndVal = DepositRand.NextFloat();
								float quantity = childDeposits[i].TriesPerChunk * invChunkAreaSize;
								if (quantity > rndVal && ShouldPlaceAdjustedForOreMap(childDeposits[i], posx, posz, quantity, rndVal))
								{
									subDepositsToPlace[new BlockPos(posx, posy, posz)] = childDeposits[i];
								}
							}
						}
						if (shouldGenSurfaceDeposit)
						{
							int surfaceY = heremapchunk.RainHeightMap[lz * 32 + lx];
							int depth = surfaceY - posy;
							float chance = SurfaceBlockChance * Math.Max(0f, 1.11f - (float)depth / 9f);
							if (surfaceY < worldheight - 1 && DepositRand.NextFloat() < chance && Api.World.Blocks[chunks[surfaceY / 32].Data.GetBlockIdUnsafe((surfaceY % 32 * 32 + lz) * 32 + lx)].SideSolid[BlockFacing.UP.Index])
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
					posy--;
				}
			}
		}
	}

	protected virtual bool shouldGenDepositHere(BlockPos depoCenterPos)
	{
		return true;
	}

	protected abstract void beforeGenDeposit(IMapChunk mapChunk, BlockPos pos);

	protected abstract void loadYPosAndThickness(IMapChunk heremapchunk, int lx, int lz, BlockPos pos, double distanceToEdge);

	public float getDepositYDistort(BlockPos pos, int lx, int lz, float step, IMapChunk heremapchunk)
	{
		int rdx = pos.X / 32 % regionChunkSize;
		int rdz = pos.Z / 32 % regionChunkSize;
		IMapRegion mapRegion = heremapchunk.MapRegion;
		float yOffTop = mapRegion.OreMapVerticalDistortTop.GetIntLerpedCorrectly((float)rdx * step + step * ((float)lx / 32f), (float)rdz * step + step * ((float)lz / 32f)) - 20f;
		float num = mapRegion.OreMapVerticalDistortBottom.GetIntLerpedCorrectly((float)rdx * step + step * ((float)lx / 32f), (float)rdz * step + step * ((float)lz / 32f)) - 20f;
		float yRel = (float)pos.Y / (float)worldheight;
		return num * (1f - yRel) + yOffTop * yRel;
	}

	private bool ShouldPlaceAdjustedForOreMap(DepositVariant variant, int posX, int posZ, float quantity, float rndVal)
	{
		if (variant.WithOreMap)
		{
			return variant.GetOreMapFactor(posX / 32, posZ / 32) * quantity > rndVal;
		}
		return true;
	}

	public override void GetPropickReading(BlockPos pos, int oreDist, int[] blockColumn, out double ppt, out double totalFactor)
	{
		int qchunkblocks = Api.World.BlockAccessor.GetTerrainMapheightAt(pos) * 32 * 32;
		double oreMapFactor = (double)(oreDist & 0xFF) / 255.0;
		double rockFactor = oreBearingBlockQuantityRelative(pos, variant.Code, blockColumn);
		totalFactor = oreMapFactor * rockFactor;
		double relq = totalFactor * absAvgQuantity / (double)qchunkblocks;
		ppt = relq * 1000.0;
	}

	private double oreBearingBlockQuantityRelative(BlockPos pos, string oreCode, int[] blockColumn)
	{
		HashSet<int> oreBearingBlocks = new HashSet<int>();
		if (variant == null)
		{
			return 0.0;
		}
		int[] blocks = GetBearingBlocks();
		if (blocks == null)
		{
			return 1.0;
		}
		int[] array = blocks;
		foreach (int val in array)
		{
			oreBearingBlocks.Add(val);
		}
		GetYMinMax(pos, out var minYAvg, out var maxYAvg);
		int q = 0;
		for (int ypos = 0; ypos < blockColumn.Length; ypos++)
		{
			if (!((double)ypos < minYAvg) && !((double)ypos > maxYAvg) && oreBearingBlocks.Contains(blockColumn[ypos]))
			{
				q++;
			}
		}
		return (double)q / (double)blockColumn.Length;
	}

	[Obsolete("Use GetAbsAvgQuantity(LCGRandom rnd) instead to ensure your code is seed deterministic.")]
	public float GetAbsAvgQuantity()
	{
		return GetAbsAvgQuantity(new LCGRandom(Api.World.Seed));
	}

	public float GetAbsAvgQuantity(LCGRandom rnd)
	{
		float radius = 0f;
		float thickness = 0f;
		for (int i = 0; i < 100; i++)
		{
			radius += Radius.nextFloat(1f, rnd);
			thickness += Thickness.nextFloat(1f, rnd);
		}
		radius /= 100f;
		thickness /= 100f;
		return thickness * radius * radius * (float)Math.PI * variant.TriesPerChunk;
	}

	public int[] GetBearingBlocks()
	{
		return placeBlockByInBlockId.Keys.ToArray();
	}

	public override float GetMaxRadius()
	{
		return (Radius.avg + Radius.var) * 1.3f;
	}
}
