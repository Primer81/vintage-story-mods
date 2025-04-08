using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFruitPressTop : Block
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		PlacedPriorityInteract = true;
		interactions = ObjectCacheUtil.GetOrCreate(api, "fruitPressInteractionsTop", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				JsonObject attributes = current.Attributes;
				if (attributes != null && attributes["juiceableProperties"].Exists)
				{
					list.Add(new ItemStack(current));
				}
			}
			ItemStack[] jstacks = list.ToArray();
			return new WorldInteraction[5]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-fruitpress-press",
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position.DownCopy()) is BlockEntityFruitPress blockEntityFruitPress5 && bs.SelectionBoxIndex == 1 && blockEntityFruitPress5.CanScrew
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-fruitpress-release",
					HotKeyCode = "ctrl",
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position.DownCopy()) is BlockEntityFruitPress blockEntityFruitPress4 && bs.SelectionBoxIndex == 1 && blockEntityFruitPress4.CanUnscrew
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-fruitpress-fillremove",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = jstacks,
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position.DownCopy()) is BlockEntityFruitPress blockEntityFruitPress3 && bs.SelectionBoxIndex == 0 && blockEntityFruitPress3.CanFillRemoveItems) ? jstacks : null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-fruitpress-fillsingle",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = jstacks,
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position.DownCopy()) is BlockEntityFruitPress blockEntityFruitPress2 && bs.SelectionBoxIndex == 0 && blockEntityFruitPress2.CanFillRemoveItems) ? jstacks : null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-fruitpress-fillstack",
					HotKeyCode = "ctrl",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = jstacks,
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position.DownCopy()) is BlockEntityFruitPress blockEntityFruitPress && bs.SelectionBoxIndex == 0 && blockEntityFruitPress.CanFillRemoveItems) ? jstacks : null
				}
			};
		});
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (world.BlockAccessor.GetBlock(pos.DownCopy()) is BlockFruitPress block)
		{
			block.OnBlockBroken(world, pos.DownCopy(), byPlayer, dropQuantityMultiplier);
		}
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlock(pos.DownCopy()) is BlockFruitPress block)
		{
			return block.OnPickBlock(world, pos.DownCopy());
		}
		return base.OnPickBlock(world, pos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntityFruitPress be = world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) as BlockEntityFruitPress;
		BlockFruitPress block = world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()) as BlockFruitPress;
		if (be != null)
		{
			bool result = be.OnBlockInteractStart(byPlayer, blockSel, (blockSel.SelectionBoxIndex != 1) ? EnumFruitPressSection.MashContainer : EnumFruitPressSection.Screw, !block.RightMouseDown);
			block.RightMouseDown = true;
			return result;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) is BlockEntityFruitPress be)
		{
			return be.OnBlockInteractStep(secondsUsed, byPlayer, (blockSel.SelectionBoxIndex != 1) ? EnumFruitPressSection.MashContainer : EnumFruitPressSection.Screw);
		}
		return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) is BlockEntityFruitPress be)
		{
			be.OnBlockInteractStop(secondsUsed, byPlayer);
		}
		base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
	}

	public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) is BlockEntityFruitPress be)
		{
			return be.OnBlockInteractCancel(secondsUsed, byPlayer);
		}
		return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (world.BlockAccessor.GetBlock(pos.DownCopy()) is BlockFruitPress block)
		{
			return block.GetPlacedBlockInfo(world, pos.DownCopy(), forPlayer);
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}
}
