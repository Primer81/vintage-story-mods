using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// A subclass of ExplosionSmokeParticles.
/// </summary>
public class ExplosionParticles
{
	public static AdvancedParticleProperties ExplosionFireTrailCubicles;

	public static SimpleParticleProperties ExplosionFireParticles;

	static ExplosionParticles()
	{
		ExplosionFireTrailCubicles = new AdvancedParticleProperties
		{
			HsvaColor = new NatFloat[4]
			{
				NatFloat.createUniform(30f, 20f),
				NatFloat.createUniform(255f, 50f),
				NatFloat.createUniform(255f, 50f),
				NatFloat.createUniform(255f, 0f)
			},
			Size = NatFloat.createUniform(0.5f, 0.2f),
			GravityEffect = NatFloat.createUniform(0.3f, 0f),
			Velocity = new NatFloat[3]
			{
				NatFloat.createUniform(0f, 0.6f),
				NatFloat.createUniform(0.4f, 0f),
				NatFloat.createUniform(0f, 0.6f)
			},
			Quantity = NatFloat.createUniform(30f, 10f),
			VertexFlags = 64,
			SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
			DieInLiquid = true,
			PosOffset = new NatFloat[3]
			{
				NatFloat.createUniform(0f, 0.5f),
				NatFloat.createUniform(0f, 0.5f),
				NatFloat.createUniform(0f, 0.5f)
			},
			SecondaryParticles = new AdvancedParticleProperties[1]
			{
				new AdvancedParticleProperties
				{
					HsvaColor = new NatFloat[4]
					{
						NatFloat.createUniform(0f, 0f),
						NatFloat.createUniform(0f, 0f),
						NatFloat.createUniform(100f, 30f),
						NatFloat.createUniform(220f, 50f)
					},
					OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -16f),
					GravityEffect = NatFloat.createUniform(0f, 0f),
					Size = NatFloat.createUniform(0.25f, 0.05f),
					SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 0.5f),
					Quantity = NatFloat.createUniform(1f, 1f),
					DieInLiquid = true,
					SecondarySpawnInterval = NatFloat.createUniform(0.15f, 0f),
					Velocity = new NatFloat[3]
					{
						NatFloat.createUniform(0f, 0.025f),
						NatFloat.createUniform(0.15f, 0.1f),
						NatFloat.createUniform(0f, 0.025f)
					},
					ParticleModel = EnumParticleModel.Quad,
					VertexFlags = 64
				}
			}
		};
		ExplosionFireParticles = new SimpleParticleProperties(10f, 20f, ColorUtil.ToRgba(150, 255, 255, 0), new Vec3d(), new Vec3d(), new Vec3f(-1.5f, -1.5f, -1.5f), new Vec3f(3f, 3f, 3f), 0.12f, 0f, 0.5f, 1.5f, EnumParticleModel.Quad);
		ExplosionFireParticles.RedEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -255f);
		ExplosionFireParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -12.4f);
		ExplosionFireParticles.AddPos.Set(1.0, 1.0, 1.0);
	}
}
