using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class WeatherSimulationRegion
{
	public bool Transitioning;

	public float TransitionDelay;

	public WeatherPattern NewWePattern;

	public WeatherPattern OldWePattern;

	public RingArray<SnowAccumSnapshot> SnowAccumSnapshots;

	public static object snowAccumSnapshotLock = new object();

	public WindPattern CurWindPattern;

	public WeatherEvent CurWeatherEvent;

	public float Weight;

	public double LastUpdateTotalHours;

	public LCGRandom Rand;

	public int regionX;

	public int regionZ;

	public int cloudTilebasePosX;

	public int cloudTilebasePosZ;

	public WeatherDataSnapshot weatherData = new WeatherDataSnapshot();

	public bool IsInitialized;

	public bool IsDummy;

	public WeatherPattern[] WeatherPatterns;

	public WindPattern[] WindPatterns;

	public WeatherEvent[] WeatherEvents;

	protected WeatherSystemBase ws;

	protected WeatherSystemServer wsServer;

	protected ICoreClientAPI capi;

	protected float quarterSecAccum;

	protected BlockPos regionCenterPos;

	protected Vec3d tmpVecPos = new Vec3d();

	public IMapRegion MapRegion;

	public static int snowAccumResolution = 2;

	public WeatherSimulationRegion(WeatherSystemBase ws, int regionX, int regionZ)
	{
		this.ws = ws;
		this.regionX = regionX;
		this.regionZ = regionZ;
		SnowAccumSnapshots = new RingArray<SnowAccumSnapshot>((int)((float)ws.api.World.Calendar.DaysPerYear * ws.api.World.Calendar.HoursPerDay) + 1);
		int regsize = ws.api.World.BlockAccessor.RegionSize;
		LastUpdateTotalHours = ws.api.World.Calendar.TotalHours;
		cloudTilebasePosX = regionX * regsize / ws.CloudTileSize;
		cloudTilebasePosZ = regionZ * regsize / ws.CloudTileSize;
		regionCenterPos = new BlockPos(regionX * regsize + regsize / 2, 0, regionZ * regsize + regsize / 2);
		Rand = new LCGRandom(ws.api.World.Seed);
		Rand.InitPositionSeed(regionX / 3, regionZ / 3);
		weatherData.Ambient = new AmbientModifier().EnsurePopulated();
		if (ws.api.Side == EnumAppSide.Client)
		{
			capi = ws.api as ICoreClientAPI;
			weatherData.Ambient.FogColor = capi.Ambient.Base.FogColor.Clone();
		}
		else
		{
			wsServer = ws as WeatherSystemServer;
		}
		ReloadPatterns(ws.api.World.Seed);
	}

	internal void ReloadPatterns(int seed)
	{
		WeatherPatterns = new WeatherPattern[ws.WeatherConfigs.Length];
		for (int k = 0; k < ws.WeatherConfigs.Length; k++)
		{
			WeatherPatterns[k] = new WeatherPattern(ws, ws.WeatherConfigs[k], Rand, cloudTilebasePosX, cloudTilebasePosZ);
			WeatherPatterns[k].State.Index = k;
		}
		WindPatterns = new WindPattern[ws.WindConfigs.Length];
		for (int j = 0; j < ws.WindConfigs.Length; j++)
		{
			WindPatterns[j] = new WindPattern(ws.api, ws.WindConfigs[j], j, Rand, seed);
		}
		WeatherEvents = new WeatherEvent[ws.WeatherEventConfigs.Length];
		for (int i = 0; i < ws.WeatherEventConfigs.Length; i++)
		{
			WeatherEvents[i] = new WeatherEvent(ws.api, ws.WeatherEventConfigs[i], i, Rand, seed - 876);
		}
	}

	internal void LoadRandomPattern()
	{
		NewWePattern = RandomWeatherPattern();
		OldWePattern = RandomWeatherPattern();
		NewWePattern.OnBeginUse();
		OldWePattern.OnBeginUse();
		CurWindPattern = WindPatterns[Rand.NextInt(WindPatterns.Length)];
		CurWindPattern.OnBeginUse();
		CurWeatherEvent = RandomWeatherEvent();
		CurWeatherEvent.OnBeginUse();
		Weight = 1f;
		wsServer?.SendWeatherStateUpdate(new WeatherState
		{
			RegionX = regionX,
			RegionZ = regionZ,
			NewPattern = NewWePattern.State,
			OldPattern = OldWePattern.State,
			WindPattern = CurWindPattern.State,
			WeatherEvent = CurWeatherEvent?.State,
			TransitionDelay = 0f,
			Transitioning = false,
			Weight = Weight,
			updateInstant = false,
			LcgCurrentSeed = Rand.currentSeed,
			LcgMapGenSeed = Rand.mapGenSeed,
			LcgWorldSeed = Rand.worldSeed
		});
	}

	internal void Initialize()
	{
		for (int i = 0; i < WeatherPatterns.Length; i++)
		{
			WeatherPatterns[i].Initialize(i, ws.api.World.Seed);
		}
		NewWePattern = WeatherPatterns[0];
		OldWePattern = WeatherPatterns[0];
		CurWindPattern = WindPatterns[0];
		CurWeatherEvent = WeatherEvents[0];
		IsInitialized = true;
	}

	public void UpdateWeatherData()
	{
		weatherData.SetAmbientLerped(OldWePattern, NewWePattern, Weight, (capi == null) ? 0f : capi.Ambient.Base.FogDensity.Value);
	}

	public void UpdateSnowAccumulation(int count)
	{
		SnowAccumSnapshot[] snaps = new SnowAccumSnapshot[count];
		for (int k = 0; k < count; k++)
		{
			snaps[k] = new SnowAccumSnapshot
			{
				TotalHours = LastUpdateTotalHours + (double)k,
				SnowAccumulationByRegionCorner = new FloatDataMap3D(snowAccumResolution, snowAccumResolution, snowAccumResolution)
			};
		}
		BlockPos tmpPos = new BlockPos();
		int regsize = ws.api.World.BlockAccessor.RegionSize;
		for (int ix = 0; ix < snowAccumResolution; ix++)
		{
			for (int iy = 0; iy < snowAccumResolution; iy++)
			{
				for (int iz = 0; iz < snowAccumResolution; iz++)
				{
					int y = ((iy == 0) ? ws.api.World.SeaLevel : (ws.api.World.BlockAccessor.MapSizeY - 1));
					tmpPos.Set(regionX * regsize + ix * (regsize - 1), y, regionZ * regsize + iz * (regsize - 1));
					ClimateCondition nowcond = null;
					for (int j = 0; j < snaps.Length; j++)
					{
						double nowTotalDays = (LastUpdateTotalHours + (double)j + 0.5) / (double)ws.api.World.Calendar.HoursPerDay;
						if (nowcond == null)
						{
							nowcond = ws.api.World.BlockAccessor.GetClimateAt(tmpPos, EnumGetClimateMode.ForSuppliedDate_TemperatureRainfallOnly, nowTotalDays);
							if (nowcond == null)
							{
								return;
							}
						}
						else
						{
							ws.api.World.BlockAccessor.GetClimateAt(tmpPos, nowcond, EnumGetClimateMode.ForSuppliedDate_TemperatureRainfallOnly, nowTotalDays);
						}
						SnowAccumSnapshot latestSnap2 = snaps[j];
						if (nowcond.Temperature > 1.5f || ((double)nowcond.Rainfall < 0.05 && nowcond.Temperature > 0f))
						{
							latestSnap2.SnowAccumulationByRegionCorner.AddValue(ix, iy, iz, (0f - nowcond.Temperature) / 15f);
						}
						else
						{
							latestSnap2.SnowAccumulationByRegionCorner.AddValue(ix, iy, iz, nowcond.Rainfall / 3f);
						}
					}
				}
			}
		}
		lock (snowAccumSnapshotLock)
		{
			foreach (SnowAccumSnapshot latestSnap in snaps)
			{
				SnowAccumSnapshots.Add(latestSnap);
				latestSnap.Checks++;
			}
		}
		LastUpdateTotalHours += count;
	}

	public void TickEvery25ms(float dt)
	{
		if (ws.api.Side == EnumAppSide.Client)
		{
			clientUpdate(dt);
		}
		else
		{
			int hourCount = (int)(ws.api.World.Calendar.TotalHours - LastUpdateTotalHours);
			if (hourCount > 0)
			{
				UpdateSnowAccumulation(Math.Min(hourCount, 480));
			}
			ws.api.World.FrameProfiler.Mark("snowaccum");
			Random rnd = ws.api.World.Rand;
			float targetLightninMinTemp = CurWeatherEvent.State.LightningMinTemp;
			if (rnd.NextDouble() < (double)CurWeatherEvent.State.LightningRate)
			{
				ClimateCondition nowcond2 = ws.api.World.BlockAccessor.GetClimateAt(regionCenterPos);
				if (nowcond2.Temperature >= targetLightninMinTemp && (double)nowcond2.RainCloudOverlay > 0.15)
				{
					Vec3d pos = regionCenterPos.ToVec3d().Add(-200.0 + rnd.NextDouble() * 400.0, ws.api.World.SeaLevel, -200.0 + rnd.NextDouble() * 400.0);
					ws.SpawnLightningFlash(pos);
				}
				ws.api.World.FrameProfiler.Mark("lightningcheck");
			}
		}
		if (Transitioning)
		{
			float speed = ws.api.World.Calendar.SpeedOfTime / 60f;
			Weight += dt / TransitionDelay * speed;
			if (Weight > 1f)
			{
				Transitioning = false;
				Weight = 1f;
			}
		}
		else if (ws.autoChangePatterns && ws.api.Side == EnumAppSide.Server && ws.api.World.Calendar.TotalHours > NewWePattern.State.ActiveUntilTotalHours)
		{
			TriggerTransition();
			ws.api.World.FrameProfiler.Mark("weathertransition");
		}
		if (ws.autoChangePatterns && ws.api.Side == EnumAppSide.Server)
		{
			bool sendPacket = false;
			if (ws.api.World.Calendar.TotalHours > CurWindPattern.State.ActiveUntilTotalHours)
			{
				CurWindPattern = WindPatterns[Rand.NextInt(WindPatterns.Length)];
				CurWindPattern.OnBeginUse();
				sendPacket = true;
			}
			if (ws.api.World.Calendar.TotalHours > CurWeatherEvent.State.ActiveUntilTotalHours || CurWeatherEvent.ShouldStop(weatherData.climateCond.Rainfall, weatherData.climateCond.Temperature))
			{
				selectRandomWeatherEvent();
				sendPacket = true;
			}
			if (sendPacket)
			{
				sendWeatherUpdatePacket();
			}
			ws.api.World.FrameProfiler.Mark("weatherchange");
		}
		NewWePattern.Update(dt);
		OldWePattern.Update(dt);
		CurWindPattern.Update(dt);
		CurWeatherEvent.Update(dt);
		float curWindSpeed = weatherData.curWindSpeed.X;
		float targetWindSpeed = (float)GetWindSpeed(ws.api.World.SeaLevel);
		curWindSpeed += GameMath.Clamp((targetWindSpeed - curWindSpeed) * dt, -0.001f, 0.001f);
		weatherData.curWindSpeed.X = curWindSpeed;
		quarterSecAccum += dt;
		if (quarterSecAccum > 0.25f)
		{
			regionCenterPos.Y = ws.api.World.BlockAccessor.GetRainMapHeightAt(regionCenterPos);
			if (regionCenterPos.Y == 0)
			{
				regionCenterPos.Y = ws.api.World.SeaLevel;
			}
			ClimateCondition nowcond = ws.api.World.BlockAccessor.GetClimateAt(regionCenterPos);
			if (nowcond != null)
			{
				weatherData.climateCond = nowcond;
			}
			quarterSecAccum = 0f;
		}
		weatherData.BlendedPrecType = CurWeatherEvent.State.PrecType;
	}

	public void selectRandomWeatherEvent()
	{
		CurWeatherEvent = RandomWeatherEvent();
		CurWeatherEvent.OnBeginUse();
	}

	public void sendWeatherUpdatePacket()
	{
		wsServer.SendWeatherStateUpdate(new WeatherState
		{
			RegionX = regionX,
			RegionZ = regionZ,
			NewPattern = NewWePattern.State,
			OldPattern = OldWePattern.State,
			WindPattern = CurWindPattern.State,
			WeatherEvent = CurWeatherEvent?.State,
			TransitionDelay = TransitionDelay,
			Transitioning = Transitioning,
			Weight = Weight,
			LcgCurrentSeed = Rand.currentSeed,
			LcgMapGenSeed = Rand.mapGenSeed,
			LcgWorldSeed = Rand.worldSeed
		});
	}

	private void clientUpdate(float dt)
	{
		EntityPlayer eplr = (ws.api as ICoreClientAPI).World.Player.Entity;
		regionCenterPos.Y = (int)eplr.Pos.Y;
		float targetNearLightningRate = CurWeatherEvent.State.NearThunderRate;
		float targetDistantLightningRate = CurWeatherEvent.State.DistantThunderRate;
		float targetLightninMinTemp = CurWeatherEvent.State.LightningMinTemp;
		weatherData.nearLightningRate += GameMath.Clamp((targetNearLightningRate - weatherData.nearLightningRate) * dt, -0.001f, 0.001f);
		weatherData.distantLightningRate += GameMath.Clamp((targetDistantLightningRate - weatherData.distantLightningRate) * dt, -0.001f, 0.001f);
		weatherData.lightningMinTemp += GameMath.Clamp((targetLightninMinTemp - weatherData.lightningMinTemp) * dt, -0.001f, 0.001f);
		weatherData.BlendedPrecType = CurWeatherEvent.State.PrecType;
	}

	public double GetWindSpeed(double posY)
	{
		if (CurWindPattern == null)
		{
			return 0.0;
		}
		double strength = CurWindPattern.Strength;
		if (posY > (double)ws.api.World.SeaLevel)
		{
			strength *= Math.Max(1.0, 0.9 + (posY - (double)ws.api.World.SeaLevel) / 100.0);
			return Math.Min(strength, 1.5);
		}
		return strength / (1.0 + ((double)ws.api.World.SeaLevel - posY) / 4.0);
	}

	public EnumPrecipitationType GetPrecipitationType()
	{
		return weatherData.BlendedPrecType;
	}

	public bool SetWindPattern(string code, bool updateInstant)
	{
		WindPattern pattern = WindPatterns.FirstOrDefault((WindPattern p) => p.config.Code == code);
		if (pattern == null)
		{
			return false;
		}
		CurWindPattern = pattern;
		CurWindPattern.OnBeginUse();
		sendState(updateInstant);
		return true;
	}

	public bool SetWeatherEvent(string code, bool updateInstant)
	{
		WeatherEvent pattern = WeatherEvents.FirstOrDefault((WeatherEvent p) => p.config.Code == code);
		if (pattern == null)
		{
			return false;
		}
		CurWeatherEvent = pattern;
		CurWeatherEvent.OnBeginUse();
		sendState(updateInstant);
		return true;
	}

	public bool SetWeatherPattern(string code, bool updateInstant)
	{
		WeatherPattern pattern = WeatherPatterns.FirstOrDefault((WeatherPattern p) => p.config.Code == code);
		if (pattern == null)
		{
			return false;
		}
		OldWePattern = NewWePattern;
		NewWePattern = pattern;
		Weight = 1f;
		Transitioning = false;
		TransitionDelay = 0f;
		if (NewWePattern != OldWePattern || updateInstant)
		{
			NewWePattern.OnBeginUse();
		}
		UpdateWeatherData();
		sendState(updateInstant);
		return true;
	}

	private void sendState(bool updateInstant)
	{
		wsServer.SendWeatherStateUpdate(new WeatherState
		{
			RegionX = regionX,
			RegionZ = regionZ,
			NewPattern = NewWePattern.State,
			OldPattern = OldWePattern.State,
			WindPattern = CurWindPattern.State,
			WeatherEvent = CurWeatherEvent?.State,
			TransitionDelay = 0f,
			Transitioning = false,
			Weight = Weight,
			updateInstant = updateInstant,
			LcgCurrentSeed = Rand.currentSeed,
			LcgMapGenSeed = Rand.mapGenSeed,
			LcgWorldSeed = Rand.worldSeed
		});
	}

	public void TriggerTransition()
	{
		TriggerTransition(30f + Rand.NextFloat() * 60f * 60f / ws.api.World.Calendar.SpeedOfTime);
	}

	public void TriggerTransition(float delay)
	{
		Transitioning = true;
		TransitionDelay = delay;
		Weight = 0f;
		OldWePattern = NewWePattern;
		NewWePattern = RandomWeatherPattern();
		if (NewWePattern != OldWePattern)
		{
			NewWePattern.OnBeginUse();
		}
		wsServer.SendWeatherStateUpdate(new WeatherState
		{
			RegionX = regionX,
			RegionZ = regionZ,
			NewPattern = NewWePattern.State,
			OldPattern = OldWePattern.State,
			WindPattern = CurWindPattern.State,
			WeatherEvent = CurWeatherEvent?.State,
			TransitionDelay = TransitionDelay,
			Transitioning = true,
			Weight = Weight,
			LcgCurrentSeed = Rand.currentSeed,
			LcgMapGenSeed = Rand.mapGenSeed,
			LcgWorldSeed = Rand.worldSeed
		});
	}

	public WeatherEvent RandomWeatherEvent()
	{
		float totalChance = 0f;
		for (int j = 0; j < WeatherEvents.Length; j++)
		{
			WeatherEvents[j].updateHereChance(weatherData.climateCond.WorldgenRainfall, weatherData.climateCond.Temperature);
			totalChance += WeatherEvents[j].hereChance;
		}
		float rndVal = Rand.NextFloat() * totalChance;
		for (int i = 0; i < WeatherEvents.Length; i++)
		{
			rndVal -= WeatherEvents[i].config.Weight;
			if (rndVal <= 0f)
			{
				return WeatherEvents[i];
			}
		}
		return WeatherEvents[WeatherEvents.Length - 1];
	}

	public WeatherPattern RandomWeatherPattern()
	{
		float totalChance = 0f;
		for (int j = 0; j < WeatherPatterns.Length; j++)
		{
			WeatherPatterns[j].updateHereChance(weatherData.climateCond.Rainfall, weatherData.climateCond.Temperature);
			totalChance += WeatherPatterns[j].hereChance;
		}
		float rndVal = Rand.NextFloat() * totalChance;
		for (int i = 0; i < WeatherPatterns.Length; i++)
		{
			rndVal -= WeatherPatterns[i].hereChance;
			if (rndVal <= 0f)
			{
				return WeatherPatterns[i];
			}
		}
		return WeatherPatterns[WeatherPatterns.Length - 1];
	}

	public double GetBlendedCloudThicknessAt(int cloudTilePosX, int cloudTilePosZ)
	{
		if (IsDummy)
		{
			return 0.0;
		}
		int x = cloudTilePosX - cloudTilebasePosX;
		int z = cloudTilePosZ - cloudTilebasePosZ;
		return NewWePattern.GetCloudDensityAt(x, z) * (double)Weight + OldWePattern.GetCloudDensityAt(x, z) * (double)(1f - Weight);
	}

	public double GetBlendedCloudOpaqueness()
	{
		return NewWePattern.State.nowbaseOpaqueness * Weight + OldWePattern.State.nowbaseOpaqueness * (1f - Weight);
	}

	public double GetBlendedCloudBrightness(float b)
	{
		float w = weatherData.Ambient.CloudBrightness.Weight;
		float bc = weatherData.Ambient.CloudBrightness.Value * weatherData.Ambient.SceneBrightness.Value;
		return b * (1f - w) + bc * w;
	}

	public double GetBlendedThinCloudModeness()
	{
		return NewWePattern.State.nowThinCloudModeness * Weight + OldWePattern.State.nowThinCloudModeness * (1f - Weight);
	}

	public double GetBlendedUndulatingCloudModeness()
	{
		return NewWePattern.State.nowUndulatingCloudModeness * Weight + OldWePattern.State.nowUndulatingCloudModeness * (1f - Weight);
	}

	internal void EnsureCloudTileCacheIsFresh(Vec3i tilePos)
	{
		if (!IsDummy)
		{
			NewWePattern.EnsureCloudTileCacheIsFresh(tilePos);
			OldWePattern.EnsureCloudTileCacheIsFresh(tilePos);
		}
	}

	public byte[] ToBytes()
	{
		return SerializerUtil.Serialize(new WeatherState
		{
			NewPattern = (NewWePattern?.State ?? null),
			OldPattern = (OldWePattern?.State ?? null),
			WindPattern = (CurWindPattern?.State ?? null),
			WeatherEvent = (CurWeatherEvent?.State ?? null),
			Weight = Weight,
			TransitionDelay = TransitionDelay,
			Transitioning = Transitioning,
			LastUpdateTotalHours = LastUpdateTotalHours,
			LcgCurrentSeed = Rand.currentSeed,
			LcgMapGenSeed = Rand.mapGenSeed,
			LcgWorldSeed = Rand.worldSeed,
			SnowAccumSnapshots = SnowAccumSnapshots.Values,
			Ringarraycursor = SnowAccumSnapshots.EndPosition
		});
	}

	internal void FromBytes(byte[] data)
	{
		if (data == null)
		{
			LoadRandomPattern();
			NewWePattern.OnBeginUse();
			return;
		}
		WeatherState state = SerializerUtil.Deserialize<WeatherState>(data);
		if (state.NewPattern != null)
		{
			NewWePattern = WeatherPatterns[GameMath.Clamp(state.NewPattern.Index, 0, WeatherPatterns.Length - 1)];
			NewWePattern.State = state.NewPattern;
		}
		else
		{
			NewWePattern = WeatherPatterns[0];
		}
		if (state.OldPattern != null && state.OldPattern.Index < WeatherPatterns.Length)
		{
			OldWePattern = WeatherPatterns[GameMath.Clamp(state.OldPattern.Index, 0, WeatherPatterns.Length - 1)];
			OldWePattern.State = state.OldPattern;
		}
		else
		{
			OldWePattern = WeatherPatterns[0];
		}
		if (state.WindPattern != null)
		{
			CurWindPattern = WindPatterns[GameMath.Clamp(state.WindPattern.Index, 0, WindPatterns.Length - 1)];
			CurWindPattern.State = state.WindPattern;
		}
		Weight = state.Weight;
		TransitionDelay = state.TransitionDelay;
		Transitioning = state.Transitioning;
		LastUpdateTotalHours = state.LastUpdateTotalHours;
		Rand.worldSeed = state.LcgWorldSeed;
		Rand.currentSeed = state.LcgCurrentSeed;
		Rand.mapGenSeed = state.LcgMapGenSeed;
		double nowTotalHours = ws.api.World.Calendar.TotalHours;
		LastUpdateTotalHours = Math.Max(LastUpdateTotalHours, nowTotalHours - (double)(ws.api.World.Calendar.DaysPerYear * 24) + ws.api.World.Rand.NextDouble());
		int capacity = (int)((float)ws.api.World.Calendar.DaysPerYear * ws.api.World.Calendar.HoursPerDay) + 1;
		SnowAccumSnapshots = new RingArray<SnowAccumSnapshot>(capacity, state.SnowAccumSnapshots);
		SnowAccumSnapshots.EndPosition = GameMath.Clamp(state.Ringarraycursor, 0, capacity - 1);
		if (state.WeatherEvent != null)
		{
			CurWeatherEvent = WeatherEvents[state.WeatherEvent.Index];
			CurWeatherEvent.State = state.WeatherEvent;
		}
		if (CurWeatherEvent == null)
		{
			CurWeatherEvent = RandomWeatherEvent();
			CurWeatherEvent.OnBeginUse();
		}
	}
}
