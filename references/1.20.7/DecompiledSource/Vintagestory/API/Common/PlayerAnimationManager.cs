#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class PlayerAnimationManager : AnimationManager
{
    public bool UseFpAnmations = true;

    private EntityPlayer plrEntity;

    protected string lastActiveHeldReadyAnimation;

    protected string lastActiveRightHeldIdleAnimation;

    protected string lastActiveLeftHeldIdleAnimation;

    protected string lastActiveHeldHitAnimation;

    protected string lastActiveHeldUseAnimation;

    public string lastRunningHeldHitAnimation;

    public string lastRunningHeldUseAnimation;

    private bool useFpAnimSet
    {
        get
        {
            if (UseFpAnmations && api.Side == EnumAppSide.Client && capi.World.Player.Entity.EntityId == entity.EntityId)
            {
                return capi.World.Player.CameraMode == EnumCameraMode.FirstPerson;
            }

            return false;
        }
    }

    private string fpEnding
    {
        get
        {
            if (UseFpAnmations)
            {
                ICoreClientAPI coreClientAPI = capi;
                if (coreClientAPI != null && coreClientAPI.World.Player.CameraMode == EnumCameraMode.FirstPerson)
                {
                    ICoreClientAPI obj = api as ICoreClientAPI;
                    if (obj == null || !obj.Settings.Bool["immersiveFpMode"])
                    {
                        return "-fp";
                    }

                    return "-ifp";
                }
            }

            return "";
        }
    }

    public override void Init(ICoreAPI api, Entity entity)
    {
        base.Init(api, entity);
        plrEntity = entity as EntityPlayer;
    }

    public override void OnClientFrame(float dt)
    {
        base.OnClientFrame(dt);
        if (useFpAnimSet)
        {
            plrEntity.TpAnimManager.OnClientFrame(dt);
        }
    }

    public override void ResetAnimation(string animCode)
    {
        base.ResetAnimation(animCode);
        base.ResetAnimation(animCode + "-ifp");
        base.ResetAnimation(animCode + "-fp");
    }

    public override bool StartAnimation(string configCode)
    {
        if (configCode == null)
        {
            return false;
        }

        AnimationMetaData value2;
        if (useFpAnimSet)
        {
            plrEntity.TpAnimManager.StartAnimationBase(configCode);
            if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(configCode + fpEnding, out var value))
            {
                StartAnimation(value);
                return true;
            }
        }
        else if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(configCode + fpEnding, out value2))
        {
            plrEntity.SelfFpAnimManager.StartAnimationBase(value2);
        }

        return base.StartAnimation(configCode);
    }

    public override bool StartAnimation(AnimationMetaData animdata)
    {
        if (useFpAnimSet && !animdata.Code.EndsWithOrdinal(fpEnding))
        {
            plrEntity.TpAnimManager.StartAnimation(animdata);
            if (animdata.WithFpVariant)
            {
                if (ActiveAnimationsByAnimCode.TryGetValue(animdata.FpVariant.Animation, out var value) && value == animdata.FpVariant)
                {
                    return false;
                }

                return base.StartAnimation(animdata.FpVariant);
            }

            if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(animdata.Code + fpEnding, out var value2))
            {
                if (ActiveAnimationsByAnimCode.TryGetValue(value2.Animation, out var value3) && value3 == value2)
                {
                    return false;
                }

                return base.StartAnimation(value2);
            }
        }

        return base.StartAnimation(animdata);
    }

    public bool StartAnimationBase(string configCode)
    {
        if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(configCode + fpEnding, out var value))
        {
            StartAnimation(value);
            return true;
        }

        return base.StartAnimation(configCode);
    }

    public bool StartAnimationBase(AnimationMetaData animdata)
    {
        return base.StartAnimation(animdata);
    }

    public override void RegisterFrameCallback(AnimFrameCallback trigger)
    {
        if (useFpAnimSet && !trigger.Animation.EndsWithOrdinal(fpEnding) && entity.Properties.Client.AnimationsByMetaCode.ContainsKey(trigger.Animation + fpEnding))
        {
            trigger.Animation += fpEnding;
        }

        base.RegisterFrameCallback(trigger);
    }

    public override void StopAnimation(string code)
    {
        if (code != null)
        {
            if (api.Side == EnumAppSide.Client)
            {
                (plrEntity.OtherAnimManager as PlayerAnimationManager).StopSelfAnimation(code);
            }

            StopSelfAnimation(code);
        }
    }

    public void StopSelfAnimation(string code)
    {
        string[] array = new string[3]
        {
            code,
            code + "-ifp",
            code + "-fp"
        };
        foreach (string code2 in array)
        {
            base.StopAnimation(code2);
        }
    }

    public override bool IsAnimationActive(params string[] anims)
    {
        if (useFpAnimSet)
        {
            foreach (string text in anims)
            {
                if (ActiveAnimationsByAnimCode.ContainsKey(text + fpEnding))
                {
                    return true;
                }
            }
        }

        return base.IsAnimationActive(anims);
    }

    public override RunningAnimation GetAnimationState(string anim)
    {
        if (useFpAnimSet && !anim.EndsWithOrdinal(fpEnding) && entity.Properties.Client.AnimationsByMetaCode.ContainsKey(anim + fpEnding))
        {
            return base.GetAnimationState(anim + fpEnding);
        }

        return base.GetAnimationState(anim);
    }

    public bool IsAnimationActiveOrRunning(string anim, float untilProgress = 0.95f)
    {
        if (anim == null || base.Animator == null)
        {
            return false;
        }

        if (!IsAnimationMostlyRunning(anim, untilProgress))
        {
            return IsAnimationMostlyRunning(anim + fpEnding, untilProgress);
        }

        return true;
    }

    protected bool IsAnimationMostlyRunning(string anim, float untilProgress = 0.95f)
    {
        RunningAnimation animationState = base.Animator.GetAnimationState(anim);
        if (animationState != null && animationState.Running && animationState.AnimProgress < untilProgress)
        {
            return animationState.Active;
        }

        return false;
    }

    protected override void onReceivedServerAnimation(AnimationMetaData animmetadata)
    {
        StartAnimation(animmetadata);
    }

    public override void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
    {
        base.OnReceivedServerAnimations(activeAnimations, activeAnimationsCount, activeAnimationSpeeds);
    }

    public void OnActiveSlotChanged(ItemSlot slot)
    {
        string text = slot.Itemstack?.Collectible?.GetHeldReadyAnimation(slot, entity, EnumHand.Right);
        if (text != lastActiveHeldReadyAnimation)
        {
            StopHeldReadyAnim();
        }

        if (text != null)
        {
            StartHeldReadyAnim(text);
        }

        lastActiveHeldHitAnimation = null;
    }

    public void StartHeldReadyAnim(string heldReadyAnim, bool force = false)
    {
        if (force || (!IsHeldHitActive() && !IsHeldUseActive()))
        {
            if (lastActiveHeldReadyAnimation != null)
            {
                StopAnimation(lastActiveHeldReadyAnimation);
            }

            ResetAnimation(heldReadyAnim);
            StartAnimation(heldReadyAnim);
            lastActiveHeldReadyAnimation = heldReadyAnim;
        }
    }

    public void StartHeldUseAnim(string animCode)
    {
        StopHeldReadyAnim();
        StopAnimation(lastActiveRightHeldIdleAnimation);
        StopAnimation(lastActiveHeldHitAnimation);
        StartAnimation(animCode);
        lastActiveHeldUseAnimation = animCode;
        lastRunningHeldUseAnimation = animCode;
    }

    public void StartHeldHitAnim(string animCode)
    {
        StopHeldReadyAnim();
        StopAnimation(lastActiveRightHeldIdleAnimation);
        StopAnimation(lastActiveHeldUseAnimation);
        StartAnimation(animCode);
        lastActiveHeldHitAnimation = animCode;
        lastRunningHeldHitAnimation = animCode;
    }

    public void StartRightHeldIdleAnim(string animCode)
    {
        StopAnimation(lastActiveRightHeldIdleAnimation);
        StopAnimation(lastActiveHeldUseAnimation);
        StartAnimation(animCode);
        lastActiveRightHeldIdleAnimation = animCode;
    }

    public void StartLeftHeldIdleAnim(string animCode)
    {
        StopAnimation(lastActiveLeftHeldIdleAnimation);
        StartAnimation(animCode);
        lastActiveLeftHeldIdleAnimation = animCode;
    }

    public void StopHeldReadyAnim()
    {
        if (!plrEntity.RightHandItemSlot.Empty)
        {
            JsonObject itemAttributes = plrEntity.RightHandItemSlot.Itemstack.ItemAttributes;
            if (itemAttributes != null && itemAttributes.IsTrue("alwaysPlayHeldReady"))
            {
                return;
            }
        }

        StopAnimation(lastActiveHeldReadyAnimation);
        lastActiveHeldReadyAnimation = null;
    }

    public void StopHeldUseAnim()
    {
        StopAnimation(lastActiveHeldUseAnimation);
        lastActiveHeldUseAnimation = null;
    }

    public void StopHeldAttackAnim()
    {
        if (lastActiveHeldHitAnimation != null && entity.Properties.Client.AnimationsByMetaCode.TryGetValue(lastActiveHeldHitAnimation, out var value))
        {
            JsonObject attributes = value.Attributes;
            if (attributes != null && attributes.IsTrue("authorative") && IsHeldHitActive())
            {
                return;
            }
        }

        StopAnimation(lastActiveHeldHitAnimation);
        lastActiveHeldHitAnimation = null;
    }

    public void StopRightHeldIdleAnim()
    {
        StopAnimation(lastActiveRightHeldIdleAnimation);
        lastActiveRightHeldIdleAnimation = null;
    }

    public void StopLeftHeldIdleAnim()
    {
        StopAnimation(lastActiveLeftHeldIdleAnimation);
        lastActiveLeftHeldIdleAnimation = null;
    }

    public bool IsHeldHitAuthoritative()
    {
        return IsAuthoritative(lastActiveHeldHitAnimation);
    }

    public bool IsAuthoritative(string anim)
    {
        if (anim == null)
        {
            return false;
        }

        if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(anim, out var value))
        {
            return value.Attributes?.IsTrue("authorative") ?? false;
        }

        return false;
    }

    public bool IsHeldUseActive()
    {
        if (lastActiveHeldUseAnimation != null)
        {
            return IsAnimationActiveOrRunning(lastActiveHeldUseAnimation);
        }

        return false;
    }

    public bool IsHeldHitActive(float untilProgress = 0.95f)
    {
        if (lastActiveHeldHitAnimation != null)
        {
            return IsAnimationActiveOrRunning(lastActiveHeldHitAnimation, untilProgress);
        }

        return false;
    }

    public bool IsLeftHeldActive()
    {
        if (lastActiveLeftHeldIdleAnimation != null)
        {
            return IsAnimationActiveOrRunning(lastActiveLeftHeldIdleAnimation);
        }

        return false;
    }

    public bool IsRightHeldActive()
    {
        if (lastActiveRightHeldIdleAnimation != null)
        {
            return IsAnimationActiveOrRunning(lastActiveRightHeldIdleAnimation);
        }

        return false;
    }

    public bool IsRightHeldReadyActive()
    {
        if (lastActiveHeldReadyAnimation != null)
        {
            return IsAnimationActiveOrRunning(lastActiveHeldReadyAnimation);
        }

        return false;
    }

    public bool HeldRightReadyAnimChanged(string nowHeldRightReadyAnim)
    {
        if (lastActiveHeldReadyAnimation != null)
        {
            return nowHeldRightReadyAnim != lastActiveHeldReadyAnimation;
        }

        return false;
    }

    public bool HeldUseAnimChanged(string nowHeldRightUseAnim)
    {
        if (lastActiveHeldUseAnimation != null)
        {
            return nowHeldRightUseAnim != lastActiveHeldUseAnimation;
        }

        return false;
    }

    public bool HeldHitAnimChanged(string nowHeldRightHitAnim)
    {
        if (lastActiveHeldHitAnimation != null)
        {
            return nowHeldRightHitAnim != lastActiveHeldHitAnimation;
        }

        return false;
    }

    public bool RightHeldIdleChanged(string nowHeldRightIdleAnim)
    {
        if (lastActiveRightHeldIdleAnimation != null)
        {
            return nowHeldRightIdleAnim != lastActiveRightHeldIdleAnimation;
        }

        return false;
    }

    public bool LeftHeldIdleChanged(string nowHeldLeftIdleAnim)
    {
        if (lastActiveLeftHeldIdleAnimation != null)
        {
            return nowHeldLeftIdleAnim != lastActiveLeftHeldIdleAnimation;
        }

        return false;
    }

    public override void FromAttributes(ITreeAttribute tree, string version)
    {
        if (entity == null || capi?.World.Player.Entity.EntityId != entity.EntityId)
        {
            base.FromAttributes(tree, version);
        }

        lastActiveHeldUseAnimation = tree.GetString("lrHeldUseAnim");
        lastActiveHeldHitAnimation = tree.GetString("lrHeldHitAnim");
    }

    public override void ToAttributes(ITreeAttribute tree, bool forClient)
    {
        base.ToAttributes(tree, forClient);
        if (lastActiveHeldUseAnimation != null)
        {
            tree.SetString("lrHeldUseAnim", lastActiveHeldUseAnimation);
        }

        if (lastActiveHeldHitAnimation != null)
        {
            tree.SetString("lrHeldHitAnim", lastActiveHeldHitAnimation);
        }

        if (lastActiveRightHeldIdleAnimation != null)
        {
            tree.SetString("lrRightHeldIdleAnim", lastActiveRightHeldIdleAnimation);
        }
    }

    public void OnIfpModeChanged(bool prev, bool now)
    {
        if (prev == now)
        {
            return;
        }

        string[] array = ActiveAnimationsByAnimCode.Keys.ToArray();
        string value = (now ? "-fp" : "-ifp");
        string[] array2 = array;
        foreach (string text in array2)
        {
            if (text.EndsWith(value))
            {
                StopAnimation(text);
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
