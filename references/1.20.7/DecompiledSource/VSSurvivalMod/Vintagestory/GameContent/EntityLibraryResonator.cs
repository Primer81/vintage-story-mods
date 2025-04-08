using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityLibraryResonator : EntityAgent
{
	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (Api.Side == EnumAppSide.Client && Api.World.Rand.NextDouble() < 0.05)
		{
			Api.World.SpawnParticles(new SimpleParticleProperties
			{
				MinPos = Pos.XYZ.AddCopy(-0.5, 0.10000000149011612, -0.5),
				AddPos = new Vec3d(1.0, 0.10000000149011612, 1.0),
				MinQuantity = 3f,
				OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -75f),
				ParticleModel = EnumParticleModel.Quad,
				GravityEffect = 0f,
				LifeLength = 6f,
				MinSize = 0.125f,
				MaxSize = 0.125f,
				MinVelocity = new Vec3f(-0.0625f, 1f / 32f, -0.0625f),
				AddVelocity = new Vec3f(0.125f, 0.0625f, 0.125f),
				Color = ColorUtil.ColorFromRgba(200, 250, 250, 75)
			});
		}
	}
}
