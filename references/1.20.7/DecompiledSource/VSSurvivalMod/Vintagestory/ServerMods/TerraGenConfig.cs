using System;

namespace Vintagestory.ServerMods;

public class TerraGenConfig
{
	public static double CavesPerChunkColumn = 0.37;

	protected static int terrainGenOctaves = 9;

	public const int lerpHorizontal = 4;

	public const int lerpVertical = 8;

	public static int WaterFreezingTempOnGen = -15;

	public static double terrainNoiseVerticalScale = 2.0;

	public static int rockStrataScale = 16;

	public static int rockStrataOctaveScale = 32;

	public static int shrubMapScale = 16;

	public static int forestMapScale = 32;

	public static int geoUpheavelMapScale = 64;

	public static int oceanMapScale = 32;

	public static int blockPatchesMapScale = 32;

	public static int forestMapPadding = 0;

	public static int beachMapScale = 16;

	public static int beachMapPadding = 0;

	public static int climateMapWobbleScale = 192;

	public static int climateMapScale = 32;

	public static int climateMapSubScale = 16;

	public static int oreMapWobbleScale = 110;

	public static int oreMapScale = 16;

	public static int oreMapSubScale = 16;

	public static int depositVerticalDistortScale = 8;

	public static int oreMapPadding = 0;

	public static int climateMapPadding = 1;

	public static int geoProvMapPadding = 3;

	public static int geoProvMapScale = 64;

	public static int geoProvSmoothingRadius = 2;

	public static int landformMapPadding = 4;

	public static int landformMapScale = 16;

	public static int landFormSmoothingRadius = 3;

	public static int seaLevel = 110;

	public static bool GenerateVegetation = true;

	public static bool GenerateStructures = true;

	public static bool DoDecorationPass = true;

	public static int GetTerrainOctaveCount(int worldheight)
	{
		return terrainGenOctaves + (worldheight - 256) / 128;
	}

	public static float SoilThickness(float rainRel, float temp, int distToSealevel, float thicknessVar)
	{
		float f = 8f * thicknessVar;
		float weight = 1f - Math.Max(0f, (4f - f) / 4f);
		return f - (float)(distToSealevel / 35) * weight;
	}
}
