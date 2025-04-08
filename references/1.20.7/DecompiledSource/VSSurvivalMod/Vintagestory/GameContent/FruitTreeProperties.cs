using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class FruitTreeProperties
{
	public float VernalizationHours;

	public float VernalizationTemp;

	public float FloweringDays;

	public float FruitingDays;

	public float RipeDays;

	public float GrowthStepDays;

	public float LeaveDormancyTemp;

	public float EnterDormancyTemp;

	public float DieBelowTemp;

	public BlockDropItemStack[] FruitStacks;

	public double lastCheckAtTotalDays;

	public double lastStateChangeTotalDays;

	public double vernalizedHours;

	public EnumTreeCycleType CycleType;

	public float LooseLeavesBelowTemp = -10f;

	public float BlossomAtYearRel = 0.3f;

	public float RootSizeMul;

	protected EnumFruitTreeState state;

	public EnumFruitTreeState workingState;

	public EnumFruitTreeState State
	{
		get
		{
			return state;
		}
		set
		{
			bool num = state != value;
			state = value;
			if (num)
			{
				this.OnFruitingStateChange?.Invoke(state);
			}
		}
	}

	public event FruitingStateChangeDelegate OnFruitingStateChange;

	public void FromTreeAttributes(ITreeAttribute tree)
	{
		State = (EnumFruitTreeState)tree.GetInt("rootFruitTreeState", 4);
		lastCheckAtTotalDays = tree.GetDouble("lastCheckAtTotalDays");
		vernalizedHours = tree.GetDouble("vernalizedHours");
		lastStateChangeTotalDays = tree.GetDouble("lastStateChangeTotalDays");
		FloweringDays = tree.GetFloat("floweringDays");
		FruitingDays = tree.GetFloat("fruitingDays");
		RipeDays = tree.GetFloat("ripeDays");
		GrowthStepDays = tree.GetFloat("growthStepDays");
		RootSizeMul = tree.GetFloat("rootSizeMul");
		DieBelowTemp = tree.GetFloat("dieBelowTemp");
		CycleType = (EnumTreeCycleType)tree.GetInt("cycleType");
		if (CycleType == EnumTreeCycleType.Deciduous)
		{
			VernalizationHours = tree.GetFloat("vernalizationHours");
			VernalizationTemp = tree.GetFloat("vernalizationTemp");
		}
		if (CycleType == EnumTreeCycleType.Evergreen)
		{
			LooseLeavesBelowTemp = tree.GetFloat("looseLeavesBelowTemp");
			BlossomAtYearRel = tree.GetFloat("blossomAtYearRel");
		}
	}

	public void ToTreeAttributes(ITreeAttribute tree)
	{
		tree.SetInt("rootFruitTreeState", (int)State);
		tree.SetDouble("lastCheckAtTotalDays", lastCheckAtTotalDays);
		tree.SetDouble("lastStateChangeTotalDays", lastStateChangeTotalDays);
		tree.SetDouble("vernalizedHours", vernalizedHours);
		tree.SetFloat("floweringDays", FloweringDays);
		tree.SetFloat("fruitingDays", FruitingDays);
		tree.SetFloat("ripeDays", RipeDays);
		tree.SetFloat("growthStepDays", GrowthStepDays);
		tree.SetFloat("rootSizeMul", RootSizeMul);
		tree.SetFloat("dieBelowTemp", DieBelowTemp);
		tree.SetInt("cycleType", (int)CycleType);
		if (CycleType == EnumTreeCycleType.Deciduous)
		{
			tree.SetFloat("vernalizationHours", VernalizationHours);
			tree.SetFloat("vernalizationTemp", VernalizationTemp);
		}
		if (CycleType == EnumTreeCycleType.Evergreen)
		{
			tree.SetFloat("looseLeavesBelowTemp", LooseLeavesBelowTemp);
			tree.SetFloat("blossomAtYearRel", BlossomAtYearRel);
		}
	}
}
