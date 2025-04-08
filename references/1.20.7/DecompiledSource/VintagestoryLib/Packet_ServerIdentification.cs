public class Packet_ServerIdentification
{
	public string NetworkVersion;

	public string GameVersion;

	public string ServerName;

	public int MapSizeX;

	public int MapSizeY;

	public int MapSizeZ;

	public int RegionMapSizeX;

	public int RegionMapSizeY;

	public int RegionMapSizeZ;

	public int DisableShadows;

	public int PlayerAreaSize;

	public int Seed;

	public string PlayStyle;

	public int RequireRemapping;

	public Packet_ModId[] Mods;

	public int ModsCount;

	public int ModsLength;

	public byte[] WorldConfiguration;

	public string SavegameIdentifier;

	public string PlayListCode;

	public string[] ServerModIdBlackList;

	public int ServerModIdBlackListCount;

	public int ServerModIdBlackListLength;

	public string[] ServerModIdWhiteList;

	public int ServerModIdWhiteListCount;

	public int ServerModIdWhiteListLength;

	public const int NetworkVersionFieldID = 1;

	public const int GameVersionFieldID = 17;

	public const int ServerNameFieldID = 3;

	public const int MapSizeXFieldID = 7;

	public const int MapSizeYFieldID = 8;

	public const int MapSizeZFieldID = 9;

	public const int RegionMapSizeXFieldID = 21;

	public const int RegionMapSizeYFieldID = 22;

	public const int RegionMapSizeZFieldID = 23;

	public const int DisableShadowsFieldID = 11;

	public const int PlayerAreaSizeFieldID = 12;

	public const int SeedFieldID = 13;

	public const int PlayStyleFieldID = 16;

	public const int RequireRemappingFieldID = 18;

	public const int ModsFieldID = 19;

	public const int WorldConfigurationFieldID = 20;

	public const int SavegameIdentifierFieldID = 24;

	public const int PlayListCodeFieldID = 25;

	public const int ServerModIdBlackListFieldID = 26;

	public const int ServerModIdWhiteListFieldID = 27;

	public void SetNetworkVersion(string value)
	{
		NetworkVersion = value;
	}

	public void SetGameVersion(string value)
	{
		GameVersion = value;
	}

	public void SetServerName(string value)
	{
		ServerName = value;
	}

	public void SetMapSizeX(int value)
	{
		MapSizeX = value;
	}

	public void SetMapSizeY(int value)
	{
		MapSizeY = value;
	}

	public void SetMapSizeZ(int value)
	{
		MapSizeZ = value;
	}

	public void SetRegionMapSizeX(int value)
	{
		RegionMapSizeX = value;
	}

	public void SetRegionMapSizeY(int value)
	{
		RegionMapSizeY = value;
	}

	public void SetRegionMapSizeZ(int value)
	{
		RegionMapSizeZ = value;
	}

	public void SetDisableShadows(int value)
	{
		DisableShadows = value;
	}

	public void SetPlayerAreaSize(int value)
	{
		PlayerAreaSize = value;
	}

	public void SetSeed(int value)
	{
		Seed = value;
	}

	public void SetPlayStyle(string value)
	{
		PlayStyle = value;
	}

	public void SetRequireRemapping(int value)
	{
		RequireRemapping = value;
	}

	public Packet_ModId[] GetMods()
	{
		return Mods;
	}

	public void SetMods(Packet_ModId[] value, int count, int length)
	{
		Mods = value;
		ModsCount = count;
		ModsLength = length;
	}

	public void SetMods(Packet_ModId[] value)
	{
		Mods = value;
		ModsCount = value.Length;
		ModsLength = value.Length;
	}

	public int GetModsCount()
	{
		return ModsCount;
	}

	public void ModsAdd(Packet_ModId value)
	{
		if (ModsCount >= ModsLength)
		{
			if ((ModsLength *= 2) == 0)
			{
				ModsLength = 1;
			}
			Packet_ModId[] newArray = new Packet_ModId[ModsLength];
			for (int i = 0; i < ModsCount; i++)
			{
				newArray[i] = Mods[i];
			}
			Mods = newArray;
		}
		Mods[ModsCount++] = value;
	}

	public void SetWorldConfiguration(byte[] value)
	{
		WorldConfiguration = value;
	}

	public void SetSavegameIdentifier(string value)
	{
		SavegameIdentifier = value;
	}

	public void SetPlayListCode(string value)
	{
		PlayListCode = value;
	}

	public string[] GetServerModIdBlackList()
	{
		return ServerModIdBlackList;
	}

	public void SetServerModIdBlackList(string[] value, int count, int length)
	{
		ServerModIdBlackList = value;
		ServerModIdBlackListCount = count;
		ServerModIdBlackListLength = length;
	}

	public void SetServerModIdBlackList(string[] value)
	{
		ServerModIdBlackList = value;
		ServerModIdBlackListCount = value.Length;
		ServerModIdBlackListLength = value.Length;
	}

	public int GetServerModIdBlackListCount()
	{
		return ServerModIdBlackListCount;
	}

	public void ServerModIdBlackListAdd(string value)
	{
		if (ServerModIdBlackListCount >= ServerModIdBlackListLength)
		{
			if ((ServerModIdBlackListLength *= 2) == 0)
			{
				ServerModIdBlackListLength = 1;
			}
			string[] newArray = new string[ServerModIdBlackListLength];
			for (int i = 0; i < ServerModIdBlackListCount; i++)
			{
				newArray[i] = ServerModIdBlackList[i];
			}
			ServerModIdBlackList = newArray;
		}
		ServerModIdBlackList[ServerModIdBlackListCount++] = value;
	}

	public string[] GetServerModIdWhiteList()
	{
		return ServerModIdWhiteList;
	}

	public void SetServerModIdWhiteList(string[] value, int count, int length)
	{
		ServerModIdWhiteList = value;
		ServerModIdWhiteListCount = count;
		ServerModIdWhiteListLength = length;
	}

	public void SetServerModIdWhiteList(string[] value)
	{
		ServerModIdWhiteList = value;
		ServerModIdWhiteListCount = value.Length;
		ServerModIdWhiteListLength = value.Length;
	}

	public int GetServerModIdWhiteListCount()
	{
		return ServerModIdWhiteListCount;
	}

	public void ServerModIdWhiteListAdd(string value)
	{
		if (ServerModIdWhiteListCount >= ServerModIdWhiteListLength)
		{
			if ((ServerModIdWhiteListLength *= 2) == 0)
			{
				ServerModIdWhiteListLength = 1;
			}
			string[] newArray = new string[ServerModIdWhiteListLength];
			for (int i = 0; i < ServerModIdWhiteListCount; i++)
			{
				newArray[i] = ServerModIdWhiteList[i];
			}
			ServerModIdWhiteList = newArray;
		}
		ServerModIdWhiteList[ServerModIdWhiteListCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
