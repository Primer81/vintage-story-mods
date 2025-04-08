public class Packet_HighlightBlocks
{
	public int Mode;

	public int Shape;

	public byte[] Blocks;

	public int[] Colors;

	public int ColorsCount;

	public int ColorsLength;

	public int Slotid;

	public int Scale;

	public const int ModeFieldID = 1;

	public const int ShapeFieldID = 2;

	public const int BlocksFieldID = 3;

	public const int ColorsFieldID = 4;

	public const int SlotidFieldID = 5;

	public const int ScaleFieldID = 6;

	public void SetMode(int value)
	{
		Mode = value;
	}

	public void SetShape(int value)
	{
		Shape = value;
	}

	public void SetBlocks(byte[] value)
	{
		Blocks = value;
	}

	public int[] GetColors()
	{
		return Colors;
	}

	public void SetColors(int[] value, int count, int length)
	{
		Colors = value;
		ColorsCount = count;
		ColorsLength = length;
	}

	public void SetColors(int[] value)
	{
		Colors = value;
		ColorsCount = value.Length;
		ColorsLength = value.Length;
	}

	public int GetColorsCount()
	{
		return ColorsCount;
	}

	public void ColorsAdd(int value)
	{
		if (ColorsCount >= ColorsLength)
		{
			if ((ColorsLength *= 2) == 0)
			{
				ColorsLength = 1;
			}
			int[] newArray = new int[ColorsLength];
			for (int i = 0; i < ColorsCount; i++)
			{
				newArray[i] = Colors[i];
			}
			Colors = newArray;
		}
		Colors[ColorsCount++] = value;
	}

	public void SetSlotid(int value)
	{
		Slotid = value;
	}

	public void SetScale(int value)
	{
		Scale = value;
	}

	internal void InitializeValues()
	{
	}
}
