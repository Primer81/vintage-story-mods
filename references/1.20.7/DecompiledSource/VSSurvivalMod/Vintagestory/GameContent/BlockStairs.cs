using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockStairs : Block
{
	private bool hasDownVariant = true;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		JsonObject attributes = Attributes;
		if (attributes != null && attributes.IsTrue("noDownVariant"))
		{
			hasDownVariant = false;
		}
	}

	public BlockFacing GetHorizontalFacing()
	{
		return BlockFacing.FromCode(Code.Path.Split('-')[^1]);
	}

	public BlockFacing GetVerticalFacing()
	{
		return BlockFacing.FromCode(Code.Path.Split('-')[^2]);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
		if (blockSel.Face.IsVertical)
		{
			horVer[1] = blockSel.Face;
		}
		else
		{
			horVer[1] = ((blockSel.HitPosition.Y < 0.5 || !hasDownVariant) ? BlockFacing.UP : BlockFacing.DOWN);
		}
		AssetLocation blockCode = CodeWithVariants(new string[2] { "verticalorientation", "horizontalorientation" }, new string[2]
		{
			horVer[1].Code,
			horVer[0].Code
		});
		Block block = world.BlockAccessor.GetBlock(blockCode);
		if (block == null)
		{
			return false;
		}
		world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
		return true;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[1]
		{
			new BlockDropItemStack(handbookStack)
		};
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[3] { "verticalorientation", "horizontalorientation", "cover" }, new string[3] { "up", "north", "free" }));
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariants(new string[3] { "verticalorientation", "horizontalorientation", "cover" }, new string[3] { "up", "north", "free" })));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		BlockFacing newFacing = BlockFacing.HORIZONTALS_ANGLEORDER[((360 - angle) / 90 + BlockFacing.FromCode(Variant["horizontalorientation"]).HorizontalAngleIndex) % 4];
		return CodeWithVariant("horizontalorientation", newFacing.Code);
	}

	public override AssetLocation GetVerticallyFlippedBlockCode()
	{
		if (!(Variant["verticalorientation"] == "up") || !hasDownVariant)
		{
			return CodeWithVariant("verticalorientation", "up");
		}
		return CodeWithVariant("verticalorientation", "down");
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		BlockFacing facing = BlockFacing.FromCode(Variant["horizontalorientation"]);
		if (facing.Axis == axis)
		{
			return CodeWithVariant("horizontalorientation", facing.Opposite.Code);
		}
		return Code;
	}
}
