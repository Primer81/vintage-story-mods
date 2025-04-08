using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class ChiselBlockInEdit
{
	public BoolArray16x16x16 voxels;

	public byte[,,] voxelMaterial;

	public BlockEntityChisel be;

	public bool isNew;
}
