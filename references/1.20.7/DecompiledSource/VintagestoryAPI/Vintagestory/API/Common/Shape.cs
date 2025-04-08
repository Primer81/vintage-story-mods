using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

/// <summary>
/// The base shape for all json objects.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class Shape
{
	/// <summary>
	/// The collection of textures in the shape. The Dictionary keys are the texture short names, used in each ShapeElementFace
	/// <br />Note: from game version 1.20.4, this is <b>null on server-side</b> (except during asset loading start-up stage)
	/// </summary>
	[JsonProperty]
	public Dictionary<string, AssetLocation> Textures;

	/// <summary>
	/// The elements of the shape.
	/// </summary>
	[JsonProperty]
	public ShapeElement[] Elements;

	/// <summary>
	/// The animations for the shape.
	/// </summary>
	[JsonProperty]
	public Animation[] Animations;

	public Dictionary<uint, Animation> AnimationsByCrc32 = new Dictionary<uint, Animation>();

	/// <summary>
	/// The width of the texture. (default: 16)
	/// </summary>
	[JsonProperty]
	public int TextureWidth = 16;

	/// <summary>
	/// The height of the texture (default: 16) 
	/// </summary>
	[JsonProperty]
	public int TextureHeight = 16;

	[JsonProperty]
	public Dictionary<string, int[]> TextureSizes = new Dictionary<string, int[]>();

	public Dictionary<int, AnimationJoint> JointsById = new Dictionary<int, AnimationJoint>();

	[OnDeserialized]
	public void TrimTextureNamesAndResolveFaces(StreamingContext context)
	{
		ShapeElement[] elements = Elements;
		for (int i = 0; i < elements.Length; i++)
		{
			elements[i].TrimTextureNamesAndResolveFaces();
		}
	}

	public void ResolveReferences(ILogger errorLogger, string shapeNameForLogging)
	{
		CollectAndResolveReferences(errorLogger, shapeNameForLogging);
	}

	/// <summary>
	/// Attempts to resolve all references within the shape. Logs missing references them to the errorLogger
	/// </summary>
	/// <param name="errorLogger"></param>
	/// <param name="shapeNameForLogging"></param>
	public Dictionary<string, ShapeElement> CollectAndResolveReferences(ILogger errorLogger, string shapeNameForLogging)
	{
		Dictionary<string, ShapeElement> elementsByName = new Dictionary<string, ShapeElement>();
		ShapeElement[] Elements = this.Elements;
		CollectElements(Elements, elementsByName);
		Animation[] Animations = this.Animations;
		if (Animations != null)
		{
			foreach (Animation anim in Animations)
			{
				AnimationKeyFrame[] KeyFrames = anim.KeyFrames;
				foreach (AnimationKeyFrame keyframe in KeyFrames)
				{
					ResolveReferences(errorLogger, shapeNameForLogging, elementsByName, keyframe);
					foreach (AnimationKeyFrameElement value in keyframe.Elements.Values)
					{
						value.Frame = keyframe.Frame;
					}
				}
				if (anim.Code == null || anim.Code.Length == 0)
				{
					anim.Code = anim.Name.ToLowerInvariant().Replace(" ", "");
				}
				AnimationsByCrc32[AnimationMetaData.GetCrc32(anim.Code)] = anim;
			}
		}
		for (int i = 0; i < Elements.Length; i++)
		{
			Elements[i].ResolveRefernces();
		}
		return elementsByName;
	}

	/// <summary>
	/// Prefixes texturePrefixCode to all textures in this shape. Required pre-step for stepparenting. The long arguments StepParentShape() calls this method.
	/// </summary>
	/// <param name="texturePrefixCode"></param>
	/// <param name="damageEffect"></param>
	/// <returns></returns>
	public bool SubclassForStepParenting(string texturePrefixCode, float damageEffect = 0f)
	{
		HashSet<string> textureCodes = new HashSet<string>();
		ShapeElement[] elements = Elements;
		for (int i = 0; i < elements.Length; i++)
		{
			elements[i].WalkRecursive(delegate(ShapeElement el)
			{
				el.Name = texturePrefixCode + el.Name;
				if (damageEffect >= 0f)
				{
					el.DamageEffect = damageEffect;
				}
				ShapeElementFace[] facesResolved = el.FacesResolved;
				foreach (ShapeElementFace shapeElementFace in facesResolved)
				{
					if (shapeElementFace != null && shapeElementFace.Enabled)
					{
						textureCodes.Add(shapeElementFace.Texture);
						shapeElementFace.Texture = texturePrefixCode + shapeElementFace.Texture;
					}
				}
			});
		}
		if (Textures != null)
		{
			KeyValuePair<string, int[]>[] array = TextureSizes.ToArray();
			TextureSizes.Clear();
			KeyValuePair<string, int[]>[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				KeyValuePair<string, int[]> val = array2[i];
				TextureSizes[texturePrefixCode + val.Key] = val.Value;
				textureCodes.Remove(val.Key);
			}
			foreach (string code in textureCodes)
			{
				TextureSizes[texturePrefixCode + code] = new int[2] { TextureWidth, TextureHeight };
			}
		}
		if (Animations != null)
		{
			Animation[] animations = Animations;
			for (int i = 0; i < animations.Length; i++)
			{
				AnimationKeyFrame[] keyFrames = animations[i].KeyFrames;
				foreach (AnimationKeyFrame kf in keyFrames)
				{
					Dictionary<string, AnimationKeyFrameElement> scElements = new Dictionary<string, AnimationKeyFrameElement>();
					foreach (KeyValuePair<string, AnimationKeyFrameElement> kelem in kf.Elements)
					{
						scElements[texturePrefixCode + kelem.Key] = kelem.Value;
					}
					kf.Elements = scElements;
				}
			}
		}
		return true;
	}

	/// <summary>
	/// Adds a step parented shape to this shape. If you plan to cache the childShape use the shorter argument method and call SubclassForStepParenting() only once on it
	/// </summary>
	/// <param name="childShape"></param>
	/// <param name="texturePrefixCode"></param>
	/// <param name="childLocationForLogging"></param>
	/// <param name="parentLocationForLogging"></param>
	/// <param name="logger"></param>
	/// <param name="onTexture"></param>
	/// <param name="damageEffect"></param>
	/// <returns></returns>
	public bool StepParentShape(Shape childShape, string texturePrefixCode, string childLocationForLogging, string parentLocationForLogging, ILogger logger, Action<string, AssetLocation> onTexture, float damageEffect = 0f)
	{
		childShape.SubclassForStepParenting(texturePrefixCode, damageEffect);
		return StepParentShape(null, childShape.Elements, childShape, childLocationForLogging, parentLocationForLogging, logger, onTexture);
	}

	/// <summary>
	/// Adds a step parented shape to this shape, does not call the required pre-step SubclassForStepParenting()
	/// </summary>
	/// <param name="childShape"></param>
	/// <param name="childLocationForLogging"></param>
	/// <param name="parentLocationForLogging"></param>
	/// <param name="logger"></param>
	/// <param name="onTexture"></param>
	/// <returns></returns>
	public bool StepParentShape(Shape childShape, string childLocationForLogging, string parentLocationForLogging, ILogger logger, Action<string, AssetLocation> onTexture)
	{
		return StepParentShape(null, childShape.Elements, childShape, childLocationForLogging, parentLocationForLogging, logger, onTexture);
	}

	private bool StepParentShape(ShapeElement parentElem, ShapeElement[] elements, Shape childShape, string childLocationForLogging, string parentLocationForLogging, ILogger logger, Action<string, AssetLocation> onTexture)
	{
		bool anyElementAdded = false;
		foreach (ShapeElement childElem in elements)
		{
			if (childElem.Children != null)
			{
				bool added = StepParentShape(childElem, childElem.Children, childShape, childLocationForLogging, parentLocationForLogging, logger, onTexture);
				anyElementAdded = anyElementAdded || added;
			}
			if (childElem.StepParentName != null)
			{
				ShapeElement stepparentElem = GetElementByName(childElem.StepParentName, StringComparison.InvariantCultureIgnoreCase);
				if (stepparentElem == null)
				{
					logger.Warning("Step parented shape {0} requires step parent element with name {1}, but no such element was found in parent shape {2}. Will not be visible.", childLocationForLogging, childElem.StepParentName, parentLocationForLogging);
					continue;
				}
				if (parentElem != null)
				{
					parentElem.Children = parentElem.Children.Remove(childElem);
				}
				if (stepparentElem.Children == null)
				{
					stepparentElem.Children = new ShapeElement[1] { childElem };
				}
				else
				{
					stepparentElem.Children = stepparentElem.Children.Append(childElem);
				}
				childElem.ParentElement = stepparentElem;
				childElem.SetJointIdRecursive(stepparentElem.JointId);
				anyElementAdded = true;
			}
			else if (parentElem == null)
			{
				logger.Warning("Step parented shape {0} did not define a step parent element for parent shape {1}. Will not be visible.", childLocationForLogging, parentLocationForLogging);
			}
		}
		if (!anyElementAdded)
		{
			return false;
		}
		if (childShape.Animations != null && Animations != null)
		{
			Animation[] animations = childShape.Animations;
			foreach (Animation gearAnim in animations)
			{
				Animation entityAnim = Animations.FirstOrDefault((Animation anim) => anim.Code == gearAnim.Code);
				if (entityAnim == null)
				{
					continue;
				}
				AnimationKeyFrame[] gearKeyFrames = gearAnim.KeyFrames;
				foreach (AnimationKeyFrame gearKeyFrame in gearKeyFrames)
				{
					AnimationKeyFrame entityKeyFrame = getOrCreateKeyFrame(entityAnim, gearKeyFrame.Frame);
					foreach (KeyValuePair<string, AnimationKeyFrameElement> val4 in gearKeyFrame.Elements)
					{
						entityKeyFrame.Elements[val4.Key] = val4.Value;
					}
				}
			}
		}
		if (childShape.Textures != null)
		{
			foreach (KeyValuePair<string, AssetLocation> val3 in childShape.Textures)
			{
				onTexture(val3.Key, val3.Value);
			}
			foreach (KeyValuePair<string, int[]> val2 in childShape.TextureSizes)
			{
				TextureSizes[val2.Key] = val2.Value;
			}
			if (childShape.Textures.Count > 0 && childShape.TextureSizes.Count == 0)
			{
				foreach (KeyValuePair<string, AssetLocation> val in childShape.Textures)
				{
					TextureSizes[val.Key] = new int[2] { childShape.TextureWidth, childShape.TextureHeight };
				}
			}
		}
		return anyElementAdded;
	}

	private AnimationKeyFrame getOrCreateKeyFrame(Animation entityAnim, int frame)
	{
		for (int ei2 = 0; ei2 < entityAnim.KeyFrames.Length; ei2++)
		{
			AnimationKeyFrame entityKeyFrame = entityAnim.KeyFrames[ei2];
			if (entityKeyFrame.Frame == frame)
			{
				return entityKeyFrame;
			}
		}
		for (int ei = 0; ei < entityAnim.KeyFrames.Length; ei++)
		{
			if (entityAnim.KeyFrames[ei].Frame > frame)
			{
				AnimationKeyFrame kfm = new AnimationKeyFrame
				{
					Frame = frame,
					Elements = new Dictionary<string, AnimationKeyFrameElement>()
				};
				entityAnim.KeyFrames = entityAnim.KeyFrames.InsertAt(kfm, ei);
				return kfm;
			}
		}
		AnimationKeyFrame kf = new AnimationKeyFrame
		{
			Frame = frame,
			Elements = new Dictionary<string, AnimationKeyFrameElement>()
		};
		entityAnim.KeyFrames = entityAnim.KeyFrames.InsertAt(kf, 0);
		return kf;
	}

	/// <summary>
	/// Collects all the elements in the shape recursively.
	/// </summary>
	/// <param name="elements"></param>
	/// <param name="elementsByName"></param>
	public void CollectElements(ShapeElement[] elements, IDictionary<string, ShapeElement> elementsByName)
	{
		if (elements != null)
		{
			foreach (ShapeElement elem in elements)
			{
				elementsByName[elem.Name] = elem;
				CollectElements(elem.Children, elementsByName);
			}
		}
	}

	[Obsolete("Must call ResolveAndFindJoints(errorLogger, shapeName, joints) instead")]
	public void ResolveAndLoadJoints(params string[] requireJointsForElements)
	{
		ResolveAndFindJoints(null, null, null, requireJointsForElements);
	}

	/// <summary>
	/// Resolves all joints and loads them.
	/// </summary>
	/// <param name="shapeName"></param>
	/// <param name="requireJointsForElements"></param>
	/// <param name="errorLogger"></param>
	public void ResolveAndFindJoints(ILogger errorLogger, string shapeName, params string[] requireJointsForElements)
	{
		ResolveAndFindJoints(errorLogger, shapeName, null, requireJointsForElements);
	}

	public void ResolveAndFindJoints(ILogger errorLogger, string shapeName, Dictionary<string, ShapeElement> elementsByName, params string[] requireJointsForElements)
	{
		Animation[] Animations = this.Animations;
		if (Animations == null)
		{
			return;
		}
		if (elementsByName == null)
		{
			elementsByName = new Dictionary<string, ShapeElement>(Elements.Length);
			CollectElements(Elements, elementsByName);
		}
		int jointCount = 0;
		HashSet<string> AnimatedElements = new HashSet<string>();
		HashSet<string> animationCodes = new HashSet<string>();
		int version = -1;
		bool errorLogged = false;
		foreach (Animation anim in Animations)
		{
			if (!animationCodes.Add(anim.Code))
			{
				errorLogger?.Warning("Shape {0}: Two or more animations use the same code '{1}'. This will lead to undefined behavior.", shapeName, anim.Code);
			}
			if (version == -1)
			{
				version = anim.Version;
			}
			else if (version != anim.Version)
			{
				if (!errorLogged)
				{
					errorLogger?.Error("Shape {0} has mixed animation versions. This will cause incorrect animation blending.", shapeName);
				}
				errorLogged = true;
			}
			AnimationKeyFrame[] KeyFrames = anim.KeyFrames;
			foreach (AnimationKeyFrame kf in KeyFrames)
			{
				foreach (string key in kf.Elements.Keys)
				{
					AnimatedElements.Add(key);
				}
				kf.Resolve(elementsByName);
			}
		}
		foreach (ShapeElement value in elementsByName.Values)
		{
			value.JointId = 0;
		}
		int maxDepth = 0;
		foreach (string code in AnimatedElements)
		{
			elementsByName.TryGetValue(code, out var elem2);
			if (elem2 != null)
			{
				AnimationJoint animationJoint = new AnimationJoint();
				jointCount = (animationJoint.JointId = jointCount + 1);
				animationJoint.Element = elem2;
				AnimationJoint joint3 = animationJoint;
				JointsById[jointCount] = joint3;
				maxDepth = Math.Max(maxDepth, elem2.CountParents());
			}
		}
		foreach (string elemName in requireJointsForElements)
		{
			if (!AnimatedElements.Contains(elemName))
			{
				ShapeElement elem = GetElementByName(elemName);
				if (elem != null)
				{
					AnimationJoint animationJoint2 = new AnimationJoint();
					jointCount = (animationJoint2.JointId = jointCount + 1);
					animationJoint2.Element = elem;
					AnimationJoint joint2 = animationJoint2;
					JointsById[joint2.JointId] = joint2;
					maxDepth = Math.Max(maxDepth, elem.CountParents());
				}
			}
		}
		for (int depth = 0; depth <= maxDepth; depth++)
		{
			foreach (AnimationJoint joint in JointsById.Values)
			{
				if (joint.Element.CountParents() == depth)
				{
					joint.Element.SetJointId(joint.JointId);
				}
			}
		}
	}

	/// <summary>
	/// Tries to load the shape from the specified JSON file, with error logging
	/// <br />Returns null if the file could not be found, or if there was an error
	/// </summary>
	/// <param name="api"></param>
	/// <param name="shapePath"></param>
	/// <returns></returns>
	public static Shape TryGet(ICoreAPI api, string shapePath)
	{
		ShapeElement.locationForLogging = shapePath;
		try
		{
			return api.Assets.TryGet(shapePath)?.ToObject<Shape>();
		}
		catch (Exception e)
		{
			api.World.Logger.Error("Exception thrown when trying to load shape file {0}", shapePath);
			api.World.Logger.Error(e);
			return null;
		}
	}

	/// <summary>
	/// Tries to load the shape from the specified JSON file, with error logging
	/// <br />Returns null if the file could not be found, or if there was an error
	/// </summary>
	/// <param name="api"></param>
	/// <param name="shapePath"></param>
	/// <returns></returns>
	public static Shape TryGet(ICoreAPI api, AssetLocation shapePath)
	{
		ShapeElement.locationForLogging = shapePath;
		try
		{
			return api.Assets.TryGet(shapePath)?.ToObject<Shape>();
		}
		catch (Exception e)
		{
			api.World.Logger.Error("Exception thrown when trying to load shape file {0}\n{1}", shapePath, e.Message);
			return null;
		}
	}

	public void WalkElements(string wildcardpath, Action<ShapeElement> onElement)
	{
		walkElements(Elements, wildcardpath, onElement);
	}

	private void walkElements(ShapeElement[] elements, string wildcardpath, Action<ShapeElement> onElement)
	{
		if (elements == null)
		{
			return;
		}
		int slashIndex = wildcardpath.IndexOf('/');
		string pathElem;
		string subPath;
		if (slashIndex >= 0)
		{
			pathElem = wildcardpath.Substring(0, slashIndex);
			subPath = wildcardpath.Substring(slashIndex + 1);
		}
		else
		{
			pathElem = wildcardpath;
			subPath = "";
			if (pathElem == "*")
			{
				subPath = "*";
			}
		}
		foreach (ShapeElement elem in elements)
		{
			if (pathElem == "*" || elem.Name.Equals(pathElem, StringComparison.InvariantCultureIgnoreCase))
			{
				onElement(elem);
				if (elem.Children != null)
				{
					walkElements(elem.Children, subPath, onElement);
				}
			}
		}
	}

	/// <summary>
	/// Recursively searches the element by name from the shape.
	/// </summary>
	/// <param name="name">The name of the element to get.</param>
	/// <param name="stringComparison">Ignored but retained for API backwards compatibility. The implementation always uses OrdinalIgnoreCase comparison</param>
	/// <returns>The shape element or null if none was found</returns>
	public ShapeElement GetElementByName(string name, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
	{
		if (Elements == null)
		{
			return null;
		}
		return GetElementByName(name, Elements);
	}

	private ShapeElement GetElementByName(string name, ShapeElement[] elems)
	{
		foreach (ShapeElement elem in elems)
		{
			if (elem.Name.EqualsFastIgnoreCase(name))
			{
				return elem;
			}
			if (elem.Children != null)
			{
				ShapeElement foundElem = GetElementByName(name, elem.Children);
				if (foundElem != null)
				{
					return foundElem;
				}
			}
		}
		return null;
	}

	public void RemoveElements(string[] elementNames)
	{
		if (elementNames != null)
		{
			foreach (string val in elementNames)
			{
				RemoveElementByName(val);
				RemoveElementByName("skinpart-" + val);
			}
		}
	}

	/// <summary>
	/// Removes *all* elements with given name
	/// </summary>
	/// <param name="name"></param>
	/// <param name="stringComparison"></param>
	/// <returns></returns>
	public bool RemoveElementByName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
	{
		return RemoveElementByName(name, ref Elements, stringComparison);
	}

	private bool RemoveElementByName(string name, ref ShapeElement[] elems, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
	{
		if (elems == null)
		{
			return false;
		}
		bool removed = false;
		for (int i = 0; i < elems.Length; i++)
		{
			if (elems[i].Name.Equals(name, stringComparison))
			{
				elems = elems.RemoveEntry(i);
				removed = true;
				i--;
			}
			else if (RemoveElementByName(name, ref elems[i].Children, stringComparison))
			{
				removed = true;
			}
		}
		return removed;
	}

	public ShapeElement[] CloneElements()
	{
		if (Elements == null)
		{
			return null;
		}
		ShapeElement[] elems = new ShapeElement[Elements.Length];
		for (int i = 0; i < elems.Length; i++)
		{
			elems[i] = Elements[i].Clone();
		}
		return elems;
	}

	public Animation[] CloneAnimations()
	{
		Animation[] Animations = this.Animations;
		if (Animations == null)
		{
			return null;
		}
		Animation[] elems = new Animation[Animations.Length];
		for (int i = 0; i < Animations.Length; i++)
		{
			elems[i] = Animations[i].Clone();
		}
		return elems;
	}

	public void CacheInvTransforms()
	{
		CacheInvTransforms(Elements);
	}

	public static void CacheInvTransforms(ShapeElement[] elements)
	{
		if (elements != null)
		{
			for (int i = 0; i < elements.Length; i++)
			{
				elements[i].CacheInverseTransformMatrix();
				CacheInvTransforms(elements[i].Children);
			}
		}
	}

	/// <summary>
	/// Creates a deep copy of the shape. If the shape has animations, then it also resolves references and joints to ensure the cloned shape is fully initialized
	/// </summary>
	/// <returns></returns>
	public Shape Clone()
	{
		Shape shape = new Shape
		{
			Elements = CloneElements(),
			Animations = CloneAnimations(),
			TextureWidth = TextureWidth,
			TextureHeight = TextureHeight,
			TextureSizes = TextureSizes,
			Textures = Textures
		};
		for (int i = 0; i < shape.Elements.Length; i++)
		{
			shape.Elements[i].ResolveRefernces();
		}
		return shape;
	}

	public void InitForAnimations(ILogger logger, string shapeNameForLogging, params string[] requireJointsForElements)
	{
		CacheInvTransforms();
		Dictionary<string, ShapeElement> elementsByName = CollectAndResolveReferences(logger, shapeNameForLogging);
		ResolveAndFindJoints(logger, shapeNameForLogging, elementsByName, requireJointsForElements);
	}

	private void ResolveReferences(ILogger errorLogger, string shapeName, Dictionary<string, ShapeElement> elementsByName, AnimationKeyFrame kf)
	{
		if (kf?.Elements == null)
		{
			return;
		}
		foreach (KeyValuePair<string, AnimationKeyFrameElement> val in kf.Elements)
		{
			if (elementsByName.TryGetValue(val.Key, out var elem))
			{
				val.Value.ForElement = elem;
				continue;
			}
			errorLogger.Error("Shape {0} has a key frame element for which the referencing shape element {1} cannot be found.", shapeName, val.Key);
			val.Value.ForElement = new ShapeElement();
		}
	}

	public virtual void FreeRAMServer()
	{
		Textures = null;
		if (this.Elements != null)
		{
			ShapeElement[] elements = this.Elements;
			for (int k = 0; k < elements.Length; k++)
			{
				elements[k].FreeRAMServer();
			}
		}
		Animation[] Animations = this.Animations;
		if (Animations == null)
		{
			return;
		}
		foreach (Animation obj in Animations)
		{
			obj.Code = obj.Code.DeDuplicate();
			obj.Name = obj.Name.DeDuplicate();
			AnimationKeyFrame[] KeyFrames = obj.KeyFrames;
			for (int j = 0; j < KeyFrames.Length; j++)
			{
				Dictionary<string, AnimationKeyFrameElement> Elements = KeyFrames[j].Elements;
				if (Elements == null)
				{
					continue;
				}
				Dictionary<string, AnimationKeyFrameElement> newElements = new Dictionary<string, AnimationKeyFrameElement>(Elements.Count);
				foreach (KeyValuePair<string, AnimationKeyFrameElement> entry in Elements)
				{
					newElements[entry.Key.DeDuplicate()] = entry.Value;
					entry.Value.ForElement.Name = entry.Value.ForElement.Name.DeDuplicate();
				}
				KeyFrames[j].Elements = newElements;
			}
		}
	}
}
