using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSSurvivalMod.Systems.ChiselModes;

public abstract class ChiselMode
{
	public virtual int ChiselSize => 1;

	public abstract DrawSkillIconDelegate DrawAction(ICoreClientAPI capi);

	public virtual bool Apply(BlockEntityChisel chiselEntity, IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool isBreak, byte currentMaterialIndex)
	{
		Vec3i addAtPos = voxelPos.Clone().Add(ChiselSize * facing.Normali.X, ChiselSize * facing.Normali.Y, ChiselSize * facing.Normali.Z);
		if (isBreak)
		{
			return chiselEntity.SetVoxel(voxelPos, add: false, byPlayer, currentMaterialIndex);
		}
		if (addAtPos.X >= 0 && addAtPos.X < 16 && addAtPos.Y >= 0 && addAtPos.Y < 16 && addAtPos.Z >= 0 && addAtPos.Z < 16)
		{
			return chiselEntity.SetVoxel(addAtPos, add: true, byPlayer, currentMaterialIndex);
		}
		return false;
	}
}
