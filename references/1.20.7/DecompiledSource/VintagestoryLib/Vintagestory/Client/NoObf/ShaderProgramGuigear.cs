namespace Vintagestory.Client.NoObf;

public class ShaderProgramGuigear : ShaderProgram
{
	public int Tex2d2D
	{
		set
		{
			BindTexture2D("tex2d", value, 0);
		}
	}

	public float GearCounter
	{
		set
		{
			Uniform("gearCounter", value);
		}
	}

	public float StabilityLevel
	{
		set
		{
			Uniform("stabilityLevel", value);
		}
	}

	public float ShadeYPos
	{
		set
		{
			Uniform("shadeYPos", value);
		}
	}

	public float HotbarYPos
	{
		set
		{
			Uniform("hotbarYPos", value);
		}
	}

	public float GearHeight
	{
		set
		{
			Uniform("gearHeight", value);
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
