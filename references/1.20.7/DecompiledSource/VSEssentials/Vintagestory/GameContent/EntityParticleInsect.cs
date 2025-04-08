using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class EntityParticleInsect : EntityParticle
{
	protected ICoreClientAPI capi;

	protected float jumpCooldown;

	protected float dieAccum;

	protected float soundCoolDownLeft;

	protected static Random rand = new Random();

	protected float jumpHeight = 1.3f;

	protected AssetLocation sound;

	protected bool doubleJump = true;

	protected float soundCoolDown = 5f;

	protected virtual float soundRange => 16f;

	protected virtual float despawnDistanceSq => 400f;

	public EntityParticleInsect(ICoreClientAPI capi, double x, double y, double z)
	{
		this.capi = capi;
		Position.Set(x, y, z);
		ColorAlpha = byte.MaxValue;
		SwimOnLiquid = true;
		Alive = true;
		base.Size = 0.5f + (float)capi.World.Rand.NextDouble() * 0.5f;
	}

	protected override void doSlowTick(ParticlePhysics physicsSim, float dt)
	{
		base.doSlowTick(physicsSim, dt);
		if (capi.World.BlockAccessor.GetBlock((int)Position.X, (int)Position.Y, (int)Position.Z, 2).IsLiquid())
		{
			dieAccum += dt;
			if (dieAccum > 10f)
			{
				Alive = false;
			}
			return;
		}
		if (jumpHeight > 0f && doubleJump && jumpCooldown < 1.5f && jumpCooldown > 0f && (flags & EnumCollideFlags.CollideY) > (EnumCollideFlags)0 && Velocity.Y <= 0f && rand.NextDouble() < 0.4)
		{
			jump((float)rand.NextDouble() - 0.5f, jumpHeight, (float)rand.NextDouble() - 0.5f);
		}
		if (jumpCooldown > 0f)
		{
			jumpCooldown = GameMath.Max(0f, jumpCooldown - dt);
			return;
		}
		soundCoolDownLeft = GameMath.Max(0f, soundCoolDownLeft - dt);
		if (jumpHeight > 0f && rand.NextDouble() < 0.005)
		{
			jump((float)rand.NextDouble() - 0.5f, jumpHeight, (float)rand.NextDouble() - 0.5f);
			return;
		}
		if (soundCoolDownLeft <= 0f && shouldPlaySound())
		{
			playsound();
			soundCoolDownLeft = soundCoolDown;
			return;
		}
		EntityPlayer npe = capi.World.NearestPlayer(Position.X, Position.Y, Position.Z).Entity;
		double sqdist = 2500.0;
		if (npe != null && (sqdist = npe.Pos.SquareHorDistanceTo(Position)) < 9.0 && jumpHeight > 0f)
		{
			Vec3d vec = npe.Pos.XYZ.Sub(Position).Normalize();
			jump((float)(0.0 - vec.X), jumpHeight, (float)(0.0 - vec.Z));
		}
		if (npe == null || sqdist > (double)despawnDistanceSq)
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
	}

	private void playsound()
	{
		EntityPos plrPos = capi.World.Player.Entity.Pos;
		float attnRoom = ((capi.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(plrPos.AsBlockPos).ExitCount < 2) ? 0.1f : 1f);
		float volume = 1f * attnRoom;
		if (volume > 0.05f)
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				capi.World.PlaySoundAt(sound, Position.X, Position.Y, Position.Z, null, RandomPitch(), soundRange, volume);
			}, "playginsectsound");
		}
	}

	protected virtual float RandomPitch()
	{
		return (float)capi.World.Rand.NextDouble() * 0.5f + 0.75f;
	}

	protected virtual bool shouldPlaySound()
	{
		return rand.NextDouble() < 0.01;
	}

	private void jump(float dirx, float diry, float dirz)
	{
		Velocity.Add(dirx, diry, dirz);
		jumpCooldown = 2f;
	}
}
