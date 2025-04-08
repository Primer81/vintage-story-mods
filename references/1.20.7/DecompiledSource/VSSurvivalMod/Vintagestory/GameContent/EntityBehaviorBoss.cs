using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorBoss : EntityBehavior
{
	private AssetLocation musicTrackLocation;

	private bool playtrack;

	private MusicTrack track;

	private long startLoadingMs;

	private long handlerId;

	private bool wasStopped;

	public bool ShowHealthBar => entity.WatchedAttributes.GetBool("showHealthbar", defaultValue: true);

	private ICoreClientAPI capi => entity.World.Api as ICoreClientAPI;

	public bool ShouldPlayTrack
	{
		get
		{
			return playtrack;
		}
		set
		{
			if (playtrack != value)
			{
				if (value)
				{
					StartMusic();
				}
				else
				{
					StopMusic();
				}
			}
			playtrack = value;
		}
	}

	public float BossHpbarRange { get; set; }

	public EntityBehaviorBoss(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		if (attributes["musicTrack"].Exists)
		{
			musicTrackLocation = AssetLocation.Create(attributes["musicTrack"].AsString(), entity.Code.Domain);
		}
		BossHpbarRange = attributes["bossHpBarRange"].AsFloat(30f);
	}

	public override string PropertyName()
	{
		return "boss";
	}

	public override void OnGameTick(float deltaTime)
	{
		base.OnGameTick(deltaTime);
	}

	private void StartMusic()
	{
		if (capi != null && !(musicTrackLocation == null))
		{
			MusicTrack musicTrack = track;
			if (musicTrack == null || !musicTrack.IsActive)
			{
				startLoadingMs = capi.World.ElapsedMilliseconds;
				track = capi.StartTrack(musicTrackLocation, 99f, EnumSoundType.MusicGlitchunaffected, onTrackLoaded);
				track.Priority = 5f;
				wasStopped = false;
			}
		}
	}

	private void StopMusic()
	{
		if (capi != null)
		{
			track?.FadeOut(3f);
			track = null;
			capi.Event.UnregisterCallback(handlerId);
			wasStopped = true;
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
			sound.SetLooping(on: true);
			track.Sound = sound;
			track.ManualDispose = true;
			long longMsPassed = capi.World.ElapsedMilliseconds - startLoadingMs;
			handlerId = capi.Event.RegisterCallback(delegate
			{
				if (!sound.IsDisposed)
				{
					if (!wasStopped)
					{
						sound.Start();
						sound.FadeIn(2f, null);
					}
					track.loading = false;
				}
			}, (int)Math.Max(0L, 500 - longMsPassed));
		}
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		track?.Stop();
		track?.Sound?.Dispose();
	}
}
