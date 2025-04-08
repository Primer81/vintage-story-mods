namespace Vintagestory.Client.NoObf;

public class ShaderProgramWoittest : ShaderProgram
{
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
