#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.IO;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class EntityItem : Entity
{
    public EntityItemSlot Slot;

    public long itemSpawnedMilliseconds;

    private long lastPlayedSizzlesTotalMs;

    private float getWindSpeedAccum = 0.25f;

    private Vec3d windSpeed = new Vec3d();

    private BlockPos tmpPos = new BlockPos();

    private float windLoss;

    private float fireDamage;

    //
    // Summary:
    //     The itemstack attached to this Item Entity.
    public ItemStack Itemstack
    {
        get
        {
            return WatchedAttributes.GetItemstack("itemstack");
        }
        set
        {
            WatchedAttributes.SetItemstack("itemstack", value);
            Slot.Itemstack = value;
        }
    }

    //
    // Summary:
    //     The UID of the player that dropped this itemstack.
    public string ByPlayerUid
    {
        get
        {
            return WatchedAttributes.GetString("byPlayerUid");
        }
        set
        {
            WatchedAttributes.SetString("byPlayerUid", value);
        }
    }

    //
    // Summary:
    //     Returns the material density of the item.
    public override float MaterialDensity => (Slot.Itemstack?.Collectible != null) ? Slot.Itemstack.Collectible.MaterialDensity : 2000;

    //
    // Summary:
    //     Whether or not the EntityItem is interactable.
    public override bool IsInteractable => false;

    //
    // Summary:
    //     Get the HSV colors for the lighting.
    public override byte[] LightHsv => Slot.Itemstack?.Collectible?.GetLightHsv(World.BlockAccessor, null, Slot.Itemstack);

    public override double SwimmingOffsetY => base.SwimmingOffsetY;

    public EntityItem()
        : base(GlobalConstants.DefaultSimulationRange * 3 / 4)
    {
        Stats = new EntityStats(this);
        Slot = new EntityItemSlot(this);
    }

    public override void Initialize(EntityProperties properties, ICoreAPI api, long chunkindex3d)
    {
        World = api.World;
        Api = api;
        base.Properties = properties;
        Class = properties.Class;
        InChunkIndex3d = chunkindex3d;
        if (Itemstack == null || Itemstack.StackSize == 0 || !Itemstack.ResolveBlockOrItem(World))
        {
            Die();
            Itemstack = null;
            return;
        }

        alive = WatchedAttributes.GetInt("entityDead") == 0;
        WatchedAttributes.RegisterModifiedListener("onFire", base.updateOnFire);
        if (base.Properties.CollisionBoxSize != null || properties.SelectionBoxSize != null)
        {
            updateColSelBoxes();
        }

        DoInitialActiveCheck(api);
        base.Properties.Initialize(this, api);
        LocalEyePos.Y = base.Properties.EyeHeight;
        TriggerOnInitialized();
        WatchedAttributes.RegisterModifiedListener("itemstack", delegate
        {
            if (Itemstack != null && Itemstack.Collectible == null)
            {
                Itemstack.ResolveBlockOrItem(World);
            }

            Slot.Itemstack = Itemstack;
        });
        JsonObject jsonObject = Itemstack.Collectible.Attributes?["gravityFactor"];
        if (jsonObject != null && jsonObject.Exists)
        {
            WatchedAttributes.SetDouble("gravityFactor", jsonObject.AsDouble(1.0));
        }

        JsonObject jsonObject2 = Itemstack.Collectible.Attributes?["airDragFactor"];
        if (jsonObject2 != null && jsonObject2.Exists)
        {
            WatchedAttributes.SetDouble("airDragFactor", jsonObject2.AsDouble(1.0));
        }

        itemSpawnedMilliseconds = World.ElapsedMilliseconds;
        Swimming = (FeetInLiquid = World.BlockAccessor.GetBlock(Pos.AsBlockPos, 2).IsLiquid());
        tmpPos.Set(Pos.XInt, Pos.YInt, Pos.ZInt);
        windLoss = (float)World.BlockAccessor.GetDistanceToRainFall(tmpPos) / 4f;
    }

    public override void OnGameTick(float dt)
    {
        if (World.Side == EnumAppSide.Client)
        {
            try
            {
                base.OnGameTick(dt);
            }
            catch (Exception e)
            {
                if (World == null)
                {
                    throw new NullReferenceException("'World' was null for EntityItem; entity is " + (alive ? "alive" : "post-lifetime"));
                }

                Api.Logger.Error("Erroring EntityItem tick: please report this as a bug!");
                Api.Logger.Error(e);
            }
        }
        else
        {
            foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
            {
                behavior.OnGameTick(dt);
            }

            if (InLava)
            {
                Ignite();
            }

            if (base.IsOnFire)
            {
                Block block = World.BlockAccessor.GetBlock(Pos.AsBlockPos, 2);
                if ((block.IsLiquid() && block.LiquidCode != "lava") || World.ElapsedMilliseconds - OnFireBeginTotalMs > 12000)
                {
                    base.IsOnFire = false;
                }
                else
                {
                    ApplyFireDamage(dt);
                    if (!alive && InLava)
                    {
                        DieInLava();
                    }
                }
            }
        }

        if (!Alive)
        {
            return;
        }

        if (Itemstack != null)
        {
            if (!base.Collided && !Swimming && World.Side == EnumAppSide.Server)
            {
                getWindSpeedAccum += dt;
                if ((double)getWindSpeedAccum > 0.25)
                {
                    getWindSpeedAccum = 0f;
                    tmpPos.Set(Pos.XInt, Pos.YInt, Pos.ZInt);
                    windSpeed = World.BlockAccessor.GetWindSpeedAt(tmpPos);
                    windSpeed.X = Math.Max(0.0, Math.Abs(windSpeed.X) - (double)windLoss) * (double)Math.Sign(windSpeed.X);
                    windSpeed.Y = Math.Max(0.0, Math.Abs(windSpeed.Y) - (double)windLoss) * (double)Math.Sign(windSpeed.Y);
                    windSpeed.Z = Math.Max(0.0, Math.Abs(windSpeed.Z) - (double)windLoss) * (double)Math.Sign(windSpeed.Z);
                }

                float num = GameMath.Clamp(1000f / (float)Itemstack.Collectible.MaterialDensity, 1f, 10f);
                base.SidedPos.Motion.X += windSpeed.X / 1000.0 * (double)num * GameMath.Clamp(1.0 / (1.0 + Math.Abs(base.SidedPos.Motion.X)), 0.0, 1.0);
                base.SidedPos.Motion.Y += windSpeed.Y / 1000.0 * (double)num * GameMath.Clamp(1.0 / (1.0 + Math.Abs(base.SidedPos.Motion.Y)), 0.0, 1.0);
                base.SidedPos.Motion.Z += windSpeed.Z / 1000.0 * (double)num * GameMath.Clamp(1.0 / (1.0 + Math.Abs(base.SidedPos.Motion.Z)), 0.0, 1.0);
            }

            Itemstack.Collectible.OnGroundIdle(this);
            if (FeetInLiquid && !InLava)
            {
                float temperature = Itemstack.Collectible.GetTemperature(World, Itemstack);
                if (temperature > 20f)
                {
                    Itemstack.Collectible.SetTemperature(World, Itemstack, Math.Max(20f, temperature - 5f));
                    if (temperature > 90f)
                    {
                        double num2 = SelectionBox.XSize;
                        Entity.SplashParticleProps.BasePos.Set(Pos.X - num2 / 2.0, Pos.Y - 0.75, Pos.Z - num2 / 2.0);
                        Entity.SplashParticleProps.AddVelocity.Set(0f, 0f, 0f);
                        Entity.SplashParticleProps.QuantityMul = 0.1f;
                        World.SpawnParticles(Entity.SplashParticleProps);
                    }

                    if (temperature > 200f && World.Side == EnumAppSide.Client && World.ElapsedMilliseconds - lastPlayedSizzlesTotalMs > 10000)
                    {
                        World.PlaySoundAt(new AssetLocation("sounds/sizzle"), this);
                        lastPlayedSizzlesTotalMs = World.ElapsedMilliseconds;
                    }
                }
            }
        }
        else
        {
            Die();
        }

        World.FrameProfiler.Mark("entity-tick-droppeditems");
    }

    public override void Ignite()
    {
        ItemStack itemstack = Itemstack;
        if (InLava || (itemstack != null && itemstack.Collectible.CombustibleProps != null && (itemstack.Collectible.CombustibleProps.MeltingPoint < 700 || itemstack.Collectible.CombustibleProps.BurnTemperature > 0)))
        {
            base.Ignite();
        }
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
        if (base.SidedProperties == null)
        {
            return;
        }

        foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
        {
            behavior.OnEntityDespawn(despawn);
        }

        WatchedAttributes.OnModified.Clear();
    }

    public override void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
    {
    }

    public override void UpdateDebugAttributes()
    {
    }

    public override void StartAnimation(string code)
    {
    }

    public override void StopAnimation(string code)
    {
    }

    public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
    {
        if (Alive)
        {
            Alive = false;
            DespawnReason = new EntityDespawnData
            {
                Reason = reason,
                DamageSourceForDeath = damageSourceForDeath
            };
        }
    }

    //
    // Summary:
    //     Builds and spawns an EntityItem from a provided ItemStack.
    //
    // Parameters:
    //   itemstack:
    //     The contents of the EntityItem
    //
    //   position:
    //     The position of the EntityItem
    //
    //   velocity:
    //     The velocity of the EntityItem
    //
    //   world:
    //     The world the EntityItems preside in.
    //
    // Returns:
    //     A freshly baked EntityItem to introduce to the world.
    public static EntityItem FromItemstack(ItemStack itemstack, Vec3d position, Vec3d velocity, IWorldAccessor world)
    {
        EntityItem entityItem = new EntityItem();
        entityItem.Code = GlobalConstants.EntityItemTypeCode;
        entityItem.SimulationRange = (int)(0.75f * (float)GlobalConstants.DefaultSimulationRange);
        entityItem.Itemstack = itemstack;
        entityItem.ServerPos.SetPosWithDimension(position);
        if (velocity == null)
        {
            velocity = new Vec3d((float)world.Rand.NextDouble() * 0.1f - 0.05f, (float)world.Rand.NextDouble() * 0.1f - 0.05f, (float)world.Rand.NextDouble() * 0.1f - 0.05f);
        }

        entityItem.ServerPos.Motion = velocity;
        entityItem.Pos.SetFrom(entityItem.ServerPos);
        return entityItem;
    }

    public override bool CanCollect(Entity byEntity)
    {
        if (Alive)
        {
            return World.ElapsedMilliseconds - itemSpawnedMilliseconds > 1000;
        }

        return false;
    }

    public override ItemStack OnCollected(Entity byEntity)
    {
        return Slot.Itemstack;
    }

    public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
    {
        return false;
    }

    public override bool ReceiveDamage(DamageSource damageSource, float damage)
    {
        if (damageSource.Source == EnumDamageSource.Internal && damageSource.Type == EnumDamageType.Fire)
        {
            fireDamage += damage;
        }

        if (fireDamage > 4f)
        {
            Die();
        }

        return base.ReceiveDamage(damageSource, damage);
    }

    public override void FromBytes(BinaryReader reader, bool forClient)
    {
        base.FromBytes(reader, forClient);
        if (Itemstack != null)
        {
            Slot.Itemstack = Itemstack;
        }

        if (World != null)
        {
            ItemStack itemstack = Slot.Itemstack;
            if (itemstack == null || !itemstack.ResolveBlockOrItem(World))
            {
                Itemstack = null;
                Die();
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
