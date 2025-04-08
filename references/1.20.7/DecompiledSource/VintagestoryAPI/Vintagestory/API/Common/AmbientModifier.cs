using System.IO;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class AmbientModifier
{
	public WeightedFloat FlatFogDensity = new WeightedFloat(0f, 0f);

	public WeightedFloat FlatFogYPos = new WeightedFloat(0f, 0f);

	public WeightedFloat FogMin;

	public WeightedFloat FogDensity;

	public WeightedFloatArray FogColor;

	public WeightedFloatArray AmbientColor;

	public WeightedFloat CloudDensity;

	public WeightedFloat CloudBrightness;

	public WeightedFloat LerpSpeed;

	public WeightedFloat SceneBrightness;

	public WeightedFloat FogBrightness;

	public static AmbientModifier DefaultAmbient
	{
		get
		{
			AmbientModifier ambientModifier = new AmbientModifier();
			ambientModifier.FogColor = WeightedFloatArray.New(new float[3]
			{
				67f / 85f,
				0.827451f,
				73f / 85f
			}, 1f);
			ambientModifier.FogDensity = WeightedFloat.New(0.00125f, 1f);
			ambientModifier.AmbientColor = WeightedFloatArray.New(new float[3] { 1f, 1f, 1f }, 1f);
			ambientModifier.CloudBrightness = WeightedFloat.New(1f, 1f);
			ambientModifier.CloudDensity = WeightedFloat.New(0f, 0f);
			ambientModifier.SceneBrightness = WeightedFloat.New(1f, 0f);
			ambientModifier.FogBrightness = WeightedFloat.New(1f, 0f);
			return ambientModifier.EnsurePopulated();
		}
	}

	public void SetLerped(AmbientModifier left, AmbientModifier right, float w)
	{
		FlatFogDensity.SetLerped(left.FlatFogDensity, right.FlatFogDensity, w);
		FlatFogYPos.SetLerped(left.FlatFogYPos, right.FlatFogYPos, w);
		FogMin.SetLerped(left.FogMin, right.FogMin, w);
		FogDensity.SetLerped(left.FogDensity, right.FogDensity, w);
		FogColor.SetLerped(left.FogColor, right.FogColor, w);
		AmbientColor.SetLerped(left.AmbientColor, right.AmbientColor, w);
		CloudDensity.SetLerped(left.CloudDensity, right.CloudDensity, w);
		CloudBrightness.SetLerped(left.CloudBrightness, right.CloudBrightness, w);
		LerpSpeed.SetLerped(left.LerpSpeed, right.LerpSpeed, w);
		SceneBrightness.SetLerped(left.SceneBrightness, right.SceneBrightness, w);
		FogBrightness.SetLerped(left.FogBrightness, right.FogBrightness, w);
	}

	public AmbientModifier Clone()
	{
		return new AmbientModifier
		{
			FlatFogDensity = FlatFogDensity.Clone(),
			FlatFogYPos = FlatFogYPos.Clone(),
			FogDensity = FogDensity.Clone(),
			FogMin = FogMin.Clone(),
			FogColor = FogColor.Clone(),
			AmbientColor = AmbientColor.Clone(),
			CloudDensity = CloudDensity.Clone(),
			CloudBrightness = CloudBrightness.Clone(),
			SceneBrightness = SceneBrightness.Clone(),
			FogBrightness = FogBrightness.Clone()
		};
	}

	public AmbientModifier EnsurePopulated()
	{
		if (FogMin == null)
		{
			FogMin = WeightedFloat.New(0f, 0f);
		}
		if (FogDensity == null)
		{
			FogDensity = WeightedFloat.New(0f, 0f);
		}
		if (FogColor == null)
		{
			FogColor = WeightedFloatArray.New(new float[3], 0f);
		}
		if (FogColor.Value == null)
		{
			FogColor.Value = new float[3];
		}
		if (AmbientColor == null)
		{
			AmbientColor = WeightedFloatArray.New(new float[3], 0f);
		}
		if (AmbientColor.Value == null)
		{
			AmbientColor.Value = new float[3];
		}
		if (CloudDensity == null)
		{
			CloudDensity = WeightedFloat.New(0f, 0f);
		}
		if (CloudBrightness == null)
		{
			CloudBrightness = WeightedFloat.New(0f, 0f);
		}
		if (LerpSpeed == null)
		{
			LerpSpeed = WeightedFloat.New(0f, 0f);
		}
		if (SceneBrightness == null)
		{
			SceneBrightness = WeightedFloat.New(1f, 0f);
		}
		if (FogBrightness == null)
		{
			FogBrightness = WeightedFloat.New(1f, 0f);
		}
		return this;
	}

	public void ToBytes(BinaryWriter writer)
	{
		FogMin.ToBytes(writer);
		FogDensity.ToBytes(writer);
		FogColor.ToBytes(writer);
		AmbientColor.ToBytes(writer);
		CloudDensity.ToBytes(writer);
		CloudBrightness.ToBytes(writer);
		LerpSpeed.ToBytes(writer);
		FlatFogDensity.ToBytes(writer);
		FlatFogYPos.ToBytes(writer);
		SceneBrightness.ToBytes(writer);
		FogBrightness.ToBytes(writer);
	}

	public void FromBytes(BinaryReader reader)
	{
		FogMin.FromBytes(reader);
		FogDensity.FromBytes(reader);
		FogColor.FromBytes(reader);
		AmbientColor.FromBytes(reader);
		CloudDensity.FromBytes(reader);
		CloudBrightness.FromBytes(reader);
		LerpSpeed.FromBytes(reader);
		FlatFogDensity.FromBytes(reader);
		FlatFogYPos.FromBytes(reader);
		SceneBrightness.FromBytes(reader);
		FogBrightness.FromBytes(reader);
	}
}
