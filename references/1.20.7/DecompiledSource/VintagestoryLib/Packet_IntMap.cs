public class Packet_IntMap
{
	public int[] Data;

	public int DataCount;

	public int DataLength;

	public int Size;

	public int TopLeftPadding;

	public int BottomRightPadding;

	public const int DataFieldID = 1;

	public const int SizeFieldID = 2;

	public const int TopLeftPaddingFieldID = 3;

	public const int BottomRightPaddingFieldID = 4;

	public int[] GetData()
	{
		return Data;
	}

	public void SetData(int[] value, int count, int length)
	{
		Data = value;
		DataCount = count;
		DataLength = length;
	}

	public void SetData(int[] value)
	{
		Data = value;
		DataCount = value.Length;
		DataLength = value.Length;
	}

	public int GetDataCount()
	{
		return DataCount;
	}

	public void DataAdd(int value)
	{
		if (DataCount >= DataLength)
		{
			if ((DataLength *= 2) == 0)
			{
				DataLength = 1;
			}
			int[] newArray = new int[DataLength];
			for (int i = 0; i < DataCount; i++)
			{
				newArray[i] = Data[i];
			}
			Data = newArray;
		}
		Data[DataCount++] = value;
	}

	public void SetSize(int value)
	{
		Size = value;
	}

	public void SetTopLeftPadding(int value)
	{
		TopLeftPadding = value;
	}

	public void SetBottomRightPadding(int value)
	{
		BottomRightPadding = value;
	}

	internal void InitializeValues()
	{
	}
}
