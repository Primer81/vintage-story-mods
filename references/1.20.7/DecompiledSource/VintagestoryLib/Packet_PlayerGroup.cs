public class Packet_PlayerGroup
{
	public int Uid;

	public string Owneruid;

	public string Name;

	public Packet_ChatLine[] Chathistory;

	public int ChathistoryCount;

	public int ChathistoryLength;

	public int Createdbyprivatemessage;

	public int Membership;

	public const int UidFieldID = 1;

	public const int OwneruidFieldID = 2;

	public const int NameFieldID = 3;

	public const int ChathistoryFieldID = 4;

	public const int CreatedbyprivatemessageFieldID = 5;

	public const int MembershipFieldID = 6;

	public void SetUid(int value)
	{
		Uid = value;
	}

	public void SetOwneruid(string value)
	{
		Owneruid = value;
	}

	public void SetName(string value)
	{
		Name = value;
	}

	public Packet_ChatLine[] GetChathistory()
	{
		return Chathistory;
	}

	public void SetChathistory(Packet_ChatLine[] value, int count, int length)
	{
		Chathistory = value;
		ChathistoryCount = count;
		ChathistoryLength = length;
	}

	public void SetChathistory(Packet_ChatLine[] value)
	{
		Chathistory = value;
		ChathistoryCount = value.Length;
		ChathistoryLength = value.Length;
	}

	public int GetChathistoryCount()
	{
		return ChathistoryCount;
	}

	public void ChathistoryAdd(Packet_ChatLine value)
	{
		if (ChathistoryCount >= ChathistoryLength)
		{
			if ((ChathistoryLength *= 2) == 0)
			{
				ChathistoryLength = 1;
			}
			Packet_ChatLine[] newArray = new Packet_ChatLine[ChathistoryLength];
			for (int i = 0; i < ChathistoryCount; i++)
			{
				newArray[i] = Chathistory[i];
			}
			Chathistory = newArray;
		}
		Chathistory[ChathistoryCount++] = value;
	}

	public void SetCreatedbyprivatemessage(int value)
	{
		Createdbyprivatemessage = value;
	}

	public void SetMembership(int value)
	{
		Membership = value;
	}

	internal void InitializeValues()
	{
	}
}
