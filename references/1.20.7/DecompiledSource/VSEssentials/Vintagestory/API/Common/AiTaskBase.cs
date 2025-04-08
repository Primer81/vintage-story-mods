using System;
using System.Linq;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Essentials;
using Vintagestory.GameContent;

namespace Vintagestory.API.Common;

public abstract class AiTaskBase : IAiTask
{
	[ThreadStatic]
	private static Random randTL;

	public EntityAgent entity;

	public IWorldAccessor world;

	public AnimationMetaData animMeta;

	protected float priority;

	protected float priorityForCancel;

	protected int slot;

	public int Mincooldown;

	public int Maxcooldown;

	protected double mincooldownHours;

	protected double maxcooldownHours;

	protected AssetLocation finishSound;

	protected AssetLocation sound;

	protected float soundRange = 16f;

	protected int soundStartMs;

	protected int soundRepeatMs;

	protected float soundChance = 1.01f;

	protected long lastSoundTotalMs;

	protected string whenInEmotionState;

	protected bool? whenSwimming;

	protected string whenNotInEmotionState;

	protected long cooldownUntilMs;

	protected double cooldownUntilTotalHours;

	protected WaypointsTraverser pathTraverser;

	protected EntityBehaviorEmotionStates bhEmo;

	private string profilerName;

	public Random rand => randTL ?? (randTL = new Random());

	public string Id { get; set; }

	public string ProfilerName
	{
		get
		{
			return profilerName;
		}
		set
		{
			profilerName = value;
		}
	}

	public virtual int Slot => slot;

	public virtual float Priority
	{
		get
		{
			return priority;
		}
		set
		{
			priority = value;
		}
	}

	public virtual float PriorityForCancel => priorityForCancel;

	public AiTaskBase(EntityAgent entity)
	{
		this.entity = entity;
		world = entity.World;
		if (randTL == null)
		{
			randTL = new Random((int)entity.EntityId);
		}
		pathTraverser = entity.GetBehavior<EntityBehaviorTaskAI>().PathTraverser;
		bhEmo = entity.GetBehavior<EntityBehaviorEmotionStates>();
	}

	public virtual void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		priority = taskConfig["priority"].AsFloat();
		priorityForCancel = taskConfig["priorityForCancel"].AsFloat(priority);
		Id = taskConfig["id"].AsString();
		slot = (taskConfig["slot"]?.AsInt()).Value;
		Mincooldown = (taskConfig["mincooldown"]?.AsInt()).Value;
		Maxcooldown = (taskConfig["maxcooldown"]?.AsInt(100)).Value;
		mincooldownHours = (taskConfig["mincooldownHours"]?.AsDouble()).Value;
		maxcooldownHours = (taskConfig["maxcooldownHours"]?.AsDouble()).Value;
		int initialmincooldown = (taskConfig["initialMinCoolDown"]?.AsInt(Mincooldown)).Value;
		int initialmaxcooldown = (taskConfig["initialMaxCoolDown"]?.AsInt(Maxcooldown)).Value;
		JsonObject animationCfg = taskConfig["animation"];
		if (animationCfg.Exists)
		{
			string code = animationCfg.AsString()?.ToLowerInvariant();
			JsonObject animationSpeedCfg = taskConfig["animationSpeed"];
			float speed = animationSpeedCfg.AsFloat(1f);
			AnimationMetaData cmeta = entity.Properties.Client.Animations.FirstOrDefault((AnimationMetaData a) => a.Code == code);
			if (cmeta != null)
			{
				if (animationSpeedCfg.Exists)
				{
					animMeta = cmeta.Clone();
					animMeta.AnimationSpeed = speed;
				}
				else
				{
					animMeta = cmeta;
				}
			}
			else
			{
				animMeta = new AnimationMetaData
				{
					Code = code,
					Animation = code,
					AnimationSpeed = speed
				}.Init();
				animMeta.EaseInSpeed = 1f;
				animMeta.EaseOutSpeed = 1f;
			}
		}
		whenSwimming = taskConfig["whenSwimming"]?.AsBool();
		whenInEmotionState = taskConfig["whenInEmotionState"].AsString();
		whenNotInEmotionState = taskConfig["whenNotInEmotionState"].AsString();
		JsonObject soundCfg = taskConfig["sound"];
		if (soundCfg.Exists)
		{
			sound = AssetLocation.Create(soundCfg.AsString(), entity.Code.Domain).WithPathPrefixOnce("sounds/");
			soundRange = taskConfig["soundRange"].AsFloat(16f);
			soundStartMs = taskConfig["soundStartMs"].AsInt();
			soundRepeatMs = taskConfig["soundRepeatMs"].AsInt();
		}
		JsonObject finishSoundCfg = taskConfig["finishSound"];
		if (finishSoundCfg.Exists)
		{
			finishSound = AssetLocation.Create(finishSoundCfg.AsString(), entity.Code.Domain).WithPathPrefixOnce("sounds/");
		}
		cooldownUntilMs = entity.World.ElapsedMilliseconds + initialmincooldown + entity.World.Rand.Next(initialmaxcooldown - initialmincooldown);
	}

	protected bool PreconditionsSatisifed()
	{
		if (whenSwimming.HasValue && whenSwimming != entity.Swimming)
		{
			return false;
		}
		if (whenInEmotionState != null && !IsInEmotionState(whenInEmotionState))
		{
			return false;
		}
		if (whenNotInEmotionState != null && IsInEmotionState(whenNotInEmotionState))
		{
			return false;
		}
		return true;
	}

	protected bool IsInEmotionState(string emostate)
	{
		if (bhEmo == null)
		{
			return false;
		}
		if (emostate.ContainsFast('|'))
		{
			string[] states = emostate.Split("|");
			for (int i = 0; i < states.Length; i++)
			{
				if (bhEmo.IsInEmotionState(states[i]))
				{
					return true;
				}
			}
			return false;
		}
		return bhEmo.IsInEmotionState(emostate);
	}

	public virtual void AfterInitialize()
	{
	}

	public abstract bool ShouldExecute();

	public virtual void StartExecute()
	{
		if (animMeta != null)
		{
			entity.AnimManager.StartAnimation(animMeta);
		}
		if (!(sound != null) || !(entity.World.Rand.NextDouble() <= (double)soundChance))
		{
			return;
		}
		if (soundStartMs > 0)
		{
			entity.World.RegisterCallback(delegate
			{
				entity.World.PlaySoundAt(sound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, randomizePitch: true, soundRange);
				lastSoundTotalMs = entity.World.ElapsedMilliseconds;
			}, soundStartMs);
		}
		else
		{
			entity.World.PlaySoundAt(sound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, randomizePitch: true, soundRange);
			lastSoundTotalMs = entity.World.ElapsedMilliseconds;
		}
	}

	public virtual bool ContinueExecute(float dt)
	{
		if (sound != null && soundRepeatMs > 0 && entity.World.ElapsedMilliseconds > lastSoundTotalMs + soundRepeatMs)
		{
			entity.World.PlaySoundAt(sound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, randomizePitch: true, soundRange);
			lastSoundTotalMs = entity.World.ElapsedMilliseconds;
		}
		return true;
	}

	public virtual void FinishExecute(bool cancelled)
	{
		cooldownUntilMs = entity.World.ElapsedMilliseconds + Mincooldown + entity.World.Rand.Next(Maxcooldown - Mincooldown);
		cooldownUntilTotalHours = entity.World.Calendar.TotalHours + mincooldownHours + entity.World.Rand.NextDouble() * (maxcooldownHours - mincooldownHours);
		if (animMeta != null && animMeta.Code != "attack" && animMeta.Code != "idle")
		{
			entity.AnimManager.StopAnimation(animMeta.Code);
		}
		if (finishSound != null)
		{
			entity.World.PlaySoundAt(finishSound, entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, null, randomizePitch: true, soundRange);
		}
	}

	public virtual void OnStateChanged(EnumEntityState beforeState)
	{
		if (entity.State == EnumEntityState.Active)
		{
			cooldownUntilMs = entity.World.ElapsedMilliseconds + Mincooldown + entity.World.Rand.Next(Maxcooldown - Mincooldown);
		}
	}

	public virtual bool Notify(string key, object data)
	{
		return false;
	}

	public virtual void OnEntityLoaded()
	{
	}

	public virtual void OnEntitySpawn()
	{
	}

	public virtual void OnEntityDespawn(EntityDespawnData reason)
	{
	}

	public virtual void OnEntityHurt(DamageSource source, float damage)
	{
	}

	public virtual void OnNoPath(Vec3d target)
	{
	}

	public virtual bool CanContinueExecute()
	{
		return true;
	}
}
