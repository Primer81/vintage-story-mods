public class Packet_ModId
{
	public string Modid;

	public string Name;

	public string Version;

	public string Networkversion;

	public bool RequiredOnClient;

	public const int ModidFieldID = 1;

	public const int NameFieldID = 2;

	public const int VersionFieldID = 3;

	public const int NetworkversionFieldID = 4;

	public const int RequiredOnClientFieldID = 5;

	public void SetModid(string value)
	{
		Modid = value;
	}

	public void SetName(string value)
	{
		Name = value;
	}

	public void SetVersion(string value)
	{
		Version = value;
	}

	public void SetNetworkversion(string value)
	{
		Networkversion = value;
	}

	public void SetRequiredOnClient(bool value)
	{
		RequiredOnClient = value;
	}

	internal void InitializeValues()
	{
	}
}
