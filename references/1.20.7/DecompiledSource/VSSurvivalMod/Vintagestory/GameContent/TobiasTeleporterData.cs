using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class TobiasTeleporterData
{
	[ProtoMember(2)]
	public Dictionary<string, PlayerLocationData> PlayerLocations = new Dictionary<string, PlayerLocationData>();

	[ProtoMember(1)]
	public Vec3d TobiasTeleporterLocation { get; set; }
}
