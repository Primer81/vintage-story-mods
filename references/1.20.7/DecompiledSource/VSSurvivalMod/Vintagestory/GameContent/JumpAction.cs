using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class JumpAction : EntityActionBase
{
	[JsonProperty]
	private int Index;

	[JsonProperty]
	private float hourOfDay = -1f;

	[JsonProperty]
	private int MinRepetitions = -1;

	[JsonProperty]
	private int MaxRepetitions = -1;

	private int repetitionsLeft = -1;

	public override string Type => "jump";

	public JumpAction()
	{
	}

	public JumpAction(EntityActivitySystem vas, int index, float hourOfDay, int minrepetitions, int maxrepetitions)
	{
		base.vas = vas;
		Index = index;
		this.hourOfDay = hourOfDay;
		MinRepetitions = minrepetitions;
		MaxRepetitions = maxrepetitions;
	}

	public override void Start(EntityActivity act)
	{
		if (hourOfDay >= 0f && vas.Entity.World.Calendar.HourOfDay > hourOfDay)
		{
			return;
		}
		if (MinRepetitions >= 0 && MaxRepetitions > 0)
		{
			if (repetitionsLeft < 0)
			{
				repetitionsLeft = vas.Entity.World.Rand.Next(MinRepetitions, MaxRepetitions);
			}
			else
			{
				repetitionsLeft--;
				if (repetitionsLeft <= 0)
				{
					return;
				}
			}
		}
		act.currentActionIndex = Index;
		act.CurrentAction?.Start(act);
	}

	public override void LoadState(ITreeAttribute tree)
	{
		repetitionsLeft = tree.GetInt("repetitionsLeft");
	}

	public override void StoreState(ITreeAttribute tree)
	{
		tree.SetInt("repetitionsLeft", repetitionsLeft);
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0);
		singleComposer.AddStaticText("Jump to Action Index", CairoFont.WhiteDetailText(), b).AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "index").AddStaticText("Until hour of day (0..24, -1 to ignore)", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 15.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "hourOfDay")
			.AddStaticText("OR amount of min repetitions (-1 to ignore)", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 15.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "minrepetitions")
			.AddStaticText("+ max repetitions (-1 to ignore)", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 5.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "maxrepetitions");
		singleComposer.GetTextInput("index").SetValue(Index);
		singleComposer.GetNumberInput("hourOfDay").SetValue(hourOfDay);
		singleComposer.GetNumberInput("minrepetitions").SetValue(MinRepetitions);
		singleComposer.GetNumberInput("maxrepetitions").SetValue(MaxRepetitions);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Index = (int)singleComposer.GetNumberInput("index").GetValue();
		hourOfDay = singleComposer.GetNumberInput("hourOfDay").GetValue();
		MinRepetitions = (int)singleComposer.GetNumberInput("minrepetitions").GetValue();
		MaxRepetitions = (int)singleComposer.GetNumberInput("maxrepetitions").GetValue();
		return true;
	}

	public override IEntityAction Clone()
	{
		return new JumpAction(vas, Index, hourOfDay, MinRepetitions, MaxRepetitions);
	}

	public override string ToString()
	{
		if (hourOfDay >= 0f)
		{
			return "Jump to action at index " + Index + " until hour of day is " + hourOfDay;
		}
		if (MinRepetitions >= 0 && MaxRepetitions > 0)
		{
			return "Jump to action at index " + Index + ", " + MinRepetitions + " to " + MaxRepetitions + " times.";
		}
		return "Jump to action at index " + Index;
	}
}
