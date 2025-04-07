#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     Syncs every frame with entity.ActiveAnimationsByAnimCode, starts, progresses
//     and stops animations when necessary
public abstract class AnimatorBase : IAnimator
{
    public static readonly float[] identMat = Mat4f.Create();

    private WalkSpeedSupplierDelegate WalkSpeedSupplier;

    private Action<string> onAnimationStoppedListener;

    protected int activeAnimCount;

    public ShapeElement[] RootElements;

    public List<ElementPose> RootPoses;

    public RunningAnimation[] anims;

    //
    // Summary:
    //     We skip the last row - https://stackoverflow.com/questions/32565827/whats-the-purpose-of-magic-4-of-last-row-in-matrix-4x4-for-3d-graphics
    public float[] TransformationMatrices = new float[16 * GlobalConstants.MaxAnimatedElements];

    //
    // Summary:
    //     The entities default pose. Meaning for most elements this is the identity matrix,
    //     with exception of individually controlled elements such as the head.
    public float[] TransformationMatricesDefaultPose = new float[16 * GlobalConstants.MaxAnimatedElements];

    public Dictionary<string, AttachmentPointAndPose> AttachmentPointByCode = new Dictionary<string, AttachmentPointAndPose>();

    public RunningAnimation[] CurAnims = new RunningAnimation[20];

    private float accum = 0.25f;

    private double walkSpeed;

    public bool CalculateMatrices { get; set; } = true;


    public float[] Matrices
    {
        get
        {
            if (activeAnimCount <= 0)
            {
                return TransformationMatricesDefaultPose;
            }

            return TransformationMatrices;
        }
    }

    public int ActiveAnimationCount => activeAnimCount;

    [Obsolete("Use Animations instead")]
    public RunningAnimation[] RunningAnimations => Animations;

    public RunningAnimation[] Animations => anims;

    public abstract int MaxJointId { get; }

    public RunningAnimation GetAnimationState(string code)
    {
        for (int i = 0; i < anims.Length; i++)
        {
            RunningAnimation runningAnimation = anims[i];
            if (runningAnimation.Animation.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
            {
                return runningAnimation;
            }
        }

        return null;
    }

    public AnimatorBase(WalkSpeedSupplierDelegate WalkSpeedSupplier, Animation[] Animations, Action<string> onAnimationStoppedListener = null)
    {
        this.WalkSpeedSupplier = WalkSpeedSupplier;
        this.onAnimationStoppedListener = onAnimationStoppedListener;
        anims = new RunningAnimation[(Animations != null) ? Animations.Length : 0];
        for (int i = 0; i < anims.Length; i++)
        {
            Animations[i].Code = Animations[i].Code.ToLower();
            anims[i] = new RunningAnimation
            {
                Active = false,
                Running = false,
                Animation = Animations[i],
                CurrentFrame = 0f
            };
        }

        for (int j = 0; j < TransformationMatricesDefaultPose.Length; j++)
        {
            TransformationMatricesDefaultPose[j] = identMat[j % 16];
        }
    }

    public virtual void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt)
    {
        activeAnimCount = 0;
        accum += dt;
        if (accum > 0.25f)
        {
            walkSpeed = ((WalkSpeedSupplier == null) ? 1.0 : WalkSpeedSupplier());
            accum = 0f;
        }

        for (int i = 0; i < anims.Length; i++)
        {
            RunningAnimation runningAnimation = anims[i];
            activeAnimationsByAnimCode.TryGetValue(runningAnimation.Animation.Code, out var value);
            bool active = runningAnimation.Active;
            runningAnimation.Active = value != null;
            if (!active && runningAnimation.Active)
            {
                AnimNowActive(runningAnimation, value);
            }

            if (active && !runningAnimation.Active)
            {
                if (runningAnimation.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.Rewind)
                {
                    runningAnimation.ShouldRewind = true;
                }

                if (runningAnimation.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.Stop)
                {
                    runningAnimation.Stop();
                    activeAnimationsByAnimCode.Remove(runningAnimation.Animation.Code);
                    onAnimationStoppedListener?.Invoke(runningAnimation.Animation.Code);
                }

                if (runningAnimation.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd)
                {
                    runningAnimation.ShouldPlayTillEnd = true;
                }
            }

            if (!runningAnimation.Running)
            {
                continue;
            }

            if ((runningAnimation.Iterations > 0 && runningAnimation.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Stop) || (runningAnimation.Iterations > 0 && !runningAnimation.Active && (runningAnimation.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd || runningAnimation.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.EaseOut) && runningAnimation.EasingFactor < 0.002f) || (runningAnimation.Iterations > 0 && runningAnimation.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut && runningAnimation.EasingFactor < 0.002f) || (runningAnimation.Iterations < 0 && !runningAnimation.Active && runningAnimation.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.Rewind && runningAnimation.EasingFactor < 0.002f))
            {
                runningAnimation.Stop();
                if (runningAnimation.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Stop || runningAnimation.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut)
                {
                    activeAnimationsByAnimCode.Remove(runningAnimation.Animation.Code);
                    onAnimationStoppedListener?.Invoke(runningAnimation.Animation.Code);
                }

                continue;
            }

            CurAnims[activeAnimCount] = runningAnimation;
            if ((runningAnimation.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Hold && runningAnimation.Iterations != 0 && !runningAnimation.Active) || (runningAnimation.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut && runningAnimation.Iterations != 0))
            {
                runningAnimation.EaseOut(dt);
            }

            runningAnimation.Progress(dt, (float)walkSpeed);
            activeAnimCount++;
        }

        calculateMatrices(dt);
    }

    public virtual string DumpCurrentState()
    {
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < anims.Length; i++)
        {
            RunningAnimation runningAnimation = anims[i];
            if (runningAnimation.Active && runningAnimation.Running)
            {
                stringBuilder.Append("Active&Running: " + runningAnimation.Animation.Code);
            }
            else if (runningAnimation.Active)
            {
                stringBuilder.Append("Active: " + runningAnimation.Animation.Code);
            }
            else
            {
                if (!runningAnimation.Running)
                {
                    continue;
                }

                stringBuilder.Append("Running: " + runningAnimation.Animation.Code);
            }

            stringBuilder.Append(", easing: " + runningAnimation.EasingFactor);
            stringBuilder.Append(", currentframe: " + runningAnimation.CurrentFrame);
            stringBuilder.Append(", iterations: " + runningAnimation.Iterations);
            stringBuilder.Append(", blendedweight: " + runningAnimation.BlendedWeight);
            stringBuilder.Append(", animmetacode: " + runningAnimation.meta.Code);
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }

    protected virtual void AnimNowActive(RunningAnimation anim, AnimationMetaData animData)
    {
        anim.Running = true;
        anim.Active = true;
        anim.meta = animData;
        anim.ShouldRewind = false;
        anim.ShouldPlayTillEnd = false;
        anim.CurrentFrame = animData.StartFrameOnce;
        animData.StartFrameOnce = 0f;
    }

    protected abstract void calculateMatrices(float dt);

    public AttachmentPointAndPose GetAttachmentPointPose(string code)
    {
        AttachmentPointByCode.TryGetValue(code, out var value);
        return value;
    }

    public virtual ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        throw new NotImplementedException();
    }

    public virtual void ReloadAttachmentPoints()
    {
        throw new NotImplementedException();
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
