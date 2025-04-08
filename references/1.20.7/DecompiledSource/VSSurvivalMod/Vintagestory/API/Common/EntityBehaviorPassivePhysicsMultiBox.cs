using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace Vintagestory.API.Common;

public class EntityBehaviorPassivePhysicsMultiBox : EntityBehaviorPassivePhysics, IRenderer, IDisposable
{
	protected Cuboidf[] OrigCollisionBoxes;

	protected Cuboidf[] CollisionBoxes;

	private WireframeCube entityWf;

	[ThreadStatic]
	protected internal static MultiCollisionTester mcollisionTester;

	private Matrixf mat = new Matrixf();

	private Vec3d tmpPos = new Vec3d();

	private float pushVelocityMul = 1f;

	public double RenderOrder => 0.5;

	public int RenderRange => 99;

	public EntityBehaviorPassivePhysicsMultiBox(Entity entity)
		: base(entity)
	{
		if (mcollisionTester == null)
		{
			mcollisionTester = new MultiCollisionTester();
		}
	}

	public static void InitServer(ICoreServerAPI sapi)
	{
		mcollisionTester = new MultiCollisionTester();
		sapi.Event.PhysicsThreadStart += delegate
		{
			mcollisionTester = new MultiCollisionTester();
		};
	}

	public void Dispose()
	{
		entityWf?.Dispose();
		capi?.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		if (entity.Api is ICoreClientAPI capi)
		{
			capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "PassivePhysicsMultiBoxWf");
			entityWf = WireframeCube.CreateCenterOriginCube(capi, -1);
		}
		base.Initialize(properties, attributes);
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		AdjustCollisionBoxesToYaw(1f, push: false, entity.SidedPos.Yaw);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		Dispose();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (capi.Render.WireframeDebugRender.Entity)
		{
			if (capi.World.Player.Entity.MountedOn != entity)
			{
				AdjustCollisionBoxesToYaw(deltaTime * 60f, push: false, entity.SidedPos.Yaw);
			}
			Cuboidf[] collisionBoxes = CollisionBoxes;
			foreach (Cuboidf collbox in collisionBoxes)
			{
				float colScaleX = collbox.XSize / 2f;
				float colScaleY = collbox.YSize / 2f;
				float colScaleZ = collbox.ZSize / 2f;
				double x = entity.Pos.X + (double)collbox.X1 + (double)colScaleX;
				double y = entity.Pos.Y + (double)collbox.Y1 + (double)colScaleY;
				double z = entity.Pos.Z + (double)collbox.Z1 + (double)colScaleZ;
				entityWf.Render(capi, x, y, z, colScaleX, colScaleY, colScaleZ, 1f, new Vec4f(1f, 0f, 1f, 1f));
			}
		}
	}

	public override void SetProperties(JsonObject attributes)
	{
		base.SetProperties(attributes);
		CollisionBoxes = attributes["collisionBoxes"].AsArray<Cuboidf>();
		OrigCollisionBoxes = attributes["collisionBoxes"].AsArray<Cuboidf>();
	}

	protected override void applyCollision(EntityPos pos, float dtFactor)
	{
		AdjustCollisionBoxesToYaw(dtFactor, push: true, entity.SidedPos.Yaw);
		mcollisionTester.ApplyTerrainCollision(CollisionBoxes, CollisionBoxes.Length, entity, pos, dtFactor, ref newPos, 0f, CollisionYExtra);
	}

	public bool AdjustCollisionBoxesToYaw(float dtFac, bool push, float newYaw)
	{
		adjustBoxesToYaw(newYaw);
		if (push)
		{
			tmpPos.Set(entity.SidedPos.X, entity.SidedPos.Y, entity.SidedPos.Z);
			Cuboidd ccollbox = mcollisionTester.GetCollidingCollisionBox(entity.World.BlockAccessor, CollisionBoxes, CollisionBoxes.Length, tmpPos, alsoCheckTouch: false);
			if (ccollbox != null)
			{
				if (PushoutOfCollisionbox(dtFac / 60f, ccollbox))
				{
					return true;
				}
				return false;
			}
		}
		return true;
	}

	private void adjustBoxesToYaw(float newYaw)
	{
		for (int i = 0; i < OrigCollisionBoxes.Length; i++)
		{
			Cuboidf ocollbox = OrigCollisionBoxes[i];
			float x = ocollbox.MidX;
			float y = ocollbox.MidY;
			float z = ocollbox.MidZ;
			mat.Identity();
			mat.RotateY(newYaw + (float)Math.PI);
			Vec4d newMid = mat.TransformVector(new Vec4d(x, y, z, 1.0));
			Cuboidf collbox = CollisionBoxes[i];
			double value = newMid.X - (double)collbox.MidX;
			double motionZ = newMid.Z - (double)collbox.MidZ;
			if (Math.Abs(value) > 0.01 || Math.Abs(motionZ) > 0.01)
			{
				float wh = ocollbox.Width / 2f;
				float hh = ocollbox.Height / 2f;
				float lh = ocollbox.Length / 2f;
				collbox.Set((float)newMid.X - wh, (float)newMid.Y - hh, (float)newMid.Z - lh, (float)newMid.X + wh, (float)newMid.Y + hh, (float)newMid.Z + lh);
			}
		}
	}

	private bool PushoutOfCollisionbox(float dt, Cuboidd collBox)
	{
		double posX = entity.SidedPos.X;
		double posY = entity.SidedPos.Y;
		double posZ = entity.SidedPos.Z;
		IBlockAccessor ba = entity.World.BlockAccessor;
		Vec3i pushDir = null;
		double shortestDist = 99.0;
		for (int i = 0; i < Cardinal.ALL.Length; i++)
		{
			if (shortestDist <= 0.25)
			{
				break;
			}
			Cardinal cardinal = Cardinal.ALL[i];
			for (int dist = 1; dist <= 4; dist++)
			{
				float r = (float)dist / 4f;
				if (mcollisionTester.GetCollidingCollisionBox(ba, CollisionBoxes, CollisionBoxes.Length, tmpPos.Set(posX + (double)((float)cardinal.Normali.X * r), posY, posZ + (double)((float)cardinal.Normali.Z * r)), alsoCheckTouch: false) == null && (double)r < shortestDist)
				{
					shortestDist = r + (cardinal.IsDiagnoal ? 0.1f : 0f);
					pushDir = cardinal.Normali;
					break;
				}
			}
		}
		if (pushDir == null)
		{
			return false;
		}
		dt = Math.Min(dt, 0.1f);
		float rndx = ((float)entity.World.Rand.NextDouble() - 0.5f) / 600f;
		float rndz = ((float)entity.World.Rand.NextDouble() - 0.5f) / 600f;
		entity.SidedPos.X += (float)pushDir.X * dt * 1.5f;
		entity.SidedPos.Z += (float)pushDir.Z * dt * 1.5f;
		entity.SidedPos.Motion.X = pushVelocityMul * (float)pushDir.X * dt + rndx;
		entity.SidedPos.Motion.Z = pushVelocityMul * (float)pushDir.Z * dt + rndz;
		return true;
	}
}
