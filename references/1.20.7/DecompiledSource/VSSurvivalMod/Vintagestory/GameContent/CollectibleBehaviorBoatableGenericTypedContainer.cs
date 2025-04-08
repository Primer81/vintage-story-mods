using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorBoatableGenericTypedContainer : CollectibleBehaviorHeldBag, IAttachedInteractions, IAttachedListener
{
	public CollectibleBehaviorBoatableGenericTypedContainer(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public override int GetQuantitySlots(ItemStack bagstack)
	{
		ITreeAttribute attributes = bagstack.Attributes;
		if (attributes != null && attributes.HasAttribute("animalSerialized"))
		{
			return 0;
		}
		string type = bagstack.Attributes.GetString("type");
		return (bagstack.ItemAttributes?["quantitySlots"]?[type]?.AsInt()).GetValueOrDefault();
	}

	public override EnumItemStorageFlags GetStorageFlags(ItemStack bagstack)
	{
		return base.GetStorageFlags(bagstack);
	}
}
