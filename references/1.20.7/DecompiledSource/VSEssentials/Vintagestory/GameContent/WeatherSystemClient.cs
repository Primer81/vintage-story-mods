using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherSystemClient : WeatherSystemBase, IRenderer, IDisposable
{
	public static float CurrentEnvironmentWetness4h;

	public ICoreClientAPI capi;

	public IClientNetworkChannel clientChannel;

	public CloudRenderer cloudRenderer;

	public ClimateCondition clientClimateCond;

	public bool playerChunkLoaded;

	private float quarterSecAccum;

	private BlockPos plrPos = new BlockPos();

	private Vec3d plrPosd = new Vec3d();

	public bool haveLevelFinalize;

	public WeatherSimulationSound simSounds;

	public WeatherSimulationParticles simParticles;

	public AuroraRenderer auroraRenderer;

	private long blendedLastCheckedMSDiv60 = -1L;

	private WeatherDataSnapshot blendedWeatherDataCached;

	public WeatherDataReaderPreLoad WeatherDataAtPlayer;

	private Vec3f windSpeedSmoothed = new Vec3f();

	private double windRandCounter;

	private Vec3f surfaceWindSpeedSmoothed = new Vec3f();

	private double surfaceWindRandCounter;

	private float wetnessScanAccum2s;

	private Queue<WeatherState> weatherUpdateQueue = new Queue<WeatherState>();

	public WeatherDataSnapshot BlendedWeatherData
	{
		get
		{
			long msd60 = capi.ElapsedMilliseconds / 60;
			if (msd60 != blendedLastCheckedMSDiv60)
			{
				blendedLastCheckedMSDiv60 = msd60;
				blendedWeatherDataCached = WeatherDataAtPlayer.BlendedWeatherData;
				this.OnGetBlendedWeatherData(blendedWeatherDataCached);
			}
			return blendedWeatherDataCached;
		}
	}

	public double RenderOrder => -0.1;

	public int RenderRange => 999;

	public event Action<WeatherDataSnapshot> OnGetBlendedWeatherData;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI capi)
	{
		this.capi = capi;
		Initialize();
		clientChannel = capi.Network.GetChannel("weather").SetMessageHandler<WeatherState>(OnWeatherUpdatePacket).SetMessageHandler<WeatherConfigPacket>(OnWeatherConfigUpdatePacket)
			.SetMessageHandler<WeatherPatternAssetsPacket>(OnAssetsPacket)
			.SetMessageHandler<LightningFlashPacket>(OnLightningFlashPacket)
			.SetMessageHandler<WeatherCloudYposPacket>(OnCloudLevelRelPacket);
		capi.Event.RegisterGameTickListener(OnClientGameTick, 50);
		capi.Event.LevelFinalize += LevelFinalizeInit;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Before, "weatherSystem");
		capi.Event.RegisterRenderer(this, EnumRenderStage.Done, "weatherSystem");
		capi.Event.LeaveWorld += delegate
		{
			cloudRenderer?.Dispose();
		};
		capi.Event.OnGetClimate += base.Event_OnGetClimate;
		simSounds = new WeatherSimulationSound(capi, this);
		simParticles = new WeatherSimulationParticles(capi, this);
		auroraRenderer = new AuroraRenderer(capi, this);
		capi.Logger.Notification("Initialised WeatherSystemClient. simLightning is " + ((simLightning == null) ? "null." : "loaded."));
	}

	private void OnCloudLevelRelPacket(WeatherCloudYposPacket msg)
	{
		CloudLevelRel = msg.CloudYRel;
	}

	private void OnAssetsPacket(WeatherPatternAssetsPacket msg)
	{
		WeatherPatternAssets p = JsonUtil.FromString<WeatherPatternAssets>(msg.Data);
		GeneralConfig = p.GeneralConfig;
		GeneralConfig.Init(api.World);
		WeatherConfigs = p.WeatherConfigs;
		WindConfigs = p.WindConfigs;
		WeatherEventConfigs = p.WeatherEventConfigs;
		foreach (KeyValuePair<long, WeatherSimulationRegion> item in weatherSimByMapRegion)
		{
			item.Value.ReloadPatterns(api.World.Seed);
		}
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		try
		{
			simLightning.OnRenderFrame(dt, stage);
		}
		catch (Exception e)
		{
			if (simLightning == null)
			{
				api.Logger.Error("simLightning was null! Please report this as a bug");
			}
			api.Logger.Error(e);
		}
		if (stage != 0)
		{
			return;
		}
		EntityPlayer eplr = capi.World.Player.Entity;
		plrPos.Set((int)eplr.Pos.X, (int)eplr.Pos.Y, (int)eplr.Pos.Z);
		plrPosd.Set(eplr.Pos.X, eplr.Pos.Y, eplr.Pos.Z);
		WeatherDataAtPlayer.LoadAdjacentSimsAndLerpValues(plrPosd, dt);
		WeatherDataAtPlayer.UpdateAdjacentAndBlendWeatherData();
		dt = Math.Min(0.5f, dt);
		Vec3d windspeed = capi.World.BlockAccessor.GetWindSpeedAt(plrPosd);
		windSpeedSmoothed.X += ((float)windspeed.X - windSpeedSmoothed.X) * dt;
		windSpeedSmoothed.Y += ((float)windspeed.Y - windSpeedSmoothed.Y) * dt;
		windSpeedSmoothed.Z += ((float)windspeed.Z - windSpeedSmoothed.Z) * dt;
		windRandCounter = (windRandCounter + (double)dt) % (Math.PI * 2000.0);
		double rndx = (2.0 * Math.Sin(windRandCounter / 8.0) + Math.Sin(windRandCounter / 2.0) + Math.Sin(0.5 + 2.0 * windRandCounter)) / 10.0;
		GlobalConstants.CurrentWindSpeedClient.Set(windSpeedSmoothed.X, windSpeedSmoothed.Y, windSpeedSmoothed.Z + (float)rndx * windSpeedSmoothed.X);
		int rainy = capi.World.BlockAccessor.GetRainMapHeightAt(plrPos.X, plrPos.Z);
		plrPosd.Y = rainy;
		Vec3d surfacewindspeed = capi.World.BlockAccessor.GetWindSpeedAt(plrPosd);
		surfaceWindSpeedSmoothed.X += ((float)surfacewindspeed.X - surfaceWindSpeedSmoothed.X) * dt;
		surfaceWindSpeedSmoothed.Y += ((float)surfacewindspeed.Y - surfaceWindSpeedSmoothed.Y) * dt;
		surfaceWindSpeedSmoothed.Z += ((float)surfacewindspeed.Z - surfaceWindSpeedSmoothed.Z) * dt;
		surfaceWindRandCounter = (surfaceWindRandCounter + (double)dt) % (Math.PI * 2000.0);
		rndx = (2.0 * Math.Sin(surfaceWindRandCounter / 8.0) + Math.Sin(surfaceWindRandCounter / 2.0) + Math.Sin(0.5 + 2.0 * surfaceWindRandCounter)) / 10.0;
		GlobalConstants.CurrentSurfaceWindSpeedClient.Set(surfaceWindSpeedSmoothed.X, surfaceWindSpeedSmoothed.Y, surfaceWindSpeedSmoothed.Z + (float)rndx * surfaceWindSpeedSmoothed.X);
		capi.Ambient.CurrentModifiers["weather"] = WeatherDataAtPlayer.BlendedWeatherData.Ambient;
		wetnessScanAccum2s += dt;
		if (wetnessScanAccum2s > 2f)
		{
			wetnessScanAccum2s = 0f;
			double totalDays = capi.World.Calendar.TotalDays;
			float rainSum = 0f;
			for (int i = 0; i < 12; i++)
			{
				float weight = 1f - (float)i / 20f;
				rainSum += weight * capi.World.BlockAccessor.GetClimateAt(plrPos, EnumGetClimateMode.ForSuppliedDateValues, totalDays - (double)i / 24.0 / 4.0).Rainfall;
			}
			CurrentEnvironmentWetness4h = GameMath.Clamp(rainSum, 0f, 1f);
		}
	}

	private void OnClientGameTick(float dt)
	{
		quarterSecAccum += dt;
		if (quarterSecAccum > 0.25f || clientClimateCond == null)
		{
			clientClimateCond = capi.World.BlockAccessor.GetClimateAt(plrPos);
			quarterSecAccum = 0f;
			playerChunkLoaded |= capi.World.BlockAccessor.GetChunkAtBlockPos(plrPos) != null;
		}
		simLightning.ClientTick(dt);
		for (int i = 0; i < 4; i++)
		{
			WeatherSimulationRegion sim = WeatherDataAtPlayer.AdjacentSims[i];
			if (sim != dummySim)
			{
				sim.TickEvery25ms(dt);
			}
		}
		simSounds.Update(dt);
		rainOverlaySnap.climateCond = clientClimateCond;
		rainOverlaySnap.SetAmbient(rainOverlayPattern, (capi == null) ? 0f : capi.Ambient.Base.FogDensity.Value);
	}

	private void OnWeatherConfigUpdatePacket(WeatherConfigPacket packet)
	{
		OverridePrecipitation = packet.OverridePrecipitation;
		RainCloudDaysOffset = packet.RainCloudDaysOffset;
	}

	private void OnWeatherUpdatePacket(WeatherState msg)
	{
		weatherUpdateQueue.Enqueue(msg);
	}

	public void ProcessWeatherUpdates()
	{
		foreach (WeatherState packet in weatherUpdateQueue)
		{
			ProcessWeatherUpdate(packet);
		}
		weatherUpdateQueue.Clear();
	}

	private void ProcessWeatherUpdate(WeatherState msg)
	{
		WeatherSimulationRegion weatherSim = getOrCreateWeatherSimForRegion(msg.RegionX, msg.RegionZ);
		if (weatherSim == null)
		{
			Console.WriteLine("weatherSim for region {0}/{1} is null. No idea what to do here", msg.RegionX, msg.RegionZ);
			return;
		}
		if (msg.updateInstant)
		{
			weatherSim.ReloadPatterns(api.World.Seed);
			for (int i = 0; i < weatherSim.WeatherPatterns.Length; i++)
			{
				weatherSim.WeatherPatterns[i].Initialize(i, api.World.Seed);
			}
		}
		weatherSim.NewWePattern = weatherSim.WeatherPatterns[Math.Min(weatherSim.WeatherPatterns.Length - 1, msg.NewPattern.Index)];
		weatherSim.NewWePattern.State = msg.NewPattern;
		weatherSim.OldWePattern = weatherSim.WeatherPatterns[Math.Min(weatherSim.WeatherPatterns.Length - 1, msg.OldPattern.Index)];
		weatherSim.OldWePattern.State = msg.OldPattern;
		weatherSim.TransitionDelay = msg.TransitionDelay;
		weatherSim.Transitioning = msg.Transitioning;
		weatherSim.Weight = msg.Weight;
		weatherSim.CurWindPattern = weatherSim.WindPatterns[Math.Min(weatherSim.WindPatterns.Length - 1, msg.WindPattern.Index)];
		weatherSim.CurWindPattern.State = msg.WindPattern;
		weatherSim.CurWeatherEvent = weatherSim.WeatherEvents[Math.Min(weatherSim.WeatherEvents.Length - 1, msg.WeatherEvent.Index)];
		weatherSim.CurWeatherEvent.State = msg.WeatherEvent;
		if (msg.updateInstant)
		{
			weatherSim.NewWePattern.OnBeginUse();
			cloudRenderer.instantTileBlend = true;
		}
		if (msg.Transitioning)
		{
			weatherSim.Weight = 0f;
		}
		if (msg.updateInstant)
		{
			weatherSim.TickEvery25ms(0.025f);
			cloudRenderer.UpdateCloudTiles(32767);
		}
	}

	private void LevelFinalizeInit()
	{
		InitDummySim();
		WeatherDataAtPlayer = getWeatherDataReaderPreLoad();
		WeatherDataSlowAccess = getWeatherDataReader();
		simSounds.Initialize();
		simParticles.Initialize();
		cloudRenderer = new CloudRenderer(capi, this);
		capi.Ambient.CurrentModifiers.InsertBefore("serverambient", "weather", WeatherDataAtPlayer.BlendedWeatherData.Ambient);
		haveLevelFinalize = true;
		capi.Ambient.UpdateAmbient(0.1f);
		cloudRenderer.CloudTick(0.1f);
		capi.Logger.VerboseDebug("Done init WeatherSystemClient");
	}

	public override void Dispose()
	{
		base.Dispose();
		simSounds?.Dispose();
	}

	private void OnLightningFlashPacket(LightningFlashPacket msg)
	{
		if (capi.World.Player != null)
		{
			simLightning.genLightningFlash(msg.Pos, msg.Seed);
		}
	}

	public override void SpawnLightningFlash(Vec3d pos)
	{
		simLightning.genLightningFlash(pos);
	}
}
