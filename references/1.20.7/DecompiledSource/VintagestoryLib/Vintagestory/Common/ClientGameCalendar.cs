using System;
using System.Drawing;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class ClientGameCalendar : GameCalendar, IClientGameCalendar, IGameCalendar
{
	public const long ClientCalendarStartingSeconds = 28000L;

	public Vec3f SunPosition = new Vec3f();

	public Vec3f MoonPosition = new Vec3f();

	public Vec3f SunPositionNormalized = new Vec3f();

	internal float dayLight;

	private Vec3f sunColor = new Vec3f();

	private Vec3f reflectColor = new Vec3f();

	private IClientWorldAccessor cworld;

	public static Vec3f nightColor = new Vec3f(0f, 0.0627451f, 2f / 15f);

	private double transitionDaysLast;

	public float DayLightStrength
	{
		get
		{
			return dayLight;
		}
		set
		{
			dayLight = value;
		}
	}

	public float MoonLightStrength { get; set; }

	public float SunLightStrength { get; set; }

	public Vec3f SunColor => sunColor;

	public Vec3f ReflectColor => reflectColor;

	public Color SunLightColor
	{
		get
		{
			float sunIntensity = (GameMath.Clamp(SunPositionNormalized.Y * 1.5f, -1f, 1f) + 1f) / 2f;
			return getSunlightPixelRel(sunIntensity, 0.01f);
		}
	}

	Vec3f IClientGameCalendar.SunPositionNormalized => SunPositionNormalized;

	Vec3f IClientGameCalendar.SunPosition => SunPosition;

	Vec3f IClientGameCalendar.MoonPosition => MoonPosition;

	internal ClientGameCalendar(IClientWorldAccessor cworld, IAsset sunlightTexture, int worldSeed, long totalSecondsStart = 28000L)
		: base(sunlightTexture, worldSeed, totalSecondsStart)
	{
		this.cworld = cworld;
	}

	public override void Update()
	{
		base.Update();
		Vec3d plrpos = cworld.Player.Entity.Pos.XYZ;
		SunPositionNormalized.Set(GetSunPosition(plrpos, base.TotalDays));
		SunPosition.Set(SunPositionNormalized).Mul(50f);
		MoonPosition.Set(GetMoonPosition(plrpos, base.TotalDays)).Mul(50f);
		float sunIntensity = (GameMath.Clamp(SunPositionNormalized.Y * 1.4f + 0.2f, -1f, 1f) + 1f) / 2f;
		float moonYRel = MoonPosition.NormalizedCopy().Y;
		float bright = GameMath.Clamp(base.MoonPhaseBrightness * (0.66f + 0.33f * moonYRel), -0.2f, base.MoonPhaseBrightness);
		float sunExtraIntensity = Math.Max(0f, (SunPositionNormalized.Y - 0.4f) / 7.5f);
		MoonLightStrength = GameMath.Lerp(0f, bright, GameMath.Clamp(moonYRel * 20f, 0f, 1f));
		SunLightStrength = (sunColor.R + sunColor.G + sunColor.B) / 3f + sunExtraIntensity;
		DayLightStrength = Math.Max(MoonLightStrength, SunLightStrength);
		float eclipseDarkening = GameMath.Clamp((SunPositionNormalized.Dot(MoonPosition.NormalizedCopy()) - 0.9996f) * 3000f, 0f, DayLightStrength * 0.6f);
		DayLightStrength = Math.Max(0f, DayLightStrength - eclipseDarkening);
		DayLightStrength = Math.Min(1.5f, DayLightStrength + (float)Math.Max(0.0, (plrpos.Y - (double)cworld.SeaLevel - 1000.0) / 30000.0));
		double transitionDays = base.TotalDays + 1.0 / 48.0;
		float targetSunsetMod = GameMath.Clamp(((float)sunsetModNoise.Noise(0.0, (int)transitionDays) - 0.65f) / 1.8f, -0.1f, 0.3f);
		float dt = GameMath.Clamp((float)((transitionDays - transitionDaysLast) * 6.0), 4.1666666E-05f, 1f);
		transitionDaysLast = transitionDays;
		base.SunsetMod += (targetSunsetMod - base.SunsetMod) * dt;
		Color colSun = getSunlightPixelRel(GameMath.Clamp(sunIntensity + base.SunsetMod, 0f, 1f), 0.01f);
		sunColor.Set((float)(int)colSun.R / 255f, (float)(int)colSun.G / 255f, (float)(int)colSun.B / 255f);
		Color colRefle = getSunlightPixelRel(GameMath.Clamp(sunIntensity - base.SunsetMod, 0f, 1f), 0.01f);
		reflectColor.Set((float)(int)colRefle.R / 255f, (float)(int)colRefle.G / 255f, (float)(int)colRefle.B / 255f);
		if (SunPosition.Y < -0.1f)
		{
			float darkness = (0f - SunPosition.Y) / 10f - 0.3f;
			reflectColor.R = Math.Max(reflectColor.R - darkness, nightColor[0]);
			reflectColor.G = Math.Max(reflectColor.G - darkness, nightColor[1]);
			reflectColor.B = Math.Max(reflectColor.B - darkness, nightColor[2]);
		}
	}
}
