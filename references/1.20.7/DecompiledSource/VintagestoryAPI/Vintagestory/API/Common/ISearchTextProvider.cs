namespace Vintagestory.API.Common;

public interface ISearchTextProvider
{
	string GetSearchText(IWorldAccessor world, ItemSlot inSlot);
}
