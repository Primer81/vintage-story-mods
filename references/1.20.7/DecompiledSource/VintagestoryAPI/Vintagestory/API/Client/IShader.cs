namespace Vintagestory.API.Client;

public interface IShader
{
	EnumShaderType Type { get; }

	/// <summary>
	/// If set, the shader registry will attach this bit of code to the beginning of the fragment shader file. Useful for setting stuff at runtime when using file shaders, e.g. via #define
	/// </summary>
	string PrefixCode { get; set; }

	/// <summary>
	/// Source code of the shader
	/// </summary>
	string Code { get; set; }
}
