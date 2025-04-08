public class Packet_EntityPosition
{
	public long EntityId;

	public long X;

	public long Y;

	public long Z;

	public int Yaw;

	public int Pitch;

	public int Roll;

	public int HeadYaw;

	public int HeadPitch;

	public int BodyYaw;

	public int Controls;

	public int Tick;

	public int PositionVersion;

	public long MotionX;

	public long MotionY;

	public long MotionZ;

	public bool Teleport;

	public const int EntityIdFieldID = 1;

	public const int XFieldID = 2;

	public const int YFieldID = 3;

	public const int ZFieldID = 4;

	public const int YawFieldID = 5;

	public const int PitchFieldID = 6;

	public const int RollFieldID = 7;

	public const int HeadYawFieldID = 8;

	public const int HeadPitchFieldID = 9;

	public const int BodyYawFieldID = 10;

	public const int ControlsFieldID = 11;

	public const int TickFieldID = 12;

	public const int PositionVersionFieldID = 13;

	public const int MotionXFieldID = 14;

	public const int MotionYFieldID = 15;

	public const int MotionZFieldID = 16;

	public const int TeleportFieldID = 17;

	public void SetEntityId(long value)
	{
		EntityId = value;
	}

	public void SetX(long value)
	{
		X = value;
	}

	public void SetY(long value)
	{
		Y = value;
	}

	public void SetZ(long value)
	{
		Z = value;
	}

	public void SetYaw(int value)
	{
		Yaw = value;
	}

	public void SetPitch(int value)
	{
		Pitch = value;
	}

	public void SetRoll(int value)
	{
		Roll = value;
	}

	public void SetHeadYaw(int value)
	{
		HeadYaw = value;
	}

	public void SetHeadPitch(int value)
	{
		HeadPitch = value;
	}

	public void SetBodyYaw(int value)
	{
		BodyYaw = value;
	}

	public void SetControls(int value)
	{
		Controls = value;
	}

	public void SetTick(int value)
	{
		Tick = value;
	}

	public void SetPositionVersion(int value)
	{
		PositionVersion = value;
	}

	public void SetMotionX(long value)
	{
		MotionX = value;
	}

	public void SetMotionY(long value)
	{
		MotionY = value;
	}

	public void SetMotionZ(long value)
	{
		MotionZ = value;
	}

	public void SetTeleport(bool value)
	{
		Teleport = value;
	}

	internal void InitializeValues()
	{
	}
}
