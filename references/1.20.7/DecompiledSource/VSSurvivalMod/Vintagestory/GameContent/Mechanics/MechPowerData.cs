using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent.Mechanics;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MechPowerData
{
	public Dictionary<long, MechanicalNetwork> networksById = new Dictionary<long, MechanicalNetwork>();

	public long nextNetworkId = 1L;

	public long tickNumber;
}
