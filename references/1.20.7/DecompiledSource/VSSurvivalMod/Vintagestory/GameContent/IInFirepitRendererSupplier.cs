using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IInFirepitRendererSupplier
{
	IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot);

	EnumFirepitModel GetDesiredFirepitModel(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot);
}
