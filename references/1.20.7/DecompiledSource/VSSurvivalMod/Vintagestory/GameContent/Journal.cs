using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class Journal
{
	[ProtoMember(1)]
	public List<JournalEntry> Entries = new List<JournalEntry>();
}
