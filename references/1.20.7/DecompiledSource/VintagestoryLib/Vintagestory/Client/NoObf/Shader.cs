using System;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class Shader : IShader
{
	private static string shaderVersionPattern = "\\#version (\\d+)";

	public int ShaderId;

	private string prefixCode = "";

	private string code = "";

	public EnumShaderType shaderType;

	internal string Filename = "";

	public EnumShaderType Type
	{
		get
		{
			return shaderType;
		}
		set
		{
			shaderType = value;
		}
	}

	public string PrefixCode
	{
		get
		{
			return prefixCode;
		}
		set
		{
			prefixCode = value;
		}
	}

	public string Code
	{
		get
		{
			return code;
		}
		set
		{
			code = value;
		}
	}

	public Shader()
	{
	}

	public Shader(EnumShaderType shaderType, string code, string filename)
	{
		this.shaderType = shaderType;
		this.code = code;
		Filename = filename;
	}

	public bool Compile()
	{
		return ScreenManager.Platform.CompileShader(this);
	}

	public void EnsureVersionSupported()
	{
		Match match = Regex.Match(code, shaderVersionPattern);
		if (match.Groups.Count > 1)
		{
			EnsureVersionSupported(match.Groups[1].Value, Filename);
		}
	}

	public Shader Clone()
	{
		return new Shader(shaderType, code, Filename)
		{
			prefixCode = prefixCode
		};
	}

	public static void EnsureVersionSupported(string versionUsed, string ownFilename)
	{
		string versionSupported = ScreenManager.Platform.GetGLShaderVersionString();
		string versionSupportedfiltered = Regex.Match(versionSupported, "(\\d\\.\\d+)").Groups[1].Value.Replace(".", "");
		int versionSupportedInt = 0;
		int.TryParse(versionSupportedfiltered, out versionSupportedInt);
		int versionUsedInt = 0;
		int.TryParse(versionUsed, out versionUsedInt);
		if (versionUsedInt > versionSupportedInt)
		{
			throw new NotSupportedException($"Your graphics card supports only OpenGL version {versionSupportedfiltered} ({versionSupported}), but OpenGL version {versionUsed} is required.\nPlease check if you have installed the latest version of your graphics card driver. If they are, your graphics card may be to old to play Vintage Story.(Note: In case of modded gameplay with modded shaders, the mod author may be able to lower the OpenGL version requirements)");
		}
	}
}
