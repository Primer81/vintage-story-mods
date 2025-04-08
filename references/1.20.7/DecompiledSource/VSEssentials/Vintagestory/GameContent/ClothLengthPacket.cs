using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ClothLengthPacket
{
	public int ClothId;

	public double LengthChange;
}
