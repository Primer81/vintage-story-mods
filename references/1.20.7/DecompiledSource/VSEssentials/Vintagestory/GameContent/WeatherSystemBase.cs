using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class WeatherSystemBase : ModSystem
{
	public ICoreAPI api;

	public WeatherSystemConfig GeneralConfig;

	public WeatherPatternConfig[] WeatherConfigs;

	public WindPatternConfig[] WindConfigs;

	public WeatherEventConfig[] WeatherEventConfigs;

	public bool autoChangePatterns = true;

	public Dictionary<long, WeatherSimulationRegion> weatherSimByMapRegion = new Dictionary<long, WeatherSimulationRegion>();

	protected SimplexNoise precipitationNoise;

	protected SimplexNoise precipitationNoiseSub;

	public WeatherSimulationRegion dummySim;

	public WeatherDataReader WeatherDataSlowAccess;

	public WeatherPattern rainOverlayPattern;

	public WeatherDataSnapshot rainOverlaySnap;

	public WeatherSimulationLightning simLightning;

	private object weatherSimByMapRegionLock = new object();

	public virtual float? OverridePrecipitation { get; set; }

	public virtual double RainCloudDaysOffset { get; set; }

	public virtual int CloudTileSize { get; set; } = 50;


	public virtual float CloudLevelRel { get; set; } = 1f;


	public event LightningImpactDelegate OnLightningImpactBegin;

	public event LightningImpactDelegate OnLightningImpactEnd;

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		api.Network.RegisterChannel("weather").RegisterMessageType(typeof(WeatherState)).RegisterMessageType(typeof(WeatherConfigPacket))
			.RegisterMessageType(typeof(WeatherPatternAssetsPacket))
			.RegisterMessageType(typeof(LightningFlashPacket))
			.RegisterMessageType(typeof(WeatherCloudYposPacket));
		api.Event.OnGetWindSpeed += Event_OnGetWindSpeed;
	}

	private void Event_OnGetWindSpeed(Vec3d pos, ref Vec3d windSpeed)
	{
		windSpeed.X = WeatherDataSlowAccess.GetWindSpeed(pos);
	}

	public void Initialize()
	{
		precipitationNoise = SimplexNoise.FromDefaultOctaves(4, 1.0 / 150.0, 0.95, api.World.Seed - 18971121);
		precipitationNoiseSub = SimplexNoise.FromDefaultOctaves(3, 1.0 / 750.0, 0.95, api.World.Seed - 1717121);
		simLightning = new WeatherSimulationLightning(api, this);
	}

	public void InitDummySim()
	{
		dummySim = new WeatherSimulationRegion(this, 0, 0);
		dummySim.IsDummy = true;
		dummySim.Initialize();
		LCGRandom rand = new LCGRandom(api.World.Seed);
		rand.InitPositionSeed(3, 3);
		rainOverlayPattern = new WeatherPattern(this, GeneralConfig.RainOverlayPattern, rand, 0, 0);
		rainOverlayPattern.Initialize(0, api.World.Seed);
		rainOverlayPattern.OnBeginUse();
		rainOverlaySnap = new WeatherDataSnapshot();
	}

	public double GetEnvironmentWetness(BlockPos pos, double days, double hourResolution = 2.0)
	{
		double num = api.World.Calendar.TotalDays - days;
		double endDays = api.World.Calendar.TotalDays;
		double rainSum = 0.0;
		double nowDay = num;
		double hpd = api.World.Calendar.HoursPerDay;
		double weight = 1.0 / 84.0;
		for (; nowDay < endDays; nowDay += hourResolution / hpd)
		{
			rainSum += weight * (double)api.World.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDateValues, nowDay).Rainfall;
		}
		return GameMath.Clamp(rainSum, 0.0, 1.0);
	}

	public PrecipitationState GetPrecipitationState(Vec3d pos)
	{
		return GetPrecipitationState(pos, api.World.Calendar.TotalDays);
	}

	public PrecipitationState GetPrecipitationState(Vec3d pos, double totalDays)
	{
		float level = GetPrecipitation(pos.X, pos.Y, pos.Z, totalDays);
		return new PrecipitationState
		{
			Level = Math.Max(0f, level - 0.5f),
			ParticleSize = Math.Max(0f, level - 0.5f),
			Type = ((level > 0f) ? WeatherDataSlowAccess.GetPrecType(pos) : EnumPrecipitationType.Auto)
		};
	}

	public float GetPrecipitation(Vec3d pos)
	{
		return GetPrecipitation(pos.X, pos.Y, pos.Z, api.World.Calendar.TotalDays);
	}

	public float GetPrecipitation(double posX, double posY, double posZ)
	{
		return GetPrecipitation(posX, posY, posZ, api.World.Calendar.TotalDays);
	}

	public float GetPrecipitation(double posX, double posY, double posZ, double totalDays)
	{
		ClimateCondition conds = api.World.BlockAccessor.GetClimateAt(new BlockPos((int)posX, (int)posY, (int)posZ), EnumGetClimateMode.WorldGenValues, totalDays);
		return Math.Max(0f, GetRainCloudness(conds, posX, posZ, totalDays) - 0.5f);
	}

	public float GetPrecipitation(BlockPos pos, double totalDays, ClimateCondition conds)
	{
		return Math.Max(0f, GetRainCloudness(conds, (double)pos.X + 0.5, (double)pos.Z + 0.5, totalDays) - 0.5f);
	}

	protected void Event_OnGetClimate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0)
	{
		if (mode != 0 && mode != EnumGetClimateMode.ForSuppliedDate_TemperatureOnly)
		{
			float rainCloudness = GetRainCloudness(climate, (double)pos.X + 0.5, (double)pos.Z + 0.5, totalDays);
			climate.Rainfall = GameMath.Clamp(rainCloudness - 0.5f, 0f, 1f);
			climate.RainCloudOverlay = GameMath.Clamp(rainCloudness, 0f, 1f);
		}
	}

	public float GetRainCloudness(ClimateCondition conds, double posX, double posZ, double totalDays)
	{
		if (OverridePrecipitation.HasValue)
		{
			return OverridePrecipitation.Value + 0.5f;
		}
		float offset = 0f;
		if (conds != null)
		{
			offset = GameMath.Clamp((conds.Rainfall - 0.6f) * 2f, -1f, 1f);
		}
		return getPrecipNoise(posX, posZ, totalDays + RainCloudDaysOffset, offset);
	}

	public ClimateCondition GetClimateFast(BlockPos pos, int climate)
	{
		return api.World.BlockAccessor.GetClimateAt(pos, climate);
	}

	private float getPrecipNoise(double posX, double posZ, double totalDays, float wgenRain)
	{
		return (float)GameMath.Max(precipitationNoise.Noise(posX / 9.0 / 2.0 + totalDays * 18.0, posZ / 9.0 / 2.0, totalDays * 4.0) * 1.600000023841858 - GameMath.Clamp(precipitationNoiseSub.Noise(posX / 4.0 / 2.0 + totalDays * 24.0, posZ / 4.0 / 2.0, totalDays * 6.0) * 5.0 - 1.0 - (double)wgenRain, 0.0, 1.0) + (double)wgenRain, 0.0);
	}

	public WeatherDataReader getWeatherDataReader()
	{
		return new WeatherDataReader(api, this);
	}

	public WeatherDataReaderPreLoad getWeatherDataReaderPreLoad()
	{
		return new WeatherDataReaderPreLoad(api, this);
	}

	public WeatherSimulationRegion getOrCreateWeatherSimForRegion(int regionX, int regionZ)
	{
		long index2d = MapRegionIndex2D(regionX, regionZ);
		IMapRegion mapregion = api.World.BlockAccessor.GetMapRegion(regionX, regionZ);
		if (mapregion == null)
		{
			return null;
		}
		return getOrCreateWeatherSimForRegion(index2d, mapregion);
	}

	public WeatherSimulationRegion getOrCreateWeatherSimForRegion(long index2d, IMapRegion mapregion)
	{
		Vec3i regioncoord = MapRegionPosFromIndex2D(index2d);
		WeatherSimulationRegion weatherSim;
		lock (weatherSimByMapRegionLock)
		{
			if (weatherSimByMapRegion.TryGetValue(index2d, out weatherSim))
			{
				return weatherSim;
			}
		}
		weatherSim = new WeatherSimulationRegion(this, regioncoord.X, regioncoord.Z);
		weatherSim.Initialize();
		mapregion.RemoveModdata("weather");
		byte[] data = mapregion.GetModdata("weatherState");
		if (data != null)
		{
			try
			{
				weatherSim.FromBytes(data);
			}
			catch (Exception)
			{
				weatherSim.LoadRandomPattern();
				weatherSim.NewWePattern.OnBeginUse();
			}
		}
		else
		{
			weatherSim.LoadRandomPattern();
			weatherSim.NewWePattern.OnBeginUse();
			mapregion.SetModdata("weatherState", weatherSim.ToBytes());
		}
		weatherSim.MapRegion = mapregion;
		lock (weatherSimByMapRegionLock)
		{
			weatherSimByMapRegion[index2d] = weatherSim;
			return weatherSim;
		}
	}

	public long MapRegionIndex2D(int regionX, int regionZ)
	{
		return (long)regionZ * (long)api.World.BlockAccessor.RegionMapSizeX + regionX;
	}

	public Vec3i MapRegionPosFromIndex2D(long index)
	{
		return new Vec3i((int)(index % api.World.BlockAccessor.RegionMapSizeX), 0, (int)(index / api.World.BlockAccessor.RegionMapSizeX));
	}

	public virtual void SpawnLightningFlash(Vec3d pos)
	{
	}

	internal void TriggerOnLightningImpactStart(ref Vec3d impactPos, out EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (this.OnLightningImpactBegin != null)
		{
			TriggerOnLightningImpactAny(ref impactPos, out handling, this.OnLightningImpactBegin.GetInvocationList());
		}
	}

	internal void TriggerOnLightningImpactEnd(Vec3d impactPos, out EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (this.OnLightningImpactEnd != null)
		{
			TriggerOnLightningImpactAny(ref impactPos, out handling, this.OnLightningImpactEnd.GetInvocationList());
		}
	}

	internal void TriggerOnLightningImpactAny(ref Vec3d pos, out EnumHandling handling, Delegate[] delegates)
	{
		handling = EnumHandling.PassThrough;
		for (int i = 0; i < delegates.Length; i++)
		{
			LightningImpactDelegate obj = (LightningImpactDelegate)delegates[i];
			EnumHandling delehandling = EnumHandling.PassThrough;
			obj(ref pos, ref delehandling);
			switch (delehandling)
			{
			case EnumHandling.PreventSubsequent:
				handling = EnumHandling.PreventSubsequent;
				return;
			case EnumHandling.PreventDefault:
				handling = EnumHandling.PreventDefault;
				break;
			}
		}
	}
}
