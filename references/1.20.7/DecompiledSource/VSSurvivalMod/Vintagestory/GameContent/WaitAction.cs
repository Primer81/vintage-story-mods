using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class WaitAction : EntityActionBase
{
	[JsonProperty]
	private float durationSeconds;

	private float waitLeftSeconds;

	public override string Type => "wait";

	public WaitAction()
	{
	}

	public WaitAction(EntityActivitySystem vas, float durationSeconds)
	{
		base.vas = vas;
		this.durationSeconds = durationSeconds;
	}

	public override bool IsFinished()
	{
		return waitLeftSeconds < 0f;
	}

	public override void Start(EntityActivity act)
	{
		waitLeftSeconds = durationSeconds;
	}

	public override void OnTick(float dt)
	{
		waitLeftSeconds -= dt;
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0);
		singleComposer.AddStaticText("Wait IRL seconds", CairoFont.WhiteDetailText(), b).AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "wait");
		singleComposer.GetTextInput("wait").SetValue(durationSeconds);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		durationSeconds = singleComposer.GetNumberInput("wait").GetValue();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new WaitAction(vas, durationSeconds);
	}

	public override string ToString()
	{
		return "Wait for " + durationSeconds + " IRL seconds";
	}
}
