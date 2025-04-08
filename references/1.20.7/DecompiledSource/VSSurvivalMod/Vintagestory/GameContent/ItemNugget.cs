using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class ItemNugget : Item
{
	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		ItemSlot oreSlot = allInputslots.FirstOrDefault((ItemSlot slot) => slot.Itemstack?.Collectible is ItemOre);
		if (oreSlot != null)
		{
			int units = oreSlot.Itemstack.ItemAttributes["metalUnits"].AsInt(5);
			string type = oreSlot.Itemstack.Collectible.Variant["ore"].Replace("quartz_", "").Replace("galena_", "");
			ItemStack outStack = new ItemStack(api.World.GetItem(new AssetLocation("nugget-" + type)));
			outStack.StackSize = Math.Max(1, units / 5);
			outputSlot.Itemstack = outStack;
		}
		base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		if (CombustibleProps?.SmeltedStack == null)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			return;
		}
		_ = CombustibleProps;
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string smelttype = CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
		int instacksize = CombustibleProps.SmeltedRatio;
		float units = (float)CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize * 100f / (float)instacksize;
		string metalname = CombustibleProps.SmeltedStack.ResolvedItemstack.GetName().Replace(" ingot", "");
		string str = Lang.Get("game:smeltdesc-" + smelttype + "ore-plural", units.ToString("0.#"), metalname);
		dsc.AppendLine(str);
	}
}
