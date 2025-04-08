using System.Text;

public class ProtoPlatform
{
	public static byte[] StringToBytes(string s)
	{
		return Encoding.UTF8.GetBytes(s);
	}

	public static string BytesToString(byte[] bytes, int length)
	{
		return Encoding.UTF8.GetString(bytes);
	}

	public static int ArrayLength(byte[] a)
	{
		return a.Length;
	}

	public static byte IntToByte(int a)
	{
		return (byte)a;
	}

	public static int logical_right_shift(int x, int n)
	{
		return ~(~(-1 << n) << 32 - n) & (x >> n);
	}

	public static long logical_right_shift(long x, int n)
	{
		return ~(~(-1L << n) << 64 - n) & (x >> n);
	}
}
