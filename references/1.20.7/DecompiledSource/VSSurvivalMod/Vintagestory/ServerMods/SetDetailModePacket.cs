using ProtoBuf;

namespace Vintagestory.ServerMods;

[ProtoContract]
internal class SetDetailModePacket
{
	[ProtoMember(1)]
	public bool Enable;
}
