using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class JournalEntryOld
{
	public int EntryId;

	public string LoreCode;

	public string Title;

	public bool Editable;

	public List<JournalChapterOld> Chapters = new List<JournalChapterOld>();
}
