public class Packet_BlockDamage
{
	public int PosX;

	public int PosY;

	public int PosZ;

	public int Facing;

	public int Damage;

	public const int PosXFieldID = 1;

	public const int PosYFieldID = 2;

	public const int PosZFieldID = 3;

	public const int FacingFieldID = 4;

	public const int DamageFieldID = 5;

	public void SetPosX(int value)
	{
		PosX = value;
	}

	public void SetPosY(int value)
	{
		PosY = value;
	}

	public void SetPosZ(int value)
	{
		PosZ = value;
	}

	public void SetFacing(int value)
	{
		Facing = value;
	}

	public void SetDamage(int value)
	{
		Damage = value;
	}

	internal void InitializeValues()
	{
	}
}
