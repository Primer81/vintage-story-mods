using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherDataReaderPreLoad : WeatherDataReaderBase
{
	public WeatherDataReaderPreLoad(ICoreAPI api, WeatherSystemBase ws)
		: base(api, ws)
	{
	}

	public void LoadAdjacentSimsAndLerpValues(Vec3d pos, float dt)
	{
		LoadAdjacentSimsAndLerpValues(pos, useArgValues: false, 0f, 0f, dt);
	}

	public void LoadLerp(Vec3d pos)
	{
		LoadLerp(pos, useArgValues: false);
	}

	public void UpdateAdjacentAndBlendWeatherData()
	{
		updateAdjacentAndBlendWeatherData();
	}

	public void EnsureCloudTileCacheIsFresh(Vec3i tilePos)
	{
		ensureCloudTileCacheIsFresh(tilePos);
	}

	public double GetWindSpeed(double posY)
	{
		return pgetWindSpeed(posY);
	}

	public double GetBlendedCloudThicknessAt(int cloudTileX, int cloudTileZ)
	{
		return pgetBlendedCloudThicknessAt(cloudTileX, cloudTileZ);
	}

	public double GetBlendedCloudOpaqueness()
	{
		return pgetBlendedCloudOpaqueness();
	}

	public double GetBlendedCloudBrightness(float b)
	{
		return pgetBlendedCloudBrightness(b);
	}

	public double GetBlendedThinCloudModeness()
	{
		return pgetBlendedThinCloudModeness();
	}

	public double GetBlendedUndulatingCloudModeness()
	{
		return pgetBlendedUndulatingCloudModeness();
	}
}
