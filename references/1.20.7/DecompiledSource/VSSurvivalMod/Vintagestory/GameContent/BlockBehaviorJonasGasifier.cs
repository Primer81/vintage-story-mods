using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBehaviorJonasGasifier : BlockBehavior
{
	private WorldInteraction[] interactions;

	public BlockBehaviorJonasGasifier(Block block)
		: base(block)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		BlockForge forgeBlock = api.World.GetBlock(new AssetLocation("forge")) as BlockForge;
		interactions = ObjectCacheUtil.GetOrCreate(api, "gasifierBlockInteractions", delegate
		{
			List<ItemStack> list = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: false);
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-coalpile-addcoal",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = forgeBlock.coalStacklist.ToArray()
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-forge-ignite",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BEBehaviorJonasGasifier bEBehaviorJonasGasifier = api.World.BlockAccessor.GetBlockEntity(bs.Position)?.GetBehavior<BEBehaviorJonasGasifier>();
						return (bEBehaviorJonasGasifier != null && bEBehaviorJonasGasifier.HasFuel && !bEBehaviorJonasGasifier.Lit) ? wi.Itemstacks : null;
					}
				}
			};
		});
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling));
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		block.GetBEBehavior<BEBehaviorJonasGasifier>(blockSel.Position)?.Interact(byPlayer, blockSel);
		handling = EnumHandling.PreventDefault;
		(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}
}
