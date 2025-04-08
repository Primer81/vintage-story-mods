using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class BlockBreakingParticleProps : CollectibleParticleProperties
{
	internal BlockDamage blockdamage;

	public bool boyant;

	private EvolvingNatFloat sizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f);

	public override Vec3d Pos => RandomBlockPos(api.World.BlockAccessor, blockdamage.Position, blockdamage.Block, blockdamage.Facing);

	public override float Size => 0.5f + (float)rand.NextDouble() * 0.8f;

	public override EvolvingNatFloat SizeEvolve => sizeEvolve;

	public override bool SwimOnLiquid => boyant;

	public override EnumParticleModel ParticleModel => EnumParticleModel.Cube;

	public override float Quantity => 0.5f;

	public override int VertexFlags => blockdamage.Block.VertexFlags.GlowLevel;

	public override float LifeLength => base.LifeLength + 0.5f + (float)rand.NextDouble() / 4f;

	public override int GetRgbaColor(ICoreClientAPI capi)
	{
		return blockdamage.Block.GetRandomColor(capi, blockdamage.Position, blockdamage.Facing);
	}

	public override Vec3f GetVelocity(Vec3d pos)
	{
		Vec3i face = blockdamage.Facing.Normali;
		return new Vec3f((float)((face.X == 0) ? (rand.NextDouble() - 0.5) : ((0.25 + rand.NextDouble()) * (double)face.X)), (float)((face.Y == 0) ? (rand.NextDouble() - 0.25) : ((0.75 + rand.NextDouble()) * (double)face.Y)), (float)((face.Z == 0) ? (rand.NextDouble() - 0.5) : ((0.25 + rand.NextDouble()) * (double)face.Z))) * (1f + (float)rand.NextDouble() / 2f);
	}

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(blockdamage.Position.X);
		writer.Write(blockdamage.Position.InternalY);
		writer.Write(blockdamage.Position.Z);
		writer.Write(blockdamage.Facing.Index);
		writer.Write(blockdamage.Block.Id);
	}

	public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		base.FromBytes(reader, resolver);
		blockdamage = new BlockDamage();
		blockdamage.Position = new BlockPos(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
		blockdamage.Facing = BlockFacing.ALLFACES[reader.ReadInt32()];
		blockdamage.Block = resolver.GetBlock(reader.ReadInt32());
	}
}
