using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class FollowSealevelDiscGenerator : DiscDepositGenerator
{
	[JsonProperty]
	public NatFloat YPosRel;

	private float step;

	public FollowSealevelDiscGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
		: base(api, variant, depositRand, noiseGen)
	{
	}

	public override void Init()
	{
		base.Init();
		if (YPosRel == null)
		{
			Api.World.Logger.Error("Deposit variant {0} in {1} does not have a YPosRel definition. Where shall I place the deposit?", variant.Code, variant.fromFile);
			YPosRel = NatFloat.Zero;
		}
		YPosRel.avg *= TerraGenConfig.seaLevel;
		YPosRel.var *= TerraGenConfig.seaLevel;
	}

	public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
	{
		float yposmin = 9999f;
		float yposmax = -9999f;
		for (int i = 0; i < 100; i++)
		{
			float ypos = YPosRel.nextFloat(1f, DepositRand);
			yposmin = Math.Min(yposmin, ypos);
			yposmax = Math.Max(yposmax, ypos);
		}
		miny = yposmin;
		maxy = yposmax;
	}

	protected override void beforeGenDeposit(IMapChunk mapChunk, BlockPos targetPos)
	{
		ypos = YPosRel.nextFloat(1f, DepositRand);
		posyi = (int)ypos;
		currentRelativeDepth = ypos / (float)TerraGenConfig.seaLevel;
		targetPos.Y = posyi;
		step = (float)mapChunk.MapRegion.OreMapVerticalDistortTop.InnerSize / (float)regionChunkSize;
	}

	protected override void loadYPosAndThickness(IMapChunk heremapchunk, int lx, int lz, BlockPos pos, double distanceToEdge)
	{
		double curTh = (double)depoitThickness * GameMath.Clamp(distanceToEdge * 2.0 - 0.2, 0.0, 1.0);
		hereThickness = (int)curTh + ((DepositRand.NextDouble() < curTh - (double)(int)curTh) ? 1 : 0);
		int yOff = (int)getDepositYDistort(targetPos, lx, lz, step, heremapchunk);
		pos.Y = posyi + yOff;
	}
}
