#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.IO;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     The available controls to move around a character in a game world
public class EntityControls
{
    //
    // Summary:
    //     To execute a call handler registered by the engine. Don't use this one, use api.Input.InWorldAction
    //     instead.
    public OnEntityAction OnAction = delegate
    {
    };

    private bool[] flags = new bool[15];

    //
    // Summary:
    //     If true, the entity is either flying, gliding or swimming.
    public bool DetachedMode;

    //
    // Summary:
    //     If true, the entity has NoClip active.
    public bool NoClip;

    //
    // Summary:
    //     the axis lock for the fly plane.
    public EnumFreeMovAxisLock FlyPlaneLock;

    //
    // Summary:
    //     Current walking direction.
    public Vec3d WalkVector = new Vec3d();

    //
    // Summary:
    //     Current flying direction
    public Vec3d FlyVector = new Vec3d();

    //
    // Summary:
    //     Whether or not the entity is flying.
    public bool IsFlying;

    //
    // Summary:
    //     Whether or not the entity is climbing
    public bool IsClimbing;

    //
    // Summary:
    //     Whether or not the entity is aiming
    public bool IsAiming;

    //
    // Summary:
    //     Whether or not the entity is currently stepping up a block
    public bool IsStepping;

    //
    // Summary:
    //     If the player is currently using the currently held item in a special way (e.g.
    //     attacking with smithing hammer or eating an edible item)
    public EnumHandInteract HandUse;

    //
    // Summary:
    //     The block pos the player started using
    public BlockSelection HandUsingBlockSel;

    public int UsingCount;

    public long UsingBeginMS;

    public ModelTransform LeftUsingHeldItemTransformBefore;

    [Obsolete("Setting this value has no effect anymore. Add an animation to the seraph instead")]
    public ModelTransform UsingHeldItemTransformBefore;

    [Obsolete("Setting this value has no effect anymore. Add an animation to the seraph instead")]
    public ModelTransform UsingHeldItemTransformAfter;

    //
    // Summary:
    //     The movement speed multiplier.
    public float MovespeedMultiplier = 1f;

    //
    // Summary:
    //     Whether or not this entity is dirty.
    public bool Dirty;

    public double GlideSpeed;

    public bool[] Flags => flags;

    //
    // Summary:
    //     Checks to see if the entity is attempting to move in any direction (excluding
    //     jumping)
    public bool TriesToMove
    {
        get
        {
            if (!Forward && !Backward && !Left)
            {
                return Right;
            }

            return true;
        }
    }

    //
    // Summary:
    //     A check for if the entity is moving in the direction it's facing.
    public virtual bool Forward
    {
        get
        {
            return flags[0];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.Forward, value);
        }
    }

    //
    // Summary:
    //     A check for if the entity is moving the opposite direction it's facing.
    public virtual bool Backward
    {
        get
        {
            return flags[1];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.Backward, value);
        }
    }

    //
    // Summary:
    //     A check to see if the entity is moving left the direction it's facing.
    public virtual bool Left
    {
        get
        {
            return flags[2];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.Left, value);
        }
    }

    //
    // Summary:
    //     A check to see if the entity is moving right the direction it's facing.
    public virtual bool Right
    {
        get
        {
            return flags[3];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.Right, value);
        }
    }

    //
    // Summary:
    //     A check whether to see if the entity is jumping.
    public virtual bool Jump
    {
        get
        {
            return flags[4];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.Jump, value);
        }
    }

    //
    // Summary:
    //     A check whether to see if the entity is sneaking. Use Controls.ShiftKey instead
    //     for mouse interaction modifiers, as it is a separable control.
    //     A test for Sneak should be used only when we want to know whether the entity
    //     is crouching or using Sneak motion, which affects things like whether it is detectable
    //     by other entities, seen on the map, or how the shield is used
    public virtual bool Sneak
    {
        get
        {
            return flags[5];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.Sneak, value);
        }
    }

    //
    // Summary:
    //     A check to see whether the entity is gliding
    public virtual bool Gliding
    {
        get
        {
            return flags[7];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.Glide, value);
        }
    }

    //
    // Summary:
    //     A check to see whether the entity is sitting on the floor.
    public virtual bool FloorSitting
    {
        get
        {
            return flags[8];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.FloorSit, value);
        }
    }

    //
    // Summary:
    //     A check to see whether the entity is sprinting. Use Controls.CtrlKey instead
    //     for mouse interaction modifiers, as it is a separable control.
    //     A test for Sprint should be used only when we want to know whether the entity
    //     is sprinting.
    public virtual bool Sprint
    {
        get
        {
            return flags[6];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.Sprint, value);
        }
    }

    //
    // Summary:
    //     A check to see whether the entity is moving up.
    public virtual bool Up
    {
        get
        {
            return flags[11];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.Up, value);
        }
    }

    //
    // Summary:
    //     A check to see whether the entity is moving down.
    public virtual bool Down
    {
        get
        {
            return flags[12];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.Down, value);
        }
    }

    //
    // Summary:
    //     A check to see if the entity is holding the in-world rleft mouse button down.
    public virtual bool LeftMouseDown
    {
        get
        {
            return flags[9];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.LeftMouseDown, value);
        }
    }

    //
    // Summary:
    //     A check to see if the entity is holding the in-world right mouse button down.
    public virtual bool RightMouseDown
    {
        get
        {
            return flags[10];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.RightMouseDown, value);
        }
    }

    //
    // Summary:
    //     A check to see if the entity is holding down the Ctrl key (which may be the same
    //     as the Sprint key or one or other may have been remapped).
    //     Should normally be used in conjunction with a mouse button, including OnHeldInteractStart()
    //     methods etc
    public virtual bool CtrlKey
    {
        get
        {
            return flags[13];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.CtrlKey, value);
        }
    }

    //
    // Summary:
    //     A check to see if the entity is holding down the Shift key (which may be the
    //     same as the Sneak key or one or other may have been remapped).
    //     Should normally be used in conjunction with a mouse button, including OnHeldInteractStart()
    //     methods etc
    public virtual bool ShiftKey
    {
        get
        {
            return flags[14];
        }
        set
        {
            AttemptToggleAction(EnumEntityAction.ShiftKey, value);
        }
    }

    public virtual bool this[EnumEntityAction action]
    {
        get
        {
            return flags[(int)action];
        }
        set
        {
            flags[(int)action] = value;
        }
    }

    protected virtual void AttemptToggleAction(EnumEntityAction action, bool on)
    {
        if (flags[(int)action] != on)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            OnAction(action, on, ref handled);
            if (handled == EnumHandling.PassThrough)
            {
                flags[(int)action] = on;
                Dirty = true;
            }
        }
    }

    //
    // Summary:
    //     Calculates the movement vectors for the player.
    //
    // Parameters:
    //   pos:
    //     The position of the player.
    //
    //   dt:
    //     The change in time.
    public virtual void CalcMovementVectors(EntityPos pos, float dt)
    {
        double num = dt * GlobalConstants.BaseMoveSpeed * MovespeedMultiplier * GlobalConstants.OverallSpeedMultiplier;
        double num2 = (Forward ? num : 0.0) + (Backward ? (0.0 - num) : 0.0);
        double num3 = (Right ? (0.0 - num) : 0.0) + (Left ? num : 0.0);
        double num4 = Math.Cos(pos.Pitch);
        double num5 = Math.Sin(pos.Pitch);
        double num6 = Math.Cos(0f - pos.Yaw);
        double num7 = Math.Sin(0f - pos.Yaw);
        WalkVector.Set(num3 * num6 - num2 * num7, 0.0, num3 * num7 + num2 * num6);
        if (FlyPlaneLock == EnumFreeMovAxisLock.Y)
        {
            num4 = -1.0;
        }

        FlyVector.Set(num3 * num6 + num2 * num4 * num7, num2 * num5, num3 * num7 - num2 * num4 * num6);
        double val = (((Forward || Backward) && (Right || Left)) ? (1.0 / Math.Sqrt(2.0)) : 1.0);
        WalkVector.Mul(val);
        if (FlyPlaneLock == EnumFreeMovAxisLock.X)
        {
            FlyVector.X = 0.0;
        }

        if (FlyPlaneLock == EnumFreeMovAxisLock.Y)
        {
            FlyVector.Y = 0.0;
        }

        if (FlyPlaneLock == EnumFreeMovAxisLock.Z)
        {
            FlyVector.Z = 0.0;
        }
    }

    //
    // Summary:
    //     Copies the controls from the provided controls to this set of controls.
    //
    // Parameters:
    //   controls:
    //     The controls to copy over.
    public virtual void SetFrom(EntityControls controls)
    {
        for (int i = 0; i < controls.flags.Length; i++)
        {
            flags[i] = controls.flags[i];
        }

        DetachedMode = controls.DetachedMode;
        FlyPlaneLock = controls.FlyPlaneLock;
        IsFlying = controls.IsFlying;
        NoClip = controls.NoClip;
    }

    //
    // Summary:
    //     Updates the data from the packet.
    //
    // Parameters:
    //   pressed:
    //     Whether or not the key was pressed.
    //
    //   action:
    //     the id of the key that was pressed.
    public virtual void UpdateFromPacket(bool pressed, int action)
    {
        if (flags[action] != pressed)
        {
            AttemptToggleAction((EnumEntityAction)action, pressed);
        }
    }

    //
    // Summary:
    //     Forces the entity to stop all movements, resets all flags to false
    public virtual void StopAllMovement()
    {
        for (int i = 0; i < flags.Length; i++)
        {
            flags[i] = false;
        }
    }

    //
    // Summary:
    //     Converts the values to a single int flag.
    //
    // Returns:
    //     the compressed integer.
    public virtual int ToInt()
    {
        return (Forward ? 1 : 0) | (Backward ? 2 : 0) | (Left ? 4 : 0) | (Right ? 8 : 0) | (Jump ? 16 : 0) | (Sneak ? 32 : 0) | (Sprint ? 64 : 0) | (Up ? 128 : 0) | (Down ? 256 : 0) | (flags[7] ? 512 : 0) | (flags[8] ? 1024 : 0) | (flags[9] ? 2048 : 0) | (flags[10] ? 4096 : 0) | (IsClimbing ? 8192 : 0) | (flags[13] ? 16384 : 0) | (flags[14] ? 32768 : 0);
    }

    //
    // Summary:
    //     Converts the int flags to movement controls.
    //
    // Parameters:
    //   flagsInt:
    //     The compressed integer.
    public virtual void FromInt(int flagsInt)
    {
        Forward = (flagsInt & 1) > 0;
        Backward = (flagsInt & 2) > 0;
        Left = (flagsInt & 4) > 0;
        Right = (flagsInt & 8) > 0;
        Jump = (flagsInt & 0x10) > 0;
        Sneak = (flagsInt & 0x20) > 0;
        Sprint = (flagsInt & 0x40) > 0;
        Up = (flagsInt & 0x80) > 0;
        Down = (flagsInt & 0x100) > 0;
        flags[7] = (flagsInt & 0x200) > 0;
        flags[8] = (flagsInt & 0x400) > 0;
        flags[9] = (flagsInt & 0x800) > 0;
        flags[10] = (flagsInt & 0x1000) > 0;
        IsClimbing = (flagsInt & 0x2000) > 0;
        flags[13] = (flagsInt & 0x4000) > 0;
        flags[14] = (flagsInt & 0x8000) > 0;
    }

    public virtual void ToBytes(BinaryWriter writer)
    {
        writer.Write(ToInt());
    }

    public virtual void FromBytes(BinaryReader reader, bool ignoreData)
    {
        int flagsInt = reader.ReadInt32();
        if (!ignoreData)
        {
            FromInt(flagsInt);
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
