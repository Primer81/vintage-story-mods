public class Packet_Roles
{
	public Packet_Role[] Roles;

	public int RolesCount;

	public int RolesLength;

	public const int RolesFieldID = 1;

	public Packet_Role[] GetRoles()
	{
		return Roles;
	}

	public void SetRoles(Packet_Role[] value, int count, int length)
	{
		Roles = value;
		RolesCount = count;
		RolesLength = length;
	}

	public void SetRoles(Packet_Role[] value)
	{
		Roles = value;
		RolesCount = value.Length;
		RolesLength = value.Length;
	}

	public int GetRolesCount()
	{
		return RolesCount;
	}

	public void RolesAdd(Packet_Role value)
	{
		if (RolesCount >= RolesLength)
		{
			if ((RolesLength *= 2) == 0)
			{
				RolesLength = 1;
			}
			Packet_Role[] newArray = new Packet_Role[RolesLength];
			for (int i = 0; i < RolesCount; i++)
			{
				newArray[i] = Roles[i];
			}
			Roles = newArray;
		}
		Roles[RolesCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
