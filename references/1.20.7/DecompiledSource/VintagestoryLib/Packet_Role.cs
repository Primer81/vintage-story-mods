public class Packet_Role
{
	public string Code;

	public int PrivilegeLevel;

	public const int CodeFieldID = 1;

	public const int PrivilegeLevelFieldID = 2;

	public void SetCode(string value)
	{
		Code = value;
	}

	public void SetPrivilegeLevel(int value)
	{
		PrivilegeLevel = value;
	}

	internal void InitializeValues()
	{
	}
}
