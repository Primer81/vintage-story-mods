using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common;

public class CreativeTabs
{
	private int index;

	private OrderedDictionary<string, CreativeTab> tabsByCode = new OrderedDictionary<string, CreativeTab>();

	public OrderedDictionary<string, CreativeTab> TabsByCode => tabsByCode;

	public IEnumerable<CreativeTab> Tabs => tabsByCode.ValuesOrdered;

	public void Add(CreativeTab tab)
	{
		if (tab != null)
		{
			tabsByCode.Add(tab.Code, tab);
			tab.Index = index;
			index++;
		}
	}

	public CreativeTab GetTabByCode(string code)
	{
		return tabsByCode[code];
	}

	internal void CreateSearchCache(IWorldAccessor world)
	{
		foreach (KeyValuePair<string, CreativeTab> item in tabsByCode)
		{
			item.Value.CreateSearchCache(world);
		}
	}
}
