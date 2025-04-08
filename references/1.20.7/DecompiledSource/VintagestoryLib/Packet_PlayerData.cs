public class Packet_PlayerData
{
	public int ClientId;

	public long EntityId;

	public int GameMode;

	public int MoveSpeed;

	public int FreeMove;

	public int NoClip;

	public Packet_InventoryContents[] InventoryContents;

	public int InventoryContentsCount;

	public int InventoryContentsLength;

	public string PlayerUID;

	public int PickingRange;

	public int FreeMovePlaneLock;

	public int AreaSelectionMode;

	public string[] Privileges;

	public int PrivilegesCount;

	public int PrivilegesLength;

	public string PlayerName;

	public string Entitlements;

	public int HotbarSlotId;

	public int Deaths;

	public int Spawnx;

	public int Spawny;

	public int Spawnz;

	public string RoleCode;

	public const int ClientIdFieldID = 1;

	public const int EntityIdFieldID = 2;

	public const int GameModeFieldID = 3;

	public const int MoveSpeedFieldID = 4;

	public const int FreeMoveFieldID = 5;

	public const int NoClipFieldID = 6;

	public const int InventoryContentsFieldID = 7;

	public const int PlayerUIDFieldID = 8;

	public const int PickingRangeFieldID = 9;

	public const int FreeMovePlaneLockFieldID = 10;

	public const int AreaSelectionModeFieldID = 11;

	public const int PrivilegesFieldID = 12;

	public const int PlayerNameFieldID = 13;

	public const int EntitlementsFieldID = 14;

	public const int HotbarSlotIdFieldID = 15;

	public const int DeathsFieldID = 16;

	public const int SpawnxFieldID = 17;

	public const int SpawnyFieldID = 18;

	public const int SpawnzFieldID = 19;

	public const int RoleCodeFieldID = 20;

	public void SetClientId(int value)
	{
		ClientId = value;
	}

	public void SetEntityId(long value)
	{
		EntityId = value;
	}

	public void SetGameMode(int value)
	{
		GameMode = value;
	}

	public void SetMoveSpeed(int value)
	{
		MoveSpeed = value;
	}

	public void SetFreeMove(int value)
	{
		FreeMove = value;
	}

	public void SetNoClip(int value)
	{
		NoClip = value;
	}

	public Packet_InventoryContents[] GetInventoryContents()
	{
		return InventoryContents;
	}

	public void SetInventoryContents(Packet_InventoryContents[] value, int count, int length)
	{
		InventoryContents = value;
		InventoryContentsCount = count;
		InventoryContentsLength = length;
	}

	public void SetInventoryContents(Packet_InventoryContents[] value)
	{
		InventoryContents = value;
		InventoryContentsCount = value.Length;
		InventoryContentsLength = value.Length;
	}

	public int GetInventoryContentsCount()
	{
		return InventoryContentsCount;
	}

	public void InventoryContentsAdd(Packet_InventoryContents value)
	{
		if (InventoryContentsCount >= InventoryContentsLength)
		{
			if ((InventoryContentsLength *= 2) == 0)
			{
				InventoryContentsLength = 1;
			}
			Packet_InventoryContents[] newArray = new Packet_InventoryContents[InventoryContentsLength];
			for (int i = 0; i < InventoryContentsCount; i++)
			{
				newArray[i] = InventoryContents[i];
			}
			InventoryContents = newArray;
		}
		InventoryContents[InventoryContentsCount++] = value;
	}

	public void SetPlayerUID(string value)
	{
		PlayerUID = value;
	}

	public void SetPickingRange(int value)
	{
		PickingRange = value;
	}

	public void SetFreeMovePlaneLock(int value)
	{
		FreeMovePlaneLock = value;
	}

	public void SetAreaSelectionMode(int value)
	{
		AreaSelectionMode = value;
	}

	public string[] GetPrivileges()
	{
		return Privileges;
	}

	public void SetPrivileges(string[] value, int count, int length)
	{
		Privileges = value;
		PrivilegesCount = count;
		PrivilegesLength = length;
	}

	public void SetPrivileges(string[] value)
	{
		Privileges = value;
		PrivilegesCount = value.Length;
		PrivilegesLength = value.Length;
	}

	public int GetPrivilegesCount()
	{
		return PrivilegesCount;
	}

	public void PrivilegesAdd(string value)
	{
		if (PrivilegesCount >= PrivilegesLength)
		{
			if ((PrivilegesLength *= 2) == 0)
			{
				PrivilegesLength = 1;
			}
			string[] newArray = new string[PrivilegesLength];
			for (int i = 0; i < PrivilegesCount; i++)
			{
				newArray[i] = Privileges[i];
			}
			Privileges = newArray;
		}
		Privileges[PrivilegesCount++] = value;
	}

	public void SetPlayerName(string value)
	{
		PlayerName = value;
	}

	public void SetEntitlements(string value)
	{
		Entitlements = value;
	}

	public void SetHotbarSlotId(int value)
	{
		HotbarSlotId = value;
	}

	public void SetDeaths(int value)
	{
		Deaths = value;
	}

	public void SetSpawnx(int value)
	{
		Spawnx = value;
	}

	public void SetSpawny(int value)
	{
		Spawny = value;
	}

	public void SetSpawnz(int value)
	{
		Spawnz = value;
	}

	public void SetRoleCode(string value)
	{
		RoleCode = value;
	}

	internal void InitializeValues()
	{
	}
}
