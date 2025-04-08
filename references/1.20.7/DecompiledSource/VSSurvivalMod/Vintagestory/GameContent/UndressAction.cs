using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class UndressAction : EntityActionBase
{
	[JsonProperty]
	private string Slot;

	public override string Type => "undress";

	public UndressAction()
	{
	}

	public UndressAction(EntityActivitySystem vas, string slot)
	{
		base.vas = vas;
		Slot = slot;
	}

	public override void Start(EntityActivity act)
	{
		if (vas.Entity is EntityDressedHumanoid edh)
		{
			int index = edh.OutfitSlots.IndexOf(Slot);
			if (index >= 0)
			{
				edh.OutfitCodes = edh.OutfitCodes.RemoveAt(index);
				edh.OutfitSlots = edh.OutfitSlots.RemoveAt(index);
			}
		}
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("From Slot", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "slot");
		singleComposer.GetTextInput("slot").SetValue(Slot);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Slot = singleComposer.GetTextInput("slot").GetText();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new UndressAction(vas, Slot);
	}

	public override string ToString()
	{
		return "Remove outfit in slot " + Slot;
	}
}
