#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     Represents a shape animation and can calculate the transformation matrices for
//     each frame to be sent to the shader Process 1. For each frame, for each root
//     element, calculate the transformation matrix. Curent model matrix is identy mat.
//     1.1. Get previous and next key frame. Apply translation, rotation and scale to
//     model matrix. 1.2. Store this matrix as animationmatrix in list 1.3. For each
//     child element 1.3.1. Multiply local transformation matrix with the animation
//     matrix. This matrix is now the curent model matrix. Go to 1 with child elements
//     as root elems 2. For each frame, for each joint 2.1. Calculate the inverse model
//     matrix 2.2. Multiply stored animationmatrix with the inverse model matrix 3.
//     done
[JsonObject(MemberSerialization.OptIn)]
public class Animation
{
    [JsonProperty]
    public int QuantityFrames;

    [JsonProperty]
    public string Name;

    [JsonProperty]
    public string Code;

    [JsonProperty]
    public int Version;

    [JsonProperty]
    public bool EaseAnimationSpeed;

    [JsonProperty]
    public AnimationKeyFrame[] KeyFrames;

    [JsonProperty]
    public EnumEntityActivityStoppedHandling OnActivityStopped = EnumEntityActivityStoppedHandling.Rewind;

    [JsonProperty]
    public EnumEntityAnimationEndHandling OnAnimationEnd;

    public uint CodeCrc32;

    public AnimationFrame[][] PrevNextKeyFrameByFrame;

    protected HashSet<int> jointsDone = new HashSet<int>();

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        if (Code == null)
        {
            Code = Name;
        }

        CodeCrc32 = AnimationMetaData.GetCrc32(Code);
    }

    //
    // Summary:
    //     Compiles the animation into a bunch of matrices, 31 matrices per frame.
    //
    // Parameters:
    //   rootElements:
    //
    //   jointsById:
    //
    //   recursive:
    //     When false, will only do root elements
    public void GenerateAllFrames(ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, bool recursive = true)
    {
        for (int i = 0; i < rootElements.Length; i++)
        {
            rootElements[i].CacheInverseTransformMatrixRecursive();
        }

        AnimationFrame[] array = new AnimationFrame[KeyFrames.Length];
        for (int j = 0; j < array.Length; j++)
        {
            array[j] = new AnimationFrame
            {
                FrameNumber = KeyFrames[j].Frame
            };
        }

        if (KeyFrames.Length == 0)
        {
            throw new Exception("Animation '" + Code + "' has no keyframes, this will cause other errors every time it is ticked");
        }

        if (jointsById.Count >= GlobalConstants.MaxAnimatedElements)
        {
            if (GlobalConstants.MaxAnimatedElements < 46 && jointsById.Count <= 46)
            {
                throw new Exception("Max joint cap of " + GlobalConstants.MaxAnimatedElements + " reached, needs to be at least " + jointsById.Count + ". In clientsettings.json, please try increasing the \"maxAnimatedElements\": setting to 46.  This works for most GPUs.  Otherwise you might need to disable the creature.");
            }

            throw new Exception("A mod's entity has " + jointsById.Count + " animation joints which exceeds the max joint cap of " + GlobalConstants.MaxAnimatedElements + ". Sorry, you'll have to either disable this creature or simplify the model.");
        }

        for (int k = 0; k < array.Length; k++)
        {
            jointsDone.Clear();
            GenerateFrame(k, array, rootElements, jointsById, Mat4f.Create(), array[k].RootElementTransforms, recursive);
        }

        for (int l = 0; l < array.Length; l++)
        {
            array[l].FinalizeMatrices(jointsById);
        }

        PrevNextKeyFrameByFrame = new AnimationFrame[QuantityFrames][];
        for (int m = 0; m < QuantityFrames; m++)
        {
            getLeftRightResolvedFrame(m, array, out var left, out var right);
            PrevNextKeyFrameByFrame[m] = new AnimationFrame[2] { left, right };
        }
    }

    protected void GenerateFrame(int indexNumber, AnimationFrame[] resKeyFrames, ShapeElement[] elements, Dictionary<int, AnimationJoint> jointsById, float[] modelMatrix, List<ElementPose> transforms, bool recursive = true)
    {
        int frameNumber = resKeyFrames[indexNumber].FrameNumber;
        if (frameNumber >= QuantityFrames)
        {
            throw new InvalidOperationException("Invalid animation '" + Code + "'. Has QuantityFrames set to " + QuantityFrames + " but a key frame at frame " + frameNumber + ". QuantityFrames always must be higher than frame number");
        }

        foreach (ShapeElement shapeElement in elements)
        {
            ElementPose transform = new ElementPose();
            transform.ForElement = shapeElement;
            GenerateFrameForElement(frameNumber, shapeElement, ref transform);
            transforms.Add(transform);
            float[] array = Mat4f.CloneIt(modelMatrix);
            Mat4f.Mul(array, array, shapeElement.GetLocalTransformMatrix(Version, null, transform));
            if (shapeElement.JointId > 0 && !jointsDone.Contains(shapeElement.JointId))
            {
                resKeyFrames[indexNumber].SetTransform(shapeElement.JointId, array);
                jointsDone.Add(shapeElement.JointId);
            }

            if (recursive && shapeElement.Children != null)
            {
                GenerateFrame(indexNumber, resKeyFrames, shapeElement.Children, jointsById, array, transform.ChildElementPoses);
            }
        }
    }

    protected void GenerateFrameForElement(int frameNumber, ShapeElement element, ref ElementPose transform)
    {
        for (int i = 0; i < 3; i++)
        {
            getTwoKeyFramesElementForFlag(frameNumber, element, i, out var left, out var right);
            if (left != null)
            {
                float t;
                if (right == null || left == right)
                {
                    right = left;
                    t = 0f;
                }
                else if (right.Frame < left.Frame)
                {
                    int num = right.Frame + (QuantityFrames - left.Frame);
                    t = (float)GameMath.Mod(frameNumber - left.Frame, QuantityFrames) / (float)num;
                }
                else
                {
                    t = (float)(frameNumber - left.Frame) / (float)(right.Frame - left.Frame);
                }

                lerpKeyFrameElement(left, right, i, t, ref transform);
                transform.RotShortestDistanceX = left.RotShortestDistanceX;
                transform.RotShortestDistanceY = left.RotShortestDistanceY;
                transform.RotShortestDistanceZ = left.RotShortestDistanceZ;
            }
        }
    }

    protected void lerpKeyFrameElement(AnimationKeyFrameElement prev, AnimationKeyFrameElement next, int forFlag, float t, ref ElementPose transform)
    {
        if (prev != null || next != null)
        {
            switch (forFlag)
            {
                case 0:
                    transform.translateX = GameMath.Lerp((float)prev.OffsetX.Value / 16f, (float)next.OffsetX.Value / 16f, t);
                    transform.translateY = GameMath.Lerp((float)prev.OffsetY.Value / 16f, (float)next.OffsetY.Value / 16f, t);
                    transform.translateZ = GameMath.Lerp((float)prev.OffsetZ.Value / 16f, (float)next.OffsetZ.Value / 16f, t);
                    break;
                case 1:
                    transform.degX = GameMath.Lerp((float)prev.RotationX.Value, (float)next.RotationX.Value, t);
                    transform.degY = GameMath.Lerp((float)prev.RotationY.Value, (float)next.RotationY.Value, t);
                    transform.degZ = GameMath.Lerp((float)prev.RotationZ.Value, (float)next.RotationZ.Value, t);
                    break;
                default:
                    transform.scaleX = GameMath.Lerp((float)prev.StretchX.Value, (float)next.StretchX.Value, t);
                    transform.scaleY = GameMath.Lerp((float)prev.StretchY.Value, (float)next.StretchY.Value, t);
                    transform.scaleZ = GameMath.Lerp((float)prev.StretchZ.Value, (float)next.StretchZ.Value, t);
                    break;
            }
        }
    }

    protected void getTwoKeyFramesElementForFlag(int frameNumber, ShapeElement forElement, int forFlag, out AnimationKeyFrameElement left, out AnimationKeyFrameElement right)
    {
        left = null;
        right = null;
        int num = seekRightKeyFrame(frameNumber, forElement, forFlag);
        if (num != -1)
        {
            right = KeyFrames[num].GetKeyFrameElement(forElement);
            int num2 = seekLeftKeyFrame(num, forElement, forFlag);
            if (num2 == -1)
            {
                left = right;
            }
            else
            {
                left = KeyFrames[num2].GetKeyFrameElement(forElement);
            }
        }
    }

    private int seekRightKeyFrame(int aboveFrameNumber, ShapeElement forElement, int forFlag)
    {
        int num = -1;
        for (int i = 0; i < KeyFrames.Length; i++)
        {
            AnimationKeyFrame animationKeyFrame = KeyFrames[i];
            AnimationKeyFrameElement keyFrameElement = animationKeyFrame.GetKeyFrameElement(forElement);
            if (keyFrameElement != null && keyFrameElement.IsSet(forFlag))
            {
                if (num == -1)
                {
                    num = i;
                }

                if (animationKeyFrame.Frame > aboveFrameNumber)
                {
                    return i;
                }
            }
        }

        return num;
    }

    private int seekLeftKeyFrame(int leftOfKeyFrameIndex, ShapeElement forElement, int forFlag)
    {
        for (int i = 0; i < KeyFrames.Length; i++)
        {
            int num = GameMath.Mod(leftOfKeyFrameIndex - i - 1, KeyFrames.Length);
            AnimationKeyFrameElement keyFrameElement = KeyFrames[num].GetKeyFrameElement(forElement);
            if (keyFrameElement != null && keyFrameElement.IsSet(forFlag))
            {
                return num;
            }
        }

        return -1;
    }

    protected void getLeftRightResolvedFrame(int frameNumber, AnimationFrame[] frames, out AnimationFrame left, out AnimationFrame right)
    {
        left = null;
        right = null;
        int num = frames.Length - 1;
        bool flag = false;
        while (num >= -1)
        {
            AnimationFrame animationFrame = frames[GameMath.Mod(num, frames.Length)];
            num--;
            if (animationFrame.FrameNumber <= frameNumber || flag)
            {
                left = animationFrame;
                break;
            }

            if (num == -1)
            {
                flag = true;
            }
        }

        num += 2;
        AnimationFrame animationFrame2 = frames[GameMath.Mod(num, frames.Length)];
        right = animationFrame2;
    }

    public Animation Clone()
    {
        return new Animation
        {
            Code = Code,
            CodeCrc32 = CodeCrc32,
            EaseAnimationSpeed = EaseAnimationSpeed,
            jointsDone = jointsDone,
            KeyFrames = CloneKeyFrames(),
            Name = Name,
            OnActivityStopped = OnActivityStopped,
            OnAnimationEnd = OnAnimationEnd,
            QuantityFrames = QuantityFrames,
            Version = Version
        };
    }

    private AnimationKeyFrame[] CloneKeyFrames()
    {
        AnimationKeyFrame[] array = new AnimationKeyFrame[KeyFrames.Length];
        for (int i = 0; i < KeyFrames.Length; i++)
        {
            array[i] = KeyFrames[i].Clone();
        }

        return array;
    }
}
#if false // Decompilation log
'182' items in cache
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
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
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
