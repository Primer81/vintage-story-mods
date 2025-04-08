using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Datastructures;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class GenMaps : ModSystem
{
	private ICoreServerAPI sapi;

	private ICoreClientAPI capi;

	public MapLayerBase upheavelGen;

	public MapLayerBase oceanGen;

	public MapLayerBase climateGen;

	public MapLayerBase flowerGen;

	public MapLayerBase bushGen;

	public MapLayerBase forestGen;

	public MapLayerBase beachGen;

	public MapLayerBase geologicprovinceGen;

	public MapLayerBase landformsGen;

	public int noiseSizeUpheavel;

	public int noiseSizeOcean;

	public int noiseSizeClimate;

	public int noiseSizeForest;

	public int noiseSizeBeach;

	public int noiseSizeShrubs;

	public int noiseSizeGeoProv;

	public int noiseSizeLandform;

	private LatitudeData latdata = new LatitudeData();

	private List<ForceLandform> forceLandforms = new List<ForceLandform>();

	private List<ForceClimate> forceClimate = new List<ForceClimate>();

	private NormalizedSimplexNoise noisegenX;

	private NormalizedSimplexNoise noisegenZ;

	public static float upheavelCommonness;

	public List<XZ> requireLandAt = new List<XZ>();

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("latitudedata").RegisterMessageType(typeof(LatitudeData));
	}

	public void ForceClimateAt(ForceClimate climate)
	{
		forceClimate.Add(climate);
	}

	public void ForceLandformAt(ForceLandform landform)
	{
		forceLandforms.Add(landform);
		ForceLandAt(landform);
		LandformVariant[] list = NoiseLandforms.landforms.LandFormsByIndex;
		for (int i = 0; i < list.Length; i++)
		{
			if (list[i].Code.Path == landform.LandformCode)
			{
				landform.landFormIndex = i;
				return;
			}
		}
		throw new ArgumentException("No landform with code " + landform.LandformCode + " found.");
	}

	public void ForceLandAt(ForceLandform fl)
	{
		if (GameVersion.IsLowerVersionThan(sapi.WorldManager.SaveGame.CreatedGameVersion, "1.20.0-pre.14"))
		{
			int regSize = sapi.WorldManager.RegionSize;
			int flRadius = fl.Radius;
			int num = (fl.CenterPos.X - flRadius) * noiseSizeOcean / regSize;
			int minz = (fl.CenterPos.Z - flRadius) * noiseSizeOcean / regSize;
			int maxx = (fl.CenterPos.X + flRadius) * noiseSizeOcean / regSize;
			int maxz = (fl.CenterPos.Z + flRadius) * noiseSizeOcean / regSize;
			for (int x = num; x <= maxx; x++)
			{
				for (int z = minz; z < maxz; z++)
				{
					requireLandAt.Add(new XZ(x, z));
				}
			}
		}
		else
		{
			int radius = fl.Radius + sapi.WorldManager.ChunkSize;
			ForceRandomLandArea(fl.CenterPos.X, fl.CenterPos.Z, radius);
		}
	}

	private void ForceRandomLandArea(int positionX, int positionZ, int radius)
	{
		int regionSize = sapi.WorldManager.RegionSize;
		int minx = (positionX - radius) * noiseSizeOcean / regionSize;
		int minz = (positionZ - radius) * noiseSizeOcean / regionSize;
		int maxx = (positionX + radius) * noiseSizeOcean / regionSize;
		int maxz = (positionZ + radius) * noiseSizeOcean / regionSize;
		LCGRandom lCGRandom = new LCGRandom(sapi.World.Seed);
		lCGRandom.InitPositionSeed(positionX, positionZ);
		NaturalShape naturalShape = new NaturalShape(lCGRandom);
		int sizeX = maxx - minx;
		int sizeZ = maxz - minz;
		naturalShape.InitSquare(sizeX, sizeZ);
		naturalShape.Grow(sizeX * sizeZ);
		foreach (Vec2i pos in naturalShape.GetPositions())
		{
			requireLandAt.Add(new XZ(minx + pos.X, minz + pos.Y));
		}
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		api.Network.GetChannel("latitudedata").SetMessageHandler<LatitudeData>(onLatitudeDataReceived);
		api.Event.LevelFinalize += Event_LevelFinalize;
		capi = api;
	}

	private void Event_LevelFinalize()
	{
		capi.World.Calendar.OnGetLatitude = getLatitude;
	}

	private void onLatitudeDataReceived(LatitudeData latdata)
	{
		this.latdata = latdata;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.InitWorldGenerator(initWorldGen, "standard");
		api.Event.InitWorldGenerator(initWorldGen, "superflat");
		api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
		api.Event.MapRegionGeneration(OnMapRegionGen, "superflat");
		api.Event.PlayerJoin += delegate(IServerPlayer plr)
		{
			api.Network.GetChannel("latitudedata").SendPacket(latdata, plr);
		};
	}

	public void initWorldGen()
	{
		requireLandAt.Clear();
		forceLandforms.Clear();
		long seed = sapi.WorldManager.Seed;
		noiseSizeOcean = sapi.WorldManager.RegionSize / TerraGenConfig.oceanMapScale;
		noiseSizeUpheavel = sapi.WorldManager.RegionSize / TerraGenConfig.climateMapScale;
		noiseSizeClimate = sapi.WorldManager.RegionSize / TerraGenConfig.climateMapScale;
		noiseSizeForest = sapi.WorldManager.RegionSize / TerraGenConfig.forestMapScale;
		noiseSizeShrubs = sapi.WorldManager.RegionSize / TerraGenConfig.shrubMapScale;
		noiseSizeGeoProv = sapi.WorldManager.RegionSize / TerraGenConfig.geoProvMapScale;
		noiseSizeLandform = sapi.WorldManager.RegionSize / TerraGenConfig.landformMapScale;
		noiseSizeBeach = sapi.WorldManager.RegionSize / TerraGenConfig.beachMapScale;
		ITreeAttribute worldConfig = sapi.WorldManager.SaveGame.WorldConfiguration;
		string @string = worldConfig.GetString("worldClimate", "realistic");
		float tempModifier = worldConfig.GetString("globalTemperature", "1").ToFloat(1f);
		float rainModifier = worldConfig.GetString("globalPrecipitation", "1").ToFloat(1f);
		latdata.polarEquatorDistance = worldConfig.GetString("polarEquatorDistance", "50000").ToInt(50000);
		upheavelCommonness = worldConfig.GetString("upheavelCommonness", "0.3").ToFloat(0.3f);
		float landcover = worldConfig.GetString("landcover", "1").ToFloat(1f);
		float oceanscale = worldConfig.GetString("oceanscale", "1").ToFloat(1f);
		float landformScale = worldConfig.GetString("landformScale", "1.0").ToFloat(1f);
		NoiseClimate noiseClimate;
		if (@string == "realistic")
		{
			int spawnMinTemp = 6;
			int spawnMaxTemp = 14;
			switch (worldConfig.GetString("startingClimate"))
			{
			case "hot":
				spawnMinTemp = 28;
				spawnMaxTemp = 32;
				break;
			case "warm":
				spawnMinTemp = 19;
				spawnMaxTemp = 23;
				break;
			case "cool":
				spawnMinTemp = -5;
				spawnMaxTemp = 1;
				break;
			case "icy":
				spawnMinTemp = -15;
				spawnMaxTemp = -10;
				break;
			}
			noiseClimate = new NoiseClimateRealistic(seed, (double)sapi.WorldManager.MapSizeZ / (double)TerraGenConfig.climateMapScale / (double)TerraGenConfig.climateMapSubScale, latdata.polarEquatorDistance, spawnMinTemp, spawnMaxTemp);
			(noiseClimate as NoiseClimateRealistic).GeologicActivityStrength = worldConfig.GetString("geologicActivity").ToFloat(0.05f);
			latdata.isRealisticClimate = true;
			latdata.ZOffset = (noiseClimate as NoiseClimateRealistic).ZOffset;
		}
		else
		{
			noiseClimate = new NoiseClimatePatchy(seed);
		}
		noiseClimate.rainMul = rainModifier;
		noiseClimate.tempMul = tempModifier;
		bool requiresSpawnOffset = GameVersion.IsLowerVersionThan(sapi.WorldManager.SaveGame.CreatedGameVersion, "1.20.0-pre.14");
		if (requiresSpawnOffset)
		{
			int centerRegX = sapi.WorldManager.MapSizeX / sapi.WorldManager.RegionSize / 2;
			int centerRegZ = sapi.WorldManager.MapSizeZ / sapi.WorldManager.RegionSize / 2;
			requireLandAt.Add(new XZ(centerRegX * noiseSizeOcean, centerRegZ * noiseSizeOcean));
		}
		else
		{
			int chunkSize = sapi.WorldManager.ChunkSize;
			int radius = 4 * chunkSize;
			int spawnPosX = (sapi.WorldManager.MapSizeX + chunkSize) / 2;
			int spawnPosZ = (sapi.WorldManager.MapSizeZ + chunkSize) / 2;
			ForceRandomLandArea(spawnPosX, spawnPosZ, radius);
		}
		climateGen = GetClimateMapGen(seed + 1, noiseClimate);
		upheavelGen = GetGeoUpheavelMapGen(seed + 873, TerraGenConfig.geoUpheavelMapScale);
		oceanGen = GetOceanMapGen(seed + 1873, landcover, TerraGenConfig.oceanMapScale, oceanscale, requireLandAt, requiresSpawnOffset);
		forestGen = GetForestMapGen(seed + 2, TerraGenConfig.forestMapScale);
		bushGen = GetForestMapGen(seed + 109, TerraGenConfig.shrubMapScale);
		flowerGen = GetForestMapGen(seed + 223, TerraGenConfig.forestMapScale);
		beachGen = GetBeachMapGen(seed + 2273, TerraGenConfig.beachMapScale);
		geologicprovinceGen = GetGeologicProvinceMapGen(seed + 3, sapi);
		landformsGen = GetLandformMapGen(seed + 4, noiseClimate, sapi, landformScale);
		sapi.World.Calendar.OnGetLatitude = getLatitude;
		int woctaves = 2;
		float wscale = 2f * (float)TerraGenConfig.landformMapScale;
		float wpersistence = 0.9f;
		noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(woctaves, 1f / wscale, wpersistence, seed + 2);
		noisegenZ = NormalizedSimplexNoise.FromDefaultOctaves(woctaves, 1f / wscale, wpersistence, seed + 1231296);
	}

	private double getLatitude(double posZ)
	{
		if (!latdata.isRealisticClimate)
		{
			return 0.5;
		}
		double num = (double)latdata.polarEquatorDistance / (double)TerraGenConfig.climateMapScale / (double)TerraGenConfig.climateMapSubScale;
		double A = 2.0;
		double P = num;
		double z = posZ / (double)TerraGenConfig.climateMapScale / (double)TerraGenConfig.climateMapSubScale + latdata.ZOffset;
		return A / P * (P - Math.Abs(Math.Abs(z / 2.0 - P) % (2.0 * P) - P)) - 1.0;
	}

	private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
		int pad = TerraGenConfig.geoProvMapPadding;
		mapRegion.GeologicProvinceMap.Data = geologicprovinceGen.GenLayer(regionX * noiseSizeGeoProv - pad, regionZ * noiseSizeGeoProv - pad, noiseSizeGeoProv + 2 * pad, noiseSizeGeoProv + 2 * pad);
		mapRegion.GeologicProvinceMap.Size = noiseSizeGeoProv + 2 * pad;
		mapRegion.GeologicProvinceMap.TopLeftPadding = (mapRegion.GeologicProvinceMap.BottomRightPadding = pad);
		pad = 2;
		mapRegion.ClimateMap.Data = climateGen.GenLayer(regionX * noiseSizeClimate - pad, regionZ * noiseSizeClimate - pad, noiseSizeClimate + 2 * pad, noiseSizeClimate + 2 * pad);
		mapRegion.ClimateMap.Size = noiseSizeClimate + 2 * pad;
		mapRegion.ClimateMap.TopLeftPadding = (mapRegion.ClimateMap.BottomRightPadding = pad);
		mapRegion.ForestMap.Size = noiseSizeForest + 1;
		mapRegion.ForestMap.BottomRightPadding = 1;
		forestGen.SetInputMap(mapRegion.ClimateMap, mapRegion.ForestMap);
		mapRegion.ForestMap.Data = forestGen.GenLayer(regionX * noiseSizeForest, regionZ * noiseSizeForest, noiseSizeForest + 1, noiseSizeForest + 1);
		int upPad = 3;
		mapRegion.UpheavelMap.Size = noiseSizeUpheavel + 2 * upPad;
		mapRegion.UpheavelMap.TopLeftPadding = upPad;
		mapRegion.UpheavelMap.BottomRightPadding = upPad;
		mapRegion.UpheavelMap.Data = upheavelGen.GenLayer(regionX * noiseSizeUpheavel - upPad, regionZ * noiseSizeUpheavel - upPad, noiseSizeUpheavel + 2 * upPad, noiseSizeUpheavel + 2 * upPad);
		int opad = 5;
		mapRegion.OceanMap.Size = noiseSizeOcean + 2 * opad;
		mapRegion.OceanMap.TopLeftPadding = opad;
		mapRegion.OceanMap.BottomRightPadding = opad;
		mapRegion.OceanMap.Data = oceanGen.GenLayer(regionX * noiseSizeOcean - opad, regionZ * noiseSizeOcean - opad, noiseSizeOcean + 2 * opad, noiseSizeOcean + 2 * opad);
		mapRegion.BeachMap.Size = noiseSizeBeach + 1;
		mapRegion.BeachMap.BottomRightPadding = 1;
		mapRegion.BeachMap.Data = beachGen.GenLayer(regionX * noiseSizeBeach, regionZ * noiseSizeBeach, noiseSizeBeach + 1, noiseSizeBeach + 1);
		mapRegion.ShrubMap.Size = noiseSizeShrubs + 1;
		mapRegion.ShrubMap.BottomRightPadding = 1;
		bushGen.SetInputMap(mapRegion.ClimateMap, mapRegion.ShrubMap);
		mapRegion.ShrubMap.Data = bushGen.GenLayer(regionX * noiseSizeShrubs, regionZ * noiseSizeShrubs, noiseSizeShrubs + 1, noiseSizeShrubs + 1);
		mapRegion.FlowerMap.Size = noiseSizeForest + 1;
		mapRegion.FlowerMap.BottomRightPadding = 1;
		flowerGen.SetInputMap(mapRegion.ClimateMap, mapRegion.FlowerMap);
		mapRegion.FlowerMap.Data = flowerGen.GenLayer(regionX * noiseSizeForest, regionZ * noiseSizeForest, noiseSizeForest + 1, noiseSizeForest + 1);
		pad = TerraGenConfig.landformMapPadding;
		mapRegion.LandformMap.Data = landformsGen.GenLayer(regionX * noiseSizeLandform - pad, regionZ * noiseSizeLandform - pad, noiseSizeLandform + 2 * pad, noiseSizeLandform + 2 * pad);
		mapRegion.LandformMap.Size = noiseSizeLandform + 2 * pad;
		mapRegion.LandformMap.TopLeftPadding = (mapRegion.LandformMap.BottomRightPadding = pad);
		if (chunkGenParams != null && chunkGenParams.HasAttribute("forceLandform"))
		{
			int index = chunkGenParams.GetInt("forceLandform");
			for (int i = 0; i < mapRegion.LandformMap.Data.Length; i++)
			{
				mapRegion.LandformMap.Data[i] = index;
			}
		}
		int regionsize = sapi.WorldManager.RegionSize;
		foreach (ForceLandform fl in forceLandforms)
		{
			forceLandform(mapRegion, regionX, regionZ, pad, regionsize, fl);
			forceNoUpheavel(mapRegion, regionX, regionZ, upPad, regionsize, fl);
		}
		foreach (ForceClimate climate in forceClimate)
		{
			ForceClimate(mapRegion, regionX, regionZ, pad, regionsize, climate);
		}
		mapRegion.DirtyForSaving = true;
	}

	private void forceNoUpheavel(IMapRegion mapRegion, int regionX, int regionZ, int pad, int regionsize, ForceLandform fl)
	{
		IntDataMap2D map = mapRegion.UpheavelMap;
		int uhmapsize = map.InnerSize;
		float wobbleIntensityBlocks = 80f;
		float padRel_wobblepaduh = (float)pad / (float)noiseSizeUpheavel + wobbleIntensityBlocks / (float)regionsize;
		float minlf = 0f - padRel_wobblepaduh;
		float maxlf = 1f + padRel_wobblepaduh;
		int rad = fl.Radius + 100;
		float startX = (float)(fl.CenterPos.X - rad) / (float)regionsize - (float)regionX;
		float endX = (float)(fl.CenterPos.X + rad) / (float)regionsize - (float)regionX;
		float startZ = (float)(fl.CenterPos.Z - rad) / (float)regionsize - (float)regionZ;
		float endZ = (float)(fl.CenterPos.Z + rad) / (float)regionsize - (float)regionZ;
		if (!(endX >= minlf) || !(startX <= maxlf) || !(endZ >= minlf) || !(startZ <= maxlf))
		{
			return;
		}
		double radiussq = Math.Pow((double)rad / (double)regionsize * (double)uhmapsize, 2.0);
		double centerRegionX = (double)fl.CenterPos.X / (double)regionsize;
		double num = (double)fl.CenterPos.Z / (double)regionsize;
		double regionOffsetToCenterX = centerRegionX - (double)regionX;
		double regionOffsetToCenterZ = num - (double)regionZ;
		regionOffsetToCenterX *= (double)uhmapsize;
		regionOffsetToCenterZ *= (double)uhmapsize;
		startX = GameMath.Clamp(startX, minlf, maxlf) * (float)uhmapsize - (float)pad;
		endX = GameMath.Clamp(endX, minlf, maxlf) * (float)uhmapsize + (float)pad;
		startZ = GameMath.Clamp(startZ, minlf, maxlf) * (float)uhmapsize - (float)pad;
		endZ = GameMath.Clamp(endZ, minlf, maxlf) * (float)uhmapsize + (float)pad;
		for (int x = (int)startX; (float)x < endX; x++)
		{
			for (int z = (int)startZ; (float)z < endZ; z++)
			{
				double rsq = Math.Pow((double)x - regionOffsetToCenterX, 2.0) + Math.Pow((double)z - regionOffsetToCenterZ, 2.0);
				if (!(rsq >= radiussq))
				{
					double attn = Math.Pow(1.0 - rsq / radiussq, 3.0) * 512.0;
					int finalX = x + pad;
					int finalZ = z + pad;
					if (finalX >= 0 && finalX < map.Size && finalZ >= 0 && finalZ < map.Size)
					{
						map.SetInt(finalX, finalZ, (int)Math.Max(0.0, (double)map.GetInt(finalX, finalZ) - attn));
					}
				}
			}
		}
	}

	private void ForceClimate(IMapRegion mapRegion, int regionX, int regionZ, int pad, int regionsize, ForceClimate fl)
	{
		IntDataMap2D map = mapRegion.ClimateMap;
		int innerSize = map.InnerSize;
		float wobbleIntensityBlocks = 80f;
		float padRel_wobblepaduh = (float)pad / (float)noiseSizeClimate + wobbleIntensityBlocks / (float)regionsize;
		float minlf = 0f - padRel_wobblepaduh;
		float maxlf = 1f + padRel_wobblepaduh;
		float transitionDist = 300f;
		float rad = (float)fl.Radius + transitionDist;
		float startX = ((float)fl.CenterPos.X - rad) / (float)regionsize - (float)regionX;
		float endX = ((float)fl.CenterPos.X + rad) / (float)regionsize - (float)regionX;
		float startZ = ((float)fl.CenterPos.Z - rad) / (float)regionsize - (float)regionZ;
		float endZ = ((float)fl.CenterPos.Z + rad) / (float)regionsize - (float)regionZ;
		if (!(endX >= minlf) || !(startX <= maxlf) || !(endZ >= minlf) || !(startZ <= maxlf))
		{
			return;
		}
		double radiussq = Math.Pow((double)rad / (double)regionsize * (double)innerSize, 2.0);
		double transsq = Math.Pow((double)transitionDist / (double)regionsize * (double)innerSize, 2.0);
		double startTransitionFade = Math.Sqrt(radiussq) - Math.Sqrt(transsq);
		double centerRegionX = (double)fl.CenterPos.X / (double)regionsize;
		double num = (double)fl.CenterPos.Z / (double)regionsize;
		double regionOffsetToCenterX = centerRegionX - (double)regionX;
		double regionOffsetToCenterZ = num - (double)regionZ;
		regionOffsetToCenterX *= (double)innerSize;
		regionOffsetToCenterZ *= (double)innerSize;
		startX = GameMath.Clamp(startX, minlf, maxlf) * (float)innerSize - (float)pad;
		endX = GameMath.Clamp(endX, minlf, maxlf) * (float)innerSize + (float)pad;
		startZ = GameMath.Clamp(startZ, minlf, maxlf) * (float)innerSize - (float)pad;
		endZ = GameMath.Clamp(endZ, minlf, maxlf) * (float)innerSize + (float)pad;
		int forceRain = (fl.Climate >> 8) & 0xFF;
		int forceTemperature = (fl.Climate >> 16) & 0xFF;
		for (int x = (int)startX; (float)x < endX; x++)
		{
			for (int z = (int)startZ; (float)z < endZ; z++)
			{
				double rsq = Math.Pow((double)x - regionOffsetToCenterX, 2.0) + Math.Pow((double)z - regionOffsetToCenterZ, 2.0);
				if (!(rsq >= radiussq))
				{
					int finalX = x + pad;
					int finalZ = z + pad;
					if (finalX >= 0 && finalX < map.Size && finalZ >= 0 && finalZ < map.Size)
					{
						int @int = map.GetInt(finalX, finalZ);
						int geologicActivity = @int & 0xFF;
						int rain = (@int >> 8) & 0xFF;
						int temperature = (@int >> 16) & 0xFF;
						double mapDist = Math.Sqrt(rsq);
						double distanceFadeStart = Math.Max(0.0, mapDist - startTransitionFade);
						double lerpAmount = Math.Min(1.0, distanceFadeStart / startTransitionFade);
						int num2 = (int)GameMath.Lerp(forceTemperature, temperature, lerpAmount);
						int newRain = (int)GameMath.Lerp(forceRain, rain, lerpAmount);
						int newClimate = (num2 << 16) + (newRain << 8) + geologicActivity;
						map.SetInt(finalX, finalZ, newClimate);
					}
				}
			}
		}
	}

	private void forceLandform(IMapRegion mapRegion, int regionX, int regionZ, int pad, int regionsize, ForceLandform fl)
	{
		int lfmapsize = mapRegion.LandformMap.InnerSize;
		float wobbleIntensityBlocks = 80f;
		float wobbleIntensityPixelslf = wobbleIntensityBlocks / (float)regionsize * (float)lfmapsize;
		float padRel_wobblepadlf = (float)pad / (float)noiseSizeLandform + wobbleIntensityBlocks / (float)regionsize;
		float minlf = 0f - padRel_wobblepadlf;
		float maxlf = 1f + padRel_wobblepadlf;
		int flRadius = fl.Radius;
		float startX = (float)(fl.CenterPos.X - flRadius) / (float)regionsize - (float)regionX;
		float endX = (float)(fl.CenterPos.X + flRadius) / (float)regionsize - (float)regionX;
		float startZ = (float)(fl.CenterPos.Z - flRadius) / (float)regionsize - (float)regionZ;
		float endZ = (float)(fl.CenterPos.Z + flRadius) / (float)regionsize - (float)regionZ;
		if (!(endX >= minlf) || !(startX <= maxlf) || !(endZ >= minlf) || !(startZ <= maxlf))
		{
			return;
		}
		startX = GameMath.Clamp(startX, minlf, maxlf) * (float)lfmapsize - (float)pad;
		endX = GameMath.Clamp(endX, minlf, maxlf) * (float)lfmapsize + (float)pad;
		startZ = GameMath.Clamp(startZ, minlf, maxlf) * (float)lfmapsize - (float)pad;
		endZ = GameMath.Clamp(endZ, minlf, maxlf) * (float)lfmapsize + (float)pad;
		double radiussq = Math.Pow((double)flRadius / (double)regionsize * (double)lfmapsize, 2.0);
		double centerRegionX = (double)fl.CenterPos.X / (double)regionsize;
		double num = (double)fl.CenterPos.Z / (double)regionsize;
		double regionOffsetToCenterX = centerRegionX - (double)regionX;
		double regionOffsetToCenterZ = num - (double)regionZ;
		regionOffsetToCenterX *= (double)lfmapsize;
		regionOffsetToCenterZ *= (double)lfmapsize;
		for (int x = (int)startX; (float)x < endX; x++)
		{
			for (int z = (int)startZ; (float)z < endZ; z++)
			{
				if (!(Math.Pow((double)x - regionOffsetToCenterX, 2.0) + Math.Pow((double)z - regionOffsetToCenterZ, 2.0) >= radiussq))
				{
					double nx = x + regionX * lfmapsize;
					double nz = z + regionZ * lfmapsize;
					int offsetX = (int)((double)wobbleIntensityPixelslf * noisegenX.Noise(nx, nz));
					int offsetZ = (int)((double)wobbleIntensityPixelslf * noisegenZ.Noise(nx, nz));
					int finalX = x + offsetX + pad;
					int finalZ = z + offsetZ + pad;
					if (finalX >= 0 && finalX < mapRegion.LandformMap.Size && finalZ >= 0 && finalZ < mapRegion.LandformMap.Size)
					{
						mapRegion.LandformMap.SetInt(finalX, finalZ, fl.landFormIndex);
					}
				}
			}
		}
	}

	public static MapLayerBase GetDebugWindMap(long seed)
	{
		MapLayerDebugWind mapLayerDebugWind = new MapLayerDebugWind(seed + 1);
		mapLayerDebugWind.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, "Wind 1 - Wind");
		return mapLayerDebugWind;
	}

	public static MapLayerBase GetClimateMapGen(long seed, NoiseClimate climateNoise)
	{
		MapLayerBase climate = new MapLayerClimate(seed + 1, climateNoise);
		climate.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, "Climate 1 - Noise");
		climate = new MapLayerPerlinWobble(seed + 2, climate, 6, 0.7f, TerraGenConfig.climateMapWobbleScale, (float)TerraGenConfig.climateMapWobbleScale * 0.15f);
		climate.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, "Climate 2 - Perlin Wobble");
		return climate;
	}

	public static MapLayerBase GetOreMap(long seed, NoiseOre oreNoise, float scaleMul, float contrast, float sub)
	{
		MapLayerBase ore = new MapLayerOre(seed + 1, oreNoise, scaleMul, contrast, sub);
		ore.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, 512, "Ore 1 - Noise");
		ore = new MapLayerPerlinWobble(seed + 2, ore, 5, 0.85f, TerraGenConfig.oreMapWobbleScale, (float)TerraGenConfig.oreMapWobbleScale * 0.15f);
		ore.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, 512, "Ore 1 - Perlin Wobble");
		return ore;
	}

	public static MapLayerBase GetDepositVerticalDistort(long seed)
	{
		double[] thresholds = new double[4] { 0.1, 0.1, 0.1, 0.1 };
		MapLayerPerlin mapLayerPerlin = new MapLayerPerlin(seed + 1, 4, 0.8f, 25 * TerraGenConfig.depositVerticalDistortScale, 40, thresholds);
		mapLayerPerlin.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, "Vertical Distort");
		return mapLayerPerlin;
	}

	public static MapLayerBase GetForestMapGen(long seed, int scale)
	{
		return new MapLayerWobbledForest(seed + 1, 3, 0.9f, scale, 600f, -100);
	}

	public static MapLayerBase GetGeoUpheavelMapGen(long seed, int scale)
	{
		MapLayerPerlinUpheavel map = new MapLayerPerlinUpheavel(seed, upheavelCommonness, scale, 600f, -300);
		return new MapLayerBlur(0L, map, 3);
	}

	public static MapLayerBase GetOceanMapGen(long seed, float landcover, int oceanMapScale, float oceanScaleMul, List<XZ> requireLandAt, bool requiresSpawnOffset)
	{
		MapLayerOceans map = new MapLayerOceans(seed, (float)oceanMapScale * oceanScaleMul, landcover, requireLandAt, requiresSpawnOffset);
		return new MapLayerBlur(0L, map, 5);
	}

	public static MapLayerBase GetBeachMapGen(long seed, int scale)
	{
		MapLayerPerlin layer = new MapLayerPerlin(seed + 1, 6, 0.9f, scale / 3, 255, new double[6] { 0.20000000298023224, 0.20000000298023224, 0.20000000298023224, 0.20000000298023224, 0.20000000298023224, 0.20000000298023224 });
		return new MapLayerPerlinWobble(seed + 986876, layer, 4, 0.9f, scale / 2);
	}

	public static MapLayerBase GetGeologicProvinceMapGen(long seed, ICoreServerAPI api)
	{
		MapLayerGeoProvince mapLayerGeoProvince = new MapLayerGeoProvince(seed + 5, api);
		mapLayerGeoProvince.DebugDrawBitmap(DebugDrawMode.ProvinceRGB, 0, 0, "Geologic Province 1 - WobbleProvinces");
		return mapLayerGeoProvince;
	}

	public static MapLayerBase GetLandformMapGen(long seed, NoiseClimate climateNoise, ICoreServerAPI api, float landformScale)
	{
		MapLayerLandforms mapLayerLandforms = new MapLayerLandforms(seed + 12, climateNoise, api, landformScale);
		mapLayerLandforms.DebugDrawBitmap(DebugDrawMode.LandformRGB, 0, 0, "Landforms 1 - Wobble Landforms");
		return mapLayerLandforms;
	}
}
