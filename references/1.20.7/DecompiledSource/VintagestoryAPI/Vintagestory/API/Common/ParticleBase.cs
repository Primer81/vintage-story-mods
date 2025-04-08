using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Represents a particle that has been spawned
/// </summary>
public abstract class ParticleBase
{
	public ParticleBase Next;

	public ParticleBase Prev;

	public byte ColorRed;

	public byte ColorGreen;

	public byte ColorBlue;

	public byte ColorAlpha;

	public int VertexFlags;

	public float LifeLength;

	public float SecondsAlive;

	public bool Alive;

	public Vec3f Velocity = new Vec3f();

	public Vec3d Position = new Vec3d();

	public float Bounciness;

	public float prevPosDeltaX;

	public float prevPosDeltaY;

	public float prevPosDeltaZ;

	public float prevPosAdvance;

	protected Vec3f motion = new Vec3f();

	public int lightrgbs;

	public float accum;

	protected byte tickCount;

	protected float tdragnow;

	protected float rnddrag = 1f;

	protected EnumCollideFlags flags;

	/// <summary>
	/// Advances the physics simulation of the particle if "physicsSim.PhysicsTickTime" seconds have passed by, otherwise
	/// it will only advance PrevPosition by the particles velocity.
	/// </summary>
	/// <param name="dt">Will never be above PhysicsTickTime</param>
	/// <param name="api"></param>
	/// <param name="physicsSim"></param>
	public virtual void TickFixedStep(float dt, ICoreClientAPI api, ParticlePhysics physicsSim)
	{
		accum += dt;
		prevPosAdvance += dt / physicsSim.PhysicsTickTime;
		if (accum >= physicsSim.PhysicsTickTime)
		{
			dt = physicsSim.PhysicsTickTime;
			accum = 0f;
			prevPosAdvance = 0f;
			double x = Position.X;
			double y = Position.Y;
			double z = Position.Z;
			TickNow(dt, dt, api, physicsSim);
			SecondsAlive += dt - physicsSim.PhysicsTickTime;
			prevPosDeltaX = (float)(Position.X - x);
			prevPosDeltaY = (float)(Position.Y - y);
			prevPosDeltaZ = (float)(Position.Z - z);
		}
		else
		{
			SecondsAlive += dt;
		}
	}

	protected void updatePositionWithCollision(float pdt, ICoreClientAPI api, ParticlePhysics physicsSim, float height)
	{
		flags = physicsSim.UpdateMotion(Position, motion, height);
		Position.X += (double)motion.X * 0.999;
		Position.Y += (double)motion.Y * 0.999;
		Position.Z += (double)motion.Z * 0.999;
		if (flags > (EnumCollideFlags)0)
		{
			tdragnow = (1f - GameMath.Clamp(6.060606f * pdt, 0.001f, 1f)) * rnddrag;
			if (api.World.Rand.NextDouble() < 0.001)
			{
				rnddrag = Math.Max(0f, (float)api.World.Rand.NextDouble() - 0.1f);
			}
			if ((flags & EnumCollideFlags.CollideX) != 0)
			{
				Velocity.X *= (0f - Bounciness) * 0.65f;
				Velocity.Y *= Math.Min(1f, tdragnow * 5f);
				Velocity.Z *= tdragnow;
			}
			if ((flags & EnumCollideFlags.CollideY) != 0)
			{
				Velocity.X *= tdragnow;
				Velocity.Y *= (0f - Bounciness) * 0.65f;
				Velocity.Z *= tdragnow;
			}
			if ((flags & EnumCollideFlags.CollideZ) != 0)
			{
				Velocity.X *= tdragnow;
				Velocity.Y *= Math.Min(1f, tdragnow * 5f);
				Velocity.Z *= (0f - Bounciness) * 0.65f;
			}
		}
		else
		{
			tdragnow = 1f;
		}
	}

	public abstract void TickNow(float dt, float physicsdt, ICoreClientAPI api, ParticlePhysics physicsSim);

	public abstract void UpdateBuffers(MeshData buffer, Vec3d cameraPos, ref int posPosition, ref int rgbaPosition, ref int flagPosition);
}
