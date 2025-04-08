using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramChunkliquid : ShaderProgram
{
	public int TerrainTex2D
	{
		set
		{
			BindTexture2D("terrainTex", value, 0);
		}
	}

	public int DepthTex2D
	{
		set
		{
			BindTexture2D("depthTex", value, 1);
		}
	}

	public Vec2f BlockTextureSize
	{
		set
		{
			Uniform("blockTextureSize", value);
		}
	}

	public Vec2f TextureAtlasSize
	{
		set
		{
			Uniform("textureAtlasSize", value);
		}
	}

	public float WaterFlowCounter
	{
		set
		{
			Uniform("waterFlowCounter", value);
		}
	}

	public Vec3f SunPosRel
	{
		set
		{
			Uniform("sunPosRel", value);
		}
	}

	public Vec3f SunColor
	{
		set
		{
			Uniform("sunColor", value);
		}
	}

	public Vec3f ReflectColor
	{
		set
		{
			Uniform("reflectColor", value);
		}
	}

	public float WaterWaveCounter
	{
		set
		{
			Uniform("waterWaveCounter", value);
		}
	}

	public float SunSpecularIntensity
	{
		set
		{
			Uniform("sunSpecularIntensity", value);
		}
	}

	public float DropletIntensity
	{
		set
		{
			Uniform("dropletIntensity", value);
		}
	}

	public float WindSpeed
	{
		set
		{
			Uniform("windSpeed", value);
		}
	}

	public float FlatFogDensity
	{
		set
		{
			Uniform("flatFogDensity", value);
		}
	}

	public float FlatFogStart
	{
		set
		{
			Uniform("flatFogStart", value);
		}
	}

	public float ViewDistance
	{
		set
		{
			Uniform("viewDistance", value);
		}
	}

	public float ViewDistanceLod0
	{
		set
		{
			Uniform("viewDistanceLod0", value);
		}
	}

	public float ZNear
	{
		set
		{
			Uniform("zNear", value);
		}
	}

	public float ZFar
	{
		set
		{
			Uniform("zFar", value);
		}
	}

	public Vec3f LightPosition
	{
		set
		{
			Uniform("lightPosition", value);
		}
	}

	public float ShadowIntensity
	{
		set
		{
			Uniform("shadowIntensity", value);
		}
	}

	public int ShadowMapFar2D
	{
		set
		{
			BindTexture2D("shadowMapFar", value, 2);
		}
	}

	public float ShadowMapWidthInv
	{
		set
		{
			Uniform("shadowMapWidthInv", value);
		}
	}

	public float ShadowMapHeightInv
	{
		set
		{
			Uniform("shadowMapHeightInv", value);
		}
	}

	public int ShadowMapNear2D
	{
		set
		{
			BindTexture2D("shadowMapNear", value, 3);
		}
	}

	public float WindWaveCounter
	{
		set
		{
			Uniform("windWaveCounter", value);
		}
	}

	public float GlitchStrength
	{
		set
		{
			Uniform("glitchStrength", value);
		}
	}

	public float[] FogSpheres
	{
		set
		{
			Uniform("fogSpheres", value.Length, value);
		}
	}

	public int FogSphereQuantity
	{
		set
		{
			Uniform("fogSphereQuantity", value);
		}
	}

	public int LiquidDepth2D
	{
		set
		{
			BindTexture2D("liquidDepth", value, 4);
		}
	}

	public float CameraUnderwater
	{
		set
		{
			Uniform("cameraUnderwater", value);
		}
	}

	public Vec2f FrameSize
	{
		set
		{
			Uniform("frameSize", value);
		}
	}

	public Vec4f WaterMurkColor
	{
		set
		{
			Uniform("waterMurkColor", value);
		}
	}

	public float WaterStillCounter
	{
		set
		{
			Uniform("waterStillCounter", value);
		}
	}

	public Vec4f RgbaFogIn
	{
		set
		{
			Uniform("rgbaFogIn", value);
		}
	}

	public Vec3f RgbaAmbientIn
	{
		set
		{
			Uniform("rgbaAmbientIn", value);
		}
	}

	public float FogDensityIn
	{
		set
		{
			Uniform("fogDensityIn", value);
		}
	}

	public float FogMinIn
	{
		set
		{
			Uniform("fogMinIn", value);
		}
	}

	public Vec3f Origin
	{
		set
		{
			Uniform("origin", value);
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

	public Vec3f PlayerViewVec
	{
		set
		{
			Uniform("playerViewVec", value);
		}
	}

	public Vec3f PlayerPosForFoam
	{
		set
		{
			Uniform("playerPosForFoam", value);
		}
	}

	public float ShadowRangeFar
	{
		set
		{
			Uniform("shadowRangeFar", value);
		}
	}

	public float[] ToShadowMapSpaceMatrixFar
	{
		set
		{
			UniformMatrix("toShadowMapSpaceMatrixFar", value);
		}
	}

	public float ShadowRangeNear
	{
		set
		{
			Uniform("shadowRangeNear", value);
		}
	}

	public float[] ToShadowMapSpaceMatrixNear
	{
		set
		{
			UniformMatrix("toShadowMapSpaceMatrixNear", value);
		}
	}

	public float GlitchStrengthFL
	{
		set
		{
			Uniform("glitchStrengthFL", value);
		}
	}

	public float NightVisionStrength
	{
		set
		{
			Uniform("nightVisionStrength", value);
		}
	}

	public int PointLightQuantity
	{
		set
		{
			Uniform("pointLightQuantity", value);
		}
	}

	public float TimeCounter
	{
		set
		{
			Uniform("timeCounter", value);
		}
	}

	public float WindWaveCounterHighFreq
	{
		set
		{
			Uniform("windWaveCounterHighFreq", value);
		}
	}

	public Vec3f Playerpos
	{
		set
		{
			Uniform("playerpos", value);
		}
	}

	public float GlobalWarpIntensity
	{
		set
		{
			Uniform("globalWarpIntensity", value);
		}
	}

	public float GlitchWaviness
	{
		set
		{
			Uniform("glitchWaviness", value);
		}
	}

	public float WindWaveIntensity
	{
		set
		{
			Uniform("windWaveIntensity", value);
		}
	}

	public float WaterWaveIntensity
	{
		set
		{
			Uniform("waterWaveIntensity", value);
		}
	}

	public int PerceptionEffectId
	{
		set
		{
			Uniform("perceptionEffectId", value);
		}
	}

	public float PerceptionEffectIntensity
	{
		set
		{
			Uniform("perceptionEffectIntensity", value);
		}
	}

	public float SeasonRel
	{
		set
		{
			Uniform("seasonRel", value);
		}
	}

	public float SeaLevel
	{
		set
		{
			Uniform("seaLevel", value);
		}
	}

	public float AtlasHeight
	{
		set
		{
			Uniform("atlasHeight", value);
		}
	}

	public float SeasonTemperature
	{
		set
		{
			Uniform("seasonTemperature", value);
		}
	}

	public void PointLightsArray(int count, float[] values)
	{
		Uniforms3("pointLights", count, values);
	}

	public void PointLightColorsArray(int count, float[] values)
	{
		Uniforms3("pointLightColors", count, values);
	}

	public void ColorMapRectsArray(int count, float[] values)
	{
		Uniforms4("colorMapRects", count, values);
	}
}
