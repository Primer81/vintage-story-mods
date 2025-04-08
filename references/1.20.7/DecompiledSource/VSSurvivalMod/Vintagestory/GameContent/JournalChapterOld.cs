using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class JournalChapterOld
{
	public int EntryId;

	public string Text;
}
