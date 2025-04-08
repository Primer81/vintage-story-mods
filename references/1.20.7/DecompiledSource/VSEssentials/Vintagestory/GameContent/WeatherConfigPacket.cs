using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class WeatherConfigPacket
{
	[ProtoMember(1)]
	public float? OverridePrecipitation;

	[ProtoMember(2)]
	public double RainCloudDaysOffset;
}
