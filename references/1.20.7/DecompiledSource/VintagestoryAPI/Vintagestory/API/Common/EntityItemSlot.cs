namespace Vintagestory.API.Common;

public class EntityItemSlot : DummySlot
{
	public EntityItem Ei;

	public EntityItemSlot(EntityItem ei)
	{
		Ei = ei;
	}
}
