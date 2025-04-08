using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class EditSignPacket
{
	[ProtoMember(1)]
	public string Text;

	[ProtoMember(2)]
	public float FontSize;
}
