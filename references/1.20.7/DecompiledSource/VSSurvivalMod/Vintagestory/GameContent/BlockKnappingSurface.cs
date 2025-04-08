using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockKnappingSurface : Block
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
		interactions = ObjectCacheUtil.GetOrCreate(api, "knappingBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				JsonObject attributes = current.Attributes;
				if (attributes != null && attributes.IsTrue("knappable"))
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-knappingsurface-knap",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Left,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	internal virtual bool HasSolidGround(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return blockAccessor.GetBlock(pos.DownCopy()).CanAttachBlockAt(blockAccessor, this, pos.DownCopy(), BlockFacing.UP);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!HasSolidGround(world.BlockAccessor, blockSel.Position))
		{
			failureCode = "requiresolidground";
			return false;
		}
		return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
	}

	public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
	{
		return box;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityKnappingSurface bea)
		{
			return bea.GetSelectionBoxes(blockAccessor, pos);
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
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
