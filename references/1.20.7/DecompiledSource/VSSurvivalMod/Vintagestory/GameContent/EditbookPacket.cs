using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class EditbookPacket
{
	[ProtoMember(1)]
	public bool DidSave;

	[ProtoMember(2)]
	public bool DidSign;

	[ProtoMember(3)]
	public string Text;

	[ProtoMember(4)]
	public string Title;
}
