public class Packet_LandClaims
{
	public Packet_LandClaim[] Allclaims;

	public int AllclaimsCount;

	public int AllclaimsLength;

	public Packet_LandClaim[] Addclaims;

	public int AddclaimsCount;

	public int AddclaimsLength;

	public const int AllclaimsFieldID = 1;

	public const int AddclaimsFieldID = 2;

	public Packet_LandClaim[] GetAllclaims()
	{
		return Allclaims;
	}

	public void SetAllclaims(Packet_LandClaim[] value, int count, int length)
	{
		Allclaims = value;
		AllclaimsCount = count;
		AllclaimsLength = length;
	}

	public void SetAllclaims(Packet_LandClaim[] value)
	{
		Allclaims = value;
		AllclaimsCount = value.Length;
		AllclaimsLength = value.Length;
	}

	public int GetAllclaimsCount()
	{
		return AllclaimsCount;
	}

	public void AllclaimsAdd(Packet_LandClaim value)
	{
		if (AllclaimsCount >= AllclaimsLength)
		{
			if ((AllclaimsLength *= 2) == 0)
			{
				AllclaimsLength = 1;
			}
			Packet_LandClaim[] newArray = new Packet_LandClaim[AllclaimsLength];
			for (int i = 0; i < AllclaimsCount; i++)
			{
				newArray[i] = Allclaims[i];
			}
			Allclaims = newArray;
		}
		Allclaims[AllclaimsCount++] = value;
	}

	public Packet_LandClaim[] GetAddclaims()
	{
		return Addclaims;
	}

	public void SetAddclaims(Packet_LandClaim[] value, int count, int length)
	{
		Addclaims = value;
		AddclaimsCount = count;
		AddclaimsLength = length;
	}

	public void SetAddclaims(Packet_LandClaim[] value)
	{
		Addclaims = value;
		AddclaimsCount = value.Length;
		AddclaimsLength = value.Length;
	}

	public int GetAddclaimsCount()
	{
		return AddclaimsCount;
	}

	public void AddclaimsAdd(Packet_LandClaim value)
	{
		if (AddclaimsCount >= AddclaimsLength)
		{
			if ((AddclaimsLength *= 2) == 0)
			{
				AddclaimsLength = 1;
			}
			Packet_LandClaim[] newArray = new Packet_LandClaim[AddclaimsLength];
			for (int i = 0; i < AddclaimsCount; i++)
			{
				newArray[i] = Addclaims[i];
			}
			Addclaims = newArray;
		}
		Addclaims[AddclaimsCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
