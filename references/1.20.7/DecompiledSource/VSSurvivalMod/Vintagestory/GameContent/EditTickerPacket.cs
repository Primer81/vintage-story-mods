using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class EditTickerPacket
{
	[ProtoMember(1)]
	public string Interval;

	[ProtoMember(2)]
	public bool Active;
}
