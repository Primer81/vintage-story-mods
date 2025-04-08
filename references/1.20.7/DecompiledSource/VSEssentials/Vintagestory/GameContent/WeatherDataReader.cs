using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherDataReader : WeatherDataReaderBase
{
	public WeatherDataReader(ICoreAPI api, WeatherSystemBase ws)
		: base(api, ws)
	{
	}

	public double GetBlendedCloudBrightness(Vec3d pos, float bMul = 1f)
	{
		LoadAdjacentSimsAndLerpValues(pos, useArgValues: false);
		return pgetBlendedCloudBrightness(bMul);
	}

	public double GetBlendedCloudOpaqueness(Vec3d pos)
	{
		LoadAdjacentSimsAndLerpValues(pos, useArgValues: false);
		return pgetBlendedCloudOpaqueness();
	}

	public double GetBlendedCloudThicknessAt(Vec3d pos, int cloudTileX, int cloudTileZ)
	{
		LoadAdjacentSimsAndLerpValues(pos, useArgValues: false);
		return pgetBlendedCloudThicknessAt(cloudTileX, cloudTileZ);
	}

	public double GetBlendedThinCloudModeness(Vec3d pos)
	{
		LoadAdjacentSimsAndLerpValues(pos, useArgValues: false);
		return pgetBlendedThinCloudModeness();
	}

	public double GetBlendedUndulatingCloudModeness(Vec3d pos)
	{
		LoadAdjacentSimsAndLerpValues(pos, useArgValues: false);
		return pgetBlendedUndulatingCloudModeness();
	}

	public double GetWindSpeed(Vec3d pos)
	{
		LoadAdjacentSimsAndLerpValues(pos, useArgValues: false);
		return pgetWindSpeed(pos.Y);
	}

	public EnumPrecipitationType GetPrecType(Vec3d pos)
	{
		LoadAdjacentSimsAndLerpValues(pos, useArgValues: false);
		return pgGetPrecType();
	}
}
