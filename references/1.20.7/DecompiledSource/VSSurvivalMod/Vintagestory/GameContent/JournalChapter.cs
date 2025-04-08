using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class JournalChapter
{
	[ProtoMember(1)]
	public int EntryId;

	[ProtoMember(2)]
	public int ChapterId;

	[ProtoMember(3)]
	public string Text;
}
