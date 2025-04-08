using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class SetVarAction : EntityActionBase
{
	[JsonProperty]
	private EnumActivityVariableScope scope;

	[JsonProperty]
	private string op;

	[JsonProperty]
	private string name;

	[JsonProperty]
	private string value;

	public override string Type => "setvariable";

	public SetVarAction()
	{
	}

	public SetVarAction(EntityActivitySystem vas, EnumActivityVariableScope scope, string op, string name, string value)
	{
		base.vas = vas;
		this.op = op;
		this.scope = scope;
		this.name = name;
		this.value = value;
	}

	public override void Start(EntityActivity act)
	{
		VariablesModSystem avs = vas.Entity.Api.ModLoader.GetModSystem<VariablesModSystem>();
		switch (op)
		{
		case "set":
			avs.SetVariable(vas.Entity, scope, name, value);
			break;
		case "incrementby":
		case "decrementby":
		{
			string curvalue = avs.GetVariable(scope, name, vas.Entity);
			int sign = ((!(op == "decrementby")) ? 1 : (-1));
			avs.SetVariable(vas.Entity, scope, name, (curvalue.ToDouble() + (double)sign * value.ToDouble()).ToString() ?? "");
			break;
		}
		}
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		string[] scope = new string[3] { "entity", "group", "global" };
		string[] ops = new string[3] { "set", "incrementby", "decrementby" };
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0);
		singleComposer.AddStaticText("Variable Scope", CairoFont.WhiteDetailText(), b).AddDropDown(scope, scope, (int)this.scope, null, b = b.BelowCopy(0.0, -5.0), CairoFont.WhiteDetailText(), "scope").AddStaticText("Operation", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 15.0))
			.AddDropDown(ops, ops, Math.Max(0, ops.IndexOf(op)), null, b = b.BelowCopy(0.0, -5.0), CairoFont.WhiteDetailText(), "op")
			.AddStaticText("Name", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 15.0).WithFixedWidth(150.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "name")
			.AddStaticText("Value", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 15.0).WithFixedWidth(150.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "value");
		singleComposer.GetTextInput("name").SetValue(name);
		singleComposer.GetTextInput("value").SetValue(value);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		scope = (EnumActivityVariableScope)singleComposer.GetDropDown("scope").SelectedIndices[0];
		op = singleComposer.GetDropDown("op").SelectedValue;
		name = singleComposer.GetTextInput("name").GetText();
		value = singleComposer.GetTextInput("value").GetText();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new SetVarAction(vas, scope, op, name, value);
	}

	public override string ToString()
	{
		(vas?.Entity.Api.ModLoader.GetModSystem<VariablesModSystem>())?.GetVariable(scope, name, vas.Entity);
		if (op == "incrementby" || op == "decrementby")
		{
			return string.Format("{3} {0} variable {1} by {2}", scope, name, value, (op == "incrementby") ? "Increment" : "Decrement");
		}
		return $"Set {scope} variable {name} to {value}";
	}
}
