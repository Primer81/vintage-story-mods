public class Packet_CombustibleProperties
{
	public int BurnTemperature;

	public int BurnDuration;

	public int HeatResistance;

	public int MeltingPoint;

	public int MeltingDuration;

	public byte[] SmeltedStack;

	public int SmeltedRatio;

	public int RequiresContainer;

	public int MeltingType;

	public int MaxTemperature;

	public const int BurnTemperatureFieldID = 1;

	public const int BurnDurationFieldID = 2;

	public const int HeatResistanceFieldID = 3;

	public const int MeltingPointFieldID = 4;

	public const int MeltingDurationFieldID = 5;

	public const int SmeltedStackFieldID = 6;

	public const int SmeltedRatioFieldID = 7;

	public const int RequiresContainerFieldID = 8;

	public const int MeltingTypeFieldID = 9;

	public const int MaxTemperatureFieldID = 10;

	public void SetBurnTemperature(int value)
	{
		BurnTemperature = value;
	}

	public void SetBurnDuration(int value)
	{
		BurnDuration = value;
	}

	public void SetHeatResistance(int value)
	{
		HeatResistance = value;
	}

	public void SetMeltingPoint(int value)
	{
		MeltingPoint = value;
	}

	public void SetMeltingDuration(int value)
	{
		MeltingDuration = value;
	}

	public void SetSmeltedStack(byte[] value)
	{
		SmeltedStack = value;
	}

	public void SetSmeltedRatio(int value)
	{
		SmeltedRatio = value;
	}

	public void SetRequiresContainer(int value)
	{
		RequiresContainer = value;
	}

	public void SetMeltingType(int value)
	{
		MeltingType = value;
	}

	public void SetMaxTemperature(int value)
	{
		MaxTemperature = value;
	}

	internal void InitializeValues()
	{
	}
}
