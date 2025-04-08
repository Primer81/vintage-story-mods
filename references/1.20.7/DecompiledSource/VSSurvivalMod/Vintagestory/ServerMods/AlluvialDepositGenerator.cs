using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

[JsonObject(MemberSerialization.OptIn)]
public class AlluvialDepositGenerator : DepositGeneratorBase
{
	[JsonProperty]
	public NatFloat Radius;

	[JsonProperty]
	public NatFloat Thickness;

	[JsonProperty]
	public NatFloat Depth;

	[JsonProperty]
	public int MaxYRoughness = 999;

	protected int worldheight;

	protected int radiusX;

	protected int radiusZ;

	private Random avgQRand = new Random();

	public AlluvialDepositGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
		: base(api, variant, depositRand, noiseGen)
	{
		worldheight = api.World.BlockAccessor.MapSizeY;
	}

	public override void Init()
	{
		if (Radius == null)
		{
			Api.Server.LogWarning("Alluvial Deposit {0} has no radius property defined. Defaulting to uniform radius 10", variant.fromFile);
			Radius = NatFloat.createUniform(10f, 0f);
		}
		if (variant.Climate != null && Radius.avg + Radius.var >= 32f)
		{
			Api.Server.LogWarning("Alluvial Deposit {0} has CheckClimate=true and radius > 32 blocks - this is not supported, sorry. Defaulting to uniform radius 10", variant.fromFile);
			Radius = NatFloat.createUniform(10f, 0f);
		}
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
		float th = Thickness.nextFloat(1f, DepositRand);
		float depositThickness = (int)th + ((DepositRand.NextFloat() < th - (float)(int)th) ? 1 : 0);
		float xRadSqInv = 1f / (float)(radiusX * radiusX);
		float zRadSqInv = 1f / (float)(radiusZ * radiusZ);
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
		IList<Block> blocktypes = Api.World.Blocks;
		double sandMaxY = (double)Api.World.BlockAccessor.MapSizeY * 0.8;
		bool doGravel = (double)depoCenterPos.Y > sandMaxY || (double)DepositRand.NextFloat() > 0.33;
		int rockblockCached = -1;
		Block alluvialblock = null;
		for (int posx = minx; posx < maxx; posx++)
		{
			int lx = posx - baseX;
			int num = posx - depoCenterPos.X;
			float xSq = (float)(num * num) * xRadSqInv;
			for (int posz = minz; posz < maxz; posz++)
			{
				int lz = posz - baseZ;
				int distz = posz - depoCenterPos.Z;
				int posy = heremapchunk.WorldGenTerrainHeightMap[lz * 32 + lx];
				if (posy >= worldheight || Math.Abs(depoCenterPos.Y - posy) > MaxYRoughness)
				{
					continue;
				}
				int rockblockid = heremapchunk.TopRockIdMap[lz * 32 + lx];
				if (rockblockid != rockblockCached)
				{
					rockblockCached = rockblockid;
					Block rockblock = blocktypes[rockblockid];
					alluvialblock = (rockblock.Variant.ContainsKey("rock") ? Api.World.GetBlock(new AssetLocation((doGravel ? "gravel-" : "sand-") + rockblock.Variant["rock"])) : null);
				}
				if (alluvialblock == null || 1.0 - DistortNoiseGen.Noise((double)posx / 3.0, (double)posz / 3.0) * 1.5 + 0.15 - (double)(xSq + (float)(distz * distz) * zRadSqInv) < 0.0)
				{
					continue;
				}
				for (int yy = 0; (float)yy < depositThickness; yy++)
				{
					if (posy > 1)
					{
						int index3d = (posy % 32 * 32 + lz) * 32 + lx;
						IChunkBlocks chunkdata = chunks[posy / 32].Data;
						int blockId = chunkdata.GetBlockIdUnsafe(index3d);
						Block block = blocktypes[blockId];
						if (alluvialblock.BlockMaterial != EnumBlockMaterial.Soil || block.BlockMaterial == EnumBlockMaterial.Soil)
						{
							chunkdata.SetBlockUnsafe(index3d, alluvialblock.BlockId);
							chunkdata.SetFluid(index3d, 0);
							posy--;
						}
					}
				}
			}
		}
	}

	public float GetAbsAvgQuantity()
	{
		float radius = 0f;
		float thickness = 0f;
		for (int i = 0; i < 100; i++)
		{
			radius += Radius.nextFloat(1f, avgQRand);
			thickness += Thickness.nextFloat(1f, avgQRand);
		}
		radius /= 100f;
		thickness /= 100f;
		return thickness * radius * radius * (float)Math.PI * variant.TriesPerChunk;
	}

	public int[] GetBearingBlocks()
	{
		return new int[0];
	}

	public override float GetMaxRadius()
	{
		return (Radius.avg + Radius.var) * 1.3f;
	}

	public override void GetPropickReading(BlockPos pos, int oreDist, int[] blockColumn, out double ppt, out double totalFactor)
	{
		throw new NotImplementedException();
	}

	public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
	{
		throw new NotImplementedException();
	}
}
