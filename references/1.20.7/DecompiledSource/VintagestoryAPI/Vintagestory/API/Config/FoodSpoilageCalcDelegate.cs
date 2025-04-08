using Vintagestory.API.Common;

namespace Vintagestory.API.Config;

public delegate float FoodSpoilageCalcDelegate(float spoilState, ItemStack stack, EntityAgent byEntity);
