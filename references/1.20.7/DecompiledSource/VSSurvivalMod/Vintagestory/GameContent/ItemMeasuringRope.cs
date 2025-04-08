using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class ItemMeasuringRope : Item
{
	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel != null)
		{
			ITreeAttribute attributes = slot.Itemstack.Attributes;
			attributes.SetDouble("startX", (double)blockSel.Position.X + blockSel.HitPosition.X);
			attributes.SetDouble("startY", (double)blockSel.Position.Y + blockSel.HitPosition.Y);
			attributes.SetDouble("startZ", (double)blockSel.Position.Z + blockSel.HitPosition.Z);
			attributes.SetInt("blockX", blockSel.Position.X);
			attributes.SetInt("blockY", blockSel.Position.Y);
			attributes.SetInt("blockZ", blockSel.Position.Z);
			handling = EnumHandHandling.PreventDefault;
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		slot.Itemstack.Attributes.RemoveAttribute("startX");
		slot.Itemstack.Attributes.RemoveAttribute("startY");
		slot.Itemstack.Attributes.RemoveAttribute("startZ");
		handling = EnumHandHandling.PreventDefault;
	}
}
