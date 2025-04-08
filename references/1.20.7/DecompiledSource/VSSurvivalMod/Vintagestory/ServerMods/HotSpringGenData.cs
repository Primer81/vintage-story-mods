using ProtoBuf;

namespace Vintagestory.ServerMods;

[ProtoContract]
public class HotSpringGenData
{
	[ProtoMember(1)]
	public double horRadius;

	[ProtoMember(2)]
	public double verRadiusSq;
}
