using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskMeleeAttack : AiTaskBaseTargetable
{
	protected long lastCheckOrAttackMs;

	protected float damage = 2f;

	protected float knockbackStrength = 1f;

	protected float minDist = 1.5f;

	protected float minVerDist = 1f;

	protected float attackAngleRangeDeg = 20f;

	protected bool damageInflicted;

	protected int attackDurationMs = 1500;

	protected int damagePlayerAtMs = 500;

	public EnumDamageType damageType = EnumDamageType.BluntAttack;

	public int damageTier;

	protected float attackRange = 3f;

	protected bool turnToTarget = true;

	private float curTurnRadPerSec;

	private bool didStartAnim;

	public AiTaskMeleeAttack(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		damage = taskConfig["damage"].AsFloat(2f);
		knockbackStrength = taskConfig["knockbackStrength"].AsFloat(GameMath.Sqrt(damage / 4f));
		attackAngleRangeDeg = taskConfig["attackAngleRangeDeg"].AsFloat(20f);
		attackDurationMs = taskConfig["attackDurationMs"].AsInt(1500);
		damagePlayerAtMs = taskConfig["damagePlayerAtMs"].AsInt(1000);
		minDist = taskConfig["minDist"].AsFloat(2f);
		minVerDist = taskConfig["minVerDist"].AsFloat(1f);
		string strdt = taskConfig["damageType"].AsString();
		if (strdt != null)
		{
			damageType = (EnumDamageType)Enum.Parse(typeof(EnumDamageType), strdt, ignoreCase: true);
		}
		damageTier = taskConfig["damageTier"].AsInt();
		entity.WatchedAttributes.GetTreeAttribute("extraInfoText").SetString("dmgTier", Lang.Get("Damage tier: {0}", damageTier));
	}

	public override bool ShouldExecute()
	{
		long ellapsedMs = entity.World.ElapsedMilliseconds;
		if (ellapsedMs - lastCheckOrAttackMs < attackDurationMs || cooldownUntilMs > ellapsedMs)
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		Vec3d pos = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		int generation = GetOwnGeneration();
		bool fullyTamed = (float)generation >= tamingGenerations;
		float fearReductionFactor = Math.Max(0f, (tamingGenerations - (float)generation) / tamingGenerations);
		if (whenInEmotionState != null)
		{
			fearReductionFactor = 1f;
		}
		if (fearReductionFactor <= 0f)
		{
			return false;
		}
		if (entity.World.ElapsedMilliseconds - attackedByEntityMs > 30000)
		{
			attackedByEntity = null;
		}
		if (retaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && IsTargetableEntity(attackedByEntity, 15f, ignoreEntityCode: true) && hasDirectContact(attackedByEntity, minDist, minVerDist))
		{
			targetEntity = attackedByEntity;
		}
		else
		{
			targetEntity = entity.World.GetNearestEntity(pos, attackRange * fearReductionFactor, attackRange * fearReductionFactor, delegate(Entity e)
			{
				if (fullyTamed && isNonAttackingPlayer(e))
				{
					return false;
				}
				return IsTargetableEntity(e, 15f) && hasDirectContact(e, minDist, minVerDist);
			});
		}
		lastCheckOrAttackMs = entity.World.ElapsedMilliseconds;
		damageInflicted = false;
		return targetEntity != null;
	}

	public override void StartExecute()
	{
		didStartAnim = false;
		curTurnRadPerSec = entity.GetBehavior<EntityBehaviorTaskAI>().PathTraverser.curTurnRadPerSec;
		if (!turnToTarget)
		{
			base.StartExecute();
		}
	}

	public override bool ContinueExecute(float dt)
	{
		EntityPos own = entity.ServerPos;
		EntityPos his = targetEntity.ServerPos;
		if (own.Dimension != his.Dimension)
		{
			return false;
		}
		bool correctYaw = true;
		if (turnToTarget)
		{
			float desiredYaw = (float)Math.Atan2(his.X - own.X, his.Z - own.Z);
			float yawDist = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);
			entity.ServerPos.Yaw += GameMath.Clamp(yawDist, (0f - curTurnRadPerSec) * dt * GlobalConstants.OverallSpeedMultiplier, curTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier);
			entity.ServerPos.Yaw = entity.ServerPos.Yaw % ((float)Math.PI * 2f);
			correctYaw = Math.Abs(yawDist) < attackAngleRangeDeg * ((float)Math.PI / 180f);
			if (correctYaw && !didStartAnim)
			{
				didStartAnim = true;
				base.StartExecute();
			}
		}
		if (lastCheckOrAttackMs + damagePlayerAtMs > entity.World.ElapsedMilliseconds)
		{
			return true;
		}
		if (!damageInflicted && correctYaw)
		{
			attackTarget();
			damageInflicted = true;
		}
		if (lastCheckOrAttackMs + attackDurationMs > entity.World.ElapsedMilliseconds)
		{
			return true;
		}
		return false;
	}

	protected virtual void attackTarget()
	{
		if (!hasDirectContact(targetEntity, minDist, minVerDist))
		{
			return;
		}
		bool alive = targetEntity.Alive;
		targetEntity.ReceiveDamage(new DamageSource
		{
			Source = EnumDamageSource.Entity,
			SourceEntity = entity,
			Type = damageType,
			DamageTier = damageTier,
			KnockbackStrength = knockbackStrength
		}, damage * GlobalConstants.CreatureDamageModifier);
		if (entity is IMeleeAttackListener imal)
		{
			imal.DidAttack(targetEntity);
		}
		if (alive && !targetEntity.Alive)
		{
			if (!(targetEntity is EntityPlayer))
			{
				entity.WatchedAttributes.SetDouble("lastMealEatenTotalHours", entity.World.Calendar.TotalHours);
			}
			bhEmo?.TryTriggerState("saturated", targetEntity.EntityId);
		}
	}
}
