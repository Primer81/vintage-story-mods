using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class DlgPositioner : ICustomDialogPositioning
{
	public Entity entity;

	public int slotIndex;

	public DlgPositioner(Entity entity, int slotIndex)
	{
		this.entity = entity;
		this.slotIndex = slotIndex;
	}

	public Vec3d GetDialogPosition()
	{
		return entity.GetBehavior<EntityBehaviorSelectionBoxes>().GetCenterPosOfBox(slotIndex)?.Add(0.0, 1.0, 0.0);
	}
}
