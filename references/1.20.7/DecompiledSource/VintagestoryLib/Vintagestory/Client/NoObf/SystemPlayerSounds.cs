using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemPlayerSounds : ClientSystem
{
	private ILoadedSound FlySound;

	private ILoadedSound UnderWaterSound;

	private Dictionary<AmbientSound, AmbientSound> ambientSounds = new Dictionary<AmbientSound, AmbientSound>();

	private WireframeCube[] wireframes;

	private bool fallActive;

	private bool underwaterActive;

	private float targetVolume;

	private float curVolume;

	private double flySpeed;

	public override string Name => "plso";

	public SystemPlayerSounds(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.FallSpeed, OnFallSpeedChange);
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInWaterDepth, OnSwimDepthChange);
		game.RegisterGameTickListener(OnGameTick, 20);
		game.eventManager.OnAmbientSoundsScanComplete = OnAmbientSoundScan;
		game.eventManager.RegisterRenderer(Render3D, EnumRenderStage.Opaque, "playersoundswireframe", 0.9);
		wireframes = new WireframeCube[6]
		{
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 255, 255, 0)),
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 255, 0, 0)),
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 0, 255, 0)),
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 0, 0, 255)),
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 0, 255, 255)),
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 255, 0, 255))
		};
	}

	public override void OnBlockTexturesLoaded()
	{
		FlySound = game.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/environment/wind.ogg"),
			ShouldLoop = true,
			RelativePosition = true,
			DisposeOnFinish = false
		});
		UnderWaterSound = game.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/environment/underwater.ogg"),
			ShouldLoop = true,
			RelativePosition = true,
			DisposeOnFinish = false
		});
	}

	private void Render3D(float dt)
	{
		updateFlySound(dt);
		if (!game.api.renderapi.WireframeDebugRender.AmbientSounds)
		{
			return;
		}
		int i = 0;
		foreach (AmbientSound key in ambientSounds.Keys)
		{
			key.RenderWireFrame(game, wireframes[i % wireframes.Length]);
			i++;
		}
	}

	private void updateFlySound(float dt)
	{
		bool nowActive = Math.Abs(flySpeed) - 0.05000000074505806 > 0.2;
		if (nowActive && !fallActive && !FlySound.IsPlaying)
		{
			FlySound.Start();
		}
		if (!nowActive && (double)curVolume < 0.08 && FlySound.IsPlaying)
		{
			FlySound.Stop();
		}
		if (FlySound.IsPlaying)
		{
			targetVolume = (nowActive ? Math.Min(1f, Math.Abs((float)flySpeed)) : 0f);
			curVolume = GameMath.Clamp(curVolume + (targetVolume - curVolume) * dt * (float)(nowActive ? 1 : 5), 0f, 1f);
			FlySound.SetVolume(curVolume);
		}
		fallActive = nowActive;
	}

	private void OnAmbientSoundScan(List<AmbientSound> newAmbientSounds)
	{
		HashSet<AmbientSound> soundsToFadeout = new HashSet<AmbientSound>(ambientSounds.Keys);
		foreach (AmbientSound newambsound in newAmbientSounds)
		{
			soundsToFadeout.Remove(newambsound);
			if (ambientSounds.ContainsKey(newambsound))
			{
				AmbientSound ambientSound = ambientSounds[newambsound];
				ambientSound.QuantityNearbyBlocks = newambsound.QuantityNearbyBlocks;
				ambientSound.BoundingBoxes = newambsound.BoundingBoxes;
				ambientSound.VolumeMul = newambsound.VolumeMul;
				ambientSound.FadeToNewVolumne();
				continue;
			}
			ambientSounds[newambsound] = newambsound;
			newambsound.Sound = game.LoadSound(new SoundParams
			{
				Location = newambsound.AssetLoc,
				ShouldLoop = true,
				RelativePosition = false,
				DisposeOnFinish = false,
				Volume = 0.01f,
				Position = new Vec3f(),
				Range = 40f,
				SoundType = newambsound.SoundType
			});
			newambsound.updatePosition(game.EntityPlayer.Pos);
			newambsound.Sound.Start();
			newambsound.Sound.PlaybackPosition = (float)game.Rand.NextDouble() * newambsound.Sound.SoundLengthSeconds;
			newambsound.FadeToNewVolumne();
		}
		foreach (AmbientSound ambsound in soundsToFadeout)
		{
			ambsound.Sound.FadeOut(1f, delegate(ILoadedSound loadedsound)
			{
				loadedsound.Stop();
				loadedsound.Dispose();
			});
			ambientSounds.Remove(ambsound);
		}
	}

	public void OnGameTick(float dt)
	{
		foreach (KeyValuePair<AmbientSound, AmbientSound> ambientSound in ambientSounds)
		{
			ambientSound.Value.updatePosition(game.EntityPlayer.Pos);
		}
	}

	internal int GetSoundCount(string[] soundwalk)
	{
		int count = 0;
		if (soundwalk == null)
		{
			return 0;
		}
		for (int i = 0; i < soundwalk.Length; i++)
		{
			if (soundwalk[i] != null)
			{
				count++;
			}
		}
		return count;
	}

	private void OnSwimDepthChange(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		bool nowActive = Math.Abs(newValues.EyesInWaterDepth) > 0f;
		if (nowActive && !underwaterActive)
		{
			UnderWaterSound.Start();
		}
		if (!nowActive && underwaterActive)
		{
			UnderWaterSound.Stop();
		}
		if (nowActive)
		{
			UnderWaterSound.SetVolume(Math.Min(0.1f, newValues.EyesInWaterDepth / 2f));
		}
		underwaterActive = nowActive;
	}

	private void OnFallSpeedChange(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		flySpeed = newValues.FallSpeed;
	}

	public override void Dispose(ClientMain game)
	{
		foreach (AmbientSound key in ambientSounds.Keys)
		{
			key.Sound?.Dispose();
		}
		FlySound?.Dispose();
		UnderWaterSound?.Dispose();
		if (wireframes != null)
		{
			WireframeCube[] array = wireframes;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Dispose();
			}
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
