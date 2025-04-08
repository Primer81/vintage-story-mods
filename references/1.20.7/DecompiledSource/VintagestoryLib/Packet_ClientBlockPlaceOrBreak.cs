public class Packet_ClientBlockPlaceOrBreak
{
	public int X;

	public int Y;

	public int Z;

	public int Mode;

	public int BlockType;

	public int OnBlockFace;

	public long HitX;

	public long HitY;

	public long HitZ;

	public int SelectionBoxIndex;

	public int DidOffset;

	public const int XFieldID = 1;

	public const int YFieldID = 2;

	public const int ZFieldID = 3;

	public const int ModeFieldID = 4;

	public const int BlockTypeFieldID = 5;

	public const int OnBlockFaceFieldID = 7;

	public const int HitXFieldID = 8;

	public const int HitYFieldID = 9;

	public const int HitZFieldID = 10;

	public const int SelectionBoxIndexFieldID = 11;

	public const int DidOffsetFieldID = 12;

	public void SetX(int value)
	{
		X = value;
	}

	public void SetY(int value)
	{
		Y = value;
	}

	public void SetZ(int value)
	{
		Z = value;
	}

	public void SetMode(int value)
	{
		Mode = value;
	}

	public void SetBlockType(int value)
	{
		BlockType = value;
	}

	public void SetOnBlockFace(int value)
	{
		OnBlockFace = value;
	}

	public void SetHitX(long value)
	{
		HitX = value;
	}

	public void SetHitY(long value)
	{
		HitY = value;
	}

	public void SetHitZ(long value)
	{
		HitZ = value;
	}

	public void SetSelectionBoxIndex(int value)
	{
		SelectionBoxIndex = value;
	}

	public void SetDidOffset(int value)
	{
		DidOffset = value;
	}

	internal void InitializeValues()
	{
		Mode = 0;
	}
}
