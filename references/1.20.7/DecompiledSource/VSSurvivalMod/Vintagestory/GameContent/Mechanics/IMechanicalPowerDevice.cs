using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public interface IMechanicalPowerDevice : IMechanicalPowerRenderable, IMechanicalPowerNode
{
	BlockFacing OutFacingForNetworkDiscovery { get; }

	MechanicalNetwork Network { get; }

	BlockFacing GetPropagationDirection();

	BlockFacing GetPropagationDirectionInput();

	bool IsPropagationDirection(BlockPos fromPos, BlockFacing test);

	void SetPropagationDirection(MechPowerPath turnDir);

	bool JoinAndSpreadNetworkToNeighbours(ICoreAPI api, MechanicalNetwork network, MechPowerPath turnDir, out Vec3i missingChunkPos);

	MechanicalNetwork CreateJoinAndDiscoverNetwork(BlockFacing powerOutFacing);

	bool isRotationReversed();

	bool isInvertedNetworkFor(BlockPos pos);

	void DestroyJoin(BlockPos pos);

	float GetGearedRatio(BlockFacing toFacing);
}
