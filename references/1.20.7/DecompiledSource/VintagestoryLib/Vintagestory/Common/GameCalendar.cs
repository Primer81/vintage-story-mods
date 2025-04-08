using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class GameCalendar : IGameCalendar
{
	protected float currentSpeedOfTime = 60f;

	public float HoursPerDay = 24f;

	public int MoonOrbitDays = 8;

	public float DayLengthInRealLifeSeconds;

	public long LastIngameSecond;

	internal Stopwatch watchIngameTime;

	protected TimeSpan timespan = TimeSpan.Zero;

	protected long totalSecondsStart;

	public Size2i sunLightTextureSize;

	public int[] sunLightTexture;

	protected Dictionary<string, float> timeSpeedModifiers = new Dictionary<string, float>();

	protected NormalizedSimplexNoise sunsetModNoise;

	protected NormalizedSimplexNoise superMoonNoise;

	protected float superMoonSize;

	protected float timelapse;

	private float calendarSpeedMul = 0.5f;

	public static float[] MoonBrightnesByPhase = new float[8] { -0.1f, 0.1f, 0.2f, 0.26f, 0.33f, 0.26f, 0.2f, 0.1f };

	public static float[] MoonSizeByPhase = new float[8] { 0.8f, 0.9f, 1f, 1.1f, 1.2f, 1.1f, 1f, 0.9f };

	public Dictionary<string, float> TimeSpeedModifiers
	{
		get
		{
			return timeSpeedModifiers;
		}
		set
		{
			timeSpeedModifiers = value;
			CalculateCurrentTimeSpeed();
		}
	}

	public float SpeedOfTime => currentSpeedOfTime;

	public float CalendarSpeedMul
	{
		get
		{
			return calendarSpeedMul;
		}
		set
		{
			calendarSpeedMul = value;
			CalculateCurrentTimeSpeed();
		}
	}

	public float Timelapse
	{
		get
		{
			return timelapse;
		}
		set
		{
			timelapse = value;
		}
	}

	public int DaysPerMonth { get; set; } = 9;


	public int DaysPerYear => DaysPerMonth * 12;

	public int DayOfMonth => (int)(TotalDays % (double)DaysPerMonth) + 1;

	public int MonthsPerYear => DaysPerYear / DaysPerMonth;

	public int FullHourOfDay => (int)(timespan.TotalHours % (double)HoursPerDay);

	public float HourOfDay => (float)(timespan.TotalHours % (double)HoursPerDay);

	public long ElapsedSeconds => (long)(timespan.TotalSeconds - (double)totalSecondsStart);

	public double ElapsedHours => (double)ElapsedSeconds / 60.0 / 60.0;

	public double ElapsedDays => ElapsedHours / (double)HoursPerDay;

	public long TotalSeconds => (long)timespan.TotalSeconds;

	public double TotalHours => timespan.TotalHours;

	public double TotalDays => timespan.TotalHours / (double)HoursPerDay + (double)timelapse;

	public int DayOfYear => (int)(TotalDays % (double)DaysPerYear);

	public float DayOfYearf => (float)(TotalDays % (double)DaysPerYear);

	public int Year => (int)(TotalDays / (double)DaysPerYear);

	public int Month => (int)Math.Ceiling(YearRel * (float)MonthsPerYear);

	public float Monthf => YearRel * (float)MonthsPerYear;

	public EnumMonth MonthName => (EnumMonth)Month;

	public float YearRel => (float)(GameMath.Mod(TotalDays, DaysPerYear) / (double)DaysPerYear);

	int IGameCalendar.DaysPerYear => DaysPerYear;

	float IGameCalendar.HoursPerDay => HoursPerDay;

	public EnumMoonPhase MoonPhase => (EnumMoonPhase)((int)(TotalDays / 2.0 + 5.0) % MoonOrbitDays);

	public double MoonPhaseExact => GetMoonPhase(TotalDays);

	public bool Dusk => (double)(HourOfDay / HoursPerDay) > 0.5;

	public float MoonPhaseBrightness
	{
		get
		{
			double ph = MoonPhaseExact;
			float i = (float)ph - (float)(int)ph;
			float num = MoonBrightnesByPhase[(int)ph];
			float b = MoonBrightnesByPhase[(int)(ph + 1.0) % MoonOrbitDays];
			return num * (1f - i) + b * i;
		}
	}

	public float MoonSize
	{
		get
		{
			double ph = MoonPhaseExact;
			float i = (float)ph - (float)(int)ph;
			float num = MoonSizeByPhase[(int)ph];
			float b = MoonSizeByPhase[(int)(ph + 1.0) % MoonOrbitDays];
			return (num * (1f - i) + b * i) * superMoonSize;
		}
	}

	public float SunsetMod { get; protected set; }

	public bool IsRunning => watchIngameTime.IsRunning;

	public SolarSphericalCoordsDelegate OnGetSolarSphericalCoords { get; set; }

	public HemisphereDelegate OnGetHemisphere { get; set; }

	public GetLatitudeDelegate OnGetLatitude { get; set; }

	public float? SeasonOverride { get; set; }

	public GameCalendar(IAsset sunlightTexture, int worldSeed, long totalSecondsStart = 4176000L)
	{
		OnGetSolarSphericalCoords = (double posX, double posZ, float yearRel, float dayRel) => new SolarSphericalCoords((float)Math.PI * 2f * GameMath.Mod(HourOfDay / HoursPerDay, 1f) - (float)Math.PI, 0f);
		OnGetLatitude = (double posZ) => 0.5;
		watchIngameTime = new Stopwatch();
		timespan = TimeSpan.FromSeconds(totalSecondsStart);
		timeSpeedModifiers["baseline"] = 60f;
		BitmapRef bmp = BitmapCreateFromPng(sunlightTexture);
		sunLightTexture = bmp.Pixels;
		sunLightTextureSize = new Size2i(bmp.Width, bmp.Height);
		bmp.Dispose();
		sunsetModNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, worldSeed);
		superMoonNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, worldSeed + 12098);
	}

	public double GetMoonPhase(double totaldays)
	{
		return (totaldays / 2.0 + 4.5) % (double)MoonOrbitDays;
	}

	public Vec3f GetSunPosition(Vec3d pos, double totalDays)
	{
		float yearRel = (float)(GameMath.Mod(totalDays, DaysPerYear) / (double)DaysPerYear);
		float dayRel = (float)GameMath.Mod(totalDays, 1.0);
		SolarSphericalCoords solarSphericalCoords = OnGetSolarSphericalCoords(pos.X, pos.Z, yearRel, dayRel);
		float phi = solarSphericalCoords.AzimuthAngle;
		float theta = solarSphericalCoords.ZenithAngle;
		return new Vec3f(GameMath.Sin(theta) * GameMath.Sin(phi), GameMath.Cos(theta), GameMath.Sin(theta) * GameMath.Cos(phi));
	}

	public Vec3f GetMoonPosition(Vec3d position, double totalDays)
	{
		float moonOrbitRel = (float)GetMoonPhase(totalDays) / (float)MoonOrbitDays;
		float moonAngle = (float)Math.PI * 2f * moonOrbitRel;
		float dayRel = (float)GameMath.Mod(totalDays, 1.0);
		float earthAngle = (float)Math.PI * -2f * dayRel;
		return new Vec3f(50f * (float)Math.Cos(earthAngle + moonAngle), 50f * (float)Math.Sin(earthAngle + moonAngle), (float)(15.0 + 45.0 * Math.Sin(earthAngle + moonAngle))).Normalize();
	}

	public float RealSecondsToGameSeconds(float seconds)
	{
		return seconds * currentSpeedOfTime * CalendarSpeedMul;
	}

	public void SetSeasonOverride(float? seasonRel)
	{
		SeasonOverride = seasonRel;
	}

	public void SetTimeSpeedModifier(string name, float speed)
	{
		timeSpeedModifiers[name] = speed;
		CalculateCurrentTimeSpeed();
	}

	public void RemoveTimeSpeedModifier(string name)
	{
		timeSpeedModifiers.Remove(name);
		CalculateCurrentTimeSpeed();
	}

	private void CalculateCurrentTimeSpeed()
	{
		float totalSpeed = 0f;
		foreach (float speed in timeSpeedModifiers.Values)
		{
			totalSpeed += speed;
		}
		currentSpeedOfTime = totalSpeed;
		DayLengthInRealLifeSeconds = ((currentSpeedOfTime == 0f) ? float.MaxValue : (3600f * HoursPerDay / currentSpeedOfTime / CalendarSpeedMul));
	}

	public void SetTotalSeconds(long totalSecondsNow, long totalSecondsStart)
	{
		timespan = TimeSpan.FromSeconds(totalSecondsNow);
		this.totalSecondsStart = totalSecondsStart;
	}

	public void Start()
	{
		if (!watchIngameTime.IsRunning)
		{
			watchIngameTime.Start();
		}
	}

	public void Stop()
	{
		if (watchIngameTime.IsRunning)
		{
			watchIngameTime.Stop();
		}
	}

	public virtual void Tick()
	{
		if (watchIngameTime.IsRunning)
		{
			double elapsedGameSeconds = (double)watchIngameTime.ElapsedMilliseconds / 1000.0 * (double)SpeedOfTime * (double)CalendarSpeedMul;
			timespan += TimeSpan.FromSeconds(elapsedGameSeconds);
			watchIngameTime.Restart();
			Update();
		}
	}

	public virtual void Update()
	{
		float sub = Math.Max(0f, 1.15f - MoonSize);
		double noiseval = superMoonNoise.Noise(0.0, TotalDays / 8.0);
		superMoonSize = (float)GameMath.Clamp((noiseval - 0.74 - (double)sub) * 50.0, 1.0, 2.5);
	}

	public void SetDayTime(float wantHourOfDay)
	{
		float hoursToAdd = ((!(HourOfDay > wantHourOfDay)) ? (wantHourOfDay - HourOfDay) : (wantHourOfDay + (HoursPerDay - HourOfDay)));
		Add(hoursToAdd);
	}

	public void SetMonth(float month)
	{
		float hoursToAdd = ((!(Monthf > month)) ? ((month - Monthf) * HoursPerDay * (float)DaysPerMonth + 12f) : ((month + ((float)MonthsPerYear - Monthf)) * HoursPerDay * (float)DaysPerMonth + 12f));
		Add(hoursToAdd);
	}

	public void Add(float hours)
	{
		TimeSpan toadd = TimeSpan.FromHours(hours);
		timespan = timespan.Add(toadd);
	}

	public float GetDayLightStrength(double x, double z)
	{
		SolarSphericalCoords solarSphericalCoords = OnGetSolarSphericalCoords(x, z, YearRel, HourOfDay / HoursPerDay);
		GameMath.Mod(HourOfDay / HoursPerDay, 1f);
		float moonOrbitRel = (float)MoonPhaseExact / (float)MoonOrbitDays;
		float phi = solarSphericalCoords.AzimuthAngle;
		float theta = solarSphericalCoords.ZenithAngle;
		Vec3f sunPos = new Vec3f(GameMath.Sin(theta) * GameMath.Sin(phi), GameMath.Cos(theta), GameMath.Sin(theta) * GameMath.Cos(phi));
		Vec3f moonPos = GetMoonPosition(null, TotalDays);
		Vec3f vec3f = moonPos.Normalize();
		float sunIntensity = (GameMath.Clamp(sunPos.Y * 1.4f + 0.2f, -1f, 1f) + 1f) / 2f;
		float moonYRel = vec3f.Y;
		float bright = GameMath.Clamp(MoonPhaseBrightness * (0.66f + 0.33f * moonYRel), -0.2f, MoonPhaseBrightness);
		float eclipseDarkening = GameMath.Clamp((sunPos.Dot(moonPos) - 0.9996f) * 3000f, 0f, sunIntensity * 0.6f);
		sunIntensity = Math.Max(0f, sunIntensity - eclipseDarkening);
		bright = Math.Max(0f, bright - eclipseDarkening);
		float val = GameMath.Lerp(0f, bright, GameMath.Clamp(moonYRel * 20f, 0f, 1f));
		float sunExtraIntensity = Math.Max(0f, (sunPos.Y - 0.4f) / 7.5f);
		Color colSun = getSunlightPixelRel(GameMath.Clamp(sunIntensity + SunsetMod, 0f, 1f), 0.01f);
		return Math.Max(val, (float)(colSun.R + colSun.G + colSun.B) / 3f / 255f + sunExtraIntensity);
	}

	public float GetDayLightStrength(BlockPos pos)
	{
		return GetDayLightStrength(pos.X, pos.Z);
	}

	public EnumSeason GetSeason(BlockPos pos)
	{
		float val = GameMath.Mod(GetSeasonRel(pos) - 0.21916668f, 1f);
		return (EnumSeason)(4f * val);
	}

	public float GetSeasonRel(BlockPos pos)
	{
		if (SeasonOverride.HasValue)
		{
			return SeasonOverride.Value;
		}
		if (GetHemisphere(pos) != 0)
		{
			return (YearRel + 0.5f) % 1f;
		}
		return YearRel;
	}

	public EnumHemisphere GetHemisphere(BlockPos pos)
	{
		if (OnGetHemisphere != null)
		{
			return OnGetHemisphere(pos.X, pos.Z);
		}
		return EnumHemisphere.North;
	}

	public Color getSunlightPixelRel(float relx, float rely)
	{
		float num = Math.Min(sunLightTextureSize.Width - 1, relx * (float)sunLightTextureSize.Width);
		int x = (int)num;
		int y = (int)Math.Min(sunLightTextureSize.Height - 1, rely * (float)sunLightTextureSize.Height);
		float lx = num - (float)x;
		int col1 = sunLightTexture[y * sunLightTextureSize.Width + x];
		int col2 = sunLightTexture[y * sunLightTextureSize.Width + Math.Min(sunLightTextureSize.Width - 1, x + 1)];
		return Color.FromArgb(GameMath.LerpRgbaColor(lx, col1, col2));
	}

	public Packet_Server ToPacket()
	{
		string[] names = new string[timeSpeedModifiers.Count];
		int[] speeds = new int[timeSpeedModifiers.Count];
		int i = 0;
		foreach (KeyValuePair<string, float> val in timeSpeedModifiers)
		{
			names[i] = val.Key;
			speeds[i] = CollectibleNet.SerializeFloatPrecise(val.Value);
			i++;
		}
		Packet_ServerCalendar p = new Packet_ServerCalendar
		{
			TotalSeconds = (long)timespan.TotalSeconds,
			TotalSecondsStart = totalSecondsStart,
			MoonOrbitDays = MoonOrbitDays,
			DaysPerMonth = DaysPerMonth,
			HoursPerDay = CollectibleNet.SerializeFloatVeryPrecise(HoursPerDay),
			CalendarSpeedMul = CollectibleNet.SerializeFloatVeryPrecise(calendarSpeedMul)
		};
		p.SetTimeSpeedModifierNames(names);
		p.SetTimeSpeedModifierSpeeds(speeds);
		p.Running = (watchIngameTime.IsRunning ? 1 : 0);
		return new Packet_Server
		{
			Id = 13,
			Calendar = p
		};
	}

	public string PrettyDate()
	{
		float hourOfDay = HourOfDay;
		int hour = (int)hourOfDay;
		int minute = (int)((hourOfDay - (float)hour) * 60f);
		return Lang.Get("dateformat", DayOfMonth, Lang.Get("month-" + MonthName), Year.ToString("0"), hour.ToString("00"), minute.ToString("00"));
	}

	public void UpdateFromPacket(Packet_Server packet)
	{
		Packet_ServerCalendar calpacket = packet.Calendar;
		SetTotalSeconds(calpacket.TotalSeconds, calpacket.TotalSecondsStart);
		timeSpeedModifiers.Clear();
		for (int i = 0; i < calpacket.TimeSpeedModifierNamesCount; i++)
		{
			timeSpeedModifiers[calpacket.TimeSpeedModifierNames[i]] = CollectibleNet.DeserializeFloatPrecise(calpacket.TimeSpeedModifierSpeeds[i]);
		}
		MoonOrbitDays = calpacket.MoonOrbitDays;
		HoursPerDay = CollectibleNet.DeserializeFloatVeryPrecise(calpacket.HoursPerDay);
		calendarSpeedMul = CollectibleNet.DeserializeFloatVeryPrecise(calpacket.CalendarSpeedMul);
		DaysPerMonth = calpacket.DaysPerMonth;
		if (HoursPerDay == 0f)
		{
			throw new ArgumentException("Trying to set 0 hours per day.");
		}
		if (calpacket.Running > 0)
		{
			if (!watchIngameTime.IsRunning)
			{
				watchIngameTime.Start();
			}
		}
		else if (watchIngameTime.IsRunning)
		{
			watchIngameTime.Stop();
		}
		CalculateCurrentTimeSpeed();
	}

	public BitmapRef BitmapCreateFromPng(IAsset asset)
	{
		return new BitmapExternal(new MemoryStream(asset.Data));
	}
}
