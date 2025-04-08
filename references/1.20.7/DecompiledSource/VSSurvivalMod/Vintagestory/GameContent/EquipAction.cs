using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class EquipAction : EntityActionBase
{
	[JsonProperty]
	private string Target;

	[JsonProperty]
	private string Value;

	public override string Type => "equip";

	public EquipAction()
	{
	}

	public EquipAction(EntityActivitySystem vas, string target, string value)
	{
		base.vas = vas;
		Target = target;
		Value = value;
	}

	public override void Start(EntityActivity act)
	{
		string target = Target;
		if (target == "righthand" || target == "lefthand")
		{
			JsonItemStack jstack = JsonItemStack.FromString(Value);
			if (jstack.Resolve(vas.Entity.World, string.Concat(vas.Entity.Code, " entity activity system, equip action - could not resolve ", Value, ". Will ignore.")))
			{
				ItemSlot obj = ((Target == "righthand") ? vas.Entity.RightHandItemSlot : vas.Entity.LeftHandItemSlot);
				obj.Itemstack = jstack.ResolvedItemstack;
				obj.MarkDirty();
				vas.Entity.GetBehavior<EntityBehaviorContainer>().storeInv();
			}
		}
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		string[] vals = new string[2] { "lefthand", "righthand" };
		string[] cclass = new string[2] { "item", "block" };
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Target", CairoFont.WhiteDetailText(), b).AddDropDown(vals, vals, vals.IndexOf(Target), null, b.BelowCopy(0.0, -5.0), "target").AddStaticText("Class", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 25.0))
			.AddDropDown(cclass, cclass, vals.IndexOf(Target), null, b.BelowCopy(0.0, -5.0), "cclass")
			.AddStaticText("Block/Item code", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 25.0).WithFixedWidth(300.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "code")
			.AddStaticText("Attributes", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 25.0).WithFixedWidth(300.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "attr");
		if (Value != null && Value.Length > 0)
		{
			JsonItemStack jstack = JsonItemStack.FromString(Value);
			singleComposer.GetDropDown("cclass").SetSelectedIndex(cclass.IndexOf<string>(jstack.Type.ToString().ToLowerInvariant()));
			singleComposer.GetTextInput("code").SetValue(jstack.Code.ToShortString());
			singleComposer.GetTextInput("attr").SetValue(jstack.Attributes?.ToString() ?? "");
		}
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		string type = singleComposer.GetDropDown("cclass").SelectedValue;
		string code = singleComposer.GetTextInput("code").GetText();
		string attr = singleComposer.GetTextInput("attr").GetText();
		if (attr.Length > 0)
		{
			Value = $"{{ type: \"{type}\", code: \"{code}\", attributes: {attr} }}";
		}
		else
		{
			Value = $"{{ type: \"{type}\", code: \"{code}\" }}";
		}
		Target = singleComposer.GetDropDown("target").SelectedValue;
		try
		{
			if (!JsonItemStack.FromString(Value).Resolve(capi.World, "Entity activity system, equip action - could not resolve " + Value + ". Will ignore."))
			{
				capi.TriggerIngameError(this, "cantresolve", "Can't save. Unable to resolve json stack " + Value + ".");
				return false;
			}
		}
		catch
		{
			capi.TriggerIngameError(this, "cantresolve", "Can't save. Not valid json stack " + Value + " - an exception was thrown.");
			return false;
		}
		return true;
	}

	public override IEntityAction Clone()
	{
		return new EquipAction(vas, Target, Value);
	}

	public override string ToString()
	{
		return "Grab " + Value + " in " + Target;
	}
}
