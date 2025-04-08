namespace Vintagestory.API.Common;

/// A climate condition at a given position
public class ClimateCondition
{
	/// <summary>
	/// Between -20 and +40 degrees
	/// </summary>
	public float Temperature;

	/// <summary>
	/// If you read the now values, you can still get the world gen rain fall from this value. Between 0..1
	/// </summary>
	public float WorldgenRainfall;

	/// <summary>
	/// If you read the now values, you can still get the world gen temp from this value
	/// </summary>
	public float WorldGenTemperature;

	/// <summary>
	/// Nomalized value between 0..1. Static value determined on world generation
	/// </summary>
	public float GeologicActivity;

	/// <summary>
	/// Nomalized value between 0..1. When loading the now values, this is set to the current precipitation value, otherwise to "yearly averages" or the values generated during worldgen
	/// </summary>
	public float Rainfall;

	public float RainCloudOverlay;

	/// <summary>
	/// Nomalized value between 0..1
	/// </summary>
	public float Fertility;

	/// <summary>
	/// Nomalized value between 0..1
	/// </summary>
	public float ForestDensity;

	/// <summary>
	/// Nomalized value between 0..1
	/// </summary>
	public float ShrubDensity;

	public void SetLerped(ClimateCondition left, ClimateCondition right, float w)
	{
		Temperature = left.Temperature * (1f - w) + right.Temperature * w;
		Rainfall = left.Rainfall * (1f - w) + right.Rainfall * w;
		Fertility = left.Fertility * (1f - w) + right.Fertility * w;
		ForestDensity = left.ForestDensity * (1f - w) + right.ForestDensity * w;
		ShrubDensity = left.ShrubDensity * (1f - w) + right.ShrubDensity * w;
	}
}
