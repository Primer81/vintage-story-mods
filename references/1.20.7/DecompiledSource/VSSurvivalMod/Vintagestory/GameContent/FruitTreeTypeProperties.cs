using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class FruitTreeTypeProperties
{
	public NatFloat VernalizationHours = NatFloat.createUniform(100f, 10f);

	public NatFloat VernalizationTemp = NatFloat.createUniform(1f, 1f);

	public NatFloat FloweringDays = NatFloat.createUniform(3f, 1.5f);

	public NatFloat FruitingDays = NatFloat.createUniform(6f, 1.5f);

	public NatFloat RipeDays = NatFloat.createUniform(3f, 1.5f);

	public NatFloat GrowthStepDays = NatFloat.createUniform(2f, 0.5f);

	public BlockDropItemStack[] FruitStacks;

	public EnumTreeCycleType CycleType;

	public bool BlossomParticles = true;

	public NatFloat DieBelowTemp = NatFloat.createUniform(-20f, -5f);

	public NatFloat LeaveDormancyTemp = NatFloat.createUniform(20f, 0f);

	public NatFloat EnterDormancyTemp = NatFloat.createUniform(-2f, 0f);

	public NatFloat LooseLeavesBelowTemp = NatFloat.createUniform(0f, 0f);

	public NatFloat BlossomAtYearRel = NatFloat.createUniform(0.4f, 0f);

	public NatFloat RootSizeMul = NatFloat.createUniform(1f, 0f);

	public float CuttingRootingChance = 0.25f;

	public float CuttingGraftChance = 0.5f;
}
