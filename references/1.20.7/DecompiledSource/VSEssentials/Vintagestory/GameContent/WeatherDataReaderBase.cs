using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class WeatherDataReaderBase
{
	public WeatherDataSnapshot BlendedWeatherData = new WeatherDataSnapshot();

	protected WeatherDataSnapshot blendedWeatherDataNoPrec = new WeatherDataSnapshot();

	protected WeatherDataSnapshot topBlendedWeatherData = new WeatherDataSnapshot();

	protected WeatherDataSnapshot botBlendedWeatherData = new WeatherDataSnapshot();

	public WeatherSimulationRegion[] AdjacentSims = new WeatherSimulationRegion[4];

	public double LerpLeftRight;

	public double LerpTopBot;

	private ICoreAPI api;

	private WeatherSystemBase ws;

	private WeatherPattern rainOverlayData;

	private WeatherDataSnapshot rainSnapData;

	public float lerpRainCloudOverlay;

	public float lerpRainOverlay;

	private BlockPos tmpPos = new BlockPos();

	private IMapRegion hereMapRegion;

	public WeatherDataReaderBase(ICoreAPI api, WeatherSystemBase ws)
	{
		this.api = api;
		this.ws = ws;
		BlendedWeatherData.Ambient = new AmbientModifier().EnsurePopulated();
		blendedWeatherDataNoPrec.Ambient = new AmbientModifier().EnsurePopulated();
		AdjacentSims[0] = ws.dummySim;
		AdjacentSims[1] = ws.dummySim;
		AdjacentSims[2] = ws.dummySim;
		AdjacentSims[3] = ws.dummySim;
	}

	public void LoadAdjacentSims(Vec3d pos)
	{
		int regSize = api.World.BlockAccessor.RegionSize;
		int hereRegX = (int)pos.X / regSize;
		int hereRegZ = (int)pos.Z / regSize;
		int topLeftRegX = (int)Math.Round(pos.X / (double)regSize) - 1;
		int topLeftRegZ = (int)Math.Round(pos.Z / (double)regSize) - 1;
		int i = 0;
		for (int dx = 0; dx <= 1; dx++)
		{
			for (int dz = 0; dz <= 1; dz++)
			{
				int regX = topLeftRegX + dx;
				int regZ = topLeftRegZ + dz;
				WeatherSimulationRegion weatherSim = ws.getOrCreateWeatherSimForRegion(regX, regZ);
				if (weatherSim == null)
				{
					weatherSim = ws.dummySim;
				}
				AdjacentSims[i++] = weatherSim;
				if (regX == hereRegX && regZ == hereRegZ)
				{
					hereMapRegion = weatherSim.MapRegion;
				}
			}
		}
	}

	public void LoadAdjacentSimsAndLerpValues(Vec3d pos, bool useArgValues, float lerpRainCloudOverlay = 0f, float lerpRainOverlay = 0f, float dt = 1f)
	{
		LoadAdjacentSims(pos);
		LoadLerp(pos, useArgValues, lerpRainCloudOverlay, lerpRainOverlay, dt);
	}

	public void LoadLerp(Vec3d pos, bool useArgValues, float lerpRainCloudOverlay = 0f, float lerpRainOverlay = 0f, float dt = 1f)
	{
		int regSize = api.World.BlockAccessor.RegionSize;
		double regionRelX = pos.X / (double)regSize - (double)(int)Math.Round(pos.X / (double)regSize);
		double regionRelZ = pos.Z / (double)regSize - (double)(int)Math.Round(pos.Z / (double)regSize);
		LerpTopBot = GameMath.Smootherstep(regionRelX + 0.5);
		LerpLeftRight = GameMath.Smootherstep(regionRelZ + 0.5);
		rainOverlayData = ws.rainOverlayPattern;
		rainSnapData = ws.rainOverlaySnap;
		if (hereMapRegion == null)
		{
			this.lerpRainCloudOverlay = 0f;
			this.lerpRainOverlay = 0f;
			return;
		}
		if (useArgValues)
		{
			this.lerpRainCloudOverlay = lerpRainCloudOverlay;
			this.lerpRainOverlay = lerpRainOverlay;
			return;
		}
		tmpPos.Set((int)pos.X, (int)pos.Y, (int)pos.Z);
		int noiseSizeClimate = hereMapRegion.ClimateMap.InnerSize;
		int climate = 8421504;
		if (noiseSizeClimate > 0)
		{
			double posXInRegionClimate = Math.Max(0.0, (pos.X / (double)regSize - (double)((int)pos.X / regSize)) * (double)noiseSizeClimate);
			double posZInRegionClimate = Math.Max(0.0, (pos.Z / (double)regSize - (double)((int)pos.Z / regSize)) * (double)noiseSizeClimate);
			climate = hereMapRegion.ClimateMap.GetUnpaddedColorLerped((float)posXInRegionClimate, (float)posZInRegionClimate);
		}
		ClimateCondition conds = ws.GetClimateFast(tmpPos, climate);
		float tspeed = Math.Min(1f, dt * 10f);
		this.lerpRainCloudOverlay += (conds.RainCloudOverlay - this.lerpRainCloudOverlay) * tspeed;
		this.lerpRainOverlay += (conds.Rainfall - this.lerpRainOverlay) * tspeed;
	}

	protected void updateAdjacentAndBlendWeatherData()
	{
		AdjacentSims[0].UpdateWeatherData();
		AdjacentSims[1].UpdateWeatherData();
		AdjacentSims[2].UpdateWeatherData();
		AdjacentSims[3].UpdateWeatherData();
		topBlendedWeatherData.SetLerped(AdjacentSims[0].weatherData, AdjacentSims[1].weatherData, (float)LerpLeftRight);
		botBlendedWeatherData.SetLerped(AdjacentSims[2].weatherData, AdjacentSims[3].weatherData, (float)LerpLeftRight);
		blendedWeatherDataNoPrec.SetLerped(topBlendedWeatherData, botBlendedWeatherData, (float)LerpTopBot);
		blendedWeatherDataNoPrec.Ambient.CloudBrightness.Weight = 0f;
		BlendedWeatherData.SetLerpedPrec(blendedWeatherDataNoPrec, rainSnapData, lerpRainOverlay);
	}

	protected void ensureCloudTileCacheIsFresh(Vec3i tilePos)
	{
		AdjacentSims[0].EnsureCloudTileCacheIsFresh(tilePos);
		AdjacentSims[1].EnsureCloudTileCacheIsFresh(tilePos);
		AdjacentSims[2].EnsureCloudTileCacheIsFresh(tilePos);
		AdjacentSims[3].EnsureCloudTileCacheIsFresh(tilePos);
	}

	protected EnumPrecipitationType pgGetPrecType()
	{
		if (LerpTopBot <= 0.5)
		{
			if (!(LerpLeftRight <= 0.5))
			{
				return AdjacentSims[1].GetPrecipitationType();
			}
			return AdjacentSims[0].GetPrecipitationType();
		}
		if (!(LerpLeftRight <= 0.5))
		{
			return AdjacentSims[3].GetPrecipitationType();
		}
		return AdjacentSims[2].GetPrecipitationType();
	}

	protected double pgetWindSpeed(double posY)
	{
		return GameMath.BiLerp(AdjacentSims[0].GetWindSpeed(posY), AdjacentSims[1].GetWindSpeed(posY), AdjacentSims[2].GetWindSpeed(posY), AdjacentSims[3].GetWindSpeed(posY), LerpLeftRight, LerpTopBot);
	}

	protected double pgetBlendedCloudThicknessAt(int cloudTileX, int cloudTileZ)
	{
		double v = GameMath.BiLerp(AdjacentSims[0].GetBlendedCloudThicknessAt(cloudTileX, cloudTileZ), AdjacentSims[1].GetBlendedCloudThicknessAt(cloudTileX, cloudTileZ), AdjacentSims[2].GetBlendedCloudThicknessAt(cloudTileX, cloudTileZ), AdjacentSims[3].GetBlendedCloudThicknessAt(cloudTileX, cloudTileZ), LerpLeftRight, LerpTopBot);
		double rainThick = rainOverlayData.State.nowbaseThickness;
		return GameMath.Lerp(v, rainThick, lerpRainCloudOverlay);
	}

	protected double pgetBlendedCloudOpaqueness()
	{
		double v = GameMath.BiLerp(AdjacentSims[0].GetBlendedCloudOpaqueness(), AdjacentSims[1].GetBlendedCloudOpaqueness(), AdjacentSims[2].GetBlendedCloudOpaqueness(), AdjacentSims[3].GetBlendedCloudOpaqueness(), LerpLeftRight, LerpTopBot);
		double rainopaque = rainOverlayData.State.nowbaseOpaqueness;
		return GameMath.Lerp(v, rainopaque, lerpRainCloudOverlay);
	}

	protected double pgetBlendedCloudBrightness(float b)
	{
		double v = GameMath.BiLerp(AdjacentSims[0].GetBlendedCloudBrightness(b), AdjacentSims[1].GetBlendedCloudBrightness(b), AdjacentSims[2].GetBlendedCloudBrightness(b), AdjacentSims[3].GetBlendedCloudBrightness(b), LerpLeftRight, LerpTopBot);
		double rainbright = rainOverlayData.State.nowCloudBrightness;
		return GameMath.Lerp(v, rainbright, lerpRainCloudOverlay);
	}

	protected double pgetBlendedThinCloudModeness()
	{
		return GameMath.BiLerp(AdjacentSims[0].GetBlendedThinCloudModeness(), AdjacentSims[1].GetBlendedThinCloudModeness(), AdjacentSims[2].GetBlendedThinCloudModeness(), AdjacentSims[3].GetBlendedThinCloudModeness(), LerpLeftRight, LerpTopBot);
	}

	protected double pgetBlendedUndulatingCloudModeness()
	{
		return GameMath.BiLerp(AdjacentSims[0].GetBlendedUndulatingCloudModeness(), AdjacentSims[1].GetBlendedUndulatingCloudModeness(), AdjacentSims[2].GetBlendedUndulatingCloudModeness(), AdjacentSims[3].GetBlendedUndulatingCloudModeness(), LerpLeftRight, LerpTopBot);
	}
}
