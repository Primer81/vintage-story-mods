#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     Syncs every frame with entity.ActiveAnimationsByAnimCode, starts and stops animations
//     when necessary and does recursive interpolation on the rotation, position and
//     scale value for each frame, for each element and for each active element this
//     produces always correctly blended animations but is significantly more costly
//     for the cpu when compared to the technique used by the Vintagestory.API.Common.AnimatorBase.
public class ClientAnimator : AnimatorBase
{
    protected HashSet<int> jointsDone = new HashSet<int>();

    public Dictionary<int, AnimationJoint> jointsById;

    public static int MaxConcurrentAnimations = 16;

    private int maxDepth;

    private List<ElementPose>[][] frameByDepthByAnimation;

    private List<ElementPose>[][] nextFrameTransformsByAnimation;

    private ShapeElementWeights[][][] weightsByAnimationAndElement;

    private float[] localTransformMatrix = Mat4f.Create();

    private float[] tmpMatrix = Mat4f.Create();

    private Action<AnimationSound> onShouldPlaySoundListener;

    private int[] prevFrame = new int[MaxConcurrentAnimations];

    private int[] nextFrame = new int[MaxConcurrentAnimations];

    private static bool EleWeightDebug = false;

    private Dictionary<string, string> eleWeights = new Dictionary<string, string>();

    public override int MaxJointId => jointsById.Count + 1;

    public static ClientAnimator CreateForEntity(Entity entity, List<ElementPose> rootPoses, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById)
    {
        if (entity is EntityAgent)
        {
            EntityAgent entityag = entity as EntityAgent;
            return new ClientAnimator(() => (double)entityag.Controls.MovespeedMultiplier * entityag.GetWalkSpeedMultiplier(), rootPoses, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
        }

        return new ClientAnimator(() => 1.0, rootPoses, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
    }

    public static ClientAnimator CreateForEntity(Entity entity, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById)
    {
        if (entity is EntityAgent)
        {
            EntityAgent entityag = entity as EntityAgent;
            return new ClientAnimator(() => (double)entityag.Controls.MovespeedMultiplier * entityag.GetWalkSpeedMultiplier(), animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
        }

        return new ClientAnimator(() => 1.0, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
    }

    public ClientAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, Animation[] animations, Action<string> onAnimationStoppedListener = null, Action<AnimationSound> onShouldPlaySoundListener = null)
        : base(walkSpeedSupplier, animations, onAnimationStoppedListener)
    {
        this.onShouldPlaySoundListener = onShouldPlaySoundListener;
        initFields();
    }

    public ClientAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, List<ElementPose> rootPoses, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, Action<string> onAnimationStoppedListener = null, Action<AnimationSound> onShouldPlaySoundListener = null)
        : base(walkSpeedSupplier, animations, onAnimationStoppedListener)
    {
        RootElements = rootElements;
        this.jointsById = jointsById;
        RootPoses = rootPoses;
        this.onShouldPlaySoundListener = onShouldPlaySoundListener;
        LoadAttachmentPoints(RootPoses);
        initFields();
    }

    public ClientAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, Action<string> onAnimationStoppedListener = null, Action<AnimationSound> onShouldPlaySoundListener = null)
        : base(walkSpeedSupplier, animations, onAnimationStoppedListener)
    {
        RootElements = rootElements;
        this.jointsById = jointsById;
        RootPoses = new List<ElementPose>();
        LoadPosesAndAttachmentPoints(rootElements, RootPoses);
        this.onShouldPlaySoundListener = onShouldPlaySoundListener;
        initFields();
    }

    protected virtual void initFields()
    {
        maxDepth = 2 + ((RootPoses != null) ? getMaxDepth(RootPoses, 1) : 0);
        frameByDepthByAnimation = new List<ElementPose>[maxDepth][];
        nextFrameTransformsByAnimation = new List<ElementPose>[maxDepth][];
        weightsByAnimationAndElement = new ShapeElementWeights[maxDepth][][];
        for (int i = 0; i < maxDepth; i++)
        {
            frameByDepthByAnimation[i] = new List<ElementPose>[MaxConcurrentAnimations];
            nextFrameTransformsByAnimation[i] = new List<ElementPose>[MaxConcurrentAnimations];
            weightsByAnimationAndElement[i] = new ShapeElementWeights[MaxConcurrentAnimations][];
        }
    }

    public override void ReloadAttachmentPoints()
    {
        LoadAttachmentPoints(RootPoses);
    }

    protected virtual void LoadAttachmentPoints(List<ElementPose> cachedPoses)
    {
        for (int i = 0; i < cachedPoses.Count; i++)
        {
            ElementPose elementPose = cachedPoses[i];
            if (elementPose.ForElement.AttachmentPoints != null)
            {
                for (int j = 0; j < elementPose.ForElement.AttachmentPoints.Length; j++)
                {
                    AttachmentPoint attachmentPoint = elementPose.ForElement.AttachmentPoints[j];
                    AttachmentPointByCode[attachmentPoint.Code] = new AttachmentPointAndPose
                    {
                        AttachPoint = attachmentPoint,
                        CachedPose = elementPose
                    };
                }
            }

            if (elementPose.ChildElementPoses != null)
            {
                LoadAttachmentPoints(elementPose.ChildElementPoses);
            }
        }
    }

    protected virtual void LoadPosesAndAttachmentPoints(ShapeElement[] elements, List<ElementPose> intoPoses)
    {
        foreach (ShapeElement shapeElement in elements)
        {
            ElementPose elementPose;
            intoPoses.Add(elementPose = new ElementPose());
            elementPose.AnimModelMatrix = Mat4f.Create();
            elementPose.ForElement = shapeElement;
            if (shapeElement.AttachmentPoints != null)
            {
                for (int j = 0; j < shapeElement.AttachmentPoints.Length; j++)
                {
                    AttachmentPoint attachmentPoint = shapeElement.AttachmentPoints[j];
                    AttachmentPointByCode[attachmentPoint.Code] = new AttachmentPointAndPose
                    {
                        AttachPoint = attachmentPoint,
                        CachedPose = elementPose
                    };
                }
            }

            if (shapeElement.Children != null)
            {
                elementPose.ChildElementPoses = new List<ElementPose>(shapeElement.Children.Length);
                LoadPosesAndAttachmentPoints(shapeElement.Children, elementPose.ChildElementPoses);
            }
        }
    }

    private int getMaxDepth(List<ElementPose> poses, int depth)
    {
        for (int i = 0; i < poses.Count; i++)
        {
            ElementPose elementPose = poses[i];
            if (elementPose.ChildElementPoses != null)
            {
                depth = getMaxDepth(elementPose.ChildElementPoses, depth);
            }
        }

        return depth + 1;
    }

    public override ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        return getPosebyName(RootPoses, name);
    }

    private ElementPose getPosebyName(List<ElementPose> poses, string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        for (int i = 0; i < poses.Count; i++)
        {
            ElementPose elementPose = poses[i];
            if (elementPose.ForElement.Name.Equals(name, stringComparison))
            {
                return elementPose;
            }

            if (elementPose.ChildElementPoses != null)
            {
                ElementPose posebyName = getPosebyName(elementPose.ChildElementPoses, name);
                if (posebyName != null)
                {
                    return posebyName;
                }
            }
        }

        return null;
    }

    protected override void AnimNowActive(RunningAnimation anim, AnimationMetaData animData)
    {
        base.AnimNowActive(anim, animData);
        if (anim.Animation.PrevNextKeyFrameByFrame == null)
        {
            anim.Animation.GenerateAllFrames(RootElements, jointsById);
        }

        anim.LoadWeights(RootElements);
    }

    public override void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt)
    {
        for (int i = 0; i < activeAnimCount; i++)
        {
            RunningAnimation runningAnimation = CurAnims[i];
            if (runningAnimation.Animation.PrevNextKeyFrameByFrame == null && runningAnimation.Animation.KeyFrames.Length != 0)
            {
                runningAnimation.Animation.GenerateAllFrames(RootElements, jointsById);
            }

            if (runningAnimation.meta.AnimationSound != null && onShouldPlaySoundListener != null && runningAnimation.CurrentFrame >= (float)runningAnimation.meta.AnimationSound.Frame && runningAnimation.SoundPlayedAtIteration != runningAnimation.Iterations && runningAnimation.Active)
            {
                onShouldPlaySoundListener(runningAnimation.meta.AnimationSound);
                runningAnimation.SoundPlayedAtIteration = runningAnimation.Iterations;
            }
        }

        base.OnFrame(activeAnimationsByAnimCode, dt);
    }

    protected override void calculateMatrices(float dt)
    {
        if (!base.CalculateMatrices)
        {
            return;
        }

        jointsDone.Clear();
        int num = 0;
        for (int i = 0; i < activeAnimCount; i++)
        {
            RunningAnimation runningAnimation = CurAnims[i];
            weightsByAnimationAndElement[0][i] = runningAnimation.ElementWeights;
            num = Math.Max(num, runningAnimation.Animation.Version);
            AnimationFrame[] array = runningAnimation.Animation.PrevNextKeyFrameByFrame[(int)runningAnimation.CurrentFrame % runningAnimation.Animation.QuantityFrames];
            frameByDepthByAnimation[0][i] = array[0].RootElementTransforms;
            prevFrame[i] = array[0].FrameNumber;
            if (runningAnimation.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Hold && (int)runningAnimation.CurrentFrame + 1 == runningAnimation.Animation.QuantityFrames)
            {
                nextFrameTransformsByAnimation[0][i] = array[0].RootElementTransforms;
                nextFrame[i] = array[0].FrameNumber;
            }
            else
            {
                nextFrameTransformsByAnimation[0][i] = array[1].RootElementTransforms;
                nextFrame[i] = array[1].FrameNumber;
            }
        }

        calculateMatrices(num, dt, RootPoses, weightsByAnimationAndElement[0], Mat4f.Create(), frameByDepthByAnimation[0], nextFrameTransformsByAnimation[0], 0);
        for (int j = 0; j < GlobalConstants.MaxAnimatedElements; j++)
        {
            if (!jointsById.ContainsKey(j))
            {
                for (int k = 0; k < 16; k++)
                {
                    TransformationMatrices[j * 16 + k] = AnimatorBase.identMat[k];
                }
            }
        }

        foreach (KeyValuePair<string, AttachmentPointAndPose> item in AttachmentPointByCode)
        {
            for (int l = 0; l < 16; l++)
            {
                item.Value.AnimModelMatrix[l] = item.Value.CachedPose.AnimModelMatrix[l];
            }
        }
    }

    private void calculateMatrices(int animVersion, float dt, List<ElementPose> outFrame, ShapeElementWeights[][] weightsByAnimationAndElement, float[] modelMatrix, List<ElementPose>[] nowKeyFrameByAnimation, List<ElementPose>[] nextInKeyFrameByAnimation, int depth)
    {
        depth++;
        List<ElementPose>[] array = frameByDepthByAnimation[depth];
        List<ElementPose>[] array2 = nextFrameTransformsByAnimation[depth];
        ShapeElementWeights[][] array3 = this.weightsByAnimationAndElement[depth];
        for (int i = 0; i < outFrame.Count; i++)
        {
            ElementPose elementPose = outFrame[i];
            ShapeElement forElement = elementPose.ForElement;
            elementPose.SetMat(modelMatrix);
            Mat4f.Identity(localTransformMatrix);
            elementPose.Clear();
            float num = 0f;
            for (int j = 0; j < activeAnimCount; j++)
            {
                RunningAnimation runningAnimation = CurAnims[j];
                ShapeElementWeights shapeElementWeights = weightsByAnimationAndElement[j][i];
                if (shapeElementWeights.BlendMode != 0)
                {
                    num += shapeElementWeights.Weight * runningAnimation.EasingFactor;
                }
            }

            for (int k = 0; k < activeAnimCount; k++)
            {
                RunningAnimation runningAnimation2 = CurAnims[k];
                ShapeElementWeights shapeElementWeights2 = weightsByAnimationAndElement[k][i];
                runningAnimation2.CalcBlendedWeight(num / shapeElementWeights2.Weight, shapeElementWeights2.BlendMode);
                ElementPose elementPose2 = nowKeyFrameByAnimation[k][i];
                ElementPose elementPose3 = nextInKeyFrameByAnimation[k][i];
                int num2 = prevFrame[k];
                int num3 = nextFrame[k];
                float num4 = ((num3 > num2) ? (num3 - num2) : (runningAnimation2.Animation.QuantityFrames - num2 + num3));
                float l = ((runningAnimation2.CurrentFrame >= (float)num2) ? (runningAnimation2.CurrentFrame - (float)num2) : ((float)(runningAnimation2.Animation.QuantityFrames - num2) + runningAnimation2.CurrentFrame)) / num4;
                elementPose.Add(elementPose2, elementPose3, l, runningAnimation2.BlendedWeight);
                array[k] = elementPose2.ChildElementPoses;
                array3[k] = shapeElementWeights2.ChildElements;
                array2[k] = elementPose3.ChildElementPoses;
            }

            forElement.GetLocalTransformMatrix(animVersion, localTransformMatrix, elementPose);
            Mat4f.Mul(elementPose.AnimModelMatrix, elementPose.AnimModelMatrix, localTransformMatrix);
            if (forElement.JointId > 0 && !jointsDone.Contains(forElement.JointId))
            {
                Mat4f.Mul(tmpMatrix, elementPose.AnimModelMatrix, forElement.inverseModelTransform);
                int num5 = 16 * forElement.JointId;
                for (int m = 0; m < 16; m++)
                {
                    TransformationMatrices[num5 + m] = tmpMatrix[m];
                }

                jointsDone.Add(forElement.JointId);
            }

            if (elementPose.ChildElementPoses != null)
            {
                calculateMatrices(animVersion, dt, elementPose.ChildElementPoses, array3, elementPose.AnimModelMatrix, array, array2, depth);
            }
        }
    }

    public override string DumpCurrentState()
    {
        EleWeightDebug = true;
        eleWeights.Clear();
        calculateMatrices(1f / 60f);
        EleWeightDebug = false;
        return base.DumpCurrentState() + "\nElement weights:\n" + string.Join("\n", eleWeights.Select((KeyValuePair<string, string> x) => x.Key + ": " + x.Value));
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
