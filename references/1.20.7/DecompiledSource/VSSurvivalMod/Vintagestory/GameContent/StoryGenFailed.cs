using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class StoryGenFailed
{
	[ProtoMember(1)]
	public List<string> MissingStructures;
}
