using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPBrake : BEBehaviorMPAxle
{
	private BEBrake bebrake;

	private float resistance;

	private ILoadedSound brakeSound;

	public override CompositeShape Shape
	{
		get
		{
			string side = base.Block.Variant["side"];
			CompositeShape shape = new CompositeShape
			{
				Base = new AssetLocation("shapes/block/wood/mechanics/axle.json")
			};
			if (side == "east" || side == "west")
			{
				shape.rotateY = 90f;
			}
			return shape;
		}
		set
		{
		}
	}

	protected override bool AddStands => false;

	public BEBehaviorMPBrake(BlockEntity blockentity)
		: base(blockentity)
	{
		bebrake = blockentity as BEBrake;
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		bebrake.RegisterGameTickListener(OnEvery50Ms, 100);
		switch (base.Block.Variant["side"])
		{
		case "north":
		case "south":
			AxisSign = new int[3] { -1, 0, 0 };
			break;
		case "east":
		case "west":
			AxisSign = new int[3] { 0, 0, -1 };
			break;
		}
	}

	private void OnEvery50Ms(float dt)
	{
		resistance = GameMath.Clamp(resistance + dt / (float)(bebrake.Engaged ? 20 : (-10)), 0f, 3f);
		if (bebrake.Engaged && network != null && (double)network.Speed > 0.1)
		{
			Api.World.SpawnParticles(network.Speed * 1.7f, ColorUtil.ColorFromRgba(60, 60, 60, 100), Position.ToVec3d().Add(0.10000000149011612, 0.5, 0.10000000149011612), Position.ToVec3d().Add(0.800000011920929, 0.30000001192092896, 0.800000011920929), new Vec3f(-0.1f, 0.1f, -0.1f), new Vec3f(0.2f, 0.2f, 0.2f), 2f, 0f, 0.3f);
		}
		UpdateBreakSounds();
	}

	public void UpdateBreakSounds()
	{
		if (Api.Side != EnumAppSide.Client)
		{
			return;
		}
		if (resistance > 0f && bebrake.Engaged && network != null && (double)network.Speed > 0.1)
		{
			if (brakeSound == null || !brakeSound.IsPlaying)
			{
				brakeSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/effect/woodgrind.ogg"),
					ShouldLoop = true,
					Position = Position.ToVec3f().Add(0.5f, 0.25f, 0.5f),
					DisposeOnFinish = false,
					Volume = 1f
				});
				brakeSound.Start();
			}
			brakeSound.SetPitch(GameMath.Clamp(network.Speed * 1.5f + 0.2f, 0.5f, 1f));
		}
		else
		{
			brakeSound?.FadeOut(1f, delegate
			{
				brakeSound.Stop();
			});
		}
	}

	public override float GetResistance()
	{
		return resistance;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		return base.OnTesselation(mesher, tesselator);
	}
}
