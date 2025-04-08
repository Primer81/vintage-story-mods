using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ClothPointPacket
{
	public int ClothId;

	public int PointX;

	public int PointY;

	public ClothPoint Point;
}
