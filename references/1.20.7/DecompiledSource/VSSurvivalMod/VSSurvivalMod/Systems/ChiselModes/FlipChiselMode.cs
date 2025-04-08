using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSSurvivalMod.Systems.ChiselModes;

public class FlipChiselMode : ChiselMode
{
	public override DrawSkillIconDelegate DrawAction(ICoreClientAPI capi)
	{
		return capi.Gui.Icons.Drawrepeat_svg;
	}

	public override bool Apply(BlockEntityChisel chiselEntity, IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool isBreak, byte currentMaterialIndex)
	{
		BlockFacing[] facings = Block.SuggestedHVOrientation(byPlayer, new BlockSelection
		{
			Position = chiselEntity.Pos.Copy(),
			HitPosition = new Vec3d((double)voxelPos.X / 16.0, (double)voxelPos.Y / 16.0, (double)voxelPos.Z / 16.0)
		});
		chiselEntity.FlipVoxels(facings[0]);
		return true;
	}
}
