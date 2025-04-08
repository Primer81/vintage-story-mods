public class Packet_ServerChunk
{
	public byte[] Blocks;

	public byte[] Light;

	public byte[] LightSat;

	public byte[] Liquids;

	public int[] LightPositions;

	public int LightPositionsCount;

	public int LightPositionsLength;

	public int X;

	public int Y;

	public int Z;

	public Packet_Entity[] Entities;

	public int EntitiesCount;

	public int EntitiesLength;

	public Packet_BlockEntity[] BlockEntities;

	public int BlockEntitiesCount;

	public int BlockEntitiesLength;

	public byte[] Moddata;

	public int Empty;

	public int[] DecorsPos;

	public int DecorsPosCount;

	public int DecorsPosLength;

	public int[] DecorsIds;

	public int DecorsIdsCount;

	public int DecorsIdsLength;

	public int Compver;

	public const int BlocksFieldID = 1;

	public const int LightFieldID = 2;

	public const int LightSatFieldID = 3;

	public const int LiquidsFieldID = 15;

	public const int LightPositionsFieldID = 9;

	public const int XFieldID = 4;

	public const int YFieldID = 5;

	public const int ZFieldID = 6;

	public const int EntitiesFieldID = 7;

	public const int BlockEntitiesFieldID = 8;

	public const int ModdataFieldID = 10;

	public const int EmptyFieldID = 11;

	public const int DecorsPosFieldID = 12;

	public const int DecorsIdsFieldID = 13;

	public const int CompverFieldID = 14;

	public void SetBlocks(byte[] value)
	{
		Blocks = value;
	}

	public void SetLight(byte[] value)
	{
		Light = value;
	}

	public void SetLightSat(byte[] value)
	{
		LightSat = value;
	}

	public void SetLiquids(byte[] value)
	{
		Liquids = value;
	}

	public int[] GetLightPositions()
	{
		return LightPositions;
	}

	public void SetLightPositions(int[] value, int count, int length)
	{
		LightPositions = value;
		LightPositionsCount = count;
		LightPositionsLength = length;
	}

	public void SetLightPositions(int[] value)
	{
		LightPositions = value;
		LightPositionsCount = value.Length;
		LightPositionsLength = value.Length;
	}

	public int GetLightPositionsCount()
	{
		return LightPositionsCount;
	}

	public void LightPositionsAdd(int value)
	{
		if (LightPositionsCount >= LightPositionsLength)
		{
			if ((LightPositionsLength *= 2) == 0)
			{
				LightPositionsLength = 1;
			}
			int[] newArray = new int[LightPositionsLength];
			for (int i = 0; i < LightPositionsCount; i++)
			{
				newArray[i] = LightPositions[i];
			}
			LightPositions = newArray;
		}
		LightPositions[LightPositionsCount++] = value;
	}

	public void SetX(int value)
	{
		X = value;
	}

	public void SetY(int value)
	{
		Y = value;
	}

	public void SetZ(int value)
	{
		Z = value;
	}

	public Packet_Entity[] GetEntities()
	{
		return Entities;
	}

	public void SetEntities(Packet_Entity[] value, int count, int length)
	{
		Entities = value;
		EntitiesCount = count;
		EntitiesLength = length;
	}

	public void SetEntities(Packet_Entity[] value)
	{
		Entities = value;
		EntitiesCount = value.Length;
		EntitiesLength = value.Length;
	}

	public int GetEntitiesCount()
	{
		return EntitiesCount;
	}

	public void EntitiesAdd(Packet_Entity value)
	{
		if (EntitiesCount >= EntitiesLength)
		{
			if ((EntitiesLength *= 2) == 0)
			{
				EntitiesLength = 1;
			}
			Packet_Entity[] newArray = new Packet_Entity[EntitiesLength];
			for (int i = 0; i < EntitiesCount; i++)
			{
				newArray[i] = Entities[i];
			}
			Entities = newArray;
		}
		Entities[EntitiesCount++] = value;
	}

	public Packet_BlockEntity[] GetBlockEntities()
	{
		return BlockEntities;
	}

	public void SetBlockEntities(Packet_BlockEntity[] value, int count, int length)
	{
		BlockEntities = value;
		BlockEntitiesCount = count;
		BlockEntitiesLength = length;
	}

	public void SetBlockEntities(Packet_BlockEntity[] value)
	{
		BlockEntities = value;
		BlockEntitiesCount = value.Length;
		BlockEntitiesLength = value.Length;
	}

	public int GetBlockEntitiesCount()
	{
		return BlockEntitiesCount;
	}

	public void BlockEntitiesAdd(Packet_BlockEntity value)
	{
		if (BlockEntitiesCount >= BlockEntitiesLength)
		{
			if ((BlockEntitiesLength *= 2) == 0)
			{
				BlockEntitiesLength = 1;
			}
			Packet_BlockEntity[] newArray = new Packet_BlockEntity[BlockEntitiesLength];
			for (int i = 0; i < BlockEntitiesCount; i++)
			{
				newArray[i] = BlockEntities[i];
			}
			BlockEntities = newArray;
		}
		BlockEntities[BlockEntitiesCount++] = value;
	}

	public void SetModdata(byte[] value)
	{
		Moddata = value;
	}

	public void SetEmpty(int value)
	{
		Empty = value;
	}

	public int[] GetDecorsPos()
	{
		return DecorsPos;
	}

	public void SetDecorsPos(int[] value, int count, int length)
	{
		DecorsPos = value;
		DecorsPosCount = count;
		DecorsPosLength = length;
	}

	public void SetDecorsPos(int[] value)
	{
		DecorsPos = value;
		DecorsPosCount = value.Length;
		DecorsPosLength = value.Length;
	}

	public int GetDecorsPosCount()
	{
		return DecorsPosCount;
	}

	public void DecorsPosAdd(int value)
	{
		if (DecorsPosCount >= DecorsPosLength)
		{
			if ((DecorsPosLength *= 2) == 0)
			{
				DecorsPosLength = 1;
			}
			int[] newArray = new int[DecorsPosLength];
			for (int i = 0; i < DecorsPosCount; i++)
			{
				newArray[i] = DecorsPos[i];
			}
			DecorsPos = newArray;
		}
		DecorsPos[DecorsPosCount++] = value;
	}

	public int[] GetDecorsIds()
	{
		return DecorsIds;
	}

	public void SetDecorsIds(int[] value, int count, int length)
	{
		DecorsIds = value;
		DecorsIdsCount = count;
		DecorsIdsLength = length;
	}

	public void SetDecorsIds(int[] value)
	{
		DecorsIds = value;
		DecorsIdsCount = value.Length;
		DecorsIdsLength = value.Length;
	}

	public int GetDecorsIdsCount()
	{
		return DecorsIdsCount;
	}

	public void DecorsIdsAdd(int value)
	{
		if (DecorsIdsCount >= DecorsIdsLength)
		{
			if ((DecorsIdsLength *= 2) == 0)
			{
				DecorsIdsLength = 1;
			}
			int[] newArray = new int[DecorsIdsLength];
			for (int i = 0; i < DecorsIdsCount; i++)
			{
				newArray[i] = DecorsIds[i];
			}
			DecorsIds = newArray;
		}
		DecorsIds[DecorsIdsCount++] = value;
	}

	public void SetCompver(int value)
	{
		Compver = value;
	}

	internal void InitializeValues()
	{
	}
}
