#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

//
// Summary:
//     The base shape for all json objects.
[JsonObject(/*Could not decode attribute arguments.*/)]
public class Shape
{
    //
    // Summary:
    //     The collection of textures in the shape. The Dictionary keys are the texture
    //     short names, used in each ShapeElementFace
    //     Note: from game version 1.20.4, this is null on server-side (except during asset
    //     loading start-up stage)
    [JsonProperty]
    public Dictionary<string, AssetLocation> Textures;

    //
    // Summary:
    //     The elements of the shape.
    [JsonProperty]
    public ShapeElement[] Elements;

    //
    // Summary:
    //     The animations for the shape.
    [JsonProperty]
    public Animation[] Animations;

    public Dictionary<uint, Animation> AnimationsByCrc32 = new Dictionary<uint, Animation>();

    //
    // Summary:
    //     The width of the texture. (default: 16)
    [JsonProperty]
    public int TextureWidth = 16;

    //
    // Summary:
    //     The height of the texture (default: 16)
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

    //
    // Summary:
    //     Attempts to resolve all references within the shape. Logs missing references
    //     them to the errorLogger
    //
    // Parameters:
    //   errorLogger:
    //
    //   shapeNameForLogging:
    public Dictionary<string, ShapeElement> CollectAndResolveReferences(ILogger errorLogger, string shapeNameForLogging)
    {
        Dictionary<string, ShapeElement> dictionary = new Dictionary<string, ShapeElement>();
        ShapeElement[] elements = Elements;
        CollectElements(elements, dictionary);
        Animation[] animations = Animations;
        if (animations != null)
        {
            foreach (Animation animation in animations)
            {
                AnimationKeyFrame[] keyFrames = animation.KeyFrames;
                foreach (AnimationKeyFrame animationKeyFrame in keyFrames)
                {
                    ResolveReferences(errorLogger, shapeNameForLogging, dictionary, animationKeyFrame);
                    foreach (AnimationKeyFrameElement value in animationKeyFrame.Elements.Values)
                    {
                        value.Frame = animationKeyFrame.Frame;
                    }
                }

                if (animation.Code == null || animation.Code.Length == 0)
                {
                    animation.Code = animation.Name.ToLowerInvariant().Replace(" ", "");
                }

                AnimationsByCrc32[AnimationMetaData.GetCrc32(animation.Code)] = animation;
            }
        }

        for (int k = 0; k < elements.Length; k++)
        {
            elements[k].ResolveRefernces();
        }

        return dictionary;
    }

    //
    // Summary:
    //     Prefixes texturePrefixCode to all textures in this shape. Required pre-step for
    //     stepparenting. The long arguments StepParentShape() calls this method.
    //
    // Parameters:
    //   texturePrefixCode:
    //
    //   damageEffect:
    public bool SubclassForStepParenting(string texturePrefixCode, float damageEffect = 0f)
    {
        HashSet<string> textureCodes = new HashSet<string>();
        ShapeElement[] elements = Elements;
        for (int i = 0; i < elements.Length; i++)
        {
            elements[i].WalkRecursive(delegate (ShapeElement el)
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
                KeyValuePair<string, int[]> keyValuePair = array2[i];
                TextureSizes[texturePrefixCode + keyValuePair.Key] = keyValuePair.Value;
                textureCodes.Remove(keyValuePair.Key);
            }

            foreach (string item in textureCodes)
            {
                TextureSizes[texturePrefixCode + item] = new int[2] { TextureWidth, TextureHeight };
            }
        }

        if (Animations != null)
        {
            Animation[] animations = Animations;
            for (int i = 0; i < animations.Length; i++)
            {
                AnimationKeyFrame[] keyFrames = animations[i].KeyFrames;
                foreach (AnimationKeyFrame animationKeyFrame in keyFrames)
                {
                    Dictionary<string, AnimationKeyFrameElement> dictionary = new Dictionary<string, AnimationKeyFrameElement>();
                    foreach (KeyValuePair<string, AnimationKeyFrameElement> element in animationKeyFrame.Elements)
                    {
                        dictionary[texturePrefixCode + element.Key] = element.Value;
                    }

                    animationKeyFrame.Elements = dictionary;
                }
            }
        }

        return true;
    }

    //
    // Summary:
    //     Adds a step parented shape to this shape. If you plan to cache the childShape
    //     use the shorter argument method and call SubclassForStepParenting() only once
    //     on it
    //
    // Parameters:
    //   childShape:
    //
    //   texturePrefixCode:
    //
    //   childLocationForLogging:
    //
    //   parentLocationForLogging:
    //
    //   logger:
    //
    //   onTexture:
    //
    //   damageEffect:
    public bool StepParentShape(Shape childShape, string texturePrefixCode, string childLocationForLogging, string parentLocationForLogging, ILogger logger, Action<string, AssetLocation> onTexture, float damageEffect = 0f)
    {
        childShape.SubclassForStepParenting(texturePrefixCode, damageEffect);
        return StepParentShape(null, childShape.Elements, childShape, childLocationForLogging, parentLocationForLogging, logger, onTexture);
    }

    //
    // Summary:
    //     Adds a step parented shape to this shape, does not call the required pre-step
    //     SubclassForStepParenting()
    //
    // Parameters:
    //   childShape:
    //
    //   childLocationForLogging:
    //
    //   parentLocationForLogging:
    //
    //   logger:
    //
    //   onTexture:
    public bool StepParentShape(Shape childShape, string childLocationForLogging, string parentLocationForLogging, ILogger logger, Action<string, AssetLocation> onTexture)
    {
        return StepParentShape(null, childShape.Elements, childShape, childLocationForLogging, parentLocationForLogging, logger, onTexture);
    }

    private bool StepParentShape(ShapeElement parentElem, ShapeElement[] elements, Shape childShape, string childLocationForLogging, string parentLocationForLogging, ILogger logger, Action<string, AssetLocation> onTexture)
    {
        bool flag = false;
        foreach (ShapeElement shapeElement in elements)
        {
            if (shapeElement.Children != null)
            {
                bool flag2 = StepParentShape(shapeElement, shapeElement.Children, childShape, childLocationForLogging, parentLocationForLogging, logger, onTexture);
                flag = flag || flag2;
            }

            if (shapeElement.StepParentName != null)
            {
                ShapeElement elementByName = GetElementByName(shapeElement.StepParentName, StringComparison.InvariantCultureIgnoreCase);
                if (elementByName == null)
                {
                    logger.Warning("Step parented shape {0} requires step parent element with name {1}, but no such element was found in parent shape {2}. Will not be visible.", childLocationForLogging, shapeElement.StepParentName, parentLocationForLogging);
                    continue;
                }

                if (parentElem != null)
                {
                    parentElem.Children = parentElem.Children.Remove(shapeElement);
                }

                if (elementByName.Children == null)
                {
                    elementByName.Children = new ShapeElement[1] { shapeElement };
                }
                else
                {
                    elementByName.Children = elementByName.Children.Append(shapeElement);
                }

                shapeElement.ParentElement = elementByName;
                shapeElement.SetJointIdRecursive(elementByName.JointId);
                flag = true;
            }
            else if (parentElem == null)
            {
                logger.Warning("Step parented shape {0} did not define a step parent element for parent shape {1}. Will not be visible.", childLocationForLogging, parentLocationForLogging);
            }
        }

        if (!flag)
        {
            return false;
        }

        if (childShape.Animations != null && Animations != null)
        {
            Animation[] animations = childShape.Animations;
            foreach (Animation gearAnim in animations)
            {
                Animation animation = Animations.FirstOrDefault((Animation anim) => anim.Code == gearAnim.Code);
                if (animation == null)
                {
                    continue;
                }

                AnimationKeyFrame[] keyFrames = gearAnim.KeyFrames;
                foreach (AnimationKeyFrame animationKeyFrame in keyFrames)
                {
                    AnimationKeyFrame orCreateKeyFrame = getOrCreateKeyFrame(animation, animationKeyFrame.Frame);
                    foreach (KeyValuePair<string, AnimationKeyFrameElement> element in animationKeyFrame.Elements)
                    {
                        orCreateKeyFrame.Elements[element.Key] = element.Value;
                    }
                }
            }
        }

        if (childShape.Textures != null)
        {
            foreach (KeyValuePair<string, AssetLocation> texture in childShape.Textures)
            {
                onTexture(texture.Key, texture.Value);
            }

            foreach (KeyValuePair<string, int[]> textureSize in childShape.TextureSizes)
            {
                TextureSizes[textureSize.Key] = textureSize.Value;
            }

            if (childShape.Textures.Count > 0 && childShape.TextureSizes.Count == 0)
            {
                foreach (KeyValuePair<string, AssetLocation> texture2 in childShape.Textures)
                {
                    TextureSizes[texture2.Key] = new int[2] { childShape.TextureWidth, childShape.TextureHeight };
                }
            }
        }

        return flag;
    }

    private AnimationKeyFrame getOrCreateKeyFrame(Animation entityAnim, int frame)
    {
        for (int i = 0; i < entityAnim.KeyFrames.Length; i++)
        {
            AnimationKeyFrame animationKeyFrame = entityAnim.KeyFrames[i];
            if (animationKeyFrame.Frame == frame)
            {
                return animationKeyFrame;
            }
        }

        for (int j = 0; j < entityAnim.KeyFrames.Length; j++)
        {
            if (entityAnim.KeyFrames[j].Frame > frame)
            {
                AnimationKeyFrame animationKeyFrame2 = new AnimationKeyFrame
                {
                    Frame = frame,
                    Elements = new Dictionary<string, AnimationKeyFrameElement>()
                };
                entityAnim.KeyFrames = entityAnim.KeyFrames.InsertAt(animationKeyFrame2, j);
                return animationKeyFrame2;
            }
        }

        AnimationKeyFrame animationKeyFrame3 = new AnimationKeyFrame
        {
            Frame = frame,
            Elements = new Dictionary<string, AnimationKeyFrameElement>()
        };
        entityAnim.KeyFrames = entityAnim.KeyFrames.InsertAt(animationKeyFrame3, 0);
        return animationKeyFrame3;
    }

    //
    // Summary:
    //     Collects all the elements in the shape recursively.
    //
    // Parameters:
    //   elements:
    //
    //   elementsByName:
    public void CollectElements(ShapeElement[] elements, IDictionary<string, ShapeElement> elementsByName)
    {
        if (elements != null)
        {
            foreach (ShapeElement shapeElement in elements)
            {
                elementsByName[shapeElement.Name] = shapeElement;
                CollectElements(shapeElement.Children, elementsByName);
            }
        }
    }

    [Obsolete("Must call ResolveAndFindJoints(errorLogger, shapeName, joints) instead")]
    public void ResolveAndLoadJoints(params string[] requireJointsForElements)
    {
        ResolveAndFindJoints(null, null, null, requireJointsForElements);
    }

    //
    // Summary:
    //     Resolves all joints and loads them.
    //
    // Parameters:
    //   shapeName:
    //
    //   requireJointsForElements:
    //
    //   errorLogger:
    public void ResolveAndFindJoints(ILogger errorLogger, string shapeName, params string[] requireJointsForElements)
    {
        ResolveAndFindJoints(errorLogger, shapeName, null, requireJointsForElements);
    }

    public void ResolveAndFindJoints(ILogger errorLogger, string shapeName, Dictionary<string, ShapeElement> elementsByName, params string[] requireJointsForElements)
    {
        Animation[] animations = Animations;
        if (animations == null)
        {
            return;
        }

        if (elementsByName == null)
        {
            elementsByName = new Dictionary<string, ShapeElement>(Elements.Length);
            CollectElements(Elements, elementsByName);
        }

        int num = 0;
        HashSet<string> hashSet = new HashSet<string>();
        HashSet<string> hashSet2 = new HashSet<string>();
        int num2 = -1;
        bool flag = false;
        foreach (Animation animation in animations)
        {
            if (!hashSet2.Add(animation.Code))
            {
                errorLogger?.Warning("Shape {0}: Two or more animations use the same code '{1}'. This will lead to undefined behavior.", shapeName, animation.Code);
            }

            if (num2 == -1)
            {
                num2 = animation.Version;
            }
            else if (num2 != animation.Version)
            {
                if (!flag)
                {
                    errorLogger?.Error("Shape {0} has mixed animation versions. This will cause incorrect animation blending.", shapeName);
                }

                flag = true;
            }

            AnimationKeyFrame[] keyFrames = animation.KeyFrames;
            foreach (AnimationKeyFrame animationKeyFrame in keyFrames)
            {
                foreach (string key in animationKeyFrame.Elements.Keys)
                {
                    hashSet.Add(key);
                }

                animationKeyFrame.Resolve(elementsByName);
            }
        }

        foreach (ShapeElement value3 in elementsByName.Values)
        {
            value3.JointId = 0;
        }

        int num3 = 0;
        foreach (string item in hashSet)
        {
            elementsByName.TryGetValue(item, out var value);
            if (value != null)
            {
                AnimationJoint animationJoint = new AnimationJoint();
                num = (animationJoint.JointId = num + 1);
                animationJoint.Element = value;
                AnimationJoint value2 = animationJoint;
                JointsById[num] = value2;
                num3 = Math.Max(num3, value.CountParents());
            }
        }

        foreach (string text in requireJointsForElements)
        {
            if (!hashSet.Contains(text))
            {
                ShapeElement elementByName = GetElementByName(text);
                if (elementByName != null)
                {
                    AnimationJoint animationJoint2 = new AnimationJoint();
                    num = (animationJoint2.JointId = num + 1);
                    animationJoint2.Element = elementByName;
                    AnimationJoint animationJoint3 = animationJoint2;
                    JointsById[animationJoint3.JointId] = animationJoint3;
                    num3 = Math.Max(num3, elementByName.CountParents());
                }
            }
        }

        for (int l = 0; l <= num3; l++)
        {
            foreach (AnimationJoint value4 in JointsById.Values)
            {
                if (value4.Element.CountParents() == l)
                {
                    value4.Element.SetJointId(value4.JointId);
                }
            }
        }
    }

    //
    // Summary:
    //     Tries to load the shape from the specified JSON file, with error logging
    //     Returns null if the file could not be found, or if there was an error
    //
    // Parameters:
    //   api:
    //
    //   shapePath:
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

    //
    // Summary:
    //     Tries to load the shape from the specified JSON file, with error logging
    //     Returns null if the file could not be found, or if there was an error
    //
    // Parameters:
    //   api:
    //
    //   shapePath:
    public static Shape TryGet(ICoreAPI api, AssetLocation shapePath)
    {
        ShapeElement.locationForLogging = shapePath;
        try
        {
            return api.Assets.TryGet(shapePath)?.ToObject<Shape>();
        }
        catch (Exception ex)
        {
            api.World.Logger.Error("Exception thrown when trying to load shape file {0}\n{1}", shapePath, ex.Message);
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

        int num = wildcardpath.IndexOf('/');
        string text;
        string wildcardpath2;
        if (num >= 0)
        {
            text = wildcardpath.Substring(0, num);
            wildcardpath2 = wildcardpath.Substring(num + 1);
        }
        else
        {
            text = wildcardpath;
            wildcardpath2 = "";
            if (text == "*")
            {
                wildcardpath2 = "*";
            }
        }

        foreach (ShapeElement shapeElement in elements)
        {
            if (text == "*" || shapeElement.Name.Equals(text, StringComparison.InvariantCultureIgnoreCase))
            {
                onElement(shapeElement);
                if (shapeElement.Children != null)
                {
                    walkElements(shapeElement.Children, wildcardpath2, onElement);
                }
            }
        }
    }

    //
    // Summary:
    //     Recursively searches the element by name from the shape.
    //
    // Parameters:
    //   name:
    //     The name of the element to get.
    //
    //   stringComparison:
    //     Ignored but retained for API backwards compatibility. The implementation always
    //     uses OrdinalIgnoreCase comparison
    //
    // Returns:
    //     The shape element or null if none was found
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
        foreach (ShapeElement shapeElement in elems)
        {
            if (shapeElement.Name.EqualsFastIgnoreCase(name))
            {
                return shapeElement;
            }

            if (shapeElement.Children != null)
            {
                ShapeElement elementByName = GetElementByName(name, shapeElement.Children);
                if (elementByName != null)
                {
                    return elementByName;
                }
            }
        }

        return null;
    }

    public void RemoveElements(string[] elementNames)
    {
        if (elementNames != null)
        {
            foreach (string text in elementNames)
            {
                RemoveElementByName(text);
                RemoveElementByName("skinpart-" + text);
            }
        }
    }

    //
    // Summary:
    //     Removes *all* elements with given name
    //
    // Parameters:
    //   name:
    //
    //   stringComparison:
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

        bool result = false;
        for (int i = 0; i < elems.Length; i++)
        {
            if (elems[i].Name.Equals(name, stringComparison))
            {
                elems = elems.RemoveEntry(i);
                result = true;
                i--;
            }
            else if (RemoveElementByName(name, ref elems[i].Children, stringComparison))
            {
                result = true;
            }
        }

        return result;
    }

    public ShapeElement[] CloneElements()
    {
        if (Elements == null)
        {
            return null;
        }

        ShapeElement[] array = new ShapeElement[Elements.Length];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = Elements[i].Clone();
        }

        return array;
    }

    public Animation[] CloneAnimations()
    {
        Animation[] animations = Animations;
        if (animations == null)
        {
            return null;
        }

        Animation[] array = new Animation[animations.Length];
        for (int i = 0; i < animations.Length; i++)
        {
            array[i] = animations[i].Clone();
        }

        return array;
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

    //
    // Summary:
    //     Creates a deep copy of the shape. If the shape has animations, then it also resolves
    //     references and joints to ensure the cloned shape is fully initialized
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

        foreach (KeyValuePair<string, AnimationKeyFrameElement> element in kf.Elements)
        {
            if (elementsByName.TryGetValue(element.Key, out var value))
            {
                element.Value.ForElement = value;
                continue;
            }

            errorLogger.Error("Shape {0} has a key frame element for which the referencing shape element {1} cannot be found.", shapeName, element.Key);
            element.Value.ForElement = new ShapeElement();
        }
    }

    public virtual void FreeRAMServer()
    {
        Textures = null;
        if (Elements != null)
        {
            ShapeElement[] elements = Elements;
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i].FreeRAMServer();
            }
        }

        Animation[] animations = Animations;
        if (animations == null)
        {
            return;
        }

        foreach (Animation obj in animations)
        {
            obj.Code = obj.Code.DeDuplicate();
            obj.Name = obj.Name.DeDuplicate();
            AnimationKeyFrame[] keyFrames = obj.KeyFrames;
            for (int k = 0; k < keyFrames.Length; k++)
            {
                Dictionary<string, AnimationKeyFrameElement> elements2 = keyFrames[k].Elements;
                if (elements2 == null)
                {
                    continue;
                }

                Dictionary<string, AnimationKeyFrameElement> dictionary = new Dictionary<string, AnimationKeyFrameElement>(elements2.Count);
                foreach (KeyValuePair<string, AnimationKeyFrameElement> item in elements2)
                {
                    dictionary[item.Key.DeDuplicate()] = item.Value;
                    item.Value.ForElement.Name = item.Value.ForElement.Name.DeDuplicate();
                }

                keyFrames[k].Elements = dictionary;
            }
        }
    }
}
#if false // Decompilation log
'181' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
