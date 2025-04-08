using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramShadowmapentityanimated : ShaderProgram
{
	public int EntityTex2D
	{
		set
		{
			BindTexture2D("entityTex", value, 0);
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

	public int AddRenderFlags
	{
		set
		{
			Uniform("addRenderFlags", value);
		}
	}

	public override bool Compile()
	{
		bool num = base.Compile();
		if (num)
		{
			initUbos();
		}
		return num;
	}

	public void initUbos()
	{
		foreach (UBORef value in ubos.Values)
		{
			value.Dispose();
		}
		ubos.Clear();
		ubos["Animation"] = ScreenManager.Platform.CreateUBO(ProgramId, 0, "Animation", GlobalConstants.MaxAnimatedElements * 16 * 4);
	}
}
