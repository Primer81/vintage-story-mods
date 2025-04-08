using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class UnregisterClothSystemPacket
{
	public int[] ClothIds;
}
