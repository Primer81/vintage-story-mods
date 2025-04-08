using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.MathTools;

public interface IWorldIntersectionSupplier
{
	Vec3i MapSize { get; }

	IBlockAccessor blockAccessor { get; }

	Block GetBlock(BlockPos pos);

	Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos);

	Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null);

	bool IsValidPos(BlockPos pos);
}
