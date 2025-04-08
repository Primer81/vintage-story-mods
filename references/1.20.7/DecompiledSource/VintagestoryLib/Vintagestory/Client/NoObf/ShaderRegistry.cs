using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ShaderRegistry
{
	private static string[] shaderNames;

	private static ShaderProgram[] shaderPrograms;

	private static Dictionary<string, string> includes;

	private static Dictionary<string, int> shaderIdsByName;

	private static int nextPassId;

	public static bool NormalView;

	private static void registerDefaultShaderPrograms()
	{
		RegisterShaderProgram(EnumShaderProgram.Autocamera, ShaderPrograms.Autocamera = new ShaderProgramAutocamera());
		RegisterShaderProgram(EnumShaderProgram.Bilateralblur, ShaderPrograms.Bilateralblur = new ShaderProgramBilateralblur());
		RegisterShaderProgram(EnumShaderProgram.Blit, ShaderPrograms.Blit = new ShaderProgramBlit());
		RegisterShaderProgram(EnumShaderProgram.Blockhighlights, ShaderPrograms.Blockhighlights = new ShaderProgramBlockhighlights());
		RegisterShaderProgram(EnumShaderProgram.Blur, ShaderPrograms.Blur = new ShaderProgramBlur());
		RegisterShaderProgram(EnumShaderProgram.Celestialobject, ShaderPrograms.Celestialobject = new ShaderProgramCelestialobject());
		RegisterShaderProgram(EnumShaderProgram.Chunkliquid, ShaderPrograms.Chunkliquid = new ShaderProgramChunkliquid());
		RegisterShaderProgram(EnumShaderProgram.Chunkliquiddepth, ShaderPrograms.Chunkliquiddepth = new ShaderProgramChunkliquiddepth());
		RegisterShaderProgram(EnumShaderProgram.Chunkopaque, ShaderPrograms.Chunkopaque = new ShaderProgramChunkopaque());
		RegisterShaderProgram(EnumShaderProgram.Chunktopsoil, ShaderPrograms.Chunktopsoil = new ShaderProgramChunktopsoil());
		RegisterShaderProgram(EnumShaderProgram.Chunktransparent, ShaderPrograms.Chunktransparent = new ShaderProgramChunktransparent());
		RegisterShaderProgram(EnumShaderProgram.Colorgrade, ShaderPrograms.Colorgrade = new ShaderProgramColorgrade());
		RegisterShaderProgram(EnumShaderProgram.Debugdepthbuffer, ShaderPrograms.Debugdepthbuffer = new ShaderProgramDebugdepthbuffer());
		RegisterShaderProgram(EnumShaderProgram.Decals, ShaderPrograms.Decals = new ShaderProgramDecals());
		RegisterShaderProgram(EnumShaderProgram.Entityanimated, ShaderPrograms.Entityanimated = new ShaderProgramEntityanimated());
		RegisterShaderProgram(EnumShaderProgram.Final, ShaderPrograms.Final = new ShaderProgramFinal());
		RegisterShaderProgram(EnumShaderProgram.Findbright, ShaderPrograms.Findbright = new ShaderProgramFindbright());
		RegisterShaderProgram(EnumShaderProgram.Godrays, ShaderPrograms.Godrays = new ShaderProgramGodrays());
		RegisterShaderProgram(EnumShaderProgram.Gui, ShaderPrograms.Gui = new ShaderProgramGui());
		RegisterShaderProgram(EnumShaderProgram.Guigear, ShaderPrograms.Guigear = new ShaderProgramGuigear());
		RegisterShaderProgram(EnumShaderProgram.Guitopsoil, ShaderPrograms.Guitopsoil = new ShaderProgramGuitopsoil());
		RegisterShaderProgram(EnumShaderProgram.Helditem, ShaderPrograms.Helditem = new ShaderProgramHelditem());
		RegisterShaderProgram(EnumShaderProgram.Luma, ShaderPrograms.Luma = new ShaderProgramLuma());
		RegisterShaderProgram(EnumShaderProgram.Nightsky, ShaderPrograms.Nightsky = new ShaderProgramNightsky());
		RegisterShaderProgram(EnumShaderProgram.Particlescube, ShaderPrograms.Particlescube = new ShaderProgramParticlescube());
		RegisterShaderProgram(EnumShaderProgram.Particlesquad, ShaderPrograms.Particlesquad = new ShaderProgramParticlesquad());
		RegisterShaderProgram(EnumShaderProgram.Particlesquad2d, ShaderPrograms.Particlesquad2d = new ShaderProgramParticlesquad2d());
		RegisterShaderProgram(EnumShaderProgram.Shadowmapentityanimated, ShaderPrograms.Shadowmapentityanimated = new ShaderProgramShadowmapentityanimated());
		RegisterShaderProgram(EnumShaderProgram.Shadowmapgeneric, ShaderPrograms.Shadowmapgeneric = new ShaderProgramShadowmapgeneric());
		RegisterShaderProgram(EnumShaderProgram.Sky, ShaderPrograms.Sky = new ShaderProgramSky());
		RegisterShaderProgram(EnumShaderProgram.Ssao, ShaderPrograms.Ssao = new ShaderProgramSsao());
		RegisterShaderProgram(EnumShaderProgram.Standard, ShaderPrograms.Standard = new ShaderProgramStandard());
		RegisterShaderProgram(EnumShaderProgram.Texture2texture, ShaderPrograms.Texture2texture = new ShaderProgramTexture2texture());
		RegisterShaderProgram(EnumShaderProgram.Transparentcompose, ShaderPrograms.Transparentcompose = new ShaderProgramTransparentcompose());
		RegisterShaderProgram(EnumShaderProgram.Wireframe, ShaderPrograms.Wireframe = new ShaderProgramWireframe());
		RegisterShaderProgram(EnumShaderProgram.Woittest, ShaderPrograms.Woittest = new ShaderProgramWoittest());
	}

	static ShaderRegistry()
	{
		shaderNames = new string[100];
		shaderPrograms = new ShaderProgram[100];
		includes = new Dictionary<string, string>();
		shaderIdsByName = new Dictionary<string, int>();
		nextPassId = 0;
		registerDefaultShaderPrograms();
	}

	public static int RegisterShaderProgram(string name, ShaderProgram program)
	{
		int passid = nextPassId;
		if (shaderIdsByName.ContainsKey(name))
		{
			passid = shaderIdsByName[name];
		}
		else
		{
			nextPassId++;
		}
		program.PassId = passid;
		program.PassName = name;
		shaderNames[passid] = name;
		shaderPrograms[passid] = program;
		shaderIdsByName[name] = passid;
		if (program.LoadFromFile)
		{
			LoadShader(program, EnumShaderType.VertexShader);
			LoadShader(program, EnumShaderType.FragmentShader);
			LoadShader(program, EnumShaderType.GeometryShader);
		}
		registerDefaultShaderCodePrefixes(program);
		return program.PassId;
	}

	public static void RegisterShaderProgram(EnumShaderProgram defaultProgram, ShaderProgram program)
	{
		program.PassId = (int)defaultProgram;
		program.PassName = defaultProgram.ToString().ToLowerInvariant();
		shaderNames[(int)defaultProgram] = defaultProgram.ToString().ToLowerInvariant();
		shaderPrograms[(int)defaultProgram] = program;
		nextPassId = Math.Max((int)(defaultProgram + 1), nextPassId);
	}

	public static ShaderProgram getProgram(EnumShaderProgram renderPass)
	{
		return shaderPrograms[(int)renderPass];
	}

	public static ShaderProgram getProgram(int renderPass)
	{
		return shaderPrograms[renderPass];
	}

	public static ShaderProgram getProgramByName(string shadername)
	{
		if (shaderIdsByName.TryGetValue(shadername, out var id))
		{
			return shaderPrograms[id];
		}
		return null;
	}

	public static void Load()
	{
		loadRegisteredShaderPrograms();
	}

	public static bool ReloadShaders()
	{
		ScreenManager.Platform.AssetManager.Reload(AssetCategory.shaders);
		ScreenManager.Platform.AssetManager.Reload(AssetCategory.shaderincludes);
		for (int i = 0; i < shaderPrograms.Length; i++)
		{
			if (shaderPrograms[i] != null)
			{
				shaderPrograms[i].Dispose();
				shaderPrograms[i] = null;
			}
		}
		registerDefaultShaderPrograms();
		return loadRegisteredShaderPrograms();
	}

	private static bool loadRegisteredShaderPrograms()
	{
		ScreenManager.Platform.Logger.Notification("Loading shaders...");
		bool ok = true;
		_ = ScreenManager.Platform.AssetManager;
		List<IAsset> many = ScreenManager.Platform.AssetManager.GetMany(AssetCategory.shaderincludes);
		many.AddRange(ScreenManager.Platform.AssetManager.GetMany(AssetCategory.shaders));
		foreach (IAsset asset in many)
		{
			includes[asset.Name] = asset.ToText();
		}
		for (int i = 0; i < nextPassId; i++)
		{
			ShaderProgram program = shaderPrograms[i];
			if (program != null)
			{
				new Dictionary<string, string>();
				_ = shaderNames[i];
				if (program.LoadFromFile)
				{
					LoadShader(program, EnumShaderType.VertexShader);
					LoadShader(program, EnumShaderType.FragmentShader);
					LoadShader(program, EnumShaderType.GeometryShader);
				}
				if (program.VertexShader == null)
				{
					ScreenManager.Platform.Logger.Error("Vertex shader missing for shader {0}. Will probably crash.", program.PassName);
				}
				if (program.FragmentShader == null)
				{
					ScreenManager.Platform.Logger.Error("Fragment shader missing for shader {0}. Will probably crash.", program.PassName);
				}
				registerDefaultShaderCodePrefixes(program);
				ok = shaderPrograms[i].Compile() && ok;
			}
		}
		ShaderPrograms.Chunkopaque.SetCustomSampler("terrainTex", isLinear: false);
		ShaderPrograms.Chunkopaque.SetCustomSampler("terrainTexLinear", isLinear: true);
		ShaderPrograms.Chunktopsoil.SetCustomSampler("terrainTex", isLinear: false);
		ShaderPrograms.Chunktopsoil.SetCustomSampler("terrainTexLinear", isLinear: true);
		return ok;
	}

	private static void LoadShader(ShaderProgram program, EnumShaderType shaderType)
	{
		AssetManager amgr = ScreenManager.Platform.AssetManager;
		string ext = ".unknown";
		switch (shaderType)
		{
		case EnumShaderType.VertexShader:
			ext = ".vsh";
			break;
		case EnumShaderType.FragmentShader:
			ext = ".fsh";
			break;
		case EnumShaderType.GeometryShader:
			ext = ".gsh";
			break;
		}
		AssetLocation loc = new AssetLocation(program.AssetDomain, "shaders/" + program.PassName + ext);
		IAsset asset = amgr.TryGet_BaseAssets(loc);
		if (asset == null)
		{
			if (shaderType != EnumShaderType.GeometryShader)
			{
				ScreenManager.Platform.Logger.Error("Shader file {0} not found. Stack trace:\n{1}", loc, Environment.StackTrace);
				program.LoadError = true;
			}
			return;
		}
		string code = HandleIncludes(program, asset.ToText());
		switch (shaderType)
		{
		case EnumShaderType.VertexShader:
			if (program.VertexShader == null)
			{
				program.VertexShader = new Shader(shaderType, code, program.PassName + ext);
				break;
			}
			program.VertexShader.Code = code;
			program.VertexShader.Type = shaderType;
			program.VertexShader.Filename = program.PassName + ext;
			break;
		case EnumShaderType.FragmentShader:
			if (program.FragmentShader == null)
			{
				program.FragmentShader = new Shader(shaderType, code, program.PassName + ext);
				break;
			}
			program.FragmentShader.Code = code;
			program.FragmentShader.Type = shaderType;
			program.FragmentShader.Filename = program.PassName + ext;
			break;
		case EnumShaderType.GeometryShader:
			if (program.GeometryShader == null)
			{
				program.GeometryShader = new Shader(shaderType, code, program.PassName + ext);
				break;
			}
			program.GeometryShader.Code = code;
			program.GeometryShader.Type = shaderType;
			program.GeometryShader.Filename = program.PassName + ext;
			break;
		}
	}

	private static void registerDefaultShaderCodePrefixes(ShaderProgram program)
	{
		Shader fragmentShader = program.FragmentShader;
		fragmentShader.PrefixCode = fragmentShader.PrefixCode + "#define FXAA " + (ClientSettings.FXAA ? 1 : 0) + "\r\n";
		Shader fragmentShader2 = program.FragmentShader;
		fragmentShader2.PrefixCode = fragmentShader2.PrefixCode + "#define SSAOLEVEL " + ClientSettings.SSAOQuality + "\r\n";
		Shader fragmentShader3 = program.FragmentShader;
		fragmentShader3.PrefixCode = fragmentShader3.PrefixCode + "#define NORMALVIEW " + (NormalView ? 1 : 0) + "\r\n";
		Shader fragmentShader4 = program.FragmentShader;
		fragmentShader4.PrefixCode = fragmentShader4.PrefixCode + "#define BLOOM " + (ClientSettings.Bloom ? 1 : 0) + "\r\n";
		Shader fragmentShader5 = program.FragmentShader;
		fragmentShader5.PrefixCode = fragmentShader5.PrefixCode + "#define GODRAYS " + ClientSettings.GodRayQuality + "\r\n";
		Shader fragmentShader6 = program.FragmentShader;
		fragmentShader6.PrefixCode = fragmentShader6.PrefixCode + "#define FOAMEFFECT " + (ClientSettings.LiquidFoamAndShinyEffect ? 1 : 0) + "\r\n";
		Shader fragmentShader7 = program.FragmentShader;
		fragmentShader7.PrefixCode = fragmentShader7.PrefixCode + "#define SHINYEFFECT " + (ClientSettings.LiquidFoamAndShinyEffect ? 1 : 0) + "\r\n";
		Shader fragmentShader8 = program.FragmentShader;
		fragmentShader8.PrefixCode = fragmentShader8.PrefixCode + "#define SHADOWQUALITY " + ClientSettings.ShadowMapQuality + "\r\n#define DYNLIGHTS " + ClientSettings.MaxDynamicLights + "\r\n";
		Shader vertexShader = program.VertexShader;
		vertexShader.PrefixCode = vertexShader.PrefixCode + "#define WAVINGSTUFF " + (ClientSettings.WavingFoliage ? 1 : 0) + "\r\n";
		Shader vertexShader2 = program.VertexShader;
		vertexShader2.PrefixCode = vertexShader2.PrefixCode + "#define FOAMEFFECT " + (ClientSettings.LiquidFoamAndShinyEffect ? 1 : 0) + "\r\n";
		Shader vertexShader3 = program.VertexShader;
		vertexShader3.PrefixCode = vertexShader3.PrefixCode + "#define SSAOLEVEL " + ClientSettings.SSAOQuality + "\r\n";
		Shader vertexShader4 = program.VertexShader;
		vertexShader4.PrefixCode = vertexShader4.PrefixCode + "#define NORMALVIEW " + (NormalView ? 1 : 0) + "\r\n";
		Shader vertexShader5 = program.VertexShader;
		vertexShader5.PrefixCode = vertexShader5.PrefixCode + "#define SHINYEFFECT " + (ClientSettings.LiquidFoamAndShinyEffect ? 1 : 0) + "\r\n";
		Shader vertexShader6 = program.VertexShader;
		vertexShader6.PrefixCode = vertexShader6.PrefixCode + "#define GODRAYS " + ClientSettings.GodRayQuality + "\r\n";
		Shader vertexShader7 = program.VertexShader;
		vertexShader7.PrefixCode = vertexShader7.PrefixCode + "#define MINBRIGHT " + ClientSettings.Minbrightness + "\r\n";
		fragmentShader8 = program.VertexShader;
		fragmentShader8.PrefixCode = fragmentShader8.PrefixCode + "#define SHADOWQUALITY " + ClientSettings.ShadowMapQuality + "\r\n#define DYNLIGHTS " + ClientSettings.MaxDynamicLights + "\r\n";
		Shader vertexShader8 = program.VertexShader;
		vertexShader8.PrefixCode = vertexShader8.PrefixCode + "#define MAXANIMATEDELEMENTS " + GlobalConstants.MaxAnimatedElements + "\r\n";
	}

	private static string HandleIncludes(ShaderProgram program, string shaderCode, HashSet<string> filenames = null)
	{
		if (filenames == null)
		{
			filenames = new HashSet<string>();
		}
		return Regex.Replace(shaderCode, "^#include\\s+(.*)", delegate(Match m)
		{
			string text = m.Groups[1].Value.Trim().ToLower();
			if (filenames.Contains(text))
			{
				return "";
			}
			filenames.Add(text);
			return InsertIncludedFile(program, text, filenames);
		}, RegexOptions.Multiline);
	}

	private static string InsertIncludedFile(ShaderProgram program, string filename, HashSet<string> filenames = null)
	{
		if (!includes.ContainsKey(filename))
		{
			ScreenManager.Platform.Logger.Warning("Error when loading shaders: Include file {0} not found. Ignoring.", filename);
			return "";
		}
		program.includes.Add(filename);
		string includedCode = includes[filename];
		return HandleIncludes(program, includedCode, filenames);
	}

	public static bool IsGLSLVersionSupported(string minVersion)
	{
		string s = Regex.Match(ScreenManager.Platform.GetGLShaderVersionString(), "(\\d\\.\\d+)").Groups[1].Value.Replace(".", "");
		int versionSupportedInt = 0;
		int.TryParse(s, out versionSupportedInt);
		int versionUsedInt = 0;
		int.TryParse(minVersion, out versionUsedInt);
		return versionUsedInt <= versionSupportedInt;
	}
}
