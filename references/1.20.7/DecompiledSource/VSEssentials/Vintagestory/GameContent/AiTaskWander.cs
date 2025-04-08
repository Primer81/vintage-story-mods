using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskWander : AiTaskBase
{
	public Vec3d MainTarget;

	private bool done;

	private float moveSpeed = 0.03f;

	private float wanderChance = 0.02f;

	private float maxHeight = 7f;

	private float? preferredLightLevel;

	private float targetDistance = 0.12f;

	private NatFloat wanderRangeHorizontal = NatFloat.createStrongerInvexp(3f, 40f);

	private NatFloat wanderRangeVertical = NatFloat.createStrongerInvexp(3f, 10f);

	public bool StayCloseToSpawn;

	public Vec3d SpawnPosition;

	public double MaxDistanceToSpawn;

	private long lastTimeInRangeMs;

	private int failedWanders;

	private bool needsToTele;

	public float WanderRangeMul
	{
		get
		{
			return entity.Attributes.GetFloat("wanderRangeMul", 1f);
		}
		set
		{
			entity.Attributes.SetFloat("wanderRangeMul", value);
		}
	}

	public int FailedConsecutivePathfinds
	{
		get
		{
			return entity.Attributes.GetInt("failedConsecutivePathfinds");
		}
		set
		{
			entity.Attributes.SetInt("failedConsecutivePathfinds", value);
		}
	}

	public AiTaskWander(EntityAgent entity)
		: base(entity)
	{
	}

	public override void OnEntityLoaded()
	{
		if (SpawnPosition == null && !entity.Attributes.HasAttribute("spawnX"))
		{
			OnEntitySpawn();
		}
	}

	public override void OnEntitySpawn()
	{
		entity.Attributes.SetDouble("spawnX", entity.ServerPos.X);
		entity.Attributes.SetDouble("spawnY", entity.ServerPos.Y);
		entity.Attributes.SetDouble("spawnZ", entity.ServerPos.Z);
		SpawnPosition = entity.ServerPos.XYZ;
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		SpawnPosition = new Vec3d(entity.Attributes.GetDouble("spawnX"), entity.Attributes.GetDouble("spawnY"), entity.Attributes.GetDouble("spawnZ"));
		float wanderRangeMin = 3f;
		float wanderRangeMax = 30f;
		if (taskConfig["maxDistanceToSpawn"].Exists)
		{
			StayCloseToSpawn = true;
			MaxDistanceToSpawn = taskConfig["maxDistanceToSpawn"].AsDouble(10.0);
		}
		targetDistance = taskConfig["targetDistance"].AsFloat(0.12f);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
		wanderChance = taskConfig["wanderChance"].AsFloat(0.02f);
		wanderRangeMin = taskConfig["wanderRangeMin"].AsFloat(3f);
		wanderRangeMax = taskConfig["wanderRangeMax"].AsFloat(30f);
		wanderRangeHorizontal = NatFloat.createInvexp(wanderRangeMin, wanderRangeMax);
		maxHeight = taskConfig["maxHeight"].AsFloat(7f);
		preferredLightLevel = taskConfig["preferredLightLevel"].AsFloat(-99f);
		if (preferredLightLevel < 0f)
		{
			preferredLightLevel = null;
		}
	}

	public Vec3d loadNextWanderTarget()
	{
		EnumHabitat habitat = entity.Properties.Habitat;
		_ = entity.Properties.FallDamage;
		_ = StayCloseToSpawn;
		int tries = 9;
		Vec4d bestTarget = null;
		Vec4d curTarget = new Vec4d();
		BlockPos tmpPos = new BlockPos();
		if (FailedConsecutivePathfinds > 10)
		{
			WanderRangeMul = Math.Max(0.1f, WanderRangeMul * 0.9f);
		}
		else
		{
			WanderRangeMul = Math.Min(1f, WanderRangeMul * 1.1f);
			if (base.rand.NextDouble() < 0.05)
			{
				WanderRangeMul = Math.Min(1f, WanderRangeMul * 1.5f);
			}
		}
		float wRangeMul = WanderRangeMul;
		if (base.rand.NextDouble() < 0.05)
		{
			wRangeMul *= 3f;
		}
		while (tries-- > 0)
		{
			double dx = wanderRangeHorizontal.nextFloat() * (float)(base.rand.Next(2) * 2 - 1) * wRangeMul;
			double dy = wanderRangeVertical.nextFloat() * (float)(base.rand.Next(2) * 2 - 1) * wRangeMul;
			double dz = wanderRangeHorizontal.nextFloat() * (float)(base.rand.Next(2) * 2 - 1) * wRangeMul;
			curTarget.X = entity.ServerPos.X + dx;
			curTarget.Y = entity.ServerPos.Y + dy;
			curTarget.Z = entity.ServerPos.Z + dz;
			curTarget.W = 1.0;
			if (StayCloseToSpawn)
			{
				double distToEdge = (double)curTarget.SquareDistanceTo(SpawnPosition) / (MaxDistanceToSpawn * MaxDistanceToSpawn);
				curTarget.W = 1.0 - distToEdge;
			}
			switch (habitat)
			{
			case EnumHabitat.Air:
			{
				int rainMapY = world.BlockAccessor.GetRainMapHeightAt((int)curTarget.X, (int)curTarget.Z);
				curTarget.Y = Math.Min(curTarget.Y, (float)rainMapY + maxHeight);
				if (entity.World.BlockAccessor.GetBlock((int)curTarget.X, (int)curTarget.Y, (int)curTarget.Z, 2).IsLiquid())
				{
					curTarget.W = 0.0;
				}
				break;
			}
			case EnumHabitat.Land:
			{
				curTarget.Y = moveDownToFloor((int)curTarget.X, curTarget.Y, (int)curTarget.Z);
				if (curTarget.Y < 0.0)
				{
					curTarget.W = 0.0;
					break;
				}
				if (entity.World.BlockAccessor.GetBlock((int)curTarget.X, (int)curTarget.Y, (int)curTarget.Z, 2).IsLiquid())
				{
					curTarget.W /= 2.0;
				}
				bool stop = false;
				bool willFall = false;
				float angleHor = (float)Math.Atan2(dx, dz) + (float)Math.PI / 2f;
				Vec3d target1BlockAhead = curTarget.XYZ.Ahead(1.0, 0f, angleHor);
				Vec3d startAhead = entity.ServerPos.XYZ.Ahead(1.0, 0f, angleHor);
				int prevY = (int)startAhead.Y;
				GameMath.BresenHamPlotLine2d((int)startAhead.X, (int)startAhead.Z, (int)target1BlockAhead.X, (int)target1BlockAhead.Z, delegate(int x, int z)
				{
					if (!stop)
					{
						double num = moveDownToFloor(x, prevY, z);
						if (num < 0.0 || (double)prevY - num > 4.0)
						{
							willFall = true;
							stop = true;
						}
						if (num - (double)prevY > 2.0)
						{
							stop = true;
						}
						prevY = (int)num;
					}
				});
				if (willFall)
				{
					curTarget.W = 0.0;
				}
				break;
			}
			case EnumHabitat.Sea:
				if (!entity.World.BlockAccessor.GetBlock((int)curTarget.X, (int)curTarget.Y, (int)curTarget.Z, 2).IsLiquid())
				{
					curTarget.W = 0.0;
				}
				break;
			case EnumHabitat.Underwater:
				if (!entity.World.BlockAccessor.GetBlock((int)curTarget.X, (int)curTarget.Y, (int)curTarget.Z, 2).IsLiquid())
				{
					curTarget.W = 0.0;
				}
				else
				{
					curTarget.W = 1.0 / (Math.Abs(dy) + 1.0);
				}
				break;
			}
			if (curTarget.W > 0.0)
			{
				for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
				{
					BlockFacing face = BlockFacing.HORIZONTALS[i];
					if (entity.World.BlockAccessor.IsSideSolid((int)curTarget.X + face.Normali.X, (int)curTarget.Y, (int)curTarget.Z + face.Normali.Z, face.Opposite))
					{
						curTarget.W *= 0.5;
					}
				}
			}
			if (preferredLightLevel.HasValue && curTarget.W != 0.0)
			{
				tmpPos.Set((int)curTarget.X, (int)curTarget.Y, (int)curTarget.Z);
				int lightdiff = Math.Abs((int)preferredLightLevel.Value - entity.World.BlockAccessor.GetLightLevel(tmpPos, EnumLightLevelType.MaxLight));
				curTarget.W /= Math.Max(1, lightdiff);
			}
			if (bestTarget == null || curTarget.W > bestTarget.W)
			{
				bestTarget = new Vec4d(curTarget.X, curTarget.Y, curTarget.Z, curTarget.W);
				if (curTarget.W >= 1.0)
				{
					break;
				}
			}
		}
		if (bestTarget.W > 0.0)
		{
			FailedConsecutivePathfinds = Math.Max(FailedConsecutivePathfinds - 3, 0);
			return bestTarget.XYZ;
		}
		FailedConsecutivePathfinds++;
		return null;
	}

	private double moveDownToFloor(int x, double y, int z)
	{
		int tries = 5;
		while (tries-- > 0)
		{
			if (world.BlockAccessor.IsSideSolid(x, (int)y, z, BlockFacing.UP))
			{
				return y + 1.0;
			}
			y -= 1.0;
		}
		return -1.0;
	}

	public override bool ShouldExecute()
	{
		if (base.rand.NextDouble() > (double)((failedWanders > 0) ? (1f - wanderChance * 4f * (float)failedWanders) : wanderChance))
		{
			failedWanders = 0;
			return false;
		}
		needsToTele = false;
		double dist = entity.ServerPos.XYZ.SquareDistanceTo(SpawnPosition);
		if (StayCloseToSpawn)
		{
			long ellapsedMs = entity.World.ElapsedMilliseconds;
			if (dist > MaxDistanceToSpawn * MaxDistanceToSpawn)
			{
				if (ellapsedMs - lastTimeInRangeMs > 120000 && entity.World.GetNearestEntity(entity.ServerPos.XYZ, 15f, 15f, (Entity e) => e is EntityPlayer) == null)
				{
					needsToTele = true;
				}
				MainTarget = SpawnPosition.Clone();
				return true;
			}
			lastTimeInRangeMs = ellapsedMs;
		}
		MainTarget = loadNextWanderTarget();
		return MainTarget != null;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		if (needsToTele)
		{
			entity.TeleportTo(SpawnPosition);
			done = true;
		}
		else
		{
			done = false;
			pathTraverser.WalkTowards(MainTarget, moveSpeed, targetDistance, OnGoalReached, OnStuck);
		}
	}

	public override bool ContinueExecute(float dt)
	{
		base.ContinueExecute(dt);
		if (entity.Controls.IsClimbing && entity.Properties.CanClimbAnywhere && entity.ClimbingIntoFace != null)
		{
			BlockFacing climbingIntoFace = entity.ClimbingIntoFace;
			if (Math.Sign(climbingIntoFace.Normali.X) == Math.Sign(pathTraverser.CurrentTarget.X - entity.ServerPos.X))
			{
				pathTraverser.CurrentTarget.X = entity.ServerPos.X;
			}
			if (Math.Sign(climbingIntoFace.Normali.Y) == Math.Sign(pathTraverser.CurrentTarget.Y - entity.ServerPos.Y))
			{
				pathTraverser.CurrentTarget.Y = entity.ServerPos.Y;
			}
			if (Math.Sign(climbingIntoFace.Normali.Z) == Math.Sign(pathTraverser.CurrentTarget.Z - entity.ServerPos.Z))
			{
				pathTraverser.CurrentTarget.Z = entity.ServerPos.Z;
			}
		}
		if ((double)MainTarget.HorizontalSquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Z) < 0.5)
		{
			pathTraverser.Stop();
			return false;
		}
		return !done;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		if (cancelled)
		{
			pathTraverser.Stop();
		}
	}

	private void OnStuck()
	{
		done = true;
		failedWanders++;
	}

	private void OnGoalReached()
	{
		done = true;
		failedWanders = 0;
	}
}
