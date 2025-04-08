using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AiTaskFireFeathersAttack : AiTaskTargetableAt
{
	protected float seekingRangeVer = 25f;

	protected float seekingRangeHor = 25f;

	private int fireAfterMs;

	private int durationMs;

	private ProjectileConfig[] projectileConfigs;

	public bool Enabled = true;

	private float accum;

	private bool projectilesFired;

	public AiTaskFireFeathersAttack(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		fireAfterMs = taskConfig["fireAfterMs"].AsInt(1000);
		durationMs = taskConfig["durationMs"].AsInt(1000);
		seekingRangeHor = taskConfig["seekingRangeHor"].AsFloat(25f);
		seekingRangeVer = taskConfig["seekingRangeVer"].AsFloat(25f);
		projectileConfigs = taskConfig["projectileConfigs"].AsObject<ProjectileConfig[]>(null, entity.Code.Domain);
		ProjectileConfig[] array = projectileConfigs;
		foreach (ProjectileConfig val in array)
		{
			val.EntityType = entity.World.GetEntityType(val.Code);
			if (val.EntityType == null)
			{
				throw new Exception("No such projectile exists - " + val.Code);
			}
			val.CollectibleStack?.Resolve(entity.World, $"Projectile stack of {entity.Code}");
		}
	}

	public override bool ShouldExecute()
	{
		if (!Enabled)
		{
			return false;
		}
		CenterPos = SpawnPos;
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
			targetEntity = entity.World.GetNearestEntity(CenterPos, seekingRangeHor, seekingRangeVer, (Entity e) => IsTargetableEntity(e, seekingRangeHor) && hasDirectContact(e, seekingRangeHor, seekingRangeVer));
		}
		if (targetEntity != null && entity.ServerPos.Y - targetEntity.ServerPos.Y > 9.0)
		{
			return entity.ServerPos.HorDistanceTo(targetEntity.ServerPos) > 25.0;
		}
		return false;
	}

	private void fireProjectiles()
	{
		IWorldAccessor world = entity.World;
		Random rnd = world.Rand;
		projectileConfigs = projectileConfigs.Shuffle(rnd);
		ProjectileConfig[] array = projectileConfigs;
		foreach (ProjectileConfig cfg in array)
		{
			if (cfg.LeftToFire > 0)
			{
				cfg.LeftToFire--;
				EntityProjectile entitypr = world.ClassRegistry.CreateEntity(cfg.EntityType) as EntityProjectile;
				entitypr.FiredBy = entity;
				entitypr.DamageType = cfg.DamageType;
				entitypr.Damage = cfg.Damage;
				entitypr.ProjectileStack = cfg.CollectibleStack?.ResolvedItemstack?.Clone() ?? new ItemStack(world.GetItem(new AssetLocation("stone-granite")));
				entitypr.NonCollectible = cfg.CollectibleStack?.ResolvedItemstack == null;
				entitypr.World = world;
				Vec3d spawnpos = entity.ServerPos.XYZ.Add(rnd.NextDouble() * 6.0 - 3.0, rnd.NextDouble() * 5.0, rnd.NextDouble() * 6.0 - 3.0);
				Vec3d targetPos = targetEntity.ServerPos.XYZ.Add(0.0, targetEntity.LocalEyePos.Y, 0.0) + targetEntity.ServerPos.Motion * 8f;
				double dist = spawnpos.DistanceTo(targetPos);
				double distf = Math.Pow(dist, 0.2);
				Vec3d velocity = (targetPos - spawnpos).Normalize() * GameMath.Clamp(distf - 1.0, 0.10000000149011612, 1.0);
				velocity.Y += (dist - 10.0) / 150.0;
				velocity.X *= 1.0 + (rnd.NextDouble() - 0.5) / 3.0;
				velocity.Y *= 1.0 + (rnd.NextDouble() - 0.5) / 5.0;
				velocity.Z *= 1.0 + (rnd.NextDouble() - 0.5) / 3.0;
				entitypr.ServerPos.SetPosWithDimension(spawnpos);
				entitypr.Pos.SetFrom(spawnpos);
				entitypr.ServerPos.Motion.Set(velocity);
				entitypr.SetInitialRotation();
				world.SpawnEntity(entitypr);
				break;
			}
		}
	}

	public override void StartExecute()
	{
		base.StartExecute();
		accum = 0f;
		projectilesFired = false;
	}

	public override bool ContinueExecute(float dt)
	{
		accum += dt;
		if (accum * 1000f > (float)fireAfterMs)
		{
			if (!projectilesFired)
			{
				ProjectileConfig[] array = projectileConfigs;
				foreach (ProjectileConfig cfg in array)
				{
					cfg.LeftToFire = GameMath.RoundRandom(entity.World.Rand, cfg.Quantity.nextFloat());
				}
				world.PlaySoundAt("sounds/creature/erel/fire", entity, null, randomizePitch: false, 100f);
			}
			fireProjectiles();
			projectilesFired = true;
		}
		return accum * 1000f < (float)durationMs;
	}
}
