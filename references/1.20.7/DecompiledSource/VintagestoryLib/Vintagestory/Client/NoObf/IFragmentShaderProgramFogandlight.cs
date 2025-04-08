using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public interface IFragmentShaderProgramFogandlight
{
	float FlatFogDensity { set; }

	float FlatFogStart { set; }

	float ViewDistance { set; }

	float ViewDistanceLod0 { set; }

	float ZNear { set; }

	float ZFar { set; }

	Vec3f LightPosition { set; }

	float ShadowIntensity { set; }

	int ShadowMapFar2D { set; }

	float ShadowMapWidthInv { set; }

	float ShadowMapHeightInv { set; }

	int ShadowMapNear2D { set; }

	float WindWaveCounter { set; }

	float GlitchStrength { set; }
}
