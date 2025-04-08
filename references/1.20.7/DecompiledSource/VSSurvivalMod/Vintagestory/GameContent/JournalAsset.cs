using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class JournalAsset
{
	public string Code;

	public string Title;

	public string[] Pieces;

	public string Category;
}
