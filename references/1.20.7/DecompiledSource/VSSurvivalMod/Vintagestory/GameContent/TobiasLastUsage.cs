using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class TobiasLastUsage
{
	[ProtoMember(1)]
	public double LastUsage { get; set; }
}
