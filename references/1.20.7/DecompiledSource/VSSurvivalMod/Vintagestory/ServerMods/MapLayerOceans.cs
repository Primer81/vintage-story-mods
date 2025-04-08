using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

internal class MapLayerOceans : MapLayerBase
{
	private NormalizedSimplexNoise noisegenX;

	private NormalizedSimplexNoise noisegenY;

	private float wobbleIntensity;

	private NoiseOcean noiseOcean;

	public float landFormHorizontalScale = 1f;

	private List<XZ> requireLandAt;

	private int spawnOffsX;

	private int spawnOffsZ;

	private float scale;

	private readonly bool requiresSpawnOffset;

	public MapLayerOceans(long seed, float scale, float landCoverRate, List<XZ> requireLandAt, bool requiresSpawnOffset)
		: base(seed)
	{
		noiseOcean = new NoiseOcean(seed, scale, landCoverRate);
		this.requireLandAt = requireLandAt;
		this.scale = scale;
		int woctaves = 4;
		float wscale = 2f * (float)TerraGenConfig.oceanMapScale;
		float wpersistence = 0.9f;
		wobbleIntensity = (float)TerraGenConfig.oceanMapScale * 1.5f * 1.2f;
		noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(woctaves, 1f / wscale, wpersistence, seed + 2);
		noisegenY = NormalizedSimplexNoise.FromDefaultOctaves(woctaves, 1f / wscale, wpersistence, seed + 1231296);
		this.requiresSpawnOffset = requiresSpawnOffset;
		XZ spawnCoord = requireLandAt[0];
		XZ offs = GetNoiseOffsetAt(spawnCoord.X, spawnCoord.Z);
		spawnOffsX = -offs.X;
		spawnOffsZ = -offs.Z;
	}

	public XZ GetNoiseOffsetAt(int xCoord, int zCoord)
	{
		int x = (int)((double)wobbleIntensity * noisegenX.Noise(xCoord, zCoord) * 1.2000000476837158);
		int offsetY = (int)((double)wobbleIntensity * noisegenY.Noise(xCoord, zCoord) * 1.2000000476837158);
		return new XZ(x, offsetY);
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		if (requiresSpawnOffset)
		{
			xCoord += spawnOffsX;
			zCoord += spawnOffsZ;
		}
		int[] result = new int[sizeX * sizeZ];
		for (int x = 0; x < sizeX; x++)
		{
			for (int z = 0; z < sizeZ; z++)
			{
				int nx = xCoord + x;
				int nz = zCoord + z;
				int offsetX = (int)((double)wobbleIntensity * noisegenX.Noise(nx, nz));
				int offsetZ = (int)((double)wobbleIntensity * noisegenY.Noise(nx, nz));
				int unscaledXpos = nx + offsetX;
				int unscaledZpos = nz + offsetZ;
				int oceanicity = noiseOcean.GetOceanIndexAt(unscaledXpos, unscaledZpos);
				if (oceanicity == 255)
				{
					if (requiresSpawnOffset)
					{
						float scaled = scale / 2f;
						for (int i = 0; i < requireLandAt.Count; i++)
						{
							XZ xz = requireLandAt[i];
							if ((float)Math.Abs(xz.X - unscaledXpos) <= scaled && (float)Math.Abs(xz.Z - unscaledZpos) <= scaled)
							{
								oceanicity = 0;
								break;
							}
						}
					}
					else
					{
						for (int j = 0; j < requireLandAt.Count; j++)
						{
							XZ xz2 = requireLandAt[j];
							if (xz2.X == nx && xz2.Z == nz)
							{
								oceanicity = 0;
								break;
							}
						}
					}
				}
				result[z * sizeX + x] = oceanicity;
			}
		}
		return result;
	}
}
