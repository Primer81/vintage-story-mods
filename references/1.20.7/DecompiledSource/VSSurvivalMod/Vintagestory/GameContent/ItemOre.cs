using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class ItemOre : ItemPileable
{
	public bool IsCoal
	{
		get
		{
			if (!(Variant["ore"] == "lignite") && !(Variant["ore"] == "bituminouscoal"))
			{
				return Variant["ore"] == "anthracite";
			}
			return true;
		}
	}

	public override bool IsPileable => IsCoal;

	protected override AssetLocation PileBlockCode => new AssetLocation("coalpile");

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		if (CombustibleProps?.SmeltedStack?.ResolvedItemstack == null)
		{
			JsonObject attributes = Attributes;
			if (attributes != null && attributes["metalUnits"].Exists)
			{
				float units = Attributes["metalUnits"].AsInt();
				string orename = LastCodePart(1);
				if (orename.Contains("_"))
				{
					orename = orename.Split('_')[1];
				}
				AssetLocation loc = new AssetLocation("nugget-" + orename);
				Item item = api.World.GetItem(loc);
				if (item?.CombustibleProps?.SmeltedStack?.ResolvedItemstack != null)
				{
					string metalname = item.CombustibleProps.SmeltedStack.ResolvedItemstack.GetName().Replace(" ingot", "");
					dsc.AppendLine(Lang.Get("{0} units of {1}", units.ToString("0.#"), metalname));
				}
				dsc.AppendLine(Lang.Get("Parent Material: {0}", Lang.Get("rock-" + LastCodePart())));
				dsc.AppendLine();
				dsc.AppendLine(Lang.Get("Crush with hammer to extract nuggets"));
			}
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		}
		else
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			if (CombustibleProps.SmeltedStack.ResolvedItemstack.GetName().Contains("ingot"))
			{
				string smelttype = CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
				int instacksize = CombustibleProps.SmeltedRatio;
				float units2 = (float)CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize * 100f / (float)instacksize;
				string metalname2 = CombustibleProps.SmeltedStack.ResolvedItemstack.GetName().Replace(" ingot", "");
				string str = Lang.Get("game:smeltdesc-" + smelttype + "ore-plural", units2.ToString("0.#"), metalname2);
				dsc.AppendLine(str);
			}
		}
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["metalUnits"].Exists)
		{
			string orename = LastCodePart(1);
			LastCodePart();
			if (FirstCodePart() == "crystalizedore")
			{
				return Lang.Get(LastCodePart(2) + "-crystallizedore-chunk", Lang.Get("ore-" + orename));
			}
			return Lang.Get(LastCodePart(2) + "-ore-chunk", Lang.Get("ore-" + orename));
		}
		return base.GetHeldItemName(itemStack);
	}
}
