using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class DressAction : EntityActionBase
{
	[JsonProperty]
	private string Code;

	[JsonProperty]
	private string Slot;

	public override string Type => "dress";

	public DressAction()
	{
	}

	public DressAction(EntityActivitySystem vas, string code, string slot)
	{
		base.vas = vas;
		Code = code;
		Slot = slot;
	}

	public override bool IsFinished()
	{
		return true;
	}

	public override void Start(EntityActivity act)
	{
		if (vas.Entity is EntityDressedHumanoid edh)
		{
			int index = edh.OutfitSlots.IndexOf(Slot);
			if (index < 0)
			{
				edh.OutfitCodes = edh.OutfitCodes.Append(Code);
				edh.OutfitSlots = edh.OutfitSlots.Append(Slot);
			}
			else
			{
				edh.OutfitCodes[index] = Code;
				edh.WatchedAttributes.MarkPathDirty("outfitcodes");
			}
		}
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Slot", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "slot").AddStaticText("Outfit code", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 25.0).WithFixedWidth(300.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "code");
		singleComposer.GetTextInput("slot").SetValue(Slot);
		singleComposer.GetTextInput("code").SetValue(Code);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Slot = singleComposer.GetTextInput("slot").GetText();
		Code = singleComposer.GetTextInput("code").GetText();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new DressAction(vas, Code, Slot);
	}

	public override string ToString()
	{
		return "Dress outfit " + Code + " in slot" + Slot;
	}
}
