using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockClayForm : Block
{
	private WorldInteraction[] interactions;

	private Cuboidf box = new Cuboidf(0f, 0f, 0f, 1f, 0.0625f, 1f);

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "clayformBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (current is ItemClay)
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[3]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-clayform-addclay",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = getMatchingStacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-clayform-removeclay",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Left,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = getMatchingStacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-selecttoolmode",
					HotKeyCode = "toolmodeselect",
					MouseButton = EnumMouseButton.None,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = getMatchingStacks
				}
			};
		});
	}

	private ItemStack[] getMatchingStacks(WorldInteraction wi, BlockSelection bs, EntitySelection es)
	{
		BlockEntityClayForm bec = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityClayForm;
		List<ItemStack> stacks = new List<ItemStack>();
		ItemStack[] itemstacks = wi.Itemstacks;
		foreach (ItemStack val in itemstacks)
		{
			if (bec?.BaseMaterial != null && bec.BaseMaterial.Collectible.LastCodePart() == val.Collectible.LastCodePart())
			{
				stacks.Add(val);
			}
		}
		return stacks.ToArray();
	}

	public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
	{
		return box;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityClayForm bea)
		{
			return bea.GetSelectionBoxes(blockAccessor, pos);
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return GetSelectionBoxes(blockAccessor, pos);
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[0];
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[0];
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		pos.Y--;
		if (!world.BlockAccessor.GetMostSolidBlock(pos).CanAttachBlockAt(world.BlockAccessor, this, pos, BlockFacing.UP))
		{
			pos.Y++;
			world.BlockAccessor.BreakBlock(pos, null);
		}
		else
		{
			pos.Y++;
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
