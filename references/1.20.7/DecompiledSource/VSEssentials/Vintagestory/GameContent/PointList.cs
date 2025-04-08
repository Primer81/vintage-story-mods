using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class PointList
{
	[ProtoMember(1)]
	public List<ClothPoint> Points = new List<ClothPoint>();
}
