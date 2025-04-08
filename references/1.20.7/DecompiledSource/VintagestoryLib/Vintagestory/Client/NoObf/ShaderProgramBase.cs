using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public abstract class ShaderProgramBase : IShaderProgram, IDisposable
{
	public static int shadowmapQuality;

	public static ShaderProgramBase CurrentShaderProgram;

	public int PassId;

	public int ProgramId;

	public string PassName;

	public Shader VertexShader;

	public Shader GeometryShader;

	public Shader FragmentShader;

	public Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

	public Dictionary<string, int> textureLocations = new Dictionary<string, int>();

	public OrderedDictionary<string, UBORef> ubos = new OrderedDictionary<string, UBORef>();

	public bool clampTToEdge;

	public HashSet<string> includes = new HashSet<string>();

	public Dictionary<string, int> customSamplers = new Dictionary<string, int>();

	private bool disposed;

	public bool Disposed => disposed;

	int IShaderProgram.PassId => PassId;

	string IShaderProgram.PassName => PassName;

	public bool ClampTexturesToEdge
	{
		get
		{
			return clampTToEdge;
		}
		set
		{
			clampTToEdge = value;
		}
	}

	IShader IShaderProgram.VertexShader
	{
		get
		{
			return VertexShader;
		}
		set
		{
			VertexShader = (Shader)value;
		}
	}

	IShader IShaderProgram.FragmentShader
	{
		get
		{
			return FragmentShader;
		}
		set
		{
			FragmentShader = (Shader)value;
		}
	}

	IShader IShaderProgram.GeometryShader
	{
		get
		{
			return GeometryShader;
		}
		set
		{
			GeometryShader = (Shader)value;
		}
	}

	public bool LoadError { get; set; }

	public OrderedDictionary<string, UBORef> UBOs => ubos;

	public string AssetDomain { get; set; }

	public void SetCustomSampler(string uniformName, bool isLinear)
	{
		int samplerId = ScreenManager.Platform.GenSampler(isLinear);
		customSamplers.Add(uniformName, samplerId);
	}

	public void Uniform(string uniformName, float value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform1(uniformLocations[uniformName], value);
	}

	public void Uniform(string uniformName, int count, float[] value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform1(uniformLocations[uniformName], count, value);
	}

	public void Uniform(string uniformName, int value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform1(uniformLocations[uniformName], value);
	}

	public void Uniform(string uniformName, Vec2f value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform2(uniformLocations[uniformName], value.X, value.Y);
	}

	public void Uniform(string uniformName, Vec3f value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform3(uniformLocations[uniformName], value.X, value.Y, value.Z);
	}

	public void Uniform(string uniformName, Vec3i value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform3(uniformLocations[uniformName], value.X, value.Y, value.Z);
	}

	public void Uniforms2(string uniformName, int count, float[] values)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform2(uniformLocations[uniformName], count, values);
	}

	public void Uniforms3(string uniformName, int count, float[] values)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform3(uniformLocations[uniformName], count, values);
	}

	public void Uniform(string uniformName, Vec4f value)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform4(uniformLocations[uniformName], value.X, value.Y, value.Z, value.W);
	}

	public void Uniforms4(string uniformName, int count, float[] values)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.Uniform4(uniformLocations[uniformName], count, values);
	}

	public void UniformMatrix(string uniformName, float[] matrix)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.UniformMatrix4(uniformLocations[uniformName], 1, transpose: false, matrix);
	}

	public void UniformMatrix(string uniformName, ref Matrix4 matrix)
	{
		if (CurrentShaderProgram?.ProgramId != ProgramId)
		{
			throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
		}
		GL.UniformMatrix4(uniformLocations[uniformName], transpose: false, ref matrix);
	}

	public bool HasUniform(string uniformName)
	{
		return uniformLocations.ContainsKey(uniformName);
	}

	public void BindTexture2D(string samplerName, int textureId, int textureNumber)
	{
		GL.Uniform1(uniformLocations[samplerName], textureNumber);
		GL.ActiveTexture((TextureUnit)(33984 + textureNumber));
		GL.BindTexture(TextureTarget.Texture2D, textureId);
		if (customSamplers.TryGetValue(samplerName, out var sampler))
		{
			GL.BindSampler(textureNumber, sampler);
		}
		if (clampTToEdge)
		{
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, Convert.ToInt32(TextureWrapMode.ClampToEdge));
		}
	}

	public void BindTexture2D(string samplerName, int textureId)
	{
		BindTexture2D(samplerName, textureId, textureLocations[samplerName]);
	}

	public void BindTextureCube(string samplerName, int textureId, int textureNumber)
	{
		GL.Uniform1(uniformLocations[samplerName], textureNumber);
		GL.ActiveTexture((TextureUnit)(33984 + textureNumber));
		GL.BindTexture(TextureTarget.TextureCubeMap, textureId);
		if (clampTToEdge)
		{
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, Convert.ToInt32(TextureWrapMode.ClampToEdge));
		}
	}

	public void UniformMatrices4x3(string uniformName, int count, float[] matrix)
	{
		GL.UniformMatrix4x3(uniformLocations[uniformName], count, transpose: false, matrix);
	}

	public void UniformMatrices(string uniformName, int count, float[] matrix)
	{
		GL.UniformMatrix4(uniformLocations[uniformName], count, transpose: false, matrix);
	}

	public void Use()
	{
		if (CurrentShaderProgram != null && CurrentShaderProgram != this)
		{
			throw new InvalidOperationException("Already a different shader (" + CurrentShaderProgram.PassName + ") in use!");
		}
		if (disposed)
		{
			throw new InvalidOperationException("Can't use a disposed shader!");
		}
		GL.UseProgram(ProgramId);
		CurrentShaderProgram = this;
		DefaultShaderUniforms shUniforms = ScreenManager.Platform.ShaderUniforms;
		if (includes.Contains("fogandlight.fsh"))
		{
			Uniform("zNear", shUniforms.ZNear);
			Uniform("zFar", shUniforms.ZFar);
			Uniform("lightPosition", shUniforms.LightPosition3D);
			Uniform("shadowIntensity", shUniforms.DropShadowIntensity);
			Uniform("glitchStrength", shUniforms.GlitchStrength);
			if (shadowmapQuality > 0)
			{
				FrameBufferRef farFb = ScreenManager.Platform.FrameBuffers[11];
				FrameBufferRef nearFb = ScreenManager.Platform.FrameBuffers[12];
				BindTexture2D("shadowMapFar", farFb.DepthTextureId);
				BindTexture2D("shadowMapNear", nearFb.DepthTextureId);
				Uniform("shadowMapWidthInv", 1f / (float)farFb.Width);
				Uniform("shadowMapHeightInv", 1f / (float)farFb.Height);
				Uniform("viewDistance", (float)ClientSettings.ViewDistance);
				Uniform("viewDistanceLod0", (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBias);
			}
		}
		if (includes.Contains("fogandlight.vsh"))
		{
			int fcnt = shUniforms.FogSphereQuantity;
			Uniform("fogSphereQuantity", fcnt);
			Uniform("fogSpheres", fcnt * 8, shUniforms.FogSpheres);
			int cnt = shUniforms.PointLightsCount;
			Uniform("pointLightQuantity", cnt);
			Uniforms3("pointLights", cnt, shUniforms.PointLights3);
			Uniforms3("pointLightColors", cnt, shUniforms.PointLightColors3);
			Uniform("flatFogDensity", shUniforms.FlagFogDensity);
			Uniform("flatFogStart", shUniforms.FlatFogStartYPos - shUniforms.PlayerPos.Y);
			Uniform("glitchStrengthFL", shUniforms.GlitchStrength);
			Uniform("viewDistance", (float)ClientSettings.ViewDistance);
			Uniform("viewDistanceLod0", (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBias);
			Uniform("nightVisionStrength", shUniforms.NightVisionStrength);
		}
		if (includes.Contains("shadowcoords.vsh"))
		{
			Uniform("shadowRangeNear", shUniforms.ShadowRangeNear);
			Uniform("shadowRangeFar", shUniforms.ShadowRangeFar);
			UniformMatrix("toShadowMapSpaceMatrixNear", shUniforms.ToShadowMapSpaceMatrixNear);
			UniformMatrix("toShadowMapSpaceMatrixFar", shUniforms.ToShadowMapSpaceMatrixFar);
		}
		if (includes.Contains("vertexwarp.vsh"))
		{
			Uniform("timeCounter", shUniforms.TimeCounter);
			Uniform("windWaveCounter", shUniforms.WindWaveCounter);
			Uniform("windWaveCounterHighFreq", shUniforms.WindWaveCounterHighFreq);
			Uniform("windSpeed", shUniforms.WindSpeed);
			Uniform("waterWaveCounter", shUniforms.WaterWaveCounter);
			Uniform("playerpos", shUniforms.PlayerPos);
			Uniform("globalWarpIntensity", shUniforms.GlobalWorldWarp);
			Uniform("glitchWaviness", shUniforms.GlitchWaviness);
			Uniform("windWaveIntensity", shUniforms.WindWaveIntensity);
			Uniform("waterWaveIntensity", shUniforms.WaterWaveIntensity);
			Uniform("perceptionEffectId", shUniforms.PerceptionEffectId);
			Uniform("perceptionEffectIntensity", shUniforms.PerceptionEffectIntensity);
		}
		if (includes.Contains("skycolor.fsh"))
		{
			Uniform("fogWaveCounter", shUniforms.FogWaveCounter);
			BindTexture2D("sky", shUniforms.SkyTextureId);
			BindTexture2D("glow", shUniforms.GlowTextureId);
			Uniform("sunsetMod", shUniforms.SunsetMod);
			Uniform("ditherSeed", shUniforms.DitherSeed);
			Uniform("horizontalResolution", shUniforms.FrameWidth);
			Uniform("playerToSealevelOffset", shUniforms.PlayerToSealevelOffset);
		}
		if (includes.Contains("colormap.vsh"))
		{
			Uniforms4("colorMapRects", 40, shUniforms.ColorMapRects4);
			Uniform("seasonRel", shUniforms.SeasonRel);
			Uniform("seaLevel", shUniforms.SeaLevel);
			Uniform("atlasHeight", shUniforms.BlockAtlasHeight);
			Uniform("seasonTemperature", shUniforms.SeasonTemperature);
		}
		if (includes.Contains("underwatereffects.fsh"))
		{
			FrameBufferRef fb = ScreenManager.Platform.FrameBuffers[5];
			BindTexture2D("liquidDepth", fb.DepthTextureId);
			Uniform("cameraUnderwater", shUniforms.CameraUnderwater);
			Uniform("waterMurkColor", shUniforms.WaterMurkColor);
			FrameBufferRef pfb = ScreenManager.Platform.FrameBuffers[0];
			Uniform("frameSize", new Vec2f(pfb.Width, pfb.Height));
		}
		if (this == ShaderPrograms.Gui)
		{
			ShaderPrograms.Gui.LightPosition = new Vec3f(1f, -1f, 0f).Normalize();
		}
		foreach (KeyValuePair<string, UBORef> ubo in ubos)
		{
			ubo.Value.Bind();
		}
	}

	public void Stop()
	{
		GL.UseProgram(0);
		for (int i = 0; i < customSamplers.Count; i++)
		{
			GL.BindSampler(i, 0);
		}
		foreach (KeyValuePair<string, UBORef> ubo in ubos)
		{
			ubo.Value.Unbind();
		}
		CurrentShaderProgram = null;
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}
		disposed = true;
		if (VertexShader != null)
		{
			GL.DetachShader(ProgramId, VertexShader.ShaderId);
			GL.DeleteShader(VertexShader.ShaderId);
		}
		if (FragmentShader != null)
		{
			GL.DetachShader(ProgramId, FragmentShader.ShaderId);
			GL.DeleteShader(FragmentShader.ShaderId);
		}
		if (GeometryShader != null)
		{
			GL.DetachShader(ProgramId, GeometryShader.ShaderId);
			GL.DeleteShader(GeometryShader.ShaderId);
		}
		foreach (KeyValuePair<string, int> customSampler in customSamplers)
		{
			GL.DeleteSampler(customSampler.Value);
		}
		GL.DeleteProgram(ProgramId);
	}

	public abstract bool Compile();
}
