using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class OreReading
{
	[ProtoMember(1)]
	public string DepositCode;

	[ProtoMember(2)]
	public double TotalFactor;

	[ProtoMember(3)]
	public double PartsPerThousand;
}
