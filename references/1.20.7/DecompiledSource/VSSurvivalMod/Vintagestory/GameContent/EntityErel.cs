using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityErel : EntityAgent
{
	private ILoadedSound aliveSound;

	private ILoadedSound glideSound;

	private AiTaskManager tmgr;

	private Dictionary<string, int[]> attackCooldowns = new Dictionary<string, int[]>();

	private EntityBehaviorHealth ebh;

	private Vec3d devaLocationPresent;

	private Vec3d devaLocationPast;

	private float nextFlyIdleSec = -1f;

	private float nextFlapCruiseSec = -1f;

	private float prevYaw;

	private float annoyCheckAccum;

	private bool wasAtBossFightArea;

	public override bool CanSwivel => true;

	public override bool CanSwivelNow => true;

	public override bool StoreWithChunk => false;

	public override bool AllowOutsideLoadedRange => true;

	public double LastAnnoyedTotalDays
	{
		get
		{
			return WatchedAttributes.GetDouble("lastannoyedtotaldays", -9999999.0);
		}
		set
		{
			WatchedAttributes.SetDouble("lastannoyedtotaldays", value);
		}
	}

	public bool Annoyed
	{
		get
		{
			return WatchedAttributes.GetBool("annoyed");
		}
		set
		{
			WatchedAttributes.SetBool("annoyed", value);
		}
	}

	public override bool AlwaysActive => true;

	static EntityErel()
	{
		AiTaskRegistry.Register("flycircle", typeof(AiTaskFlyCircle));
		AiTaskRegistry.Register("flycircleifentity", typeof(AiTaskFlyCircleIfEntity));
		AiTaskRegistry.Register("flycircletarget", typeof(AiTaskFlyCircleTarget));
		AiTaskRegistry.Register("flywander", typeof(AiTaskFlyWander));
		AiTaskRegistry.Register("flyswoopattack", typeof(AiTaskFlySwoopAttack));
		AiTaskRegistry.Register("flydiveattack", typeof(AiTaskFlyDiveAttack));
		AiTaskRegistry.Register("firefeathersattack", typeof(AiTaskFireFeathersAttack));
		AiTaskRegistry.Register("flyleave", typeof(AiTaskFlyLeave));
	}

	public EntityErel()
	{
		SimulationRange = 1024;
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		WatchedAttributes.SetBool("showHealthbar", value: true);
		devaLocationPresent = api.ModLoader.GetModSystem<ModSystemDevastationEffects>().DevaLocationPresent;
		devaLocationPast = api.ModLoader.GetModSystem<ModSystemDevastationEffects>().DevaLocationPast;
		if (api is ICoreClientAPI capi)
		{
			aliveSound = capi.World.LoadSound(new SoundParams
			{
				DisposeOnFinish = false,
				Location = new AssetLocation("sounds/creature/erel/alive"),
				ShouldLoop = true,
				Range = 48f
			});
			aliveSound.Start();
			glideSound = capi.World.LoadSound(new SoundParams
			{
				DisposeOnFinish = false,
				Location = new AssetLocation("sounds/creature/erel/glide"),
				ShouldLoop = true,
				Range = 24f
			});
		}
		ebh = GetBehavior<EntityBehaviorHealth>();
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		Api.ModLoader.GetModSystem<ModSystemDevastationEffects>().SetErelAnnoyed(Annoyed);
		if (World.Side == EnumAppSide.Server)
		{
			tmgr = GetBehavior<EntityBehaviorTaskAI>().TaskManager;
			tmgr.OnShouldExecuteTask += Tmgr_OnShouldExecuteTask;
			if (Annoyed)
			{
				tmgr.GetTask<AiTaskFlyLeave>().AllowExecute = true;
			}
			attackCooldowns["swoop"] = new int[2]
			{
				tmgr.GetTask<AiTaskFlySwoopAttack>().Mincooldown,
				tmgr.GetTask<AiTaskFlySwoopAttack>().Maxcooldown
			};
			attackCooldowns["dive"] = new int[2]
			{
				tmgr.GetTask<AiTaskFlyDiveAttack>().Mincooldown,
				tmgr.GetTask<AiTaskFlyDiveAttack>().Maxcooldown
			};
			attackCooldowns["feathers"] = new int[2]
			{
				tmgr.GetTask<AiTaskFireFeathersAttack>().Mincooldown,
				tmgr.GetTask<AiTaskFireFeathersAttack>().Maxcooldown
			};
		}
		updateAnnoyedState();
	}

	protected bool outSideDevaRange()
	{
		return distanceToTower() > 600.0;
	}

	protected bool inTowerRange()
	{
		return distanceToTower() < 100.0;
	}

	public double distanceToTower()
	{
		ModSystemDevastationEffects msdevaeff = Api.ModLoader.GetModSystem<ModSystemDevastationEffects>();
		Vec3d loc = ((ServerPos.Dimension == 0) ? msdevaeff.DevaLocationPresent : msdevaeff.DevaLocationPast);
		return ServerPos.DistanceTo(loc);
	}

	private bool Tmgr_OnShouldExecuteTask(IAiTask t)
	{
		if ((t is AiTaskFlySwoopAttack || t is AiTaskFlyDiveAttack || t is AiTaskFireFeathersAttack || t is AiTaskFlyCircleTarget) && outSideDevaRange())
		{
			return false;
		}
		return true;
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		aliveSound?.Dispose();
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (Api.Side == EnumAppSide.Server)
		{
			doOccasionalFlapping(dt);
			annoyCheckAccum += dt;
			if (annoyCheckAccum > 1f)
			{
				annoyCheckAccum = 0f;
				updateAnnoyedState();
				toggleBossFightModeNearTower();
				stopAttacksOutsideDevaRange();
			}
		}
		else
		{
			aliveSound.SetPosition((float)Pos.X, (float)Pos.Y, (float)Pos.Z);
			glideSound.SetPosition((float)Pos.X, (float)Pos.Y, (float)Pos.Z);
			if (AnimManager.IsAnimationActive("fly-flapactive", "fly-flapactive-fast") && glideSound.IsPlaying)
			{
				glideSound.Stop();
			}
			else if ((AnimManager.IsAnimationActive("fly-idle", "fly-flapcruise") || AnimManager.ActiveAnimationsByAnimCode.Count == 0) && !glideSound.IsPlaying)
			{
				glideSound.Start();
			}
		}
		if (!AnimManager.IsAnimationActive("dive", "slam"))
		{
			double speed = ServerPos.Motion.Length();
			if (speed > 0.01)
			{
				ServerPos.Roll = (float)Math.Asin(GameMath.Clamp((0.0 - ServerPos.Motion.Y) / speed, -1.0, 1.0));
			}
		}
	}

	private void stopAttacksOutsideDevaRange()
	{
		if (!outSideDevaRange())
		{
			return;
		}
		IAiTask[] activeTasksBySlot = tmgr.ActiveTasksBySlot;
		foreach (IAiTask t in activeTasksBySlot)
		{
			if (t is AiTaskFlySwoopAttack || t is AiTaskFlyDiveAttack || t is AiTaskFireFeathersAttack || t is AiTaskFlyCircleTarget)
			{
				tmgr.StopTask(t.GetType());
			}
		}
	}

	private void toggleBossFightModeNearTower()
	{
		ModSystemDevastationEffects msdevaeff = Api.ModLoader.GetModSystem<ModSystemDevastationEffects>();
		Vec3d loc = ((ServerPos.Dimension == 0) ? msdevaeff.DevaLocationPresent : msdevaeff.DevaLocationPast);
		WatchedAttributes.SetBool("showHealthbar", ServerPos.Y > loc.Y + 70.0);
		AiTaskFlyCircleIfEntity ctask = tmgr.GetTask<AiTaskFlyCircleIfEntity>();
		bool atBossFightArea = ctask.getEntity() != null && ServerPos.XYZ.HorizontalSquareDistanceTo(ctask.CenterPos) < 4900f;
		AiTaskFlySwoopAttack swoopAtta = tmgr.GetTask<AiTaskFlySwoopAttack>();
		AiTaskFlyDiveAttack diveAtta = tmgr.GetTask<AiTaskFlyDiveAttack>();
		AiTaskFireFeathersAttack feathersAtta = tmgr.GetTask<AiTaskFireFeathersAttack>();
		feathersAtta.Enabled = atBossFightArea;
		diveAtta.Enabled = atBossFightArea;
		if (wasAtBossFightArea && !atBossFightArea)
		{
			swoopAtta.Mincooldown = attackCooldowns["swoop"][0];
			swoopAtta.Maxcooldown = attackCooldowns["swoop"][1];
			diveAtta.Mincooldown = attackCooldowns["dive"][0];
			diveAtta.Maxcooldown = attackCooldowns["dive"][1];
			feathersAtta.Mincooldown = attackCooldowns["feathers"][0];
			feathersAtta.Maxcooldown = attackCooldowns["feathers"][1];
		}
		if (!wasAtBossFightArea && atBossFightArea)
		{
			swoopAtta.Mincooldown = attackCooldowns["swoop"][0] / 2;
			swoopAtta.Maxcooldown = attackCooldowns["swoop"][1] / 2;
			diveAtta.Mincooldown = attackCooldowns["dive"][0] / 2;
			diveAtta.Maxcooldown = attackCooldowns["dive"][1] / 2;
			feathersAtta.Mincooldown = attackCooldowns["feathers"][0] / 2;
			feathersAtta.Maxcooldown = attackCooldowns["feathers"][1] / 2;
		}
		wasAtBossFightArea = atBossFightArea;
	}

	private void updateAnnoyedState()
	{
		if (Api.Side == EnumAppSide.Client)
		{
			return;
		}
		if (!Annoyed)
		{
			if ((double)(ebh.Health / ebh.MaxHealth) < 0.6)
			{
				Api.World.PlaySoundAt("sounds/creature/erel/annoyed", this, null, randomizePitch: false, 1024f);
				AnimManager.StartAnimation("defeat");
				LastAnnoyedTotalDays = Api.World.Calendar.TotalDays;
				Annoyed = true;
				tmgr.GetTask<AiTaskFlyLeave>().AllowExecute = true;
				Api.ModLoader.GetModSystem<ModSystemDevastationEffects>().SetErelAnnoyed(on: true);
			}
		}
		else if (Api.World.Calendar.TotalDays - LastAnnoyedTotalDays > 14.0)
		{
			Annoyed = false;
			ebh.Health = ebh.MaxHealth;
			tmgr.GetTask<AiTaskFlyLeave>().AllowExecute = false;
			Api.ModLoader.GetModSystem<ModSystemDevastationEffects>().SetErelAnnoyed(on: false);
		}
	}

	private void doOccasionalFlapping(float dt)
	{
		float turnSpeed = Math.Abs(GameMath.AngleRadDistance(prevYaw, ServerPos.Yaw));
		double flyspeed = ServerPos.Motion.Length();
		if (AnimManager.IsAnimationActive("dive", "slam"))
		{
			return;
		}
		if ((ServerPos.Motion.Y >= 0.03 || (double)turnSpeed > 0.05 || flyspeed < 0.15) && (AnimManager.IsAnimationActive("fly-idle", "fly-flapcruise") || AnimManager.ActiveAnimationsByAnimCode.Count == 0))
		{
			AnimManager.StopAnimation("fly-flapcruise");
			AnimManager.StopAnimation("fly-idle");
			AnimManager.StartAnimation("fly-flapactive-fast");
			return;
		}
		if (ServerPos.Motion.Y <= 0.01 && (double)turnSpeed < 0.03 && flyspeed >= 0.25 && AnimManager.IsAnimationActive("fly-flapactive", "fly-flapactive-fast"))
		{
			AnimManager.StopAnimation("fly-flapactive");
			AnimManager.StopAnimation("fly-flapactive-fast");
			AnimManager.StartAnimation("fly-idle");
		}
		prevYaw = ServerPos.Yaw;
		if (nextFlyIdleSec > 0f)
		{
			nextFlyIdleSec -= dt;
			if (nextFlyIdleSec < 0f)
			{
				AnimManager.StopAnimation("fly-flapcruise");
				AnimManager.StartAnimation("fly-idle");
				return;
			}
		}
		if (nextFlapCruiseSec < 0f)
		{
			nextFlapCruiseSec = (float)Api.World.Rand.NextDouble() * 15f + 5f;
		}
		else if (AnimManager.IsAnimationActive("fly-idle"))
		{
			nextFlapCruiseSec -= dt;
			if (nextFlapCruiseSec < 0f)
			{
				AnimManager.StopAnimation("fly-idle");
				AnimManager.StartAnimation("fly-flapcruise");
				nextFlyIdleSec = (float)(Api.World.Rand.NextDouble() * 4.0 + 1.0) * 130f / 30f;
			}
		}
	}

	public override bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		if (!inTowerRange())
		{
			damage /= 2f;
		}
		if (World.Side == EnumAppSide.Server)
		{
			int x = nearbyPlayerCount();
			damage *= 1f / (1f + (float)Math.Sqrt((x - 1) / 4));
		}
		if (ebh != null && ebh.Health - damage < 0f)
		{
			return false;
		}
		return base.ReceiveDamage(damageSource, damage);
	}

	private int nearbyPlayerCount()
	{
		int cnt = 0;
		IPlayer[] allOnlinePlayers = World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer plr = (IServerPlayer)allOnlinePlayers[i];
			if (plr.ConnectionState == EnumClientState.Playing && plr.WorldData.CurrentGameMode == EnumGameMode.Survival)
			{
				double dx = plr.Entity.Pos.X - Pos.X;
				double dz = plr.Entity.Pos.Z - Pos.Z;
				if (Math.Abs(dx) <= 7.0 && Math.Abs(dz) <= 7.0)
				{
					cnt++;
				}
			}
		}
		return cnt;
	}
}
