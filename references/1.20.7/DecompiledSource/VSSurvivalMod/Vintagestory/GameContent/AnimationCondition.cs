using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class AnimationCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private string animCode = "";

	protected EntityActivitySystem vas;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "animation";

	public AnimationCondition()
	{
	}

	public AnimationCondition(EntityActivitySystem vas, string animCode, bool invert = false)
	{
		this.vas = vas;
		this.animCode = animCode;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		if (animCode.Contains(","))
		{
			return vas.Entity.AnimManager.IsAnimationActive(animCode.Split(","));
		}
		return vas.Entity.AnimManager.IsAnimationActive(animCode);
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
		singleComposer.AddStaticText("Animation Code", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "animCode");
		singleComposer.GetTextInput("animCode").SetValue(animCode);
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		animCode = singleComposer.GetTextInput("animCode").GetText();
	}

	public IActionCondition Clone()
	{
		return new AnimationCondition(vas, animCode, Invert);
	}

	public override string ToString()
	{
		if (!Invert)
		{
			return "When animation " + animCode + " plays";
		}
		return "When animation " + animCode + " does not play";
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
