using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class BlockEntityCommandPacket
{
	[ProtoMember(1)]
	public string Commands;

	[ProtoMember(2)]
	public bool Silent;
}
