public struct Key
{
	public int key;

	public readonly int Field => key >> 3;

	public readonly int WireType => key % 8;

	public Key(byte field, byte wiretype)
	{
		key = (field << 3) + wiretype;
	}

	public static implicit operator int(Key a)
	{
		return a.key;
	}

	public static Key Create(int firstByte, int secondByte)
	{
		Key result = default(Key);
		result.key = (secondByte << 7) | (firstByte & 0x7F);
		return result;
	}

	public static Key Create(int n)
	{
		Key result = default(Key);
		result.key = n;
		return result;
	}
}
