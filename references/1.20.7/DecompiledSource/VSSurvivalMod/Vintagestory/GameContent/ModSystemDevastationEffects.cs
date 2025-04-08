using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class ModSystemDevastationEffects : ModSystem, IRenderer, IDisposable
{
	public MobExtraSpawnsDeva.DevaAreaMobConfig mobConfig;

	public Vec3d DevaLocationPresent;

	public Vec3d DevaLocationPast;

	public int EffectRadius;

	private ICoreClientAPI capi;

	private int EffectDist = 5000;

	private static SimpleParticleProperties dustParticles;

	private ICoreServerAPI sapi;

	private CollisionTester collisionTester = new CollisionTester();

	private AmbientModifier towerAmbientPresent;

	private AmbientModifier towerAmbientPast;

	private EntityErel entityErel;

	public bool ErelAnnoyed;

	private float weatherAttenuate;

	private float devaRangeness;

	private bool layer2InRange;

	private bool bossFightInRange;

	private bool allLayersLoaded;

	private MusicTrack baseTrack;

	private MusicTrack rustTrack;

	private MusicTrack layer2Track;

	private MusicTrack bossFightTrack;

	private bool wasStopped = true;

	private bool wasStarted;

	private float priority = 4.5f;

	private float fadeInDuration = 2f;

	private AssetLocation baseLayerMloc = new AssetLocation("music/devastation-baselayer.ogg");

	private AssetLocation rustlayerMloc = new AssetLocation("music/devastation-bellsandrust.ogg");

	private AssetLocation layer2Mloc = new AssetLocation("music/devastation-erelharrass.ogg");

	private AssetLocation erelFightMloc = new AssetLocation("music/devastation-erelfight.ogg");

	private bool baseLayerLoaded;

	private bool rustLayerLoaded;

	private bool kayer2Loaded;

	private bool erelFightLoaded;

	private HashSet<AssetLocation> allowedInDevaAreaCodes = new HashSet<AssetLocation>();

	public double RenderOrder => 1.0;

	public int RenderRange => 1000;

	public override double ExecuteOrder()
	{
		return 2.0;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		api.Event.OnGetWindSpeed += Event_OnGetWindSpeed;
		api.Network.GetChannel("devastation").RegisterMessageType<ErelAnnoyedPacket>();
	}

	private void Event_OnGetWindSpeed(Vec3d pos, ref Vec3d windSpeed)
	{
		if (!(DevaLocationPresent == null))
		{
			float dist = DevaLocationPresent.DistanceTo(pos.X, pos.Y, pos.Z);
			if (!(dist > (float)EffectDist))
			{
				windSpeed.Mul(GameMath.Clamp(dist / (float)EffectRadius - 0.5f, 0f, 1f));
			}
		}
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.PlayerDimensionChanged += Event_PlayerDimensionChanged;
		dustParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.3f, 0.3f, EnumParticleModel.Quad);
		dustParticles.MinQuantity = 0.1f;
		dustParticles.MinVelocity.Set(-0.05f, -0.4f, -0.05f);
		dustParticles.AddVelocity.Set(0f, 0f, 0f);
		dustParticles.ParticleModel = EnumParticleModel.Quad;
		dustParticles.GravityEffect = 0f;
		dustParticles.MaxSize = 1f;
		dustParticles.AddPos.Set(0.0, 0.0, 0.0);
		dustParticles.MinSize = 0.2f;
		dustParticles.Color = ColorUtil.ColorFromRgba(18, 25, 37, 255);
		dustParticles.addLifeLength = 0f;
		dustParticles.WithTerrainCollision = false;
		dustParticles.SelfPropelled = false;
		dustParticles.LifeLength = 4f;
		dustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f);
		api.Network.GetChannel("devastation").SetMessageHandler<DevaLocation>(OnDevaLocation).SetMessageHandler(delegate(ErelAnnoyedPacket p)
		{
			ErelAnnoyed = p.Annoyed;
		});
		api.Event.RegisterAsyncParticleSpawner(asyncParticleSpawn);
		api.Event.RegisterRenderer(this, EnumRenderStage.Before, "devastationeffects");
		api.Event.RegisterGameTickListener(clientTick100ms, 100, 123);
		towerAmbientPresent = new AmbientModifier
		{
			FogColor = new WeightedFloatArray(new float[3]
			{
				22f / 85f,
				0.1764706f,
				5f / 51f
			}, 0f),
			FogDensity = new WeightedFloat(0.05f, 0f)
		}.EnsurePopulated();
		api.Ambient.CurrentModifiers["towerAmbientPresent"] = towerAmbientPresent;
		towerAmbientPast = new AmbientModifier
		{
			FogColor = AmbientModifier.DefaultAmbient.FogColor,
			FogDensity = new WeightedFloat(0.04f, 0f)
		}.EnsurePopulated();
		towerAmbientPast.FogColor.Value[0] *= 0.8f;
		towerAmbientPast.FogColor.Value[1] *= 0.8f;
		towerAmbientPast.FogColor.Value[2] *= 0.8f;
		api.Ambient.CurrentModifiers["towerAmbientPast"] = towerAmbientPast;
		api.ModLoader.GetModSystem<ModSystemAmbientParticles>().ShouldSpawnAmbientParticles += () => devaRangeness > 1f;
		api.ModLoader.GetModSystem<WeatherSystemClient>().OnGetBlendedWeatherData += DevastationEffects_OnGetBlendedWeatherData;
		api.Event.OnGetClimate += Event_OnGetClimate;
		api.Settings.Int.AddWatcher("musicLevel", delegate
		{
			baseTrack?.UpdateVolume();
			rustTrack?.UpdateVolume();
			layer2Track?.UpdateVolume();
			bossFightTrack?.UpdateVolume();
		});
	}

	private void Event_OnGetClimate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0)
	{
		if ((double)devaRangeness < 1.1)
		{
			climate.Rainfall = Math.Max(0f, climate.Rainfall - weatherAttenuate);
			climate.RainCloudOverlay = Math.Max(0f, climate.RainCloudOverlay - weatherAttenuate);
		}
	}

	private void DevastationEffects_OnGetBlendedWeatherData(WeatherDataSnapshot obj)
	{
		if ((double)devaRangeness < 1.1)
		{
			obj.nearLightningRate = Math.Max(0f, obj.nearLightningRate - weatherAttenuate);
			obj.distantLightningRate = Math.Max(0f, obj.distantLightningRate - weatherAttenuate);
			obj.PrecIntensity = Math.Max(0f, obj.PrecIntensity - weatherAttenuate);
			float nowspeed = Math.Max(0f, obj.curWindSpeed.Length() - weatherAttenuate);
			obj.curWindSpeed.Normalize().Mul(nowspeed);
		}
	}

	private void clientTick100ms(float obj)
	{
		if (DevaLocationPresent == null)
		{
			return;
		}
		Vec3d plrpos = capi.World.Player.Entity.Pos.XYZ;
		float distsq = plrpos.HorizontalSquareDistanceTo(DevaLocationPresent);
		devaRangeness = distsq / 624100f;
		weatherAttenuate = Math.Max(0f, 1.1f - devaRangeness) * 1.7f;
		if (devaRangeness < 1f)
		{
			LoadMusic();
			StartMusic();
		}
		if (allLayersLoaded && !wasStopped)
		{
			if (distsq < 5184f && !layer2InRange)
			{
				layer2Track.Sound.FadeTo(1.0, fadeInDuration, null);
				layer2InRange = true;
			}
			if (distsq > 5776f && layer2InRange)
			{
				layer2Track.Sound.FadeTo(0.0, fadeInDuration, delegate(ILoadedSound s)
				{
					s.SetVolume(0f);
				});
				layer2InRange = false;
			}
			bool playBossTrack = !ErelAnnoyed;
			double towerLevel = plrpos.Y - ((capi.World.Player.Entity.Pos.Dimension > 0) ? DevaLocationPast.Y : DevaLocationPresent.Y);
			if (distsq < 1600f && towerLevel > 68.0 && !bossFightInRange && playBossTrack)
			{
				bossFightTrack.Sound.FadeTo(1.0, fadeInDuration, null);
				rustTrack.Sound.FadeTo(0.0, fadeInDuration, delegate(ILoadedSound s)
				{
					s.SetVolume(0f);
				});
				bossFightInRange = true;
			}
			if ((distsq > 2500f || towerLevel <= 64.0 || !playBossTrack) && bossFightInRange)
			{
				bossFightTrack.Sound.FadeTo(0.0, fadeInDuration, delegate(ILoadedSound s)
				{
					s.SetVolume(0f);
				});
				rustTrack.Sound.FadeTo(1.0, fadeInDuration, null);
				bossFightInRange = false;
			}
		}
		if (distsq > 656100f)
		{
			StopMusic();
		}
	}

	private void LoadMusic()
	{
		if (layer2Track == null)
		{
			layer2Track = capi.StartTrack(layer2Mloc, 99f, EnumSoundType.MusicGlitchunaffected, delegate(ILoadedSound s)
			{
				kayer2Loaded = true;
				onTrackLoaded(s, layer2Track);
			});
			layer2Track.Priority = priority;
			bossFightTrack = capi.StartTrack(erelFightMloc, 99f, EnumSoundType.MusicGlitchunaffected, delegate(ILoadedSound s)
			{
				erelFightLoaded = true;
				onTrackLoaded(s, bossFightTrack);
			});
			bossFightTrack.Priority = priority;
			rustTrack = capi.StartTrack(rustlayerMloc, 99f, EnumSoundType.MusicGlitchunaffected, delegate(ILoadedSound s)
			{
				rustLayerLoaded = true;
				onTrackLoaded(s, rustTrack);
			});
			rustTrack.Priority = priority;
			wasStopped = false;
		}
	}

	private void StopMusic()
	{
		if (capi != null && !wasStopped)
		{
			baseTrack?.Sound?.FadeTo(0.0, 4f, delegate(ILoadedSound s)
			{
				s.Stop();
			});
			rustTrack?.Sound?.FadeTo(0.0, 4f, delegate(ILoadedSound s)
			{
				s.Stop();
			});
			layer2Track?.Sound?.FadeTo(0.0, 4f, delegate(ILoadedSound s)
			{
				s.Stop();
			});
			bossFightTrack?.Sound?.FadeTo(0.0, 4f, delegate(ILoadedSound s)
			{
				s.Stop();
			});
			wasStopped = true;
			wasStarted = false;
		}
	}

	private void onTrackLoaded(ILoadedSound sound, MusicTrack track)
	{
		if (track == null)
		{
			sound?.Dispose();
		}
		else
		{
			if (sound == null)
			{
				return;
			}
			track.Sound = sound;
			track.Sound.SetLooping(on: true);
			track.ManualDispose = true;
			if (rustLayerLoaded && kayer2Loaded && erelFightLoaded)
			{
				baseTrack = capi.StartTrack(baseLayerMloc, 99f, EnumSoundType.MusicGlitchunaffected, delegate(ILoadedSound s)
				{
					baseLayerLoaded = true;
					baseTrack.Sound = s;
					baseTrack.Sound.SetLooping(on: true);
					baseTrack.ManualDispose = true;
					StartMusic();
				});
				baseTrack.Priority = priority;
			}
		}
	}

	private void StartMusic()
	{
		if (baseLayerLoaded && rustLayerLoaded && kayer2Loaded && erelFightLoaded && !wasStarted)
		{
			capi.StartTrack(baseTrack, 99f, EnumSoundType.MusicGlitchunaffected, playnow: false);
			baseTrack.Sound.SetVolume(0f);
			rustTrack.Sound.SetVolume(0f);
			layer2Track.Sound.SetVolume(0f);
			bossFightTrack.Sound.SetVolume(0f);
			baseTrack.Sound.Start();
			baseTrack.Sound.FadeIn(fadeInDuration, null);
			rustTrack.Sound.Start();
			rustTrack.Sound.FadeIn(fadeInDuration, null);
			layer2Track.Sound.Start();
			bossFightTrack.Sound.Start();
			wasStopped = false;
			wasStarted = true;
			allLayersLoaded = true;
			capi.Event.RegisterCallback(delegate
			{
				baseTrack.loading = false;
				rustTrack.loading = false;
				layer2Track.loading = false;
				bossFightTrack.loading = false;
			}, 500, permittedWhilePaused: true);
		}
	}

	private void Event_PlayerDimensionChanged(IPlayer byPlayer)
	{
		updateFogState();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		updateFogState();
	}

	private void updateFogState()
	{
		if (DevaLocationPresent == null)
		{
			towerAmbientPast.FogDensity.Weight = 0f;
			towerAmbientPast.FogColor.Weight = 0f;
			towerAmbientPresent.FogDensity.Weight = 0f;
			towerAmbientPresent.FogColor.Weight = 0f;
			return;
		}
		Vec3d offsetToTowerCenter = DevaLocationPresent - capi.World.Player.Entity.Pos.XYZ;
		double presentDist = offsetToTowerCenter.Length();
		if (presentDist > (double)EffectDist)
		{
			capi.Render.ShaderUniforms.FogSphereQuantity = 0;
			towerAmbientPresent.FogDensity.Weight = 0f;
			towerAmbientPresent.FogColor.Weight = 0f;
		}
		else
		{
			towerAmbientPresent.FogColor.Weight = (float)GameMath.Clamp((1.0 - (presentDist - (double)(EffectRadius / 2)) / (double)EffectRadius) * 2.0, 0.0, 1.0);
			towerAmbientPresent.FogDensity.Value = 0.05f;
			towerAmbientPresent.FogDensity.Weight = (float)GameMath.Clamp(1.0 - (presentDist - (double)(EffectRadius / 2)) / (double)EffectRadius, 0.0, 1.0);
			float f = (float)(1.0 - presentDist / (double)EffectDist);
			f = GameMath.Clamp(1.5f * (f - 0.25f), 0f, 1f);
			Vec4f fogColor = capi.Ambient.BlendedFogColor;
			float blendedFogDensity = capi.Ambient.BlendedFogDensity;
			float w = towerAmbientPresent.FogColor.Weight;
			float i = GameMath.Clamp(blendedFogDensity * 100f + (1f - w) - 1f, 0f, 1f);
			float b = capi.Ambient.BlendedFogBrightness * capi.Ambient.BlendedSceneBrightness;
			capi.Render.ShaderUniforms.FogSphereQuantity = 1;
			capi.Render.ShaderUniforms.FogSpheres[0] = (float)offsetToTowerCenter.X;
			capi.Render.ShaderUniforms.FogSpheres[1] = (float)offsetToTowerCenter.Y - 300f;
			capi.Render.ShaderUniforms.FogSpheres[2] = (float)offsetToTowerCenter.Z;
			capi.Render.ShaderUniforms.FogSpheres[3] = (float)EffectRadius * 1.6f;
			capi.Render.ShaderUniforms.FogSpheres[4] = 0.00125f * f;
			capi.Render.ShaderUniforms.FogSpheres[5] = GameMath.Lerp(22f / 85f * b, fogColor.R, i);
			capi.Render.ShaderUniforms.FogSpheres[6] = GameMath.Lerp(0.1764706f * b, fogColor.G, i);
			capi.Render.ShaderUniforms.FogSpheres[7] = GameMath.Lerp(5f / 51f * b, fogColor.B, i);
		}
		double pastDist = (DevaLocationPast - capi.World.Player.Entity.Pos.XYZ).Length();
		if (pastDist > (double)EffectDist)
		{
			towerAmbientPast.FogDensity.Weight = 0f;
			towerAmbientPast.FogColor.Weight = 0f;
		}
		else
		{
			towerAmbientPast.FogColor.Weight = (float)GameMath.Clamp((1.0 - (pastDist - (double)(EffectRadius / 2)) / (double)EffectRadius) * 2.0, 0.0, 1.0);
			towerAmbientPast.FogDensity.Value = 0.05f;
			towerAmbientPast.FogDensity.Weight = (float)GameMath.Clamp(1.0 - (pastDist - (double)(EffectRadius / 2)) / (double)EffectRadius, 0.0, 1.0);
		}
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.OnTrySpawnEntity += Event_OnTrySpawnEntity;
		api.Event.PlayerJoin += Event_PlayerJoin;
		api.Event.RegisterGameTickListener(OnGameTickServer, 1000);
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		sapi.Network.GetChannel("devastation").SendPacket(new ErelAnnoyedPacket
		{
			Annoyed = ErelAnnoyed
		}, byPlayer);
	}

	private bool Event_OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
	{
		if (DevaLocationPresent == null)
		{
			return true;
		}
		if ((double)spawnPosition.HorizontalSquareDistanceTo(DevaLocationPresent) < 640000.0 && !allowedInDevaAreaCodes.Contains(properties.Code))
		{
			return false;
		}
		return true;
	}

	private void Event_SaveGameLoaded()
	{
		foreach (EntityProperties val2 in sapi.World.EntityTypes)
		{
			JsonObject attributes = val2.Attributes;
			if (attributes != null && attributes.IsTrue("allowInDevastationArea"))
			{
				allowedInDevaAreaCodes.Add(val2.Code);
			}
		}
		mobConfig = sapi.Assets.Get("config/mobextraspawns.json").ToObject<MobExtraSpawnsDeva>().devastationAreaSpawns;
		Dictionary<string, EntityProperties[]> rdi = (mobConfig.ResolvedVariantGroups = new Dictionary<string, EntityProperties[]>());
		foreach (KeyValuePair<string, AssetLocation[]> val in mobConfig.VariantGroups)
		{
			int i = 0;
			rdi[val.Key] = new EntityProperties[val.Value.Length];
			AssetLocation[] value = val.Value;
			foreach (AssetLocation code in value)
			{
				rdi[val.Key][i++] = sapi.World.GetEntityType(code);
			}
		}
	}

	private void OnGameTickServer(float obj)
	{
		if (DevaLocationPresent == null)
		{
			return;
		}
		double towerMinDistanceXZ = double.MaxValue;
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		foreach (IPlayer player in allOnlinePlayers)
		{
			double distance = player.Entity.ServerPos.DistanceTo(DevaLocationPresent);
			towerMinDistanceXZ = Math.Min(towerMinDistanceXZ, player.Entity.ServerPos.HorDistanceTo(DevaLocationPresent));
			EntityStat<float> value;
			bool hasEffect = player.Entity.Stats["gliderLiftMax"].ValuesByKey.TryGetValue("deva", out value);
			if (distance < (double)EffectRadius)
			{
				if (!hasEffect)
				{
					player.Entity.Stats.Set("gliderSpeedMax", "deva", -0.185f);
					player.Entity.Stats.Set("gliderLiftMax", "deva", -1.01f);
				}
				if (sapi.World.Rand.NextDouble() < 10.15 && distance > 25.0)
				{
					trySpawnMobsForPlayer(player);
				}
			}
			else if (hasEffect)
			{
				player.Entity.Stats.Remove("gliderSpeedMax", "deva");
				player.Entity.Stats.Remove("gliderLiftMax", "deva");
			}
		}
		if (towerMinDistanceXZ < (double)EffectDist)
		{
			if (entityErel == null || sapi.World.GetEntityById(entityErel.EntityId) == null)
			{
				entityErel = null;
				loadErel();
			}
		}
		else if (entityErel != null)
		{
			saveErel();
			sapi.World.DespawnEntity(entityErel, new EntityDespawnData
			{
				Reason = EnumDespawnReason.OutOfRange
			});
			entityErel = null;
		}
	}

	private void Event_GameWorldSave()
	{
		saveErel();
	}

	private void saveErel()
	{
		if (entityErel == null)
		{
			return;
		}
		sapi.Logger.VerboseDebug("Unloading erel");
		using MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		try
		{
			writer.Write(sapi.ClassRegistry.GetEntityClassName(entityErel.GetType()));
			entityErel.ToBytes(writer, forClient: false);
			sapi.WorldManager.SaveGame.StoreData<byte[]>("erelEntity", ms.ToArray());
		}
		catch (Exception e)
		{
			sapi.Logger.Error("Error thrown trying to serialize erel entity with code {0}, will not save, sorry!", entityErel?.Code);
			sapi.Logger.Error(e);
		}
	}

	private void loadErel()
	{
		sapi.Logger.VerboseDebug("Loading erel");
		byte[] erelBytes = sapi.WorldManager.SaveGame.GetData<byte[]>("erelEntity");
		if (erelBytes != null)
		{
			string className = "unknown";
			try
			{
				using MemoryStream ms = new MemoryStream(erelBytes);
				BinaryReader reader = new BinaryReader(ms);
				className = reader.ReadString();
				Entity entity2 = sapi.ClassRegistry.CreateEntity(className);
				entity2.FromBytes(reader, isSync: false, sapi.World.RemappedEntities);
				entityErel = (EntityErel)entity2;
				long chunkindex3d = sapi.WorldManager.ChunkIndex3D((int)entity2.ServerPos.X / 32, (int)entity2.ServerPos.Y / 32, (int)entity2.ServerPos.Z / 32);
				sapi.World.LoadEntity(entityErel, chunkindex3d);
			}
			catch (Exception e)
			{
				sapi.Logger.Error("Failed loading erel entity (type " + className + "). Will create new one. Exception logged to verbose debug.");
				sapi.Logger.VerboseDebug("Failed loading erel entity. Will create new one. Exception: {0}", LoggerBase.CleanStackTrace(e.ToString()));
			}
		}
		if (entityErel == null)
		{
			sapi.Logger.VerboseDebug("Spawned erel");
			EntityProperties type = sapi.World.GetEntityType(new AssetLocation("erel-corrupted"));
			Entity entity = sapi.World.ClassRegistry.CreateEntity(type);
			entity.ServerPos.SetPos(DevaLocationPresent);
			entity.ServerPos.Y = TerraGenConfig.seaLevel + 90;
			entity.Pos.SetPos(entity.ServerPos);
			sapi.World.SpawnEntity(entity);
			entityErel = (EntityErel)entity;
		}
	}

	private void trySpawnMobsForPlayer(IPlayer player)
	{
		EntityPartitioning modSystem = sapi.ModLoader.GetModSystem<EntityPartitioning>();
		Dictionary<string, int> spawnCountsByGroup = new Dictionary<string, int>();
		Vec3d spawnPos = new Vec3d();
		BlockPos spawnPosi = new BlockPos();
		int range = 30;
		Random rnd = sapi.World.Rand;
		Vec3d plrPos = player.Entity.ServerPos.XYZ;
		modSystem.WalkEntities(plrPos, range + 5, delegate(Entity e)
		{
			foreach (KeyValuePair<string, AssetLocation[]> current in mobConfig.VariantGroups)
			{
				if (current.Value.Contains(e.Code))
				{
					spawnCountsByGroup.TryGetValue(current.Key, out var value);
					spawnCountsByGroup[current.Key] = value + 1;
					break;
				}
			}
			return true;
		}, EnumEntitySearchType.Creatures);
		string[] array = mobConfig.VariantGroups.Keys.ToArray().Shuffle(rnd);
		foreach (string groupcode in array)
		{
			float allowedCount = mobConfig.Quantities[groupcode];
			int nowCount = 0;
			spawnCountsByGroup.TryGetValue(groupcode, out nowCount);
			if (!((float)nowCount < allowedCount))
			{
				continue;
			}
			EntityProperties[] variantGroup = mobConfig.ResolvedVariantGroups[groupcode];
			int tries = 15;
			while (tries-- > 0)
			{
				double typernd = sapi.World.Rand.NextDouble() * (double)variantGroup.Length;
				int index = GameMath.RoundRandom(sapi.World.Rand, (float)typernd);
				EntityProperties type = variantGroup[GameMath.Clamp(index, 0, variantGroup.Length - 1)];
				int mindist = 18;
				if (groupcode == "bowtorn")
				{
					mindist = 32;
				}
				int rndx = (mindist + rnd.Next(range - 10)) * (1 - 2 * rnd.Next(2));
				int rndy = rnd.Next(range - 10) * (1 - 2 * rnd.Next(2));
				int rndz = (mindist + rnd.Next(range - 10)) * (1 - 2 * rnd.Next(2));
				spawnPos.Set((double)((int)plrPos.X + rndx) + 0.5, (double)((int)plrPos.Y + rndy) + 0.001, (double)((int)plrPos.Z + rndz) + 0.5);
				spawnPosi.Set((int)spawnPos.X, (int)spawnPos.Y, (int)spawnPos.Z);
				while (sapi.World.BlockAccessor.GetBlockBelow(spawnPosi).Id == 0 && spawnPos.Y > 0.0)
				{
					spawnPosi.Y--;
					spawnPos.Y -= 1.0;
				}
				if (sapi.World.BlockAccessor.IsValidPos(spawnPosi))
				{
					Cuboidf collisionBox = type.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
					if (!collisionTester.IsColliding(sapi.World.BlockAccessor, collisionBox, spawnPos, alsoCheckTouch: false))
					{
						DoSpawn(type, spawnPos);
						return;
					}
				}
			}
		}
	}

	private void DoSpawn(EntityProperties entityType, Vec3d spawnPosition)
	{
		Entity entity = sapi.ClassRegistry.CreateEntity(entityType);
		entity.ServerPos.SetPosWithDimension(spawnPosition);
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.ServerPos.SetYaw((float)sapi.World.Rand.NextDouble() * ((float)Math.PI * 2f));
		entity.Attributes.SetString("origin", "devastation");
		sapi.World.SpawnEntity(entity);
		entity.Attributes.SetBool("ignoreDaylightFlee", value: true);
	}

	private bool asyncParticleSpawn(float dt, IAsyncParticleManager manager)
	{
		if (DevaLocationPresent == null)
		{
			return true;
		}
		EntityPlayer plr = capi.World.Player.Entity;
		if ((float)GameMath.Clamp((1.5 - plr.Pos.DistanceTo(DevaLocationPresent) / (double)EffectRadius) * 1.5, 0.0, 1.0) <= 0f)
		{
			return true;
		}
		double offsetx = plr.Pos.Motion.X * 200.0;
		double offsetz = plr.Pos.Motion.Z * 200.0;
		for (int dx = -60; dx <= 60; dx++)
		{
			for (int dz = -60; dz <= 60; dz++)
			{
				Vec3d pos = plr.Pos.XYZ.Add((double)dx + offsetx, 0.0, (double)dz + offsetz);
				float hereintensity = (float)GameMath.Clamp((double)(1f - pos.DistanceTo(DevaLocationPresent) / (float)EffectRadius) * 1.5, 0.0, 1.0);
				if (!(capi.World.Rand.NextDouble() > (double)hereintensity * 0.015))
				{
					pos.Y = (double)(capi.World.BlockAccessor.GetRainMapHeightAt((int)pos.X, (int)pos.Z) - 8) + capi.World.Rand.NextDouble() * 25.0;
					if (!(capi.World.BlockAccessor.GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z).FirstCodePart() != "devastatedsoil"))
					{
						Vec3f velocity = DevaLocationPresent.Clone().Sub(pos.X, pos.Y, pos.Z).Normalize()
							.Mul(2.0 * capi.World.Rand.NextDouble())
							.ToVec3f() / 2f;
						velocity.Y = 1f + Math.Max(0f, 40f - pos.DistanceTo(DevaLocationPresent)) / 20f;
						dustParticles.MinPos = pos;
						dustParticles.MinVelocity = velocity;
						manager.Spawn(dustParticles);
					}
				}
			}
		}
		return true;
	}

	private void OnDevaLocation(DevaLocation packet)
	{
		DevaLocationPresent = packet.Pos.ToVec3d();
		EffectRadius = packet.Radius;
		DevaLocationPast = packet.Pos.SetDimension(2).ToVec3d();
	}

	public void SetErelAnnoyed(bool on)
	{
		if (sapi != null)
		{
			sapi.Network.GetChannel("devastation").BroadcastPacket(new ErelAnnoyedPacket
			{
				Annoyed = on
			});
			ErelAnnoyed = on;
		}
	}
}
