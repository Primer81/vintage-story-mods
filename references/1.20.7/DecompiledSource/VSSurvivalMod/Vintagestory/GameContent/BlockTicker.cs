using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class BlockTicker : Block
{
	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntityTicker obj = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityTicker;
		if (obj != null && !obj.OnInteract(byPlayer))
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		return true;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "Set (requires Creative mode)",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right
			}
		};
	}
}
