using System;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Controls the transformations of 3D shapes. Note that defaults change depending on where this class is used.
/// </summary>
/// <example>
/// Use '.tfedit' in game to help customize these values, just make sure to copy them into your json file when you finish.
/// <code language="json">
///             "tpHandTransform": {
///             	"translation": {
///             		"x": -0.87,
///             		"y": -0.01,
///             		"z": -0.56
///             	},
///             	"rotation": {
///             		"x": -90,
///             		"y": 0,
///             		"z": 0
///             	},
///             	"origin": {
///             		"x": 0.5,
///             		"y": 0,
///             		"z": 0.5
///             	},
///             	"scale": 0.8
///             },
/// </code>
/// </example>
[DocumentAsJson]
[JsonObject(MemberSerialization.OptIn)]
public class ModelTransformNoDefaults
{
	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional>-->
	/// Offsetting
	/// </summary>
	[JsonProperty]
	public Vec3f Translation;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional>-->
	/// Rotation in degrees
	/// </summary>
	[JsonProperty]
	public Vec3f Rotation;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional>-->
	/// Rotation/Scaling Origin
	/// </summary>
	[JsonProperty]
	public Vec3f Origin = new Vec3f(0.5f, 0.5f, 0.5f);

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional>-->
	/// For Gui Transform: Whether to slowly spin in gui item preview 
	/// For Ground Transform: Whether to apply a random rotation to the dropped item
	/// No effect on other transforms
	/// </summary>
	[JsonProperty]
	public bool Rotate = true;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional>-->
	/// Scaling per axis
	/// </summary>
	[JsonProperty]
	public Vec3f ScaleXYZ = new Vec3f(1f, 1f, 1f);

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional>-->
	/// Sets the same scale of an object for all axes.
	/// </summary>
	[JsonProperty]
	public float Scale
	{
		set
		{
			ScaleXYZ.Set(value, value, value);
		}
	}

	/// <summary>
	/// Converts the transform into a matrix.
	/// </summary>
	public float[] AsMatrix
	{
		get
		{
			float[] array = Mat4f.Create();
			Mat4f.Translate(array, array, Translation.X, Translation.Y, Translation.Z);
			Mat4f.Translate(array, array, Origin.X, Origin.Y, Origin.Z);
			Mat4f.RotateX(array, array, Rotation.X * ((float)Math.PI / 180f));
			Mat4f.RotateY(array, array, Rotation.Y * ((float)Math.PI / 180f));
			Mat4f.RotateZ(array, array, Rotation.Z * ((float)Math.PI / 180f));
			Mat4f.Scale(array, array, ScaleXYZ.X, ScaleXYZ.Y, ScaleXYZ.Z);
			Mat4f.Translate(array, array, 0f - Origin.X, 0f - Origin.Y, 0f - Origin.Z);
			return array;
		}
	}

	/// <summary>
	/// Clears the transformation values.
	/// </summary>
	public void Clear()
	{
		Rotation.Set(0f, 0f, 0f);
		Translation.Set(0f, 0f, 0f);
		Origin.Set(0f, 0f, 0f);
		Scale = 1f;
	}
}
