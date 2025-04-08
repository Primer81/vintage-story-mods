using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CurrentPattern
{
	public string Code;

	public double UntilTotalHours;
}
