using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Used to add a set of particle properties to a collectible.
/// </summary>
[DocumentAsJson]
[JsonObject(MemberSerialization.OptIn)]
public class AdvancedParticleProperties : IParticlePropertiesProvider
{
	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>Random</jsondefault>-->
	/// The Hue/Saturation/Value/Alpha for the color of the particle.
	/// </summary>
	[JsonProperty]
	public NatFloat[] HsvaColor = new NatFloat[4]
	{
		NatFloat.createUniform(128f, 128f),
		NatFloat.createUniform(128f, 128f),
		NatFloat.createUniform(128f, 128f),
		NatFloat.createUniform(255f, 0f)
	};

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0, 0, 0</jsondefault>-->
	/// Offset from the blocks hitboxes top middle position
	/// </summary>
	[JsonProperty]
	public NatFloat[] PosOffset = new NatFloat[3]
	{
		NatFloat.createUniform(0f, 0f),
		NatFloat.createUniform(0f, 0f),
		NatFloat.createUniform(0f, 0f)
	};

	/// <summary>
	/// The base position for the particles.
	/// </summary>
	public Vec3d basePos = new Vec3d();

	public Vec3f baseVelocity = new Vec3f();

	/// <summary>
	/// The base block for the particle.
	/// </summary>
	public Block block;

	/// <summary>
	/// When HsvaColor is null, this is used
	/// </summary>
	public int Color;

	/// <summary>
	/// Gets the position of the particle in world.
	/// </summary>
	/// <returns></returns>
	private Vec3d tmpPos = new Vec3d();

	private Vec3f tmpVelo = new Vec3f();

	public bool Async => false;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// Allows each particle to randomly change its velocity over time.
	/// </summary>
	[JsonProperty]
	public bool RandomVelocityChange { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// If true, particle dies if it falls below the rain height at its given location
	/// </summary>
	[JsonProperty]
	public bool DieOnRainHeightmap { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// More particles that spawn from this particle over time. See <see cref="P:Vintagestory.API.Common.AdvancedParticleProperties.SecondarySpawnInterval" /> to control rate.
	/// </summary>
	[JsonProperty]
	public AdvancedParticleProperties[] SecondaryParticles { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// More particles that spawn when this particle dies.
	/// </summary>
	[JsonProperty]
	public AdvancedParticleProperties[] DeathParticles { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// The inverval that the <see cref="P:Vintagestory.API.Common.AdvancedParticleProperties.SecondaryParticles" /> spawn.
	/// </summary>
	[JsonProperty]
	public NatFloat SecondarySpawnInterval { get; set; } = NatFloat.createUniform(0f, 0f);


	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// The amount of velocity to be kept when this particle collides with something. Directional velocity is multipled by (-Bounciness * 0.65) on any collision.
	/// </summary>
	[JsonProperty]
	public float Bounciness { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// Whether or not the particle dies in air.
	/// </summary>
	[JsonProperty]
	public bool DieInAir { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// Whether or not the particle dies in water.
	/// </summary>
	[JsonProperty]
	public bool DieInLiquid { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// Whether or not the particle floats on liquids.
	/// </summary>
	[JsonProperty]
	public bool SwimOnLiquid { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// Whether or not to color the particle by the block it's on.
	/// </summary>
	[JsonProperty]
	public bool ColorByBlock { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// A transforming opacity value.
	/// </summary>
	[JsonProperty]
	public EvolvingNatFloat OpacityEvolve { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// A transforming Red value.
	/// </summary>
	[JsonProperty]
	public EvolvingNatFloat RedEvolve { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// A transforming Green value.
	/// </summary>
	[JsonProperty]
	public EvolvingNatFloat GreenEvolve { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// A transforming Blue value.
	/// </summary>
	[JsonProperty]
	public EvolvingNatFloat BlueEvolve { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>1</jsondefault>-->
	/// The gravity effect on the particle.
	/// </summary>
	[JsonProperty]
	public NatFloat GravityEffect { get; set; } = NatFloat.createUniform(1f, 0f);


	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>1</jsondefault>-->
	/// The life length, in seconds, of the particle.
	/// </summary>
	[JsonProperty]
	public NatFloat LifeLength { get; set; } = NatFloat.createUniform(1f, 0f);


	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>1</jsondefault>-->
	/// The quantity of the particles given.
	/// </summary>
	[JsonProperty]
	public NatFloat Quantity { get; set; } = NatFloat.createUniform(1f, 0f);


	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>1</jsondefault>-->
	/// The size of the particles given.
	/// </summary>
	[JsonProperty]
	public NatFloat Size { get; set; } = NatFloat.createUniform(1f, 0f);


	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// A transforming Size value.
	/// </summary>
	[JsonProperty]
	public EvolvingNatFloat SizeEvolve { get; set; } = EvolvingNatFloat.createIdentical(0f);


	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>Random</jsondefault>-->
	/// The velocity of the particles.
	/// </summary>
	[JsonProperty]
	public NatFloat[] Velocity { get; set; } = new NatFloat[3]
	{
		NatFloat.createUniform(0f, 0.5f),
		NatFloat.createUniform(0f, 0.5f),
		NatFloat.createUniform(0f, 0.5f)
	};


	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// A dynamic velocity value.
	/// </summary>
	[JsonProperty]
	public EvolvingNatFloat[] VelocityEvolve { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>Cube</jsondefault>-->
	/// Sets the base model for the particle.
	/// </summary>
	[JsonProperty]
	public EnumParticleModel ParticleModel { get; set; } = EnumParticleModel.Cube;


	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// The level of glow in the particle.
	/// </summary>
	[JsonProperty]
	public int VertexFlags { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>false</jsondefault>-->
	/// Whether or not the particle is self propelled.
	/// </summary>
	[JsonProperty]
	public bool SelfPropelled { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>true</jsondefault>-->
	/// Whether or not the particle collides with the terrain.
	/// </summary>
	[JsonProperty]
	public bool TerrainCollision { get; set; } = true;


	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// How much the particles are affected by wind.
	/// </summary>
	[JsonProperty]
	public float WindAffectednes { get; set; }

	public int LightEmission => 0;

	bool IParticlePropertiesProvider.DieInAir => DieInAir;

	bool IParticlePropertiesProvider.DieInLiquid => DieInLiquid;

	bool IParticlePropertiesProvider.SwimOnLiquid => SwimOnLiquid;

	public Vec3d Pos
	{
		get
		{
			tmpPos.Set(basePos.X + (double)PosOffset[0].nextFloat(), basePos.Y + (double)PosOffset[1].nextFloat(), basePos.Z + (double)PosOffset[2].nextFloat());
			return tmpPos;
		}
	}

	/// <summary>
	/// gets the quantity released.
	/// </summary>
	/// <returns></returns>
	float IParticlePropertiesProvider.Quantity => Quantity.nextFloat();

	/// <summary>
	/// Gets the dynamic size of the particle.
	/// </summary>
	float IParticlePropertiesProvider.Size => Size.nextFloat();

	public Vec3f ParentVelocity { get; set; }

	public float WindAffectednesAtPos { get; set; }

	public float ParentVelocityWeight { get; set; }

	EnumParticleModel IParticlePropertiesProvider.ParticleModel => ParticleModel;

	bool IParticlePropertiesProvider.SelfPropelled => SelfPropelled;

	/// <summary>
	/// Gets the secondary spawn interval.
	/// </summary>
	/// <returns></returns>
	float IParticlePropertiesProvider.SecondarySpawnInterval => SecondarySpawnInterval.nextFloat();

	bool IParticlePropertiesProvider.TerrainCollision => TerrainCollision;

	float IParticlePropertiesProvider.GravityEffect => GravityEffect.nextFloat();

	float IParticlePropertiesProvider.LifeLength => LifeLength.nextFloat();

	IParticlePropertiesProvider[] IParticlePropertiesProvider.SecondaryParticles => SecondaryParticles;

	IParticlePropertiesProvider[] IParticlePropertiesProvider.DeathParticles => DeathParticles;

	/// <summary>
	/// Initializes the particle.
	/// </summary>
	/// <param name="api">The core API.</param>
	public void Init(ICoreAPI api)
	{
	}

	/// <summary>
	/// Converts the color to RGBA.
	/// </summary>
	/// <param name="capi">The Core Client API.</param>
	/// <returns>The set RGBA color.</returns>
	public int GetRgbaColor(ICoreClientAPI capi)
	{
		if (HsvaColor == null)
		{
			return Color;
		}
		int num = ColorUtil.HsvToRgba((byte)GameMath.Clamp(HsvaColor[0].nextFloat(), 0f, 255f), (byte)GameMath.Clamp(HsvaColor[1].nextFloat(), 0f, 255f), (byte)GameMath.Clamp(HsvaColor[2].nextFloat(), 0f, 255f), (byte)GameMath.Clamp(HsvaColor[3].nextFloat(), 0f, 255f));
		int r = num & 0xFF;
		int g = (num >> 8) & 0xFF;
		int b = (num >> 16) & 0xFF;
		int a = (num >> 24) & 0xFF;
		return (r << 16) | (g << 8) | b | (a << 24);
	}

	/// <summary>
	/// Gets the velocity of the particle.
	/// </summary>
	public Vec3f GetVelocity(Vec3d pos)
	{
		tmpVelo.Set(baseVelocity.X + Velocity[0].nextFloat(), baseVelocity.Y + Velocity[1].nextFloat(), baseVelocity.Z + Velocity[2].nextFloat());
		return tmpVelo;
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(basePos.X);
		writer.Write(basePos.Y);
		writer.Write(basePos.Z);
		writer.Write(DieInAir);
		writer.Write(DieInLiquid);
		writer.Write(SwimOnLiquid);
		for (int n = 0; n < 4; n++)
		{
			HsvaColor[n].ToBytes(writer);
		}
		GravityEffect.ToBytes(writer);
		LifeLength.ToBytes(writer);
		for (int m = 0; m < 3; m++)
		{
			PosOffset[m].ToBytes(writer);
		}
		Quantity.ToBytes(writer);
		Size.ToBytes(writer);
		for (int l = 0; l < 3; l++)
		{
			Velocity[l].ToBytes(writer);
		}
		writer.Write((byte)ParticleModel);
		writer.Write(VertexFlags);
		writer.Write(OpacityEvolve == null);
		if (OpacityEvolve != null)
		{
			OpacityEvolve.ToBytes(writer);
		}
		writer.Write(RedEvolve == null);
		if (RedEvolve != null)
		{
			RedEvolve.ToBytes(writer);
		}
		writer.Write(GreenEvolve == null);
		if (GreenEvolve != null)
		{
			GreenEvolve.ToBytes(writer);
		}
		writer.Write(BlueEvolve == null);
		if (BlueEvolve != null)
		{
			BlueEvolve.ToBytes(writer);
		}
		SizeEvolve.ToBytes(writer);
		writer.Write(SelfPropelled);
		writer.Write(TerrainCollision);
		writer.Write(ColorByBlock);
		writer.Write(VelocityEvolve != null);
		if (VelocityEvolve != null)
		{
			for (int k = 0; k < 3; k++)
			{
				VelocityEvolve[k].ToBytes(writer);
			}
		}
		SecondarySpawnInterval.ToBytes(writer);
		if (SecondaryParticles == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(SecondaryParticles.Length);
			for (int j = 0; j < SecondaryParticles.Length; j++)
			{
				SecondaryParticles[j].ToBytes(writer);
			}
		}
		if (DeathParticles == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(DeathParticles.Length);
			for (int i = 0; i < DeathParticles.Length; i++)
			{
				DeathParticles[i].ToBytes(writer);
			}
		}
		writer.Write(WindAffectednes);
		writer.Write(Bounciness);
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		basePos = new Vec3d(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
		DieInAir = reader.ReadBoolean();
		DieInLiquid = reader.ReadBoolean();
		SwimOnLiquid = reader.ReadBoolean();
		HsvaColor = new NatFloat[4]
		{
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader)
		};
		GravityEffect = NatFloat.createFromBytes(reader);
		LifeLength = NatFloat.createFromBytes(reader);
		PosOffset = new NatFloat[3]
		{
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader)
		};
		Quantity = NatFloat.createFromBytes(reader);
		Size = NatFloat.createFromBytes(reader);
		Velocity = new NatFloat[3]
		{
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader)
		};
		ParticleModel = (EnumParticleModel)reader.ReadByte();
		VertexFlags = reader.ReadInt32();
		if (!reader.ReadBoolean())
		{
			OpacityEvolve = EvolvingNatFloat.CreateFromBytes(reader);
		}
		if (!reader.ReadBoolean())
		{
			RedEvolve = EvolvingNatFloat.CreateFromBytes(reader);
		}
		if (!reader.ReadBoolean())
		{
			GreenEvolve = EvolvingNatFloat.CreateFromBytes(reader);
		}
		if (!reader.ReadBoolean())
		{
			BlueEvolve = EvolvingNatFloat.CreateFromBytes(reader);
		}
		SizeEvolve.FromBytes(reader);
		SelfPropelled = reader.ReadBoolean();
		TerrainCollision = reader.ReadBoolean();
		ColorByBlock = reader.ReadBoolean();
		if (reader.ReadBoolean())
		{
			VelocityEvolve = new EvolvingNatFloat[3]
			{
				EvolvingNatFloat.createIdentical(0f),
				EvolvingNatFloat.createIdentical(0f),
				EvolvingNatFloat.createIdentical(0f)
			};
			VelocityEvolve[0].FromBytes(reader);
			VelocityEvolve[1].FromBytes(reader);
			VelocityEvolve[2].FromBytes(reader);
		}
		SecondarySpawnInterval = NatFloat.createFromBytes(reader);
		int secondaryPropCount = reader.ReadInt32();
		if (secondaryPropCount > 0)
		{
			SecondaryParticles = new AdvancedParticleProperties[secondaryPropCount];
			for (int j = 0; j < secondaryPropCount; j++)
			{
				SecondaryParticles[j] = createFromBytes(reader, resolver);
			}
		}
		int deathPropCount = reader.ReadInt32();
		if (deathPropCount > 0)
		{
			DeathParticles = new AdvancedParticleProperties[deathPropCount];
			for (int i = 0; i < deathPropCount; i++)
			{
				DeathParticles[i] = createFromBytes(reader, resolver);
			}
		}
		WindAffectednes = reader.ReadSingle();
		Bounciness = reader.ReadSingle();
	}

	public static AdvancedParticleProperties createFromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		AdvancedParticleProperties advancedParticleProperties = new AdvancedParticleProperties();
		advancedParticleProperties.FromBytes(reader, resolver);
		return advancedParticleProperties;
	}

	public AdvancedParticleProperties Clone()
	{
		AdvancedParticleProperties cloned = new AdvancedParticleProperties();
		using MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		ToBytes(writer);
		ms.Position = 0L;
		cloned.FromBytes(new BinaryReader(ms), null);
		return cloned;
	}

	/// <summary>
	/// Begins the advanced particle.
	/// </summary>
	public void BeginParticle()
	{
		if (WindAffectednes > 0f)
		{
			ParentVelocityWeight = WindAffectednesAtPos * WindAffectednes;
			ParentVelocity = GlobalConstants.CurrentWindSpeedClient;
		}
	}

	/// <summary>
	/// prepares the particle for secondary spawning.
	/// </summary>
	/// <param name="particleInstance"></param>
	public void PrepareForSecondarySpawn(ParticleBase particleInstance)
	{
		Vec3d particlePos = particleInstance.Position;
		basePos.X = particlePos.X;
		basePos.Y = particlePos.Y;
		basePos.Z = particlePos.Z;
	}
}
