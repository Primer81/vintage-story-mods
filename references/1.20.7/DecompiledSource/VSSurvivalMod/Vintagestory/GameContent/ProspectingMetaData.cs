using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ProspectingMetaData
{
	[ProtoMember(1)]
	public Dictionary<string, string> PageCodes = new Dictionary<string, string>();
}
