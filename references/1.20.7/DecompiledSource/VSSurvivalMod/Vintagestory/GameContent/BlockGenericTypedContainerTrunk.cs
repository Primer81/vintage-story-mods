using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockGenericTypedContainerTrunk : BlockGenericTypedContainer, IMultiBlockColSelBoxes
{
	private Cuboidf[] mirroredColBox;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		mirroredColBox = new Cuboidf[1] { CollisionBoxes[0].RotatedCopy(0f, 180f, 0f, new Vec3d(0.5, 0.5, 0.5)) };
	}

	public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
	{
		return mirroredColBox;
	}

	public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
	{
		return mirroredColBox;
	}

	public override bool IsAttachable(Entity toEntity, ItemStack itemStack)
	{
		return false;
	}
}
