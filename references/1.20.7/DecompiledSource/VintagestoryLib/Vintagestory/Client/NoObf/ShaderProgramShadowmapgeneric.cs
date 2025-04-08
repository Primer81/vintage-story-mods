using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramShadowmapgeneric : ShaderProgram
{
	public int Tex2d2D
	{
		set
		{
			BindTexture2D("tex2d", value, 0);
		}
	}

	public Vec3f Origin
	{
		set
		{
			Uniform("origin", value);
		}
	}

	public float[] MvpMatrix
	{
		set
		{
			UniformMatrix("mvpMatrix", value);
		}
	}

	public float TimeCounter
	{
		set
		{
			Uniform("timeCounter", value);
		}
	}

	public float WindWaveCounter
	{
		set
		{
			Uniform("windWaveCounter", value);
		}
	}

	public float WindWaveCounterHighFreq
	{
		set
		{
			Uniform("windWaveCounterHighFreq", value);
		}
	}

	public float WaterWaveCounter
	{
		set
		{
			Uniform("waterWaveCounter", value);
		}
	}

	public float WindSpeed
	{
		set
		{
			Uniform("windSpeed", value);
		}
	}

	public Vec3f Playerpos
	{
		set
		{
			Uniform("playerpos", value);
		}
	}

	public float GlobalWarpIntensity
	{
		set
		{
			Uniform("globalWarpIntensity", value);
		}
	}

	public float GlitchWaviness
	{
		set
		{
			Uniform("glitchWaviness", value);
		}
	}

	public float WindWaveIntensity
	{
		set
		{
			Uniform("windWaveIntensity", value);
		}
	}

	public float WaterWaveIntensity
	{
		set
		{
			Uniform("waterWaveIntensity", value);
		}
	}

	public int PerceptionEffectId
	{
		set
		{
			Uniform("perceptionEffectId", value);
		}
	}

	public float PerceptionEffectIntensity
	{
		set
		{
			Uniform("perceptionEffectIntensity", value);
		}
	}
}
