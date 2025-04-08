using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFlyDiveAttack : AiTaskBaseTargetable
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

	protected EntityPos targetPos = new EntityPos();

	protected Vec3d beginAttackPos;

	protected float diveRange = 20f;

	protected float requireMinRange = 30f;

	public bool Enabled = true;

	private float damageAccum;

	private bool diving;

	private bool impact;

	private int approachPoints;

	public AiTaskFlyDiveAttack(EntityAgent entity)
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
		if (!Enabled)
		{
			return false;
		}
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
		if (targetEntity != null && entity.ServerPos.Y - targetEntity.ServerPos.Y > 9.0)
		{
			return entity.ServerPos.HorDistanceTo(targetEntity.ServerPos) > 20.0;
		}
		return false;
	}

	public override void StartExecute()
	{
		didDamageEntity.Clear();
		targetPos.SetFrom(targetEntity.ServerPos);
		diving = false;
		impact = false;
		base.StartExecute();
	}

	public override bool ContinueExecute(float dt)
	{
		if (targetEntity.Pos.Dimension == entity.Pos.Dimension)
		{
			targetPos.SetFrom(targetEntity.ServerPos);
		}
		if (!impact)
		{
			double hordist = entity.ServerPos.HorDistanceTo(targetPos);
			if (!diving && entity.ServerPos.Y - targetPos.Y < hordist * 1.350000023841858)
			{
				entity.ServerPos.Motion.Y = 0.15000000596046448;
				entity.ServerPos.Motion.X *= 0.8999999761581421;
				entity.ServerPos.Motion.Z *= 0.8999999761581421;
				return true;
			}
			if (!diving)
			{
				entity.AnimManager.StopAnimation("fly-idle");
				entity.AnimManager.StopAnimation("fly-flapactive");
				entity.AnimManager.StopAnimation("fly-flapcruise");
				entity.AnimManager.StartAnimation("dive");
			}
			diving = true;
			Vec3d offs = targetPos.XYZ - entity.ServerPos.XYZ;
			Vec3d dir = offs.Normalize();
			entity.ServerPos.Motion.X = dir.X * (double)moveSpeed * 10.0;
			entity.ServerPos.Motion.Y = dir.Y * (double)moveSpeed * 10.0;
			entity.ServerPos.Motion.Z = dir.Z * (double)moveSpeed * 10.0;
			double speed = entity.ServerPos.Motion.Length();
			entity.ServerPos.Roll = (float)Math.Asin(GameMath.Clamp((0.0 - dir.Y) / speed, -1.0, 1.0));
			entity.ServerPos.Yaw = (float)Math.Atan2(offs.X, offs.Z);
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
			if (entity.Collided)
			{
				entity.AnimManager.StopAnimation("dive");
				entity.AnimManager.StartAnimation("slam");
				impact = true;
			}
		}
		if (impact)
		{
			entity.ServerPos.Roll = 0f;
			entity.ServerPos.Motion.Set(0.0, 0.0, 0.0);
		}
		if (!impact)
		{
			return true;
		}
		RunningAnimation state = entity.AnimManager.GetAnimationState("slam");
		if (state != null && state.AnimProgress > 0.5f)
		{
			entity.AnimManager.StartAnimation("takeoff");
		}
		if (state != null)
		{
			return state.AnimProgress < 0.6f;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		pathTraverser.Stop();
		entity.AnimManager.StartAnimation("fly-flapactive-fast");
		base.FinishExecute(cancelled);
	}

	protected virtual Vec3d[] getSwoopPath(Entity target, int its)
	{
		bool withDive = true;
		Vec3d targetPos = target.ServerPos.XYZ.AddCopy(target.LocalEyePos);
		Vec3d selfPos = entity.ServerPos.XYZ;
		Vec3d vec3d = targetPos - entity.ServerPos.XYZ;
		double approachDist = vec3d.HorLength();
		Vec3d unitDist = vec3d.Normalize();
		int apprinterval = 3;
		approachPoints = Math.Max(0, (int)((approachDist - (double)(diveRange * 0.8f)) / (double)apprinterval));
		Vec3d[] points = new Vec3d[(withDive ? its : 0) + approachPoints];
		for (int j = 0; j < approachPoints; j++)
		{
			float p2 = (float)j / (float)approachPoints;
			points[j] = new Vec3d(selfPos.X + unitDist.X * (double)j * (double)apprinterval, targetPos.Y + (double)(30f * p2), selfPos.Z + unitDist.Z * (double)j * (double)apprinterval);
		}
		if (withDive)
		{
			Vec3d start1 = ((approachPoints <= 0) ? selfPos : points[approachPoints - 1]);
			Vec3d start2;
			Vec3d vec3d2 = (start2 = new Vec3d(targetPos.X, selfPos.Y, targetPos.Z));
			Vec3d end2 = targetPos;
			Vec3d delta1 = vec3d2 - start1;
			Vec3d delta2 = end2 - start2;
			for (int i = 0; i < its; i++)
			{
				double p = (double)i / (double)its;
				Vec3d mid1 = start1 + p * delta1;
				Vec3d mid2 = start2 + p * delta2;
				points[approachPoints + i] = (1.0 - p) * mid1 + p * mid2;
			}
		}
		return points;
	}
}
