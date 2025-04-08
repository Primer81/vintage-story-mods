using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common;

public class CreativeTab
{
	public IInventory Inventory { get; set; }

	public string Code { get; set; }

	public Dictionary<int, string> SearchCache { get; set; }

	public Dictionary<int, string> SearchCacheNames { get; set; }

	public int Index { get; set; }

	public CreativeTab(string code, IInventory inventory)
	{
		Code = code;
		Inventory = inventory;
	}

	public Dictionary<int, string> CreateSearchCache(IWorldAccessor world)
	{
		Dictionary<int, string> searchCache = new Dictionary<int, string>();
		Dictionary<int, string> searchCacheNames = new Dictionary<int, string>();
		for (int slotID = 0; slotID < Inventory.Count; slotID++)
		{
			if (((ClientCoreAPI)world.Api).disposed)
			{
				break;
			}
			ItemSlot slot = Inventory[slotID];
			ItemStack stack = slot.Itemstack;
			if (stack != null)
			{
				string stackName = stack.GetName();
				searchCacheNames[slotID] = stackName.ToSearchFriendly().ToLowerInvariant();
				searchCache[slotID] = stackName + " " + ((stack.Collectible as ISearchTextProvider)?.GetSearchText(world, slot) ?? stack.GetDescription(world, slot).ToSearchFriendly().ToLowerInvariant());
			}
		}
		SearchCacheNames = searchCacheNames;
		SearchCache = searchCache;
		return SearchCache;
	}
}
