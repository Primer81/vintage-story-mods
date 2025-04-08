public class Packet_ClientIdentification
{
	public string MdProtocolVersion;

	public string Playername;

	public string MpToken;

	public string ServerPassword;

	public string PlayerUID;

	public int ViewDistance;

	public int RenderMetaBlocks;

	public string NetworkVersion;

	public string ShortGameVersion;

	public const int MdProtocolVersionFieldID = 1;

	public const int PlayernameFieldID = 2;

	public const int MpTokenFieldID = 3;

	public const int ServerPasswordFieldID = 4;

	public const int PlayerUIDFieldID = 6;

	public const int ViewDistanceFieldID = 7;

	public const int RenderMetaBlocksFieldID = 8;

	public const int NetworkVersionFieldID = 9;

	public const int ShortGameVersionFieldID = 10;

	public void SetMdProtocolVersion(string value)
	{
		MdProtocolVersion = value;
	}

	public void SetPlayername(string value)
	{
		Playername = value;
	}

	public void SetMpToken(string value)
	{
		MpToken = value;
	}

	public void SetServerPassword(string value)
	{
		ServerPassword = value;
	}

	public void SetPlayerUID(string value)
	{
		PlayerUID = value;
	}

	public void SetViewDistance(int value)
	{
		ViewDistance = value;
	}

	public void SetRenderMetaBlocks(int value)
	{
		RenderMetaBlocks = value;
	}

	public void SetNetworkVersion(string value)
	{
		NetworkVersion = value;
	}

	public void SetShortGameVersion(string value)
	{
		ShortGameVersion = value;
	}

	internal void InitializeValues()
	{
	}
}
