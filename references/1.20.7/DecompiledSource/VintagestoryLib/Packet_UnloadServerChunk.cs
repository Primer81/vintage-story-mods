public class Packet_UnloadServerChunk
{
	public int[] X;

	public int XCount;

	public int XLength;

	public int[] Y;

	public int YCount;

	public int YLength;

	public int[] Z;

	public int ZCount;

	public int ZLength;

	public const int XFieldID = 1;

	public const int YFieldID = 2;

	public const int ZFieldID = 3;

	public int[] GetX()
	{
		return X;
	}

	public void SetX(int[] value, int count, int length)
	{
		X = value;
		XCount = count;
		XLength = length;
	}

	public void SetX(int[] value)
	{
		X = value;
		XCount = value.Length;
		XLength = value.Length;
	}

	public int GetXCount()
	{
		return XCount;
	}

	public void XAdd(int value)
	{
		if (XCount >= XLength)
		{
			if ((XLength *= 2) == 0)
			{
				XLength = 1;
			}
			int[] newArray = new int[XLength];
			for (int i = 0; i < XCount; i++)
			{
				newArray[i] = X[i];
			}
			X = newArray;
		}
		X[XCount++] = value;
	}

	public int[] GetY()
	{
		return Y;
	}

	public void SetY(int[] value, int count, int length)
	{
		Y = value;
		YCount = count;
		YLength = length;
	}

	public void SetY(int[] value)
	{
		Y = value;
		YCount = value.Length;
		YLength = value.Length;
	}

	public int GetYCount()
	{
		return YCount;
	}

	public void YAdd(int value)
	{
		if (YCount >= YLength)
		{
			if ((YLength *= 2) == 0)
			{
				YLength = 1;
			}
			int[] newArray = new int[YLength];
			for (int i = 0; i < YCount; i++)
			{
				newArray[i] = Y[i];
			}
			Y = newArray;
		}
		Y[YCount++] = value;
	}

	public int[] GetZ()
	{
		return Z;
	}

	public void SetZ(int[] value, int count, int length)
	{
		Z = value;
		ZCount = count;
		ZLength = length;
	}

	public void SetZ(int[] value)
	{
		Z = value;
		ZCount = value.Length;
		ZLength = value.Length;
	}

	public int GetZCount()
	{
		return ZCount;
	}

	public void ZAdd(int value)
	{
		if (ZCount >= ZLength)
		{
			if ((ZLength *= 2) == 0)
			{
				ZLength = 1;
			}
			int[] newArray = new int[ZLength];
			for (int i = 0; i < ZCount; i++)
			{
				newArray[i] = Z[i];
			}
			Z = newArray;
		}
		Z[ZCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
