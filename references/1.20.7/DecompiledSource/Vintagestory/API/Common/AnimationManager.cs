#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class AnimationManager : IAnimationManager, IDisposable
{
    protected ICoreAPI api;

    protected ICoreClientAPI capi;

    //
    // Summary:
    //     The list of currently active animations that should be playing
    public Dictionary<string, AnimationMetaData> ActiveAnimationsByAnimCode = new Dictionary<string, AnimationMetaData>(StringComparer.OrdinalIgnoreCase);

    public List<AnimFrameCallback> Triggers;

    //
    // Summary:
    //     The entity attached to this Animation Manager.
    protected Entity entity;

    //
    // Summary:
    //     Are the animations dirty in this AnimationManager?
    public bool AnimationsDirty { get; set; }

    //
    // Summary:
    //     The animator for the animation manager.
    public IAnimator Animator { get; set; }

    //
    // Summary:
    //     The entity head controller for this animator.
    public EntityHeadController HeadController { get; set; }

    Dictionary<string, AnimationMetaData> IAnimationManager.ActiveAnimationsByAnimCode => ActiveAnimationsByAnimCode;

    public event StartAnimationDelegate OnStartAnimation;

    public event StartAnimationDelegate OnAnimationReceived;

    public event Action<string> OnAnimationStopped;

    //
    // Summary:
    //     Initializes the Animation Manager.
    //
    // Parameters:
    //   api:
    //     The Core API.
    //
    //   entity:
    //     The entity this manager is attached to.
    public virtual void Init(ICoreAPI api, Entity entity)
    {
        this.api = api;
        this.entity = entity;
        capi = api as ICoreClientAPI;
    }

    public IAnimator LoadAnimator(ICoreAPI api, Entity entity, Shape entityShape, RunningAnimation[] copyOverAnims, bool requirePosesOnServer, params string[] requireJointsForElements)
    {
        Init(entity.Api, entity);
        if (entityShape == null)
        {
            return null;
        }

        JsonObject attributes = entity.Properties.Attributes;
        if (attributes != null && attributes["requireJointsForElements"].Exists)
        {
            requireJointsForElements = requireJointsForElements.Append(entity.Properties.Attributes["requireJointsForElements"].AsArray<string>());
        }

        entityShape.InitForAnimations(api.Logger, entity.Properties.Client.ShapeForEntity.Base.ToString(), requireJointsForElements);
        IAnimator animator2 = (Animator = ((api.Side == EnumAppSide.Client) ? ClientAnimator.CreateForEntity(entity, entityShape.Animations, entityShape.Elements, entityShape.JointsById) : ServerAnimator.CreateForEntity(entity, entityShape.Animations, entityShape.Elements, entityShape.JointsById, requirePosesOnServer)));
        CopyOverAnimStates(copyOverAnims, animator2);
        return animator2;
    }

    public void CopyOverAnimStates(RunningAnimation[] copyOverAnims, IAnimator animator)
    {
        if (copyOverAnims == null || animator == null)
        {
            return;
        }

        foreach (RunningAnimation runningAnimation in copyOverAnims)
        {
            if (runningAnimation != null && runningAnimation.Active)
            {
                ActiveAnimationsByAnimCode.TryGetValue(runningAnimation.Animation.Code, out var value);
                if (value != null)
                {
                    value.StartFrameOnce = runningAnimation.CurrentFrame;
                }
            }
        }
    }

    public virtual bool IsAnimationActive(params string[] anims)
    {
        foreach (string key in anims)
        {
            if (ActiveAnimationsByAnimCode.ContainsKey(key))
            {
                return true;
            }
        }

        return false;
    }

    public virtual RunningAnimation GetAnimationState(string anim)
    {
        return Animator.GetAnimationState(anim);
    }

    //
    // Summary:
    //     If given animation is running, will set its progress to the first animation frame
    //
    //
    // Parameters:
    //   animCode:
    public virtual void ResetAnimation(string animCode)
    {
        RunningAnimation runningAnimation = Animator?.GetAnimationState(animCode);
        if (runningAnimation != null)
        {
            runningAnimation.CurrentFrame = 0f;
            runningAnimation.Iterations = 0;
        }
    }

    //
    // Summary:
    //     As StartAnimation, except that it does not attempt to start the animation if
    //     the named animation is non-existent for this entity
    //
    // Parameters:
    //   animdata:
    public virtual bool TryStartAnimation(AnimationMetaData animdata)
    {
        if (((AnimatorBase)Animator).GetAnimationState(animdata.Animation) == null)
        {
            return false;
        }

        return StartAnimation(animdata);
    }

    //
    // Summary:
    //     Client: Starts given animation Server: Sends all active anims to all connected
    //     clients then purges the ActiveAnimationsByAnimCode list
    //
    // Parameters:
    //   animdata:
    public virtual bool StartAnimation(AnimationMetaData animdata)
    {
        if (this.OnStartAnimation != null)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag = false;
            bool result = false;
            Delegate[] invocationList = this.OnStartAnimation.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                result = ((StartAnimationDelegate)invocationList[i])(ref animdata, ref handling);
                if (handling == EnumHandling.PreventSubsequent)
                {
                    return result;
                }

                flag = handling == EnumHandling.PreventDefault;
            }

            if (flag)
            {
                return result;
            }
        }

        if (ActiveAnimationsByAnimCode.TryGetValue(animdata.Animation, out var value) && value == animdata)
        {
            return false;
        }

        if (animdata.Code == null)
        {
            throw new Exception("anim meta data code cannot be null!");
        }

        AnimationsDirty = true;
        ActiveAnimationsByAnimCode[animdata.Animation] = animdata;
        entity?.UpdateDebugAttributes();
        return true;
    }

    //
    // Summary:
    //     Start a new animation defined in the entity config file. If it's not defined,
    //     it won't play. Use StartAnimation(AnimationMetaData animdata) to circumvent the
    //     entity config anim data.
    //
    // Parameters:
    //   configCode:
    //     Anim config code, not the animation code!
    public virtual bool StartAnimation(string configCode)
    {
        if (configCode == null)
        {
            return false;
        }

        if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(configCode, out var value))
        {
            StartAnimation(value);
            return true;
        }

        return false;
    }

    //
    // Summary:
    //     Stops given animation
    //
    // Parameters:
    //   code:
    public virtual void StopAnimation(string code)
    {
        if (code == null || entity == null)
        {
            return;
        }

        if (entity.World.Side == EnumAppSide.Server)
        {
            AnimationsDirty = true;
        }

        if (!ActiveAnimationsByAnimCode.Remove(code) && ActiveAnimationsByAnimCode.Count > 0)
        {
            foreach (KeyValuePair<string, AnimationMetaData> item in ActiveAnimationsByAnimCode)
            {
                if (item.Value.Code == code)
                {
                    ActiveAnimationsByAnimCode.Remove(item.Key);
                    break;
                }
            }
        }

        if (entity.World.EntityDebugMode)
        {
            entity.UpdateDebugAttributes();
        }
    }

    //
    // Summary:
    //     The event fired when the manager recieves the server animations.
    //
    // Parameters:
    //   activeAnimations:
    //
    //   activeAnimationsCount:
    //
    //   activeAnimationSpeeds:
    public virtual void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
    {
        HashSet<string> hashSet = new HashSet<string>();
        string text = "";
        int num = int.MaxValue;
        for (int i = 0; i < activeAnimationsCount; i++)
        {
            uint key = (uint)(activeAnimations[i] & num);
            if (entity.Properties.Client.AnimationsByCrc32.TryGetValue(key, out var value))
            {
                hashSet.Add(value.Animation);
                if (!ActiveAnimationsByAnimCode.ContainsKey(value.Code))
                {
                    value.AnimationSpeed = activeAnimationSpeeds[i];
                    onReceivedServerAnimation(value);
                }
            }
            else
            {
                if (!entity.Properties.Client.LoadedShapeForEntity.AnimationsByCrc32.TryGetValue(key, out var value2))
                {
                    continue;
                }

                hashSet.Add(value2.Code);
                if (!ActiveAnimationsByAnimCode.ContainsKey(value2.Code))
                {
                    string text2 = ((value2.Code == null) ? value2.Name.ToLowerInvariant() : value2.Code);
                    text = text + ", " + text2;
                    entity.Properties.Client.AnimationsByMetaCode.TryGetValue(text2, out var value3);
                    if (value3 == null)
                    {
                        value3 = new AnimationMetaData
                        {
                            Code = text2,
                            Animation = text2,
                            CodeCrc32 = value2.CodeCrc32
                        };
                    }

                    value3.AnimationSpeed = activeAnimationSpeeds[i];
                    onReceivedServerAnimation(value3);
                }
            }
        }

        if (entity.EntityId == (entity.World as IClientWorldAccessor).Player.Entity.EntityId)
        {
            return;
        }

        string[] array = ActiveAnimationsByAnimCode.Keys.ToArray();
        foreach (string text3 in array)
        {
            AnimationMetaData animationMetaData = ActiveAnimationsByAnimCode[text3];
            if (!hashSet.Contains(text3) && !animationMetaData.ClientSide && (!entity.Properties.Client.AnimationsByMetaCode.TryGetValue(text3, out var value4) || value4.TriggeredBy == null || !value4.WasStartedFromTrigger))
            {
                ActiveAnimationsByAnimCode.Remove(text3);
            }
        }
    }

    protected virtual void onReceivedServerAnimation(AnimationMetaData animmetadata)
    {
        EnumHandling handling = EnumHandling.PassThrough;
        this.OnAnimationReceived?.Invoke(ref animmetadata, ref handling);
        if (handling == EnumHandling.PassThrough)
        {
            ActiveAnimationsByAnimCode[animmetadata.Animation] = animmetadata;
        }
    }

    //
    // Summary:
    //     Serializes the slots contents to be stored in the SaveGame
    //
    // Parameters:
    //   tree:
    //
    //   forClient:
    public virtual void ToAttributes(ITreeAttribute tree, bool forClient)
    {
        if (Animator == null)
        {
            return;
        }

        ITreeAttribute treeAttribute = (ITreeAttribute)(tree["activeAnims"] = new TreeAttribute());
        if (ActiveAnimationsByAnimCode.Count == 0)
        {
            return;
        }

        using FastMemoryStream fastMemoryStream = new FastMemoryStream();
        foreach (KeyValuePair<string, AnimationMetaData> item in ActiveAnimationsByAnimCode)
        {
            if (item.Value.Code == null)
            {
                item.Value.Code = item.Key;
            }

            if (forClient || !(item.Value.Code != "die"))
            {
                RunningAnimation animationState = Animator.GetAnimationState(item.Value.Animation);
                if (animationState != null)
                {
                    item.Value.StartFrameOnce = animationState.CurrentFrame;
                }

                fastMemoryStream.Reset();
                using (BinaryWriter writer = new BinaryWriter(fastMemoryStream))
                {
                    item.Value.ToBytes(writer);
                }

                treeAttribute[item.Key] = new ByteArrayAttribute(fastMemoryStream.ToArray());
                item.Value.StartFrameOnce = 0f;
            }
        }
    }

    //
    // Summary:
    //     Loads the entity from a stored byte array from the SaveGame
    //
    // Parameters:
    //   tree:
    //
    //   version:
    public virtual void FromAttributes(ITreeAttribute tree, string version)
    {
        if (!(tree["activeAnims"] is ITreeAttribute treeAttribute))
        {
            return;
        }

        foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
        {
            using MemoryStream input = new MemoryStream((item.Value as ByteArrayAttribute).value);
            using BinaryReader reader = new BinaryReader(input);
            ActiveAnimationsByAnimCode[item.Key] = AnimationMetaData.FromBytes(reader, version);
        }
    }

    //
    // Summary:
    //     The event fired at each server tick.
    //
    // Parameters:
    //   dt:
    public virtual void OnServerTick(float dt)
    {
        if (Animator != null)
        {
            Animator.OnFrame(ActiveAnimationsByAnimCode, dt);
            Animator.CalculateMatrices = !entity.Alive || entity.requirePosesOnServer;
        }

        runTriggers();
    }

    //
    // Summary:
    //     The event fired each time the client ticks.
    //
    // Parameters:
    //   dt:
    public virtual void OnClientFrame(float dt)
    {
        if (!capi.IsGamePaused && Animator != null)
        {
            if (HeadController != null)
            {
                HeadController.OnFrame(dt);
            }

            if (entity.IsRendered || entity.IsShadowRendered || !entity.Alive)
            {
                Animator.OnFrame(ActiveAnimationsByAnimCode, dt);
                runTriggers();
            }
        }
    }

    public virtual void RegisterFrameCallback(AnimFrameCallback trigger)
    {
        if (Triggers == null)
        {
            Triggers = new List<AnimFrameCallback>();
        }

        Triggers.Add(trigger);
    }

    private void runTriggers()
    {
        if (Triggers == null)
        {
            return;
        }

        for (int i = 0; i < Triggers.Count; i++)
        {
            AnimFrameCallback animFrameCallback = Triggers[i];
            if (ActiveAnimationsByAnimCode.ContainsKey(animFrameCallback.Animation))
            {
                RunningAnimation animationState = Animator.GetAnimationState(animFrameCallback.Animation);
                if (animationState != null && animationState.CurrentFrame >= animFrameCallback.Frame)
                {
                    Triggers.RemoveAt(i);
                    animFrameCallback.Callback();
                    i--;
                }
            }
        }
    }

    //
    // Summary:
    //     Disposes of the animation manager.
    public void Dispose()
    {
    }

    public virtual void TriggerAnimationStopped(string code)
    {
        this.OnAnimationStopped?.Invoke(code);
    }

    public void ShouldPlaySound(AnimationSound sound)
    {
        entity.World.PlaySoundAt(sound.Location, entity, null, sound.RandomizePitch, sound.Range);
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
