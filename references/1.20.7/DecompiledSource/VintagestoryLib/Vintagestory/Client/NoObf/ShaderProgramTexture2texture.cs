namespace Vintagestory.Client.NoObf;

public class ShaderProgramTexture2texture : ShaderProgram
{
	public int Tex2d2D
	{
		set
		{
			BindTexture2D("tex2d", value, 0);
		}
	}

	public float Texu
	{
		set
		{
			Uniform("texu", value);
		}
	}

	public float Texv
	{
		set
		{
			Uniform("texv", value);
		}
	}

	public float Texw
	{
		set
		{
			Uniform("texw", value);
		}
	}

	public float Texh
	{
		set
		{
			Uniform("texh", value);
		}
	}

	public float AlphaTest
	{
		set
		{
			Uniform("alphaTest", value);
		}
	}

	public float Xs
	{
		set
		{
			Uniform("xs", value);
		}
	}

	public float Ys
	{
		set
		{
			Uniform("ys", value);
		}
	}

	public float Width
	{
		set
		{
			Uniform("width", value);
		}
	}

	public float Height
	{
		set
		{
			Uniform("height", value);
		}
	}
}
