using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class PlayAnimationAction : EntityActionBase
{
	[JsonProperty]
	protected AnimationMetaData meta;

	[JsonProperty]
	protected float DurationHours = -1f;

	[JsonProperty]
	protected float DurationIrlSeconds = -1f;

	[JsonProperty]
	protected int OnAnimEnd;

	private double untilTotalHours;

	private double secondsLeft;

	public override string Type => "playanimation";

	public PlayAnimationAction()
	{
	}

	public PlayAnimationAction(EntityActivitySystem vas)
	{
		base.vas = vas;
	}

	public PlayAnimationAction(EntityActivitySystem vas, AnimationMetaData meta, float durationHours, float durationIrlSeconds, int onAnimEnd)
	{
		this.meta = meta;
		DurationHours = durationHours;
		DurationIrlSeconds = durationIrlSeconds;
		OnAnimEnd = onAnimEnd;
	}

	public override void Pause()
	{
		vas.Entity.AnimManager.StopAnimation(meta.Animation);
	}

	public override void Resume()
	{
		vas.Entity.AnimManager.StartAnimation(meta.Init());
	}

	public override bool IsFinished()
	{
		if (OnAnimEnd == 0 && DurationHours >= 0f)
		{
			return vas.Entity.World.Calendar.TotalHours > untilTotalHours;
		}
		if (OnAnimEnd == 0 && DurationIrlSeconds >= 0f)
		{
			return secondsLeft <= 0.0;
		}
		return !vas.Entity.AnimManager.IsAnimationActive(meta.Animation);
	}

	public override void Start(EntityActivity act)
	{
		untilTotalHours = vas.Entity.World.Calendar.TotalHours + (double)DurationHours;
		secondsLeft = DurationIrlSeconds;
		if (meta.Animation.Contains(","))
		{
			AnimationMetaData cm = meta.Clone();
			string[] anims = meta.Animation.Split(",");
			cm.Code = (cm.Animation = anims[vas.Entity.World.Rand.Next(anims.Length)]);
			vas.Entity.AnimManager.StartAnimation(cm.Init());
		}
		else
		{
			vas.Entity.AnimManager.StartAnimation(meta.Init());
		}
	}

	public override void Cancel()
	{
		Finish();
	}

	public override void Finish()
	{
		vas.Entity.AnimManager.StopAnimation(meta.Animation);
	}

	public override void LoadState(ITreeAttribute tree)
	{
		untilTotalHours = tree.GetDouble("untilTotalHours");
		secondsLeft = tree.GetDouble("secondsLeft");
	}

	public override void StoreState(ITreeAttribute tree)
	{
		tree.SetDouble("untilTotalHours", untilTotalHours);
		tree.SetDouble("secondsLeft", secondsLeft);
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 250.0, 25.0);
		GuiComposer composer = singleComposer.AddStaticText("Animation Code", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "animation").AddStaticText("Animation Speed", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "speed")
			.AddStaticText("Duration (ingame hours. -1 to ignore)", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "durationHours")
			.AddStaticText("OR Duration (irl seconds. -1 to ignore)", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "durationIrlSeconds");
		CairoFont font = CairoFont.WhiteDetailText();
		ElementBounds elementBounds = b.BelowCopy(0.0, 10.0);
		Vintagestory.API.Client.GuiComposerHelpers.AddDropDown(bounds: b = elementBounds.BelowCopy(0.0, -5.0), composer: composer.AddStaticText("On Animation End", font, elementBounds), values: new string[2] { "repeat", "stop" }, names: new string[2] { "Repeat Animation", "Stop Action" }, selectedIndex: OnAnimEnd, onSelectionChanged: null, font: CairoFont.WhiteDetailText(), key: "onAnimEnd");
		singleComposer.GetTextInput("animation").SetValue(meta?.Animation ?? "");
		singleComposer.GetNumberInput("speed").SetValue(meta?.AnimationSpeed ?? 1f);
		singleComposer.GetNumberInput("durationHours").SetValue(DurationHours);
		singleComposer.GetNumberInput("durationIrlSeconds").SetValue(DurationIrlSeconds);
	}

	public override IEntityAction Clone()
	{
		return new PlayAnimationAction(vas, meta, DurationHours, DurationIrlSeconds, OnAnimEnd);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		meta = new AnimationMetaData
		{
			Animation = singleComposer.GetTextInput("animation").GetText(),
			AnimationSpeed = singleComposer.GetNumberInput("speed").GetText().ToFloat(1f)
		};
		DurationHours = singleComposer.GetNumberInput("durationHours").GetValue();
		DurationIrlSeconds = singleComposer.GetNumberInput("durationIrlSeconds").GetValue();
		OnAnimEnd = singleComposer.GetDropDown("onAnimEnd").SelectedIndices[0];
		return true;
	}

	public override string ToString()
	{
		if (DurationHours >= 0f && OnAnimEnd == 0)
		{
			return "Play animation " + meta?.Animation + ". Repeat for " + DurationHours + " ingame hours";
		}
		if (DurationIrlSeconds >= 0f && OnAnimEnd == 0)
		{
			return "Play animation " + meta?.Animation + ". Repeat for " + DurationIrlSeconds + " irl seconds";
		}
		return "Play animation " + meta?.Animation + " until finished.";
	}

	public override void OnTick(float dt)
	{
		secondsLeft -= dt;
		if (OnAnimEnd == 0 && (secondsLeft >= 0.0 || DurationHours >= 0f) && !vas.Entity.AnimManager.IsAnimationActive(meta.Animation))
		{
			vas.Entity.AnimManager.StartAnimation(meta.Init());
		}
	}
}
