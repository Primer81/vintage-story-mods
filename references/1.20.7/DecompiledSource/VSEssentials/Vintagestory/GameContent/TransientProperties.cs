namespace Vintagestory.GameContent;

public class TransientProperties
{
	public EnumTransientCondition Condition;

	public float InGameHours = 24f;

	public float WhenBelowTemperature = -999f;

	public float WhenAboveTemperature = 999f;

	public float ResetBelowTemperature = -999f;

	public float StopBelowTemperature = -999f;

	public string ConvertTo;

	public string ConvertFrom;
}
