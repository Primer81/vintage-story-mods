using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class BlockCubeParticles : CollectibleParticleProperties
{
	/// <summary>
	/// The position of the particle
	/// </summary>
	public Vec3d particlePos;

	/// <summary>
	/// The amount of particles.
	/// </summary>
	public int quantity;

	/// <summary>
	/// The radius of the particle emission.
	/// </summary>
	public float radius;

	/// <summary>
	/// The scale of the particles.
	/// </summary>
	public float scale;

	private Block block;

	private BlockPos blockpos;

	public Vec3f velocity;

	public override bool DieInLiquid => block.LiquidCode != null;

	public override Vec3d Pos => new Vec3d(particlePos.X + rand.NextDouble() * (double)radius - (double)(radius / 2f), particlePos.Y + 0.10000000149011612, particlePos.Z + rand.NextDouble() * (double)radius - (double)(radius / 2f));

	public override float Size => scale;

	public override EnumParticleModel ParticleModel => EnumParticleModel.Cube;

	public override float Quantity => quantity;

	public override float LifeLength => 0.5f + (float)api.World.Rand.NextDouble() / 4f;

	public override int VertexFlags => block.VertexFlags.GlowLevel;

	public override IParticlePropertiesProvider[] SecondaryParticles => null;

	public BlockCubeParticles()
	{
	}

	public BlockCubeParticles(IWorldAccessor world, BlockPos blockpos, Vec3d particlePos, float radius, int quantity, float scale, Vec3f velocity = null)
	{
		this.particlePos = particlePos;
		this.blockpos = blockpos;
		this.quantity = quantity;
		this.radius = radius;
		this.scale = scale;
		this.velocity = velocity;
		block = world.BlockAccessor.GetBlock(blockpos);
	}

	public override void Init(ICoreAPI api)
	{
		base.Init(api);
		if (block == null)
		{
			block = api.World.BlockAccessor.GetBlock(blockpos);
		}
	}

	public override int GetRgbaColor(ICoreClientAPI capi)
	{
		return block.GetRandomColor(capi, blockpos, BlockFacing.UP);
	}

	public override Vec3f GetVelocity(Vec3d pos)
	{
		if (velocity != null)
		{
			return velocity;
		}
		Vec3f distanceVector = new Vec3f(1.5f - 3f * (float)rand.NextDouble(), 1.5f - 3f * (float)rand.NextDouble(), 1.5f - 3f * (float)rand.NextDouble());
		if (block.IsLiquid())
		{
			distanceVector.Y += 4f;
		}
		return distanceVector;
	}

	public override void ToBytes(BinaryWriter writer)
	{
		particlePos.ToBytes(writer);
		blockpos.ToBytes(writer);
		writer.Write(quantity);
		writer.Write(radius);
		writer.Write(scale);
		writer.Write(velocity != null);
		if (velocity != null)
		{
			velocity.ToBytes(writer);
		}
	}

	public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		particlePos = Vec3d.CreateFromBytes(reader);
		blockpos = BlockPos.CreateFromBytes(reader);
		quantity = reader.ReadInt32();
		radius = reader.ReadSingle();
		scale = reader.ReadSingle();
		if (reader.ReadBoolean())
		{
			velocity = Vec3f.CreateFromBytes(reader);
		}
	}
}
