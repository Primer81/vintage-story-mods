public class Packet_MoveKeyChange
{
	public int Key;

	public int Down;

	public const int KeyFieldID = 1;

	public const int DownFieldID = 2;

	public void SetKey(int value)
	{
		Key = value;
	}

	public void SetDown(int value)
	{
		Down = value;
	}

	internal void InitializeValues()
	{
		Key = 0;
	}
}
