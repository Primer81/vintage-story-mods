using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class DressedCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private string Code;

	[JsonProperty]
	private string Slot;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "dressed";

	public DressedCondition()
	{
	}

	public DressedCondition(EntityActivitySystem vas, string code, string slot, bool invert = false)
	{
		this.vas = vas;
		Code = code;
		Slot = slot;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		if (!(vas.Entity is EntityDressedHumanoid edh))
		{
			return false;
		}
		int index = edh.OutfitSlots.IndexOf(Slot);
		if (index < 0)
		{
			return false;
		}
		return edh.OutfitCodes[index] == Code;
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 250.0, 25.0);
		singleComposer.AddStaticText("Slot", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "slot").AddStaticText("Accessory Code", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 15.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "code");
		singleComposer.GetTextInput("code").SetValue(Code);
		singleComposer.GetTextInput("slot").SetValue(Slot);
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Code = singleComposer.GetTextInput("code").GetText();
		Slot = singleComposer.GetTextInput("slot").GetText();
	}

	public IActionCondition Clone()
	{
		return new DressedCondition(vas, Code, Slot, Invert);
	}

	public override string ToString()
	{
		return string.Format(Invert ? "When not {0} dressed in slot {1}" : "When {0} dressed in slot {1}", Code, Slot);
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
