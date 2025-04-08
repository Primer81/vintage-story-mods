using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ModSystemBoatingSound : ModSystem
{
	public ILoadedSound travelSound;

	public ILoadedSound idleSound;

	private ICoreClientAPI capi;

	private bool soundsActive;

	private float accum;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		capi.Event.LevelFinalize += Event_LevelFinalize;
		capi.Event.RegisterGameTickListener(onTick, 0, 123);
	}

	private void onTick(float dt)
	{
		if (capi.World.Player.Entity.MountedOn is EntityBoatSeat eboatseat)
		{
			NowInMotion((float)eboatseat.Entity.Pos.Motion.Length(), dt);
		}
		else
		{
			NotMounted();
		}
	}

	private void Event_LevelFinalize()
	{
		travelSound = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/raft-moving.ogg"),
			ShouldLoop = true,
			RelativePosition = false,
			DisposeOnFinish = false,
			Volume = 0f
		});
		idleSound = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/raft-idle.ogg"),
			ShouldLoop = true,
			RelativePosition = false,
			DisposeOnFinish = false,
			Volume = 0.35f
		});
	}

	public void NowInMotion(float velocity, float dt)
	{
		accum += dt;
		if ((double)accum < 0.2)
		{
			return;
		}
		accum = 0f;
		if (!soundsActive)
		{
			idleSound.Start();
			soundsActive = true;
		}
		if ((double)velocity > 0.01)
		{
			if (!travelSound.IsPlaying)
			{
				travelSound.Start();
			}
			float volume = GameMath.Clamp((velocity - 0.025f) * 7f, 0f, 1f);
			travelSound.FadeTo(volume, 0.5f, null);
		}
		else if (travelSound.IsPlaying)
		{
			travelSound.FadeTo(0.0, 0.5f, delegate
			{
				travelSound.Stop();
			});
		}
	}

	public override void Dispose()
	{
		travelSound?.Dispose();
		idleSound?.Dispose();
	}

	public void NotMounted()
	{
		if (soundsActive)
		{
			idleSound.Stop();
			travelSound.SetVolume(0f);
			travelSound.Stop();
		}
		soundsActive = false;
	}
}
