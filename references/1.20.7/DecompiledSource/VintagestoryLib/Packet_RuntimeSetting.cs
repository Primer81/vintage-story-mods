public class Packet_RuntimeSetting
{
	public int ImmersiveFpMode;

	public int ItemCollectMode;

	public const int ImmersiveFpModeFieldID = 1;

	public const int ItemCollectModeFieldID = 2;

	public void SetImmersiveFpMode(int value)
	{
		ImmersiveFpMode = value;
	}

	public void SetItemCollectMode(int value)
	{
		ItemCollectMode = value;
	}

	internal void InitializeValues()
	{
	}
}
