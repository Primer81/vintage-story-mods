using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherPatternConfig : ConditionalPatternConfig
{
	public string Code;

	public string Name;

	public NatFloat DurationHours = NatFloat.createUniform(7.5f, 4.5f);

	public NatFloat SceneBrightness = NatFloat.createUniform(1f, 0f);

	public WeatherPrecipitationConfig Precipitation;

	public WeatherCloudConfig Clouds;

	public WeatherFogConfig Fog;
}
