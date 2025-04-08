using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Server;

public class ClaimInProgress
{
	public bool IsNew;

	public LandClaim OriginalClaim;

	public LandClaim Claim;

	public BlockPos Start;

	public BlockPos End;
}
