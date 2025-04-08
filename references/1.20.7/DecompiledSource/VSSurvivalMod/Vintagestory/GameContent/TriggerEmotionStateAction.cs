using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class TriggerEmotionStateAction : EntityActionBase
{
	[JsonProperty]
	public string emotionState;

	public override string Type => "triggeremotionstate";

	public TriggerEmotionStateAction(EntityActivitySystem vas, string emotionState)
	{
		base.vas = vas;
		this.emotionState = emotionState;
	}

	public TriggerEmotionStateAction()
	{
	}

	public override void Start(EntityActivity act)
	{
		vas.Entity.GetBehavior<EntityBehaviorEmotionStates>()?.TryTriggerState(emotionState, vas.Entity.EntityId);
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Emotion State Code", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "emotionState");
		singleComposer.GetTextInput("emotionState").SetValue(emotionState);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		emotionState = singleComposer.GetTextInput("emotionState").GetText();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new TriggerEmotionStateAction(vas, emotionState);
	}

	public override string ToString()
	{
		return "Trigger emotion state " + emotionState;
	}
}
