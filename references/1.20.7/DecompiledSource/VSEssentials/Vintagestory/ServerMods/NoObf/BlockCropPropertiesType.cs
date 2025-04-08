using Vintagestory.API;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
public class BlockCropPropertiesType
{
	[DocumentAsJson]
	public EnumSoilNutrient RequiredNutrient;

	[DocumentAsJson]
	public float NutrientConsumption;

	[DocumentAsJson]
	public int GrowthStages;

	[DocumentAsJson]
	public float TotalGrowthDays;

	[DocumentAsJson]
	public float TotalGrowthMonths;

	[DocumentAsJson]
	public bool MultipleHarvests;

	[DocumentAsJson]
	public int HarvestGrowthStageLoss;

	[DocumentAsJson]
	public float ColdDamageBelow = -5f;

	[DocumentAsJson]
	public float DamageGrowthStuntMul = 0.5f;

	[DocumentAsJson]
	public float ColdDamageRipeMul = 0.5f;

	[DocumentAsJson]
	public float HeatDamageAbove = 40f;

	[DocumentAsJson]
	public CropBehaviorType[] Behaviors;
}
