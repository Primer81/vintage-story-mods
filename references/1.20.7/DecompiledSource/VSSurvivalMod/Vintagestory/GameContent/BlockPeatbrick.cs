using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class BlockPeatbrick : Block
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		return false;
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		GetCollectibleBehavior<CollectibleBehaviorGroundStorable>(withInheritance: false).OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
	}
}
