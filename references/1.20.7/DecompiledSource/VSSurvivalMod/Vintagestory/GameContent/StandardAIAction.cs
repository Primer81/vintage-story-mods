using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class StandardAIAction : EntityActionBase
{
	[JsonProperty]
	public float durationSeconds;

	private float secondsLeft;

	public override string Type => "standardai";

	public StandardAIAction(EntityActivitySystem vas, float durationSeconds)
	{
		base.vas = vas;
		this.durationSeconds = durationSeconds;
	}

	public StandardAIAction()
	{
	}

	public override void OnTick(float dt)
	{
		secondsLeft -= dt;
	}

	public override bool IsFinished()
	{
		return secondsLeft <= 0f;
	}

	public override void Start(EntityActivity act)
	{
		secondsLeft = durationSeconds;
	}

	public override void Cancel()
	{
		secondsLeft = 0f;
	}

	public override void Finish()
	{
		secondsLeft = 0f;
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0);
		singleComposer.AddStaticText("Duration in IRL Seconds", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "duration");
		singleComposer.GetTextInput("duration").SetValue(durationSeconds);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		durationSeconds = singleComposer.GetTextInput("duration").GetText().ToFloat();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new StandardAIAction(vas, durationSeconds);
	}

	public override string ToString()
	{
		return "Run standard AI for " + durationSeconds + "s";
	}
}
