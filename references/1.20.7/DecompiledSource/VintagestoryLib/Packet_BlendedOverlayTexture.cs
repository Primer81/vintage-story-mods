public class Packet_BlendedOverlayTexture
{
	public string Base;

	public int Mode;

	public const int BaseFieldID = 1;

	public const int ModeFieldID = 2;

	public void SetBase(string value)
	{
		Base = value;
	}

	public void SetMode(int value)
	{
		Mode = value;
	}

	internal void InitializeValues()
	{
	}
}
