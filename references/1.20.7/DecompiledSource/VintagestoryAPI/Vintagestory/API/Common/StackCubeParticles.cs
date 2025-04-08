using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class StackCubeParticles : CollectibleParticleProperties
{
	/// <summary>
	/// The position of the collision to create these particles.
	/// </summary>
	public Vec3d collisionPos;

	/// <summary>
	/// The contents that the particles are built off of.
	/// </summary>
	public ItemStack stack;

	/// <summary>
	/// The amount of particles to be released.
	/// </summary>
	public int quantity;

	/// <summary>
	/// The radius to release the particles.
	/// </summary>
	public float radius;

	/// <summary>
	/// The scale of the particles.
	/// </summary>
	public float scale;

	public Vec3f velocity = new Vec3f();

	public override bool DieInLiquid => false;

	public override bool SwimOnLiquid => stack.Collectible.MaterialDensity < 1000;

	public override Vec3d Pos => new Vec3d(collisionPos.X + rand.NextDouble() * (double)radius - (double)(radius / 2f), collisionPos.Y + 0.10000000149011612, collisionPos.Z + rand.NextDouble() * (double)radius - (double)(radius / 2f));

	public override float Size => scale;

	public override EnumParticleModel ParticleModel => EnumParticleModel.Cube;

	public override float Quantity => quantity;

	public override float LifeLength => 1f + (float)api.World.Rand.NextDouble() / 2f;

	public override int VertexFlags
	{
		get
		{
			if (stack.Class != 0)
			{
				return 0;
			}
			return stack.Block.VertexFlags.GlowLevel;
		}
	}

	public override IParticlePropertiesProvider[] SecondaryParticles => null;

	public StackCubeParticles()
	{
	}

	public StackCubeParticles(Vec3d collisionPos, ItemStack stack, float radius, int quantity, float scale, Vec3f velocity = null)
	{
		this.collisionPos = collisionPos;
		this.stack = stack;
		this.quantity = quantity;
		this.radius = radius;
		this.scale = scale;
		this.velocity = velocity;
	}

	public override int GetRgbaColor(ICoreClientAPI capi)
	{
		return stack.Collectible.GetRandomColor(capi, stack);
	}

	public override Vec3f GetVelocity(Vec3d pos)
	{
		if (velocity != null)
		{
			return new Vec3f(1.5f - 3f * (float)rand.NextDouble() + velocity.X, 1.5f - 3f * (float)rand.NextDouble() + velocity.Y, 1.5f - 3f * (float)rand.NextDouble() + velocity.Z);
		}
		return new Vec3f(1.5f - 3f * (float)rand.NextDouble(), 1.5f - 3f * (float)rand.NextDouble(), 1.5f - 3f * (float)rand.NextDouble());
	}

	public override void ToBytes(BinaryWriter writer)
	{
		collisionPos.ToBytes(writer);
		stack.ToBytes(writer);
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
		collisionPos = Vec3d.CreateFromBytes(reader);
		stack = new ItemStack();
		stack.FromBytes(reader);
		stack.ResolveBlockOrItem(resolver);
		quantity = reader.ReadInt32();
		radius = reader.ReadSingle();
		scale = reader.ReadSingle();
		if (reader.ReadBoolean())
		{
			velocity = Vec3f.CreateFromBytes(reader);
		}
	}
}
