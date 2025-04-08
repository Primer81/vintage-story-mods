using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ClothStack
{
	public EnumItemClass Class;

	public string Code;

	public int SlotNum;
}
