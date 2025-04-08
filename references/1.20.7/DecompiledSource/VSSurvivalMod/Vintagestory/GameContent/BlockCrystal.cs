using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCrystal : Block
{
	private Block[] _facingBlocks;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		_facingBlocks = new Block[6];
		for (int i = 0; i < 6; i++)
		{
			_facingBlocks[i] = api.World.GetBlock(CodeWithPart(BlockFacing.ALLFACES[i].Code, 2));
		}
	}

	public Block FacingCrystal(IBlockAccessor blockAccessor, BlockFacing facing)
	{
		return blockAccessor.GetBlock(CodeWithPart(facing.Code));
	}

	public override double ExplosionDropChance(IWorldAccessor world, BlockPos pos, EnumBlastType blastType)
	{
		return 0.2;
	}

	public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
	{
		if (world.Rand.NextDouble() < 0.25)
		{
			ItemStack stack = new ItemStack(api.World.GetBlock(CodeWithVariant("position", "up")));
			stack.StackSize = 1;
			world.SpawnItemEntity(stack, pos);
		}
		else
		{
			int startquantity = 3;
			if (Variant["variant"] == "cluster1" || Variant["variant"] == "cluster2")
			{
				startquantity = 5;
			}
			if (Variant["variant"] == "large1" || Variant["variant"] == "large2")
			{
				startquantity = 7;
			}
			int quantity = (int)((double)startquantity * Math.Min(1.0, world.Rand.NextDouble() * 0.3100000023841858 + 0.699999988079071));
			string text = Variant["type"];
			string text2 = ((text == "milkyquartz") ? "clearquartz" : ((!(text == "olivine")) ? Variant["type"] : "ore-olivine"));
			string type = text2;
			ItemStack stack2 = new ItemStack(api.World.GetItem(new AssetLocation(type)));
			for (int i = 0; i < quantity; i++)
			{
				ItemStack drop = stack2.Clone();
				drop.StackSize = 1;
				world.SpawnItemEntity(drop, pos);
			}
		}
		world.BulkBlockAccessor.SetBlock(0, pos);
	}

	public override double GetBlastResistance(IWorldAccessor world, BlockPos pos, Vec3f blastDirectionVector, EnumBlastType blastType)
	{
		return 0.5;
	}
}
