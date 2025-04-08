using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityParticleMatingGnats : EntityParticle
{
	protected ICoreClientAPI capi;

	protected float dieAccum;

	protected static Random rand = new Random();

	private Vec3d centerPosition;

	private float cohesion;

	public override string Type => "matinggnats";

	public EntityParticleMatingGnats(ICoreClientAPI capi, float cohesion, double x, double y, double z)
	{
		this.capi = capi;
		centerPosition = new Vec3d(x, y, z);
		Position.Set(x + rand.NextDouble() - 0.5, y + rand.NextDouble() - 0.5, z + rand.NextDouble() - 0.5);
		ColorAlpha = 200;
		SwimOnLiquid = true;
		base.GravityStrength = 0f;
		Alive = true;
		base.Size = 0.25f;
		ColorRed = 33;
		ColorGreen = 33;
		ColorBlue = 33;
		this.cohesion = cohesion;
	}

	public override void TickNow(float dt, float physicsdt, ICoreClientAPI api, ParticlePhysics physicsSim)
	{
		base.TickNow(dt, physicsdt, api, physicsSim);
		if (rand.NextDouble() < 0.5)
		{
			Vec3d vec = centerPosition.SubCopy(Position).Normalize();
			Velocity.Add((float)(vec.X / 2.0 + rand.NextDouble() / 8.0 - 0.0625) / (3f / cohesion), (float)(vec.Y / 2.0 + rand.NextDouble() / 8.0 - 0.0625) / 3f, (float)(vec.Z / 2.0 + rand.NextDouble() / 8.0 - 0.0625) / (3f / cohesion));
		}
		Velocity.X = GameMath.Clamp(Velocity.X, -0.5f, 0.5f);
		Velocity.Y = GameMath.Clamp(Velocity.Y, -0.5f, 0.5f);
		Velocity.Z = GameMath.Clamp(Velocity.Z, -0.5f, 0.5f);
	}

	protected override void doSlowTick(ParticlePhysics physicsSim, float dt)
	{
		base.doSlowTick(physicsSim, dt);
		EntityPlayer npe = capi.World.NearestPlayer(Position.X, Position.Y, Position.Z).Entity;
		double sqdist = npe.Pos.SquareHorDistanceTo(Position);
		if (sqdist > 100.0 && (capi.World.BlockAccessor.GetBlock((int)Position.X, (int)Position.Y, (int)Position.Z, 2).IsLiquid() || GlobalConstants.CurrentWindSpeedClient.Length() > 0.35f))
		{
			dieAccum += dt;
			if (dieAccum > 5f)
			{
				Alive = false;
			}
			return;
		}
		if (npe == null || sqdist > 225.0)
		{
			dieAccum += dt;
			if (dieAccum > 10f)
			{
				Alive = false;
			}
		}
		else
		{
			dieAccum = 0f;
		}
		if (sqdist < 4.0)
		{
			Vec3d vec = npe.Pos.XYZ.Sub(Position).Normalize();
			Velocity.Add((0f - (float)vec.X) / 2f, 0f, (0f - (float)vec.Z) / 2f);
			if (capi.World.BlockAccessor.GetBlock((int)Position.X, (int)Position.Y - 1, (int)Position.Z, 1).Replaceable < 6000)
			{
				Velocity.Add(0f, 0.5f, 1f);
			}
		}
	}
}
