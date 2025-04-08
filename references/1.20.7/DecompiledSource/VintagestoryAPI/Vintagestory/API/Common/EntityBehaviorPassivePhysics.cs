using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public class EntityBehaviorPassivePhysics : PhysicsBehaviorBase, IPhysicsTickable, IRemotePhysics
{
	private readonly Vec3d prevPos = new Vec3d();

	private double motionBeforeY;

	private bool feetInLiquidBefore;

	private bool onGroundBefore;

	private bool swimmingBefore;

	private bool collidedBefore;

	protected Vec3d newPos = new Vec3d();

	/// <summary>
	/// The amount of drag while travelling through water.
	/// </summary>
	private double waterDragValue = GlobalConstants.WaterDrag;

	/// <summary>
	/// The amount of drag while travelling through the air.
	/// </summary>
	private double airDragValue = GlobalConstants.AirDragAlways;

	/// <summary>
	/// The amount of drag while travelling on the ground.
	/// </summary>
	private double groundDragValue = 0.699999988079071;

	/// <summary>
	/// The amount of gravity applied per tick to this entity.
	/// </summary>
	private double gravityPerSecond = GlobalConstants.GravityPerSecond;

	/// <summary>
	/// If set, will test for entity collision every tick (expensive)
	/// </summary>
	public Action<float> OnPhysicsTickCallback;

	public bool Ticking { get; set; } = true;


	public EntityBehaviorPassivePhysics(Entity entity)
		: base(entity)
	{
	}

	public void SetState(EntityPos pos)
	{
		prevPos.Set(pos);
		motionBeforeY = pos.Motion.Y;
		onGroundBefore = entity.OnGround;
		feetInLiquidBefore = entity.FeetInLiquid;
		swimmingBefore = entity.Swimming;
		collidedBefore = entity.Collided;
	}

	public virtual void SetProperties(JsonObject attributes)
	{
		waterDragValue = 1.0 - (1.0 - waterDragValue) * attributes["waterDragFactor"].AsDouble(1.0);
		JsonObject airDragFactor = attributes["airDragFactor"];
		double airDrag = (airDragFactor.Exists ? airDragFactor.AsDouble(1.0) : attributes["airDragFallingFactor"].AsDouble(1.0));
		airDragValue = 1.0 - (1.0 - airDragValue) * airDrag;
		if (entity.WatchedAttributes.HasAttribute("airDragFactor"))
		{
			airDragValue = 1f - (1f - GlobalConstants.AirDragAlways) * (float)entity.WatchedAttributes.GetDouble("airDragFactor");
		}
		groundDragValue = 0.3 * attributes["groundDragFactor"].AsDouble(1.0);
		gravityPerSecond *= attributes["gravityFactor"].AsDouble(1.0);
		if (entity.WatchedAttributes.HasAttribute("gravityFactor"))
		{
			gravityPerSecond = GlobalConstants.GravityPerSecond * (float)entity.WatchedAttributes.GetDouble("gravityFactor");
		}
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		Init();
		SetProperties(attributes);
		if (entity.Api is ICoreServerAPI esapi)
		{
			esapi.Server.AddPhysicsTickable(this);
			return;
		}
		EnumHandling handling = EnumHandling.Handled;
		OnReceivedServerPos(isTeleport: true, ref handling);
	}

	public override void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
	{
	}

	public void OnReceivedClientPos(int version)
	{
		if (version > previousVersion)
		{
			previousVersion = version;
			HandleRemotePhysics(1f / 15f, isTeleport: true);
		}
		else
		{
			HandleRemotePhysics(1f / 15f, isTeleport: false);
		}
	}

	public void HandleRemotePhysics(float dt, bool isTeleport)
	{
		if (nPos == null)
		{
			nPos = new Vec3d();
			nPos.Set(entity.ServerPos);
		}
		float dtFactor = dt * 60f;
		lPos.SetFrom(nPos);
		nPos.Set(entity.ServerPos);
		if (isTeleport)
		{
			lPos.SetFrom(nPos);
		}
		lPos.Motion.X = (nPos.X - lPos.X) / (double)dtFactor;
		lPos.Motion.Y = (nPos.Y - lPos.Y) / (double)dtFactor;
		lPos.Motion.Z = (nPos.Z - lPos.Z) / (double)dtFactor;
		if (lPos.Motion.Length() > 20.0)
		{
			lPos.Motion.Set(0.0, 0.0, 0.0);
		}
		entity.Pos.Motion.Set(lPos.Motion);
		entity.ServerPos.Motion.Set(lPos.Motion);
		PhysicsBehaviorBase.collisionTester.NewTick(lPos);
		entity.Pos.SetFrom(entity.ServerPos);
		SetState(lPos);
		RemoteMotionAndCollision(lPos, dtFactor);
		ApplyTests(lPos);
	}

	public void RemoteMotionAndCollision(EntityPos pos, float dtFactor)
	{
		double gravityStrength = gravityPerSecond / 60.0 * (double)dtFactor + Math.Max(0.0, -0.014999999664723873 * pos.Motion.Y * (double)dtFactor);
		pos.Motion.Y -= gravityStrength;
		PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFactor, ref newPos, 0f, CollisionYExtra);
		bool falling = pos.Motion.Y < 0.0;
		entity.OnGround = entity.CollidedVertically && falling;
		pos.Motion.Y += gravityStrength;
		pos.SetPos(nPos);
	}

	public void MotionAndCollision(EntityPos pos, float dt)
	{
		float dtFactor = 60f * dt;
		if (onGroundBefore && !feetInLiquidBefore)
		{
			Block belowBlock = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y - 0.05000000074505806), (int)pos.Z, 1);
			pos.Motion.X *= 1.0 - groundDragValue * (double)belowBlock.DragMultiplier;
			pos.Motion.Z *= 1.0 - groundDragValue * (double)belowBlock.DragMultiplier;
		}
		Block insideFluid = null;
		if (feetInLiquidBefore || swimmingBefore)
		{
			pos.Motion *= Math.Pow(waterDragValue, dt * 33f);
			insideFluid = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z, 2);
			if (feetInLiquidBefore)
			{
				Vec3d pushVector = insideFluid.PushVector;
				if (pushVector != null)
				{
					float pushStrength = 300f / GameMath.Clamp(entity.MaterialDensity, 750f, 2500f) * dtFactor;
					pos.Motion.Add(pushVector.X * (double)pushStrength, pushVector.Y * (double)pushStrength, pushVector.Z * (double)pushStrength);
				}
			}
		}
		else
		{
			pos.Motion *= (float)Math.Pow(airDragValue, dt * 33f);
		}
		if (entity.ApplyGravity)
		{
			double gravityStrength = gravityPerSecond / 60.0 * (double)dtFactor + Math.Max(0.0, -0.014999999664723873 * pos.Motion.Y * (double)dtFactor);
			if (entity.Swimming)
			{
				float boyancy = GameMath.Clamp(1f - entity.MaterialDensity / (float)insideFluid.MaterialDensity, -1f, 1f);
				Block aboveFluid = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y + 1.0), (int)pos.Z, 2);
				float swimLineSubmergedness = GameMath.Clamp((float)(int)pos.Y + (float)insideFluid.LiquidLevel / 8f + (aboveFluid.IsLiquid() ? 1.125f : 0f) - (float)pos.Y - (entity.SelectionBox.Y2 - (float)entity.SwimmingOffsetY), 0f, 1f);
				double boyancyStrength = GameMath.Clamp(60f * boyancy * swimLineSubmergedness, -1.5f, 1.5f) - 1f;
				double waterDrag = GameMath.Clamp(100.0 * Math.Abs(pos.Motion.Y * (double)dtFactor) - 0.019999999552965164, 1.0, 1.25);
				pos.Motion.Y += gravityStrength * boyancyStrength;
				pos.Motion.Y /= waterDrag;
			}
			else
			{
				pos.Motion.Y -= gravityStrength;
			}
		}
		double nextX = pos.Motion.X * (double)dtFactor + pos.X;
		double nextY = pos.Motion.Y * (double)dtFactor + pos.Y;
		double nextZ = pos.Motion.Z * (double)dtFactor + pos.Z;
		applyCollision(pos, dtFactor);
		if (entity.World.BlockAccessor.IsNotTraversable((int)nextX, (int)pos.Y, (int)pos.Z))
		{
			newPos.X = pos.X;
		}
		if (entity.World.BlockAccessor.IsNotTraversable((int)pos.X, (int)nextY, (int)pos.Z))
		{
			newPos.Y = pos.Y;
		}
		if (entity.World.BlockAccessor.IsNotTraversable((int)pos.X, (int)pos.Y, (int)nextZ))
		{
			newPos.Z = pos.Z;
		}
		pos.SetPos(newPos);
		if ((nextX < newPos.X && pos.Motion.X < 0.0) || (nextX > newPos.X && pos.Motion.X > 0.0))
		{
			pos.Motion.X = 0.0;
		}
		if ((nextY < newPos.Y && pos.Motion.Y < 0.0) || (nextY > newPos.Y && pos.Motion.Y > 0.0))
		{
			pos.Motion.Y = 0.0;
		}
		if ((nextZ < newPos.Z && pos.Motion.Z < 0.0) || (nextZ > newPos.Z && pos.Motion.Z > 0.0))
		{
			pos.Motion.Z = 0.0;
		}
	}

	protected virtual void applyCollision(EntityPos pos, float dtFactor)
	{
		PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFactor, ref newPos, 0f, CollisionYExtra);
	}

	public void ApplyTests(EntityPos pos)
	{
		bool falling = pos.Motion.Y <= 0.0;
		entity.OnGround = entity.CollidedVertically && falling;
		Block fluidBlock = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z, 2);
		entity.FeetInLiquid = fluidBlock.MatterState == EnumMatterState.Liquid;
		entity.InLava = fluidBlock.LiquidCode == "lava";
		if (entity.FeetInLiquid)
		{
			Block aboveBlockFluid = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y + 1.0), (int)pos.Z, 2);
			float swimlineSubmergedness = (float)(int)pos.Y + (float)fluidBlock.LiquidLevel / 8f + (aboveBlockFluid.IsLiquid() ? 1.125f : 0f) - (float)pos.Y - (entity.SelectionBox.Y2 - (float)entity.SwimmingOffsetY);
			entity.Swimming = swimlineSubmergedness > 0f;
		}
		else
		{
			entity.Swimming = false;
		}
		if (!onGroundBefore && entity.OnGround)
		{
			entity.OnFallToGround(motionBeforeY);
		}
		if (!feetInLiquidBefore && entity.FeetInLiquid)
		{
			entity.OnCollideWithLiquid();
		}
		if ((swimmingBefore || feetInLiquidBefore) && !entity.Swimming && !entity.FeetInLiquid)
		{
			entity.OnExitedLiquid();
		}
		if (!collidedBefore && entity.Collided)
		{
			entity.OnCollided();
		}
		if (entity.OnGround)
		{
			entity.PositionBeforeFalling.Set(newPos);
		}
		if (GlobalConstants.OutsideWorld(pos.X, pos.Y, pos.Z, entity.World.BlockAccessor))
		{
			entity.DespawnReason = new EntityDespawnData
			{
				Reason = EnumDespawnReason.Death,
				DamageSourceForDeath = new DamageSource
				{
					Source = EnumDamageSource.Fall
				}
			};
			return;
		}
		Cuboidd entityBox = PhysicsBehaviorBase.collisionTester.entityBox;
		int xMax = (int)entityBox.X2;
		int yMax = (int)entityBox.Y2;
		int zMax = (int)entityBox.Z2;
		int zMin = (int)entityBox.Z1;
		for (int y = (int)entityBox.Y1; y <= yMax; y++)
		{
			for (int x = (int)entityBox.X1; x <= xMax; x++)
			{
				for (int z = zMin; z <= zMax; z++)
				{
					PhysicsBehaviorBase.collisionTester.tmpPos.Set(x, y, z);
					entity.World.BlockAccessor.GetBlock(x, y, z).OnEntityInside(entity.World, entity, PhysicsBehaviorBase.collisionTester.tmpPos);
				}
			}
		}
		OnPhysicsTickCallback?.Invoke(0f);
		entity.PhysicsUpdateWatcher?.Invoke(0f, prevPos);
	}

	public void OnPhysicsTick(float dt)
	{
		if (entity.State != 0 || !Ticking)
		{
			return;
		}
		IMountable mountable = mountableSupplier;
		if (mountable == null || !mountable.IsBeingControlled() || entity.World.Side != EnumAppSide.Server)
		{
			EntityPos pos = entity.SidedPos;
			PhysicsBehaviorBase.collisionTester.AssignToEntity(this, pos.Dimension);
			int loops = ((!(pos.Motion.Length() > 0.1)) ? 1 : 10);
			float newDt = dt / (float)loops;
			for (int i = 0; i < loops; i++)
			{
				SetState(pos);
				MotionAndCollision(pos, newDt);
				ApplyTests(pos);
			}
			entity.Pos.SetFrom(pos);
		}
	}

	public void AfterPhysicsTick(float dt)
	{
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		if (sapi != null)
		{
			sapi.Server.RemovePhysicsTickable(this);
		}
	}

	public override string PropertyName()
	{
		return "entitypassivephysics";
	}
}
