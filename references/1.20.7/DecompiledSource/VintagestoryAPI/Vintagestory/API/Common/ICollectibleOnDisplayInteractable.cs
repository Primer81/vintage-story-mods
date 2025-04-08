namespace Vintagestory.API.Common;

/// <summary>
/// A collectible object that can be placed on the ground or on shelves or in display cases, but also can still accept interactions from the player
/// </summary>
public interface ICollectibleOnDisplayInteractable
{
	bool OnInteractStart(ItemSlot inSlot, IPlayer byPlayer);

	bool OnInteractStep(float secondsUsed, ItemSlot inSlot, IPlayer byPlayer);

	void OnInteractStop(float secondsUsed, ItemSlot inSlot, IPlayer byPlayer);

	bool OnInteractCancel(float secondsUsed, ItemSlot inSlot, IPlayer byPlayer, EnumItemUseCancelReason cancelReason);
}
