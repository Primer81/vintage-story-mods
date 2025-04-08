using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFlySwoopAttack : AiTaskBaseTargetable
{
	public EnumDamageType damageType = EnumDamageType.BluntAttack;

	public int damageTier;

	protected long lastCheckOrAttackMs;

	protected float damage = 2f;

	protected float knockbackStrength = 1f;

	protected float seekingRangeVer = 25f;

	protected float seekingRangeHor = 25f;

	protected float damageRange = 5f;

	protected float moveSpeed = 0.04f;

	protected HashSet<long> didDamageEntity = new HashSet<long>();

	protected Vec3d targetPos;

	protected Vec3d beginAttackPos;

	protected List<Vec3d> swoopPath;

	private float refreshAccum;

	private float execAccum;

	private float damageAccum;

	public AiTaskFlySwoopAttack(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		moveSpeed = taskConfig["moveSpeed"].AsFloat(0.04f);
		damage = taskConfig["damage"].AsFloat(2f);
		knockbackStrength = taskConfig["knockbackStrength"].AsFloat(GameMath.Sqrt(damage / 2f));
		seekingRangeHor = taskConfig["seekingRangeHor"].AsFloat(25f);
		seekingRangeVer = taskConfig["seekingRangeVer"].AsFloat(25f);
		damageRange = taskConfig["damageRange"].AsFloat(2f);
		string strdt = taskConfig["damageType"].AsString();
		if (strdt != null)
		{
			damageType = (EnumDamageType)Enum.Parse(typeof(EnumDamageType), strdt, ignoreCase: true);
		}
		damageTier = taskConfig["damageTier"].AsInt();
	}

	public override void OnEntityLoaded()
	{
	}

	public override bool ShouldExecute()
	{
		long ellapsedMs = entity.World.ElapsedMilliseconds;
		if (cooldownUntilMs > ellapsedMs)
		{
			return false;
		}
		cooldownUntilMs = entity.World.ElapsedMilliseconds + 1500;
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		Vec3d pos = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		if (entity.World.ElapsedMilliseconds - attackedByEntityMs > 30000)
		{
			attackedByEntity = null;
		}
		if (retaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && IsTargetableEntity(attackedByEntity, 15f, ignoreEntityCode: true))
		{
			targetEntity = attackedByEntity;
		}
		else
		{
			targetEntity = entity.World.GetNearestEntity(pos, seekingRangeHor, seekingRangeVer, (Entity e) => IsTargetableEntity(e, seekingRangeHor) && hasDirectContact(e, seekingRangeHor, seekingRangeVer));
		}
		lastCheckOrAttackMs = entity.World.ElapsedMilliseconds;
		if (targetEntity == null || !(entity.ServerPos.Y - targetEntity.ServerPos.Y > 9.0) || !(entity.ServerPos.HorDistanceTo(targetEntity.ServerPos) > 25.0))
		{
			return false;
		}
		beginAttackPos = entity.ServerPos.XYZ;
		swoopPath = new List<Vec3d>(getSwoopPath(targetEntity as EntityAgent, 35, simplifiedOut: false));
		return pathClear(swoopPath);
	}

	private bool pathClear(List<Vec3d> swoopPath)
	{
		int skipPoints = 2;
		Vec3d tmppos = new Vec3d();
		for (int i = 0; i < swoopPath.Count; i += skipPoints)
		{
			tmppos.Set(swoopPath[i]);
			tmppos.Y -= 1.0;
			if (world.CollisionTester.IsColliding(entity.World.BlockAccessor, entity.CollisionBox, tmppos))
			{
				return false;
			}
		}
		return true;
	}

	public override void StartExecute()
	{
		didDamageEntity.Clear();
		targetPos = targetEntity.ServerPos.XYZ;
		swoopPath.Clear();
		swoopPath.AddRange(getSwoopPath(targetEntity as EntityAgent, 35, simplifiedOut: true));
		pathTraverser.FollowRoute(swoopPath, moveSpeed, 8f, null, null);
		execAccum = 0f;
		refreshAccum = 0f;
		base.StartExecute();
	}

	public override bool ContinueExecute(float dt)
	{
		execAccum += dt;
		refreshAccum += dt;
		if (refreshAccum > 1f && execAccum < 5f && targetEntity.Pos.Dimension == entity.Pos.Dimension)
		{
			Vec3d[] path = getSwoopPath(targetEntity as EntityAgent, 35, simplifiedOut: true);
			if (pathClear(new List<Vec3d>(path)))
			{
				swoopPath.Clear();
				swoopPath.AddRange(path);
			}
			refreshAccum = 0f;
		}
		if ((targetPos - entity.ServerPos.XYZ).Length() > (double)(Math.Max(seekingRangeHor, seekingRangeVer) * 2f))
		{
			return false;
		}
		damageAccum += dt;
		if (damageAccum > 0.2f)
		{
			damageAccum = 0f;
			List<Entity> attackableEntities = new List<Entity>();
			entity.Api.ModLoader.GetModSystem<EntityPartitioning>().GetNearestEntity(entity.ServerPos.XYZ, damageRange + 1f, delegate(Entity e)
			{
				if (IsTargetableEntity(e, damageRange) && hasDirectContact(e, damageRange, damageRange) && !didDamageEntity.Contains(entity.EntityId))
				{
					attackableEntities.Add(e);
				}
				return false;
			}, EnumEntitySearchType.Creatures);
			foreach (Entity attackEntity in attackableEntities)
			{
				attackEntity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Entity,
					SourceEntity = entity,
					Type = damageType,
					DamageTier = damageTier,
					KnockbackStrength = knockbackStrength
				}, damage * GlobalConstants.CreatureDamageModifier);
				if (entity is IMeleeAttackListener imal)
				{
					imal.DidAttack(attackEntity);
				}
				didDamageEntity.Add(entity.EntityId);
			}
		}
		return pathTraverser.Active;
	}

	public override void FinishExecute(bool cancelled)
	{
		pathTraverser.Stop();
		base.FinishExecute(cancelled);
	}

	protected virtual Vec3d[] getSwoopPath(Entity target, int its, bool simplifiedOut)
	{
		Vec3d targetPos = target.ServerPos.XYZ.AddCopy(target.LocalEyePos);
		Vec3d start1 = entity.ServerPos.XYZ;
		Vec3d start2;
		Vec3d vec3d = (start2 = new Vec3d(start1.X, targetPos.Y + 10.0, start1.Z));
		Vec3d end2 = targetPos;
		Vec3d delta1 = vec3d - start1;
		Vec3d delta2 = end2 - start2;
		int outits = (simplifiedOut ? (its / 3) : its);
		Vec3d[] points = new Vec3d[its + outits];
		for (int j = 0; j < its; j++)
		{
			double p2 = (double)j / (double)its;
			Vec3d mid2 = start1 + p2 * delta1;
			Vec3d mid4 = start2 + p2 * delta2;
			points[j] = (1.0 - p2) * mid2 + p2 * mid4;
		}
		start1 = points[its - 1];
		Vec3d offs = (target.ServerPos.XYZ - entity.ServerPos.XYZ) * 1f;
		Vec3d vec3d2 = (start2 = new Vec3d(targetPos.X + offs.X, targetPos.Y, targetPos.Z + offs.Z));
		end2 = new Vec3d(targetPos.X + offs.X * 1.2999999523162842, targetPos.Y + (beginAttackPos.Y - targetPos.Y) * 0.5, targetPos.Z + offs.Z * 1.2999999523162842);
		delta1 = vec3d2 - start1;
		delta2 = end2 - start2;
		for (int i = 0; i < outits; i++)
		{
			double p = (double)i / (double)outits;
			Vec3d mid1 = start1 + p * delta1;
			Vec3d mid3 = start2 + p * delta2;
			points[its + i] = (1.0 - p) * mid1 + p * mid3;
		}
		return points;
	}
}
