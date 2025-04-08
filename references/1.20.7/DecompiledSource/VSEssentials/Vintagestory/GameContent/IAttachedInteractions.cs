using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public interface IAttachedInteractions : IAttachedListener
{
	bool OnTryAttach(ItemSlot itemslot, int slotIndex, Entity toEntity);

	bool OnTryDetach(ItemSlot itemslot, int slotIndex, Entity toEntity);

	void OnInteract(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave);

	void OnEntityDespawn(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityDespawnData despawn);

	void OnEntityDeath(ItemSlot itemslot, int slotIndex, Entity onEntity, DamageSource damageSourceForDeath);

	void OnReceivedClientPacket(ItemSlot itemslot, int slotIndex, Entity onEntity, IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled, Action onRequireSave);
}
