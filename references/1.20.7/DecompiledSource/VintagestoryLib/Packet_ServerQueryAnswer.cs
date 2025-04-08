public class Packet_ServerQueryAnswer
{
	public string Name;

	public string MOTD;

	public int PlayerCount;

	public int MaxPlayers;

	public string GameMode;

	public bool Password;

	public string ServerVersion;

	public const int NameFieldID = 1;

	public const int MOTDFieldID = 2;

	public const int PlayerCountFieldID = 3;

	public const int MaxPlayersFieldID = 4;

	public const int GameModeFieldID = 5;

	public const int PasswordFieldID = 6;

	public const int ServerVersionFieldID = 7;

	public void SetName(string value)
	{
		Name = value;
	}

	public void SetMOTD(string value)
	{
		MOTD = value;
	}

	public void SetPlayerCount(int value)
	{
		PlayerCount = value;
	}

	public void SetMaxPlayers(int value)
	{
		MaxPlayers = value;
	}

	public void SetGameMode(string value)
	{
		GameMode = value;
	}

	public void SetPassword(bool value)
	{
		Password = value;
	}

	public void SetServerVersion(string value)
	{
		ServerVersion = value;
	}

	internal void InitializeValues()
	{
	}
}
