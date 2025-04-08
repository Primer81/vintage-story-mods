namespace Vintagestory.Client.NoObf;

public class ShaderProgramDebugdepthbuffer : ShaderProgram
{
	public int DepthSampler2D
	{
		set
		{
			BindTexture2D("depthSampler", value, 0);
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
