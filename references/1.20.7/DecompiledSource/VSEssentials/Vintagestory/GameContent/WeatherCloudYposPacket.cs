using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class WeatherCloudYposPacket
{
	public float CloudYRel;
}
