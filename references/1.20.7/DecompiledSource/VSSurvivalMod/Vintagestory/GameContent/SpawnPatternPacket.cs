using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SpawnPatternPacket
{
	public CurrentPattern Pattern;
}
