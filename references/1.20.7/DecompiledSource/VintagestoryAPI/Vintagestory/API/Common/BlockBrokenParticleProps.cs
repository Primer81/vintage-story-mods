using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class BlockBrokenParticleProps : BlockBreakingParticleProps
{
	private EvolvingNatFloat sizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f);

	public override Vec3d Pos => RandomBlockPos(api.World.BlockAccessor, blockdamage.Position, blockdamage.Block);

	public override float Size => 0.5f + (float)rand.NextDouble() * 0.8f;

	public override bool SwimOnLiquid => boyant;

	public override EvolvingNatFloat SizeEvolve => sizeEvolve;

	public override float Quantity => 16 + rand.Next(32);

	public override int VertexFlags => blockdamage.Block.VertexFlags.GlowLevel;

	public override float LifeLength => base.LifeLength + (float)rand.NextDouble();

	public override Vec3f GetVelocity(Vec3d pos)
	{
		return new Vec3f(3f * (float)rand.NextDouble() - 1.5f, 4f * (float)rand.NextDouble(), 3f * (float)rand.NextDouble() - 1.5f) * (1f + (float)rand.NextDouble() / 2f);
	}
}
