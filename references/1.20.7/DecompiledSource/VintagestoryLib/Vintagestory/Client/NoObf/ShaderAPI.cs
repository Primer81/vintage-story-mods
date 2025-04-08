using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

internal class ShaderAPI : IShaderAPI
{
	private ClientMain game;

	public ShaderAPI(ClientMain game)
	{
		this.game = game;
	}

	public IShaderProgram NewShaderProgram()
	{
		return new ShaderProgram();
	}

	public IShader NewShader(EnumShaderType shaderType)
	{
		return new Shader
		{
			Type = shaderType
		};
	}

	public IShaderProgram GetProgram(int renderPass)
	{
		return ShaderRegistry.getProgram(renderPass);
	}

	public IShaderProgram GetProgramByName(string name)
	{
		return ShaderRegistry.getProgramByName(name);
	}

	public bool IsGLSLVersionSupported(string minVersion)
	{
		return ShaderRegistry.IsGLSLVersionSupported(minVersion);
	}

	public int RegisterFileShaderProgram(string name, IShaderProgram program)
	{
		((ShaderProgram)program).LoadFromFile = true;
		return ShaderRegistry.RegisterShaderProgram(name, (ShaderProgram)program);
	}

	public int RegisterMemoryShaderProgram(string name, IShaderProgram program)
	{
		((ShaderProgram)program).LoadFromFile = false;
		return ShaderRegistry.RegisterShaderProgram(name, (ShaderProgram)program);
	}

	public bool ReloadShaders()
	{
		bool result = ShaderRegistry.ReloadShaders();
		ClientEventManager eventManager = game.eventManager;
		if (eventManager != null)
		{
			eventManager.TriggerReloadShaders();
			return result;
		}
		return result;
	}
}
