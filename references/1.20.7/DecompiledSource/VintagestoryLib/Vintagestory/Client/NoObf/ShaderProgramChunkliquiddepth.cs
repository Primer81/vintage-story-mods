using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramChunkliquiddepth : ShaderProgram
{
	public Vec3f Origin
	{
		set
		{
			Uniform("origin", value);
		}
	}

	public float[] ProjectionMatrix
	{
		set
		{
			UniformMatrix("projectionMatrix", value);
		}
	}

	public float[] ModelViewMatrix
	{
		set
		{
			UniformMatrix("modelViewMatrix", value);
		}
	}

	public float ViewDistance
	{
		set
		{
			Uniform("viewDistance", value);
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
