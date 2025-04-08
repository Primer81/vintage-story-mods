using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// Adds a basic music track.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class SurfaceMusicTrack : IMusicTrack
{
	/// <summary>
	/// The location of the track.
	/// </summary>
	[JsonProperty("File")]
	public AssetLocation Location;

	/// <summary>
	/// The current play style of the track
	/// </summary>
	[JsonProperty]
	public string OnPlayList = "*";

	public string[] OnPlayLists;

	/// <summary>
	/// Minimum sunlight to play the track.
	/// </summary>
	[JsonProperty]
	public int MinSunlight = 5;

	/// <summary>
	/// Earliest to play the track.
	/// </summary>
	[JsonProperty]
	public float MinHour;

	/// <summary>
	/// Latest to play the track.
	/// </summary>
	[JsonProperty]
	public float MaxHour = 24f;

	[JsonProperty]
	public float Chance = 1f;

	[JsonProperty]
	public float MaxTemperature = 99f;

	[JsonProperty]
	public float MinRainFall = -99f;

	[JsonProperty]
	public float MinSeason;

	[JsonProperty]
	public float MaxSeason = 1f;

	[JsonProperty]
	public float MinLatitude;

	[JsonProperty]
	public float MaxLatitude = 1f;

	[JsonProperty]
	public float DistanceToSpawnPoint = -1f;

	/// <summary>
	/// Is it loading?
	/// </summary>
	private bool loading;

	/// <summary>
	/// Get the current sound file.
	/// </summary>
	public ILoadedSound Sound;

	/// <summary>
	/// The music seed for random values.
	/// </summary>
	private static Random rand = new Random();

	/// <summary>
	/// Cooldowns between songs. First value is the minimum delay, second value is added randomness. (in seconds)
	/// </summary>
	private static readonly float[][] AnySongCoolDowns = new float[4][]
	{
		new float[2] { 960f, 480f },
		new float[2] { 420f, 240f },
		new float[2] { 180f, 120f },
		new float[2]
	};

	/// <summary>
	/// Time before we play the same song again. First value is the minimum delay, second value is added randomness. (in seconds)
	/// </summary>
	private static readonly float[][] SameSongCoolDowns = new float[4][]
	{
		new float[2] { 1500f, 1200f },
		new float[2] { 1200f, 1200f },
		new float[2] { 900f, 900f },
		new float[2] { 480f, 300f }
	};

	public static bool ShouldPlayMusic = true;

	/// <summary>
	/// Is this track initialized?
	/// </summary>
	private static bool initialized = false;

	/// <summary>
	/// Global cooldown until next track
	/// </summary>
	public static long globalCooldownUntilMs;

	/// <summary>
	/// Cooldown for each track by name.
	/// </summary>
	public static Dictionary<string, long> tracksCooldownUntilMs = new Dictionary<string, long>();

	/// <summary>
	/// Core client API.
	/// </summary>
	protected ICoreClientAPI capi;

	protected IMusicEngine musicEngine;

	protected float nowMinHour;

	protected float nowMaxHour;

	/// <summary>
	/// Gets the previous frequency setting.
	/// </summary>
	protected static int prevFrequency;

	/// <summary>
	/// The current song's priority. If higher than 1, will stop other tracks and start this one
	/// </summary>
	[JsonProperty]
	public float Priority { get; set; } = 1f;


	/// <summary>
	/// The songs starting priority. If higher than 1, then it will be started first. But does not interrupt already running tracks. When reading a songs start priority the maximum of start priority and priority is used
	/// </summary>
	[JsonProperty("StartPriority")]
	public NatFloat StartPriorityRnd { get; set; } = NatFloat.One;


	public float StartPriority { get; set; }

	/// <summary>
	/// Is the current song actively playing or is it loading? (False if neither action.
	/// </summary>
	public bool IsActive
	{
		get
		{
			if (!loading)
			{
				if (Sound != null)
				{
					return Sound.IsPlaying;
				}
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// The name of the track.
	/// </summary>
	public string Name => Location.ToShortString();

	/// <summary>
	/// Gets the current Music Frequency setting.
	/// </summary>
	public int MusicFrequency => capi.Settings.Int["musicFrequency"];

	public string PositionString => $"{(int)Sound.PlaybackPosition}/{(int)Sound.SoundLengthSeconds}";

	/// <summary>
	/// Initialize the track.
	/// </summary>
	/// <param name="assetManager">the global Asset Manager</param>
	/// <param name="capi">The Core Client API</param>
	/// <param name="musicEngine"></param>
	public virtual void Initialize(IAssetManager assetManager, ICoreClientAPI capi, IMusicEngine musicEngine)
	{
		this.capi = capi;
		this.musicEngine = musicEngine;
		OnPlayLists = OnPlayList.Split('|');
		for (int i = 0; i < OnPlayLists.Length; i++)
		{
			OnPlayLists[i] = OnPlayLists[i].ToLowerInvariant();
		}
		selectMinMaxHour();
		Location.Path = "music/" + Location.Path.ToLowerInvariant() + ".ogg";
		if (!initialized)
		{
			globalCooldownUntilMs = (long)(1000.0 * ((double)(AnySongCoolDowns[MusicFrequency][0] / 4f) + rand.NextDouble() * (double)AnySongCoolDowns[MusicFrequency][1] / 2.0));
			capi.Settings.Int.AddWatcher("musicFrequency", delegate(int newval)
			{
				FrequencyChanged(newval, capi);
			});
			initialized = true;
			prevFrequency = MusicFrequency;
		}
	}

	public virtual void BeginSort()
	{
		StartPriority = Math.Max(1f, StartPriorityRnd.nextFloat(1f, rand));
	}

	protected virtual void selectMinMaxHour()
	{
		Random rnd = capi.World.Rand;
		float hourRange = Math.Min(2 + Math.Max(0, prevFrequency), MaxHour - MinHour);
		nowMinHour = Math.Max(MinHour, Math.Min(MaxHour - 1f, MinHour - 1f + (float)(rnd.NextDouble() * (double)(hourRange + 1f))));
		nowMaxHour = Math.Min(MaxHour, nowMinHour + hourRange);
	}

	/// <summary>
	/// The Frequency change in the static system.
	/// </summary>
	/// <param name="newFreq">The new frequency</param>
	/// <param name="capi">the core client API</param>
	protected static void FrequencyChanged(int newFreq, ICoreClientAPI capi)
	{
		if (newFreq > prevFrequency)
		{
			globalCooldownUntilMs = 0L;
		}
		if (newFreq < prevFrequency)
		{
			globalCooldownUntilMs = (long)((double)capi.World.ElapsedMilliseconds + 1000.0 * ((double)(AnySongCoolDowns[newFreq][0] / 4f) + rand.NextDouble() * (double)AnySongCoolDowns[newFreq][1] / 2.0));
		}
		prevFrequency = newFreq;
	}

	/// <summary>
	/// Should this current track play?
	/// </summary>
	/// <param name="props">Player Properties</param>
	/// <param name="conds"></param>
	/// <param name="pos"></param>
	/// <returns>Should we play the current track?</returns>
	public virtual bool ShouldPlay(TrackedPlayerProperties props, ClimateCondition conds, BlockPos pos)
	{
		if (IsActive || !ShouldPlayMusic)
		{
			return false;
		}
		if (capi.World.ElapsedMilliseconds < globalCooldownUntilMs)
		{
			return false;
		}
		if (OnPlayList != "*" && !OnPlayLists.Contains(props.PlayListCode))
		{
			return false;
		}
		if (props.sunSlight < (float)MinSunlight)
		{
			return false;
		}
		if (musicEngine.LastPlayedTrack == this)
		{
			return false;
		}
		if (conds.Temperature > MaxTemperature)
		{
			return false;
		}
		if (conds.Rainfall < MinRainFall)
		{
			return false;
		}
		if (props.DistanceToSpawnPoint < DistanceToSpawnPoint)
		{
			return false;
		}
		float season = capi.World.Calendar.GetSeasonRel(pos);
		if (season < MinSeason || season > MaxSeason)
		{
			return false;
		}
		float latitude = (float)Math.Abs(capi.World.Calendar.OnGetLatitude(pos.Z));
		if (latitude < MinLatitude || latitude > MaxLatitude)
		{
			return false;
		}
		tracksCooldownUntilMs.TryGetValue(Name, out var trackCoolDownMs);
		if (capi.World.ElapsedMilliseconds < trackCoolDownMs)
		{
			return false;
		}
		if (prevFrequency == 3)
		{
			float hour = capi.World.Calendar.HourOfDay / 24f * capi.World.Calendar.HoursPerDay;
			if (hour < MinHour || hour > MaxHour)
			{
				return false;
			}
		}
		else
		{
			float hour2 = capi.World.Calendar.HourOfDay / 24f * capi.World.Calendar.HoursPerDay;
			if (hour2 < nowMinHour || hour2 > nowMaxHour)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Begins playing the Music track.
	/// </summary>
	/// <param name="props">Player Properties</param>
	public virtual void BeginPlay(TrackedPlayerProperties props)
	{
		loading = true;
		Sound?.Dispose();
		musicEngine.LoadTrack(Location, delegate(ILoadedSound sound)
		{
			if (sound != null)
			{
				sound.Start();
				if (!loading)
				{
					sound.Stop();
					sound.Dispose();
				}
				else
				{
					Sound = sound;
				}
			}
			loading = false;
		});
	}

	/// <summary>
	/// Is it cool for the current track to continue playing?
	/// </summary>
	/// <param name="dt">Delta Time/Change in time.</param>
	/// <param name="props">Track properties.</param>
	/// <returns>Cool or not cool?</returns>
	public virtual bool ContinuePlay(float dt, TrackedPlayerProperties props)
	{
		if (!IsActive)
		{
			Sound?.Dispose();
			Sound = null;
			SetCooldown(1f);
			return false;
		}
		if (!ShouldPlayMusic)
		{
			FadeOut(3f);
		}
		return true;
	}

	/// <summary>
	/// Fades out the current track.  
	/// </summary>
	/// <param name="seconds">The duration of the fade out in seconds.</param>
	/// <param name="onFadedOut">What to have happen after the track has faded out.</param>
	public virtual void FadeOut(float seconds, Action onFadedOut = null)
	{
		loading = false;
		if (Sound != null && IsActive)
		{
			Sound.FadeOut(seconds, delegate(ILoadedSound sound)
			{
				sound.Dispose();
				Sound = null;
				onFadedOut?.Invoke();
			});
			SetCooldown(0.5f);
		}
		else
		{
			SetCooldown(1f);
		}
	}

	/// <summary>
	/// Sets the cooldown of the current track.
	/// </summary>
	/// <param name="multiplier">The multiplier for the cooldown.</param>
	public virtual void SetCooldown(float multiplier)
	{
		globalCooldownUntilMs = (long)((float)capi.World.ElapsedMilliseconds + (float)(long)(1000.0 * ((double)AnySongCoolDowns[MusicFrequency][0] + rand.NextDouble() * (double)AnySongCoolDowns[MusicFrequency][1])) * multiplier);
		tracksCooldownUntilMs[Name] = (long)((float)capi.World.ElapsedMilliseconds + (float)(long)(1000.0 * ((double)SameSongCoolDowns[MusicFrequency][0] + rand.NextDouble() * (double)SameSongCoolDowns[MusicFrequency][1])) * multiplier);
		selectMinMaxHour();
	}

	/// <summary>
	/// Updates the volume of the current track provided Sound is not null. (effectively calls Sound.SetVolume)
	/// </summary>
	public virtual void UpdateVolume()
	{
		if (Sound != null)
		{
			Sound.SetVolume();
		}
	}

	public virtual void FastForward(float seconds)
	{
		if (Sound.PlaybackPosition + seconds > Sound.SoundLengthSeconds)
		{
			Sound.Stop();
		}
		Sound.PlaybackPosition += seconds;
	}
}
