using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class SystemTemporalStability : ModSystem
{
	private IServerNetworkChannel serverChannel;

	private IClientNetworkChannel clientChannel;

	private SimplexNoise stabilityNoise;

	private ICoreAPI api;

	private ICoreServerAPI sapi;

	private bool temporalStabilityEnabled;

	private bool stormsEnabled;

	private Dictionary<string, TemporalStormConfig> configs;

	private Dictionary<EnumTempStormStrength, TemporalStormText> texts;

	private TemporalStormConfig config;

	private TemporalStormRunTimeData data = new TemporalStormRunTimeData();

	private TempStormMobConfig mobConfig;

	private ModSystemRifts riftSys;

	public float modGlitchStrength;

	public HashSet<AssetLocation> stormMobCache = new HashSet<AssetLocation>();

	private string worldConfigStorminess;

	private CollisionTester collisionTester = new CollisionTester();

	private long spawnBreakUntilMs;

	private int nobreakSpawns;

	private Dictionary<string, Dictionary<string, int>> rareSpawnsCountByCodeByPlayer = new Dictionary<string, Dictionary<string, int>>();

	public float StormStrength
	{
		get
		{
			if (data.nowStormActive)
			{
				return data.stormGlitchStrength;
			}
			return 0f;
		}
	}

	public TemporalStormRunTimeData StormData => data;

	public event GetTemporalStabilityDelegate OnGetTemporalStability;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		riftSys = api.ModLoader.GetModSystem<ModSystemRifts>();
		texts = new Dictionary<EnumTempStormStrength, TemporalStormText>
		{
			{
				EnumTempStormStrength.Light,
				new TemporalStormText
				{
					Approaching = Lang.Get("A light temporal storm is approaching"),
					Imminent = Lang.Get("A light temporal storm is imminent"),
					Waning = Lang.Get("The temporal storm seems to be waning")
				}
			},
			{
				EnumTempStormStrength.Medium,
				new TemporalStormText
				{
					Approaching = Lang.Get("A medium temporal storm is approaching"),
					Imminent = Lang.Get("A medium temporal storm is imminent"),
					Waning = Lang.Get("The temporal storm seems to be waning")
				}
			},
			{
				EnumTempStormStrength.Heavy,
				new TemporalStormText
				{
					Approaching = Lang.Get("A heavy temporal storm is approaching"),
					Imminent = Lang.Get("A heavy temporal storm is imminent"),
					Waning = Lang.Get("The temporal storm seems to be waning")
				}
			}
		};
		configs = new Dictionary<string, TemporalStormConfig>
		{
			{
				"veryrare",
				new TemporalStormConfig
				{
					Frequency = NatFloat.create(EnumDistribution.UNIFORM, 30f, 5f),
					StrengthIncrease = 0.025f,
					StrengthIncreaseCap = 0.25f
				}
			},
			{
				"rare",
				new TemporalStormConfig
				{
					Frequency = NatFloat.create(EnumDistribution.UNIFORM, 25f, 5f),
					StrengthIncrease = 0.05f,
					StrengthIncreaseCap = 0.5f
				}
			},
			{
				"sometimes",
				new TemporalStormConfig
				{
					Frequency = NatFloat.create(EnumDistribution.UNIFORM, 15f, 5f),
					StrengthIncrease = 0.1f,
					StrengthIncreaseCap = 1f
				}
			},
			{
				"often",
				new TemporalStormConfig
				{
					Frequency = NatFloat.create(EnumDistribution.UNIFORM, 7.5f, 2.5f),
					StrengthIncrease = 0.15f,
					StrengthIncreaseCap = 1.5f
				}
			},
			{
				"veryoften",
				new TemporalStormConfig
				{
					Frequency = NatFloat.create(EnumDistribution.UNIFORM, 4.5f, 1.5f),
					StrengthIncrease = 0.2f,
					StrengthIncreaseCap = 2f
				}
			}
		};
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		api.Event.BlockTexturesLoaded += LoadNoise;
		clientChannel = api.Network.RegisterChannel("temporalstability").RegisterMessageType(typeof(TemporalStormRunTimeData)).SetMessageHandler<TemporalStormRunTimeData>(onServerData);
	}

	private void onServerData(TemporalStormRunTimeData data)
	{
		this.data = data;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.ChatCommands.Create("nexttempstorm").WithDescription("Tells you the amount of days until the next storm").RequiresPrivilege(Privilege.controlserver)
			.HandleWith(OnCmdNextStorm)
			.BeginSubCommand("now")
			.WithDescription("Start next temporal storm now")
			.HandleWith(delegate
			{
				data.nextStormTotalDays = api.World.Calendar.TotalDays;
				return TextCommandResult.Success();
			})
			.EndSubCommand();
		serverChannel = api.Network.RegisterChannel("temporalstability").RegisterMessageType(typeof(TemporalStormRunTimeData));
		api.Event.SaveGameLoaded += delegate
		{
			bool flag = sapi.WorldManager.SaveGame.IsNew;
			if (!sapi.World.Config.HasAttribute("temporalStability"))
			{
				string playStyle = sapi.WorldManager.SaveGame.PlayStyle;
				if (playStyle == "surviveandbuild" || playStyle == "wildernesssurvival")
				{
					sapi.WorldManager.SaveGame.WorldConfiguration.SetBool("temporalStability", value: true);
				}
			}
			if (!sapi.World.Config.HasAttribute("temporalStorms"))
			{
				string playStyle2 = sapi.WorldManager.SaveGame.PlayStyle;
				if (playStyle2 == "surviveandbuild" || playStyle2 == "wildernesssurvival")
				{
					sapi.WorldManager.SaveGame.WorldConfiguration.SetString("temporalStorms", (playStyle2 == "surviveandbuild") ? "sometimes" : "often");
				}
			}
			byte[] array = sapi.WorldManager.SaveGame.GetData("temporalStormData");
			if (array != null)
			{
				try
				{
					data = SerializerUtil.Deserialize<TemporalStormRunTimeData>(array);
				}
				catch (Exception)
				{
					api.World.Logger.Notification("Failed loading temporal storm data, will initialize new data set");
					data = new TemporalStormRunTimeData();
					flag = true;
				}
			}
			else
			{
				data = new TemporalStormRunTimeData();
				flag = true;
			}
			LoadNoise();
			if (flag)
			{
				prepareNextStorm();
			}
		};
		api.Event.OnTrySpawnEntity += Event_OnTrySpawnEntity;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += Event_PlayerJoin;
		api.Event.PlayerNowPlaying += Event_PlayerNowPlaying;
		api.Event.RegisterGameTickListener(onTempStormTick, 2000);
	}

	private TextCommandResult OnCmdNextStorm(TextCommandCallingArgs textCommandCallingArgs)
	{
		if (data.nowStormActive)
		{
			double daysleft = data.stormActiveTotalDays - api.World.Calendar.TotalDays;
			return TextCommandResult.Success(Lang.Get(data.nextStormStrength.ToString() + " Storm still active for {0:0.##} days", daysleft));
		}
		double nextStormDaysLeft = data.nextStormTotalDays - api.World.Calendar.TotalDays;
		return TextCommandResult.Success(Lang.Get("temporalstorm-cmd-daysleft", nextStormDaysLeft));
	}

	private void Event_PlayerNowPlaying(IServerPlayer byPlayer)
	{
		if (sapi.WorldManager.SaveGame.IsNew && stormsEnabled)
		{
			double nextStormDaysLeft = data.nextStormTotalDays - api.World.Calendar.TotalDays;
			byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("{0} days until the first temporal storm.", (int)nextStormDaysLeft), EnumChatType.Notification);
		}
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		byPlayer.Entity.OnCanSpawnNearby = (EntityProperties type, Vec3d spawnPos, RuntimeSpawnConditions sc) => CanSpawnNearby(byPlayer, type, spawnPos, sc);
		serverChannel.SendPacket(data, byPlayer);
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("temporalStormData", SerializerUtil.Serialize(data));
	}

	private void onTempStormTick(float dt)
	{
		if (config == null)
		{
			return;
		}
		if (!stormsEnabled)
		{
			data.stormGlitchStrength = 0f;
			data.nowStormActive = false;
			return;
		}
		if (data.nowStormActive)
		{
			trySpawnMobs();
		}
		double nextStormDaysLeft = data.nextStormTotalDays - api.World.Calendar.TotalDays;
		if (nextStormDaysLeft > 0.03 && nextStormDaysLeft < 0.35 && data.stormDayNotify > 1)
		{
			data.stormDayNotify = 1;
			sapi.BroadcastMessageToAllGroups(texts[data.nextStormStrength].Approaching, EnumChatType.Notification);
		}
		if (nextStormDaysLeft <= 0.02 && data.stormDayNotify > 0)
		{
			data.stormDayNotify = 0;
			sapi.BroadcastMessageToAllGroups(texts[data.nextStormStrength].Imminent, EnumChatType.Notification);
		}
		if (!(nextStormDaysLeft <= 0.0))
		{
			return;
		}
		float tempstormDurationMul = (float)api.World.Config.GetDecimal("tempstormDurationMul", 1.0);
		double stormActiveDays = (0.10000000149011612 + data.nextStormStrDouble * 0.10000000149011612) * (double)tempstormDurationMul;
		if (!data.nowStormActive && nextStormDaysLeft + stormActiveDays < 0.0)
		{
			prepareNextStorm();
			serverChannel.BroadcastPacket(data);
			return;
		}
		if (!data.nowStormActive)
		{
			data.stormActiveTotalDays = api.World.Calendar.TotalDays + stormActiveDays;
			data.stormGlitchStrength = 0.53f + (float)api.World.Rand.NextDouble() / 10f;
			if (data.nextStormStrength == EnumTempStormStrength.Medium)
			{
				data.stormGlitchStrength = 0.67f + (float)api.World.Rand.NextDouble() / 10f;
			}
			if (data.nextStormStrength == EnumTempStormStrength.Heavy)
			{
				data.stormGlitchStrength = 0.9f + (float)api.World.Rand.NextDouble() / 10f;
			}
			data.nowStormActive = true;
			serverChannel.BroadcastPacket(data);
			foreach (Entity e2 in ((CachingConcurrentDictionary<long, Entity>)(api.World as IServerWorldAccessor).LoadedEntities).Values)
			{
				if (stormMobCache.Contains(e2.Code))
				{
					e2.Attributes.SetBool("ignoreDaylightFlee", value: true);
				}
			}
		}
		double num = data.stormActiveTotalDays - api.World.Calendar.TotalDays;
		if (num < 0.02 && data.stormDayNotify == 0)
		{
			data.stormDayNotify = -1;
			sapi.BroadcastMessageToAllGroups(texts[data.nextStormStrength].Waning, EnumChatType.Notification);
		}
		if (!(num < 0.0))
		{
			return;
		}
		data.stormGlitchStrength = 0f;
		data.nowStormActive = false;
		data.stormDayNotify = 99;
		prepareNextStorm();
		serverChannel.BroadcastPacket(data);
		foreach (Entity e in ((CachingConcurrentDictionary<long, Entity>)(api.World as IServerWorldAccessor).LoadedEntities).Values)
		{
			if (stormMobCache.Contains(e.Code))
			{
				e.Attributes.RemoveAttribute("ignoreDaylightFlee");
				if (api.World.Rand.NextDouble() < 0.5)
				{
					sapi.World.DespawnEntity(e, new EntityDespawnData
					{
						Reason = EnumDespawnReason.Expire
					});
				}
			}
		}
	}

	private void prepareNextStorm()
	{
		if (config == null)
		{
			return;
		}
		double addStrength = Math.Min(config.StrengthIncreaseCap, (double)config.StrengthIncrease * api.World.Calendar.TotalDays / (double)config.Frequency.avg);
		double frequencyMod = api.World.Config.GetDecimal("tempStormFrequencyMul", 1.0);
		data.nextStormTotalDays = api.World.Calendar.TotalDays + (double)config.Frequency.nextFloat(1f, api.World.Rand) / (1.0 + addStrength / 3.0) / frequencyMod;
		double stormStrength = addStrength + api.World.Rand.NextDouble() * api.World.Rand.NextDouble() * (double)(float)addStrength * 5.0;
		int index = (int)Math.Min(2.0, stormStrength);
		data.nextStormStrength = (EnumTempStormStrength)index;
		data.nextStormStrDouble = Math.Max(0.0, addStrength);
		Dictionary<string, TempStormMobConfig.TempStormSpawnPattern> patterns = mobConfig.spawnsByStormStrength.spawnPatterns;
		string[] patterncodes = patterns.Keys.ToArray().Shuffle(sapi.World.Rand);
		float sumWeight = patterncodes.Sum((string code) => patterns[code].Weight);
		double rndval = sapi.World.Rand.NextDouble() * (double)sumWeight;
		foreach (string patterncode in patterncodes)
		{
			TempStormMobConfig.TempStormSpawnPattern pattern = patterns[patterncode];
			rndval -= (double)pattern.Weight;
			if (rndval <= 0.0)
			{
				data.spawnPatternCode = patterncode;
			}
		}
		data.rareSpawnCount = new Dictionary<string, int>();
		TempStormMobConfig.RareStormSpawnsVariant[] variants = mobConfig.rareSpawns.Variants;
		foreach (TempStormMobConfig.RareStormSpawnsVariant val in variants)
		{
			data.rareSpawnCount[val.Code] = GameMath.RoundRandom(sapi.World.Rand, val.ChancePerStorm);
		}
		rareSpawnsCountByCodeByPlayer.Clear();
	}

	private void trySpawnMobs()
	{
		float str = StormStrength;
		if (str < 0.01f || api.World.Rand.NextDouble() < 0.5 || spawnBreakUntilMs > api.World.ElapsedMilliseconds)
		{
			return;
		}
		EntityPartitioning part = api.ModLoader.GetModSystem<EntityPartitioning>();
		int range = 15;
		nobreakSpawns++;
		if (api.World.Rand.NextDouble() + 0.03999999910593033 < (double)((float)nobreakSpawns / 100f))
		{
			spawnBreakUntilMs = api.World.ElapsedMilliseconds + 1000 * api.World.Rand.Next(15);
		}
		IPlayer[] allOnlinePlayers = api.World.AllOnlinePlayers;
		foreach (IPlayer plr in allOnlinePlayers)
		{
			if (!(api.World.Rand.NextDouble() < 0.7))
			{
				trySpawnForPlayer(plr, range, str, part);
			}
		}
	}

	private void trySpawnForPlayer(IPlayer plr, int range, float stormStr, EntityPartitioning part)
	{
		Vec3d spawnPos = new Vec3d();
		BlockPos spawnPosi = new BlockPos();
		TempStormMobConfig.RareStormSpawnsVariant[] rareSpawns = mobConfig.rareSpawns.Variants.Shuffle(api.World.Rand);
		TempStormMobConfig.TempStormSpawnPattern spawnPattern = mobConfig.spawnsByStormStrength.spawnPatterns[data.spawnPatternCode];
		Dictionary<string, AssetLocation[]> variantGroups = mobConfig.spawnsByStormStrength.variantGroups;
		Dictionary<string, float> variantMuls = mobConfig.spawnsByStormStrength.variantQuantityMuls;
		Dictionary<string, EntityProperties[]> resovariantGroups = mobConfig.spawnsByStormStrength.resolvedVariantGroups;
		Dictionary<string, int> rareSpawnCounts = new Dictionary<string, int>();
		Dictionary<string, int> mainSpawnCountsByGroup = new Dictionary<string, int>();
		Vec3d plrPos = plr.Entity.ServerPos.XYZ;
		part.WalkEntities(plrPos, range + 30, delegate(Entity e)
		{
			foreach (KeyValuePair<string, AssetLocation[]> current in variantGroups)
			{
				if (current.Value.Contains(e.Code))
				{
					mainSpawnCountsByGroup.TryGetValue(current.Key, out var value);
					mainSpawnCountsByGroup[current.Key] = value + 1;
				}
			}
			for (int j = 0; j < rareSpawns.Length; j++)
			{
				if (rareSpawns[j].Code.Equals(e.Code))
				{
					rareSpawnCounts.TryGetValue(rareSpawns[j].GroupCode, out var value2);
					rareSpawnCounts[rareSpawns[j].GroupCode] = value2 + 1;
					break;
				}
			}
			return true;
		}, EnumEntitySearchType.Creatures);
		if (!rareSpawnsCountByCodeByPlayer.TryGetValue(plr.PlayerUID, out var plrdict))
		{
			Dictionary<string, int> dictionary2 = (rareSpawnsCountByCodeByPlayer[plr.PlayerUID] = new Dictionary<string, int>());
			plrdict = dictionary2;
		}
		foreach (KeyValuePair<string, int> rspc in rareSpawnCounts)
		{
			int prevcnt = 0;
			plrdict.TryGetValue(rspc.Key, out prevcnt);
			rareSpawnCounts.TryGetValue(rspc.Key, out var cnt2);
			plrdict[rspc.Key] = cnt2 + prevcnt;
		}
		foreach (KeyValuePair<string, float> group in spawnPattern.GroupWeights)
		{
			int allowedCount = (int)Math.Round((2f + stormStr * 8f) * group.Value);
			if (variantMuls.TryGetValue(group.Key, out var mul))
			{
				allowedCount = (int)Math.Round((float)allowedCount * mul);
			}
			int nowCount = 0;
			mainSpawnCountsByGroup.TryGetValue(group.Key, out nowCount);
			if (nowCount >= allowedCount)
			{
				continue;
			}
			EntityProperties[] variantGroup = resovariantGroups[group.Key];
			int tries = 10;
			int spawned = 0;
			while (tries-- > 0 && spawned < 2)
			{
				float typernd = (stormStr * 0.15f + (float)api.World.Rand.NextDouble() * (0.3f + stormStr / 2f)) * (float)variantGroup.Length;
				int index = GameMath.RoundRandom(api.World.Rand, typernd);
				EntityProperties type = variantGroup[GameMath.Clamp(index, 0, variantGroup.Length - 1)];
				if ((index == 3 || index == 4) && api.World.Rand.NextDouble() < 0.2)
				{
					for (int i = 0; i < rareSpawns.Length; i++)
					{
						plrdict.TryGetValue(rareSpawns[i].GroupCode, out var cnt);
						if (cnt == 0)
						{
							type = rareSpawns[i].ResolvedCode;
							tries = -1;
							break;
						}
					}
				}
				int rndx = api.World.Rand.Next(2 * range) - range;
				int rndy = api.World.Rand.Next(2 * range) - range;
				int rndz = api.World.Rand.Next(2 * range) - range;
				spawnPos.Set((double)((int)plrPos.X + rndx) + 0.5, (double)((int)plrPos.Y + rndy) + 0.001, (double)((int)plrPos.Z + rndz) + 0.5);
				spawnPosi.Set((int)spawnPos.X, (int)spawnPos.Y, (int)spawnPos.Z);
				while (api.World.BlockAccessor.GetBlock(spawnPosi.X, spawnPosi.Y - 1, spawnPosi.Z).Id == 0 && spawnPos.Y > 0.0)
				{
					spawnPosi.Y--;
					spawnPos.Y -= 1.0;
				}
				if (api.World.BlockAccessor.IsValidPos((int)spawnPos.X, (int)spawnPos.Y, (int)spawnPos.Z))
				{
					Cuboidf collisionBox = type.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
					if (!collisionTester.IsColliding(api.World.BlockAccessor, collisionBox, spawnPos, alsoCheckTouch: false))
					{
						DoSpawn(type, spawnPos, 0L);
						spawned++;
					}
				}
			}
		}
	}

	private void DoSpawn(EntityProperties entityType, Vec3d spawnPosition, long herdid)
	{
		Entity entity = api.ClassRegistry.CreateEntity(entityType);
		if (entity is EntityAgent agent)
		{
			agent.HerdId = herdid;
		}
		entity.ServerPos.SetPosWithDimension(spawnPosition);
		entity.ServerPos.SetYaw((float)api.World.Rand.NextDouble() * ((float)Math.PI * 2f));
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.Attributes.SetString("origin", "timedistortion");
		api.World.SpawnEntity(entity);
		entity.WatchedAttributes.SetDouble("temporalStability", GameMath.Clamp(1f - 1.5f * StormStrength, 0f, 1f));
		entity.Attributes.SetBool("ignoreDaylightFlee", value: true);
		if (entity.GetBehavior("timeddespawn") is ITimedDespawn bhDespawn)
		{
			bhDespawn.SetDespawnByCalendarDate(data.stormActiveTotalDays + 0.1 * (double)StormStrength * api.World.Rand.NextDouble());
		}
	}

	private bool Event_OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
	{
		if (mobConfig == null || !stormMobCache.Contains(properties.Code))
		{
			return true;
		}
		IPlayer plr = api.World.NearestPlayer(spawnPosition.X, spawnPosition.Y, spawnPosition.Z);
		if (plr == null)
		{
			return true;
		}
		double stab = plr.Entity.WatchedAttributes.GetDouble("temporalStability", 1.0);
		stab = Math.Min(stab, 1f - 1f * data.stormGlitchStrength);
		if (stab < 0.25)
		{
			int index = -1;
			foreach (KeyValuePair<string, AssetLocation[]> group in mobConfig.spawnsByStormStrength.variantGroups)
			{
				for (int i = 0; i < group.Value.Length; i++)
				{
					if (group.Value[i].Equals(properties.Code))
					{
						index = i;
						_ = group.Key;
						break;
					}
				}
			}
			if (index == -1)
			{
				return true;
			}
			EntityProperties[] resolvedVariantGroups = null;
			TempStormMobConfig.TempStormSpawnPattern tempStormSpawnPattern = mobConfig.spawnsByStormStrength.spawnPatterns[data.spawnPatternCode];
			float sumWeight = tempStormSpawnPattern.GroupWeights.Sum((KeyValuePair<string, float> w) => w.Value);
			double rndval = sapi.World.Rand.NextDouble() * (double)sumWeight;
			foreach (KeyValuePair<string, float> w2 in tempStormSpawnPattern.GroupWeights)
			{
				rndval -= (double)w2.Value;
				if (rndval <= 0.0)
				{
					resolvedVariantGroups = mobConfig.spawnsByStormStrength.resolvedVariantGroups[w2.Key];
				}
			}
			int difficultyIncrease = (int)Math.Round((0.25 - stab) * 15.0);
			int newIndex = Math.Min(index + difficultyIncrease, resolvedVariantGroups.Length - 1);
			properties = resolvedVariantGroups[newIndex];
		}
		return true;
	}

	private void LoadNoise()
	{
		if (api.Side == EnumAppSide.Server)
		{
			updateOldWorlds();
		}
		temporalStabilityEnabled = api.World.Config.GetBool("temporalStability", defaultValue: true);
		if (!temporalStabilityEnabled)
		{
			return;
		}
		stabilityNoise = SimplexNoise.FromDefaultOctaves(4, 0.1, 0.9, api.World.Seed);
		if (api.Side != EnumAppSide.Server)
		{
			return;
		}
		worldConfigStorminess = api.World.Config.GetString("temporalStorms");
		stormsEnabled = worldConfigStorminess != "off";
		if (worldConfigStorminess != null && configs.ContainsKey(worldConfigStorminess))
		{
			config = configs[worldConfigStorminess];
		}
		else
		{
			string playstyle = sapi.WorldManager.SaveGame.PlayStyle;
			if (playstyle == "surviveandbuild" || playstyle == "wildernesssurvival")
			{
				config = configs["rare"];
			}
			else
			{
				config = null;
			}
		}
		sapi.Event.OnEntityDeath += Event_OnEntityDeath;
		mobConfig = sapi.Assets.Get("config/mobextraspawns.json").ToObject<MobExtraSpawnsTemp>().temporalStormSpawns;
		Dictionary<string, EntityProperties[]> rdi = (mobConfig.spawnsByStormStrength.resolvedVariantGroups = new Dictionary<string, EntityProperties[]>());
		foreach (KeyValuePair<string, AssetLocation[]> val2 in mobConfig.spawnsByStormStrength.variantGroups)
		{
			int i = 0;
			rdi[val2.Key] = new EntityProperties[val2.Value.Length];
			AssetLocation[] value = val2.Value;
			foreach (AssetLocation code in value)
			{
				rdi[val2.Key][i++] = sapi.World.GetEntityType(code);
				stormMobCache.Add(code);
			}
		}
		TempStormMobConfig.RareStormSpawnsVariant[] variants = mobConfig.rareSpawns.Variants;
		foreach (TempStormMobConfig.RareStormSpawnsVariant val in variants)
		{
			val.ResolvedCode = sapi.World.GetEntityType(val.Code);
			stormMobCache.Add(val.Code);
		}
	}

	internal float GetGlitchEffectExtraStrength()
	{
		if (!data.nowStormActive)
		{
			return modGlitchStrength;
		}
		return data.stormGlitchStrength + modGlitchStrength;
	}

	private void Event_OnEntityDeath(Entity entity, DamageSource damageSource)
	{
		Entity damagedBy = damageSource?.GetCauseEntity();
		if (damagedBy != null && damagedBy.WatchedAttributes.HasAttribute("temporalStability") && entity.Properties.Attributes != null)
		{
			float stabrecovery = entity.Properties.Attributes["onDeathStabilityRecovery"].AsFloat();
			double ownstab = damagedBy.WatchedAttributes.GetDouble("temporalStability", 1.0);
			damagedBy.WatchedAttributes.SetDouble("temporalStability", Math.Min(1.0, ownstab + (double)stabrecovery));
		}
	}

	public float GetTemporalStability(BlockPos pos)
	{
		return GetTemporalStability(pos.X, pos.Y, pos.Z);
	}

	public float GetTemporalStability(Vec3d pos)
	{
		return GetTemporalStability(pos.X, pos.Y, pos.Z);
	}

	public bool CanSpawnNearby(IPlayer byPlayer, EntityProperties type, Vec3d spawnPosition, RuntimeSpawnConditions sc)
	{
		int herelightLevel = api.World.BlockAccessor.GetLightLevel((int)spawnPosition.X, (int)spawnPosition.Y, (int)spawnPosition.Z, sc.LightLevelType);
		if (temporalStabilityEnabled)
		{
			JsonObject attributes = type.Attributes;
			if (attributes != null && attributes["spawnCloserDuringLowStability"].AsBool())
			{
				double mod = Math.Min(1.0, 4.0 * byPlayer.Entity.WatchedAttributes.GetDouble("temporalStability", 1.0));
				mod = Math.Min(mod, Math.Max(0f, 1f - 2f * data.stormGlitchStrength));
				int surfaceY = api.World.BlockAccessor.GetTerrainMapheightAt(spawnPosition.AsBlockPos);
				bool isSurface = spawnPosition.Y >= (double)(surfaceY - 5);
				float riftDist = NearestRiftDistance(spawnPosition);
				float num = GameMath.Mix(0, sc.MinLightLevel, (float)mod);
				float maxl = GameMath.Mix(32, sc.MaxLightLevel, (float)mod);
				if ((num > (float)herelightLevel || maxl < (float)herelightLevel) && (!isSurface || riftDist >= 5f || api.World.Rand.NextDouble() > 0.05))
				{
					return false;
				}
				double sqdist = byPlayer.Entity.ServerPos.SquareDistanceTo(spawnPosition);
				if (isSurface)
				{
					return riftDist < 24f;
				}
				if (mod < 0.5)
				{
					return sqdist < 100.0;
				}
				return sqdist > (double)(sc.MinDistanceToPlayer * sc.MinDistanceToPlayer) * mod;
			}
		}
		if (sc.MinLightLevel > herelightLevel || sc.MaxLightLevel < herelightLevel)
		{
			return false;
		}
		return byPlayer.Entity.ServerPos.SquareDistanceTo(spawnPosition) > (double)(sc.MinDistanceToPlayer * sc.MinDistanceToPlayer);
	}

	private float NearestRiftDistance(Vec3d pos)
	{
		return riftSys.riftsById.Values.Nearest((Rift rift) => rift.Position.SquareDistanceTo(pos))?.Position.DistanceTo(pos) ?? 9999f;
	}

	public float GetTemporalStability(double x, double y, double z)
	{
		if (!temporalStabilityEnabled)
		{
			return 2f;
		}
		float noiseval = (float)GameMath.Clamp(stabilityNoise.Noise(x / 80.0, y / 80.0, z / 80.0) * 1.2000000476837158 + 0.10000000149011612, -1.0, 2.0);
		float sealLevelDistance = (float)((double)TerraGenConfig.seaLevel - y);
		float surfacenoiseval = GameMath.Clamp(1.6f + noiseval, 0.8f, 1.5f);
		float i = (float)GameMath.Clamp(Math.Pow(Math.Max(0f, (float)y) / (float)TerraGenConfig.seaLevel, 2.0), 0.0, 1.0);
		noiseval = GameMath.Mix(noiseval, surfacenoiseval, i);
		noiseval -= Math.Max(0f, sealLevelDistance / (float)api.World.BlockAccessor.MapSizeY) / 3.5f;
		noiseval = GameMath.Clamp(noiseval, 0f, 1.5f);
		float extraStr = 1.5f * GetGlitchEffectExtraStrength();
		float stability = GameMath.Clamp(noiseval - extraStr, 0f, 1.5f);
		if (this.OnGetTemporalStability != null)
		{
			stability = this.OnGetTemporalStability(stability, x, y, z);
		}
		return stability;
	}

	private void updateOldWorlds()
	{
		if (!api.World.Config.HasAttribute("temporalStorms"))
		{
			if (sapi.WorldManager.SaveGame.PlayStyle == "wildernesssurvival")
			{
				api.World.Config.SetString("temporalStorms", "often");
			}
			if (sapi.WorldManager.SaveGame.PlayStyle == "surviveandbuild")
			{
				api.World.Config.SetString("temporalStorms", "rare");
			}
		}
		if (!api.World.Config.HasAttribute("temporalStability") && (sapi.WorldManager.SaveGame.PlayStyle == "wildernesssurvival" || sapi.WorldManager.SaveGame.PlayStyle == "surviveandbuild"))
		{
			api.World.Config.SetBool("temporalStability", value: true);
		}
	}
}
