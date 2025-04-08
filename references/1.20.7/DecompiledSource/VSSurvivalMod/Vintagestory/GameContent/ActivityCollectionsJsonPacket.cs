using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ActivityCollectionsJsonPacket
{
	[ProtoMember(1)]
	public List<string> Collections;
}
