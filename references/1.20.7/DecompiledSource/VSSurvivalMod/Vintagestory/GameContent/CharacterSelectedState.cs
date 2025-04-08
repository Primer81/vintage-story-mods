using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CharacterSelectedState
{
	public bool DidSelect;
}
