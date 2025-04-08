using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface ICollectibleResolveOnLoad
{
	void ResolveOnLoad(ItemSlot slot, IWorldAccessor worldForResolve, bool resolveImports);
}
