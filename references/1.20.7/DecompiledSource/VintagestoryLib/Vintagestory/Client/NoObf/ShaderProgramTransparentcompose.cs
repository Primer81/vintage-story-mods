namespace Vintagestory.Client.NoObf;

public class ShaderProgramTransparentcompose : ShaderProgram
{
	public int Accumulation2D
	{
		set
		{
			BindTexture2D("accumulation", value, 0);
		}
	}

	public int Revealage2D
	{
		set
		{
			BindTexture2D("revealage", value, 1);
		}
	}

	public int InGlow2D
	{
		set
		{
			BindTexture2D("inGlow", value, 2);
		}
	}
}
