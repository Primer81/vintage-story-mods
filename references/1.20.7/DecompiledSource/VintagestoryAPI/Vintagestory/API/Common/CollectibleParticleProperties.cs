using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Abstract class used for BlockVoxelParticles and ItemVoxelParticles
/// </summary>
public abstract class CollectibleParticleProperties : IParticlePropertiesProvider
{
	public Random rand = new Random();

	public ICoreAPI api;

	public bool Async => false;

	public float Bounciness { get; set; }

	public bool DieOnRainHeightmap { get; set; }

	public virtual bool RandomVelocityChange { get; set; }

	public virtual bool DieInLiquid => false;

	public virtual bool SwimOnLiquid => false;

	public virtual bool DieInAir => false;

	public abstract float Quantity { get; }

	public abstract Vec3d Pos { get; }

	public int LightEmission { get; set; }

	public abstract int VertexFlags { get; }

	public abstract EnumParticleModel ParticleModel { get; }

	public virtual bool SelfPropelled => false;

	public virtual bool TerrainCollision => true;

	public virtual float Size => 1f;

	public virtual float GravityEffect => 1f;

	public virtual float LifeLength => 1.5f;

	public virtual EvolvingNatFloat OpacityEvolve => null;

	public virtual EvolvingNatFloat RedEvolve => null;

	public virtual EvolvingNatFloat GreenEvolve => null;

	public virtual EvolvingNatFloat BlueEvolve => null;

	public virtual EvolvingNatFloat SizeEvolve => null;

	public virtual EvolvingNatFloat[] VelocityEvolve => null;

	public virtual IParticlePropertiesProvider[] SecondaryParticles => null;

	public IParticlePropertiesProvider[] DeathParticles => null;

	public virtual float SecondarySpawnInterval => 0f;

	public Vec3f ParentVelocity { get; set; }

	public float ParentVelocityWeight { get; set; }

	public abstract Vec3f GetVelocity(Vec3d pos);

	public abstract int GetRgbaColor(ICoreClientAPI capi);

	public virtual bool UseLighting()
	{
		return true;
	}

	public Vec3d RandomBlockPos(IBlockAccessor blockAccess, BlockPos pos, Block block, BlockFacing facing = null)
	{
		Cuboidf box = block.GetParticleBreakBox(blockAccess, pos, facing);
		if (facing == null)
		{
			return new Vec3d((double)((float)pos.X + box.X1 + 1f / 32f) + rand.NextDouble() * (double)(box.XSize - 0.0625f), (double)((float)pos.InternalY + box.Y1 + 1f / 32f) + rand.NextDouble() * (double)(box.YSize - 0.0625f), (double)((float)pos.Z + box.Z1 + 1f / 32f) + rand.NextDouble() * (double)(box.ZSize - 0.0625f));
		}
		bool haveBox = box != null;
		Vec3i facev = facing.Normali;
		Vec3d vec3d = new Vec3d((float)pos.X + 0.5f + (float)facev.X / 1.9f + ((!haveBox || facing.Axis != 0) ? 0f : ((facev.X > 0) ? (box.X2 - 1f) : box.X1)), (float)pos.InternalY + 0.5f + (float)facev.Y / 1.9f + ((!haveBox || facing.Axis != EnumAxis.Y) ? 0f : ((facev.Y > 0) ? (box.Y2 - 1f) : box.Y1)), (float)pos.Z + 0.5f + (float)facev.Z / 1.9f + ((!haveBox || facing.Axis != EnumAxis.Z) ? 0f : ((facev.Z > 0) ? (box.Z2 - 1f) : box.Z1)));
		vec3d.Add((rand.NextDouble() - 0.5) * (double)(1 - Math.Abs(facev.X)), (rand.NextDouble() - 0.5) * (double)(1 - Math.Abs(facev.Y)) - (double)((facing == BlockFacing.DOWN) ? 0.1f : 0f), (rand.NextDouble() - 0.5) * (double)(1 - Math.Abs(facev.Z)));
		return vec3d;
	}

	public virtual Block ColorByBlock()
	{
		return null;
	}

	public virtual void ToBytes(BinaryWriter writer)
	{
	}

	public virtual void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
	}

	public void BeginParticle()
	{
	}

	public virtual void PrepareForSecondarySpawn(ParticleBase particleInstance)
	{
	}

	public virtual void Init(ICoreAPI api)
	{
		this.api = api;
	}
}
