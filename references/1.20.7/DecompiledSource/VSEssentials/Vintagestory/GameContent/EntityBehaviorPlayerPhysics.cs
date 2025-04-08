using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityBehaviorPlayerPhysics : EntityBehaviorControlledPhysics, IRenderer, IDisposable, IRemotePhysics
{
	private IPlayer player;

	private IServerPlayer serverPlayer;

	private EntityPlayer entityPlayer;

	private const float interval = 1f / 60f;

	private float accum;

	private int currentTick;

	private int prevDimension;

	public const float ClippingToleranceOnDimensionChange = 0.0625f;

	public double RenderOrder => 1.0;

	public int RenderRange => 9999;

	public EntityBehaviorPlayerPhysics(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		entityPlayer = entity as EntityPlayer;
		Init();
		SetProperties(properties, attributes);
		if (entity.Api.Side == EnumAppSide.Client)
		{
			smoothStepping = true;
			capi.Event.RegisterRenderer(this, EnumRenderStage.Before, "playerphysics");
		}
		else
		{
			EnumHandling handling = EnumHandling.Handled;
			OnReceivedServerPos(isTeleport: true, ref handling);
		}
		entity.PhysicsUpdateWatcher?.Invoke(0f, entity.SidedPos.XYZ);
	}

	public override void SetModules()
	{
		physicsModules.Add(new PModuleWind());
		physicsModules.Add(new PModuleOnGround());
		physicsModules.Add(new PModulePlayerInLiquid(entityPlayer));
		physicsModules.Add(new PModulePlayerInAir());
		physicsModules.Add(new PModuleGravity());
		physicsModules.Add(new PModuleMotionDrag());
		physicsModules.Add(new PModuleKnockback());
	}

	public override void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
	{
	}

	public new void OnReceivedClientPos(int version)
	{
		if (serverPlayer == null)
		{
			serverPlayer = entityPlayer.Player as IServerPlayer;
		}
		entity.ServerPos.SetFrom(entity.Pos);
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

	public new void HandleRemotePhysics(float dt, bool isTeleport)
	{
		if (player == null)
		{
			player = entityPlayer.Player;
		}
		if (player == null)
		{
			return;
		}
		if (nPos == null)
		{
			nPos = new Vec3d();
			nPos.Set(entity.ServerPos);
		}
		float dtFactor = dt * 60f;
		lPos.SetFrom(nPos);
		nPos.Set(entity.ServerPos);
		lPos.Dimension = entity.Pos.Dimension;
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
		EntityAgent eagent = entity as EntityAgent;
		if (eagent.MountedOn != null)
		{
			entity.Swimming = false;
			entity.OnGround = false;
			if (capi != null)
			{
				entity.Pos.SetPos(eagent.MountedOn.SeatPosition);
			}
			entity.ServerPos.Motion.X = 0.0;
			entity.ServerPos.Motion.Y = 0.0;
			entity.ServerPos.Motion.Z = 0.0;
			if (sapi != null)
			{
				PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, lPos, dtFactor, ref newPos, 0f, 0f);
			}
			return;
		}
		entity.Pos.SetFrom(entity.ServerPos);
		SetState(lPos, dt);
		EntityControls controls = eagent.Controls;
		if (!controls.NoClip)
		{
			if (sapi != null)
			{
				PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, lPos, dtFactor, ref newPos, 0f, 0f);
			}
			RemoteMotionAndCollision(lPos, dtFactor);
			ApplyTests(lPos, eagent.Controls, dt, remote: true);
		}
		else
		{
			EntityPos pos = entity.ServerPos;
			pos.X += pos.Motion.X * (double)dt * 60.0;
			pos.Y += pos.Motion.Y * (double)dt * 60.0;
			pos.Z += pos.Motion.Z * (double)dt * 60.0;
			entity.Swimming = false;
			entity.FeetInLiquid = false;
			entity.OnGround = false;
			controls.Gliding = false;
		}
	}

	public override void OnPhysicsTick(float dt)
	{
		SimPhysics(dt, entity.SidedPos);
	}

	public override void OnGameTick(float deltaTime)
	{
		base.OnGameTick(deltaTime);
		if (entity.World.Side == EnumAppSide.Server)
		{
			callOnEntityInside();
		}
	}

	public void SimPhysics(float dt, EntityPos pos)
	{
		if (entity.State != 0)
		{
			return;
		}
		if (player == null)
		{
			player = entityPlayer.Player;
		}
		if (player == null)
		{
			return;
		}
		EntityAgent eagent = entity as EntityAgent;
		EntityControls controls = eagent.Controls;
		prevPos.Set(pos);
		tmpPos.dimension = pos.Dimension;
		SetState(pos, dt);
		SetPlayerControls(pos, controls, dt);
		if (eagent.MountedOn != null)
		{
			entity.Swimming = false;
			entity.OnGround = false;
			pos.SetPos(eagent.MountedOn.SeatPosition);
			pos.Motion.X = 0.0;
			pos.Motion.Y = 0.0;
			pos.Motion.Z = 0.0;
			return;
		}
		MotionAndCollision(pos, controls, dt);
		if (!controls.NoClip)
		{
			PhysicsBehaviorBase.collisionTester.NewTick(pos);
			if (prevDimension != pos.Dimension)
			{
				prevDimension = pos.Dimension;
				PhysicsBehaviorBase.collisionTester.PushOutFromBlocks(entity.World.BlockAccessor, entity, pos.XYZ, 0.075f);
			}
			ApplyTests(pos, controls, dt, remote: false);
			if (controls.Gliding)
			{
				if (entity.Collided || entity.FeetInLiquid || !entity.Alive || player.WorldData.FreeMove)
				{
					controls.GlideSpeed = 0.0;
					controls.Gliding = false;
					controls.IsFlying = false;
					entityPlayer.WalkPitch = 0f;
				}
			}
			else
			{
				controls.GlideSpeed = 0.0;
			}
		}
		else
		{
			pos.X += pos.Motion.X * (double)dt * 60.0;
			pos.Y += pos.Motion.Y * (double)dt * 60.0;
			pos.Z += pos.Motion.Z * (double)dt * 60.0;
			entity.Swimming = false;
			entity.FeetInLiquid = false;
			entity.OnGround = false;
			controls.Gliding = false;
			prevDimension = pos.Dimension;
		}
	}

	public void SetPlayerControls(EntityPos pos, EntityControls controls, float dt)
	{
		IClientWorldAccessor clientWorld = entity.World as IClientWorldAccessor;
		controls.IsFlying = player.WorldData.FreeMove || (clientWorld != null && clientWorld.Player.ClientId != player.ClientId);
		controls.NoClip = player.WorldData.NoClip;
		controls.MovespeedMultiplier = player.WorldData.MoveSpeedMultiplier;
		if (controls.Gliding)
		{
			controls.IsFlying = true;
		}
		if ((controls.TriesToMove || controls.Gliding) && player is IClientPlayer clientPlayer)
		{
			float prevYaw = pos.Yaw;
			pos.Yaw = (entity.Api as ICoreClientAPI).Input.MouseYaw;
			if (entity.Swimming || controls.Gliding)
			{
				float prevPitch = pos.Pitch;
				pos.Pitch = clientPlayer.CameraPitch;
				controls.CalcMovementVectors(pos, dt);
				pos.Yaw = prevYaw;
				pos.Pitch = prevPitch;
			}
			else
			{
				controls.CalcMovementVectors(pos, dt);
				pos.Yaw = prevYaw;
			}
			float desiredYaw = (float)Math.Atan2(controls.WalkVector.X, controls.WalkVector.Z);
			float yawDist = GameMath.AngleRadDistance(entityPlayer.WalkYaw, desiredYaw);
			entityPlayer.WalkYaw += GameMath.Clamp(yawDist, -6f * dt * GlobalConstants.OverallSpeedMultiplier, 6f * dt * GlobalConstants.OverallSpeedMultiplier);
			entityPlayer.WalkYaw = GameMath.Mod(entityPlayer.WalkYaw, (float)Math.PI * 2f);
			if (entity.Swimming || controls.Gliding)
			{
				float desiredPitch = 0f - (float)Math.Sin(pos.Pitch);
				float pitchDist = GameMath.AngleRadDistance(entityPlayer.WalkPitch, desiredPitch);
				entityPlayer.WalkPitch += GameMath.Clamp(pitchDist, -2f * dt * GlobalConstants.OverallSpeedMultiplier, 2f * dt * GlobalConstants.OverallSpeedMultiplier);
				entityPlayer.WalkPitch = GameMath.Mod(entityPlayer.WalkPitch, (float)Math.PI * 2f);
			}
			else
			{
				entityPlayer.WalkPitch = 0f;
			}
			return;
		}
		if (!entity.Swimming && !controls.Gliding)
		{
			entityPlayer.WalkPitch = 0f;
		}
		else if (entity.OnGround && entityPlayer.WalkPitch != 0f)
		{
			entityPlayer.WalkPitch = GameMath.Mod(entityPlayer.WalkPitch, (float)Math.PI * 2f);
			if (entityPlayer.WalkPitch < 0.01f || entityPlayer.WalkPitch > 3.1315928f)
			{
				entityPlayer.WalkPitch = 0f;
			}
			else
			{
				entityPlayer.WalkPitch -= GameMath.Clamp(entityPlayer.WalkPitch, 0f, 1.2f * dt * GlobalConstants.OverallSpeedMultiplier);
				if (entityPlayer.WalkPitch < 0f)
				{
					entityPlayer.WalkPitch = 0f;
				}
			}
		}
		float prevYaw2 = pos.Yaw;
		controls.CalcMovementVectors(pos, dt);
		pos.Yaw = prevYaw2;
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (capi.IsGamePaused)
		{
			return;
		}
		if (capi.World.Player.Entity != entity)
		{
			smoothStepping = false;
			capi.Event.UnregisterRenderer(this, EnumRenderStage.Before);
			return;
		}
		accum += dt;
		if ((double)accum > 0.5)
		{
			accum = 0f;
		}
		Entity mountedEntity = entityPlayer.MountedOn?.Entity;
		IPhysicsTickable tickable = null;
		if (entityPlayer.MountedOn?.MountSupplier.Controller == entityPlayer)
		{
			tickable = mountedEntity?.SidedProperties.Behaviors.Find((EntityBehavior b) => b is IPhysicsTickable) as IPhysicsTickable;
		}
		while (accum >= 1f / 60f)
		{
			OnPhysicsTick(1f / 60f);
			tickable?.OnPhysicsTick(1f / 60f);
			accum -= 1f / 60f;
			currentTick++;
			if (currentTick % 4 == 0 && entityPlayer.EntityId != 0L && entityPlayer.Alive)
			{
				capi.Network.SendPlayerPositionPacket();
				if (tickable != null)
				{
					capi.Network.SendPlayerMountPositionPacket(mountedEntity);
				}
			}
			AfterPhysicsTick(1f / 60f);
			tickable?.AfterPhysicsTick(1f / 60f);
		}
		entity.PhysicsUpdateWatcher?.Invoke(accum, prevPos);
		mountedEntity?.PhysicsUpdateWatcher?.Invoke(accum, prevPos);
	}

	protected override bool HandleSteppingOnBlocks(EntityPos pos, Vec3d moveDelta, float dtFac, EntityControls controls)
	{
		if (!controls.TriesToMove || (!entity.OnGround && !entity.Swimming) || entity.Properties.Habitat == EnumHabitat.Underwater)
		{
			return false;
		}
		Cuboidd entityCollisionBox = entity.CollisionBox.ToDouble();
		double searchBoxLength = 0.75 + (controls.Sprint ? 0.25 : (controls.Sneak ? 0.05 : 0.2));
		Vec2d center = new Vec2d((entityCollisionBox.X1 + entityCollisionBox.X2) / 2.0, (entityCollisionBox.Z1 + entityCollisionBox.Z2) / 2.0);
		double searchHeight = Math.Max(entityCollisionBox.Y1 + (double)StepHeight, entityCollisionBox.Y2);
		entityCollisionBox.Translate(pos.X, pos.Y, pos.Z);
		Vec3d walkVec = controls.WalkVector.Clone();
		Vec3d vec3d = walkVec.Clone().Normalize();
		double outerX = vec3d.X * searchBoxLength;
		double outerZ = vec3d.Z * searchBoxLength;
		Cuboidd entitySensorBox = new Cuboidd
		{
			X1 = Math.Min(0.0, outerX),
			X2 = Math.Max(0.0, outerX),
			Z1 = Math.Min(0.0, outerZ),
			Z2 = Math.Max(0.0, outerZ),
			Y1 = (double)entity.CollisionBox.Y1 + 0.01 - ((!entity.CollidedVertically && !controls.Jump) ? 0.05 : 0.0),
			Y2 = searchHeight
		};
		entitySensorBox.Translate(center.X, 0.0, center.Y);
		entitySensorBox.Translate(pos.X, pos.Y, pos.Z);
		Vec3d testVec = new Vec3d();
		Vec2d testMotion = new Vec2d();
		List<Cuboidd> steppableBoxes = FindSteppableCollisionboxSmooth(entityCollisionBox, entitySensorBox, moveDelta.Y, walkVec);
		if (steppableBoxes != null && steppableBoxes.Count > 0)
		{
			if (TryStepSmooth(controls, pos, testMotion.Set(walkVec.X, walkVec.Z), dtFac, steppableBoxes, entityCollisionBox))
			{
				return true;
			}
			Cuboidd entitySensorBoxXAligned = entitySensorBox.Clone();
			if (entitySensorBoxXAligned.Z1 == pos.Z + center.Y)
			{
				entitySensorBoxXAligned.Z2 = entitySensorBoxXAligned.Z1;
			}
			else
			{
				entitySensorBoxXAligned.Z1 = entitySensorBoxXAligned.Z2;
			}
			if (TryStepSmooth(controls, pos, testMotion.Set(walkVec.X, 0.0), dtFac, FindSteppableCollisionboxSmooth(entityCollisionBox, entitySensorBoxXAligned, moveDelta.Y, testVec.Set(walkVec.X, walkVec.Y, 0.0)), entityCollisionBox))
			{
				return true;
			}
			Cuboidd entitySensorBoxZAligned = entitySensorBox.Clone();
			if (entitySensorBoxZAligned.X1 == pos.X + center.X)
			{
				entitySensorBoxZAligned.X2 = entitySensorBoxZAligned.X1;
			}
			else
			{
				entitySensorBoxZAligned.X1 = entitySensorBoxZAligned.X2;
			}
			if (TryStepSmooth(controls, pos, testMotion.Set(0.0, walkVec.Z), dtFac, FindSteppableCollisionboxSmooth(entityCollisionBox, entitySensorBoxZAligned, moveDelta.Y, testVec.Set(0.0, walkVec.Y, walkVec.Z)), entityCollisionBox))
			{
				return true;
			}
		}
		return false;
	}

	public bool TryStepSmooth(EntityControls controls, EntityPos pos, Vec2d walkVec, float dtFac, List<Cuboidd> steppableBoxes, Cuboidd entityCollisionBox)
	{
		if (steppableBoxes == null || steppableBoxes.Count == 0)
		{
			return false;
		}
		double gravityOffset = 0.03;
		Vec2d vec2d = new Vec2d(walkVec.Y, 0.0 - walkVec.X).Normalize();
		double maxX = Math.Abs(vec2d.X * 0.3) + 0.001;
		double minX = 0.0 - maxX;
		double maxZ = Math.Abs(vec2d.Y * 0.3) + 0.001;
		double minZ = 0.0 - maxZ;
		Cuboidf col = new Cuboidf((float)minX, entity.CollisionBox.Y1, (float)minZ, (float)maxX, entity.CollisionBox.Y2, (float)maxZ);
		double newYPos = pos.Y;
		bool foundStep = false;
		foreach (Cuboidd steppableBox in steppableBoxes)
		{
			double heightDiff = steppableBox.Y2 - entityCollisionBox.Y1 + gravityOffset;
			Vec3d stepPos = new Vec3d(GameMath.Clamp(newPos.X, steppableBox.MinX, steppableBox.MaxX), newPos.Y + heightDiff + (double)pos.DimensionYAdjustment, GameMath.Clamp(newPos.Z, steppableBox.MinZ, steppableBox.MaxZ));
			if (!PhysicsBehaviorBase.collisionTester.IsColliding(entity.World.BlockAccessor, col, stepPos, alsoCheckTouch: false))
			{
				double elevateFactor = (controls.Sprint ? 0.1 : (controls.Sneak ? 0.025 : 0.05));
				newYPos = (steppableBox.IntersectsOrTouches(entityCollisionBox) ? Math.Max(newYPos, pos.Y + elevateFactor * (double)dtFac) : Math.Max(newYPos, Math.Min(pos.Y + elevateFactor * (double)dtFac, steppableBox.Y2 - (double)entity.CollisionBox.Y1 + gravityOffset)));
				foundStep = true;
			}
		}
		if (foundStep)
		{
			pos.Y = newYPos;
			PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFac, ref newPos);
		}
		return foundStep;
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		capi?.Event.UnregisterRenderer(this, EnumRenderStage.Before);
	}

	public void Dispose()
	{
	}
}
