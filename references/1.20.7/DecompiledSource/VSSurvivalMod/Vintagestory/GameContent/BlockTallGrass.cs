using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockTallGrass : BlockPlant
{
	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		if (byPlayer != null && byPlayer.InventoryManager.ActiveTool == EnumTool.Knife && Variant["tallgrass"] != null && Variant["tallgrass"] != "eaten")
		{
			world.BlockAccessor.SetBlock(world.GetBlock(CodeWithVariant("tallgrass", "eaten")).Id, pos);
		}
	}
}
