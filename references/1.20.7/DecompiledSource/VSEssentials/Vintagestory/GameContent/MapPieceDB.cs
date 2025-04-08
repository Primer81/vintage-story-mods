using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class MapPieceDB
{
	[ProtoMember(1)]
	public int[] Pixels;
}
