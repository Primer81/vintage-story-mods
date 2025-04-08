public class Packet_PlayerMode
{
	public string PlayerUID;

	public int GameMode;

	public int MoveSpeed;

	public int FreeMove;

	public int NoClip;

	public int ViewDistance;

	public int PickingRange;

	public int FreeMovePlaneLock;

	public int ImmersiveFpMode;

	public int RenderMetaBlocks;

	public const int PlayerUIDFieldID = 1;

	public const int GameModeFieldID = 2;

	public const int MoveSpeedFieldID = 3;

	public const int FreeMoveFieldID = 4;

	public const int NoClipFieldID = 5;

	public const int ViewDistanceFieldID = 6;

	public const int PickingRangeFieldID = 7;

	public const int FreeMovePlaneLockFieldID = 8;

	public const int ImmersiveFpModeFieldID = 9;

	public const int RenderMetaBlocksFieldID = 10;

	public void SetPlayerUID(string value)
	{
		PlayerUID = value;
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

	public void SetViewDistance(int value)
	{
		ViewDistance = value;
	}

	public void SetPickingRange(int value)
	{
		PickingRange = value;
	}

	public void SetFreeMovePlaneLock(int value)
	{
		FreeMovePlaneLock = value;
	}

	public void SetImmersiveFpMode(int value)
	{
		ImmersiveFpMode = value;
	}

	public void SetRenderMetaBlocks(int value)
	{
		RenderMetaBlocks = value;
	}

	internal void InitializeValues()
	{
	}
}
