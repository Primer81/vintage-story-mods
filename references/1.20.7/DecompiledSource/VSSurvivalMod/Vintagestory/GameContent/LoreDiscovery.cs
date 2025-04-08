using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class LoreDiscovery
{
	[ProtoMember(1)]
	public string Code;

	[ProtoMember(2)]
	public List<int> ChapterIds;
}
