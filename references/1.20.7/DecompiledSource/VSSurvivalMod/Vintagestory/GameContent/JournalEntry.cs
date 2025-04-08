using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class JournalEntry
{
	[ProtoMember(1)]
	public int EntryId;

	[ProtoMember(2)]
	public string LoreCode;

	[ProtoMember(3)]
	public string Title;

	[ProtoMember(4)]
	public bool Editable;

	[ProtoMember(5)]
	public List<JournalChapter> Chapters = new List<JournalChapter>();
}
