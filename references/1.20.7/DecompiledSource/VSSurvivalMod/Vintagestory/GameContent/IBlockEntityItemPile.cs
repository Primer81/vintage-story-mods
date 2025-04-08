using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IBlockEntityItemPile
{
	bool OnPlayerInteract(IPlayer byPlayer);
}
