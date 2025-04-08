using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client;

public class Particle2D : ParticleBase
{
	public Vec3f StartingVelocity = new Vec3f(0f, 0f, 0f);

	public Vec3f ParentVelocity;

	public float ParentVelocityWeight;

	private float seq;

	private float dir = 1f;

	public bool randomVelocityChange = true;

	private EvolvingNatFloat AccelerationX = EvolvingNatFloat.create(EnumTransformFunction.SINUS, (float)Math.PI * 2f);

	private EvolvingNatFloat AccelerationY = EvolvingNatFloat.create(EnumTransformFunction.COSINUS, 7.539823f);

	public float SizeMultiplier = 1f;

	public float ParticleHeight;

	public EvolvingNatFloat SizeEvolve;

	public EvolvingNatFloat[] VelocityEvolve;

	public byte[] Color;

	public EvolvingNatFloat OpacityEvolve;

	private static Random rand = new Random();

	public override void TickFixedStep(float dt, ICoreClientAPI api, ParticlePhysics physicsSim)
	{
		TickNow(dt, dt, api, physicsSim);
	}

	public override void TickNow(float lifedt, float pdt, ICoreClientAPI api, ParticlePhysics physicsSim)
	{
		SecondsAlive += lifedt;
		if (SecondsAlive > LifeLength)
		{
			Alive = false;
		}
		if (VelocityEvolve != null)
		{
			float relLife = SecondsAlive / LifeLength;
			Position.X += Velocity.X * VelocityEvolve[0].nextFloat(0f, relLife) * pdt;
			Position.Y += Velocity.Y * VelocityEvolve[1].nextFloat(0f, relLife) * pdt;
			Position.Z += Velocity.Z * VelocityEvolve[2].nextFloat(0f, relLife) * pdt;
		}
		else
		{
			Position.X += Velocity.X * pdt;
			Position.Y += Velocity.Y * pdt;
			Position.Z += Velocity.Z * pdt;
		}
		if (ParentVelocity != null)
		{
			Position.Add(ParentVelocity.X * ParentVelocityWeight * pdt, ParentVelocity.Y * ParentVelocityWeight * pdt, ParentVelocity.Z * ParentVelocityWeight * pdt);
		}
		if (randomVelocityChange)
		{
			if (seq > 0f)
			{
				Velocity.X += dir * AccelerationX.nextFloat(0f, seq) * pdt * 10f * SizeMultiplier;
				Velocity.Y += AccelerationY.nextFloat(0f, seq) * pdt * 3f * SizeMultiplier;
				seq += pdt / 3f;
				if (seq > 2f)
				{
					seq = 0f;
				}
				if (rand.NextDouble() < 0.005)
				{
					seq = 0f;
				}
			}
			else
			{
				Velocity.X += (StartingVelocity.X - Velocity.X) * pdt;
				Velocity.Y += (StartingVelocity.Y - Velocity.Y) * pdt;
			}
			if (rand.NextDouble() < 0.005)
			{
				seq = (float)rand.NextDouble() * 0.8f;
				dir = rand.Next(2) * 2 - 1;
			}
		}
		Alive = SecondsAlive < LifeLength;
	}

	public override void UpdateBuffers(MeshData buffer, Vec3d cameraPos, ref int posPosition, ref int rgbaPosition, ref int flagPosition)
	{
		float relLife = SecondsAlive / LifeLength;
		byte alpha = Color[3];
		if (OpacityEvolve != null)
		{
			alpha = (byte)GameMath.Clamp(OpacityEvolve.nextFloat((int)alpha, relLife), 0f, 255f);
		}
		buffer.CustomFloats.Values[posPosition++] = (float)(int)Color[0] / 255f;
		buffer.CustomFloats.Values[posPosition++] = (float)(int)Color[1] / 255f;
		buffer.CustomFloats.Values[posPosition++] = (float)(int)Color[2] / 255f;
		buffer.CustomFloats.Values[posPosition++] = (float)(int)alpha / 255f;
		buffer.CustomFloats.Values[posPosition++] = (float)Position.X;
		buffer.CustomFloats.Values[posPosition++] = (float)Position.Y;
		buffer.CustomFloats.Values[posPosition++] = (float)Position.Z;
		buffer.CustomFloats.Values[posPosition++] = ((SizeEvolve != null) ? SizeEvolve.nextFloat(SizeMultiplier, relLife) : SizeMultiplier);
		buffer.CustomFloats.Values[posPosition++] = (float)VertexFlags / 255f;
	}

	public void SetAlive(float GravityEffect)
	{
		Alive = true;
		SecondsAlive = 0f;
	}
}
