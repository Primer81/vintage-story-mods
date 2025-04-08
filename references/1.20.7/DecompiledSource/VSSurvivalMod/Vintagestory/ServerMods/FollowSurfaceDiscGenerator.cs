using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class FollowSurfaceDiscGenerator : DiscDepositGenerator
{
	[JsonProperty]
	public NatFloat YPosRel;

	private float step;

	public FollowSurfaceDiscGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
		: base(api, variant, depositRand, noiseGen)
	{
	}

	protected override void beforeGenDeposit(IMapChunk mapChunk, BlockPos pos)
	{
		ypos = YPosRel.nextFloat(1f, DepositRand);
		pos.Y = (int)ypos;
		int lx = pos.X % 32;
		int lz = pos.Z % 32;
		if (lx < 0 || lz < 0)
		{
			currentRelativeDepth = 0f;
		}
		else
		{
			currentRelativeDepth = ypos / (float)(int)mapChunk.WorldGenTerrainHeightMap[lz * 32 + lx];
		}
		step = (float)mapChunk.MapRegion.OreMapVerticalDistortTop.InnerSize / (float)regionChunkSize;
	}

	public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
	{
		float yrel = 9999f;
		for (int i = 0; i < 100; i++)
		{
			yrel = Math.Min(yrel, YPosRel.nextFloat(1f, DepositRand));
		}
		miny = yrel * (float)pos.Y;
		maxy = (float)pos.Y;
	}

	protected override void loadYPosAndThickness(IMapChunk heremapchunk, int lx, int lz, BlockPos pos, double distanceToEdge)
	{
		hereThickness = depoitThickness;
		pos.Y = (int)(ypos * (float)(int)heremapchunk.WorldGenTerrainHeightMap[lz * 32 + lx]);
		pos.Y -= (int)getDepositYDistort(pos, lx, lz, step, heremapchunk);
		double curTh = (double)depoitThickness * GameMath.Clamp(distanceToEdge * 2.0 - 0.2, 0.0, 1.0);
		hereThickness = (int)curTh + ((DepositRand.NextDouble() < curTh - (double)(int)curTh) ? 1 : 0);
	}
}
