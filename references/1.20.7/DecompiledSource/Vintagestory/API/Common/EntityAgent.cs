#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     A goal-directed entity which observes and acts upon an environment
public class EntityAgent : Entity
{
    public enum EntityServerPacketId
    {
        Teleport = 1,
        Revive = 196,
        Emote = 197,
        Death = 198,
        Hurt = 199,
        PlayPlayerAnim = 200,
        PlayMusic = 201,
        StopMusic = 202,
        Talk = 203
    }

    public enum EntityClientPacketId
    {
        SitfloorEdge = 296
    }

    public float sidewaysSwivelAngle;

    //
    // Summary:
    //     True if all clients have to be informed about this entities death. Set to false
    //     once all clients have been notified
    public bool DeadNotify;

    protected EntityControls controls;

    protected EntityControls servercontrols;

    protected bool alwaysRunIdle;

    public EnumEntityActivity CurrentControls;

    //
    // Summary:
    //     Whether or not the entity is allowed to despawn (Default: true)
    public bool AllowDespawn = true;

    private AnimationMetaData curMountedAnim;

    protected bool ignoreTeleportCall;

    //
    // Summary:
    //     updated by GetWalkSpeedMultiplier()
    protected Block insideBlock;

    //
    // Summary:
    //     updated by GetWalkSpeedMultiplier()
    protected BlockPos insidePos = new BlockPos();

    public override bool IsCreature => true;

    //
    // Summary:
    //     No swivel when we are mounted
    public override bool CanSwivel
    {
        get
        {
            if (base.CanSwivel)
            {
                return MountedOn == null;
            }

            return false;
        }
    }

    public override bool CanStepPitch
    {
        get
        {
            if (base.CanStepPitch)
            {
                return MountedOn == null;
            }

            return false;
        }
    }

    //
    // Summary:
    //     The yaw of the agents body
    public virtual float BodyYaw { get; set; }

    //
    // Summary:
    //     The yaw of the agents body on the client, retrieved from the server (BehaviorInterpolatePosition
    //     lerps this value and sets BodyYaw)
    public virtual float BodyYawServer { get; set; }

    //
    // Summary:
    //     Unique identifier for a herd
    public long HerdId
    {
        get
        {
            return WatchedAttributes.GetLong("herdId", 0L);
        }
        set
        {
            WatchedAttributes.SetLong("herdId", value);
        }
    }

    public IMountableSeat MountedOn { get; protected set; }

    internal virtual bool LoadControlsFromServer => true;

    //
    // Summary:
    //     Item in the left hand slot of the entity agent.
    public virtual ItemSlot LeftHandItemSlot { get; set; }

    //
    // Summary:
    //     Item in the right hand slot of the entity agent.
    public virtual ItemSlot RightHandItemSlot { get; set; }

    public virtual ItemSlot ActiveHandItemSlot => RightHandItemSlot;

    //
    // Summary:
    //     Whether or not the entity should despawn.
    public override bool ShouldDespawn
    {
        get
        {
            if (!Alive)
            {
                return AllowDespawn;
            }

            return false;
        }
    }

    //
    // Summary:
    //     The controls for this entity.
    public EntityControls Controls => controls;

    //
    // Summary:
    //     The server controls for this entity
    public EntityControls ServerControls => servercontrols;

    public EntityAgent()
    {
        controls = new EntityControls();
        servercontrols = new EntityControls();
    }

    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
    {
        base.Initialize(properties, api, InChunkIndex3d);
        if (World.Side == EnumAppSide.Server)
        {
            servercontrols = controls;
        }

        WatchedAttributes.RegisterModifiedListener("mountedOn", updateMountedState);
        if (WatchedAttributes["mountedOn"] == null)
        {
            return;
        }

        MountedOn = World.ClassRegistry.GetMountable(WatchedAttributes["mountedOn"] as TreeAttribute);
        if (MountedOn != null && TryMount(MountedOn) && Api.Side == EnumAppSide.Server)
        {
            Entity entity = MountedOn.MountSupplier?.OnEntity;
            if (entity != null)
            {
                Api.World.Logger.Audit("{0} loaded already mounted/seated on a {1} at {2}", GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos);
            }
        }
    }

    //
    // Summary:
    //     Are the eyes of this entity submerged in liquid?
    public bool IsEyesSubmerged()
    {
        BlockPos pos = base.SidedPos.AsBlockPos.Add(0f, (float)(Swimming ? base.Properties.SwimmingEyeHeight : base.Properties.EyeHeight), 0f);
        return World.BlockAccessor.GetBlock(pos).MatterState == EnumMatterState.Liquid;
    }

    //
    // Summary:
    //     Attempts to mount this entity on a target.
    //
    // Parameters:
    //   onmount:
    //     The mount to mount
    //
    // Returns:
    //     Whether it was mounted or not.
    public virtual bool TryMount(IMountableSeat onmount)
    {
        if (!onmount.CanMount(this))
        {
            return false;
        }

        onmount.Controls.FromInt(Controls.ToInt());
        if (MountedOn != null && MountedOn != onmount)
        {
            IMountableSeat seatOfMountedEntity = MountedOn.MountSupplier.GetSeatOfMountedEntity(this);
            if (seatOfMountedEntity != null)
            {
                seatOfMountedEntity.DoTeleportOnUnmount = false;
            }

            if (!TryUnmount())
            {
                return false;
            }

            if (seatOfMountedEntity != null)
            {
                seatOfMountedEntity.DoTeleportOnUnmount = true;
            }
        }

        TreeAttribute treeAttribute = new TreeAttribute();
        onmount?.MountableToTreeAttributes(treeAttribute);
        WatchedAttributes["mountedOn"] = treeAttribute;
        doMount(onmount);
        if (World.Side == EnumAppSide.Server)
        {
            WatchedAttributes.MarkPathDirty("mountedOn");
        }

        return true;
    }

    protected virtual void updateMountedState()
    {
        if (WatchedAttributes.HasAttribute("mountedOn"))
        {
            IMountableSeat mountable = World.ClassRegistry.GetMountable(WatchedAttributes["mountedOn"] as TreeAttribute);
            doMount(mountable);
        }
        else
        {
            TryUnmount();
        }
    }

    protected virtual void doMount(IMountableSeat mountable)
    {
        MountedOn = mountable;
        controls.StopAllMovement();
        if (mountable == null)
        {
            WatchedAttributes.RemoveAttribute("mountedOn");
            return;
        }

        if (MountedOn?.SuggestedAnimation != null)
        {
            curMountedAnim = MountedOn.SuggestedAnimation;
            AnimManager?.StartAnimation(curMountedAnim);
        }

        mountable.DidMount(this);
    }

    //
    // Summary:
    //     Attempts to un-mount the player.
    //
    // Returns:
    //     Whether or not unmounting was successful
    public bool TryUnmount()
    {
        IMountableSeat mountedOn = MountedOn;
        if (mountedOn != null && !mountedOn.CanUnmount(this))
        {
            return false;
        }

        if (curMountedAnim != null)
        {
            AnimManager?.StopAnimation(curMountedAnim.Animation);
            curMountedAnim = null;
        }

        IMountableSeat mountedOn2 = MountedOn;
        MountedOn = null;
        mountedOn2?.DidUnmount(this);
        if (WatchedAttributes.HasAttribute("mountedOn"))
        {
            WatchedAttributes.RemoveAttribute("mountedOn");
        }

        if (Api.Side == EnumAppSide.Server && mountedOn2 != null)
        {
            Entity entity = mountedOn2.MountSupplier?.OnEntity;
            if (entity != null)
            {
                Api.World.Logger.Audit("{0} dismounts/disembarks from a {1} at {2}", GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos);
            }
        }

        return true;
    }

    public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
    {
        if (Alive && reason == EnumDespawnReason.Death)
        {
            PlayEntitySound("death");
            if (damageSourceForDeath?.GetCauseEntity() is EntityPlayer entityPlayer)
            {
                Api.Logger.Audit("Player {0} killed {1} at {2}", entityPlayer.GetName(), Code, Pos.AsBlockPos);
            }
        }

        if (reason != 0)
        {
            AllowDespawn = true;
        }

        controls.WalkVector.Set(0.0, 0.0, 0.0);
        controls.FlyVector.Set(0.0, 0.0, 0.0);
        ClimbingOnFace = null;
        base.Die(reason, damageSourceForDeath);
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
        base.OnEntityDespawn(despawn);
        if (despawn != null && despawn.Reason == EnumDespawnReason.Removed && (this is EntityHumanoid || MountedOn != null))
        {
            TryUnmount();
        }
    }

    public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
    {
        EnumHandling handled = EnumHandling.PassThrough;
        foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
        {
            behavior.OnInteract(byEntity, slot, hitPosition, mode, ref handled);
            if (handled == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }

        if (handled == EnumHandling.PreventDefault || handled == EnumHandling.PreventSubsequent || mode != 0)
        {
            return;
        }

        float num = ((slot.Itemstack == null) ? 0.5f : slot.Itemstack.Collectible.GetAttackPower(slot.Itemstack));
        int damageTier = ((slot.Itemstack != null) ? slot.Itemstack.Collectible.ToolTier : 0);
        float num2 = byEntity.Stats.GetBlended("meleeWeaponsDamage");
        JsonObject attributes = base.Properties.Attributes;
        if (attributes != null && attributes["isMechanical"].AsBool())
        {
            num2 *= byEntity.Stats.GetBlended("mechanicalsDamage");
        }

        num *= num2;
        IPlayer dualCallByPlayer = null;
        if (byEntity is EntityPlayer && !IsActivityRunning("invulnerable"))
        {
            dualCallByPlayer = (byEntity as EntityPlayer).Player;
            World.PlaySoundAt(new AssetLocation("sounds/player/slap"), ServerPos.X, ServerPos.InternalY, ServerPos.Z, dualCallByPlayer);
            slot?.Itemstack?.Collectible.OnAttackingWith(byEntity.World, byEntity, this, slot);
        }

        if (Api.Side == EnumAppSide.Client && num > 1f && !IsActivityRunning("invulnerable"))
        {
            JsonObject attributes2 = base.Properties.Attributes;
            if (attributes2 != null && attributes2["spawnDamageParticles"].AsBool())
            {
                Vec3d vec3d = base.SidedPos.XYZ + hitPosition;
                Vec3d minPos = vec3d.AddCopy(-0.15, -0.15, -0.15);
                Vec3d maxPos = vec3d.AddCopy(0.15, 0.15, 0.15);
                int textureSubId = base.Properties.Client.FirstTexture.Baked.TextureSubId;
                Vec3f vec3f = new Vec3f();
                for (int i = 0; i < 10; i++)
                {
                    int randomColor = (Api as ICoreClientAPI).EntityTextureAtlas.GetRandomColor(textureSubId);
                    vec3f.Set(1f - 2f * (float)World.Rand.NextDouble(), 2f * (float)World.Rand.NextDouble(), 1f - 2f * (float)World.Rand.NextDouble());
                    World.SpawnParticles(1f, randomColor, minPos, maxPos, vec3f, vec3f, 1.5f, 1f, 0.25f + (float)World.Rand.NextDouble() * 0.25f, EnumParticleModel.Cube, dualCallByPlayer);
                }
            }
        }

        DamageSource damageSource = new DamageSource
        {
            Source = (((byEntity as EntityPlayer).Player != null) ? EnumDamageSource.Player : EnumDamageSource.Entity),
            SourceEntity = byEntity,
            Type = EnumDamageType.BluntAttack,
            HitPosition = hitPosition,
            DamageTier = damageTier
        };
        if (ReceiveDamage(damageSource, num))
        {
            byEntity.DidAttack(damageSource, this);
        }
    }

    public override void TeleportToDouble(double x, double y, double z, Action onTeleported = null)
    {
        if (ignoreTeleportCall)
        {
            return;
        }

        ignoreTeleportCall = true;
        if (MountedOn != null)
        {
            if (MountedOn.Entity != null)
            {
                MountedOn.Entity.TeleportToDouble(x, y, z, onTeleported);
                ignoreTeleportCall = false;
                return;
            }

            TryUnmount();
        }

        base.TeleportToDouble(x, y, z, onTeleported);
        ignoreTeleportCall = false;
    }

    public virtual void DidAttack(DamageSource source, EntityAgent targetEntity)
    {
        EnumHandling handled = EnumHandling.PassThrough;
        foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
        {
            behavior.DidAttack(source, targetEntity, ref handled);
            if (handled == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }

        if (handled != EnumHandling.PreventDefault)
        {
            _ = 3;
        }
    }

    public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
    {
        if (!Alive)
        {
            return false;
        }

        return true;
    }

    public override bool ReceiveDamage(DamageSource damageSource, float damage)
    {
        return base.ReceiveDamage(damageSource, damage);
    }

    //
    // Summary:
    //     Recieves the saturation from a food source.
    //
    // Parameters:
    //   saturation:
    //     The amount of saturation recieved.
    //
    //   foodCat:
    //     The cat of food... err Category of food.
    //
    //   saturationLossDelay:
    //     The delay before the loss of saturation
    //
    //   nutritionGainMultiplier:
    public virtual void ReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10f, float nutritionGainMultiplier = 1f)
    {
        if (!Alive || !ShouldReceiveSaturation(saturation, foodCat, saturationLossDelay))
        {
            return;
        }

        foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
        {
            behavior.OnEntityReceiveSaturation(saturation, foodCat, saturationLossDelay, nutritionGainMultiplier);
        }
    }

    //
    // Summary:
    //     Whether or not the target should recieve saturation.
    //
    // Parameters:
    //   saturation:
    //     The amount of saturation recieved.
    //
    //   foodCat:
    //     The cat of food... err Category of food.
    //
    //   saturationLossDelay:
    //     The delay before the loss of saturation
    //
    //   nutritionGainMultiplier:
    public virtual bool ShouldReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10f, float nutritionGainMultiplier = 1f)
    {
        return true;
    }

    public override void OnGameTick(float dt)
    {
        AnimationMetaData animationMetaData = MountedOn?.SuggestedAnimation;
        if (curMountedAnim?.Code != animationMetaData?.Code)
        {
            AnimManager?.StopAnimation(curMountedAnim?.Code);
            if (animationMetaData != null)
            {
                AnimManager?.StartAnimation(animationMetaData);
            }

            curMountedAnim = animationMetaData;
        }

        if (World.Side == EnumAppSide.Client)
        {
            if (Alive)
            {
                CurrentControls = ((!servercontrols.TriesToMove && ((!servercontrols.Jump && !servercontrols.Sneak) || !servercontrols.IsClimbing)) ? EnumEntityActivity.Idle : EnumEntityActivity.Move) | ((Swimming && !servercontrols.FloorSitting) ? EnumEntityActivity.Swim : EnumEntityActivity.None) | (servercontrols.FloorSitting ? EnumEntityActivity.FloorSitting : EnumEntityActivity.None) | ((servercontrols.Sneak && !servercontrols.IsClimbing && !servercontrols.FloorSitting && !Swimming) ? EnumEntityActivity.SneakMode : EnumEntityActivity.None) | ((servercontrols.TriesToMove && servercontrols.Sprint && !Swimming && !servercontrols.Sneak) ? EnumEntityActivity.SprintMode : EnumEntityActivity.None) | (servercontrols.IsFlying ? (servercontrols.Gliding ? EnumEntityActivity.Glide : EnumEntityActivity.Fly) : EnumEntityActivity.None) | (servercontrols.IsClimbing ? EnumEntityActivity.Climb : EnumEntityActivity.None) | ((servercontrols.Jump && OnGround) ? EnumEntityActivity.Jump : EnumEntityActivity.None) | ((!OnGround && !Swimming && !FeetInLiquid && !servercontrols.IsClimbing && !servercontrols.IsFlying && base.SidedPos.Motion.Y < -0.05) ? EnumEntityActivity.Fall : EnumEntityActivity.None) | ((MountedOn != null) ? EnumEntityActivity.Mounted : EnumEntityActivity.None);
            }
            else
            {
                CurrentControls = EnumEntityActivity.Dead;
            }

            CurrentControls = ((CurrentControls == EnumEntityActivity.None) ? EnumEntityActivity.Idle : CurrentControls);
            if (MountedOn != null && MountedOn.SkipIdleAnimation)
            {
                CurrentControls &= ~EnumEntityActivity.Idle;
            }
        }

        HandleHandAnimations(dt);
        if (World.Side == EnumAppSide.Client)
        {
            AnimationMetaData animationMetaData2 = null;
            bool flag = false;
            bool flag2 = false;
            AnimationMetaData[] animations = base.Properties.Client.Animations;
            int num = 0;
            while (animations != null && num < animations.Length)
            {
                AnimationMetaData animationMetaData3 = animations[num];
                bool flag3 = AnimManager.IsAnimationActive(animationMetaData3.Animation);
                bool flag4 = animationMetaData3 != null && (animationMetaData3.TriggeredBy?.DefaultAnim).GetValueOrDefault();
                bool flag5 = animationMetaData3.Matches((int)CurrentControls) || (flag4 && CurrentControls == EnumEntityActivity.Idle);
                flag |= !flag4 && flag3 && animationMetaData3.BlendMode == EnumAnimationBlendMode.Average;
                flag2 |= (flag5 || (flag3 && !animationMetaData3.WasStartedFromTrigger)) && animationMetaData3.SupressDefaultAnimation;
                if (flag4)
                {
                    animationMetaData2 = animationMetaData3;
                }

                if (!onAnimControls(animationMetaData3, flag3, flag5))
                {
                    if (!flag3 && flag5)
                    {
                        animationMetaData3.WasStartedFromTrigger = true;
                        AnimManager.StartAnimation(animationMetaData3);
                    }

                    if (!flag4 && flag3 && !flag5 && animationMetaData3.WasStartedFromTrigger)
                    {
                        animationMetaData3.WasStartedFromTrigger = false;
                        AnimManager.StopAnimation(animationMetaData3.Animation);
                    }
                }

                num++;
            }

            if (animationMetaData2 != null && Alive && !flag2)
            {
                if (flag || MountedOn != null)
                {
                    if (!alwaysRunIdle && AnimManager.IsAnimationActive(animationMetaData2.Animation))
                    {
                        AnimManager.StopAnimation(animationMetaData2.Animation);
                    }
                }
                else
                {
                    animationMetaData2.WasStartedFromTrigger = true;
                    if (!AnimManager.IsAnimationActive(animationMetaData2.Animation))
                    {
                        AnimManager.StartAnimation(animationMetaData2);
                    }
                }
            }

            if ((!Alive || flag2) && animationMetaData2 != null)
            {
                AnimManager.StopAnimation(animationMetaData2.Code);
            }

            bool flag6 = (Api as ICoreClientAPI).World.Player.Entity.EntityId == EntityId;
            Block block = insideBlock;
            if (block != null && block.GetBlockMaterial(Api.World.BlockAccessor, insidePos) == EnumBlockMaterial.Snow && flag6)
            {
                SpawnSnowStepParticles();
            }
        }

        if (base.Properties.RotateModelOnClimb && World.Side == EnumAppSide.Server)
        {
            if (!OnGround && Alive && Controls.IsClimbing && ClimbingOnFace != null && (double)ClimbingOnCollBox.Y2 > 0.2)
            {
                ServerPos.Pitch = (float)ClimbingOnFace.HorizontalAngleIndex * (MathF.PI / 2f);
            }
            else
            {
                ServerPos.Pitch = 0f;
            }
        }

        World.FrameProfiler.Mark("entityAgent-ticking");
        base.OnGameTick(dt);
    }

    protected virtual void SpawnSnowStepParticles()
    {
        ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
        EntityPos entityPos = ((coreClientAPI.World.Player.Entity.EntityId == EntityId) ? Pos : ServerPos);
        float num = (float)Math.Sqrt(Pos.Motion.X * Pos.Motion.X + Pos.Motion.Z * Pos.Motion.Z);
        if (Api.World.Rand.NextDouble() < (double)(10f * num))
        {
            Random rand = coreClientAPI.World.Rand;
            Vec3f velocity = new Vec3f(1f - 2f * (float)rand.NextDouble() + GameMath.Clamp((float)Pos.Motion.X * 15f, -5f, 5f), 0.5f + 3.5f * (float)rand.NextDouble(), 1f - 2f * (float)rand.NextDouble() + GameMath.Clamp((float)Pos.Motion.Z * 15f, -5f, 5f));
            float radius = Math.Min(SelectionBox.XSize, SelectionBox.ZSize) * 0.9f;
            World.SpawnCubeParticles(entityPos.AsBlockPos, entityPos.XYZ.Add(0.0, 0.0, 0.0), radius, 2 + (int)(rand.NextDouble() * (double)num * 5.0), 0.5f + (float)rand.NextDouble() * 0.5f, null, velocity);
        }
    }

    protected virtual void SpawnFloatingSediment(IAsyncParticleManager manager)
    {
        ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
        EntityPos entityPos = ((coreClientAPI.World.Player.Entity.EntityId == EntityId) ? Pos : ServerPos);
        double num = SelectionBox.XSize * 0.75f;
        Entity.SplashParticleProps.BasePos.Set(entityPos.X - num / 2.0, entityPos.InternalY + 0.0, entityPos.Z - num / 2.0);
        Entity.SplashParticleProps.AddPos.Set(num, 0.5, num);
        float num2 = (float)entityPos.Motion.Length();
        Entity.SplashParticleProps.AddVelocity.Set((float)entityPos.Motion.X * 20f, 0f, (float)entityPos.Motion.Z * 20f);
        float num3 = base.Properties.Attributes?["extraSplashParticlesMul"].AsFloat(1f) ?? 1f;
        Entity.SplashParticleProps.QuantityMul = 0.15f * num2 * 5f * 2f * num3;
        World.SpawnParticles(Entity.SplashParticleProps);
        SpawnWaterMovementParticles(Math.Max(Swimming ? 0.04f : 0f, num2 * 5f));
        FloatingSedimentParticles floatingSedimentParticles = new FloatingSedimentParticles();
        floatingSedimentParticles.SedimentPos.Set((int)entityPos.X, (int)entityPos.InternalY - 1, (int)entityPos.Z);
        Block block = (floatingSedimentParticles.SedimentBlock = World.BlockAccessor.GetBlock(floatingSedimentParticles.SedimentPos));
        if (insideBlock != null && (block.BlockMaterial == EnumBlockMaterial.Gravel || block.BlockMaterial == EnumBlockMaterial.Soil || block.BlockMaterial == EnumBlockMaterial.Sand))
        {
            floatingSedimentParticles.BasePos.Set(Entity.SplashParticleProps.BasePos);
            floatingSedimentParticles.AddPos.Set(Entity.SplashParticleProps.AddPos);
            floatingSedimentParticles.quantity = num2 * 150f;
            floatingSedimentParticles.waterColor = insideBlock.GetColor(coreClientAPI, floatingSedimentParticles.SedimentPos);
            manager.Spawn(floatingSedimentParticles);
        }
    }

    protected virtual bool onAnimControls(AnimationMetaData anim, bool wasActive, bool nowActive)
    {
        return false;
    }

    protected virtual void HandleHandAnimations(float dt)
    {
    }

    //
    // Summary:
    //     Gets the walk speed multiplier.
    //
    // Parameters:
    //   groundDragFactor:
    //     The amount of drag provided by the current ground. (Default: 0.3)
    public virtual double GetWalkSpeedMultiplier(double groundDragFactor = 0.3)
    {
        int num = (int)(base.SidedPos.InternalY - 0.05000000074505806);
        int num2 = (int)(base.SidedPos.InternalY + 0.009999999776482582);
        Block blockRaw = World.BlockAccessor.GetBlockRaw((int)base.SidedPos.X, num, (int)base.SidedPos.Z);
        insidePos.Set((int)base.SidedPos.X, num2, (int)base.SidedPos.Z);
        insideBlock = World.BlockAccessor.GetBlock(insidePos);
        double num3 = (servercontrols.Sneak ? ((double)GlobalConstants.SneakSpeedMultiplier) : 1.0) * (servercontrols.Sprint ? GlobalConstants.SprintSpeedMultiplier : 1.0);
        if (FeetInLiquid)
        {
            num3 /= 2.5;
        }

        return num3 * (double)(blockRaw.WalkSpeedMultiplier * ((num == num2) ? 1f : insideBlock.WalkSpeedMultiplier));
    }

    //
    // Summary:
    //     Serializes the slots contents to be stored in the SaveGame
    public override void ToBytes(BinaryWriter writer, bool forClient)
    {
        if (MountedOn != null)
        {
            TreeAttribute treeAttribute = new TreeAttribute();
            MountedOn?.MountableToTreeAttributes(treeAttribute);
            WatchedAttributes["mountedOn"] = treeAttribute;
        }
        else if (WatchedAttributes.HasAttribute("mountedOn"))
        {
            WatchedAttributes.RemoveAttribute("mountedOn");
        }

        base.ToBytes(writer, forClient);
        controls.ToBytes(writer);
    }

    //
    // Summary:
    //     Loads the entity from a stored byte array from the SaveGame
    //
    // Parameters:
    //   reader:
    //
    //   forClient:
    public override void FromBytes(BinaryReader reader, bool forClient)
    {
        try
        {
            base.FromBytes(reader, forClient);
            controls.FromBytes(reader, LoadControlsFromServer);
        }
        catch (EndOfStreamException innerException)
        {
            throw new Exception("EndOfStreamException thrown while reading entity, you might be able to recover your savegame through repair mode", innerException);
        }

        if (MountedOn != null && !WatchedAttributes.HasAttribute("mountedOn"))
        {
            TryUnmount();
        }
    }

    //
    // Summary:
    //     Relevant only for entities with heads, implemented in EntityAgent. Other sub-classes
    //     of Entity (if not EntityAgent) should similarly override this if the headYaw/headPitch
    //     are relevant to them
    protected override void SetHeadPositionToWatchedAttributes()
    {
        WatchedAttributes["headYaw"] = new FloatAttribute(ServerPos.HeadYaw);
        WatchedAttributes["headPitch"] = new FloatAttribute(ServerPos.HeadPitch);
    }

    //
    // Summary:
    //     Relevant only for entities with heads, implemented in EntityAgent. Other sub-classes
    //     of Entity (if not EntityAgent) should similarly override this if the headYaw/headPitch
    //     are relevant to them
    protected override void GetHeadPositionFromWatchedAttributes()
    {
        ServerPos.HeadYaw = WatchedAttributes.GetFloat("headYaw");
        ServerPos.HeadPitch = WatchedAttributes.GetFloat("headPitch");
    }

    //
    // Summary:
    //     Attempts to stop the hand action.
    //
    // Parameters:
    //   isCancel:
    //     Whether or not the action is cancelled or stopped.
    //
    //   cancelReason:
    //     The reason for stopping the action.
    //
    // Returns:
    //     Whether the stop was cancelled or not.
    public virtual bool TryStopHandAction(bool isCancel, EnumItemUseCancelReason cancelReason = EnumItemUseCancelReason.ReleasedMouse)
    {
        if (controls.HandUse == EnumHandInteract.None || RightHandItemSlot?.Itemstack == null)
        {
            return true;
        }

        float secondsPassed = (float)(World.ElapsedMilliseconds - controls.UsingBeginMS) / 1000f;
        if (isCancel)
        {
            controls.HandUse = RightHandItemSlot.Itemstack.Collectible.OnHeldUseCancel(secondsPassed, RightHandItemSlot, this, null, null, cancelReason);
        }
        else
        {
            controls.HandUse = EnumHandInteract.None;
            RightHandItemSlot.Itemstack.Collectible.OnHeldUseStop(secondsPassed, RightHandItemSlot, this, null, null, controls.HandUse);
        }

        return controls.HandUse == EnumHandInteract.None;
    }

    //
    // Summary:
    //     This walks the inventory for the entity agent.
    //
    // Parameters:
    //   handler:
    //     the event to fire while walking the inventory.
    public virtual void WalkInventory(OnInventorySlot handler)
    {
    }

    public override void UpdateDebugAttributes()
    {
        base.UpdateDebugAttributes();
        DebugAttributes.SetString("Herd Id", HerdId.ToString() ?? "");
    }

    public override bool TryGiveItemStack(ItemStack itemstack)
    {
        if (itemstack == null || itemstack.StackSize == 0)
        {
            return false;
        }

        List<EntityBehavior> list = base.SidedProperties?.Behaviors;
        EnumHandling handling = EnumHandling.PassThrough;
        if (list != null)
        {
            foreach (EntityBehavior item in list)
            {
                item.TryGiveItemStack(itemstack, ref handling);
                if (handling == EnumHandling.PreventSubsequent)
                {
                    break;
                }
            }
        }

        return handling != EnumHandling.PassThrough;
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
