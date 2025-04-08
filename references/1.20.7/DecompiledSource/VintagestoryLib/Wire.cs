public class Wire
{
	public const int Varint = 0;

	public const int Fixed64 = 1;

	public const int LengthDelimited = 2;

	public const int Fixed32 = 5;

	public static bool IsValid(int v)
	{
		if (v <= 2)
		{
			return true;
		}
		return v == 5;
	}
}
