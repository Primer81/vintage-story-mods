using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class AfterActivityCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private string activity;

	private string[] activities;

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "afteractivity";

	public AfterActivityCondition()
	{
	}

	public AfterActivityCondition(EntityActivitySystem vas, string activity, bool invert = false)
	{
		this.vas = vas;
		this.activity = activity;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		string lastActivity = e.Attributes.GetString("lastActivity");
		if (lastActivity == null)
		{
			return false;
		}
		if (activities != null)
		{
			return activities.Contains(lastActivity);
		}
		return lastActivity == activity;
	}

	public void LoadState(ITreeAttribute tree)
	{
		if (activity.Contains(","))
		{
			activities = activity.Split(',');
		}
		else
		{
			activities = null;
		}
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Activity", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "activity");
		singleComposer.GetTextInput("activity").SetValue(activity ?? "");
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		activity = singleComposer.GetTextInput("activity").GetText();
		if (activity.Contains(","))
		{
			activities = activity.Split(',');
		}
		else
		{
			activities = null;
		}
	}

	public IActionCondition Clone()
	{
		return new AfterActivityCondition(vas, activity, Invert);
	}

	public override string ToString()
	{
		if (!Invert)
		{
			return "Right after activity " + activity;
		}
		return "Whenever activity " + activity + " is not running";
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
		if (activity.Contains(","))
		{
			activities = activity.Split(',');
		}
		else
		{
			activities = null;
		}
	}
}
