public class Packet_PlayerGroups
{
	public Packet_PlayerGroup[] Groups;

	public int GroupsCount;

	public int GroupsLength;

	public const int GroupsFieldID = 1;

	public Packet_PlayerGroup[] GetGroups()
	{
		return Groups;
	}

	public void SetGroups(Packet_PlayerGroup[] value, int count, int length)
	{
		Groups = value;
		GroupsCount = count;
		GroupsLength = length;
	}

	public void SetGroups(Packet_PlayerGroup[] value)
	{
		Groups = value;
		GroupsCount = value.Length;
		GroupsLength = value.Length;
	}

	public int GetGroupsCount()
	{
		return GroupsCount;
	}

	public void GroupsAdd(Packet_PlayerGroup value)
	{
		if (GroupsCount >= GroupsLength)
		{
			if ((GroupsLength *= 2) == 0)
			{
				GroupsLength = 1;
			}
			Packet_PlayerGroup[] newArray = new Packet_PlayerGroup[GroupsLength];
			for (int i = 0; i < GroupsCount; i++)
			{
				newArray[i] = Groups[i];
			}
			Groups = newArray;
		}
		Groups[GroupsCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
