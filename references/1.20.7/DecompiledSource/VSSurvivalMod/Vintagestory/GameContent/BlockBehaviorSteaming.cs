using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorSteaming : BlockBehavior
{
	private SimpleParticleProperties steamParticles;

	private static WaterSplashParticles SplashParticleProps = new WaterSplashParticles();

	private ICoreAPI api;

	public BlockBehaviorSteaming(Block block)
		: base(block)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		this.api = api;
		steamParticles = new SimpleParticleProperties(0.025f, 0.05f, ColorUtil.ColorFromRgba(240, 200, 200, 50), new Vec3d(), new Vec3d(1.0, 1.0, 1.0), new Vec3f(-0.7f, 0.2f, -0.7f), new Vec3f(0.7f, 0.5f, 0.7f), 3f, 0f, 1f, 2f, EnumParticleModel.Quad);
		steamParticles.WindAffected = true;
		steamParticles.WindAffectednes = 1f;
		steamParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -20f);
		SplashParticleProps.QuantityMul = 0.0016666667f;
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		return true;
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		Random rand = api.World.Rand;
		steamParticles.Color = ColorUtil.HsvToRgba(110, 40 + rand.Next(50), 200 + rand.Next(30), 50 + rand.Next(40));
		steamParticles.MinPos.Set((float)pos.X + 0.5f, (float)pos.Y + 0.5f, (float)pos.Z + 0.5f);
		manager.Spawn(steamParticles);
		SplashParticleProps.BasePos.Set((float)pos.X + 0.25f, pos.Y, (float)pos.Z + 0.25f);
		manager.Spawn(SplashParticleProps);
	}
}
