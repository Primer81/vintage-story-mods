using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ScreenshakePacket
{
	[ProtoMember(1)]
	public float Strength;
}
