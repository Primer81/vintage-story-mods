using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class DefaultShaderUniforms
{
	/// <summary>
	/// The currently configured z-near plane
	/// </summary>
	public float ZNear;

	/// <summary>
	/// The currently configured z-far plane
	/// </summary>
	public float ZFar;

	public float DropShadowIntensity;

	public float ShadowRangeNear;

	public float ShadowRangeFar;

	public float ShadowZExtendNear;

	public float ShadowZExtendFar;

	public float NightVisionStrength;

	public float[] ToShadowMapSpaceMatrixFar = new float[16];

	public float[] ToShadowMapSpaceMatrixNear = new float[16];

	public int PointLightsCount;

	public float[] PointLights3;

	public float[] PointLightColors3;

	public const int MaxSpheres = 3;

	/// <summary>
	/// Each sphere has 8 floats:
	/// 3 floats x/y/z offset to the player
	/// 1 float radius
	/// 1 float density
	/// 3 floats rgb color
	/// </summary>
	public float[] FogSpheres = new float[24];

	public int FogSphereQuantity;

	public Vec3f SunPositionScreen;

	public Vec3f SunPosition3D;

	public Vec3f LightPosition3D;

	public Vec3f PlayerViewVector;

	public float DamageVignetting;

	/// <summary>
	/// 0..1 (0 for left, 0.5 for left&amp;right, 1 for right)
	/// </summary>
	public float DamageVignettingSide;

	public float FrostVignetting;

	public float ExtraSepia;

	public const int BloomAddDrunkIndex = 1;

	public const int BloomAddEnvIndex = 0;

	public float[] AmbientBloomLevelAdd = new float[4];

	public float ExtraBloom;

	public float FlagFogDensity;

	public float FlatFogStartYPos;

	public int Dusk;

	public float TimeCounter;

	public float WaterStillCounter;

	public float WaterFlowCounter;

	public float WaterWaveCounter;

	public float WindWaveCounter;

	public float WindWaveCounterHighFreq;

	public float GlitchStrength;

	public float GlitchWaviness;

	public float WindSpeed;

	public float WindWaveIntensity = 1f;

	public float WaterWaveIntensity = 1f;

	public float FogWaveCounter;

	public float GlobalWorldWarp;

	public float SeaLevel;

	public float SunsetMod;

	public int SunLightTextureId;

	public int GlowTextureId;

	public int SkyTextureId;

	public int DitherSeed;

	public int FrameWidth;

	public float PlayerToSealevelOffset;

	public float[] ColorMapRects4;

	public float SeasonRel;

	public float BlockAtlasHeight;

	public float SeasonTemperature;

	public float SunSpecularIntensity = 1f;

	public float SunlightExtraBrightness;

	public int PerceptionEffectId = 1;

	public float PerceptionEffectIntensity = 1f;

	public Vec3f PlayerPos = new Vec3f();

	public Vec3f PlayerPosForFoam = new Vec3f();

	public Vec3d playerReferencePos;

	public Vec3d playerReferencePosForFoam;

	private BlockPos plrPos = new BlockPos();

	internal float SkyDaylight;

	public Vec4f WaterMurkColor { get; set; } = new Vec4f();


	public float CameraUnderwater { get; set; }

	public DefaultShaderUniforms()
	{
		SunPositionScreen = new Vec3f();
		SunPosition3D = new Vec3f();
		LightPosition3D = new Vec3f();
		PlayerViewVector = new Vec3f();
		DamageVignetting = 0f;
		PointLightsCount = 0;
		PointLights3 = new float[300];
		PointLightColors3 = new float[300];
	}

	public static int DescaleTemperature(float temperature)
	{
		return (int)((temperature + 20f) * 4.25f);
	}

	public void Update(float dt, ICoreClientAPI capi)
	{
		IGameCalendar calendar = capi.World.Calendar;
		dt *= calendar.SpeedOfTime / 60f;
		if (capi.IsGamePaused)
		{
			dt = 0f;
		}
		WaterStillCounter = (WaterStillCounter + dt / 1.5f) % 2f;
		WaterFlowCounter = (WaterFlowCounter + dt / 1.5f) % 6000f;
		WaterWaveCounter = (WaterWaveCounter + dt * 0.75f) % 6000f;
		WindWaveCounter = (WindWaveCounter + (0.5f + 2f * GlobalConstants.CurrentSurfaceWindSpeedClient.X * (1f - GlitchStrength)) * dt) % 6000f;
		WindSpeed = GlobalConstants.CurrentSurfaceWindSpeedClient.X;
		WaterWaveIntensity = 0.75f + GlobalConstants.CurrentSurfaceWindSpeedClient.X * 0.9f;
		float freq = 0.4f + WindSpeed / 10f;
		WindWaveCounterHighFreq = (WindWaveCounterHighFreq + freq * (0.5f + 5f * GlobalConstants.CurrentSurfaceWindSpeedClient.X * (1f - GlitchStrength)) * dt) % 6000f;
		TimeCounter += dt;
		FogWaveCounter += 0.1f * dt;
		plrPos.Set(capi.World.Player.Entity.Pos.XInt, capi.World.Player.Entity.Pos.YInt, capi.World.Player.Entity.Pos.ZInt);
		PlayerViewVector = EntityPos.GetViewVector(capi.Input.MousePitch, capi.Input.MouseYaw);
		Dusk = (capi.World.Calendar.Dusk ? 1 : 0);
		PlayerToSealevelOffset = (float)capi.World.Player.Entity.Pos.Y - (float)capi.World.SeaLevel;
		SeaLevel = capi.World.SeaLevel;
		FrameWidth = capi.Render.FrameWidth;
		BlockAtlasHeight = capi.BlockTextureAtlas.Size.Height;
		int y = plrPos.Y;
		plrPos.Y = capi.World.SeaLevel;
		ClimateCondition nowConds = capi.World.BlockAccessor.GetClimateAt(plrPos);
		plrPos.Y = y;
		SeasonTemperature = (float)(DescaleTemperature(nowConds.Temperature) - DescaleTemperature(nowConds.WorldGenTemperature)) / 255f;
		SeasonRel = capi.World.Calendar.GetSeasonRel(plrPos);
	}
}
