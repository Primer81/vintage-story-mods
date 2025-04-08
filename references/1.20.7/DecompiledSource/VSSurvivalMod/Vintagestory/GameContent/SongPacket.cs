using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class SongPacket
{
	[ProtoMember(1)]
	public string SoundLocation;

	[ProtoMember(2)]
	public float SecondsPassed;
}
