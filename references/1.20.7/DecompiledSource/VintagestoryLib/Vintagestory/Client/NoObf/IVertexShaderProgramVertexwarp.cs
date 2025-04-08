using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public interface IVertexShaderProgramVertexwarp
{
	float TimeCounter { set; }

	float WindWaveCounter { set; }

	float WindWaveCounterHighFreq { set; }

	float WaterWaveCounter { set; }

	float WindSpeed { set; }

	Vec3f Playerpos { set; }

	float GlobalWarpIntensity { set; }

	float GlitchWaviness { set; }

	float WindWaveIntensity { set; }

	float WaterWaveIntensity { set; }

	int PerceptionEffectId { set; }

	float PerceptionEffectIntensity { set; }
}
