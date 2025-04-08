public class Packet_BlockSoundSet
{
	public string Walk;

	public string Break;

	public string Place;

	public string Hit;

	public string Inside;

	public string Ambient;

	public int AmbientBlockCount;

	public int AmbientSoundType;

	public int AmbientMaxDistanceMerge;

	public int[] ByToolTool;

	public int ByToolToolCount;

	public int ByToolToolLength;

	public Packet_BlockSoundSet[] ByToolSound;

	public int ByToolSoundCount;

	public int ByToolSoundLength;

	public const int WalkFieldID = 1;

	public const int BreakFieldID = 2;

	public const int PlaceFieldID = 3;

	public const int HitFieldID = 4;

	public const int InsideFieldID = 5;

	public const int AmbientFieldID = 6;

	public const int AmbientBlockCountFieldID = 9;

	public const int AmbientSoundTypeFieldID = 10;

	public const int AmbientMaxDistanceMergeFieldID = 11;

	public const int ByToolToolFieldID = 7;

	public const int ByToolSoundFieldID = 8;

	public void SetWalk(string value)
	{
		Walk = value;
	}

	public void SetBreak(string value)
	{
		Break = value;
	}

	public void SetPlace(string value)
	{
		Place = value;
	}

	public void SetHit(string value)
	{
		Hit = value;
	}

	public void SetInside(string value)
	{
		Inside = value;
	}

	public void SetAmbient(string value)
	{
		Ambient = value;
	}

	public void SetAmbientBlockCount(int value)
	{
		AmbientBlockCount = value;
	}

	public void SetAmbientSoundType(int value)
	{
		AmbientSoundType = value;
	}

	public void SetAmbientMaxDistanceMerge(int value)
	{
		AmbientMaxDistanceMerge = value;
	}

	public int[] GetByToolTool()
	{
		return ByToolTool;
	}

	public void SetByToolTool(int[] value, int count, int length)
	{
		ByToolTool = value;
		ByToolToolCount = count;
		ByToolToolLength = length;
	}

	public void SetByToolTool(int[] value)
	{
		ByToolTool = value;
		ByToolToolCount = value.Length;
		ByToolToolLength = value.Length;
	}

	public int GetByToolToolCount()
	{
		return ByToolToolCount;
	}

	public void ByToolToolAdd(int value)
	{
		if (ByToolToolCount >= ByToolToolLength)
		{
			if ((ByToolToolLength *= 2) == 0)
			{
				ByToolToolLength = 1;
			}
			int[] newArray = new int[ByToolToolLength];
			for (int i = 0; i < ByToolToolCount; i++)
			{
				newArray[i] = ByToolTool[i];
			}
			ByToolTool = newArray;
		}
		ByToolTool[ByToolToolCount++] = value;
	}

	public Packet_BlockSoundSet[] GetByToolSound()
	{
		return ByToolSound;
	}

	public void SetByToolSound(Packet_BlockSoundSet[] value, int count, int length)
	{
		ByToolSound = value;
		ByToolSoundCount = count;
		ByToolSoundLength = length;
	}

	public void SetByToolSound(Packet_BlockSoundSet[] value)
	{
		ByToolSound = value;
		ByToolSoundCount = value.Length;
		ByToolSoundLength = value.Length;
	}

	public int GetByToolSoundCount()
	{
		return ByToolSoundCount;
	}

	public void ByToolSoundAdd(Packet_BlockSoundSet value)
	{
		if (ByToolSoundCount >= ByToolSoundLength)
		{
			if ((ByToolSoundLength *= 2) == 0)
			{
				ByToolSoundLength = 1;
			}
			Packet_BlockSoundSet[] newArray = new Packet_BlockSoundSet[ByToolSoundLength];
			for (int i = 0; i < ByToolSoundCount; i++)
			{
				newArray[i] = ByToolSound[i];
			}
			ByToolSound = newArray;
		}
		ByToolSound[ByToolSoundCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
