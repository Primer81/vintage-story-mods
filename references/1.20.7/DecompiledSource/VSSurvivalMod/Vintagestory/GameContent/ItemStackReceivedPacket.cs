using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ItemStackReceivedPacket
{
	[ProtoMember(1)]
	public string eventname;

	[ProtoMember(2)]
	public byte[] stackbytes;
}
