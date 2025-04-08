using ProtoBuf;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

[ProtoContract]
public class EntityOwnership
{
	[ProtoMember(1)]
	public long EntityId;

	[ProtoMember(2)]
	public EntityPos Pos;

	[ProtoMember(3)]
	public string Color;

	[ProtoMember(4)]
	public string Name;
}
