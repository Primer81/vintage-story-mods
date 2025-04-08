using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherCloudConfig
{
	public NatFloat Brightness = NatFloat.createUniform(1f, 0f);

	public NatFloat HeightMul = NatFloat.createUniform(1f, 0f);

	public NatFloat BaseThickness;

	public NatFloat ThinCloudMode = NatFloat.createUniform(0f, 0f);

	public NatFloat UndulatingCloudMode = NatFloat.createUniform(0f, 0f);

	public NatFloat ThicknessMul = NatFloat.createUniform(1f, 0f);

	public NoiseConfig LocationalThickness;

	public NatFloat Opaqueness;
}
