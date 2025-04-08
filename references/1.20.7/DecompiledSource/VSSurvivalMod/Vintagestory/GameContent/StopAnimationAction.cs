using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class StopAnimationAction : EntityActionBase
{
	[JsonProperty]
	protected string Animation;

	public override string Type => "stopanimation";

	public StopAnimationAction()
	{
	}

	public StopAnimationAction(EntityActivitySystem vas)
	{
		base.vas = vas;
	}

	public StopAnimationAction(EntityActivitySystem vas, string anim)
	{
		Animation = anim;
	}

	public override bool IsFinished()
	{
		return true;
	}

	public override void Start(EntityActivity act)
	{
		vas.Entity.AnimManager.StopAnimation(Animation);
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Animation Code", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "animation");
		singleComposer.GetTextInput("animation").SetValue(Animation ?? "");
	}

	public override IEntityAction Clone()
	{
		return new StopAnimationAction(vas, Animation);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Animation = singleComposer.GetTextInput("animation").GetText();
		return true;
	}

	public override string ToString()
	{
		return "Stop animation " + Animation;
	}
}
