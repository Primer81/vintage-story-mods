using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods.NoObf;

public class TreeGenRoot
{
	public NatFloat rootEnd;

	public NatFloat rootSpacing;

	public NatFloat numBranching;

	public NatFloat baseWidth;

	public float widthloss;

	public EvolvingNatFloat horizontalAngle;

	public EvolvingNatFloat verticalAngle;

	public NatFloat branchVerticalAngle;

	public NatFloat branchHorizontalAngle;

	public NatFloat branchSpacing;

	public NatFloat branchStart;

	public float widthBranchLossBase = 1f;

	public TreeGenRoot(NatFloat baseWidth, NatFloat rootEnd, NatFloat rootSpacing, NatFloat numBranching, float widthloss, EvolvingNatFloat horizontalAngle, EvolvingNatFloat verticalAngle, NatFloat branchVerticalAngle, NatFloat branchHorizontalAngle, NatFloat branchSpacing, NatFloat branchStart, float widthBranchLossBase)
	{
		this.baseWidth = baseWidth;
		this.rootEnd = rootEnd;
		this.rootSpacing = rootSpacing;
		this.numBranching = numBranching;
		this.horizontalAngle = horizontalAngle;
		this.verticalAngle = verticalAngle;
		this.widthloss = widthloss;
		this.branchVerticalAngle = branchVerticalAngle;
		this.branchHorizontalAngle = branchHorizontalAngle;
		this.branchSpacing = branchSpacing;
		this.branchStart = branchStart;
		this.widthBranchLossBase = widthBranchLossBase;
	}
}
