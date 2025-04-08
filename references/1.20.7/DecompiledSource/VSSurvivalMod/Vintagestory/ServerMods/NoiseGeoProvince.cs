using System.Globalization;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class NoiseGeoProvince : NoiseBase
{
	public static GeologicProvinces provinces;

	private int weightSum;

	public NoiseGeoProvince(long seed, ICoreServerAPI api)
		: base(seed)
	{
		provinces = api.Assets.Get("worldgen/geologicprovinces.json").ToObject<GeologicProvinces>();
		int mapsizey = api.WorldManager.MapSizeY;
		for (int i = 0; i < provinces.Variants.Length; i++)
		{
			provinces.Variants[i].Index = i;
			provinces.Variants[i].ColorInt = int.Parse(provinces.Variants[i].Hexcolor.TrimStart('#'), NumberStyles.HexNumber);
			weightSum += provinces.Variants[i].Weight;
			provinces.Variants[i].init(mapsizey);
		}
	}

	public int GetProvinceIndexAt(int xpos, int zpos)
	{
		InitPositionSeed(xpos, zpos);
		int rand = NextInt(weightSum);
		int i;
		for (i = 0; i < provinces.Variants.Length; i++)
		{
			rand -= provinces.Variants[i].Weight;
			if (rand <= 0)
			{
				return provinces.Variants[i].Index;
			}
		}
		return provinces.Variants[i].Index;
	}
}
