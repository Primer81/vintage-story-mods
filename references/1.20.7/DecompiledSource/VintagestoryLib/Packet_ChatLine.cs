public class Packet_ChatLine
{
	public string Message;

	public int Groupid;

	public int ChatType;

	public string Data;

	public const int MessageFieldID = 1;

	public const int GroupidFieldID = 2;

	public const int ChatTypeFieldID = 3;

	public const int DataFieldID = 4;

	public void SetMessage(string value)
	{
		Message = value;
	}

	public void SetGroupid(int value)
	{
		Groupid = value;
	}

	public void SetChatType(int value)
	{
		ChatType = value;
	}

	public void SetData(string value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
