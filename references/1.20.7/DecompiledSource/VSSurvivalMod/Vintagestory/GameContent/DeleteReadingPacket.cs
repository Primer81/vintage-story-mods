using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class DeleteReadingPacket
{
	[ProtoMember(1)]
	public int Index;
}
