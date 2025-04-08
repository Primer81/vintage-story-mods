using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

[JsonObject(MemberSerialization.OptIn)]
public class AnimationKeyFrame
{
	/// <summary>
	/// The ID of the keyframe.
	/// </summary>
	[JsonProperty]
	public int Frame;

	/// <summary>
	/// The elements of the keyframe.
	/// </summary>
	[JsonProperty]
	public Dictionary<string, AnimationKeyFrameElement> Elements;

	private IDictionary<ShapeElement, AnimationKeyFrameElement> ElementsByShapeElement;

	/// <summary>
	/// Resolves the keyframe animation for which elements are important.
	/// </summary>
	/// <param name="allElements"></param>
	[Obsolete("Use the overload taking a Dictionary argument instead for higher performance on large sets")]
	public void Resolve(ShapeElement[] allElements)
	{
		if (Elements == null)
		{
			return;
		}
		foreach (KeyValuePair<string, AnimationKeyFrameElement> element in Elements)
		{
			element.Value.Frame = Frame;
		}
		foreach (ShapeElement elem in allElements)
		{
			if (elem != null && Elements.TryGetValue(elem.Name, out var kelem))
			{
				ElementsByShapeElement[elem] = kelem;
			}
		}
	}

	/// <summary>
	/// Resolves the keyframe animation for which elements are important.
	/// </summary>
	/// <param name="allElements"></param>
	public void Resolve(Dictionary<string, ShapeElement> allElements)
	{
		if (Elements == null)
		{
			return;
		}
		ElementsByShapeElement = new FastSmallDictionary<ShapeElement, AnimationKeyFrameElement>(Elements.Count);
		foreach (KeyValuePair<string, AnimationKeyFrameElement> val in Elements)
		{
			AnimationKeyFrameElement kelem = val.Value;
			kelem.Frame = Frame;
			allElements.TryGetValue(val.Key, out var elem);
			if (elem != null)
			{
				ElementsByShapeElement[elem] = kelem;
			}
		}
	}

	internal AnimationKeyFrameElement GetKeyFrameElement(ShapeElement forElem)
	{
		if (forElem == null)
		{
			return null;
		}
		ElementsByShapeElement.TryGetValue(forElem, out var kelem);
		return kelem;
	}

	public AnimationKeyFrame Clone()
	{
		return new AnimationKeyFrame
		{
			Elements = ((Elements == null) ? null : new Dictionary<string, AnimationKeyFrameElement>(Elements)),
			Frame = Frame
		};
	}
}
