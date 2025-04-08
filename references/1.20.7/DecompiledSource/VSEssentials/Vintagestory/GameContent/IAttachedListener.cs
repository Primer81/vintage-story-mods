using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public interface IAttachedListener
{
	void OnAttached(ItemSlot itemslot, int slotIndex, Entity toEntity, EntityAgent byEntity);

	void OnDetached(ItemSlot itemslot, int slotIndex, Entity fromEntity, EntityAgent byEntity);
}
