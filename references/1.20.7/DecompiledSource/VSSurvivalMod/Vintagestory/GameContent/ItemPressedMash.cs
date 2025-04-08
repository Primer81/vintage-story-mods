using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ItemPressedMash : Item
{
	public override string GetHeldItemName(ItemStack itemStack)
	{
		string ap = (((float)Math.Round(itemStack.Attributes.GetDecimal("juiceableLitresLeft"), 2) > 0f) ? "wet" : "dry");
		string type = ItemClass.Name();
		return Lang.GetMatching(Code?.Domain + ":" + type + "-" + Code?.Path + "-" + ap);
	}

	public override ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props)
	{
		float pressedDryRatio = slot.Itemstack.ItemAttributes["juiceableProperties"]["pressedDryRatio"].AsFloat(1f);
		double juiceableLitresTotal = slot.Itemstack.Attributes.GetDouble("juiceableLitresLeft") + slot.Itemstack.Attributes.GetDouble("juiceableLitresTransfered");
		TransitionableProperties nProps = props.Clone();
		if (juiceableLitresTotal > 0.0)
		{
			nProps.TransitionRatio = props.TransitionRatio * (float)(int)((float)GameMath.RoundRandom(api.World.Rand, (float)juiceableLitresTotal) * pressedDryRatio);
		}
		return base.OnTransitionNow(slot, nProps);
	}
}
