using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Handles the smoke particles of where the explosion was.
/// </summary>
public class ExplosionSmokeParticles : IParticlePropertiesProvider
{
	private Random rand = new Random();

	public Vec3d basePos = new Vec3d();

	private List<sbyte> offsets = new List<sbyte>();

	private int quantityParticles;

	private SimpleParticleProperties[] providers;

	private int curParticle = -1;

	private int color;

	public bool Async => true;

	public float Bounciness { get; set; }

	public bool RandomVelocityChange { get; set; }

	public bool DieOnRainHeightmap => false;

	public int LightEmission => 0;

	public bool DieInAir => false;

	public bool DieInLiquid => true;

	public bool SwimOnLiquid => false;

	public int VertexFlags => 0;

	public float GravityEffect => -0.04f;

	public bool TerrainCollision => true;

	public float LifeLength => 5f + (float)SimpleParticleProperties.rand.NextDouble();

	public EvolvingNatFloat OpacityEvolve => EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -125f);

	public EvolvingNatFloat RedEvolve => null;

	public EvolvingNatFloat GreenEvolve => null;

	public EvolvingNatFloat BlueEvolve => null;

	public Vec3d Pos
	{
		get
		{
			int index = curParticle * 3 / 3;
			if (index + 2 >= offsets.Count)
			{
				return new Vec3d(basePos.X + rand.NextDouble(), basePos.Y + rand.NextDouble(), basePos.Z + rand.NextDouble());
			}
			return new Vec3d(basePos.X + (double)offsets[index] + rand.NextDouble(), basePos.Y + (double)offsets[index + 1] + rand.NextDouble(), basePos.Z + (double)offsets[index + 2] + rand.NextDouble());
		}
	}

	public float Quantity => quantityParticles;

	public float Size => providers[0].Size;

	public EvolvingNatFloat SizeEvolve => new EvolvingNatFloat(EnumTransformFunction.LINEAR, 8f);

	public EvolvingNatFloat[] VelocityEvolve => new EvolvingNatFloat[3]
	{
		EvolvingNatFloat.create(EnumTransformFunction.INVERSELINEAR, 60f),
		EvolvingNatFloat.create(EnumTransformFunction.INVERSELINEAR, 60f),
		EvolvingNatFloat.create(EnumTransformFunction.INVERSELINEAR, 60f)
	};

	public EnumParticleModel ParticleModel => EnumParticleModel.Quad;

	public bool SelfPropelled => false;

	public float SecondarySpawnInterval => 0f;

	public IParticlePropertiesProvider[] SecondaryParticles => null;

	public IParticlePropertiesProvider[] DeathParticles => null;

	public Vec3f ParentVelocity { get; set; }

	public float ParentVelocityWeight { get; set; }

	public ExplosionSmokeParticles()
	{
		color = ColorUtil.ToRgba(50, 180, 180, 180);
		providers = new SimpleParticleProperties[1]
		{
			new SimpleParticleProperties(1f, 1f, -1, new Vec3d(), new Vec3d(), new Vec3f(-0.25f, 0.1f, -0.25f), new Vec3f(0.25f, 0.1f, 0.25f), 1.5f, -0.025f, 3f, 7.5f, EnumParticleModel.Quad)
		};
		providers[0].AddPos.Set(0.1, 0.1, 0.1);
		quantityParticles += 30;
	}

	public void Init(ICoreAPI api)
	{
	}

	public void AddBlock(BlockPos pos)
	{
		offsets.Add((sbyte)((double)pos.X + 0.5 - basePos.X));
		offsets.Add((sbyte)((double)pos.Y + 0.5 - basePos.Y));
		offsets.Add((sbyte)((double)pos.Z + 0.5 - basePos.Z));
		quantityParticles += 3;
	}

	public void BeginParticle()
	{
		ParentVelocityWeight = 1f;
		ParentVelocity = GlobalConstants.CurrentWindSpeedClient;
		curParticle++;
	}

	public int GetRgbaColor(ICoreClientAPI capi)
	{
		color &= 16777215;
		color |= 50 + SimpleParticleProperties.rand.Next(100) << 24;
		return color;
	}

	public Vec3f GetVelocity(Vec3d pos)
	{
		int index = 3 * curParticle / 3;
		if (index + 2 >= offsets.Count)
		{
			return new Vec3f((float)rand.NextDouble() * 16f - 8f, (float)rand.NextDouble() * 16f - 8f, (float)rand.NextDouble() * 16f - 8f);
		}
		float x = offsets[index];
		float y = offsets[index + 1];
		float z = offsets[index + 2];
		float length = Math.Max(1f, (float)Math.Sqrt(x * x + y * y + z * z));
		return new Vec3f(8f * x / length, 8f * y / length, 8f * z / length);
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(quantityParticles);
		writer.Write(offsets.Count);
		byte[] tmp = new byte[offsets.Count];
		for (int i = 0; i < offsets.Count; i++)
		{
			tmp[i] = (byte)offsets[i];
		}
		writer.Write(tmp);
		writer.Write(basePos.X);
		writer.Write(basePos.Y);
		writer.Write(basePos.Z);
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		quantityParticles = reader.ReadInt32();
		int cnt = reader.ReadInt32();
		offsets.Clear();
		byte[] tmp = reader.ReadBytes(cnt);
		for (int i = 0; i < tmp.Length; i++)
		{
			offsets.Add((sbyte)tmp[i]);
		}
		basePos = new Vec3d(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
	}

	public void AddBlocks(Dictionary<BlockPos, Block> explodedBlocks)
	{
		foreach (BlockPos pos in explodedBlocks.Keys)
		{
			AddBlock(pos);
		}
	}

	public void PrepareForSecondarySpawn(ParticleBase particleInstance)
	{
	}
}
