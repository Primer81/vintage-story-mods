public class Packet_NutritionProperties
{
	public int FoodCategory;

	public int Saturation;

	public int Health;

	public byte[] EatenStack;

	public const int FoodCategoryFieldID = 1;

	public const int SaturationFieldID = 2;

	public const int HealthFieldID = 3;

	public const int EatenStackFieldID = 4;

	public void SetFoodCategory(int value)
	{
		FoodCategory = value;
	}

	public void SetSaturation(int value)
	{
		Saturation = value;
	}

	public void SetHealth(int value)
	{
		Health = value;
	}

	public void SetEatenStack(byte[] value)
	{
		EatenStack = value;
	}

	internal void InitializeValues()
	{
	}
}
