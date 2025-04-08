using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityBehaviorAimingAccuracy : EntityBehavior
{
	public Random Rand;

	public bool IsAiming;

	private List<AccuracyModifier> modifiers = new List<AccuracyModifier>();

	public EntityBehaviorAimingAccuracy(Entity entity)
		: base(entity)
	{
		EntityAgent agent = entity as EntityAgent;
		modifiers.Add(new BaseAimingAccuracy(agent));
		modifiers.Add(new MovingAimingAccuracy(agent));
		modifiers.Add(new SprintAimingAccuracy(agent));
		modifiers.Add(new OnHurtAimingAccuracy(agent));
		entity.Attributes.RegisterModifiedListener("aiming", OnAimingChanged);
		Rand = new Random((int)(entity.EntityId + entity.World.ElapsedMilliseconds));
	}

	private void OnAimingChanged()
	{
		bool isAiming = IsAiming;
		IsAiming = entity.Attributes.GetInt("aiming") > 0;
		if (isAiming == IsAiming)
		{
			return;
		}
		if (IsAiming && entity.World is IServerWorldAccessor)
		{
			double rndpitch = Rand.NextDouble() - 0.5;
			double rndyaw = Rand.NextDouble() - 0.5;
			entity.WatchedAttributes.SetDouble("aimingRandPitch", rndpitch);
			entity.WatchedAttributes.SetDouble("aimingRandYaw", rndyaw);
		}
		for (int i = 0; i < modifiers.Count; i++)
		{
			if (IsAiming)
			{
				modifiers[i].BeginAim();
			}
			else
			{
				modifiers[i].EndAim();
			}
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (IsAiming)
		{
			if (!entity.Alive)
			{
				entity.Attributes.SetInt("aiming", 0);
			}
			float accuracy = 0f;
			for (int i = 0; i < modifiers.Count; i++)
			{
				modifiers[i].Update(deltaTime, ref accuracy);
			}
			entity.Attributes.SetFloat("aimingAccuracy", accuracy);
		}
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		base.OnEntityReceiveDamage(damageSource, ref damage);
		if (damageSource.Type != EnumDamageType.Heal)
		{
			for (int i = 0; i < modifiers.Count; i++)
			{
				modifiers[i].OnHurt(damage);
			}
		}
	}

	public override string PropertyName()
	{
		return "aimingaccuracy";
	}
}
