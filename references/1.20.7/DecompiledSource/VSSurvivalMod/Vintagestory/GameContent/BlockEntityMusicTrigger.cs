using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityMusicTrigger : BlockEntity
{
	private ICoreClientAPI capi;

	public AssetLocation musicTrackLocation;

	public float priority = 4.5f;

	public float fadeInDuration = 0.1f;

	public Cuboidi[] areas;

	private MusicTrack track;

	private long startLoadingMs;

	private long handlerId;

	private bool nowFadingOut;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(onTick1s, 1001);
			capi = api as ICoreClientAPI;
		}
	}

	private void onTick1s(float dt)
	{
		if (!(musicTrackLocation == null) && areas != null)
		{
			int dx = (int)capi.World.Player.Entity.Pos.X - Pos.X;
			int dy = (int)capi.World.Player.Entity.Pos.Y - Pos.Y;
			int dz = (int)capi.World.Player.Entity.Pos.Z - Pos.Z;
			bool isInRange = false;
			Cuboidi[] array = areas;
			foreach (Cuboidi area in array)
			{
				isInRange |= area.ContainsOrTouches(dx, dy, dz);
			}
			if (isInRange)
			{
				StartMusic();
			}
			else
			{
				StopMusic();
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		if (tree["areas"] is TreeAttribute atree)
		{
			List<Cuboidi> cubs = new List<Cuboidi>();
			IAttribute[] values = atree.Values;
			foreach (IAttribute val in values)
			{
				cubs.Add(new Cuboidi((val as IntArrayAttribute).value));
			}
			areas = cubs.ToArray();
		}
		if (tree.HasAttribute("musicTrackLocation"))
		{
			musicTrackLocation = new AssetLocation(tree.GetString("musicTrackLocation"));
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		if (areas != null)
		{
			TreeAttribute atree = (TreeAttribute)(tree["areas"] = new TreeAttribute());
			for (int i = 0; i < areas.Length; i++)
			{
				atree[i.ToString() ?? ""] = new IntArrayAttribute(areas[i].Coordinates);
			}
		}
		if (musicTrackLocation != null)
		{
			tree.SetString("musicTrackLocation", musicTrackLocation.ToShortString());
		}
	}

	private void StartMusic()
	{
		if (capi == null || musicTrackLocation == null)
		{
			return;
		}
		if (nowFadingOut && track?.Sound != null)
		{
			track.Sound.FadeIn(1f, null);
			nowFadingOut = false;
			return;
		}
		MusicTrack musicTrack = track;
		if (musicTrack == null || !musicTrack.loading)
		{
			MusicTrack musicTrack2 = track;
			if (musicTrack2 == null || !(musicTrack2.Sound?.IsPlaying).GetValueOrDefault())
			{
				nowFadingOut = false;
				startLoadingMs = capi.World.ElapsedMilliseconds;
				track = capi.StartTrack(musicTrackLocation, 99f, EnumSoundType.Music, onTrackLoaded);
				track.ForceActive = true;
				track.Priority = priority;
			}
		}
	}

	private void StopMusic()
	{
		if (capi == null)
		{
			return;
		}
		if (handlerId != 0L)
		{
			capi.Event.UnregisterCallback(handlerId);
			return;
		}
		if (track?.Sound != null)
		{
			nowFadingOut = true;
		}
		track?.FadeOut(3f, delegate
		{
			nowFadingOut = false;
			MusicTrack musicTrack = track;
			if (musicTrack != null)
			{
				musicTrack.ForceActive = false;
			}
			track = null;
		});
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
			track.Sound.SetLooping(on: true);
			track.ManualDispose = true;
			long longMsPassed = capi.World.ElapsedMilliseconds - startLoadingMs;
			handlerId = capi.Event.RegisterCallback(delegate
			{
				if (!sound.IsDisposed)
				{
					sound.Start();
					sound.FadeIn(fadeInDuration, null);
					track.loading = false;
					handlerId = 0L;
				}
			}, (int)Math.Max(0L, 500 - longMsPassed), permittedWhilePaused: true);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		track?.Stop();
		track?.Sound?.Dispose();
	}
}
