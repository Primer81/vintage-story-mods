public class Packet_ServerMapChunk
{
	public int ChunkX;

	public int ChunkZ;

	public int Ymax;

	public byte[] RainHeightMap;

	public byte[] TerrainHeightMap;

	public byte[] Structures;

	public byte[] Moddata;

	public const int ChunkXFieldID = 1;

	public const int ChunkZFieldID = 2;

	public const int YmaxFieldID = 3;

	public const int RainHeightMapFieldID = 5;

	public const int TerrainHeightMapFieldID = 7;

	public const int StructuresFieldID = 6;

	public const int ModdataFieldID = 8;

	public void SetChunkX(int value)
	{
		ChunkX = value;
	}

	public void SetChunkZ(int value)
	{
		ChunkZ = value;
	}

	public void SetYmax(int value)
	{
		Ymax = value;
	}

	public void SetRainHeightMap(byte[] value)
	{
		RainHeightMap = value;
	}

	public void SetTerrainHeightMap(byte[] value)
	{
		TerrainHeightMap = value;
	}

	public void SetStructures(byte[] value)
	{
		Structures = value;
	}

	public void SetModdata(byte[] value)
	{
		Moddata = value;
	}

	internal void InitializeValues()
	{
	}
}
