using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class BlockPlacedPacket
{
	[ProtoMember(1)]
	public BlockPos pos;

	[ProtoMember(2)]
	public int blockId;

	[ProtoMember(3)]
	public byte[] withStackInHands;
}
