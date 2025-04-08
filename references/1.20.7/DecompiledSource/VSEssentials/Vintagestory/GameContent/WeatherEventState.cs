using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class WeatherEventState
{
	public int Index;

	public float BaseStrength;

	public double ActiveUntilTotalHours;

	public float LightningRate;

	public float NearThunderRate;

	public float DistantThunderRate;

	public float LightningMinTemp;

	public EnumPrecipitationType PrecType = EnumPrecipitationType.Auto;

	public float ParticleSize;
}
