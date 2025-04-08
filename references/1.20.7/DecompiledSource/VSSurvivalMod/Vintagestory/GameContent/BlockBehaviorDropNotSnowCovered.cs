using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorDropNotSnowCovered : BlockBehavior
{
	public BlockBehaviorDropNotSnowCovered(Block block)
		: base(block)
	{
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
	{
		if (block.Variant["cover"] == "snow")
		{
			handling = EnumHandling.PreventDefault;
			return new ItemStack[1]
			{
				new ItemStack(world.GetBlock(block.CodeWithVariant("cover", "free")))
			};
		}
		return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		if (block.Variant["cover"] == "snow")
		{
			Block snowblock = world.GetBlock(new AssetLocation("snowblock"));
			if (snowblock != null)
			{
				world.SpawnCubeParticles(pos.ToVec3d().Add(0.5, 0.5, 0.5), new ItemStack(snowblock), 1f, 20, 1f, byPlayer);
			}
		}
	}
}
