using System.Runtime.Serialization;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Used for transformations applied to a block or item model. Uses values from <see cref="T:Vintagestory.API.Common.ModelTransformNoDefaults" /> but will assign defaults if not included.
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
public class ModelTransform : ModelTransformNoDefaults
{
	/// <summary>
	/// Gets a new model with all values set to default.
	/// </summary>
	public static ModelTransform NoTransform => new ModelTransform().EnsureDefaultValues();

	public ModelTransform(ModelTransformNoDefaults baseTf, ModelTransform defaults)
	{
		Rotate = baseTf.Rotate;
		if (baseTf.Translation == null)
		{
			Translation = defaults.Translation.Clone();
		}
		else
		{
			Translation = baseTf.Translation.Clone();
		}
		if (baseTf.Rotation == null)
		{
			Rotation = defaults.Rotation.Clone();
		}
		else
		{
			Rotation = baseTf.Rotation.Clone();
		}
		if (baseTf.Origin == null)
		{
			Origin = defaults.Origin.Clone();
		}
		else
		{
			Origin = baseTf.Origin.Clone();
		}
		if (baseTf.ScaleXYZ == null)
		{
			ScaleXYZ = defaults.ScaleXYZ.Clone();
		}
		else
		{
			ScaleXYZ = baseTf.ScaleXYZ.Clone();
		}
	}

	public ModelTransform()
	{
	}

	/// <summary>
	/// Scale = 1, No Translation, Rotation by -45 deg in Y-Axis
	/// </summary>
	/// <returns></returns>
	public static ModelTransform BlockDefaultGui()
	{
		return new ModelTransform
		{
			Translation = new Vec3f(),
			Rotation = new Vec3f(-22.6f, -45.3f, 0f),
			Scale = 1f
		};
	}

	/// <summary>
	/// Scale = 1, No Translation, Rotation by -45 deg in Y-Axis
	/// </summary>
	/// <returns></returns>
	public static ModelTransform BlockDefaultFp()
	{
		return new ModelTransform
		{
			Translation = new Vec3f(0f, -0.15f, 0.5f),
			Rotation = new Vec3f(0f, -20f, 0f),
			Scale = 1.3f
		};
	}

	/// <summary>
	/// Scale = 1, No Translation, Rotation by -45 deg in Y-Axis
	/// </summary>
	/// <returns></returns>
	public static ModelTransform BlockDefaultTp()
	{
		return new ModelTransform
		{
			Translation = new Vec3f(-2.1f, -1.8f, -1.5f),
			Rotation = new Vec3f(0f, -45f, -25f),
			Scale = 0.3f
		};
	}

	/// <summary>
	/// Scale = 1, No Translation, Rotation by -45 deg in Y-Axis, 1.5x scale
	/// </summary>
	/// <returns></returns>
	public static ModelTransform BlockDefaultGround()
	{
		return new ModelTransform
		{
			Translation = new Vec3f(),
			Rotation = new Vec3f(0f, -45f, 0f),
			Origin = new Vec3f(0.5f, 0f, 0.5f),
			Scale = 1.5f
		};
	}

	/// <summary>
	/// Scale = 1, No Translation, No Rotation
	/// </summary>
	/// <returns></returns>
	public static ModelTransform ItemDefaultGui()
	{
		return new ModelTransform
		{
			Translation = new Vec3f(3f, 1f, 0f),
			Rotation = new Vec3f(0f, 0f, 0f),
			Origin = new Vec3f(0.6f, 0.6f, 0f),
			Scale = 1f,
			Rotate = false
		};
	}

	/// <summary>
	/// Scale = 1, No Translation, Rotation by 180 deg in X-Axis
	/// </summary>
	/// <returns></returns>
	public static ModelTransform ItemDefaultFp()
	{
		return new ModelTransform
		{
			Translation = new Vec3f(0.05f, 0f, 0f),
			Rotation = new Vec3f(180f, 90f, -30f),
			Scale = 1f
		};
	}

	/// <summary>
	/// Scale = 1, No Translation, Rotation by 180 deg in X-Axis
	/// </summary>
	/// <returns></returns>
	public static ModelTransform ItemDefaultTp()
	{
		return new ModelTransform
		{
			Translation = new Vec3f(-1.5f, -1.6f, -1.4f),
			Rotation = new Vec3f(0f, -62f, 18f),
			Scale = 0.33f
		};
	}

	/// <summary>
	/// Creates a default transform for a model that is now on the ground
	/// </summary>
	/// <returns></returns>
	public static ModelTransform ItemDefaultGround()
	{
		return new ModelTransform
		{
			Translation = new Vec3f(),
			Rotation = new Vec3f(90f, 0f, 0f),
			Origin = new Vec3f(0.5f, 0.5f, 0.53f),
			Scale = 1.5f
		};
	}

	/// <summary>
	/// Makes sure that Translation and Rotation is not null
	/// </summary>
	public ModelTransform EnsureDefaultValues()
	{
		if (Translation == null)
		{
			Translation = new Vec3f();
		}
		if (Rotation == null)
		{
			Rotation = new Vec3f();
		}
		return this;
	}

	public ModelTransform WithRotation(Vec3f rot)
	{
		Rotation = rot;
		return this;
	}

	/// <summary>
	/// Clones this specific transform.
	/// </summary>
	/// <returns></returns>
	public ModelTransform Clone()
	{
		return new ModelTransform
		{
			Rotate = Rotate,
			Rotation = Rotation?.Clone(),
			Translation = Translation?.Clone(),
			ScaleXYZ = ScaleXYZ.Clone(),
			Origin = Origin?.Clone()
		};
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		if (Translation == null)
		{
			Translation = new Vec3f();
		}
		if (Rotation == null)
		{
			Rotation = new Vec3f();
		}
	}
}
