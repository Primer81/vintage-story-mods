public class Packet_Cube
{
	public int Minx;

	public int Miny;

	public int Minz;

	public int Maxx;

	public int Maxy;

	public int Maxz;

	public const int MinxFieldID = 1;

	public const int MinyFieldID = 2;

	public const int MinzFieldID = 3;

	public const int MaxxFieldID = 4;

	public const int MaxyFieldID = 5;

	public const int MaxzFieldID = 6;

	public void SetMinx(int value)
	{
		Minx = value;
	}

	public void SetMiny(int value)
	{
		Miny = value;
	}

	public void SetMinz(int value)
	{
		Minz = value;
	}

	public void SetMaxx(int value)
	{
		Maxx = value;
	}

	public void SetMaxy(int value)
	{
		Maxy = value;
	}

	public void SetMaxz(int value)
	{
		Maxz = value;
	}

	internal void InitializeValues()
	{
	}
}
