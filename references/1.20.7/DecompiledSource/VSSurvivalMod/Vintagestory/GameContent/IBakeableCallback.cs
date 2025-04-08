using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IBakeableCallback
{
	void OnBaked(ItemStack oldStack, ItemStack newStack);
}
