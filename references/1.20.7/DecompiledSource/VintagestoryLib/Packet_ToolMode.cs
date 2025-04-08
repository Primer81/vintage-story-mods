public class Packet_ToolMode
{
	public int Mode;

	public int X;

	public int Y;

	public int Z;

	public int Face;

	public int SelectionBoxIndex;

	public long HitX;

	public long HitY;

	public long HitZ;

	public const int ModeFieldID = 1;

	public const int XFieldID = 2;

	public const int YFieldID = 3;

	public const int ZFieldID = 4;

	public const int FaceFieldID = 5;

	public const int SelectionBoxIndexFieldID = 6;

	public const int HitXFieldID = 7;

	public const int HitYFieldID = 8;

	public const int HitZFieldID = 9;

	public void SetMode(int value)
	{
		Mode = value;
	}

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

	public void SetFace(int value)
	{
		Face = value;
	}

	public void SetSelectionBoxIndex(int value)
	{
		SelectionBoxIndex = value;
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

	internal void InitializeValues()
	{
	}
}
