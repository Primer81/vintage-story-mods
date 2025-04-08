using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorAttractsLightning : BlockEntityBehavior
{
	private class ConfigurationProperties
	{
		public float ArtificialElevation { get; set; } = 1f;


		public float ElevationAttractivenessMultiplier { get; set; } = 1f;

	}

	private ConfigurationProperties configProps;

	private bool registered;

	private WeatherSystemServer weatherSystem => Api.ModLoader.GetModSystem<WeatherSystemServer>();

	public BEBehaviorAttractsLightning(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		configProps = properties.AsObject<ConfigurationProperties>();
		if (Api.Side == EnumAppSide.Server && !registered)
		{
			weatherSystem.OnLightningImpactBegin += OnLightningStart;
			registered = true;
		}
	}

	public override void OnBlockPlaced(ItemStack byItemstack = null)
	{
		base.OnBlockPlaced();
		if (Api.Side == EnumAppSide.Server && !registered)
		{
			weatherSystem.OnLightningImpactBegin += OnLightningStart;
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side != EnumAppSide.Client)
		{
			weatherSystem.OnLightningImpactBegin -= OnLightningStart;
		}
	}

	private void OnLightningStart(ref Vec3d impactPos, ref EnumHandling handling)
	{
		IWorldAccessor world = Blockentity.Api.World;
		BlockPos ourPos = Blockentity.Pos;
		int ourRainHeight = world.BlockAccessor.GetRainMapHeightAt(ourPos.X, ourPos.Z);
		if (ourRainHeight == ourPos.Y)
		{
			int impactRainHeight = world.BlockAccessor.GetRainMapHeightAt((int)impactPos.X, (int)impactPos.Z);
			float yDiff = configProps.ArtificialElevation + (float)ourRainHeight - (float)impactRainHeight;
			yDiff = ((!(yDiff < 0f)) ? (yDiff * configProps.ElevationAttractivenessMultiplier) : (yDiff / configProps.ElevationAttractivenessMultiplier));
			yDiff = GameMath.Min(40f, yDiff);
			if (!(new Vec2d(Blockentity.Pos.X, Blockentity.Pos.Z).DistanceTo(impactPos.X, impactPos.Z) > (double)yDiff))
			{
				impactPos = Blockentity.Pos.ToVec3d();
			}
		}
	}
}
