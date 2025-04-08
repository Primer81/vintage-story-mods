using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class FloatingSedimentParticles : ParticlesProviderBase
{
	private Random rand = new Random();

	public Vec3d BasePos = new Vec3d();

	public Vec3d AddPos = new Vec3d();

	public Vec3f AddVelocity = new Vec3f();

	public Block SedimentBlock;

	public BlockPos SedimentPos = new BlockPos();

	public float quantity;

	public int waterColor;

	public override EnumParticleModel ParticleModel => EnumParticleModel.Quad;

	public override bool DieInLiquid => false;

	public override bool DieInAir => true;

	public override float GravityEffect => 0f;

	public override float LifeLength => 7f;

	public override bool SwimOnLiquid => false;

	public override Vec3d Pos => new Vec3d(BasePos.X + rand.NextDouble() * AddPos.X, BasePos.Y + rand.NextDouble() * AddPos.Y, BasePos.Z + AddPos.Z * rand.NextDouble());

	public override float Quantity => quantity;

	public override float Size => 0.15f;

	public override int VertexFlags => 512;

	public override EvolvingNatFloat SizeEvolve => new EvolvingNatFloat(EnumTransformFunction.LINEAR, 1.5f);

	public override EvolvingNatFloat OpacityEvolve => new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -32f);

	public override Vec3f GetVelocity(Vec3d pos)
	{
		return new Vec3f(((float)rand.NextDouble() - 0.5f) / 8f + AddVelocity.X, ((float)rand.NextDouble() - 0.5f) / 8f + AddVelocity.Y, ((float)rand.NextDouble() - 0.5f) / 8f + AddVelocity.Z);
	}

	public override int GetRgbaColor(ICoreClientAPI capi)
	{
		int randomColor = SedimentBlock.GetRandomColor(capi, SedimentPos, BlockFacing.UP);
		int wCol = ((waterColor & 0xFF) << 16) | (waterColor & 0xFF00) | ((waterColor >> 16) & 0xFF) | -16777216;
		return ColorUtil.ColorOverlay(randomColor, wCol, 0.1f);
	}
}
