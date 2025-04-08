using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public sealed class ParticleGeneric : ParticleBase
{
	private static EvolvingNatFloat AccelerationX = EvolvingNatFloat.create(EnumTransformFunction.SINUS, (float)Math.PI * 2f);

	private static EvolvingNatFloat AccelerationZ = EvolvingNatFloat.create(EnumTransformFunction.COSINUS, 7.539823f);

	public Vec3f StartingVelocity = new Vec3f(0f, 0f, 0f);

	public Vec3f ParentVelocity;

	public float ParentVelocityWeight;

	public float SizeMultiplier = 1f;

	public float ParticleHeight;

	public EvolvingNatFloat SizeEvolve;

	public EvolvingNatFloat[] VelocityEvolve;

	public EvolvingNatFloat OpacityEvolve;

	public EvolvingNatFloat GreenEvolve;

	public EvolvingNatFloat RedEvolve;

	public EvolvingNatFloat BlueEvolve;

	public int LightEmission;

	public float GravityStrength;

	public bool TerrainCollision;

	public bool SelfPropelled;

	public bool DieInLiquid;

	public bool DieInAir;

	public bool DieOnRainHeightmap;

	public bool SwimOnLiquid;

	public bool RandomVelocityChange;

	public IParticlePropertiesProvider[] SecondaryParticles;

	public float[] SecondarySpawnTimers;

	public IParticlePropertiesProvider[] DeathParticles;

	private byte dirNormalizedX;

	private byte dirNormalizedY;

	private byte dirNormalizedZ;

	private float seq;

	private float dir = 1f;

	public ParticleGeneric()
	{
		SecondarySpawnTimers = new float[4];
	}

	public override void TickNow(float lifedt, float pdt, ICoreClientAPI api, ParticlePhysics physicsSim)
	{
		SecondsAlive += lifedt;
		if (SecondaryParticles != null)
		{
			for (int j = 0; j < SecondaryParticles.Length; j++)
			{
				SecondarySpawnTimers[j] += pdt;
				IParticlePropertiesProvider particleProps2 = SecondaryParticles[j];
				if (SecondarySpawnTimers[j] > particleProps2.SecondarySpawnInterval)
				{
					SecondarySpawnTimers[j] = 0f;
					particleProps2.PrepareForSecondarySpawn(this);
					api.World.SpawnParticles(particleProps2);
				}
			}
		}
		if (TerrainCollision && SelfPropelled)
		{
			Velocity.X += (StartingVelocity.X - Velocity.X) * 0.02f;
			Velocity.Y += (StartingVelocity.Y - Velocity.Y) * 0.02f;
			Velocity.Z += (StartingVelocity.Z - Velocity.Z) * 0.02f;
		}
		Velocity.Y -= GravityStrength * pdt;
		float height = ParticleHeight * SizeMultiplier;
		physicsSim.HandleBoyancy(Position, Velocity, SwimOnLiquid, GravityStrength, pdt, height);
		if (VelocityEvolve != null)
		{
			float relLife = SecondsAlive / LifeLength;
			motion.Set(Velocity.X * VelocityEvolve[0].nextFloat(0f, relLife) * pdt, Velocity.Y * VelocityEvolve[1].nextFloat(0f, relLife) * pdt, Velocity.Z * VelocityEvolve[2].nextFloat(0f, relLife) * pdt);
		}
		else
		{
			motion.Set(Velocity.X * pdt, Velocity.Y * pdt, Velocity.Z * pdt);
		}
		if (ParentVelocity != null)
		{
			motion.Add(ParentVelocity.X * ParentVelocityWeight * pdt * tdragnow, ParentVelocity.Y * ParentVelocityWeight * pdt * tdragnow, ParentVelocity.Z * ParentVelocityWeight * pdt * tdragnow);
		}
		if (TerrainCollision)
		{
			updatePositionWithCollision(pdt, api, physicsSim, height);
		}
		else
		{
			Position.X += motion.X;
			Position.Y += motion.Y;
			Position.Z += motion.Z;
		}
		if (RandomVelocityChange)
		{
			if (seq > 0f)
			{
				Velocity.X += dir * AccelerationX.nextFloat(0f, seq) * pdt * 4f * SizeMultiplier;
				Velocity.Z += AccelerationZ.nextFloat(0f, seq) * pdt * 3f * SizeMultiplier;
				Velocity.Y += (dir * AccelerationX.nextFloat(0f, seq) * pdt * 10f * SizeMultiplier - dir * AccelerationZ.nextFloat(0f, seq) * pdt * 3f * SizeMultiplier) / 10f;
				seq += pdt / 3f;
				if (seq > 2f)
				{
					seq = 0f;
				}
				if (api.World.Rand.NextDouble() < 0.005)
				{
					seq = 0f;
				}
			}
			else
			{
				Velocity.X += (StartingVelocity.X - Velocity.X) * pdt;
				Velocity.Z += (StartingVelocity.Z - Velocity.Z) * pdt;
			}
			if (api.World.Rand.NextDouble() < 0.005)
			{
				seq = (float)api.World.Rand.NextDouble() * 0.5f;
				dir = api.World.Rand.Next(2) * 2 - 1;
			}
		}
		Alive = SecondsAlive < LifeLength && (!DieInAir || physicsSim.BlockAccess.GetBlock((int)Position.X, (int)(Position.Y + 0.15000000596046448), (int)Position.Z, 2).IsLiquid());
		tickCount++;
		if (tickCount > 2)
		{
			lightrgbs = ((LightEmission == int.MaxValue) ? LightEmission : physicsSim.BlockAccess.GetLightRGBsAsInt((int)Position.X, (int)Position.Y, (int)Position.Z));
			if (LightEmission != 0)
			{
				lightrgbs = Math.Max(lightrgbs & 0xFF, LightEmission & 0xFF) | Math.Max(lightrgbs & 0xFF00, LightEmission & 0xFF00) | Math.Max(lightrgbs & 0xFF0000, LightEmission & 0xFF0000);
			}
			if (DieOnRainHeightmap)
			{
				float f = 1f - prevPosAdvance;
				double veloX = prevPosDeltaX * f * pdt * 8f;
				double veloY = prevPosDeltaY * f * pdt * 12f;
				double veloZ = prevPosDeltaZ * f * pdt * 8f;
				Alive &= (double)physicsSim.BlockAccess.GetRainMapHeightAt((int)(Position.X + veloX), (int)(Position.Z + veloZ)) - veloY < Position.Y;
			}
			Alive &= !DieInLiquid || !physicsSim.BlockAccess.GetBlock((int)Position.X, (int)(Position.Y + 0.15000000596046448), (int)Position.Z, 2).IsLiquid();
			tickCount = 0;
			float len = Velocity.Length();
			dirNormalizedX = (byte)(Velocity.X / len * 128f);
			dirNormalizedY = (byte)(Velocity.Y / len * 128f);
			dirNormalizedZ = (byte)(Velocity.Z / len * 128f);
		}
		if (!Alive && DeathParticles != null)
		{
			for (int i = 0; i < DeathParticles.Length; i++)
			{
				IParticlePropertiesProvider particleProps = DeathParticles[i];
				particleProps.PrepareForSecondarySpawn(this);
				api.World.SpawnParticles(particleProps);
			}
		}
	}

	public override void UpdateBuffers(MeshData buffer, Vec3d cameraPos, ref int posPosition, ref int rgbaPosition, ref int flagPosition)
	{
		float relLife = SecondsAlive / LifeLength;
		float f = 1f - prevPosAdvance;
		buffer.CustomFloats.Values[posPosition++] = (float)(Position.X - (double)(prevPosDeltaX * f) - cameraPos.X);
		buffer.CustomFloats.Values[posPosition++] = (float)(Position.Y - (double)(prevPosDeltaY * f) - cameraPos.Y);
		buffer.CustomFloats.Values[posPosition++] = (float)(Position.Z - (double)(prevPosDeltaZ * f) - cameraPos.Z);
		buffer.CustomFloats.Values[posPosition++] = ((SizeEvolve != null) ? SizeEvolve.nextFloat(SizeMultiplier, relLife) : SizeMultiplier);
		byte alpha = ColorAlpha;
		if (OpacityEvolve != null)
		{
			alpha = (byte)GameMath.Clamp(OpacityEvolve.nextFloat((int)alpha, relLife), 0f, 255f);
		}
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)lightrgbs;
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)(lightrgbs >> 8);
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)(lightrgbs >> 16);
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)(lightrgbs >> 24);
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)((float)(int)ColorBlue + ((BlueEvolve == null) ? 0f : BlueEvolve.nextFloat((int)ColorBlue, relLife)));
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)((float)(int)ColorGreen + ((GreenEvolve == null) ? 0f : GreenEvolve.nextFloat((int)ColorGreen, relLife)));
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)((float)(int)ColorRed + ((RedEvolve == null) ? 0f : RedEvolve.nextFloat((int)ColorRed, relLife)));
		buffer.CustomBytes.Values[rgbaPosition++] = alpha;
		buffer.CustomBytes.Values[rgbaPosition++] = dirNormalizedX;
		buffer.CustomBytes.Values[rgbaPosition++] = dirNormalizedY;
		buffer.CustomBytes.Values[rgbaPosition++] = dirNormalizedZ;
		rgbaPosition++;
		buffer.Flags[flagPosition++] = VertexFlags;
	}

	public void Spawned(ParticlePhysics physicsSim)
	{
		Alive = true;
		SecondsAlive = 0f;
		accum = physicsSim.PhysicsTickTime;
		lightrgbs = physicsSim.BlockAccess.GetLightRGBsAsInt((int)Position.X, (int)Position.Y, (int)Position.Z);
		if (SecondaryParticles != null)
		{
			for (int i = 0; i < SecondaryParticles.Length; i++)
			{
				SecondarySpawnTimers[i] = 0f;
			}
		}
	}
}
