namespace Vintagestory.Client.NoObf;

public class ShaderProgramBlit : ShaderProgram
{
	public int Scene2D
	{
		set
		{
			BindTexture2D("scene", value, 0);
		}
	}
}
