using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// The position of an element.
/// </summary>
public class ElementPose
{
	/// <summary>
	/// The element this positioning is for.
	/// </summary>
	public ShapeElement ForElement;

	/// <summary>
	/// The model matrix of this element.
	/// </summary>
	public float[] AnimModelMatrix;

	public List<ElementPose> ChildElementPoses = new List<ElementPose>();

	public float degOffX;

	public float degOffY;

	public float degOffZ;

	public float degX;

	public float degY;

	public float degZ;

	public float scaleX = 1f;

	public float scaleY = 1f;

	public float scaleZ = 1f;

	public float translateX;

	public float translateY;

	public float translateZ;

	public bool RotShortestDistanceX;

	public bool RotShortestDistanceY;

	public bool RotShortestDistanceZ;

	public void Clear()
	{
		degX = 0f;
		degY = 0f;
		degZ = 0f;
		scaleX = 1f;
		scaleY = 1f;
		scaleZ = 1f;
		translateX = 0f;
		translateY = 0f;
		translateZ = 0f;
	}

	public void Add(ElementPose tf, ElementPose tfNext, float l, float weight)
	{
		if (tf.RotShortestDistanceX)
		{
			float distX = GameMath.AngleDegDistance(tf.degX, tfNext.degX);
			degX += tf.degX + distX * l;
		}
		else
		{
			degX += (tf.degX * (1f - l) + tfNext.degX * l) * weight;
		}
		if (tf.RotShortestDistanceY)
		{
			float distY = GameMath.AngleDegDistance(tf.degY, tfNext.degY);
			degY += tf.degY + distY * l;
		}
		else
		{
			degY += (tf.degY * (1f - l) + tfNext.degY * l) * weight;
		}
		if (tf.RotShortestDistanceZ)
		{
			float distZ = GameMath.AngleDegDistance(tf.degZ, tfNext.degZ);
			degZ += tf.degZ + distZ * l;
		}
		else
		{
			degZ += (tf.degZ * (1f - l) + tfNext.degZ * l) * weight;
		}
		scaleX += ((tf.scaleX - 1f) * (1f - l) + (tfNext.scaleX - 1f) * l) * weight;
		scaleY += ((tf.scaleY - 1f) * (1f - l) + (tfNext.scaleY - 1f) * l) * weight;
		scaleZ += ((tf.scaleZ - 1f) * (1f - l) + (tfNext.scaleZ - 1f) * l) * weight;
		translateX += (tf.translateX * (1f - l) + tfNext.translateX * l) * weight;
		translateY += (tf.translateY * (1f - l) + tfNext.translateY * l) * weight;
		translateZ += (tf.translateZ * (1f - l) + tfNext.translateZ * l) * weight;
	}

	internal void SetMat(float[] modelMatrix)
	{
		for (int i = 0; i < 16; i++)
		{
			AnimModelMatrix[i] = modelMatrix[i];
		}
	}

	public override string ToString()
	{
		return $"translate: {translateX}/{translateY}/{translateZ}, rotate: {degX}/{degY}/{degZ}, scale: {scaleX}/{scaleY}/{scaleZ}";
	}
}
