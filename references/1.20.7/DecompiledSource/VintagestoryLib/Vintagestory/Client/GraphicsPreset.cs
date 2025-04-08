using System.Collections.Generic;

namespace Vintagestory.Client;

public class GraphicsPreset
{
	public static List<GraphicsPreset> Presets = new List<GraphicsPreset>();

	public int PresetId;

	public string Langcode;

	public int ViewDistance;

	public bool SmoothLight;

	public bool FXAA;

	public int SSAO;

	public bool WavingFoliage;

	public bool LiquidFoamEffect;

	public bool Bloom;

	public bool GodRays;

	public int ShadowMapQuality;

	public int ParticleLevel;

	public int DynamicLights;

	public float Resolution;

	public int MaxFps;

	public float LodBiasFar = 0.66f;

	public static GraphicsPreset Minimum = new GraphicsPreset("preset-minimum")
	{
		ViewDistance = 32,
		ParticleLevel = 0,
		Resolution = 0.25f,
		DynamicLights = 0,
		MaxFps = 25,
		WavingFoliage = false
	};

	public static GraphicsPreset Pathetic = new GraphicsPreset("preset-pathetic")
	{
		ViewDistance = 32,
		Resolution = 0.5f,
		ParticleLevel = 10,
		SmoothLight = true,
		WavingFoliage = false,
		DynamicLights = 1,
		MaxFps = 33
	};

	public static GraphicsPreset UltraLow = new GraphicsPreset("preset-ultralow")
	{
		ViewDistance = 64,
		Resolution = 0.75f,
		ParticleLevel = 25,
		SmoothLight = true,
		WavingFoliage = true,
		DynamicLights = 5,
		MaxFps = 33
	};

	public static GraphicsPreset VeryLow = new GraphicsPreset("preset-verylow")
	{
		ViewDistance = 128,
		Resolution = 1f,
		ParticleLevel = 50,
		SmoothLight = true,
		WavingFoliage = true,
		DynamicLights = 5,
		MaxFps = 33
	};

	public static GraphicsPreset Low = new GraphicsPreset("preset-low")
	{
		ViewDistance = 160,
		Resolution = 1f,
		ParticleLevel = 100,
		SmoothLight = true,
		WavingFoliage = true,
		DynamicLights = 10,
		FXAA = true,
		MaxFps = 36
	};

	public static GraphicsPreset Medium = new GraphicsPreset("preset-medium")
	{
		ViewDistance = 196,
		Resolution = 1f,
		ParticleLevel = 100,
		SmoothLight = true,
		WavingFoliage = true,
		DynamicLights = 10,
		LiquidFoamEffect = true,
		FXAA = true,
		Bloom = true,
		MaxFps = 60
	};

	public static GraphicsPreset High = new GraphicsPreset("preset-high")
	{
		ViewDistance = 256,
		Resolution = 1f,
		ParticleLevel = 100,
		SmoothLight = true,
		WavingFoliage = true,
		LiquidFoamEffect = true,
		DynamicLights = 25,
		FXAA = true,
		Bloom = true,
		ShadowMapQuality = 1,
		SSAO = 1,
		MaxFps = 75
	};

	public static GraphicsPreset VeryHigh = new GraphicsPreset("preset-veryhigh")
	{
		ViewDistance = 384,
		Resolution = 1f,
		ParticleLevel = 100,
		SSAO = 1,
		SmoothLight = true,
		WavingFoliage = true,
		LiquidFoamEffect = true,
		DynamicLights = 50,
		FXAA = true,
		Bloom = true,
		GodRays = false,
		ShadowMapQuality = 2,
		MaxFps = 75
	};

	public static GraphicsPreset UltraHigh = new GraphicsPreset("preset-ultrahigh")
	{
		ViewDistance = 512,
		Resolution = 1f,
		ParticleLevel = 100,
		SSAO = 2,
		SmoothLight = true,
		WavingFoliage = true,
		LiquidFoamEffect = true,
		DynamicLights = 50,
		FXAA = true,
		Bloom = true,
		ShadowMapQuality = 3,
		GodRays = true,
		MaxFps = 75,
		LodBiasFar = 0.85f
	};

	public static GraphicsPreset Glorious = new GraphicsPreset("preset-glorious")
	{
		ViewDistance = 768,
		Resolution = 1f,
		ParticleLevel = 100,
		SSAO = 2,
		SmoothLight = true,
		WavingFoliage = true,
		LiquidFoamEffect = true,
		DynamicLights = 50,
		FXAA = true,
		Bloom = true,
		ShadowMapQuality = 4,
		GodRays = true,
		MaxFps = 75,
		LodBiasFar = 1f
	};

	public static GraphicsPreset Maximum = new GraphicsPreset("preset-maximum")
	{
		ViewDistance = 1536,
		Resolution = 1f,
		ParticleLevel = 100,
		SSAO = 2,
		SmoothLight = true,
		WavingFoliage = true,
		LiquidFoamEffect = true,
		DynamicLights = 100,
		FXAA = true,
		Bloom = true,
		ShadowMapQuality = 4,
		GodRays = true,
		MaxFps = 144,
		LodBiasFar = 1f
	};

	public static GraphicsPreset Custom = new GraphicsPreset("preset-custom");

	public GraphicsPreset(string langcode)
	{
		Langcode = langcode;
		PresetId = Presets.Count;
		Presets.Add(this);
	}
}
