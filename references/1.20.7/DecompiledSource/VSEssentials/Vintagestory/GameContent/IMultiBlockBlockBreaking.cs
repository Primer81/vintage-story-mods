using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface IMultiBlockBlockBreaking
{
	void MBOnBlockBroken(IWorldAccessor world, BlockPos pos, Vec3i offset, IPlayer byPlayer, float dropQuantityMultiplier = 1f);

	int MBGetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex, Vec3i offsetInv);

	int MBGetColorWithoutTint(ICoreClientAPI capi, BlockPos pos, Vec3i offsetInv);

	float MBOnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter, Vec3i offsetInv);
}
