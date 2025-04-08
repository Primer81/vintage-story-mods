using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityParticleWaterStrider : EntityParticle
{
	private static Random rand = new Random();

	private ICoreClientAPI capi;

	private float jumpCooldown;

	private float dieAccum;

	public override string Type => "waterStrider";

	public EntityParticleWaterStrider(ICoreClientAPI capi, double x, double y, double z)
	{
		this.capi = capi;
		Position.Set(x, y, z);
		ColorAlpha = byte.MaxValue;
		Alive = true;
		base.Size = 0.35f + (float)capi.World.Rand.NextDouble() * 0.125f;
		base.GravityStrength = 0f;
		ColorRed = 70;
		ColorGreen = 109;
		ColorBlue = 117;
		VertexFlags = 402653184;
	}

	public override void TickNow(float dt, float physicsdt, ICoreClientAPI api, ParticlePhysics physicsSim)
	{
		base.TickNow(dt, physicsdt, api, physicsSim);
		Velocity.X *= 0.97f;
		Velocity.Z *= 0.97f;
	}

	protected override void doSlowTick(ParticlePhysics physicsSim, float dt)
	{
		base.doSlowTick(physicsSim, dt);
		if (jumpCooldown < 1.5f && jumpCooldown > 0f && (flags & EnumCollideFlags.CollideY) > (EnumCollideFlags)0 && Velocity.Y <= 0f && rand.NextDouble() < 0.4)
		{
			propel((float)rand.NextDouble() * 0.66f - 0.33f, 0f, (float)rand.NextDouble() * 0.66f - 0.33f);
		}
		if (jumpCooldown > 0f)
		{
			jumpCooldown = GameMath.Max(0f, jumpCooldown - dt);
			return;
		}
		if (rand.NextDouble() < 0.02)
		{
			propel((float)rand.NextDouble() * 0.66f - 0.33f, 0f, (float)rand.NextDouble() * 0.66f - 0.33f);
			return;
		}
		EntityPlayer npe = capi.World.NearestPlayer(Position.X, Position.Y, Position.Z).Entity;
		double sqdist = 2500.0;
		if (npe != null && (sqdist = npe.Pos.SquareHorDistanceTo(Position)) < 9.0)
		{
			Vec3d vec = npe.Pos.XYZ.Sub(Position).Normalize();
			propel((float)(0.0 - vec.X) / 3f, 0f, (float)(0.0 - vec.Z) / 3f);
		}
		Block block = capi.World.BlockAccessor.GetBlock((int)Position.X, (int)Position.Y, (int)Position.Z, 2);
		if (!block.IsLiquid())
		{
			Alive = false;
			return;
		}
		Position.Y = (float)(int)Position.Y + (float)block.LiquidLevel / 8f;
		if (npe == null || sqdist > 400.0)
		{
			dieAccum += dt;
			if (dieAccum > 15f)
			{
				Alive = false;
			}
		}
		else
		{
			dieAccum = 0f;
		}
	}

	private void propel(float dirx, float diry, float dirz)
	{
		Velocity.Add(dirx, diry, dirz);
		jumpCooldown = 2f;
	}
}
