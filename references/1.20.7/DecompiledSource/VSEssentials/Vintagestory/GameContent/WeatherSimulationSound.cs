using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherSimulationSound
{
	private WeatherSystemClient weatherSys;

	private ILoadedSound[] rainSoundsLeafless;

	private ILoadedSound[] rainSoundsLeafy;

	private ILoadedSound lowTrembleSound;

	private ILoadedSound hailSound;

	private ILoadedSound windSoundLeafy;

	private ILoadedSound windSoundLeafless;

	private ICoreClientAPI capi;

	private bool windSoundsOn;

	private bool rainSoundsOn;

	private bool hailSoundsOn;

	private float curWindVolumeLeafy;

	private float curWindVolumeLeafless;

	private float curRainVolumeLeafy;

	private float curRainVolumeLeafless;

	private float curRainPitch = 1f;

	private float curHailVolume;

	private float curHailPitch = 1f;

	private float curTrembleVolume;

	private float curTremblePitch;

	private float quarterSecAccum;

	private bool searchComplete = true;

	public static float roomVolumePitchLoss;

	private bool soundsReady;

	private BlockPos plrPos = new BlockPos();

	public WeatherSimulationSound(ICoreClientAPI capi, WeatherSystemClient weatherSys)
	{
		this.weatherSys = weatherSys;
		this.capi = capi;
	}

	internal void Initialize()
	{
		lowTrembleSound = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/tracks/verylowtremble.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
		hailSound = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/tracks/hail.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
		rainSoundsLeafless = new ILoadedSound[1];
		rainSoundsLeafless[0] = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/tracks/rain-leafless.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
		rainSoundsLeafy = new ILoadedSound[1];
		rainSoundsLeafy[0] = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/tracks/rain-leafy.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
		windSoundLeafy = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/wind-leafy.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
		windSoundLeafless = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/weather/wind-leafless.ogg"),
			ShouldLoop = true,
			DisposeOnFinish = false,
			Position = new Vec3f(0f, 0f, 0f),
			RelativePosition = true,
			Range = 16f,
			SoundType = EnumSoundType.Weather,
			Volume = 1f
		});
	}

	public void Update(float dt)
	{
		if (lowTrembleSound != null)
		{
			dt = Math.Min(0.5f, dt);
			quarterSecAccum += dt;
			if (quarterSecAccum > 0.25f)
			{
				updateSounds(dt);
			}
		}
	}

	private void updateSounds(float dt)
	{
		if (!soundsReady)
		{
			if (!lowTrembleSound.IsReady || !hailSound.IsReady || !rainSoundsLeafless.All((ILoadedSound s) => s.IsReady) || !rainSoundsLeafy.All((ILoadedSound s) => s.IsReady) || !windSoundLeafy.IsReady || !windSoundLeafless.IsReady)
			{
				return;
			}
			soundsReady = true;
		}
		float targetRainVolumeLeafy = 0f;
		float targetRainVolumeLeafless = 0f;
		float targetHailVolume = 0f;
		float targetTrembleVolume = 0f;
		float targetTremblePitch = 0f;
		float targetRainPitch = 1f;
		float targetHailPitch = 1f;
		WeatherDataSnapshot weatherData = weatherSys.BlendedWeatherData;
		if (searchComplete)
		{
			EntityPlayer eplr = capi.World.Player.Entity;
			plrPos.Set((int)eplr.Pos.X, (int)eplr.Pos.Y, (int)eplr.Pos.Z);
			searchComplete = false;
			TyronThreadPool.QueueTask(delegate
			{
				int distanceToRainFall = capi.World.BlockAccessor.GetDistanceToRainFall(plrPos, 12, 4);
				roomVolumePitchLoss = GameMath.Clamp((float)Math.Pow(Math.Max(0f, (float)(distanceToRainFall - 2) / 10f), 2.0), 0f, 1f);
				searchComplete = true;
			}, "weathersimulationsound");
		}
		EnumPrecipitationType precType = weatherData.BlendedPrecType;
		if (precType == EnumPrecipitationType.Auto)
		{
			precType = ((weatherSys.clientClimateCond?.Temperature < weatherData.snowThresholdTemp) ? EnumPrecipitationType.Snow : EnumPrecipitationType.Rain);
		}
		float nearbyLeaviness = GameMath.Clamp(GlobalConstants.CurrentNearbyRelLeavesCountClient * 60f, 0f, 1f);
		ClimateCondition conds = weatherSys.clientClimateCond;
		if (conds.Rainfall > 0f)
		{
			if (precType == EnumPrecipitationType.Rain || weatherSys.clientClimateCond.Temperature < weatherData.snowThresholdTemp)
			{
				targetRainVolumeLeafy = nearbyLeaviness * GameMath.Clamp(conds.Rainfall * 2f - Math.Max(0f, 2f * (weatherData.snowThresholdTemp - weatherSys.clientClimateCond.Temperature)), 0f, 1f);
				targetRainVolumeLeafy = GameMath.Max(0f, targetRainVolumeLeafy - roomVolumePitchLoss);
				targetRainVolumeLeafless = Math.Max(0.3f, 1f - nearbyLeaviness) * GameMath.Clamp(conds.Rainfall * 2f - Math.Max(0f, 2f * (weatherData.snowThresholdTemp - weatherSys.clientClimateCond.Temperature)), 0f, 1f);
				targetRainVolumeLeafless = GameMath.Max(0f, targetRainVolumeLeafless - roomVolumePitchLoss);
				targetRainPitch = Math.Max(0.7f, 1.25f - conds.Rainfall * 0.7f);
				targetRainPitch = Math.Max(0f, targetRainPitch - roomVolumePitchLoss / 4f);
				targetTrembleVolume = GameMath.Clamp(conds.Rainfall * 1.6f - 0.8f - roomVolumePitchLoss * 0.25f, 0f, 1f);
				targetTremblePitch = GameMath.Clamp(1f - roomVolumePitchLoss * 0.65f, 0f, 1f);
				if (!rainSoundsOn && ((double)targetRainVolumeLeafy > 0.01 || (double)targetRainVolumeLeafless > 0.01))
				{
					for (int n = 0; n < rainSoundsLeafless.Length; n++)
					{
						rainSoundsLeafless[n]?.Start();
					}
					for (int m = 0; m < rainSoundsLeafy.Length; m++)
					{
						rainSoundsLeafy[m]?.Start();
					}
					lowTrembleSound?.Start();
					rainSoundsOn = true;
					curRainPitch = targetRainPitch;
				}
				if (capi.World.Player.Entity.IsEyesSubmerged())
				{
					curRainPitch = targetRainPitch / 2f;
					targetRainVolumeLeafy *= 0.75f;
					targetRainVolumeLeafless *= 0.75f;
				}
			}
			if (precType == EnumPrecipitationType.Hail)
			{
				targetHailVolume = GameMath.Clamp(conds.Rainfall * 2f - roomVolumePitchLoss, 0f, 1f);
				targetHailVolume = GameMath.Max(0f, targetHailVolume - roomVolumePitchLoss);
				targetHailPitch = Math.Max(0.7f, 1.25f - conds.Rainfall * 0.7f);
				targetHailPitch = Math.Max(0f, targetHailPitch - roomVolumePitchLoss / 4f);
				if (!hailSoundsOn && (double)targetHailVolume > 0.01)
				{
					hailSound?.Start();
					hailSoundsOn = true;
					curHailPitch = targetHailPitch;
				}
			}
		}
		curRainVolumeLeafy += (targetRainVolumeLeafy - curRainVolumeLeafy) * dt / 2f;
		curRainVolumeLeafless += (targetRainVolumeLeafless - curRainVolumeLeafless) * dt / 2f;
		curTrembleVolume += (targetTrembleVolume - curTrembleVolume) * dt;
		curHailVolume += (targetHailVolume - curHailVolume) * dt;
		curHailPitch += (targetHailPitch - curHailPitch) * dt;
		curRainPitch += (targetRainPitch - curRainPitch) * dt;
		curTremblePitch += (targetTremblePitch - curTremblePitch) * dt;
		if (rainSoundsOn)
		{
			for (int l = 0; l < rainSoundsLeafless.Length; l++)
			{
				rainSoundsLeafless[l]?.SetVolume(curRainVolumeLeafless);
				rainSoundsLeafless[l]?.SetPitch(curRainPitch);
			}
			for (int k = 0; k < rainSoundsLeafy.Length; k++)
			{
				rainSoundsLeafy[k]?.SetVolume(curRainVolumeLeafy);
				rainSoundsLeafy[k]?.SetPitch(curRainPitch);
			}
		}
		lowTrembleSound?.SetVolume(curTrembleVolume);
		lowTrembleSound?.SetPitch(curTremblePitch);
		if (hailSoundsOn)
		{
			hailSound?.SetVolume(curHailVolume);
			hailSound?.SetPitch(curHailPitch);
		}
		if ((double)curRainVolumeLeafless < 0.01 && (double)curRainVolumeLeafy < 0.01)
		{
			for (int j = 0; j < rainSoundsLeafless.Length; j++)
			{
				rainSoundsLeafless[j]?.Stop();
			}
			for (int i = 0; i < rainSoundsLeafy.Length; i++)
			{
				rainSoundsLeafy[i]?.Stop();
			}
			rainSoundsOn = false;
		}
		if ((double)curHailVolume < 0.01)
		{
			hailSound?.Stop();
			hailSoundsOn = false;
		}
		float wstr = (1f - roomVolumePitchLoss) * weatherData.curWindSpeed.X - 0.3f;
		if (wstr > 0.03f || curWindVolumeLeafy > 0.01f || curWindVolumeLeafless > 0.01f)
		{
			if (!windSoundsOn)
			{
				windSoundLeafy?.Start();
				windSoundLeafless?.Start();
				windSoundsOn = true;
			}
			float targetVolumeLeafy = nearbyLeaviness * 1.2f * wstr;
			float targetVolumeLeafless = (1f - nearbyLeaviness) * 1.2f * wstr;
			curWindVolumeLeafy += (targetVolumeLeafy - curWindVolumeLeafy) * dt;
			curWindVolumeLeafless += (targetVolumeLeafless - curWindVolumeLeafless) * dt;
			windSoundLeafy?.SetVolume(Math.Max(0f, curWindVolumeLeafy));
			windSoundLeafless?.SetVolume(Math.Max(0f, curWindVolumeLeafless));
		}
		else if (windSoundsOn)
		{
			windSoundLeafy?.Stop();
			windSoundLeafless?.Stop();
			windSoundsOn = false;
		}
	}

	public void Dispose()
	{
		if (rainSoundsLeafless != null)
		{
			ILoadedSound[] array = rainSoundsLeafless;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Dispose();
			}
		}
		if (rainSoundsLeafy != null)
		{
			ILoadedSound[] array = rainSoundsLeafy;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Dispose();
			}
		}
		hailSound?.Dispose();
		lowTrembleSound?.Dispose();
		windSoundLeafy?.Dispose();
		windSoundLeafless?.Dispose();
	}
}
