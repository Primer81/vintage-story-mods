using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class WeatherPatternState
{
	public bool BeginUseExecuted;

	public int Index;

	public float nowSceneBrightness = 1f;

	public float nowThicknessMul;

	public float nowbaseThickness;

	public float nowbaseOpaqueness;

	public float nowThinCloudModeness;

	public float nowUndulatingCloudModeness;

	public float nowCloudBrightness;

	public float nowHeightMul;

	public double ActiveUntilTotalHours;

	public float nowFogBrightness = 1f;

	public float nowFogDensity;

	public float nowMistDensity;

	public float nowMistYPos;

	public float nowNearLightningRate;

	public float nowDistantLightningRate;

	public float nowLightningMinTempature;

	public float nowPrecIntensity;

	public EnumPrecipitationType nowPrecType = EnumPrecipitationType.Auto;

	public float nowBasePrecIntensity;
}
