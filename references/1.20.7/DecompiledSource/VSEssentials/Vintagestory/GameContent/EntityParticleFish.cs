using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityParticleFish : EntityParticle
{
	protected ICoreClientAPI capi;

	protected float swimCooldown;

	protected float dieAccum;

	protected LCGRandom rand;

	private static int[][] Colors = new int[8][]
	{
		new int[4] { 224, 221, 26, 255 },
		new int[4] { 224, 142, 26, 255 },
		new int[4] { 224, 86, 26, 255 },
		new int[4] { 224, 53, 26, 255 },
		new int[4] { 160, 191, 187, 255 },
		new int[4] { 41, 148, 206, 255 },
		new int[4] { 27, 88, 193, 255 },
		new int[4] { 157, 88, 193, 255 }
	};

	public override string Type => "fish";

	public EntityParticleFish(ICoreClientAPI capi, double x, double y, double z)
	{
		this.capi = capi;
		Position.Set(x, y, z);
		rand = new LCGRandom(this.capi.World.Seed + 6545);
		rand.InitPositionSeed((int)x, (int)z);
		Alive = true;
		base.Size = 0.45f + (float)capi.World.Rand.NextDouble() * 0.25f;
		base.GravityStrength = 0f;
		int nextInt = rand.NextInt(Colors.Length);
		ColorBlue = (byte)Colors[nextInt][0];
		ColorGreen = (byte)Colors[nextInt][1];
		ColorRed = (byte)Colors[nextInt][2];
		ColorAlpha = (byte)Colors[nextInt][3];
	}

	public override void TickNow(float dt, float physicsdt, ICoreClientAPI api, ParticlePhysics physicsSim)
	{
		base.TickNow(dt, physicsdt, api, physicsSim);
		Velocity.X *= 0.9f;
		Velocity.Y *= 0.9f;
		Velocity.Z *= 0.9f;
	}

	protected override void doSlowTick(ParticlePhysics physicsSim, float dt)
	{
		base.doSlowTick(physicsSim, dt);
		if (swimCooldown < 0f)
		{
			float dirx2 = (float)rand.NextDouble() * 0.66f - 0.33f;
			float diry2 = (float)rand.NextDouble() * 0.2f - 0.1f;
			float dirz2 = (float)rand.NextDouble() * 0.66f - 0.33f;
			propel(dirx2, diry2, dirz2);
		}
		if (swimCooldown > 0f)
		{
			swimCooldown = GameMath.Max(0f, swimCooldown - dt);
			return;
		}
		if (rand.NextDouble() < 0.2)
		{
			float dirx = (float)rand.NextDouble() * 0.66f - 0.33f;
			float diry = (float)rand.NextDouble() * 0.2f - 0.1f;
			float dirz = (float)rand.NextDouble() * 0.66f - 0.33f;
			propel(dirx, diry, dirz);
			return;
		}
		EntityPlayer npe = capi.World.NearestPlayer(Position.X, Position.Y, Position.Z).Entity;
		double sqdist = 2500.0;
		if (npe != null && (sqdist = npe.Pos.SquareHorDistanceTo(Position)) < 9.0)
		{
			Vec3d vec = npe.Pos.XYZ.Sub(Position).Normalize();
			propel((float)(0.0 - vec.X) / 3f, 0f, (float)(0.0 - vec.Z) / 3f);
		}
		if (!capi.World.BlockAccessor.GetBlock((int)Position.X, (int)Position.Y, (int)Position.Z, 2).IsLiquid())
		{
			Alive = false;
		}
		else if (npe == null || sqdist > 400.0)
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
		swimCooldown = 1f;
	}
}
