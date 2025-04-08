using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public interface IGearAcceptor
{
	bool CanAcceptGear(BlockPos pos);

	void AddGear(BlockPos pos);

	void RemoveGearAt(BlockPos pos);
}
