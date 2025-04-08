using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class VariableCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private EnumActivityVariableScope scope;

	[JsonProperty]
	public string Name;

	[JsonProperty]
	public string Comparison;

	[JsonProperty]
	public string Value;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "variable";

	public VariableCondition()
	{
	}

	public VariableCondition(EntityActivitySystem vas, EnumActivityVariableScope scope, string name, string value, string comparison, bool invert = false)
	{
		this.scope = scope;
		this.vas = vas;
		Name = name;
		Value = value;
		Comparison = comparison;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		string nowvalue = vas.Entity.Api.ModLoader.GetModSystem<VariablesModSystem>().GetVariable(scope, Name, e) ?? "";
		string testvalue = Value ?? "";
		if (nowvalue.Length == 0)
		{
			nowvalue = "0";
		}
		return Comparison switch
		{
			">" => nowvalue.ToDouble() > testvalue.ToDouble(), 
			"<" => nowvalue.ToDouble() < testvalue.ToDouble(), 
			"==" => nowvalue.Equals(testvalue), 
			_ => false, 
		};
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		string[] comps = new string[3] { ">", "<", "==" };
		string[] names = new string[3] { "&gt;", "&lt;", "==" };
		string[] scope = new string[3] { "entity", "group", "global" };
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Variable name", CairoFont.WhiteDetailText(), b).AddDropDown(scope, scope, (int)this.scope, null, b = b.BelowCopy(0.0, -5.0).WithFixedWidth(80.0), CairoFont.WhiteDetailText(), "scope").AddTextInput(b.RightCopy(5.0).WithFixedWidth(170.0), null, CairoFont.WhiteDetailText(), "name")
			.AddStaticText("Comparison", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddDropDown(comps, names, Math.Max(0, comps.IndexOf(Comparison)), null, b = b.BelowCopy(0.0, -5.0), CairoFont.WhiteDetailText(), "comparison")
			.AddStaticText("Value", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "value");
		singleComposer.GetTextInput("name").SetValue(Name);
		singleComposer.GetTextInput("value").SetValue(Value);
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		scope = (EnumActivityVariableScope)singleComposer.GetDropDown("scope").SelectedIndices[0];
		Comparison = singleComposer.GetDropDown("comparison").SelectedValue;
		Name = singleComposer.GetTextInput("name").GetText();
		Value = singleComposer.GetTextInput("value").GetText();
	}

	public IActionCondition Clone()
	{
		return new VariableCondition(vas, scope, Name, Value, Comparison, Invert);
	}

	public override string ToString()
	{
		return Comparison switch
		{
			">" => string.Format("When variable {0} {1} {2}", scope.ToString() + "." + Name, Invert ? "&lt;=" : "&gt;", Value), 
			"<" => string.Format("When variable {0} {1} {2}", scope.ToString() + "." + Name, Invert ? "&gt;=" : "&lt;", Value), 
			"==" => string.Format("When variable {0} {1} {2}", scope.ToString() + "." + Name, Invert ? "!=" : "==", Value), 
			_ => "unknown", 
		};
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
