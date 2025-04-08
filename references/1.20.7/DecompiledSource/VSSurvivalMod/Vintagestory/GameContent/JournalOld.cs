using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class JournalOld
{
	public List<JournalEntryOld> Entries = new List<JournalEntryOld>();
}
