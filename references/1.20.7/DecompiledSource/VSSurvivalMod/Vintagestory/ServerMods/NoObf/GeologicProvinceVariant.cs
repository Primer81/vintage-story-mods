using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Vintagestory.ServerMods.NoObf;

[JsonObject(MemberSerialization.OptIn)]
public class GeologicProvinceVariant
{
	public int Index;

	public int ColorInt;

	[JsonProperty]
	public string Code;

	[JsonProperty]
	public string Hexcolor;

	[JsonProperty]
	public int Weight;

	[JsonProperty]
	public Dictionary<string, GeologicProvinceRockStrata> Rockstrata;

	public GeologicProvinceRockStrata[] RockStrataIndexed;

	public void init(int mapsizey)
	{
		float mul = (float)mapsizey / 256f;
		RockStrataIndexed = new GeologicProvinceRockStrata[Enum.GetValues(typeof(EnumRockGroup)).Length];
		foreach (object val in Enum.GetValues(typeof(EnumRockGroup)))
		{
			RockStrataIndexed[(int)val] = new GeologicProvinceRockStrata();
			if (Rockstrata.ContainsKey(val?.ToString() ?? ""))
			{
				GeologicProvinceRockStrata r = (RockStrataIndexed[(int)val] = Rockstrata[val?.ToString() ?? ""]);
				r.ScaledMaxThickness = mul * r.MaxThickness;
			}
		}
	}
}
