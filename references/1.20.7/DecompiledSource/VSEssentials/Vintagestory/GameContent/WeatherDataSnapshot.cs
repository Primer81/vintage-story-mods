using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherDataSnapshot
{
	public AmbientModifier Ambient = new AmbientModifier().EnsurePopulated();

	public EnumPrecipitationType BlendedPrecType;

	public float PrecIntensity;

	public float PrecParticleSize;

	public EnumPrecipitationType nowPrecType;

	public float nearLightningRate;

	public float distantLightningRate;

	public float lightningMinTemp;

	public ClimateCondition climateCond = new ClimateCondition();

	public Vec3f curWindSpeed = new Vec3f();

	public float snowThresholdTemp = 4f;

	public void SetAmbientLerped(WeatherPattern left, WeatherPattern right, float w, float addFogDensity = 0f)
	{
		float fogMultiplier = GameMath.Clamp(1f - (float)Math.Pow(1.1 - (double)climateCond.WorldgenRainfall, 4.0), 0f, 1f);
		Ambient.FlatFogDensity.Set((right.State.nowMistDensity * w + left.State.nowMistDensity * (1f - w)) / 250f, fogMultiplier);
		Ambient.FlatFogYPos.Set(right.State.nowMistYPos * w + left.State.nowMistYPos * (1f - w));
		Ambient.FogDensity.Set((addFogDensity + right.State.nowFogDensity * w + left.State.nowFogDensity * (1f - w)) / 1000f, fogMultiplier);
		Ambient.CloudBrightness.Set(right.State.nowCloudBrightness * w + left.State.nowCloudBrightness * (1f - w));
		Ambient.CloudDensity.Set(right.State.nowbaseThickness * w + left.State.nowbaseThickness * (1f - w));
		Ambient.SceneBrightness.Set(right.State.nowSceneBrightness * w + left.State.nowSceneBrightness * (1f - w));
		Ambient.FogBrightness.Set(right.State.nowFogBrightness * w + left.State.nowFogBrightness * (1f - w));
	}

	public void SetAmbient(WeatherPattern left, float addFogDensity = 0f)
	{
		float fogMultiplier = GameMath.Clamp(1f - (float)Math.Pow(1.1 - (double)climateCond.WorldgenRainfall, 4.0), 0f, 1f);
		Ambient.FlatFogDensity.Set(left.State.nowMistDensity / 250f, fogMultiplier);
		Ambient.FlatFogYPos.Set(left.State.nowMistYPos);
		Ambient.FogDensity.Set((addFogDensity + left.State.nowFogDensity) / 1000f, fogMultiplier);
		Ambient.CloudBrightness.Set(left.State.nowCloudBrightness);
		Ambient.CloudDensity.Set(left.State.nowbaseThickness);
		Ambient.SceneBrightness.Set(left.State.nowSceneBrightness);
		Ambient.FogBrightness.Set(left.State.nowFogBrightness);
	}

	public void SetLerped(WeatherDataSnapshot left, WeatherDataSnapshot right, float w)
	{
		Ambient.SetLerped(left.Ambient, right.Ambient, w);
		PrecIntensity = left.PrecIntensity * (1f - w) + right.PrecIntensity * w;
		PrecParticleSize = left.PrecParticleSize * (1f - w) + right.PrecParticleSize * w;
		nowPrecType = (((double)w < 0.5) ? left.nowPrecType : right.nowPrecType);
		BlendedPrecType = (((double)w < 0.5) ? left.BlendedPrecType : right.BlendedPrecType);
		nearLightningRate = left.nearLightningRate * (1f - w) + right.nearLightningRate * w;
		distantLightningRate = left.distantLightningRate * (1f - w) + right.distantLightningRate * w;
		lightningMinTemp = left.lightningMinTemp * (1f - w) + right.lightningMinTemp * w;
		climateCond.SetLerped(left.climateCond, right.climateCond, w);
		curWindSpeed.X = left.curWindSpeed.X * (1f - w) + right.curWindSpeed.X * w;
		snowThresholdTemp = left.snowThresholdTemp * (1f - w) + right.snowThresholdTemp * w;
	}

	public void SetLerpedPrec(WeatherDataSnapshot left, WeatherDataSnapshot right, float w)
	{
		Ambient.SetLerped(left.Ambient, right.Ambient, w);
		PrecIntensity = left.PrecIntensity * (1f - w) + right.PrecIntensity * w;
		PrecParticleSize = left.PrecParticleSize * (1f - w) + right.PrecParticleSize * w;
		nowPrecType = left.nowPrecType;
		BlendedPrecType = left.BlendedPrecType;
		nearLightningRate = left.nearLightningRate;
		distantLightningRate = left.distantLightningRate;
		lightningMinTemp = left.lightningMinTemp;
		climateCond = left.climateCond;
		curWindSpeed.X = left.curWindSpeed.X;
		snowThresholdTemp = left.snowThresholdTemp;
	}

	public void BiLerp(WeatherDataSnapshot topLeft, WeatherDataSnapshot topRight, WeatherDataSnapshot botLeft, WeatherDataSnapshot botRight, float lerpleftRight, float lerptopBot)
	{
		WeatherDataSnapshot top = new WeatherDataSnapshot();
		top.SetLerped(topLeft, topRight, lerpleftRight);
		WeatherDataSnapshot bot = new WeatherDataSnapshot();
		bot.SetLerped(botLeft, botRight, lerptopBot);
		SetLerped(top, bot, lerptopBot);
	}
}
