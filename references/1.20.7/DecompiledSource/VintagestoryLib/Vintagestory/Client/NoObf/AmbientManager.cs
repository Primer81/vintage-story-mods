using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class AmbientManager : IAmbientManager
{
	private OrderedDictionary<string, AmbientModifier> ambientModifiers;

	private ClientMain game;

	internal float DropShadowIntensity;

	public int ShadowQuality;

	internal AmbientModifier BaseModifier = AmbientModifier.DefaultAmbient;

	internal AmbientModifier Sunglow;

	private float smoothedLightLevel = -1f;

	private float targetExtraContrastLevel;

	private float targetSepiaLevel;

	public Vec4f BlendedFogColor { get; set; }

	public Vec3f BlendedAmbientColor { get; set; }

	public float BlendedFogDensity { get; set; }

	public float BlendedFogMin { get; set; }

	public float BlendedFlatFogDensity { get; set; }

	public float BlendedFlatFogYOffset { get; set; }

	public float BlendedCloudBrightness { get; set; }

	public float BlendedCloudDensity { get; set; }

	public float BlendedCloudYPos { get; set; }

	public float BlendedFlatFogYPosForShader { get; set; }

	public float BlendedSceneBrightness { get; set; }

	public float BlendedFogBrightness { get; set; }

	public OrderedDictionary<string, AmbientModifier> CurrentModifiers => ambientModifiers;

	public float ViewDistance => ClientSettings.ViewDistance;

	public AmbientModifier Base => BaseModifier;

	public AmbientManager(ClientMain game)
	{
		BlendedFogColor = new Vec4f(1f, 1f, 1f, 1f);
		BlendedAmbientColor = new Vec3f();
		this.game = game;
		game.eventManager.RegisterRenderer(UpdateAmbient, EnumRenderStage.Before, "ambientmanager", 0.0);
		ambientModifiers = new OrderedDictionary<string, AmbientModifier>();
		ambientModifiers["sunglow"] = (Sunglow = new AmbientModifier
		{
			FogColor = WeightedFloatArray.New(new float[3] { 0.8f, 0.8f, 0.8f }, 0f),
			AmbientColor = WeightedFloatArray.New(new float[3] { 1f, 1f, 1f }, 0.9f),
			FogDensity = WeightedFloat.New(0f, 0f)
		}.EnsurePopulated());
		ambientModifiers["serverambient"] = new AmbientModifier().EnsurePopulated();
		ambientModifiers["night"] = new AmbientModifier().EnsurePopulated();
		ambientModifiers["water"] = new AmbientModifier
		{
			FogColor = WeightedFloatArray.New(new float[3] { 0.18f, 0.74f, 1f }, 0f),
			FogDensity = WeightedFloat.New(0.05f, 0f),
			FogMin = WeightedFloat.New(0.15f, 0f),
			AmbientColor = WeightedFloatArray.New(new float[3] { 0.18f, 0.74f, 1f }, 0f)
		}.EnsurePopulated();
		ambientModifiers["lava"] = new AmbientModifier
		{
			FogColor = WeightedFloatArray.New(new float[3]
			{
				1f,
				47f / 51f,
				27f / 85f
			}, 0f),
			FogDensity = WeightedFloat.New(0.3f, 0f),
			FogMin = WeightedFloat.New(0.5f, 0f),
			AmbientColor = WeightedFloatArray.New(new float[3]
			{
				1f,
				47f / 51f,
				27f / 85f
			}, 0f)
		}.EnsurePopulated();
		ambientModifiers["deepwater"] = new AmbientModifier
		{
			FogColor = WeightedFloatArray.New(new float[3] { 0f, 0f, 0.07f }, 0f),
			FogMin = WeightedFloat.New(0.1f, 0f),
			FogDensity = WeightedFloat.New(0.1f, 0f),
			AmbientColor = WeightedFloatArray.New(new float[3] { 0f, 0f, 0.07f }, 0f)
		}.EnsurePopulated();
		ambientModifiers["blackfogincaves"] = new AmbientModifier
		{
			FogColor = WeightedFloatArray.New(new float[3], 0f)
		}.EnsurePopulated();
		ClientSettings.Inst.AddWatcher<int>("viewDistance", OnViewDistanceChanged);
		ShadowQuality = ClientSettings.ShadowMapQuality;
		ClientSettings.Inst.AddWatcher<int>("shadowMapQuality", delegate
		{
			ShadowQuality = ClientSettings.ShadowMapQuality;
		});
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.DayLight, OnDayLightChanged);
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInWaterColorShift, OnPlayerSightBeingChangedByWater);
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInLavaColorShift, OnPlayerSightBeingChangedByLava);
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInWaterDepth, OnPlayerUnderWater);
	}

	public void LateInit()
	{
		game.api.eventapi.PlayerDimensionChanged += Eventapi_PlayerDimensionChanged;
	}

	private void Eventapi_PlayerDimensionChanged(IPlayer byPlayer)
	{
		UpdateAmbient(0f);
	}

	private void OnPlayerUnderWater(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		float depthFactor = GameMath.Clamp(newValues.EyesInWaterDepth / 70f, 0f, 1f);
		AmbientModifier ambientModifier = ambientModifiers["deepwater"];
		ambientModifier.FogColor.Weight = 0.95f * depthFactor;
		ambientModifier.AmbientColor.Weight = 0.85f * depthFactor;
	}

	private void UpdateDaylight(float dt)
	{
		if (smoothedLightLevel < 0f)
		{
			smoothedLightLevel = game.BlockAccessor.GetLightLevel(game.Player.Entity.Pos.AsBlockPos, EnumLightLevelType.OnlySunLight);
		}
		AmbientModifier ambientModifier = ambientModifiers["night"];
		float t = Math.Min(0.6f, 0f - game.Calendar.SunPositionNormalized.Y) - 0.75f * Math.Min(0.33f, game.Calendar.MoonLightStrength);
		ambientModifier.FogBrightness.Weight = GameMath.Clamp(1f - game.Calendar.DayLightStrength + GameMath.Clamp(t, 0f, 0.5f) * 0.85f, 0f, 0.88f);
		float p = GameMath.Clamp(1.5f * game.Calendar.DayLightStrength - 0.2f, 0.1f, 1f);
		ambientModifier.SceneBrightness.Weight = GameMath.Clamp(1f - p, 0f, 0.65f);
		BlockPos plrPos = game.player.Entity.Pos.AsBlockPos;
		int lightlevel = Math.Max(game.BlockAccessor.GetLightLevel(plrPos, EnumLightLevelType.OnlySunLight), game.BlockAccessor.GetLightLevel(plrPos.Up(), EnumLightLevelType.OnlySunLight));
		smoothedLightLevel += ((float)lightlevel - smoothedLightLevel) * dt;
		float fogMultiplier = GameMath.Clamp(3f * smoothedLightLevel / 20f, 0f, 1f);
		float fac = (float)GameMath.Clamp(game.Player.Entity.Pos.Y / (double)game.SeaLevel, 0.0, 1.0);
		fac *= fac;
		fogMultiplier *= fac;
		ambientModifiers["blackfogincaves"].FogColor.Weight = GameMath.Clamp(1f - fogMultiplier, 0f, 1f);
	}

	private void OnDayLightChanged(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		AmbientModifier ambientModifier = ambientModifiers["night"];
		ambientModifier.FogBrightness.Value = 0f;
		ambientModifier.SceneBrightness.Value = 0f;
		OnPlayerSightBeingChangedByWater(oldValues, newValues);
	}

	private void OnPlayerSightBeingChangedByWater(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		AmbientModifier ambientModifier = ambientModifiers["water"];
		ambientModifier.FogColor.Weight = (float)(newValues.EyesInWaterColorShift * newValues.EyesInWaterColorShift) / 10000f;
		ambientModifier.AmbientColor.Weight = 0.75f * (float)newValues.EyesInWaterColorShift / 100f;
		ambientModifier.FogDensity.Weight = (float)newValues.EyesInWaterColorShift / 100f;
		ambientModifier.FogMin.Weight = (float)newValues.EyesInWaterColorShift / 100f;
		game.api.Render.ShaderUniforms.CameraUnderwater = (float)newValues.EyesInWaterColorShift / 100f;
		setWaterColors();
	}

	private void setWaterColors()
	{
		AmbientModifier ambientModifier = ambientModifiers["water"];
		float daylight = Math.Max(0.2f, game.Calendar.DayLightStrength);
		int waterTint = game.WorldMap.ApplyColorMapOnRgba("climateWaterTint", null, -1, (int)game.EntityPlayer.Pos.X, (int)game.EntityPlayer.Pos.Y, (int)game.EntityPlayer.Pos.Z, flipRb: false);
		int[] hsv = ColorUtil.RgbToHsvInts(waterTint & 0xFF, (waterTint >> 8) & 0xFF, (waterTint >> 16) & 0xFF);
		hsv[2] /= 2;
		hsv[2] = (int)((float)hsv[2] * daylight);
		int[] rgbInt = ColorUtil.Hsv2RgbInts(hsv[0], hsv[1], hsv[2]);
		ambientModifier.FogColor.Value[0] = (float)rgbInt[0] / 255f;
		ambientModifier.FogColor.Value[1] = (float)rgbInt[1] / 255f;
		ambientModifier.FogColor.Value[2] = (float)rgbInt[2] / 255f;
		ambientModifier.AmbientColor = ambientModifier.FogColor;
		hsv[1] /= 2;
		rgbInt = ColorUtil.Hsv2RgbInts(hsv[0], hsv[1], hsv[2]);
		game.api.Render.ShaderUniforms.WaterMurkColor = new Vec4f((float)rgbInt[0] / 255f, (float)rgbInt[1] / 255f, (float)rgbInt[2] / 255f, 1f);
	}

	private void OnPlayerSightBeingChangedByLava(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		AmbientModifier ambientModifier = ambientModifiers["lava"];
		ambientModifier.FogColor.Weight = (float)(newValues.EyesInLavaColorShift * newValues.EyesInLavaColorShift) / 10000f;
		ambientModifier.AmbientColor.Weight = 0.5f * (float)newValues.EyesInLavaColorShift / 100f;
		ambientModifier.FogDensity.Weight = (float)newValues.EyesInLavaColorShift / 100f;
		ambientModifier.FogMin.Weight = (float)newValues.EyesInLavaColorShift / 100f;
	}

	private void OnViewDistanceChanged(int newValue)
	{
	}

	public void SetFogRange(float density, float min)
	{
		BaseModifier.FogDensity.Value = density;
		BaseModifier.FogMin.Value = min;
	}

	public void UpdateAmbient(float dt)
	{
		setWaterColors();
		updateColorGradingValues(dt);
		float[] mixedFogColor = new float[4]
		{
			BaseModifier.FogColor.Value[0],
			BaseModifier.FogColor.Value[1],
			BaseModifier.FogColor.Value[2],
			1f
		};
		float[] mixedAmbientColor = new float[3]
		{
			BaseModifier.AmbientColor.Value[0],
			BaseModifier.AmbientColor.Value[1],
			BaseModifier.AmbientColor.Value[2]
		};
		BlendedFogDensity = BaseModifier.FogDensity.Value;
		BlendedFogMin = BaseModifier.FogMin.Value;
		BlendedFlatFogDensity = BaseModifier.FlatFogDensity.Value;
		BlendedFlatFogYOffset = BaseModifier.FlatFogYPos.Value;
		BlendedCloudBrightness = BaseModifier.CloudBrightness.Value;
		BlendedCloudDensity = BaseModifier.CloudDensity.Value;
		BlendedSceneBrightness = BaseModifier.SceneBrightness.Value;
		BlendedFogBrightness = BaseModifier.FogBrightness.Value;
		UpdateDaylight(dt);
		float w = 0f;
		foreach (KeyValuePair<string, AmbientModifier> ambientModifier in ambientModifiers)
		{
			AmbientModifier modifier = ambientModifier.Value;
			w = modifier.FogColor.Weight;
			mixedFogColor[0] = w * modifier.FogColor.Value[0] + (1f - w) * mixedFogColor[0];
			mixedFogColor[1] = w * modifier.FogColor.Value[1] + (1f - w) * mixedFogColor[1];
			mixedFogColor[2] = w * modifier.FogColor.Value[2] + (1f - w) * mixedFogColor[2];
			w = modifier.AmbientColor.Weight;
			mixedAmbientColor[0] = w * modifier.AmbientColor.Value[0] + (1f - w) * mixedAmbientColor[0];
			mixedAmbientColor[1] = w * modifier.AmbientColor.Value[1] + (1f - w) * mixedAmbientColor[1];
			mixedAmbientColor[2] = w * modifier.AmbientColor.Value[2] + (1f - w) * mixedAmbientColor[2];
			w = modifier.FogDensity.Weight;
			BlendedFogDensity = w * w * modifier.FogDensity.Value + (1f - w) * (1f - w) * BlendedFogDensity;
			w = modifier.FlatFogDensity.Weight;
			BlendedFlatFogDensity = w * modifier.FlatFogDensity.Value + (1f - w) * BlendedFlatFogDensity;
			w = modifier.FogMin.Weight;
			BlendedFogMin = w * modifier.FogMin.Value + (1f - w) * BlendedFogMin;
			w = modifier.FlatFogYPos.Weight;
			BlendedFlatFogYOffset = w * modifier.FlatFogYPos.Value + (1f - w) * BlendedFlatFogYOffset;
			w = modifier.CloudBrightness.Weight;
			BlendedCloudBrightness = w * modifier.CloudBrightness.Value + (1f - w) * BlendedCloudBrightness;
			w = modifier.CloudDensity.Weight;
			BlendedCloudDensity = w * modifier.CloudDensity.Value + (1f - w) * BlendedCloudDensity;
			w = modifier.SceneBrightness.Weight;
			BlendedSceneBrightness = w * modifier.SceneBrightness.Value + (1f - w) * BlendedSceneBrightness;
			w = modifier.FogBrightness.Weight;
			BlendedFogBrightness = w * modifier.FogBrightness.Value + (1f - w) * BlendedFogBrightness;
		}
		mixedFogColor[0] *= BlendedSceneBrightness * BlendedFogBrightness;
		mixedFogColor[1] *= BlendedSceneBrightness * BlendedFogBrightness;
		mixedFogColor[2] *= BlendedSceneBrightness * BlendedFogBrightness;
		BlendedFogColor.Set(mixedFogColor);
		mixedAmbientColor[0] *= BlendedSceneBrightness;
		mixedAmbientColor[1] *= BlendedSceneBrightness;
		mixedAmbientColor[2] *= BlendedSceneBrightness;
		BlendedAmbientColor.Set(mixedAmbientColor);
		BlendedFlatFogYPosForShader = BlendedFlatFogYOffset + (float)game.SeaLevel;
		double playerHeightFactor = Math.Max(0.0, (game.Player.Entity.Pos.Y - (double)game.SeaLevel - 5000.0) / 10000.0);
		BlendedFogMin = Math.Max(0f, BlendedFogMin - (float)playerHeightFactor);
		BlendedFogDensity = Math.Max(0f, BlendedFogDensity - (float)playerHeightFactor);
		if (float.IsNaN(BlendedFlatFogDensity))
		{
			BlendedFlatFogDensity = 0f;
		}
		else
		{
			BlendedFlatFogDensity = (float)((double)Math.Sign(BlendedFlatFogDensity) * Math.Max(0.0, (double)Math.Abs(BlendedFlatFogDensity) - playerHeightFactor));
		}
	}

	private void updateColorGradingValues(float dt)
	{
		if (!ClientSettings.DynamicColorGrading)
		{
			return;
		}
		dt = Math.Min(0.2f, dt);
		BlockPos plrPos = game.player.Entity.Pos.XYZ.AsBlockPos;
		plrPos.Y = game.SeaLevel;
		ClimateCondition nowConds = game.World.BlockAccessor.GetClimateAt(plrPos);
		if (nowConds != null)
		{
			if (float.IsNaN(nowConds.Temperature) || float.IsNaN(nowConds.WorldgenRainfall))
			{
				game.Logger.Warning("Color grading: Temperature/Rainfall at {0} is {1}/{2}. Will ignore.", nowConds.Temperature, nowConds.WorldgenRainfall);
				return;
			}
			float contrastSub = game.api.renderapi.ShaderUniforms.GlitchStrength;
			targetExtraContrastLevel = GameMath.Clamp((nowConds.Temperature + 5f) / 30f - contrastSub, 0f, 0.5f);
			targetSepiaLevel = 0.2f + GameMath.Clamp((nowConds.Temperature + 5f) / 35f, 0f, 1f) * 0.2f;
			ClientSettings.ExtraContrastLevel += (targetExtraContrastLevel - ClientSettings.ExtraContrastLevel) * dt;
			ClientSettings.SepiaLevel += (targetSepiaLevel - ClientSettings.SepiaLevel) * dt;
			float extraBloom = Math.Max(0f, (nowConds.Temperature - 30f) / 20f) * Math.Max(0f, nowConds.WorldgenRainfall - 0.5f);
			game.api.Render.ShaderUniforms.AmbientBloomLevelAdd[0] = GameMath.Clamp(extraBloom, 0f, 2f);
		}
	}
}
