using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ApplyConfigPacket
{
	[ProtoMember(1)]
	public long EntityId;

	[ProtoMember(2)]
	public string ActivityCollectionName;
}
