using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class AudioOpenAl : IDisposable
{
	public GameExit d_GameExit;

	private ALContext Context = ALContext.Null;

	private ALDevice Device;

	public static bool UseHrtf;

	public static bool HasEffectsExtension;

	private const int ALC_HRTF_SOFT = 6546;

	private const int ALC_OUTPUT_MODE_SOFT = 6572;

	private const int HrtfEnabled = 5377;

	private const int HrtfDisabled = 6574;

	private const int ALC_FREQUENCY = 4103;

	public const int AL_REMIX_UNMATCHED_SOFT = 2;

	public const int AL_DIRECT_CHANNELS_SOFT = 4147;

	private static ReverbEffect[] reverbEffectsByReverbness = new ReverbEffect[24];

	private static ReverbEffect NoReverb = new ReverbEffect();

	public static int EchoFilterId;

	public IList<string> Devices => ALC.GetString(AlcGetStringList.AllDevicesSpecifier).ToList();

	public string CurrentDevice => ALC.GetString(Device, AlcGetString.DeviceSpecifier);

	public float MasterSoundLevel
	{
		get
		{
			return AL.GetListener(ALListenerf.Gain);
		}
		set
		{
			AL.Listener(ALListenerf.Gain, value);
		}
	}

	public static ReverbEffect GetOrCreateReverbEffect(float reverbness)
	{
		if (reverbness < 0.25f)
		{
			return NoReverb;
		}
		float reverbMin = 0.5f;
		float range = 7f - reverbMin;
		int key = Math.Min(Math.Max(0, (int)((reverbness - reverbMin) / range * 24f)), 23);
		ReverbEffect effe = reverbEffectsByReverbness[key];
		if (effe == null && HasEffectsExtension)
		{
			int reverbEffectSlot = ALC.EFX.GenAuxiliaryEffectSlot();
			int reverbEffectId = ALC.EFX.GenEffect();
			ALC.EFX.Effect(reverbEffectId, EffectInteger.EffectType, 1);
			ALC.EFX.Effect(reverbEffectId, EffectFloat.ReverbDecayTime, (float)key / 23f * range + reverbMin);
			ALC.EFX.AuxiliaryEffectSlot(reverbEffectSlot, EffectSlotInteger.Effect, reverbEffectId);
			ReverbEffect[] array = reverbEffectsByReverbness;
			ReverbEffect obj = new ReverbEffect
			{
				reverbEffectId = reverbEffectId,
				ReverbEffectSlot = reverbEffectSlot
			};
			ReverbEffect reverbEffect = obj;
			array[key] = obj;
			effe = reverbEffect;
		}
		return effe;
	}

	public AudioOpenAl(ILogger logger)
	{
		initContext(logger);
	}

	~AudioOpenAl()
	{
		Dispose(disposing: false);
	}

	protected virtual void Dispose(bool disposing)
	{
		LoadedSoundNative.DisposeAllSounds();
		ReverbEffect[] array = reverbEffectsByReverbness;
		foreach (ReverbEffect val in array)
		{
			if (val != null && HasEffectsExtension)
			{
				ALC.EFX.DeleteEffect(val.reverbEffectId);
				ALC.EFX.DeleteAuxiliaryEffectSlot(val.ReverbEffectSlot);
			}
		}
		if (HasEffectsExtension)
		{
			ALC.EFX.DeleteFilter(EchoFilterId);
			EchoFilterId = 0;
		}
		if (Device != ALDevice.Null)
		{
			ALC.MakeContextCurrent(ALContext.Null);
			ALC.DestroyContext(Context);
			ALC.CloseDevice(Device);
			Device = ALDevice.Null;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void initContext(ILogger logger, string desiredDevice = null)
	{
		try
		{
			if (Device != ALDevice.Null)
			{
				ALC.MakeContextCurrent(ALContext.Null);
				ALC.DestroyContext(Context);
				ALC.CloseDevice(Device);
			}
			if (desiredDevice == null)
			{
				desiredDevice = ClientSettings.AudioDevice;
			}
			if (!Devices.Any((string d) => d.Equals(desiredDevice)))
			{
				desiredDevice = null;
				ClientSettings.AudioDevice = null;
			}
			Device = ALC.OpenDevice(desiredDevice);
			UseHrtf = ClientSettings.UseHRTFAudio;
			int[] hrtfSettings;
			if (ClientSettings.AllowSettingHRTFAudio)
			{
				hrtfSettings = ((!UseHrtf) ? new int[4] { 6546, 0, 6572, 6574 } : ((!ClientSettings.Force48kHzHRTFAudio) ? new int[4] { 6546, 1, 6572, 5377 } : new int[6] { 6546, 1, 6572, 5377, 4103, 48000 }));
			}
			else
			{
				hrtfSettings = new int[0];
				UseHrtf = false;
			}
			Context = ALC.CreateContext(Device, hrtfSettings);
			ALC.MakeContextCurrent(Context);
			CheckALError("Start");
			AL.Listener(ALListener3f.Velocity, 0f, 0f, 0f);
		}
		catch (Exception ex)
		{
			logger.Error("Failed creating audio context");
			logger.Error(ex);
			return;
		}
		ALContextAttributes alContextAttributes = ALC.GetContextAttributes(Device);
		logger.Notification("OpenAL Initialized. Available Mono/Stereo Sources: {0}/{1}", alContextAttributes.MonoSources, alContextAttributes.StereoSources);
		HasEffectsExtension = ALC.EFX.IsExtensionPresent(Device);
		if (!HasEffectsExtension)
		{
			logger.Notification("OpenAL Effects Extension not found. Disabling extra sound effects now.");
		}
	}

	public static void CheckALError(string str)
	{
		ALError error = AL.GetError();
		if (error != 0)
		{
			Console.WriteLine("ALError at '" + str + "': " + AL.GetErrorString(error));
		}
	}

	internal void SetDevice(ILogger logger, string text)
	{
		initContext(logger, text);
	}

	internal void RecreateContext(Logger logger)
	{
		initContext(logger, CurrentDevice);
	}

	public static byte[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		using BinaryReader reader = new BinaryReader(stream);
		if (new string(reader.ReadChars(4)) != "RIFF")
		{
			throw new NotSupportedException("Specified stream is not a wave file.");
		}
		reader.ReadInt32();
		if (new string(reader.ReadChars(4)) != "WAVE")
		{
			throw new NotSupportedException("Specified stream is not a wave file.");
		}
		if (new string(reader.ReadChars(4)) != "fmt ")
		{
			throw new NotSupportedException("Specified wave file is not supported.");
		}
		reader.ReadInt32();
		reader.ReadInt16();
		int num_channels = reader.ReadInt16();
		int sample_rate = reader.ReadInt32();
		reader.ReadInt32();
		reader.ReadInt16();
		int bits_per_sample = reader.ReadInt16();
		if (new string(reader.ReadChars(4)) != "data")
		{
			throw new NotSupportedException("Specified wave file is not supported.");
		}
		reader.ReadInt32();
		channels = num_channels;
		bits = bits_per_sample;
		rate = sample_rate;
		return reader.ReadBytes((int)reader.BaseStream.Length);
	}

	public static ALFormat GetSoundFormat(int channels, int bits)
	{
		switch (channels)
		{
		case 1:
			if (bits != 8)
			{
				return ALFormat.Mono16;
			}
			return ALFormat.Mono8;
		case 2:
			if (bits != 8)
			{
				return ALFormat.Stereo16;
			}
			return ALFormat.Stereo8;
		default:
			throw new NotSupportedException("The specified sound format is not supported (channels: " + channels + ").");
		}
	}

	public AudioMetaData GetSampleFromArray(IAsset asset)
	{
		Stream stream = new MemoryStream(asset.Data);
		if (stream.ReadByte() == 82 && stream.ReadByte() == 73 && stream.ReadByte() == 70 && stream.ReadByte() == 70)
		{
			stream.Position = 0L;
			int channels;
			int bits_per_sample;
			int sample_rate;
			byte[] sound_data = LoadWave(stream, out channels, out bits_per_sample, out sample_rate);
			return new AudioMetaData(asset)
			{
				Pcm = sound_data,
				BitsPerSample = bits_per_sample,
				Channels = channels,
				Rate = sample_rate,
				Loaded = 1
			};
		}
		stream.Position = 0L;
		return new OggDecoder().OggToWav(stream, asset);
	}

	public void UpdateListener(Vector3 position, Vector3 orientation)
	{
		try
		{
			AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z);
			Vector3 up = Vector3.UnitY;
			AL.Listener(ALListenerfv.Orientation, ref orientation, ref up);
		}
		catch
		{
		}
	}
}
