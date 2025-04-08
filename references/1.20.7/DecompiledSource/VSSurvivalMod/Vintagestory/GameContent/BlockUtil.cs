using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public static class BlockUtil
{
	public static ItemStack[] GetKnifeStacks(ICoreAPI api)
	{
		return ObjectCacheUtil.GetOrCreate(api, "knifeStacks", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Item current in api.World.Items)
			{
				if (current.Tool == EnumTool.Knife)
				{
					list.Add(new ItemStack((CollectibleObject)current, 1));
				}
			}
			return list.ToArray();
		});
	}
}
