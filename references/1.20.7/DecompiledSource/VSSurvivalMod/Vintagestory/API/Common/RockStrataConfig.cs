using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.ServerMods;

namespace Vintagestory.API.Common;

[JsonObject(MemberSerialization.OptIn)]
public class RockStrataConfig : WorldProperty<RockStratum>
{
	public Dictionary<EnumRockGroup, float> MaxThicknessPerGroup = new Dictionary<EnumRockGroup, float>();
}
