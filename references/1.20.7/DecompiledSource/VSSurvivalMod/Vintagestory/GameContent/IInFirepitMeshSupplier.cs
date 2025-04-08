using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface IInFirepitMeshSupplier
{
	MeshData GetMeshWhenInFirepit(ItemStack stack, IWorldAccessor world, BlockPos pos, ref EnumFirepitModel firepitModel);
}
