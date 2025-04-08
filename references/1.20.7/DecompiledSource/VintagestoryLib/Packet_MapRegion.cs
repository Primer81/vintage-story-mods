public class Packet_MapRegion
{
	public int RegionX;

	public int RegionZ;

	public Packet_IntMap LandformMap;

	public Packet_IntMap ForestMap;

	public Packet_IntMap ClimateMap;

	public Packet_IntMap GeologicProvinceMap;

	public Packet_GeneratedStructure[] GeneratedStructures;

	public int GeneratedStructuresCount;

	public int GeneratedStructuresLength;

	public byte[] Moddata;

	public const int RegionXFieldID = 1;

	public const int RegionZFieldID = 2;

	public const int LandformMapFieldID = 3;

	public const int ForestMapFieldID = 4;

	public const int ClimateMapFieldID = 5;

	public const int GeologicProvinceMapFieldID = 6;

	public const int GeneratedStructuresFieldID = 7;

	public const int ModdataFieldID = 8;

	public void SetRegionX(int value)
	{
		RegionX = value;
	}

	public void SetRegionZ(int value)
	{
		RegionZ = value;
	}

	public void SetLandformMap(Packet_IntMap value)
	{
		LandformMap = value;
	}

	public void SetForestMap(Packet_IntMap value)
	{
		ForestMap = value;
	}

	public void SetClimateMap(Packet_IntMap value)
	{
		ClimateMap = value;
	}

	public void SetGeologicProvinceMap(Packet_IntMap value)
	{
		GeologicProvinceMap = value;
	}

	public Packet_GeneratedStructure[] GetGeneratedStructures()
	{
		return GeneratedStructures;
	}

	public void SetGeneratedStructures(Packet_GeneratedStructure[] value, int count, int length)
	{
		GeneratedStructures = value;
		GeneratedStructuresCount = count;
		GeneratedStructuresLength = length;
	}

	public void SetGeneratedStructures(Packet_GeneratedStructure[] value)
	{
		GeneratedStructures = value;
		GeneratedStructuresCount = value.Length;
		GeneratedStructuresLength = value.Length;
	}

	public int GetGeneratedStructuresCount()
	{
		return GeneratedStructuresCount;
	}

	public void GeneratedStructuresAdd(Packet_GeneratedStructure value)
	{
		if (GeneratedStructuresCount >= GeneratedStructuresLength)
		{
			if ((GeneratedStructuresLength *= 2) == 0)
			{
				GeneratedStructuresLength = 1;
			}
			Packet_GeneratedStructure[] newArray = new Packet_GeneratedStructure[GeneratedStructuresLength];
			for (int i = 0; i < GeneratedStructuresCount; i++)
			{
				newArray[i] = GeneratedStructures[i];
			}
			GeneratedStructures = newArray;
		}
		GeneratedStructures[GeneratedStructuresCount++] = value;
	}

	public void SetModdata(byte[] value)
	{
		Moddata = value;
	}

	internal void InitializeValues()
	{
	}
}
