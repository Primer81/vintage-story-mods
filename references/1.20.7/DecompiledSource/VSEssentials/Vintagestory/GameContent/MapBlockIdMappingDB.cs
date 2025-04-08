using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MapBlockIdMappingDB
{
	public Dictionary<AssetLocation, int> BlockIndicesByBlockCode;
}
