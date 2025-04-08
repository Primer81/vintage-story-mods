using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class TesselationMetaData
{
	public static float[] randomRotations;

	public static float[][] randomRotMatrices;

	public ITexPositionSource TexSource;

	public int GeneralGlowLevel;

	public byte ClimateColorMapId;

	public byte SeasonColorMapId;

	public int? QuantityElements;

	public string[] SelectiveElements;

	public string[] IgnoreElements;

	public Dictionary<string, int[]> TexturesSizes;

	public string TypeForLogging;

	public bool WithJointIds;

	public bool WithDamageEffect;

	public Vec3f Rotation;

	public int GeneralWindMode;

	public bool UsesColorMap;

	public int[] defaultTextureSize;

	static TesselationMetaData()
	{
		randomRotations = new float[8] { -22.5f, 22.5f, 67.5f, 112.5f, 157.5f, 202.5f, 247.5f, 292.5f };
		randomRotMatrices = new float[randomRotations.Length][];
		for (int i = 0; i < randomRotations.Length; i++)
		{
			float[] matrix = Mat4f.Create();
			Mat4f.Translate(matrix, matrix, 0.5f, 0.5f, 0.5f);
			Mat4f.RotateY(matrix, matrix, randomRotations[i] * ((float)Math.PI / 180f));
			Mat4f.Translate(matrix, matrix, -0.5f, -0.5f, -0.5f);
			randomRotMatrices[i] = matrix;
		}
	}

	public TesselationMetaData Clone()
	{
		return new TesselationMetaData
		{
			TexSource = TexSource,
			GeneralGlowLevel = GeneralGlowLevel,
			ClimateColorMapId = ClimateColorMapId,
			SeasonColorMapId = SeasonColorMapId,
			QuantityElements = QuantityElements,
			SelectiveElements = SelectiveElements,
			IgnoreElements = IgnoreElements,
			TexturesSizes = TexturesSizes,
			TypeForLogging = TypeForLogging,
			WithJointIds = WithJointIds,
			UsesColorMap = UsesColorMap,
			defaultTextureSize = defaultTextureSize,
			GeneralWindMode = GeneralWindMode,
			WithDamageEffect = WithDamageEffect,
			Rotation = Rotation
		};
	}
}
