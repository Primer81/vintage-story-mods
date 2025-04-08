using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods.WorldEdit;

[ProtoContract]
public class WorldInteractPacket
{
	[ProtoMember(1)]
	public int Mode;

	[ProtoMember(2)]
	public BlockPos Position;

	[ProtoMember(3)]
	public int Face;

	[ProtoMember(4)]
	public Vec3d HitPosition;

	[ProtoMember(5)]
	public int SelectionBoxIndex;

	[ProtoMember(6)]
	public bool DidOffset;
}
