using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BEBehaviorFirepitAmbient : BlockEntityBehavior
{
	protected ILoadedSound ambientSound;

	private static bool MusicActive;

	private static double MusicLastPlayedTotalHr = -99.0;

	private int counter;

	private MusicTrack track;

	private string trackstring = "music/safety-of-a-warm-fire.ogg";

	private IFirePit befirepit;

	private BlockEntity be;

	private bool fadingOut;

	private long startLoadingMs;

	private long handlerId;

	private bool wasStopped;

	public virtual float SoundLevel => 0.66f;

	private bool IsNight => (double)Api.World.Calendar.GetDayLightStrength(Blockentity.Pos.X, Blockentity.Pos.Z) < 0.4;

	private bool HasNearbySittingPlayer
	{
		get
		{
			IClientPlayer eplr = (Api as ICoreClientAPI).World.Player;
			if (eplr.Entity.Controls.FloorSitting)
			{
				return eplr.Entity.Pos.DistanceTo(Blockentity.Pos.ToVec3d().Add(0.5, 0.5, 0.5)) < 4.0;
			}
			return false;
		}
	}

	public BEBehaviorFirepitAmbient(BlockEntity blockentity)
		: base(blockentity)
	{
		be = blockentity;
		befirepit = blockentity as IFirePit;
	}

	public void ToggleAmbientSounds(bool on)
	{
		if (Api.Side != EnumAppSide.Client)
		{
			return;
		}
		if (on)
		{
			if (ambientSound == null || !ambientSound.IsPlaying)
			{
				ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/environment/fireplace.ogg"),
					ShouldLoop = true,
					Position = be.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
					DisposeOnFinish = false,
					Volume = SoundLevel
				});
				if (ambientSound != null)
				{
					ambientSound.Start();
					ambientSound.PlaybackPosition = ambientSound.SoundLengthSeconds * (float)Api.World.Rand.NextDouble();
				}
			}
		}
		else
		{
			ambientSound?.Stop();
			ambientSound?.Dispose();
			ambientSound = null;
		}
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (Blockentity.Api.Side == EnumAppSide.Client && befirepit != null)
		{
			Blockentity.RegisterGameTickListener(OnTick3s, 3000);
		}
	}

	private void OnTick3s(float dt)
	{
		if (MusicActive)
		{
			if (!fadingOut && track != null && track.IsActive && (!befirepit.IsBurning || !HasNearbySittingPlayer || !IsNight))
			{
				fadingOut = true;
				track.FadeOut(4f, delegate
				{
					StopMusic();
				});
			}
			return;
		}
		double nowHours = Api.World.Calendar.TotalHours;
		if (!(nowHours - MusicLastPlayedTotalHr < 6.0) && IsNight && befirepit.IsBurning)
		{
			if (HasNearbySittingPlayer)
			{
				counter++;
			}
			else
			{
				counter = 0;
			}
			if (counter > 3)
			{
				MusicActive = true;
				MusicLastPlayedTotalHr = nowHours;
				startLoadingMs = Api.World.ElapsedMilliseconds;
				track = (Api as ICoreClientAPI)?.StartTrack(new AssetLocation(trackstring), 120f, EnumSoundType.Music, onTrackLoaded);
			}
		}
	}

	private void onTrackLoaded(ILoadedSound sound)
	{
		if (track == null)
		{
			sound?.Dispose();
		}
		else
		{
			if (sound == null)
			{
				return;
			}
			track.Sound = sound;
			Api.Event.EnqueueMainThreadTask(delegate
			{
				track.loading = true;
			}, "settrackloading");
			long longMsPassed = Api.World.ElapsedMilliseconds - startLoadingMs;
			handlerId = Blockentity.RegisterDelayedCallback(delegate
			{
				if (sound.IsDisposed)
				{
					Api.World.Logger.Notification("firepit track is diposed? o.O");
				}
				if (!wasStopped)
				{
					sound.Start();
				}
				track.loading = false;
			}, (int)Math.Max(0L, 500 - longMsPassed));
		}
	}

	private void StopMusic()
	{
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			if (track != null && track.IsActive)
			{
				MusicActive = false;
			}
			track?.Stop();
			track = null;
			Api.Event.UnregisterCallback(handlerId);
			wasStopped = true;
			fadingOut = false;
		}
	}

	public override void OnBlockRemoved()
	{
		StopMusic();
		if (ambientSound != null)
		{
			ambientSound.Stop();
			ambientSound.Dispose();
		}
	}

	public override void OnBlockUnloaded()
	{
		StopMusic();
		ToggleAmbientSounds(on: false);
	}

	~BEBehaviorFirepitAmbient()
	{
		if (ambientSound != null)
		{
			ambientSound.Dispose();
		}
	}
}
