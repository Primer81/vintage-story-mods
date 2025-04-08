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

public class EntityBehaviorControlledPhysics : PhysicsBehaviorBase, IPhysicsTickable, IRemotePhysics
{
	protected const double collisionboxReductionForInsideBlocksCheck = 0.009;

	protected bool smoothStepping;

	protected readonly List<PModule> physicsModules = new List<PModule>();

	protected readonly List<PModule> customModules = new List<PModule>();

	protected Vec3d newPos = new Vec3d();

	protected readonly Vec3d prevPos = new Vec3d();

	protected readonly BlockPos tmpPos = new BlockPos();

	protected readonly Cuboidd entityBox = new Cuboidd();

	protected readonly List<FastVec3i> traversed = new List<FastVec3i>(4);

	protected readonly IComparer<FastVec3i> fastVec3IComparer = new FastVec3iComparer();

	protected readonly Vec3d moveDelta = new Vec3d();

	protected double prevYMotion;

	protected bool onGroundBefore;

	protected bool feetInLiquidBefore;

	protected bool swimmingBefore;

	protected float knockBackCounter;

	protected Cuboidf sneakTestCollisionbox = new Cuboidf();

	protected readonly Cuboidd steppingCollisionBox = new Cuboidd();

	protected readonly Vec3d steppingTestVec = new Vec3d();

	protected readonly Vec3d steppingTestMotion = new Vec3d();

	public Matrixf tmpModelMat = new Matrixf();

	public float StepHeight = 0.6f;

	public bool allowUnloadedTraverse;

	public float stepUpSpeed = 0.07f;

	public float climbUpSpeed = 0.07f;

	public float climbDownSpeed = 0.035f;

	private IMountable im;

	public bool Ticking { get; set; }

	public void SetState(EntityPos pos, float dt)
	{
		float dtFactor = dt * 60f;
		prevPos.Set(pos);
		prevYMotion = pos.Motion.Y;
		onGroundBefore = entity.OnGround;
		feetInLiquidBefore = entity.FeetInLiquid;
		swimmingBefore = entity.Swimming;
		traversed.Clear();
		if (entity.AdjustCollisionBoxToAnimation)
		{
			AdjustCollisionBoxToAnimation(dtFactor);
		}
	}

	public EntityBehaviorControlledPhysics(Entity entity)
		: base(entity)
	{
	}

	public virtual void SetModules()
	{
		physicsModules.Add(new PModuleWind());
		physicsModules.Add(new PModuleOnGround());
		physicsModules.Add(new PModuleInLiquid());
		physicsModules.Add(new PModuleInAir());
		physicsModules.Add(new PModuleGravity());
		physicsModules.Add(new PModuleMotionDrag());
		physicsModules.Add(new PModuleKnockback());
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		Init();
		SetProperties(properties, attributes);
		if (entity.Api is ICoreServerAPI esapi)
		{
			esapi.Server.AddPhysicsTickable(this);
		}
		entity.PhysicsUpdateWatcher?.Invoke(0f, entity.SidedPos.XYZ);
		if (entity.Api.Side != EnumAppSide.Client)
		{
			return;
		}
		EnumHandling handling = EnumHandling.Handled;
		OnReceivedServerPos(isTeleport: true, ref handling);
		entity.Attributes.RegisterModifiedListener("dmgkb", delegate
		{
			if (entity.Attributes.GetInt("dmgkb") == 1)
			{
				knockBackCounter = 2f;
			}
		});
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		im = entity.GetInterface<IMountable>();
	}

	public override void OnGameTick(float deltaTime)
	{
		base.OnGameTick(deltaTime);
		if (im != null && entity.World.Side == EnumAppSide.Server && im.AnyMounted())
		{
			callOnEntityInside();
		}
	}

	public void SetProperties(EntityProperties properties, JsonObject attributes)
	{
		StepHeight = attributes["stepHeight"].AsFloat(0.6f);
		stepUpSpeed = attributes["stepUpSpeed"].AsFloat(0.07f);
		climbUpSpeed = attributes["climbUpSpeed"].AsFloat(0.07f);
		climbDownSpeed = attributes["climbDownSpeed"].AsFloat(0.035f);
		allowUnloadedTraverse = attributes["allowUnloadedTraverse"].AsBool();
		sneakTestCollisionbox = entity.CollisionBox.Clone().OmniNotDownGrowBy(-0.1f);
		sneakTestCollisionbox.Y2 /= 2f;
		SetModules();
		JsonObject physics = properties?.Attributes?["physics"];
		for (int i = 0; i < physicsModules.Count; i++)
		{
			physicsModules[i].Initialize(physics, entity);
		}
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
		lPos.Dimension = entity.Pos.Dimension;
		tmpPos.dimension = lPos.Dimension;
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
		EntityAgent agent = entity as EntityAgent;
		if (agent?.MountedOn != null)
		{
			entity.Swimming = false;
			entity.OnGround = false;
			if (capi != null)
			{
				entity.Pos.SetPos(agent.MountedOn.SeatPosition);
			}
			entity.ServerPos.Motion.X = 0.0;
			entity.ServerPos.Motion.Y = 0.0;
			entity.ServerPos.Motion.Z = 0.0;
		}
		else
		{
			entity.Pos.SetFrom(entity.ServerPos);
			SetState(lPos, dt);
			RemoteMotionAndCollision(lPos, dtFactor);
			ApplyTests(lPos, ((EntityAgent)entity).Controls, dt, remote: true);
			if (knockBackCounter > 0f)
			{
				knockBackCounter -= dt;
				return;
			}
			knockBackCounter = 0f;
			entity.Attributes.SetInt("dmgkb", 0);
		}
	}

	public void RemoteMotionAndCollision(EntityPos pos, float dtFactor)
	{
		double gravityStrength = (double)(1f / 60f * dtFactor) + Math.Max(0.0, -0.014999999664723873 * pos.Motion.Y * (double)dtFactor);
		pos.Motion.Y -= gravityStrength;
		PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFactor, ref newPos, 0f, 0f);
		bool falling = lPos.Motion.Y < 0.0;
		entity.OnGround = entity.CollidedVertically && falling;
		pos.Motion.Y += gravityStrength;
		pos.SetPos(nPos);
	}

	public void MotionAndCollision(EntityPos pos, EntityControls controls, float dt)
	{
		foreach (PModule physicsModule2 in physicsModules)
		{
			if (physicsModule2.Applicable(entity, pos, controls))
			{
				physicsModule2.DoApply(dt, entity, pos, controls);
			}
		}
		foreach (PModule physicsModule in customModules)
		{
			if (physicsModule.Applicable(entity, pos, controls))
			{
				physicsModule.DoApply(dt, entity, pos, controls);
			}
		}
	}

	public void ApplyTests(EntityPos pos, EntityControls controls, float dt, bool remote)
	{
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		float dtFactor = dt * 60f;
		controls.IsClimbing = false;
		entity.ClimbingOnFace = null;
		entity.ClimbingIntoFace = null;
		if (entity.Properties.CanClimb)
		{
			int height = (int)Math.Ceiling(entity.CollisionBox.Y2);
			entityBox.SetAndTranslate(entity.CollisionBox, pos.X, pos.Y, pos.Z);
			for (int dy2 = 0; dy2 < height; dy2++)
			{
				tmpPos.Set((int)pos.X, (int)pos.Y + dy2, (int)pos.Z);
				Block inBlock2 = blockAccessor.GetBlock(tmpPos);
				if (!inBlock2.IsClimbable(tmpPos) && !entity.Properties.CanClimbAnywhere)
				{
					continue;
				}
				Cuboidf[] collisionBoxes3 = inBlock2.GetCollisionBoxes(blockAccessor, tmpPos);
				if (collisionBoxes3 == null)
				{
					continue;
				}
				for (int j = 0; j < collisionBoxes3.Length; j++)
				{
					double distance2 = entityBox.ShortestDistanceFrom(collisionBoxes3[j], tmpPos);
					controls.IsClimbing |= distance2 < (double)entity.Properties.ClimbTouchDistance;
					if (controls.IsClimbing)
					{
						entity.ClimbingOnFace = null;
						break;
					}
				}
			}
			if (controls.WalkVector.LengthSq() > 1E-05 && entity.Properties.CanClimbAnywhere && entity.Alive)
			{
				BlockFacing walkIntoFace = BlockFacing.FromVector(controls.WalkVector.X, controls.WalkVector.Y, controls.WalkVector.Z);
				if (walkIntoFace != null)
				{
					tmpPos.Set((int)pos.X + walkIntoFace.Normali.X, (int)pos.Y + walkIntoFace.Normali.Y, (int)pos.Z + walkIntoFace.Normali.Z);
					Cuboidf[] collisionBoxes2 = blockAccessor.GetBlock(tmpPos).GetCollisionBoxes(blockAccessor, tmpPos);
					entity.ClimbingIntoFace = ((collisionBoxes2 != null && collisionBoxes2.Length != 0) ? walkIntoFace : null);
				}
			}
			int i = 0;
			while (!controls.IsClimbing && i < BlockFacing.HORIZONTALS.Length)
			{
				BlockFacing facing = BlockFacing.HORIZONTALS[i];
				for (int dy = 0; dy < height; dy++)
				{
					tmpPos.Set((int)pos.X + facing.Normali.X, (int)pos.Y + dy, (int)pos.Z + facing.Normali.Z);
					Block inBlock = blockAccessor.GetBlock(tmpPos);
					if (!inBlock.IsClimbable(tmpPos) && (!entity.Properties.CanClimbAnywhere || !entity.Alive))
					{
						continue;
					}
					Cuboidf[] collisionBoxes = inBlock.GetCollisionBoxes(blockAccessor, tmpPos);
					if (collisionBoxes == null)
					{
						continue;
					}
					for (int k = 0; k < collisionBoxes.Length; k++)
					{
						double distance = entityBox.ShortestDistanceFrom(collisionBoxes[k], tmpPos);
						controls.IsClimbing |= distance < (double)entity.Properties.ClimbTouchDistance;
						if (controls.IsClimbing)
						{
							entity.ClimbingOnFace = facing;
							entity.ClimbingOnCollBox = collisionBoxes[k];
							break;
						}
					}
				}
				i++;
			}
		}
		if (!remote)
		{
			if (controls.IsClimbing && controls.WalkVector.Y == 0.0)
			{
				pos.Motion.Y = (controls.Sneak ? Math.Max(0f - climbUpSpeed, pos.Motion.Y - (double)climbUpSpeed) : pos.Motion.Y);
				if (controls.Jump)
				{
					pos.Motion.Y = climbDownSpeed * dt * 60f;
				}
			}
			double nextX = pos.Motion.X * (double)dtFactor + pos.X;
			double nextY = pos.Motion.Y * (double)dtFactor + pos.Y;
			double nextZ = pos.Motion.Z * (double)dtFactor + pos.Z;
			moveDelta.Set(pos.Motion.X * (double)dtFactor, prevYMotion * (double)dtFactor, pos.Motion.Z * (double)dtFactor);
			PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFactor, ref newPos, 0f, CollisionYExtra);
			if (!entity.Properties.CanClimbAnywhere)
			{
				controls.IsStepping = HandleSteppingOnBlocks(pos, moveDelta, dtFactor, controls);
			}
			HandleSneaking(pos, controls, dt);
			if (entity.CollidedHorizontally && !controls.IsClimbing && !controls.IsStepping && entity.Properties.Habitat != EnumHabitat.Underwater)
			{
				if (blockAccessor.GetBlockRaw((int)pos.X, (int)(pos.InternalY + 0.5), (int)pos.Z).LiquidLevel >= 7 || blockAccessor.GetBlockRaw((int)pos.X, (int)pos.InternalY, (int)pos.Z).LiquidLevel >= 7 || blockAccessor.GetBlockRaw((int)pos.X, (int)(pos.InternalY - 0.05), (int)pos.Z).LiquidLevel >= 7)
				{
					pos.Motion.Y += 0.2 * (double)dt;
					controls.IsStepping = true;
				}
				else
				{
					double absX = Math.Abs(pos.Motion.X);
					double absZ = Math.Abs(pos.Motion.Z);
					if (absX > absZ)
					{
						if (absZ < 0.001)
						{
							pos.Motion.Z += ((pos.Motion.Z < 0.0) ? (-0.0025) : 0.0025);
						}
					}
					else if (absX < 0.001)
					{
						pos.Motion.X += ((pos.Motion.X < 0.0) ? (-0.0025) : 0.0025);
					}
				}
			}
			if (!allowUnloadedTraverse)
			{
				if (entity.World.BlockAccessor.IsNotTraversable((int)nextX, (int)pos.Y, (int)pos.Z, pos.Dimension))
				{
					newPos.X = pos.X;
				}
				if (entity.World.BlockAccessor.IsNotTraversable((int)pos.X, (int)nextY, (int)pos.Z, pos.Dimension))
				{
					newPos.Y = pos.Y;
				}
				if (entity.World.BlockAccessor.IsNotTraversable((int)pos.X, (int)pos.Y, (int)nextZ, pos.Dimension))
				{
					newPos.Z = pos.Z;
				}
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
		bool falling = prevYMotion <= 0.0;
		entity.OnGround = entity.CollidedVertically && falling;
		float offX = entity.CollisionBox.X2 - entity.OriginCollisionBox.X2;
		float offZ = entity.CollisionBox.Z2 - entity.OriginCollisionBox.Z2;
		int posX = (int)(pos.X + (double)offX);
		int posY = (int)pos.InternalY;
		int posZ = (int)(pos.Z + (double)offZ);
		int swimmingY = (int)(pos.InternalY + entity.SwimmingOffsetY);
		Block blockFluid = blockAccessor.GetBlock(posX, posY, posZ, 2);
		Block middleWOIBlock = ((swimmingY == posY) ? blockFluid : blockAccessor.GetBlock(posX, swimmingY, posZ, 2));
		entity.OnGround = (entity.CollidedVertically && falling && !controls.IsClimbing) || controls.IsStepping;
		entity.FeetInLiquid = false;
		if (blockFluid.IsLiquid())
		{
			Block aboveBlock = blockAccessor.GetBlock(posX, posY + 1, posZ, 2);
			entity.FeetInLiquid = (double)((float)(blockFluid.LiquidLevel + ((aboveBlock.LiquidLevel > 0) ? 1 : 0)) / 8f) >= pos.Y - (double)(int)pos.Y;
		}
		entity.InLava = blockFluid.LiquidCode == "lava";
		entity.Swimming = middleWOIBlock.IsLiquid();
		if (!onGroundBefore && entity.OnGround)
		{
			entity.OnFallToGround(prevYMotion);
		}
		if (!feetInLiquidBefore && entity.FeetInLiquid)
		{
			entity.OnCollideWithLiquid();
		}
		if ((swimmingBefore || feetInLiquidBefore) && !entity.Swimming && !entity.FeetInLiquid)
		{
			entity.OnExitedLiquid();
		}
		if (!falling || entity.OnGround || controls.IsClimbing)
		{
			entity.PositionBeforeFalling.Set(pos);
		}
		Cuboidd cuboidd = PhysicsBehaviorBase.collisionTester.entityBox;
		int xMax = (int)(cuboidd.X2 - 0.009);
		int yMax = (int)(cuboidd.Y2 - 0.009);
		int zMax = (int)(cuboidd.Z2 - 0.009);
		int xMin = (int)(cuboidd.X1 + 0.009);
		int zMin = (int)(cuboidd.Z1 + 0.009);
		for (int y = (int)(cuboidd.Y1 + 0.009); y <= yMax; y++)
		{
			for (int x = xMin; x <= xMax; x++)
			{
				for (int z = zMin; z <= zMax; z++)
				{
					FastVec3i posTraversed = new FastVec3i(x, y, z);
					int index = traversed.BinarySearch(posTraversed, fastVec3IComparer);
					if (index < 0)
					{
						index = ~index;
					}
					traversed.Insert(index, posTraversed);
				}
			}
		}
		entity.PhysicsUpdateWatcher?.Invoke(0f, prevPos);
	}

	public virtual void OnPhysicsTick(float dt)
	{
		if (entity.State != 0)
		{
			return;
		}
		EntityPos pos = entity.SidedPos;
		PhysicsBehaviorBase.collisionTester.AssignToEntity(this, pos.Dimension);
		EntityControls controls = ((EntityAgent)entity).Controls;
		EntityAgent agent = entity as EntityAgent;
		if (agent?.MountedOn != null)
		{
			entity.Swimming = false;
			entity.OnGround = false;
			_ = agent.MountedOn.SeatPosition;
			if (!(agent is EntityPlayer))
			{
				pos.SetFrom(agent.MountedOn.SeatPosition);
			}
			else
			{
				pos.SetPos(agent.MountedOn.SeatPosition);
			}
			pos.Motion.X = 0.0;
			pos.Motion.Y = 0.0;
			pos.Motion.Z = 0.0;
		}
		else
		{
			SetState(pos, dt);
			MotionAndCollision(pos, controls, dt);
			ApplyTests(pos, controls, dt, remote: false);
			if (entity.World.Side == EnumAppSide.Server)
			{
				entity.Pos.SetFrom(entity.ServerPos);
			}
		}
	}

	public virtual void AfterPhysicsTick(float dt)
	{
		if (entity.State != 0 || (mountableSupplier != null && capi == null && mountableSupplier.IsBeingControlled()))
		{
			return;
		}
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		tmpPos.Set(-1, -1, -1);
		Block block = null;
		foreach (FastVec3i pos in traversed)
		{
			if (!pos.Equals(tmpPos))
			{
				tmpPos.Set(pos);
				block = blockAccessor.GetBlock(tmpPos);
			}
			if (block != null && block.Id > 0)
			{
				block.OnEntityInside(entity.World, entity, tmpPos);
			}
		}
	}

	public void HandleSneaking(EntityPos pos, EntityControls controls, float dt)
	{
		if (!controls.Sneak || !entity.OnGround || pos.Motion.Y > 0.0)
		{
			return;
		}
		Vec3d testPosition = new Vec3d();
		testPosition.Set(pos.X, pos.InternalY - (double)(GlobalConstants.GravityPerSecond * dt), pos.Z);
		if (!PhysicsBehaviorBase.collisionTester.IsColliding(entity.World.BlockAccessor, sneakTestCollisionbox, testPosition))
		{
			return;
		}
		tmpPos.Set((int)pos.X, (int)pos.Y - 1, (int)pos.Z);
		Block belowBlock = entity.World.BlockAccessor.GetBlock(tmpPos);
		testPosition.Set(newPos.X, newPos.Y - (double)(GlobalConstants.GravityPerSecond * dt) + (double)pos.DimensionYAdjustment, pos.Z);
		if (!PhysicsBehaviorBase.collisionTester.IsColliding(entity.World.BlockAccessor, sneakTestCollisionbox, testPosition))
		{
			if (belowBlock.IsClimbable(tmpPos))
			{
				newPos.X += (pos.X - newPos.X) / 10.0;
			}
			else
			{
				newPos.X = pos.X;
			}
		}
		testPosition.Set(pos.X, newPos.Y - (double)(GlobalConstants.GravityPerSecond * dt) + (double)pos.DimensionYAdjustment, newPos.Z);
		if (!PhysicsBehaviorBase.collisionTester.IsColliding(entity.World.BlockAccessor, sneakTestCollisionbox, testPosition))
		{
			if (belowBlock.IsClimbable(tmpPos))
			{
				newPos.Z += (pos.Z - newPos.Z) / 10.0;
			}
			else
			{
				newPos.Z = pos.Z;
			}
		}
	}

	protected virtual bool HandleSteppingOnBlocks(EntityPos pos, Vec3d moveDelta, float dtFac, EntityControls controls)
	{
		if (controls.WalkVector.X == 0.0 && controls.WalkVector.Z == 0.0)
		{
			return false;
		}
		if ((!entity.OnGround && !entity.Swimming) || entity.Properties.Habitat == EnumHabitat.Underwater)
		{
			return false;
		}
		steppingCollisionBox.SetAndTranslate(entity.CollisionBox, pos.X, pos.Y, pos.Z);
		steppingCollisionBox.Y2 = Math.Max(steppingCollisionBox.Y1 + (double)StepHeight, steppingCollisionBox.Y2);
		Vec3d walkVec = controls.WalkVector;
		Cuboidd steppableBox = FindSteppableCollisionBox(steppingCollisionBox, moveDelta.Y, walkVec);
		if (steppableBox != null)
		{
			Vec3d testMotion = steppingTestMotion;
			testMotion.Set(moveDelta.X, moveDelta.Y, moveDelta.Z);
			if (TryStep(pos, testMotion, dtFac, steppableBox, steppingCollisionBox))
			{
				return true;
			}
			Vec3d testVec = steppingTestVec;
			testMotion.Z = 0.0;
			if (TryStep(pos, testMotion, dtFac, FindSteppableCollisionBox(steppingCollisionBox, moveDelta.Y, testVec.Set(walkVec.X, walkVec.Y, 0.0)), steppingCollisionBox))
			{
				return true;
			}
			testMotion.Set(0.0, moveDelta.Y, moveDelta.Z);
			if (TryStep(pos, testMotion, dtFac, FindSteppableCollisionBox(steppingCollisionBox, moveDelta.Y, testVec.Set(0.0, walkVec.Y, walkVec.Z)), steppingCollisionBox))
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public bool TryStep(EntityPos pos, Vec3d moveDelta, float dtFac, Cuboidd steppableBox, Cuboidd entityCollisionBox)
	{
		if (steppableBox == null)
		{
			return false;
		}
		double heightDiff = steppableBox.Y2 - entityCollisionBox.Y1 + 0.03;
		Vec3d stepPos = newPos.OffsetCopy(moveDelta.X, heightDiff, moveDelta.Z);
		if (!PhysicsBehaviorBase.collisionTester.IsColliding(entity.World.BlockAccessor, entity.CollisionBox, stepPos, alsoCheckTouch: false))
		{
			pos.Y += stepUpSpeed * dtFac;
			PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFac, ref newPos);
			return true;
		}
		return false;
	}

	public static bool GetCollidingCollisionBox(IBlockAccessor blockAccessor, Cuboidf entityBoxRel, Vec3d pos, out CachedCuboidList blocks, BlockPos tmpPos, bool alsoCheckTouch = true)
	{
		blocks = new CachedCuboidList();
		Vec3d blockPosVec = new Vec3d();
		Cuboidd entityBox = entityBoxRel.ToDouble().Translate(pos);
		int minX = (int)((double)entityBoxRel.MinX + pos.X);
		int num = (int)((double)entityBoxRel.MinY + pos.Y - 1.0);
		int minZ = (int)((double)entityBoxRel.MinZ + pos.Z);
		int maxX = (int)Math.Ceiling((double)entityBoxRel.MaxX + pos.X);
		int maxY = (int)Math.Ceiling((double)entityBoxRel.MaxY + pos.Y);
		int maxZ = (int)Math.Ceiling((double)entityBoxRel.MaxZ + pos.Z);
		for (int y = num; y <= maxY; y++)
		{
			for (int x = minX; x <= maxX; x++)
			{
				for (int z = minZ; z <= maxZ; z++)
				{
					tmpPos.Set(x, y, z);
					Block block = blockAccessor.GetBlock(tmpPos);
					blockPosVec.Set(x, y, z);
					Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, tmpPos);
					if (collisionBoxes == null)
					{
						continue;
					}
					foreach (Cuboidf collBox in collisionBoxes)
					{
						if (collBox != null && (alsoCheckTouch ? entityBox.IntersectsOrTouches(collBox, blockPosVec) : entityBox.Intersects(collBox, blockPosVec)))
						{
							blocks.Add(collBox, x, tmpPos.InternalY, z, block);
						}
					}
				}
			}
		}
		return blocks.Count > 0;
	}

	public Cuboidd FindSteppableCollisionBox(Cuboidd entityCollisionBox, double motionY, Vec3d walkVector)
	{
		Cuboidd steppableBox = null;
		CachedCuboidListFaster blocks = PhysicsBehaviorBase.collisionTester.CollisionBoxList;
		int maxCount = blocks.Count;
		BlockPos pos = new BlockPos(entity.ServerPos.Dimension);
		for (int i = 0; i < maxCount; i++)
		{
			Block block = blocks.blocks[i];
			if (block.CollisionBoxes != null && !block.CanStep && entity.CollisionBox.Height < 5f * block.CollisionBoxes[0].Height)
			{
				continue;
			}
			pos.Set(blocks.positions[i]);
			if (!block.SideIsSolid(pos, 4))
			{
				pos.Down();
				Block blockBelow = entity.World.BlockAccessor.GetMostSolidBlock(pos);
				pos.Up();
				if (blockBelow.CollisionBoxes != null && !blockBelow.CanStep && entity.CollisionBox.Height < 5f * blockBelow.CollisionBoxes[0].Height)
				{
					continue;
				}
			}
			Cuboidd collisionBox = blocks.cuboids[i];
			EnumIntersect intersect = CollisionTester.AabbIntersect(collisionBox, entityCollisionBox, walkVector);
			if (intersect != 0)
			{
				if ((intersect == EnumIntersect.Stuck && !block.AllowStepWhenStuck) || (intersect == EnumIntersect.IntersectY && motionY > 0.0))
				{
					return null;
				}
				double heightDiff = collisionBox.Y2 - entityCollisionBox.Y1;
				if (!(heightDiff <= 0.0) && heightDiff <= (double)StepHeight && (steppableBox == null || steppableBox.Y2 < collisionBox.Y2))
				{
					steppableBox = collisionBox;
				}
			}
		}
		return steppableBox;
	}

	public List<Cuboidd> FindSteppableCollisionboxSmooth(Cuboidd entityCollisionBox, Cuboidd entitySensorBox, double motionY, Vec3d walkVector)
	{
		List<Cuboidd> steppableBoxes = new List<Cuboidd>();
		GetCollidingCollisionBox(entity.World.BlockAccessor, entitySensorBox.ToFloat(), new Vec3d(), out var blocks, tmpPos);
		for (int i = 0; i < blocks.Count; i++)
		{
			Cuboidd collisionbox = blocks.cuboids[i];
			Block block = blocks.blocks[i];
			if (!block.CanStep && block.CollisionBoxes != null && entity.CollisionBox.Height < 5f * block.CollisionBoxes[0].Height)
			{
				continue;
			}
			BlockPos pos = blocks.positions[i];
			if (!block.SideIsSolid(pos, 4))
			{
				pos.Down();
				Block blockBelow = entity.World.BlockAccessor.GetMostSolidBlock(pos);
				pos.Up();
				if (!blockBelow.CanStep && blockBelow.CollisionBoxes != null && entity.CollisionBox.Height < 5f * blockBelow.CollisionBoxes[0].Height)
				{
					continue;
				}
			}
			EnumIntersect intersect = CollisionTester.AabbIntersect(collisionbox, entityCollisionBox, walkVector);
			if ((intersect == EnumIntersect.Stuck && !block.AllowStepWhenStuck) || (intersect == EnumIntersect.IntersectY && motionY > 0.0))
			{
				return null;
			}
			double heightDiff = collisionbox.Y2 - entityCollisionBox.Y1;
			if (!(heightDiff <= (entity.CollidedVertically ? 0.0 : (-0.05))) && heightDiff <= (double)StepHeight)
			{
				steppableBoxes.Add(collisionbox);
			}
		}
		return steppableBoxes;
	}

	public void AdjustCollisionBoxToAnimation(float dtFac)
	{
		AttachmentPointAndPose apap = entity.AnimManager.Animator?.GetAttachmentPointPose("Center");
		if (apap != null)
		{
			float[] hitboxOff = new float[4] { 0f, 0f, 0f, 1f };
			AttachmentPoint ap = apap.AttachPoint;
			CompositeShape shape = entity.Properties.Client.Shape;
			float rotX = shape?.rotateX ?? 0f;
			float rotY = shape?.rotateY ?? 0f;
			float rotZ = shape?.rotateZ ?? 0f;
			float[] ModelMat = Mat4f.Create();
			Mat4f.Identity(ModelMat);
			Mat4f.Translate(ModelMat, ModelMat, 0f, entity.CollisionBox.Y2 / 2f, 0f);
			double[] quat = Quaterniond.Create();
			Quaterniond.RotateX(quat, quat, entity.SidedPos.Pitch + rotX * ((float)Math.PI / 180f));
			Quaterniond.RotateY(quat, quat, entity.SidedPos.Yaw + (rotY + 90f) * ((float)Math.PI / 180f));
			Quaterniond.RotateZ(quat, quat, entity.SidedPos.Roll + rotZ * ((float)Math.PI / 180f));
			float[] qf = new float[quat.Length];
			for (int i = 0; i < quat.Length; i++)
			{
				qf[i] = (float)quat[i];
			}
			Mat4f.Mul(ModelMat, ModelMat, Mat4f.FromQuat(Mat4f.Create(), qf));
			float scale = entity.Properties.Client.Size;
			Mat4f.Translate(ModelMat, ModelMat, 0f, (0f - entity.CollisionBox.Y2) / 2f, 0f);
			Mat4f.Scale(ModelMat, ModelMat, new float[3] { scale, scale, scale });
			Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0f, -0.5f);
			tmpModelMat.Set(ModelMat).Mul(apap.AnimModelMatrix).Translate(ap.PosX / 16.0, ap.PosY / 16.0, ap.PosZ / 16.0);
			EntityPos entityPos = entity.SidedPos;
			float[] endVec = Mat4f.MulWithVec4(tmpModelMat.Values, hitboxOff);
			float motionX = endVec[0] - (entity.CollisionBox.X1 - entity.OriginCollisionBox.X1);
			float motionZ = endVec[2] - (entity.CollisionBox.Z1 - entity.OriginCollisionBox.Z1);
			if ((double)Math.Abs(motionX) > 1E-05 || (double)Math.Abs(motionZ) > 1E-05)
			{
				EntityPos posMoved = entityPos.Copy();
				posMoved.Motion.X = motionX;
				posMoved.Motion.Z = motionZ;
				moveDelta.Set(posMoved.Motion.X, posMoved.Motion.Y, posMoved.Motion.Z);
				PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, posMoved, dtFac, ref newPos);
				double reflectX = (newPos.X - entityPos.X) / (double)dtFac - (double)motionX;
				double reflectZ = (newPos.Z - entityPos.Z) / (double)dtFac - (double)motionZ;
				entityPos.Motion.X = reflectX;
				entityPos.Motion.Z = reflectZ;
				entity.CollisionBox.Set(entity.OriginCollisionBox);
				entity.CollisionBox.Translate(endVec[0], 0f, endVec[2]);
				entity.SelectionBox.Set(entity.OriginSelectionBox);
				entity.SelectionBox.Translate(endVec[0], 0f, endVec[2]);
			}
		}
	}

	protected void callOnEntityInside()
	{
		PhysicsBehaviorBase.collisionTester.entityBox.SetAndTranslate(entity.CollisionBox, entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		PhysicsBehaviorBase.collisionTester.entityBox.RemoveRoundingErrors();
		Cuboidd entityBox = PhysicsBehaviorBase.collisionTester.entityBox;
		int xMax = (int)entityBox.X2;
		int yMax = (int)entityBox.Y2;
		int zMax = (int)entityBox.Z2;
		int zMin = (int)entityBox.Z1;
		BlockPos tmpPos = new BlockPos(entity.ServerPos.Dimension);
		for (int y = (int)entityBox.Y1; y <= yMax; y++)
		{
			for (int x = (int)entityBox.X1; x <= xMax; x++)
			{
				for (int z = zMin; z <= zMax; z++)
				{
					Block block = entity.World.BlockAccessor.GetBlock(tmpPos.Set(x, y, z));
					if (block.Id != 0)
					{
						block.OnEntityInside(entity.World, entity, tmpPos);
					}
				}
			}
		}
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
		return "entitycontrolledphysics";
	}
}
