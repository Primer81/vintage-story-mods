using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class MonthCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private string month;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "month";

	public MonthCondition()
	{
	}

	public MonthCondition(EntityActivitySystem vas, string month, bool invert = false)
	{
		this.vas = vas;
		this.month = month;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		ICoreAPI api = vas.Entity.Api;
		if (month.Contains(","))
		{
			string[] array = month.Split(",");
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].ToInt() == api.World.Calendar.Month)
				{
					return true;
				}
			}
			return false;
		}
		return api.World.Calendar.Month == month.ToInt();
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Month (1..12)", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "month");
		singleComposer.GetTextInput("month").SetValue(month);
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		month = singleComposer.GetTextInput("month").GetText();
	}

	public IActionCondition Clone()
	{
		return new MonthCondition(vas, month, Invert);
	}

	public override string ToString()
	{
		if (!Invert)
		{
			return "On the month " + month;
		}
		return "Outside the month " + month;
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
