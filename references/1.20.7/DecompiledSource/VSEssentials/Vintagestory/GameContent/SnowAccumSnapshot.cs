using ProtoBuf;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[ProtoContract]
public class SnowAccumSnapshot
{
	[ProtoMember(1)]
	public double TotalHours;

	[ProtoMember(2)]
	public FloatDataMap3D SumTemperatureByRegionCorner;

	[ProtoMember(3)]
	public int Checks;

	[ProtoMember(4)]
	public FloatDataMap3D SnowAccumulationByRegionCorner;

	public float GetAvgTemperatureByRegionCorner(float x, float y, float z)
	{
		return SumTemperatureByRegionCorner.GetLerped(x, y, z) / (float)Checks;
	}

	public float GetAvgSnowAccumByRegionCorner(float x, float y, float z)
	{
		return SnowAccumulationByRegionCorner.GetLerped(x, y, z);
	}
}
