public class Packet_HeldSoundSet
{
	public string Idle;

	public string Equip;

	public string Unequip;

	public string Attack;

	public string InvPickup;

	public string InvPlace;

	public const int IdleFieldID = 1;

	public const int EquipFieldID = 2;

	public const int UnequipFieldID = 3;

	public const int AttackFieldID = 4;

	public const int InvPickupFieldID = 5;

	public const int InvPlaceFieldID = 6;

	public void SetIdle(string value)
	{
		Idle = value;
	}

	public void SetEquip(string value)
	{
		Equip = value;
	}

	public void SetUnequip(string value)
	{
		Unequip = value;
	}

	public void SetAttack(string value)
	{
		Attack = value;
	}

	public void SetInvPickup(string value)
	{
		InvPickup = value;
	}

	public void SetInvPlace(string value)
	{
		InvPlace = value;
	}

	internal void InitializeValues()
	{
	}
}
