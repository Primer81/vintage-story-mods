using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityEchoChamber : EntityAgent
{
	private ILoadedSound echoChamberSound1;

	private ILoadedSound echoChamberSound2;

	private ILoadedSound echoChamberSound3;

	private ICoreClientAPI capi;

	private double accum;

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		if (api is ICoreClientAPI capi)
		{
			this.capi = capi;
			echoChamberSound1 = capi.World.LoadSound(new SoundParams
			{
				DisposeOnFinish = false,
				Location = new AssetLocation("sounds/effect/echochamber.ogg"),
				Position = Pos.XYZ.ToVec3f().Add(0f, 0f, 0f),
				RelativePosition = false,
				ShouldLoop = true,
				SoundType = EnumSoundType.Ambient,
				Volume = 1f,
				Range = 60f
			});
			echoChamberSound1.Start();
			echoChamberSound2 = capi.World.LoadSound(new SoundParams
			{
				DisposeOnFinish = false,
				Location = new AssetLocation("sounds/effect/echochamber.ogg"),
				Position = Pos.XYZ.ToVec3f().Add(0f, 30f, 0f),
				RelativePosition = false,
				ShouldLoop = true,
				SoundType = EnumSoundType.Ambient,
				Volume = 0f,
				ReferenceDistance = 28f,
				Range = 200f
			});
			echoChamberSound2.Start();
			echoChamberSound3 = capi.World.LoadSound(new SoundParams
			{
				DisposeOnFinish = false,
				Location = new AssetLocation("sounds/effect/echochamber2.ogg"),
				Position = Pos.XYZ.ToVec3f().Add(0f, 60f, 0f),
				RelativePosition = false,
				ShouldLoop = true,
				SoundType = EnumSoundType.Ambient,
				Volume = 0f,
				ReferenceDistance = 28f,
				Range = 200f
			});
			echoChamberSound3.Start();
		}
	}

	public override void OnGameTick(float dt)
	{
		accum += dt;
		if (capi != null && accum > 2.0)
		{
			accum = 0.0;
			double dist2player = capi.World.Player.Entity.Pos.HorDistanceTo(Pos) - 35.0;
			double volume = GameMath.Clamp((20.0 - dist2player) / 20.0, 0.0, 1.0);
			echoChamberSound2.FadeTo(volume, 2f, null);
			echoChamberSound3.FadeTo(volume, 2f, null);
		}
		base.OnGameTick(dt);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		echoChamberSound1?.Dispose();
		echoChamberSound2?.Dispose();
		echoChamberSound3?.Dispose();
		base.OnEntityDespawn(despawn);
	}
}
