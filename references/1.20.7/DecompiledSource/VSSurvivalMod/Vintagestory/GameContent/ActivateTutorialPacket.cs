using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ActivateTutorialPacket
{
	[ProtoMember(1)]
	public string Code;
}
