using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class DevaLocation
{
	[ProtoMember(1)]
	public BlockPos Pos;

	[ProtoMember(2)]
	public int Radius;
}
