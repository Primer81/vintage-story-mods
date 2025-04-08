using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorMortallyWoundable : EntityBehavior
{
	private float remainAliveHours;

	private float damageRequiredForFullDeath;

	private float healingRequiredForRescue;

	private float whenBelowHealth;

	private EntityBehaviorHealth ebh;

	private float accum;

	public EnumEntityHealthState HealthState
	{
		get
		{
			return (EnumEntityHealthState)entity.WatchedAttributes.GetInt("healthState");
		}
		set
		{
			entity.WatchedAttributes.SetInt("healthState", (int)value);
		}
	}

	public double MortallyWoundedTotalHours
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("mortallyWoundedTotalHours");
		}
		set
		{
			entity.WatchedAttributes.SetDouble("mortallyWoundedTotalHours", value);
		}
	}

	public double HealthHealed
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("healthHealed");
		}
		set
		{
			entity.WatchedAttributes.SetDouble("healthHealed", value);
		}
	}

	public double HealthDamaged
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("healthDamaged");
		}
		set
		{
			entity.WatchedAttributes.SetDouble("healthDamaged", value);
		}
	}

	public EntityBehaviorMortallyWoundable(Entity entity)
		: base(entity)
	{
		if (!(entity is EntityAgent))
		{
			throw new InvalidOperationException("MortallyWoundable behavior is only possible on entities deriving from EntityAgent");
		}
		(entity as EntityAgent).AllowDespawn = false;
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		remainAliveHours = typeAttributes["remainAliveHours"].AsFloat(24f);
		damageRequiredForFullDeath = typeAttributes["remainingHealth"].AsFloat(10f);
		healingRequiredForRescue = typeAttributes["healingRequiredForRescue"].AsFloat(15f);
		whenBelowHealth = typeAttributes["whenBelowHealth"].AsFloat(5f);
		entity.AnimManager.OnStartAnimation += AnimManager_OnStartAnimation;
		entity.AnimManager.OnAnimationReceived += AnimManager_OnAnimationReceived;
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		if (entity.World.Side == EnumAppSide.Server)
		{
			ebh = entity.GetBehavior<EntityBehaviorHealth>();
			entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.OnShouldExecuteTask += (IAiTask t) => HealthState != EnumEntityHealthState.MortallyWounded && HealthState != EnumEntityHealthState.Recovering;
			if (HealthState == EnumEntityHealthState.MortallyWounded)
			{
				setMortallyWounded();
			}
		}
		entity.GetBehavior<EntityBehaviorSeatable>().CanSit += EntityBehaviorMortallyWoundable_CanSit;
	}

	private bool EntityBehaviorMortallyWoundable_CanSit(EntityAgent eagent, out string errorMessage)
	{
		errorMessage = null;
		return HealthState == EnumEntityHealthState.Normal;
	}

	private bool AnimManager_OnAnimationReceived(ref AnimationMetaData animationMeta, ref EnumHandling handling)
	{
		if (HealthState != 0 && animationMeta.Animation == "die")
		{
			animationMeta = entity.Properties.Client.Animations.FirstOrDefault((AnimationMetaData m) => m.Animation == "dead");
		}
		return true;
	}

	private bool AnimManager_OnStartAnimation(ref AnimationMetaData animationMeta, ref EnumHandling handling)
	{
		if (HealthState == EnumEntityHealthState.MortallyWounded && !animationMeta.Animation.StartsWith("wounded-") && animationMeta.Animation != "die")
		{
			handling = EnumHandling.PreventDefault;
			return true;
		}
		if (HealthState != 0 && animationMeta.Animation == "die")
		{
			animationMeta = entity.Properties.Client.Animations.FirstOrDefault((AnimationMetaData m) => m.Animation == "dead");
		}
		return false;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!entity.Alive || entity.ShouldDespawn || HealthState == EnumEntityHealthState.Normal || entity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		if (HealthState == EnumEntityHealthState.Recovering && (!entity.AnimManager.IsAnimationActive("wounded-stand") || entity.AnimManager.GetAnimationState("wounded-stand").AnimProgress > 0.9f))
		{
			HealthState = EnumEntityHealthState.Normal;
		}
		if (entity.World.Rand.NextDouble() < 0.03 && HealthState == EnumEntityHealthState.MortallyWounded && entity.World.Calendar.TotalHours > MortallyWoundedTotalHours + (double)remainAliveHours)
		{
			Die();
		}
		if ((entity.World.Calendar.TotalHours - MortallyWoundedTotalHours) / (double)remainAliveHours > 0.8299999833106995)
		{
			if (entity.AnimManager.IsAnimationActive("wounded-idle"))
			{
				entity.AnimManager.StopAnimation("wounded-idle");
				entity.AnimManager.StartAnimation("wounded-resthead");
			}
			return;
		}
		accum += deltaTime;
		if (accum > 7f && entity.World.Rand.NextDouble() < 0.005)
		{
			string[] anims = new string[3] { "wounded-look", "wounded-spasm", "wounded-trystand" };
			entity.AnimManager.StartAnimation(anims[entity.World.Rand.Next(anims.Length)]);
			accum = 0f;
		}
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		if (entity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		if (ebh.Health - damage <= 0f && HealthState == EnumEntityHealthState.Normal)
		{
			damage = ebh.Health - whenBelowHealth;
			HealthState = EnumEntityHealthState.MortallyWounded;
			MortallyWoundedTotalHours = entity.World.Calendar.TotalHours;
			setMortallyWounded();
		}
		else if (HealthState == EnumEntityHealthState.MortallyWounded && damageSource.Type == EnumDamageType.Heal)
		{
			HealthHealed += damage;
			if (HealthHealed > (double)healingRequiredForRescue)
			{
				recover();
			}
		}
	}

	private void recover()
	{
		HealthState = EnumEntityHealthState.Recovering;
		entity.WatchedAttributes.SetFloat("regenSpeed", 1f);
		entity.AnimManager?.StopAnimation("wounded-idle");
		entity.AnimManager?.StopAnimation("wounded-resthead");
		entity.AnimManager?.StartAnimation("wounded-stand");
		entity.AnimManager?.StartAnimation("idle");
		List<IAiTask> tasks = entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.AllTasks;
		if (tasks != null)
		{
			foreach (IAiTask item in tasks)
			{
				(item as AiTaskBaseTargetable)?.ClearAttacker();
			}
		}
		entity.GetBehavior<EntityBehaviorEmotionStates>()?.ClearStates();
	}

	private void setMortallyWounded()
	{
		entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.StopTasks();
		(entity as EntityAgent).Controls.StopAllMovement();
		entity.GetBehavior<EntityBehaviorRideable>()?.UnmnountPassengers();
		entity.AnimManager?.StartAnimation("wounded-idle");
		entity.WatchedAttributes.SetFloat("regenSpeed", 0f);
	}

	private void Die()
	{
		HealthState = EnumEntityHealthState.Dead;
		entity.Die();
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		if (HealthState == EnumEntityHealthState.MortallyWounded && entity.Alive)
		{
			double hoursleft = MortallyWoundedTotalHours + (double)remainAliveHours - entity.World.Calendar.TotalHours;
			if (hoursleft < 1.0)
			{
				infotext.AppendLine(Lang.Get("Mortally wounded, alive for less than one hour."));
				return;
			}
			infotext.AppendLine(Lang.Get("Mortally wounded, alive for {0} more hours", (int)hoursleft));
		}
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		return base.GetInteractionHelp(world, es, player, ref handled);
	}

	public override string PropertyName()
	{
		return "mortallywoundable";
	}
}
