public class BitTools
{
	public static bool IsPowerOfTwo(int x)
	{
		if (x > 0)
		{
			return (x & (x - 1)) == 0;
		}
		return false;
	}

	public static int NextPowerOfTwo(int x)
	{
		x--;
		x |= x >> 1;
		x |= x >> 2;
		x |= x >> 4;
		x |= x >> 8;
		x |= x >> 16;
		x++;
		return x;
	}
}
