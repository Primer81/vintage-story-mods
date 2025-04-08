using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityShiver : EntityAgent, IMeleeAttackListener
{
	private int mouthType;

	private string mouthOpen;

	private string mouthIdle;

	private string mouthClose;

	private string mouthAttack;

	private AiTaskManager aiTaskManager;

	private bool strokeActive;

	private long callbackid;

	protected float hitAndRunChance;

	public override bool AdjustCollisionBoxToAnimation
	{
		get
		{
			if (!base.AdjustCollisionBoxToAnimation)
			{
				return AnimManager.IsAnimationActive("stroke-start", "stroke-idle", "stroke-end");
			}
			return true;
		}
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		hitAndRunChance = base.Properties.Attributes["hitAndRunChance"].AsFloat();
		if (WatchedAttributes.HasAttribute("mouthType"))
		{
			mouthType = WatchedAttributes.GetInt("mouthType");
		}
		else
		{
			WatchedAttributes.SetInt("mouthType", mouthType = Api.World.Rand.Next(3));
		}
		mouthOpen = "mouth-open" + (mouthType + 1);
		mouthIdle = "mouth-idle" + (mouthType + 1);
		mouthClose = "mouth-close" + (mouthType + 1);
		mouthAttack = "mouth-attack" + (mouthType + 1);
		if (Api.Side == EnumAppSide.Server)
		{
			aiTaskManager = GetBehavior<EntityBehaviorTaskAI>().TaskManager;
			aiTaskManager.OnTaskStarted += TaskManager_OnTaskStarted;
			aiTaskManager.OnTaskStopped += TaskManager_OnTaskStopped;
			aiTaskManager.OnShouldExecuteTask += AiTaskManager_OnShouldExecuteTask;
		}
	}

	private bool AiTaskManager_OnShouldExecuteTask(IAiTask t)
	{
		return !strokeActive;
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (!Alive && Api.Side == EnumAppSide.Server && AnimManager.IsAnimationActive("stroke-start", "stroke-idle", "stroke-end", "despair"))
		{
			AnimManager.StopAnimation("stroke-start");
			AnimManager.StopAnimation("stroke-idle");
			AnimManager.StopAnimation("stroke-end");
		}
		if (!Alive || Api.Side != EnumAppSide.Server || !(Api.World.Rand.NextDouble() < 0.0008) || AnimManager.IsAnimationActive("stroke-start", "stroke-idle", "stroke-end", "despair"))
		{
			return;
		}
		strokeActive = true;
		aiTaskManager.StopTasks();
		AnimManager.StartAnimation("stroke-start");
		World.PlaySoundAt(new AssetLocation("sounds/creature/shiver/shock"), this, null, randomizePitch: true, 16f);
		Api.Event.RegisterCallback(delegate
		{
			AnimManager.StartAnimation("stroke-idle");
		}, 666);
		Api.Event.RegisterCallback(delegate
		{
			AnimManager.StopAnimation("stroke-idle");
			AnimManager.StartAnimation("stroke-end");
			Api.Event.RegisterCallback(delegate
			{
				strokeActive = false;
			}, 1200);
		}, (int)(Api.World.Rand.NextDouble() * 3.0 + 3.0) * 1000);
	}

	private void TaskManager_OnTaskStopped(IAiTask task)
	{
		if (task is AiTaskSeekEntity)
		{
			Api.Event.UnregisterCallback(callbackid);
			callbackid = 0L;
			AnimManager.StopAnimation(mouthOpen);
			AnimManager.StopAnimation(mouthIdle);
			AnimManager.StopAnimation(mouthClose);
		}
		if (task is AiTaskMeleeAttack)
		{
			AnimManager.StartAnimation(mouthAttack);
		}
	}

	private void TaskManager_OnTaskStarted(IAiTask task)
	{
		if (task is AiTaskSeekEntity)
		{
			AnimManager.StartAnimation(mouthOpen);
			callbackid = Api.Event.RegisterCallback(delegate
			{
				AnimManager.StartAnimation(mouthIdle);
			}, 1700);
		}
	}

	public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
	{
		base.Die(reason, damageSourceForDeath);
		AnimManager.StopAnimation(mouthOpen);
		AnimManager.StopAnimation(mouthIdle);
		AnimManager.StopAnimation(mouthClose);
	}

	public void DidAttack(Entity targetEntity)
	{
		if (!targetEntity.Alive)
		{
			return;
		}
		if (World.Rand.NextDouble() < (double)hitAndRunChance)
		{
			GetBehavior<EntityBehaviorEmotionStates>()?.TryTriggerState("fleeondamage", 0.0, targetEntity.EntityId);
			return;
		}
		Api.Event.RegisterCallback(delegate
		{
			EntityPos entityPos = targetEntity.ServerPos.Copy();
			entityPos.Yaw -= (float)Math.PI / 2f * (float)(1 - 2 * Api.World.Rand.Next(2));
			Vec3d xYZ = entityPos.AheadCopy(4.0).XYZ;
			double num = (xYZ.X + targetEntity.ServerPos.Motion.X * 80.0 - ServerPos.X) / 30.0;
			double num2 = (xYZ.Z + targetEntity.ServerPos.Motion.Z * 80.0 - ServerPos.Z) / 30.0;
			ServerPos.Motion.Add(num, GameMath.Max(0.13, (targetEntity.ServerPos.Y - ServerPos.Y) / 30.0), num2);
			float yaw = (float)Math.Atan2(num, num2);
			ServerPos.Yaw = yaw;
		}, 500);
	}
}
