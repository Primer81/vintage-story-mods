using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityEidolon : EntityAgent
{
	private ILoadedSound activeSound;

	private AiTaskEidolonSlam slamTask;

	private EntityBehaviorHealth bhHealth;

	private HashSet<string> hurtByPlayerUids = new HashSet<string>();

	private bool IsAsleep
	{
		get
		{
			AiTaskManager tm = GetBehavior<EntityBehaviorTaskAI>()?.TaskManager;
			if (tm == null)
			{
				return false;
			}
			IAiTask[] activeTasksBySlot = tm.ActiveTasksBySlot;
			for (int i = 0; i < activeTasksBySlot.Length; i++)
			{
				if (activeTasksBySlot[i]?.Id == "inactive")
				{
					return true;
				}
			}
			return false;
		}
	}

	static EntityEidolon()
	{
		AiTaskRegistry.Register("eidolonslam", typeof(AiTaskEidolonSlam));
		AiTaskRegistry.Register("eidolonmeleeattack", typeof(AiTaskEidolonMeleeAttack));
	}

	public EntityEidolon()
	{
		AnimManager = new EidolonAnimManager();
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		Notify("starttask", "inactive");
		WatchedAttributes.SetBool("showHealthbar", value: false);
		AllowDespawn = false;
		if (api.Side == EnumAppSide.Server)
		{
			slamTask = GetBehavior<EntityBehaviorTaskAI>().TaskManager.GetTask<AiTaskEidolonSlam>();
			bhHealth = GetBehavior<EntityBehaviorHealth>();
		}
	}

	public override void OnGameTick(float dt)
	{
		if (Api is ICoreClientAPI capi)
		{
			bool nowActive = Alive && !AnimManager.IsAnimationActive("inactive");
			bool wasActive = activeSound != null && activeSound.IsPlaying;
			if (nowActive && !wasActive)
			{
				if (activeSound == null)
				{
					activeSound = capi.World.LoadSound(new SoundParams
					{
						Location = new AssetLocation("sounds/creature/eidolon/awake"),
						DisposeOnFinish = false,
						ShouldLoop = true,
						Position = Pos.XYZ.ToVec3f(),
						SoundType = EnumSoundType.Entity,
						Volume = 0f,
						Range = 16f
					});
				}
				activeSound.Start();
				activeSound.FadeTo(0.10000000149011612, 0.5f, delegate
				{
				});
			}
			if (!nowActive && wasActive)
			{
				activeSound.FadeOutAndStop(2.5f);
			}
			GetBehavior<EntityBehaviorBoss>().ShouldPlayTrack = nowActive && capi.World.Player.Entity.Pos.DistanceTo(Pos) < 15.0;
		}
		else if (slamTask.creatureSpawnChance <= 0f && (double)(bhHealth.Health / bhHealth.MaxHealth) < 0.5)
		{
			slamTask.creatureSpawnChance = 0.3f;
		}
		else
		{
			slamTask.creatureSpawnChance = 0f;
		}
		base.OnGameTick(dt);
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		activeSound?.Stop();
		activeSound?.Dispose();
	}

	public override void Revive()
	{
		base.Revive();
		hurtByPlayerUids.Clear();
	}

	public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
	{
		if (World.Side == EnumAppSide.Server)
		{
			string uid = (damageSource.CauseEntity as EntityPlayer)?.PlayerUID;
			if (uid != null)
			{
				hurtByPlayerUids.Add(uid);
			}
		}
		return base.ShouldReceiveDamage(damageSource, damage);
	}

	public override bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		Entity sourceEntity = damageSource.SourceEntity;
		if (sourceEntity != null && sourceEntity.Code.PathStartsWith("thrownboulder"))
		{
			return false;
		}
		if (IsAsleep && damageSource.Type != EnumDamageType.Heal)
		{
			return false;
		}
		if (World.Side == EnumAppSide.Server)
		{
			int x = nearbyPlayerCount();
			damage *= 1f / (1f + (float)Math.Sqrt((x - 1) / 2));
		}
		damageSource.KnockbackStrength = 0f;
		return base.ReceiveDamage(damageSource, damage);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		ItemStack[] drops = base.GetDrops(world, pos, byPlayer);
		drops[0].StackSize = Math.Max(1, hurtByPlayerUids.Count);
		return drops;
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
