namespace Vintagestory.API.Common;

public interface IMaterialExchangeable
{
	bool ExchangeWith(ItemSlot fromSlot, ItemSlot toSlot);
}
