using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IContainedCustomName
{
	string GetContainedInfo(ItemSlot inSlot);

	string GetContainedName(ItemSlot inSlot, int quantity);
}
