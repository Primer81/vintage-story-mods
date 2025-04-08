using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class TranscribePacket
{
	[ProtoMember(1)]
	public string Text;

	[ProtoMember(2)]
	public string Title;

	[ProtoMember(3)]
	public int PageNumber;
}
