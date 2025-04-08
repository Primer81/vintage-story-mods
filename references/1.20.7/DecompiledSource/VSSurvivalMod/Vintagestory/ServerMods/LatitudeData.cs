using ProtoBuf;

namespace Vintagestory.ServerMods;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class LatitudeData
{
	public double ZOffset;

	public bool isRealisticClimate;

	public int polarEquatorDistance;
}
