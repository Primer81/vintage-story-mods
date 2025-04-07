#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

//
// Summary:
//     A shape element built from JSON data within the model.
[JsonObject(MemberSerialization.OptIn)]
public class ShapeElement
{
    //
    // Summary:
    //     A static reference to the logger (null on a server) - we don't want to hold a
    //     reference to the platform or api in every ShapeElement
    public static ILogger Logger;

    public static object locationForLogging;

    //
    // Summary:
    //     The name of the ShapeElement
    [JsonProperty]
    public string Name;

    [JsonProperty]
    public double[] From;

    [JsonProperty]
    public double[] To;

    //
    // Summary:
    //     Whether or not the shape element is shaded.
    [JsonProperty]
    public bool Shade = true;

    [JsonProperty]
    public bool GradientShade;

    //
    // Summary:
    //     The faces of the shape element by name (will normally be null except during object
    //     deserialization: use FacesResolved instead!)
    [JsonProperty]
    [Obsolete("Use FacesResolved instead")]
    public Dictionary<string, ShapeElementFace> Faces;

    //
    // Summary:
    //     An array holding the faces of this shape element in BlockFacing order: North,
    //     East, South, West, Up, Down. May be null if not present or not enabled.
    //     Note: from game version 1.20.4, this is null on server-side (except during asset
    //     loading start-up stage)
    public ShapeElementFace[] FacesResolved = new ShapeElementFace[6];

    //
    // Summary:
    //     The origin point for rotation.
    [JsonProperty]
    public double[] RotationOrigin;

    //
    // Summary:
    //     The forward vertical rotation of the shape element.
    [JsonProperty]
    public double RotationX;

    //
    // Summary:
    //     The forward vertical rotation of the shape element.
    [JsonProperty]
    public double RotationY;

    //
    // Summary:
    //     The left/right tilt of the shape element
    [JsonProperty]
    public double RotationZ;

    //
    // Summary:
    //     How far away are the left/right sides of the shape from the center
    [JsonProperty]
    public double ScaleX = 1.0;

    //
    // Summary:
    //     How far away are the top/bottom sides of the shape from the center
    [JsonProperty]
    public double ScaleY = 1.0;

    //
    // Summary:
    //     How far away are the front/back sides of the shape from the center.
    [JsonProperty]
    public double ScaleZ = 1.0;

    [JsonProperty]
    public string ClimateColorMap;

    [JsonProperty]
    public string SeasonColorMap;

    [JsonProperty]
    public short RenderPass = -1;

    [JsonProperty]
    public short ZOffset;

    //
    // Summary:
    //     Set this to true to disable randomDrawOffset and randomRotations on this specific
    //     element (e.g. used for the ice element of Coopers Reeds in Ice)
    [JsonProperty]
    public bool DisableRandomDrawOffset;

    //
    // Summary:
    //     The child shapes of this shape element
    [JsonProperty]
    public ShapeElement[] Children;

    //
    // Summary:
    //     The attachment points for this shape.
    [JsonProperty]
    public AttachmentPoint[] AttachmentPoints;

    //
    // Summary:
    //     The "remote" parent for this element
    [JsonProperty]
    public string StepParentName;

    //
    // Summary:
    //     The parent element reference for this shape.
    public ShapeElement ParentElement;

    //
    // Summary:
    //     The id of the joint attached to the parent element.
    public int JointId;

    //
    // Summary:
    //     For entity animations
    public int Color = -1;

    public float DamageEffect;

    public float[] inverseModelTransform;

    private static ElementPose noTransform = new ElementPose();

    //
    // Summary:
    //     Walks the element tree and collects all parents, starting with the root element
    public List<ShapeElement> GetParentPath()
    {
        List<ShapeElement> list = new List<ShapeElement>();
        for (ShapeElement parentElement = ParentElement; parentElement != null; parentElement = parentElement.ParentElement)
        {
            list.Add(parentElement);
        }

        list.Reverse();
        return list;
    }

    public int CountParents()
    {
        int num = 0;
        for (ShapeElement parentElement = ParentElement; parentElement != null; parentElement = parentElement.ParentElement)
        {
            num++;
        }

        return num;
    }

    public void CacheInverseTransformMatrix()
    {
        if (inverseModelTransform == null)
        {
            inverseModelTransform = GetInverseModelMatrix();
        }
    }

    //
    // Summary:
    //     Returns the full inverse model matrix (includes all parent transforms)
    public float[] GetInverseModelMatrix()
    {
        List<ShapeElement> parentPath = GetParentPath();
        float[] array = Mat4f.Create();
        for (int i = 0; i < parentPath.Count; i++)
        {
            float[] localTransformMatrix = parentPath[i].GetLocalTransformMatrix(0);
            Mat4f.Mul(array, array, localTransformMatrix);
        }

        Mat4f.Mul(array, array, GetLocalTransformMatrix(0));
        return Mat4f.Invert(Mat4f.Create(), array);
    }

    internal void SetJointId(int jointId)
    {
        JointId = jointId;
        ShapeElement[] children = Children;
        if (children != null)
        {
            for (int i = 0; i < children.Length; i++)
            {
                children[i].SetJointId(jointId);
            }
        }
    }

    internal void ResolveRefernces()
    {
        ShapeElement[] children = Children;
        if (children != null)
        {
            foreach (ShapeElement obj in children)
            {
                obj.ParentElement = this;
                obj.ResolveRefernces();
            }
        }

        AttachmentPoint[] attachmentPoints = AttachmentPoints;
        if (attachmentPoints != null)
        {
            for (int j = 0; j < attachmentPoints.Length; j++)
            {
                attachmentPoints[j].ParentElement = this;
            }
        }
    }

    internal void TrimTextureNamesAndResolveFaces()
    {
        if (Faces != null)
        {
            foreach (KeyValuePair<string, ShapeElementFace> face in Faces)
            {
                ShapeElementFace value = face.Value;
                if (value.Enabled)
                {
                    BlockFacing blockFacing = BlockFacing.FromFirstLetter(face.Key);
                    if (blockFacing == null)
                    {
                        Logger?.Warning("Shape element in " + locationForLogging?.ToString() + ": Unknown facing '" + blockFacing.Code + "'. Ignoring face.");
                    }
                    else
                    {
                        FacesResolved[blockFacing.Index] = value;
                        value.Texture = value.Texture.Substring(1).DeDuplicate();
                    }
                }
            }
        }

        Faces = null;
        if (Children != null)
        {
            ShapeElement[] children = Children;
            for (int i = 0; i < children.Length; i++)
            {
                children[i].TrimTextureNamesAndResolveFaces();
            }
        }

        Name = Name.DeDuplicate();
        StepParentName = StepParentName.DeDuplicate();
        AttachmentPoint[] attachmentPoints = AttachmentPoints;
        if (attachmentPoints != null)
        {
            for (int j = 0; j < attachmentPoints.Length; j++)
            {
                attachmentPoints[j].DeDuplicate();
            }
        }
    }

    public unsafe float[] GetLocalTransformMatrix(int animVersion, float[] output = null, ElementPose tf = null)
    {
        if (tf == null)
        {
            tf = noTransform;
        }

        if (output == null)
        {
            output = Mat4f.Create();
        }

        byte* intPtr = stackalloc byte[12];
        // IL initblk instruction
        Unsafe.InitBlock(intPtr, 0, 12);
        Span<float> span = new Span<float>(intPtr, 3);
        if (RotationOrigin != null)
        {
            span[0] = (float)RotationOrigin[0] / 16f;
            span[1] = (float)RotationOrigin[1] / 16f;
            span[2] = (float)RotationOrigin[2] / 16f;
        }

        if (animVersion == 1)
        {
            Mat4f.Translate(output, output, span[0], span[1], span[2]);
            Mat4f.Scale(output, output, (float)ScaleX, (float)ScaleY, (float)ScaleZ);
            if (RotationX != 0.0)
            {
                Mat4f.RotateX(output, output, (float)(RotationX * 0.01745329238474369));
            }

            if (RotationY != 0.0)
            {
                Mat4f.RotateY(output, output, (float)(RotationY * 0.01745329238474369));
            }

            if (RotationZ != 0.0)
            {
                Mat4f.RotateZ(output, output, (float)(RotationZ * 0.01745329238474369));
            }

            Mat4f.Translate(output, output, 0f - span[0] + (float)From[0] / 16f + tf.translateX, 0f - span[1] + (float)From[1] / 16f + tf.translateY, 0f - span[2] + (float)From[2] / 16f + tf.translateZ);
            Mat4f.Scale(output, output, tf.scaleX, tf.scaleY, tf.scaleZ);
            if (tf.degX + tf.degOffX != 0f)
            {
                Mat4f.RotateX(output, output, (tf.degX + tf.degOffX) * (MathF.PI / 180f));
            }

            if (tf.degY + tf.degOffY != 0f)
            {
                Mat4f.RotateY(output, output, (tf.degY + tf.degOffY) * (MathF.PI / 180f));
            }

            if (tf.degZ + tf.degOffZ != 0f)
            {
                Mat4f.RotateZ(output, output, (tf.degZ + tf.degOffZ) * (MathF.PI / 180f));
            }
        }
        else
        {
            Mat4f.Translate(output, output, span[0], span[1], span[2]);
            if (RotationX + (double)tf.degX + (double)tf.degOffX != 0.0)
            {
                Mat4f.RotateX(output, output, (float)(RotationX + (double)tf.degX + (double)tf.degOffX) * (MathF.PI / 180f));
            }

            if (RotationY + (double)tf.degY + (double)tf.degOffY != 0.0)
            {
                Mat4f.RotateY(output, output, (float)(RotationY + (double)tf.degY + (double)tf.degOffY) * (MathF.PI / 180f));
            }

            if (RotationZ + (double)tf.degZ + (double)tf.degOffZ != 0.0)
            {
                Mat4f.RotateZ(output, output, (float)(RotationZ + (double)tf.degZ + (double)tf.degOffZ) * (MathF.PI / 180f));
            }

            Mat4f.Scale(output, output, (float)ScaleX * tf.scaleX, (float)ScaleY * tf.scaleY, (float)ScaleZ * tf.scaleZ);
            Mat4f.Translate(output, output, (float)From[0] / 16f + tf.translateX, (float)From[1] / 16f + tf.translateY, (float)From[2] / 16f + tf.translateZ);
            Mat4f.Translate(output, output, 0f - span[0], 0f - span[1], 0f - span[2]);
        }

        return output;
    }

    public ShapeElement Clone()
    {
        ShapeElement shapeElement = new ShapeElement
        {
            AttachmentPoints = (AttachmentPoint[])AttachmentPoints?.Clone(),
            FacesResolved = (ShapeElementFace[])FacesResolved?.Clone(),
            From = (double[])From?.Clone(),
            To = (double[])To?.Clone(),
            inverseModelTransform = (float[])inverseModelTransform?.Clone(),
            JointId = JointId,
            RenderPass = RenderPass,
            RotationX = RotationX,
            RotationY = RotationY,
            RotationZ = RotationZ,
            RotationOrigin = (double[])RotationOrigin?.Clone(),
            SeasonColorMap = SeasonColorMap,
            ClimateColorMap = ClimateColorMap,
            StepParentName = StepParentName,
            Shade = Shade,
            DisableRandomDrawOffset = DisableRandomDrawOffset,
            ZOffset = ZOffset,
            GradientShade = GradientShade,
            ScaleX = ScaleX,
            ScaleY = ScaleY,
            ScaleZ = ScaleZ,
            Name = Name
        };
        ShapeElement[] children = Children;
        if (children != null)
        {
            shapeElement.Children = new ShapeElement[children.Length];
            for (int i = 0; i < children.Length; i++)
            {
                ShapeElement shapeElement2 = children[i].Clone();
                shapeElement2.ParentElement = shapeElement;
                shapeElement.Children[i] = shapeElement2;
            }
        }

        return shapeElement;
    }

    public void SetJointIdRecursive(int jointId)
    {
        JointId = jointId;
        ShapeElement[] children = Children;
        if (children != null)
        {
            for (int i = 0; i < children.Length; i++)
            {
                children[i].SetJointIdRecursive(jointId);
            }
        }
    }

    public void CacheInverseTransformMatrixRecursive()
    {
        CacheInverseTransformMatrix();
        ShapeElement[] children = Children;
        if (children != null)
        {
            for (int i = 0; i < children.Length; i++)
            {
                children[i].CacheInverseTransformMatrixRecursive();
            }
        }
    }

    public void WalkRecursive(Action<ShapeElement> onElem)
    {
        onElem(this);
        ShapeElement[] children = Children;
        if (children != null)
        {
            for (int i = 0; i < children.Length; i++)
            {
                children[i].WalkRecursive(onElem);
            }
        }
    }

    internal bool HasFaces()
    {
        for (int i = 0; i < 6; i++)
        {
            if (FacesResolved[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    public virtual void FreeRAMServer()
    {
        Faces = null;
        FacesResolved = null;
        ShapeElement[] children = Children;
        if (children != null)
        {
            for (int i = 0; i < children.Length; i++)
            {
                children[i].FreeRAMServer();
            }
        }
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
