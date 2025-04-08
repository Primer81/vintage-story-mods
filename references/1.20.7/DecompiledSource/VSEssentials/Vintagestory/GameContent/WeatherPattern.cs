using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherPattern
{
	public WeatherPatternConfig config;

	public SimplexNoise LocationalCloudThicknessGen;

	public WeatherPatternState State = new WeatherPatternState();

	protected SimplexNoise TimeBasePrecIntenstityGen;

	private WeatherSystemBase ws;

	private ICoreAPI api;

	private int lastTileX;

	private int lastTileZ;

	public double[,] CloudDensityNoiseCache;

	private LCGRandom rand;

	public float hereChance;

	private int cloudTilebasePosX;

	private int cloudTilebasePosZ;

	public static int NoisePadding = 4;

	private int tilesPerRegion;

	public void updateHereChance(float rainfall, float temperature)
	{
		hereChance = config.getWeight(rainfall, temperature);
	}

	public WeatherPattern(WeatherSystemBase ws, WeatherPatternConfig config, LCGRandom rand, int cloudTilebasePosX, int cloudTilebasePosZ)
	{
		this.ws = ws;
		this.rand = rand;
		this.config = config;
		api = ws.api;
		this.cloudTilebasePosX = cloudTilebasePosX;
		this.cloudTilebasePosZ = cloudTilebasePosZ;
	}

	public virtual void Initialize(int index, int seed)
	{
		State.Index = index;
		if (config.Clouds?.LocationalThickness != null)
		{
			LocationalCloudThicknessGen = new SimplexNoise(config.Clouds.LocationalThickness.Amplitudes, config.Clouds.LocationalThickness.Frequencies, seed + index + 1512);
		}
	}

	public virtual void EnsureCloudTileCacheIsFresh(Vec3i tileOffset)
	{
		if (api.Side != EnumAppSide.Server && (CloudDensityNoiseCache == null || lastTileX != cloudTilebasePosX + tileOffset.X || lastTileZ != cloudTilebasePosZ + tileOffset.Z))
		{
			RegenCloudTileCache(tileOffset);
		}
	}

	public virtual void RegenCloudTileCache(Vec3i tileOffset)
	{
		tilesPerRegion = (int)Math.Ceiling((float)api.World.BlockAccessor.RegionSize / (float)ws.CloudTileSize) + 2 * NoisePadding;
		CloudDensityNoiseCache = new double[tilesPerRegion, tilesPerRegion];
		lastTileX = cloudTilebasePosX + tileOffset.X;
		lastTileZ = cloudTilebasePosZ + tileOffset.Z;
		double timeAxis = api.World.Calendar.TotalDays / 10.0;
		if (double.IsNaN(timeAxis))
		{
			throw new ArgumentException("timeAxis value in WeatherPattern.cs:RegenCloudTileCahce is NaN. Somethings broken.");
		}
		if (LocationalCloudThicknessGen == null)
		{
			for (int dx = 0; dx < tilesPerRegion; dx++)
			{
				for (int dz = 0; dz < tilesPerRegion; dz++)
				{
					CloudDensityNoiseCache[dx, dz] = 0.0;
				}
			}
			return;
		}
		for (int dx2 = 0; dx2 < tilesPerRegion; dx2++)
		{
			for (int dz2 = 0; dz2 < tilesPerRegion; dz2++)
			{
				double x = (double)(lastTileX + dx2 - tilesPerRegion / 2 - NoisePadding) / 20.0;
				double z = (double)(lastTileZ + dz2 - tilesPerRegion / 2 - NoisePadding) / 20.0;
				CloudDensityNoiseCache[dx2, dz2] = GameMath.Clamp(LocationalCloudThicknessGen.Noise(x, z, timeAxis), 0.0, 1.0);
			}
		}
	}

	public virtual void OnBeginUse()
	{
		State.BeginUseExecuted = true;
		State.ActiveUntilTotalHours = api.World.Calendar.TotalHours + (double)config.DurationHours.nextFloat(1f, rand);
		State.nowThinCloudModeness = (config.Clouds?.ThinCloudMode?.nextFloat(1f, rand)).GetValueOrDefault();
		State.nowUndulatingCloudModeness = (config.Clouds?.UndulatingCloudMode?.nextFloat(1f, rand)).GetValueOrDefault();
		State.nowbaseThickness = (config.Clouds?.BaseThickness?.nextFloat(1f, rand)).GetValueOrDefault();
		State.nowThicknessMul = (config.Clouds?.ThicknessMul?.nextFloat(1f, rand)).GetValueOrDefault(1f);
		State.nowbaseOpaqueness = (config.Clouds?.Opaqueness?.nextFloat(1f, rand)).GetValueOrDefault();
		State.nowCloudBrightness = config.Clouds?.Brightness.nextFloat(1f, rand) ?? 0f;
		State.nowHeightMul = (config.Clouds?.HeightMul?.nextFloat(1f, rand)).GetValueOrDefault();
		State.nowSceneBrightness = config.SceneBrightness.nextFloat(1f, rand);
		State.nowFogDensity = (config.Fog?.Density?.nextFloat(1f, rand)).GetValueOrDefault();
		State.nowMistDensity = (config.Fog?.MistDensity?.nextFloat(1f, rand)).GetValueOrDefault();
		State.nowMistYPos = (config.Fog?.MistYPos?.nextFloat(1f, rand)).GetValueOrDefault();
		State.nowFogBrightness = (config.Fog?.FogBrightness?.nextFloat(1f, rand)).GetValueOrDefault(1f);
		State.nowBasePrecIntensity = (config.Precipitation?.BaseIntensity?.nextFloat(1f, rand)).GetValueOrDefault();
	}

	public virtual void Update(float dt)
	{
	}

	public virtual double GetCloudDensityAt(int dx, int dz)
	{
		try
		{
			return GameMath.Clamp((double)State.nowbaseThickness + CloudDensityNoiseCache[GameMath.Clamp(dx + NoisePadding, 0, tilesPerRegion - 1), GameMath.Clamp(dz + NoisePadding, 0, tilesPerRegion - 1)], 0.0, 1.0) * (double)State.nowThicknessMul;
		}
		catch (Exception)
		{
			throw new Exception($"{dx}/{dz} is out of range. Width/Height: {CloudDensityNoiseCache.GetLength(0)}/{CloudDensityNoiseCache.GetLength(1)}");
		}
	}

	public virtual string GetWeatherName()
	{
		return config.Name;
	}
}
