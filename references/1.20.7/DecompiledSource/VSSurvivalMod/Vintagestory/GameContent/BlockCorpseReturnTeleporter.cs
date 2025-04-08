using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCorpseReturnTeleporter : Block
{
	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		(world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCorpseReturnTeleporter)?.OnInteract(byPlayer);
		return true;
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCorpseReturnTeleporter be)
		{
			be.OnEntityCollide(entity);
		}
	}
}
