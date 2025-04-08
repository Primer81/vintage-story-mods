using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class LoreDiscoveryOld
{
	public string Code;

	public List<int> PieceIds;
}
