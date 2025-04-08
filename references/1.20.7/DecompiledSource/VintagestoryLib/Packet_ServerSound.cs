public class Packet_ServerSound
{
	public string Name;

	public int X;

	public int Y;

	public int Z;

	public int Pitch;

	public int Range;

	public int Volume;

	public int SoundType;

	public const int NameFieldID = 1;

	public const int XFieldID = 2;

	public const int YFieldID = 3;

	public const int ZFieldID = 4;

	public const int PitchFieldID = 5;

	public const int RangeFieldID = 6;

	public const int VolumeFieldID = 7;

	public const int SoundTypeFieldID = 8;

	public void SetName(string value)
	{
		Name = value;
	}

	public void SetX(int value)
	{
		X = value;
	}

	public void SetY(int value)
	{
		Y = value;
	}

	public void SetZ(int value)
	{
		Z = value;
	}

	public void SetPitch(int value)
	{
		Pitch = value;
	}

	public void SetRange(int value)
	{
		Range = value;
	}

	public void SetVolume(int value)
	{
		Volume = value;
	}

	public void SetSoundType(int value)
	{
		SoundType = value;
	}

	internal void InitializeValues()
	{
	}
}
