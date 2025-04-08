using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public interface IVertexShaderProgramFogandlight
{
	float FlatFogDensity { set; }

	float FlatFogStart { set; }

	float ViewDistance { set; }

	float ViewDistanceLod0 { set; }

	float GlitchStrengthFL { set; }

	float NightVisionStrength { set; }

	Vec3f PointLights { set; }

	Vec3f PointLightColors { set; }

	int PointLightQuantity { set; }
}
