using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods.WorldEdit;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PreviewBlocksPacket
{
	public BlockPos pos;

	public int dimId;

	public bool TrackSelection;
}
