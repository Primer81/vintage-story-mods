using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class EntityOwnershipPacket
{
	[ProtoMember(1)]
	public Dictionary<string, EntityOwnership> OwnerShipByGroup;
}
