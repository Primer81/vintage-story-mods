using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskSeekBlockAndLay : AiTaskBase
{
	private POIRegistry porregistry;

	private IAnimalNest targetPoi;

	private float moveSpeed = 0.02f;

	private bool nowStuck;

	private bool laid;

	private float sitDays = 1f;

	private float layTime = 1f;

	private double incubationDays = 5.0;

	private string chickCode;

	private double onGroundChance = 0.3;

	private AssetLocation failBlockCode;

	private float sitTimeNow;

	private double sitEndDay;

	private bool sitAnimStarted;

	private float PortionsEatenForLay;

	private string requiresNearbyEntityCode;

	private float requiresNearbyEntityRange = 5f;

	private AnimationMetaData sitAnimMeta;

	private Dictionary<IAnimalNest, FailedAttempt> failedSeekTargets = new Dictionary<IAnimalNest, FailedAttempt>();

	private long lastPOISearchTotalMs;

	private double attemptLayEggTotalHours;

	public AiTaskSeekBlockAndLay(EntityAgent entity)
		: base(entity)
	{
		porregistry = entity.Api.ModLoader.GetModSystem<POIRegistry>();
		entity.WatchedAttributes.SetBool("doesSit", value: true);
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);
		sitDays = taskConfig["sitDays"].AsFloat(1f);
		layTime = taskConfig["layTime"].AsFloat(1.5f);
		incubationDays = taskConfig["incubationDays"].AsDouble(5.0);
		if (taskConfig["sitAnimation"].Exists)
		{
			sitAnimMeta = new AnimationMetaData
			{
				Code = taskConfig["sitAnimation"].AsString()?.ToLowerInvariant(),
				Animation = taskConfig["sitAnimation"].AsString()?.ToLowerInvariant(),
				AnimationSpeed = taskConfig["sitAnimationSpeed"].AsFloat(1f)
			}.Init();
		}
		chickCode = taskConfig["chickCode"].AsString();
		PortionsEatenForLay = taskConfig["portionsEatenForLay"].AsFloat(3f);
		requiresNearbyEntityCode = taskConfig["requiresNearbyEntityCode"].AsString();
		requiresNearbyEntityRange = taskConfig["requiresNearbyEntityRange"].AsFloat(5f);
		string code = taskConfig["failBlockCode"].AsString();
		if (code != null)
		{
			failBlockCode = new AssetLocation(code);
		}
	}

	public override bool ShouldExecute()
	{
		if (entity.World.Rand.NextDouble() > 0.03)
		{
			return false;
		}
		if (lastPOISearchTotalMs + 15000 > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (cooldownUntilTotalHours > entity.World.Calendar.TotalHours)
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		PortionsEatenForLay = 3f;
		if (!DidConsumeFood(PortionsEatenForLay))
		{
			return false;
		}
		if (attemptLayEggTotalHours <= 0.0)
		{
			attemptLayEggTotalHours = entity.World.Calendar.TotalHours;
		}
		lastPOISearchTotalMs = entity.World.ElapsedMilliseconds;
		targetPoi = FindPOI(42) as IAnimalNest;
		if (targetPoi == null)
		{
			LayEggOnGround();
		}
		return targetPoi != null;
	}

	private IPointOfInterest FindPOI(int radius)
	{
		return porregistry.GetWeightedNearestPoi(entity.ServerPos.XYZ, radius, delegate(IPointOfInterest poi)
		{
			if (poi.Type != "nest")
			{
				return false;
			}
			IAnimalNest animalNest;
			IAnimalNest animalNest2 = (animalNest = poi as IAnimalNest);
			if (animalNest2 != null && animalNest2.IsSuitableFor(entity) && !animalNest.Occupied(entity))
			{
				failedSeekTargets.TryGetValue(animalNest, out var value);
				if (value == null || value.Count < 4 || value.LastTryMs < world.ElapsedMilliseconds - 60000)
				{
					return true;
				}
			}
			return false;
		});
	}

	public float MinDistanceToTarget()
	{
		return 0.01f;
	}

	public override void StartExecute()
	{
		if (animMeta != null)
		{
			animMeta.EaseInSpeed = 1f;
			animMeta.EaseOutSpeed = 1f;
			entity.AnimManager.StartAnimation(animMeta);
		}
		nowStuck = false;
		sitTimeNow = 0f;
		laid = false;
		pathTraverser.NavigateTo_Async(targetPoi.Position, moveSpeed, MinDistanceToTarget() - 0.1f, OnGoalReached, OnStuck, null, 1000, 1);
		sitAnimStarted = false;
	}

	public override bool CanContinueExecute()
	{
		return pathTraverser.Ready;
	}

	public override bool ContinueExecute(float dt)
	{
		if (targetPoi.Occupied(entity))
		{
			onBadTarget();
			return false;
		}
		Vec3d pos = targetPoi.Position;
		double num = pos.HorizontalSquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Z);
		pathTraverser.CurrentTarget.X = pos.X;
		pathTraverser.CurrentTarget.Y = pos.Y;
		pathTraverser.CurrentTarget.Z = pos.Z;
		float minDist = MinDistanceToTarget();
		if (num <= (double)minDist)
		{
			pathTraverser.Stop();
			if (animMeta != null)
			{
				entity.AnimManager.StopAnimation(animMeta.Code);
			}
			entity.GetBehavior<EntityBehaviorMultiply>();
			if (!targetPoi.IsSuitableFor(entity))
			{
				onBadTarget();
				return false;
			}
			targetPoi.SetOccupier(entity);
			if (sitAnimMeta != null && !sitAnimStarted)
			{
				entity.AnimManager.StartAnimation(sitAnimMeta);
				sitAnimStarted = true;
				sitEndDay = entity.World.Calendar.TotalDays + (double)sitDays;
			}
			sitTimeNow += dt;
			if (sitTimeNow >= layTime && !laid)
			{
				laid = true;
				if (targetPoi.TryAddEgg(entity, (GetRequiredEntityNearby() == null) ? null : chickCode, incubationDays))
				{
					ConsumeFood(PortionsEatenForLay);
					attemptLayEggTotalHours = -1.0;
					MakeLaySound();
					failedSeekTargets.Remove(targetPoi);
					return false;
				}
			}
			if (entity.World.Calendar.TotalDays >= sitEndDay)
			{
				failedSeekTargets.Remove(targetPoi);
				return false;
			}
		}
		else if (!pathTraverser.Active)
		{
			float rndx = (float)entity.World.Rand.NextDouble() * 0.3f - 0.15f;
			float rndz = (float)entity.World.Rand.NextDouble() * 0.3f - 0.15f;
			pathTraverser.NavigateTo(targetPoi.Position.AddCopy(rndx, 0f, rndz), moveSpeed, MinDistanceToTarget() - 0.15f, OnGoalReached, OnStuck, null, giveUpWhenNoPath: false, 500);
		}
		if (nowStuck)
		{
			return false;
		}
		if (attemptLayEggTotalHours > 0.0 && entity.World.Calendar.TotalHours - attemptLayEggTotalHours > 12.0)
		{
			LayEggOnGround();
			return false;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		attemptLayEggTotalHours = -1.0;
		pathTraverser.Stop();
		if (sitAnimMeta != null)
		{
			entity.AnimManager.StopAnimation(sitAnimMeta.Code);
		}
		targetPoi?.SetOccupier(null);
		if (cancelled)
		{
			cooldownUntilTotalHours = 0.0;
		}
	}

	private void OnStuck()
	{
		nowStuck = true;
		onBadTarget();
	}

	private void onBadTarget()
	{
		IAnimalNest newTarget = null;
		if (attemptLayEggTotalHours >= 0.0 && entity.World.Calendar.TotalHours - attemptLayEggTotalHours > 12.0)
		{
			LayEggOnGround();
		}
		else if (base.rand.NextDouble() > 0.4)
		{
			newTarget = FindPOI(18) as IAnimalNest;
		}
		FailedAttempt attempt = null;
		failedSeekTargets.TryGetValue(targetPoi, out attempt);
		if (attempt == null)
		{
			attempt = (failedSeekTargets[targetPoi] = new FailedAttempt());
		}
		attempt.Count++;
		attempt.LastTryMs = world.ElapsedMilliseconds;
		if (newTarget != null)
		{
			targetPoi = newTarget;
			nowStuck = false;
			sitTimeNow = 0f;
			laid = false;
			pathTraverser.NavigateTo_Async(targetPoi.Position, moveSpeed, MinDistanceToTarget() - 0.1f, OnGoalReached, OnStuck, null, 1000, 1);
			sitAnimStarted = false;
		}
	}

	private void OnGoalReached()
	{
		pathTraverser.Active = true;
		failedSeekTargets.Remove(targetPoi);
	}

	private bool DidConsumeFood(float portion)
	{
		ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
		if (tree == null)
		{
			return false;
		}
		return tree.GetFloat("saturation") >= portion;
	}

	private bool ConsumeFood(float portion)
	{
		ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
		if (tree == null)
		{
			return false;
		}
		float saturation = tree.GetFloat("saturation");
		if (saturation >= portion)
		{
			float portionEaten = ((entity.World.Rand.NextDouble() < 0.25) ? portion : 1f);
			tree.SetFloat("saturation", saturation - portionEaten);
			return true;
		}
		return false;
	}

	private Entity GetRequiredEntityNearby()
	{
		if (requiresNearbyEntityCode == null)
		{
			return null;
		}
		return entity.World.GetNearestEntity(entity.ServerPos.XYZ, requiresNearbyEntityRange, requiresNearbyEntityRange, delegate(Entity e)
		{
			if (e.WildCardMatch(new AssetLocation(requiresNearbyEntityCode)))
			{
				ITreeAttribute treeAttribute = e.WatchedAttributes.GetTreeAttribute("hunger");
				if (!e.WatchedAttributes.GetBool("doesEat") || treeAttribute == null)
				{
					return true;
				}
				treeAttribute.SetFloat("saturation", Math.Max(0f, treeAttribute.GetFloat("saturation") - 1f));
				return true;
			}
			return false;
		});
	}

	private void LayEggOnGround()
	{
		if (!(entity.World.Rand.NextDouble() > onGroundChance))
		{
			Block block = entity.World.GetBlock(failBlockCode);
			if (block != null && (TryPlace(block, 0, 0, 0) || TryPlace(block, 1, 0, 0) || TryPlace(block, 0, 0, -1) || TryPlace(block, -1, 0, 0) || TryPlace(block, 0, 0, 1)))
			{
				ConsumeFood(PortionsEatenForLay);
				attemptLayEggTotalHours = -1.0;
			}
		}
	}

	private bool TryPlace(Block block, int dx, int dy, int dz)
	{
		IBlockAccessor blockAccess = entity.World.BlockAccessor;
		BlockPos pos = entity.ServerPos.XYZ.AsBlockPos.Add(dx, dy, dz);
		if (blockAccess.GetBlock(pos, 2).IsLiquid())
		{
			return false;
		}
		if (!blockAccess.GetBlock(pos).IsReplacableBy(block))
		{
			return false;
		}
		pos.Y--;
		if (blockAccess.GetMostSolidBlock(pos).CanAttachBlockAt(blockAccess, block, pos, BlockFacing.UP))
		{
			pos.Y++;
			blockAccess.SetBlock(block.BlockId, pos);
			BlockEntityTransient obj = blockAccess.GetBlockEntity(pos) as BlockEntityTransient;
			obj?.SetPlaceTime(entity.World.Calendar.TotalHours);
			if (obj != null && obj.IsDueTransition())
			{
				blockAccess.SetBlock(0, pos);
			}
			return true;
		}
		return false;
	}

	private void MakeLaySound()
	{
		if (sound == null)
		{
			return;
		}
		if (soundStartMs > 0)
		{
			entity.World.RegisterCallback(delegate
			{
				entity.World.PlaySoundAt(sound, entity, null, randomizePitch: true, soundRange);
				lastSoundTotalMs = entity.World.ElapsedMilliseconds;
			}, soundStartMs);
		}
		else
		{
			entity.World.PlaySoundAt(sound, entity, null, randomizePitch: true, soundRange);
			lastSoundTotalMs = entity.World.ElapsedMilliseconds;
		}
	}
}
