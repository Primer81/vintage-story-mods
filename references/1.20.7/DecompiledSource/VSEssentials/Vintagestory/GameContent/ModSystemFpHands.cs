using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class ModSystemFpHands : ModSystem
{
	public IShaderProgram fpModeItemShader;

	public IShaderProgram fpModeHandShader;

	private ICoreClientAPI capi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI capi)
	{
		this.capi = capi;
		capi.Event.ReloadShader += LoadShaders;
		LoadShaders();
	}

	public bool LoadShaders()
	{
		fpModeItemShader = createProg();
		capi.Shader.RegisterFileShaderProgram("standard", fpModeItemShader);
		fpModeHandShader = createProg();
		capi.Shader.RegisterFileShaderProgram("entityanimated", fpModeHandShader);
		bool ok = fpModeItemShader.Compile() && fpModeHandShader.Compile();
		if (ok)
		{
			foreach (UBORef value in fpModeHandShader.UBOs.Values)
			{
				value.Dispose();
			}
			fpModeHandShader.UBOs.Clear();
			fpModeHandShader.UBOs["Animation"] = capi.Render.CreateUBO(fpModeHandShader, 0, "Animation", GlobalConstants.MaxAnimatedElements * 16 * 4);
		}
		return ok;
	}

	private IShaderProgram createProg()
	{
		IShaderProgram shaderProgram = capi.Shader.NewShaderProgram();
		shaderProgram.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		shaderProgram.VertexShader.PrefixCode = "#define ALLOWDEPTHOFFSET 1\r\n";
		IShader vertexShader = shaderProgram.VertexShader;
		vertexShader.PrefixCode = vertexShader.PrefixCode + "#define MAXANIMATEDELEMENTS " + GlobalConstants.MaxAnimatedElements + "\r\n";
		shaderProgram.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		shaderProgram.FragmentShader.PrefixCode = "#define ALLOWDEPTHOFFSET 1\r\n";
		return shaderProgram;
	}
}
