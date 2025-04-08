using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods.WorldEdit;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ChangePlayerModePacket
{
	public EnumFreeMovAxisLock? axisLock;

	public float? pickingRange;

	public bool? fly;

	public bool? noclip;
}
