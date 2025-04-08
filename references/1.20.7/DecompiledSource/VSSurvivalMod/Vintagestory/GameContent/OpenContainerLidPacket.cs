using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class OpenContainerLidPacket
{
	[ProtoMember(1)]
	public long EntityId;

	[ProtoMember(2)]
	public bool Opened;

	public OpenContainerLidPacket()
	{
	}

	public OpenContainerLidPacket(long entityId, bool opened)
	{
		EntityId = entityId;
		Opened = opened;
	}
}
