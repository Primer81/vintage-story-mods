public class Packet_ServerLevelProgress
{
	public int PercentComplete;

	public string Status;

	public int PercentCompleteSubitem;

	public const int PercentCompleteFieldID = 2;

	public const int StatusFieldID = 3;

	public const int PercentCompleteSubitemFieldID = 4;

	public void SetPercentComplete(int value)
	{
		PercentComplete = value;
	}

	public void SetStatus(string value)
	{
		Status = value;
	}

	public void SetPercentCompleteSubitem(int value)
	{
		PercentCompleteSubitem = value;
	}

	internal void InitializeValues()
	{
	}
}
