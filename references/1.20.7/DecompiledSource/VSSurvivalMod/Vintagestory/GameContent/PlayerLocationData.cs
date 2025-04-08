using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class PlayerLocationData
{
	[ProtoMember(1)]
	public Vec3d Position;

	[ProtoMember(2)]
	public double TotalDaysSinceLastTeleport;
}
