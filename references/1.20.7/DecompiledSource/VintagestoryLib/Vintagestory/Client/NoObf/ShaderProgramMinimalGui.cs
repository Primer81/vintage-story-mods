using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramMinimalGui : ShaderProgram
{
	public int Tex2d2D
	{
		set
		{
			BindTexture2D("tex2d", value, 0);
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

	public ShaderProgramMinimalGui()
	{
		LoadFromFile = false;
		string minimalFrag = "#version 130\r\n#extension GL_ARB_explicit_attrib_location: enable\r\nin vec2 uv;\r\nlayout(location = 0) out vec4 outColor;\r\n\r\nuniform sampler2D tex2d;\r\n\r\nvoid main () {\r\n    outColor = texture(tex2d, uv);\r\n    if (outColor.a < 0.001) discard;\r\n}";
		string minimalVertex = "#version 130\r\n#extension GL_ARB_explicit_attrib_location: enable\r\nlayout(location = 0) in vec3 vertexPositionIn;\r\nlayout(location = 1) in vec2 uvIn;\r\n\r\nuniform mat4 projectionMatrix;\r\nuniform mat4 modelViewMatrix;\r\n\r\nout vec2 uv;\r\n\r\nvoid main(void)\r\n{\r\n\tuv = uvIn;\r\n\tgl_Position = projectionMatrix * modelViewMatrix * vec4(vertexPositionIn, 1.0);\r\n}";
		VertexShader = new Shader(EnumShaderType.VertexShader, minimalVertex, "hardcoded");
		FragmentShader = new Shader(EnumShaderType.FragmentShader, minimalFrag, "hardcoded");
	}
}
