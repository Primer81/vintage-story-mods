using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

public interface IOwnedInventory
{
	Entity Owner { get; }
}
