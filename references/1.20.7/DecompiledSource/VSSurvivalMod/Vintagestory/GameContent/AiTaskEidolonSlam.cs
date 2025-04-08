using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class AiTaskEidolonSlam : AiTaskBaseTargetable
{
	private int durationMs;

	private int releaseAtMs;

	private long lastSearchTotalMs;

	protected int searchWaitMs = 2000;

	private float maxDist = 15f;

	private float projectileDamage;

	private int projectileDamageTier;

	private AssetLocation projectileCode;

	public float creatureSpawnChance;

	public float creatureSpawnCount = 4.5f;

	private AssetLocation creatureCode;

	public float spawnRange;

	public float spawnHeight;

	public float spawnAmount;

	private float accum;

	private bool didSpawn;

	private int creaturesLeftToSpawn;

	public AiTaskEidolonSlam(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		durationMs = taskConfig["durationMs"].AsInt(1500);
		releaseAtMs = taskConfig["releaseAtMs"].AsInt(1000);
		projectileDamage = taskConfig["projectileDamage"].AsFloat(1f);
		projectileDamageTier = taskConfig["projectileDamageTier"].AsInt();
		projectileCode = AssetLocation.Create(taskConfig["projectileCode"].AsString("thrownstone-{rock}"), entity.Code.Domain);
		if (taskConfig["creatureCode"].Exists)
		{
			creatureCode = AssetLocation.Create(taskConfig["creatureCode"].AsString(), entity.Code.Domain);
		}
		spawnRange = taskConfig["spawnRange"].AsFloat(9f);
		spawnHeight = taskConfig["spawnHeight"].AsFloat(9f);
		spawnAmount = taskConfig["spawnAmount"].AsFloat(10f);
		maxDist = taskConfig["maxDist"].AsFloat(12f);
	}

	public override bool ShouldExecute()
	{
		if (base.rand.NextDouble() > 0.10000000149011612 && (whenInEmotionState == null || !IsInEmotionState(whenInEmotionState)))
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		if (lastSearchTotalMs + searchWaitMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (whenInEmotionState == null && base.rand.NextDouble() > 0.5)
		{
			return false;
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		float range = maxDist;
		lastSearchTotalMs = entity.World.ElapsedMilliseconds;
		targetEntity = partitionUtil.GetNearestEntity(entity.ServerPos.XYZ, range, (Entity e) => IsTargetableEntity(e, range), EnumEntitySearchType.Creatures);
		return targetEntity != null;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		accum = 0f;
		didSpawn = false;
	}

	public override bool ContinueExecute(float dt)
	{
		base.ContinueExecute(dt);
		if (animMeta != null)
		{
			animMeta.EaseInSpeed = 1f;
			animMeta.EaseOutSpeed = 1f;
			entity.AnimManager.StartAnimation(animMeta);
		}
		accum += dt;
		Vec3d pos = entity.Pos.XYZ;
		float damage = 6f;
		if (accum > (float)releaseAtMs / 1000f && !didSpawn)
		{
			didSpawn = true;
			Random rnd = entity.World.Rand;
			if (entity.World.Rand.NextDouble() < (double)creatureSpawnChance)
			{
				int count = 0;
				partitionUtil.WalkEntities(pos, 7.0, delegate(Entity e)
				{
					if (e.Code.Equals(creatureCode) && e.Alive)
					{
						count++;
					}
					return true;
				}, EnumEntitySearchType.Creatures);
				creaturesLeftToSpawn = Math.Max(0, GameMath.RoundRandom(entity.World.Rand, creatureSpawnCount) - count);
			}
			for (int i = 0; (float)i < spawnAmount; i++)
			{
				float dx2 = (float)rnd.NextDouble() * 2f * spawnRange - spawnRange;
				float dz2 = (float)rnd.NextDouble() * 2f * spawnRange - spawnRange;
				float dy = spawnHeight;
				spawnProjectile(dx2, dy, dz2);
			}
			partitionUtil.WalkEntities(pos, 9.0, delegate(Entity e)
			{
				if (e.EntityId == entity.EntityId || !e.IsInteractable)
				{
					return true;
				}
				if (!e.Alive || !e.OnGround)
				{
					return true;
				}
				double num = e.Pos.DistanceTo(pos);
				float num2 = (float)(5.0 - num) / 5f;
				float damage2 = Math.Max(0.02f, damage * GlobalConstants.CreatureDamageModifier * num2);
				e.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Entity,
					SourceEntity = entity,
					Type = EnumDamageType.BluntAttack,
					DamageTier = 1,
					KnockbackStrength = 0f
				}, damage2);
				float num3 = GameMath.Clamp(10f - (float)num, 0f, 5f);
				Vec3d vec3d = (entity.ServerPos.XYZ - e.ServerPos.XYZ).Normalize();
				vec3d.Y = 0.699999988079071;
				float num4 = num3 * GameMath.Clamp((1f - e.Properties.KnockbackResistance) / 10f, 0f, 1f);
				e.WatchedAttributes.SetFloat("onHurtDir", (float)Math.Atan2(vec3d.X, vec3d.Z));
				e.WatchedAttributes.SetDouble("kbdirX", vec3d.X * (double)num4);
				e.WatchedAttributes.SetDouble("kbdirY", vec3d.Y * (double)num4);
				e.WatchedAttributes.SetDouble("kbdirZ", vec3d.Z * (double)num4);
				e.WatchedAttributes.SetInt("onHurtCounter", e.WatchedAttributes.GetInt("onHurtCounter") + 1);
				e.WatchedAttributes.SetFloat("onHurt", 0.01f);
				return true;
			}, EnumEntitySearchType.Creatures);
			IPlayer[] allOnlinePlayers = entity.World.AllOnlinePlayers;
			for (int j = 0; j < allOnlinePlayers.Length; j++)
			{
				IServerPlayer plr = (IServerPlayer)allOnlinePlayers[j];
				if (plr.ConnectionState == EnumClientState.Playing)
				{
					double dx = plr.Entity.Pos.X - entity.Pos.X;
					double dz = plr.Entity.Pos.Z - entity.Pos.Z;
					if (Math.Abs(dx) <= (double)spawnRange && Math.Abs(dz) <= (double)spawnRange)
					{
						spawnProjectile((float)dx, spawnHeight, (float)dz);
					}
				}
			}
			entity.World.Api.ModLoader.GetModSystem<ScreenshakeToClientModSystem>().ShakeScreen(entity.Pos.XYZ, 1f, 16f);
		}
		return accum < (float)durationMs / 1000f;
	}

	private void spawnProjectile(float dx, float dy, float dz)
	{
		if (creaturesLeftToSpawn > 0)
		{
			spawnCreature(dx, dy, dz);
			return;
		}
		AssetLocation loc = projectileCode.Clone();
		string rocktype = "granite";
		IMapChunk mc = entity.World.BlockAccessor.GetMapChunkAtBlockPos(entity.Pos.AsBlockPos);
		if (mc != null)
		{
			int lz = (int)entity.Pos.Z % 32;
			int lx = (int)entity.Pos.X % 32;
			rocktype = entity.World.Blocks[mc.TopRockIdMap[lz * 32 + lx]].Variant["rock"] ?? "granite";
		}
		loc.Path = loc.Path.Replace("{rock}", rocktype);
		EntityProperties type = entity.World.GetEntityType(loc);
		EntityThrownStone entitypr = entity.World.ClassRegistry.CreateEntity(type) as EntityThrownStone;
		entitypr.FiredBy = entity;
		entitypr.Damage = projectileDamage;
		entitypr.DamageTier = projectileDamageTier;
		entitypr.ProjectileStack = new ItemStack(entity.World.GetItem(new AssetLocation("stone-" + rocktype)));
		entitypr.NonCollectible = true;
		entitypr.VerticalImpactBreakChance = 1f;
		entitypr.ImpactParticleSize = 1.5f;
		entitypr.ImpactParticleCount = 30;
		entitypr.ServerPos.SetPosWithDimension(entity.ServerPos.XYZ.Add(dx, dy, dz));
		entitypr.Pos.SetFrom(entitypr.ServerPos);
		entitypr.World = entity.World;
		entity.World.SpawnEntity(entitypr);
	}

	private void spawnCreature(float dx, float dy, float dz)
	{
		EntityProperties type = entity.World.GetEntityType(creatureCode);
		Entity entitypr = entity.World.ClassRegistry.CreateEntity(type);
		entitypr.ServerPos.SetPosWithDimension(entity.ServerPos.XYZ.Add(dx, dy, dz));
		entitypr.Pos.SetFrom(entitypr.ServerPos);
		entitypr.World = entity.World;
		entity.World.SpawnEntity(entitypr);
		creaturesLeftToSpawn--;
	}
}
