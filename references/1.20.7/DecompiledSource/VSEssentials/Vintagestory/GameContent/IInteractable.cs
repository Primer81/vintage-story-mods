using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IInteractable
{
	bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling);
}
