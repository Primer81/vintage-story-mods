using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class TpLocations
{
	public TeleporterLocation ForLocation;

	public Dictionary<BlockPos, TeleporterLocation> Locations;
}
