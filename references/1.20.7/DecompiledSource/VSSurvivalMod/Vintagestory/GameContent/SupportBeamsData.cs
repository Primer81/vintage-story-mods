using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class SupportBeamsData
{
	[ProtoMember(1)]
	public HashSet<StartEnd> Beams = new HashSet<StartEnd>();
}
