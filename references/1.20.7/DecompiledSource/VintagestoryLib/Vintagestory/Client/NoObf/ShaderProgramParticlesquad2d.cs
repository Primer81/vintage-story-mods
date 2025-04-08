namespace Vintagestory.Client.NoObf;

public class ShaderProgramParticlesquad2d : ShaderProgram
{
	public int ParticleTex2D
	{
		set
		{
			BindTexture2D("particleTex", value, 0);
		}
	}

	public int OitPass
	{
		set
		{
			Uniform("oitPass", value);
		}
	}

	public int WithTexture
	{
		set
		{
			Uniform("withTexture", value);
		}
	}

	public int HeldItemMode
	{
		set
		{
			Uniform("heldItemMode", value);
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
}
