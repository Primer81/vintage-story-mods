using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBehaviorRainDrip : BlockBehavior
{
	private Random random;

	protected static SimpleParticleProperties accumParticle;

	protected static SimpleParticleProperties dripParticle;

	protected static readonly Vec3d center;

	private WeatherSystemClient wsys;

	static BlockBehaviorRainDrip()
	{
		accumParticle = null;
		dripParticle = null;
		center = new Vec3d(0.5, 0.125, 0.5);
		accumParticle = new SimpleParticleProperties(1f, 1f, ColorUtil.ColorFromRgba(255, 255, 255, 128), new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f(), 0.6f, 0f, 0.2f, 0.2f);
		accumParticle.MinPos = new Vec3d(0.0, -0.05, 0.0);
		accumParticle.AddPos = new Vec3d(1.0, 0.04, 1.0);
		accumParticle.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 0.5f);
		accumParticle.ClimateColorMap = "climateWaterTint";
		accumParticle.AddQuantity = 1f;
		accumParticle.WindAffected = true;
		dripParticle = new SimpleParticleProperties(1f, 1f, ColorUtil.ColorFromRgba(255, 255, 255, 180), new Vec3d(), new Vec3d(), new Vec3f(0f, 0.08f, 0f), new Vec3f(0f, -0.1f, 0f), 0.6f, 1f, 0.6f, 0.8f);
		dripParticle.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.2f);
		dripParticle.ClimateColorMap = "climateWaterTint";
		accumParticle.DeathParticles = new IParticlePropertiesProvider[1] { dripParticle };
	}

	public BlockBehaviorRainDrip(Block block)
		: base(block)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		random = new Random();
		wsys = api.ModLoader.GetModSystem<WeatherSystemClient>();
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, ref EnumHandling handling)
	{
		if ((double)WeatherSystemClient.CurrentEnvironmentWetness4h < 0.05 || wsys.clientClimateCond.Temperature < 2f)
		{
			return false;
		}
		int rainHeight = world.BlockAccessor.GetRainMapHeightAt(pos);
		if (rainHeight <= pos.Y || (rainHeight <= pos.Y + 1 && world.BlockAccessor.GetBlockAbove(pos, 1, 1).HasBehavior<BlockBehaviorRainDrip>()) || (rainHeight <= pos.Y + 2 && world.BlockAccessor.GetBlockAbove(pos, 2, 1).HasBehavior<BlockBehaviorRainDrip>()))
		{
			handling = EnumHandling.Handled;
			return true;
		}
		return false;
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (random.NextDouble() * 75.0 < (double)WeatherSystemClient.CurrentEnvironmentWetness4h)
		{
			accumParticle.WindAffectednes = windAffectednessAtPos / 2f;
			accumParticle.MinPos.Set(pos.X, pos.InternalY, pos.Z);
			manager.Spawn(accumParticle);
		}
	}
}
