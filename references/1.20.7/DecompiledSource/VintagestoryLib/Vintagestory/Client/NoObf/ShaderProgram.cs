using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class ShaderProgram : ShaderProgramBase, IShaderProgram, IDisposable
{
	public Dictionary<int, string> attributes = new Dictionary<int, string>();

	public bool LoadFromFile = true;

	public override bool Compile()
	{
		bool ok = true;
		HashSet<string> uniformNames = new HashSet<string>();
		VertexShader?.EnsureVersionSupported();
		GeometryShader?.EnsureVersionSupported();
		FragmentShader?.EnsureVersionSupported();
		if (VertexShader != null)
		{
			ok = ok && VertexShader.Compile();
			collectUniformNames(VertexShader.Code, uniformNames);
		}
		if (FragmentShader != null)
		{
			ok = ok && FragmentShader.Compile();
			collectUniformNames(FragmentShader.Code, uniformNames);
		}
		if (GeometryShader != null)
		{
			ok = ok && GeometryShader.Compile();
			collectUniformNames(GeometryShader.Code, uniformNames);
		}
		ok = ok && ScreenManager.Platform.CreateShaderProgram(this);
		string notFoundUniforms = "";
		foreach (string uniformName in uniformNames)
		{
			uniformLocations[uniformName] = ScreenManager.Platform.GetUniformLocation(this, uniformName);
			if (uniformLocations[uniformName] == -1)
			{
				if (notFoundUniforms.Length > 0)
				{
					notFoundUniforms += ", ";
				}
				notFoundUniforms += uniformName;
			}
		}
		if (notFoundUniforms.Length > 0 && ScreenManager.Platform.GlDebugMode)
		{
			ScreenManager.Platform.Logger.Notification("Shader {0}: Uniform locations for variables {1} not found (or not used).", PassName, notFoundUniforms);
		}
		return ok;
	}

	private void collectUniformNames(string code, HashSet<string> list)
	{
		foreach (Match item in Regex.Matches(code, "(\\s|\\r\\n)uniform\\s*(?<type>float|int|ivec2|ivec3|ivec4|vec2|vec3|vec4|sampler2DShadow|sampler2D|samplerCube|mat3|mat4x3|mat4)\\s*(\\[[\\d\\w]+\\])?\\s*(?<var>[\\d\\w]+)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
		{
			string varname = item.Groups["var"].Value;
			list.Add(varname);
			if (item.Groups["type"].ToString().Contains("sampler"))
			{
				textureLocations[varname] = textureLocations.Count;
			}
		}
	}
}
