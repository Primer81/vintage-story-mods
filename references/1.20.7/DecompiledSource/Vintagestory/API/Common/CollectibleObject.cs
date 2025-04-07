#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

//
// Summary:
//     Contains all properties shared by Blocks and Items
public abstract class CollectibleObject : RegistryObject
{
    public static readonly Size3f DefaultSize = new Size3f(0.5f, 0.5f, 0.5f);

    //
    // Summary:
    //     Liquids are handled and rendered differently than solid blocks.
    public EnumMatterState MatterState = EnumMatterState.Solid;

    //
    // Summary:
    //     Max amount of collectible that one default inventory slot can hold
    public int MaxStackSize = 64;

    //
    // Summary:
    //     How many uses does this collectible has when being used. Item disappears at durability
    //     0
    public int Durability = 1;

    //
    // Summary:
    //     Physical size of this collectible when held or (notionally) in a container. 0.5
    //     x 0.5 x 0.5 meters by default.
    //     Note, if all three dimensions are set to zero, the default will be used.
    public Size3f Dimensions = DefaultSize;

    //
    // Summary:
    //     When true, liquids become selectable to the player when being held in hands
    public bool LiquidSelectable;

    //
    // Summary:
    //     How much damage this collectible deals when used as a weapon
    public float AttackPower = 0.5f;

    //
    // Summary:
    //     If true, when the player holds the sneak key and right clicks with this item
    //     in hand, calls OnHeldInteractStart first. Without it, the order is reversed.
    //     Takes precedence over priority interact placed blocks.
    public bool HeldPriorityInteract;

    //
    // Summary:
    //     Until how for away can you attack entities using this collectibe
    public float AttackRange = GlobalConstants.DefaultAttackRange;

    //
    // Summary:
    //     From which damage sources does the item takes durability damage
    public EnumItemDamageSource[] DamagedBy;

    //
    // Summary:
    //     Modifies how fast the player can break a block when holding this item
    public Dictionary<EnumBlockMaterial, float> MiningSpeed;

    //
    // Summary:
    //     What tier this block can mine when held in hands
    public int ToolTier;

    public HeldSounds HeldSounds;

    //
    // Summary:
    //     List of creative tabs in which this collectible should appear in
    public string[] CreativeInventoryTabs;

    //
    // Summary:
    //     If you want to add itemstacks with custom attributes to the creative inventory,
    //     add them to this list
    public CreativeTabAndStackList[] CreativeInventoryStacks;

    //
    // Summary:
    //     Alpha test value for rendering in gui, fp hand, tp hand or on the ground
    public float RenderAlphaTest = 0.05f;

    //
    // Summary:
    //     Used for scaling, rotation or offseting the block when rendered in guis
    public ModelTransform GuiTransform;

    //
    // Summary:
    //     Used for scaling, rotation or offseting the block when rendered in the first
    //     person mode hand
    public ModelTransform FpHandTransform;

    //
    // Summary:
    //     Used for scaling, rotation or offseting the block when rendered in the third
    //     person mode hand
    public ModelTransform TpHandTransform;

    //
    // Summary:
    //     Used for scaling, rotation or offseting the block when rendered in the third
    //     person mode offhand
    public ModelTransform TpOffHandTransform;

    //
    // Summary:
    //     Used for scaling, rotation or offseting the rendered as a dropped item on the
    //     ground
    public ModelTransform GroundTransform;

    //
    // Summary:
    //     Custom Attributes that's always assiociated with this item
    public JsonObject Attributes;

    //
    // Summary:
    //     Information about the burnable states
    public CombustibleProperties CombustibleProps;

    //
    // Summary:
    //     Information about the nutrition states
    public FoodNutritionProperties NutritionProps;

    //
    // Summary:
    //     Information about the transitionable states
    public TransitionableProperties[] TransitionableProps;

    //
    // Summary:
    //     If set, the collectible can be ground into something else
    public GrindingProperties GrindingProps;

    //
    // Summary:
    //     If set, the collectible can be crushed into something else
    public CrushingProperties CrushingProps;

    //
    // Summary:
    //     Particles that should spawn in regular intervals from this block or item when
    //     held in hands
    public AdvancedParticleProperties[] ParticleProperties;

    //
    // Summary:
    //     The origin point from which particles are being spawned
    public Vec3f TopMiddlePos = new Vec3f(0.5f, 1f, 0.5f);

    //
    // Summary:
    //     If set, this item will be classified as given tool
    public EnumTool? Tool;

    //
    // Summary:
    //     Determines in which kind of bags the item can be stored in
    public EnumItemStorageFlags StorageFlags = EnumItemStorageFlags.General;

    //
    // Summary:
    //     Determines on whether an object floats on liquids or not. Water has a density
    //     of 1000
    public int MaterialDensity = 2000;

    //
    // Summary:
    //     The animation to play in 3rd person mod when hitting with this collectible
    public string HeldTpHitAnimation = "breakhand";

    //
    // Summary:
    //     The animation to play in 3rd person mod when holding this collectible in the
    //     right hand
    public string HeldRightTpIdleAnimation;

    //
    // Summary:
    //     The animation to play in 3rd person mod when holding this collectible in the
    //     left hand
    public string HeldLeftTpIdleAnimation;

    public string HeldLeftReadyAnimation;

    public string HeldRightReadyAnimation;

    //
    // Summary:
    //     The animation to play in 3rd person mod when using this collectible
    public string HeldTpUseAnimation = "interactstatic";

    //
    // Summary:
    //     The api object, assigned during OnLoaded
    protected ICoreAPI api;

    //
    // Summary:
    //     Modifiers that can alter the behavior of the item or block, mostly for held interaction
    public CollectibleBehavior[] CollectibleBehaviors = new CollectibleBehavior[0];

    //
    // Summary:
    //     For light emitting collectibles: hue, saturation and brightness value
    public byte[] LightHsv = new byte[3];

    //
    // Summary:
    //     This value is set the the BlockId or ItemId-Remapper if it encounters a block/item
    //     in the savegame, but no longer exists as a loaded block/item
    public bool IsMissing { get; set; }

    //
    // Summary:
    //     The block or item id
    public abstract int Id { get; }

    //
    // Summary:
    //     Block or Item?
    public abstract EnumItemClass ItemClass { get; }

    [Obsolete("Use tool tier")]
    public int MiningTier
    {
        get
        {
            return ToolTier;
        }
        set
        {
            ToolTier = value;
        }
    }

    //
    // Summary:
    //     For blocks and items, the hashcode is the id - useful when building HashSets
    public override int GetHashCode()
    {
        return Id;
    }

    public void OnLoadedNative(ICoreAPI api)
    {
        this.api = api;
        OnLoaded(api);
    }

    //
    // Summary:
    //     Server Side: Called one the collectible has been registered Client Side: Called
    //     once the collectible has been loaded from server packet
    public virtual void OnLoaded(ICoreAPI api)
    {
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        for (int i = 0; i < collectibleBehaviors.Length; i++)
        {
            collectibleBehaviors[i].OnLoaded(api);
        }
    }

    //
    // Summary:
    //     Called when the client/server is shutting down
    //
    // Parameters:
    //   api:
    public virtual void OnUnloaded(ICoreAPI api)
    {
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        for (int i = 0; i < collectibleBehaviors.Length; i++)
        {
            collectibleBehaviors[i].OnUnloaded(api);
        }
    }

    //
    // Summary:
    //     Should return the light HSV values. Warning: This method is likely to get called
    //     in a background thread. Please make sure your code in here is thread safe.
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    //     May be null
    //
    //   stack:
    //     Set if its an itemstack for which the engine wants to check the light level
    public virtual byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
    {
        return LightHsv;
    }

    //
    // Summary:
    //     Should return the nutrition properties of the item/block
    //
    // Parameters:
    //   world:
    //
    //   itemstack:
    //
    //   forEntity:
    public virtual FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
    {
        return NutritionProps;
    }

    //
    // Summary:
    //     Should return the transition properties of the item/block when in itemstack form
    //
    //
    // Parameters:
    //   world:
    //
    //   itemstack:
    //
    //   forEntity:
    public virtual TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
    {
        return TransitionableProps;
    }

    //
    // Summary:
    //     Should returns true if this collectible requires UpdateAndGetTransitionStates()
    //     to be called when ticking.
    //     Typical usage: true if this collectible itself has transitionable properties,
    //     or true for collectibles which hold other itemstacks with transitionable properties
    //     (for example, a cooked food container)
    //
    // Parameters:
    //   world:
    //
    //   itemstack:
    public virtual bool RequiresTransitionableTicking(IWorldAccessor world, ItemStack itemstack)
    {
        if (TransitionableProps != null)
        {
            return TransitionableProps.Length != 0;
        }

        return false;
    }

    //
    // Summary:
    //     Should return in which storage containers this item can be placed in
    //
    // Parameters:
    //   itemstack:
    public virtual EnumItemStorageFlags GetStorageFlags(ItemStack itemstack)
    {
        bool flag = false;
        EnumItemStorageFlags enumItemStorageFlags = StorageFlags;
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        foreach (CollectibleBehavior obj in collectibleBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            EnumItemStorageFlags storageFlags = obj.GetStorageFlags(itemstack, ref handling);
            if (handling != 0)
            {
                flag = true;
                enumItemStorageFlags = storageFlags;
            }

            if (handling == EnumHandling.PreventSubsequent)
            {
                return enumItemStorageFlags;
            }
        }

        if (flag)
        {
            return enumItemStorageFlags;
        }

        IHeldBag collectibleInterface = GetCollectibleInterface<IHeldBag>();
        if (collectibleInterface != null && (enumItemStorageFlags & EnumItemStorageFlags.Backpack) > (EnumItemStorageFlags)0 && collectibleInterface.IsEmpty(itemstack))
        {
            return EnumItemStorageFlags.General | EnumItemStorageFlags.Backpack;
        }

        return enumItemStorageFlags;
    }

    //
    // Summary:
    //     Returns a hardcoded rgb color (green->yellow->red) that is representative for
    //     its remaining durability vs total durability
    //
    // Parameters:
    //   itemstack:
    public virtual int GetItemDamageColor(ItemStack itemstack)
    {
        int maxDurability = GetMaxDurability(itemstack);
        if (maxDurability == 0)
        {
            return 0;
        }

        int num = GameMath.Clamp(100 * itemstack.Collectible.GetRemainingDurability(itemstack) / maxDurability, 0, 99);
        return GuiStyle.DamageColorGradient[num];
    }

    //
    // Summary:
    //     Return true if remaining durability != total durability
    //
    // Parameters:
    //   itemstack:
    public virtual bool ShouldDisplayItemDamage(ItemStack itemstack)
    {
        return GetMaxDurability(itemstack) != GetRemainingDurability(itemstack);
    }

    //
    // Summary:
    //     This method is called before rendering the item stack into GUI, first person
    //     hand, third person hand and/or on the ground The renderinfo object is pre-filled
    //     with default values.
    //
    // Parameters:
    //   capi:
    //
    //   itemstack:
    //
    //   target:
    //
    //   renderinfo:
    public virtual void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        for (int i = 0; i < CollectibleBehaviors.Length; i++)
        {
            CollectibleBehaviors[i].OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }
    }

    [Obsolete("Use GetMaxDurability instead")]
    public virtual int GetDurability(IItemStack itemstack)
    {
        return GetMaxDurability(itemstack as ItemStack);
    }

    //
    // Summary:
    //     Returns the items total durability
    //
    // Parameters:
    //   itemstack:
    public virtual int GetMaxDurability(ItemStack itemstack)
    {
        return Durability;
    }

    public virtual int GetRemainingDurability(ItemStack itemstack)
    {
        return (int)itemstack.Attributes.GetDecimal("durability", GetMaxDurability(itemstack));
    }

    //
    // Summary:
    //     The amount of damage dealt when used as a weapon
    //
    // Parameters:
    //   withItemStack:
    public virtual float GetAttackPower(IItemStack withItemStack)
    {
        return AttackPower;
    }

    //
    // Summary:
    //     The the attack range when used as a weapon
    //
    // Parameters:
    //   withItemStack:
    public virtual float GetAttackRange(IItemStack withItemStack)
    {
        return AttackRange;
    }

    //
    // Summary:
    //     Player is holding this collectible and breaks the targeted block
    //
    // Parameters:
    //   player:
    //
    //   blockSel:
    //
    //   itemslot:
    //
    //   remainingResistance:
    //
    //   dt:
    //
    //   counter:
    public virtual float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
    {
        bool flag = false;
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        foreach (CollectibleBehavior obj in collectibleBehaviors)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            float num = obj.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter, ref handled);
            if (handled != 0)
            {
                remainingResistance = num;
                flag = true;
            }

            if (handled == EnumHandling.PreventSubsequent)
            {
                return remainingResistance;
            }
        }

        if (flag)
        {
            return remainingResistance;
        }

        Block block = player.Entity.World.BlockAccessor.GetBlock(blockSel.Position);
        EnumBlockMaterial blockMaterial = block.GetBlockMaterial(api.World.BlockAccessor, blockSel.Position);
        Vec3f normalf = blockSel.Face.Normalf;
        Random rand = player.Entity.World.Rand;
        bool flag2 = block.RequiredMiningTier > 0 && itemslot.Itemstack?.Collectible != null && (itemslot.Itemstack.Collectible.ToolTier < block.RequiredMiningTier || MiningSpeed == null || !MiningSpeed.ContainsKey(blockMaterial));
        double num2 = ((blockMaterial == EnumBlockMaterial.Ore) ? 0.72 : 0.12);
        if (counter % 5 == 0 && (rand.NextDouble() < num2 || flag2) && (blockMaterial == EnumBlockMaterial.Stone || blockMaterial == EnumBlockMaterial.Ore) && (Tool.GetValueOrDefault() == EnumTool.Pickaxe || Tool.GetValueOrDefault() == EnumTool.Hammer))
        {
            double num3 = (double)blockSel.Position.X + blockSel.HitPosition.X;
            double num4 = (double)blockSel.Position.Y + blockSel.HitPosition.Y;
            double num5 = (double)blockSel.Position.Z + blockSel.HitPosition.Z;
            player.Entity.World.SpawnParticles(new SimpleParticleProperties
            {
                MinQuantity = 0f,
                AddQuantity = 8f,
                Color = ColorUtil.ToRgba(255, 255, 255, 128),
                MinPos = new Vec3d(num3 + (double)(normalf.X * 0.01f), num4 + (double)(normalf.Y * 0.01f), num5 + (double)(normalf.Z * 0.01f)),
                AddPos = new Vec3d(0.0, 0.0, 0.0),
                MinVelocity = new Vec3f(4f * normalf.X, 4f * normalf.Y, 4f * normalf.Z),
                AddVelocity = new Vec3f(8f * ((float)rand.NextDouble() - 0.5f), 8f * ((float)rand.NextDouble() - 0.5f), 8f * ((float)rand.NextDouble() - 0.5f)),
                LifeLength = 0.025f,
                GravityEffect = 0f,
                MinSize = 0.03f,
                MaxSize = 0.4f,
                ParticleModel = EnumParticleModel.Cube,
                VertexFlags = 200,
                SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.15f)
            }, player);
        }

        if (flag2)
        {
            return remainingResistance;
        }

        return remainingResistance - GetMiningSpeed(itemslot.Itemstack, blockSel, block, player) * dt;
    }

    //
    // Summary:
    //     Whenever the collectible was modified while inside a slot, usually when it was
    //     moved, split or merged.
    //
    // Parameters:
    //   world:
    //
    //   slot:
    //     The slot the item is or was in
    //
    //   extractedStack:
    //     Non null if the itemstack was removed from this slot
    public virtual void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
    {
    }

    //
    // Summary:
    //     Player has broken a block while holding this collectible. Return false if you
    //     want to cancel the block break event.
    //
    // Parameters:
    //   world:
    //
    //   byEntity:
    //
    //   itemslot:
    //
    //   blockSel:
    //
    //   dropQuantityMultiplier:
    public virtual bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f)
    {
        bool flag = true;
        bool flag2 = false;
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        foreach (CollectibleBehavior obj in collectibleBehaviors)
        {
            EnumHandling bhHandling = EnumHandling.PassThrough;
            bool flag3 = obj.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref bhHandling);
            if (bhHandling != 0)
            {
                flag = flag && flag3;
                flag2 = true;
            }

            if (bhHandling == EnumHandling.PreventSubsequent)
            {
                return flag;
            }
        }

        if (flag2)
        {
            return flag;
        }

        IPlayer byPlayer = null;
        if (byEntity is EntityPlayer)
        {
            byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
        }

        (blockSel.Block ?? world.BlockAccessor.GetBlock(blockSel.Position)).OnBlockBroken(world, blockSel.Position, byPlayer, dropQuantityMultiplier);
        if (DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking))
        {
            DamageItem(world, byEntity, itemslot);
        }

        return true;
    }

    //
    // Summary:
    //     Called every game tick when the player breaks a block with this item in his hands.
    //     Returns the mining speed for given block.
    //
    // Parameters:
    //   itemstack:
    //
    //   blockSel:
    //
    //   block:
    //
    //   forPlayer:
    public virtual float GetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer)
    {
        float num = 1f;
        EnumBlockMaterial blockMaterial = block.GetBlockMaterial(api.World.BlockAccessor, blockSel.Position);
        if (blockMaterial == EnumBlockMaterial.Ore || blockMaterial == EnumBlockMaterial.Stone)
        {
            num = forPlayer.Entity.Stats.GetBlended("miningSpeedMul");
        }

        if (MiningSpeed == null || !MiningSpeed.ContainsKey(blockMaterial))
        {
            return num;
        }

        return MiningSpeed[blockMaterial] * GlobalConstants.ToolMiningSpeedModifier * num;
    }

    //
    // Summary:
    //     Not implemented yet
    //
    // Parameters:
    //   slot:
    //
    //   byEntity:
    [Obsolete]
    public virtual ModelTransformKeyFrame[] GeldHeldFpHitAnimation(ItemSlot slot, Entity byEntity)
    {
        return null;
    }

    //
    // Summary:
    //     Called when an entity uses this item to hit something in 3rd person mode
    //
    // Parameters:
    //   slot:
    //
    //   byEntity:
    public virtual string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
    {
        EnumHandling bhHandling = EnumHandling.PassThrough;
        string anim = null;
        WalkBehaviors(delegate (CollectibleBehavior bh, ref EnumHandling hd)
        {
            string heldTpHitAnimation = bh.GetHeldTpHitAnimation(slot, byEntity, ref bhHandling);
            if (bhHandling != 0)
            {
                anim = heldTpHitAnimation;
            }
        }, delegate
        {
            anim = HeldTpHitAnimation;
        });
        return anim;
    }

    //
    // Summary:
    //     Called when an entity holds this item in hands in 3rd person mode
    //
    // Parameters:
    //   activeHotbarSlot:
    //
    //   forEntity:
    //
    //   hand:
    public virtual string GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        EnumHandling bhHandling = EnumHandling.PassThrough;
        string anim = null;
        WalkBehaviors(delegate (CollectibleBehavior bh, ref EnumHandling hd)
        {
            string heldReadyAnimation = bh.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand, ref bhHandling);
            if (bhHandling != 0)
            {
                anim = heldReadyAnimation;
            }
        }, delegate
        {
            anim = ((hand == EnumHand.Left) ? HeldLeftReadyAnimation : HeldRightReadyAnimation);
        });
        return anim;
    }

    //
    // Summary:
    //     Called when an entity holds this item in hands in 3rd person mode
    //
    // Parameters:
    //   activeHotbarSlot:
    //
    //   forEntity:
    //
    //   hand:
    public virtual string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        EnumHandling bhHandling = EnumHandling.PassThrough;
        string anim = null;
        WalkBehaviors(delegate (CollectibleBehavior bh, ref EnumHandling hd)
        {
            string heldTpIdleAnimation = bh.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand, ref bhHandling);
            if (bhHandling != 0)
            {
                anim = heldTpIdleAnimation;
            }
        }, delegate
        {
            anim = ((hand == EnumHand.Left) ? HeldLeftTpIdleAnimation : HeldRightTpIdleAnimation);
        });
        return anim;
    }

    //
    // Summary:
    //     Called when an entity holds this item in hands in 3rd person mode
    //
    // Parameters:
    //   activeHotbarSlot:
    //
    //   forEntity:
    public virtual string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
    {
        EnumHandling bhHandling = EnumHandling.PassThrough;
        string anim = null;
        WalkBehaviors(delegate (CollectibleBehavior bh, ref EnumHandling hd)
        {
            string heldTpUseAnimation = bh.GetHeldTpUseAnimation(activeHotbarSlot, forEntity, ref bhHandling);
            if (bhHandling != 0)
            {
                anim = heldTpUseAnimation;
            }
        }, delegate
        {
            if (GetNutritionProperties(forEntity.World, activeHotbarSlot.Itemstack, forEntity) == null)
            {
                anim = HeldTpUseAnimation;
            }
        });
        return anim;
    }

    //
    // Summary:
    //     An entity used this collectibe to attack something
    //
    // Parameters:
    //   world:
    //
    //   byEntity:
    //
    //   attackedEntity:
    //
    //   itemslot:
    public virtual void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
    {
        if (DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.Attacking) && attackedEntity != null && attackedEntity.Alive)
        {
            DamageItem(world, byEntity, itemslot);
        }
    }

    //
    // Summary:
    //     Called when this collectible is attempted to being used as part of a crafting
    //     recipe and should get consumed now. Return false if it doesn't match the ingredient
    //
    //
    // Parameters:
    //   inputStack:
    //
    //   gridRecipe:
    //
    //   ingredient:
    public virtual bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
    {
        if (ingredient.IsTool && ingredient.ToolDurabilityCost > inputStack.Collectible.GetRemainingDurability(inputStack))
        {
            return false;
        }

        return true;
    }

    //
    // Summary:
    //     Called when this collectible is being used as part of a crafting recipe and should
    //     get consumed now
    //
    // Parameters:
    //   allInputSlots:
    //
    //   stackInSlot:
    //
    //   gridRecipe:
    //
    //   fromIngredient:
    //
    //   byPlayer:
    //
    //   quantity:
    public virtual void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
    {
        JsonObject attributes = Attributes;
        if (attributes != null && attributes["noConsumeOnCrafting"].AsBool())
        {
            return;
        }

        if (fromIngredient.IsTool)
        {
            stackInSlot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, stackInSlot, fromIngredient.ToolDurabilityCost);
            return;
        }

        stackInSlot.Itemstack.StackSize -= quantity;
        if (stackInSlot.Itemstack.StackSize <= 0)
        {
            stackInSlot.Itemstack = null;
            stackInSlot.MarkDirty();
        }

        if (fromIngredient.ReturnedStack != null)
        {
            ItemStack itemstack = fromIngredient.ReturnedStack.ResolvedItemstack.Clone();
            if (!byPlayer.InventoryManager.TryGiveItemstack(itemstack, slotNotifyEffect: true))
            {
                api.World.SpawnItemEntity(itemstack, byPlayer.Entity.Pos.XYZ);
            }
        }
    }

    //
    // Summary:
    //     Called when a matching grid recipe has been found and an item is placed into
    //     the crafting output slot (which is still before the player clicks on the output
    //     slot to actually craft the item and consume the ingredients)
    //
    // Parameters:
    //   allInputslots:
    //
    //   outputSlot:
    //
    //   byRecipe:
    public virtual void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
    {
        EnumHandling bhHandling = EnumHandling.PassThrough;
        WalkBehaviors(delegate (CollectibleBehavior bh, ref EnumHandling hd)
        {
            bh.OnCreatedByCrafting(allInputslots, outputSlot, ref bhHandling);
        }, delegate
        {
            float num = 0f;
            float num2 = 0f;
            if (byRecipe.AverageDurability)
            {
                GridRecipeIngredient[] resolvedIngredients = byRecipe.resolvedIngredients;
                ItemSlot[] array = allInputslots;
                foreach (ItemSlot itemSlot in array)
                {
                    if (!itemSlot.Empty)
                    {
                        ItemStack itemstack = itemSlot.Itemstack;
                        int maxDurability = itemstack.Collectible.GetMaxDurability(itemstack);
                        if (maxDurability == 0)
                        {
                            num += 0.125f;
                            num2 += 0.125f;
                        }
                        else
                        {
                            bool flag = false;
                            GridRecipeIngredient[] array2 = resolvedIngredients;
                            foreach (GridRecipeIngredient gridRecipeIngredient in array2)
                            {
                                if (gridRecipeIngredient != null && gridRecipeIngredient.IsTool && gridRecipeIngredient.SatisfiesAsIngredient(itemstack))
                                {
                                    flag = true;
                                    break;
                                }
                            }

                            if (!flag)
                            {
                                num2 += 1f;
                                int remainingDurability = itemstack.Collectible.GetRemainingDurability(itemstack);
                                num += (float)remainingDurability / (float)maxDurability;
                            }
                        }
                    }
                }

                float num3 = num / num2;
                if (num3 < 1f)
                {
                    outputSlot.Itemstack.Attributes.SetInt("durability", (int)Math.Max(1f, num3 * (float)outputSlot.Itemstack.Collectible.GetMaxDurability(outputSlot.Itemstack)));
                }
            }

            TransitionableProperties transitionableProperties = outputSlot.Itemstack.Collectible.GetTransitionableProperties(api.World, outputSlot.Itemstack, null)?.FirstOrDefault((TransitionableProperties p) => p.Type == EnumTransitionType.Perish);
            if (transitionableProperties != null)
            {
                transitionableProperties.TransitionedStack.Resolve(api.World, "oncrafted perished stack", Code);
                CarryOverFreshness(api, allInputslots, new ItemStack[1] { outputSlot.Itemstack }, transitionableProperties);
            }
        });
    }

    //
    // Summary:
    //     Called after the player has taken out the item from the output slot
    //
    // Parameters:
    //   slots:
    //
    //   outputSlot:
    //
    //   matchingRecipe:
    //
    // Returns:
    //     true to prevent default ingredient consumption
    public virtual bool ConsumeCraftingIngredients(ItemSlot[] slots, ItemSlot outputSlot, GridRecipe matchingRecipe)
    {
        return false;
    }

    //
    // Summary:
    //     Sets the items durability
    //
    // Parameters:
    //   itemstack:
    public virtual void SetDurability(ItemStack itemstack, int amount)
    {
        itemstack.Attributes.SetInt("durability", amount);
    }

    //
    // Summary:
    //     Causes the item to be damaged. Will play a breaking sound and removes the itemstack
    //     if no more durability is left
    //
    // Parameters:
    //   world:
    //
    //   byEntity:
    //
    //   itemslot:
    //
    //   amount:
    //     Amount of damage
    public virtual void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
    {
        ItemStack itemstack = itemslot.Itemstack;
        int remainingDurability = itemstack.Collectible.GetRemainingDurability(itemstack);
        remainingDurability -= amount;
        itemstack.Attributes.SetInt("durability", remainingDurability);
        if (remainingDurability <= 0)
        {
            itemslot.Itemstack = null;
            IPlayer player = (byEntity as EntityPlayer)?.Player;
            if (player != null)
            {
                if (Tool.HasValue)
                {
                    string ident = Attributes?["slotRefillIdentifier"].ToString();
                    RefillSlotIfEmpty(itemslot, byEntity as EntityAgent, (ItemStack stack) => (ident == null) ? (stack.Collectible.Tool == Tool) : (stack.ItemAttributes?["slotRefillIdentifier"]?.ToString() == ident));
                }

                if (itemslot.Itemstack != null && !itemslot.Itemstack.Attributes.HasAttribute("durability"))
                {
                    itemstack = itemslot.Itemstack;
                    itemstack.Attributes.SetInt("durability", itemstack.Collectible.GetMaxDurability(itemstack));
                }

                world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), player);
            }
            else
            {
                world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.SidedPos.X, byEntity.SidedPos.Y, byEntity.SidedPos.Z, null, 1f, 16f);
            }

            world.SpawnCubeParticles(byEntity.SidedPos.XYZ.Add(byEntity.SelectionBox.Y2 / 2f), itemstack, 0.25f, 30, 1f, player);
        }

        itemslot.MarkDirty();
    }

    public virtual void RefillSlotIfEmpty(ItemSlot slot, EntityAgent byEntity, ActionConsumable<ItemStack> matcher)
    {
        if (!slot.Empty)
        {
            return;
        }

        byEntity.WalkInventory(delegate (ItemSlot invslot)
        {
            if (invslot is ItemSlotCreative)
            {
                return true;
            }

            InventoryBase inventory = invslot.Inventory;
            if (!(inventory is InventoryBasePlayer) && !inventory.HasOpened((byEntity as EntityPlayer).Player))
            {
                return true;
            }

            if (invslot.Itemstack != null && matcher(invslot.Itemstack))
            {
                invslot.TryPutInto(byEntity.World, slot);
                invslot.Inventory?.PerformNotifySlot(invslot.Inventory.GetSlotId(invslot));
                slot.Inventory?.PerformNotifySlot(slot.Inventory.GetSlotId(slot));
                slot.MarkDirty();
                invslot.MarkDirty();
                return false;
            }

            return true;
        });
    }

    public virtual SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        for (int i = 0; i < CollectibleBehaviors.Length; i++)
        {
            SkillItem[] toolModes = CollectibleBehaviors[i].GetToolModes(slot, forPlayer, blockSel);
            if (toolModes != null)
            {
                return toolModes;
            }
        }

        return null;
    }

    //
    // Summary:
    //     Should return the current items tool mode.
    //
    // Parameters:
    //   slot:
    //
    //   byPlayer:
    //
    //   blockSelection:
    public virtual int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
    {
        for (int i = 0; i < CollectibleBehaviors.Length; i++)
        {
            int toolMode = CollectibleBehaviors[i].GetToolMode(slot, byPlayer, blockSelection);
            if (toolMode != 0)
            {
                return toolMode;
            }
        }

        return 0;
    }

    //
    // Summary:
    //     Should set given toolmode
    //
    // Parameters:
    //   slot:
    //
    //   byPlayer:
    //
    //   blockSelection:
    //
    //   toolMode:
    public virtual void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
    {
        for (int i = 0; i < CollectibleBehaviors.Length; i++)
        {
            CollectibleBehaviors[i].SetToolMode(slot, byPlayer, blockSelection, toolMode);
        }
    }

    //
    // Summary:
    //     This method is called during the opaque render pass when this item or block is
    //     being held in hands
    //
    // Parameters:
    //   inSlot:
    //
    //   byPlayer:
    public virtual void OnHeldRenderOpaque(ItemSlot inSlot, IClientPlayer byPlayer)
    {
    }

    //
    // Summary:
    //     This method is called during the order independent transparency render pass when
    //     this item or block is being held in hands
    //
    // Parameters:
    //   inSlot:
    //
    //   byPlayer:
    public virtual void OnHeldRenderOit(ItemSlot inSlot, IClientPlayer byPlayer)
    {
    }

    //
    // Summary:
    //     This method is called during the ortho (for 2D GUIs) render pass when this item
    //     or block is being held in hands
    //
    // Parameters:
    //   inSlot:
    //
    //   byPlayer:
    public virtual void OnHeldRenderOrtho(ItemSlot inSlot, IClientPlayer byPlayer)
    {
    }

    //
    // Summary:
    //     Called every frame when the player is holding this collectible in his hands.
    //     Is not called during OnUsing() or OnAttacking()
    //
    // Parameters:
    //   slot:
    //
    //   byEntity:
    public virtual void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
    {
    }

    public virtual void OnHeldActionAnimStart(ItemSlot slot, EntityAgent byEntity, EnumHandInteract type)
    {
    }

    //
    // Summary:
    //     Called every game tick when this collectible is in dropped form in the world
    //     (i.e. as EntityItem)
    //
    // Parameters:
    //   entityItem:
    public virtual void OnGroundIdle(EntityItem entityItem)
    {
        if (!entityItem.Swimming || api.Side != EnumAppSide.Server)
        {
            return;
        }

        JsonObject attributes = Attributes;
        if (attributes != null && attributes.IsTrue("dissolveInWater"))
        {
            if (api.World.Rand.NextDouble() < 0.01)
            {
                api.World.SpawnCubeParticles(entityItem.ServerPos.XYZ, entityItem.Itemstack.Clone(), 0.1f, 80, 0.3f);
                entityItem.Die();
            }
            else if (api.World.Rand.NextDouble() < 0.2)
            {
                api.World.SpawnCubeParticles(entityItem.ServerPos.XYZ, entityItem.Itemstack.Clone(), 0.1f, 2, 0.2f + (float)api.World.Rand.NextDouble() / 5f);
            }
        }
    }

    //
    // Summary:
    //     Called every frame when this item is being displayed in the gui
    //
    // Parameters:
    //   world:
    //
    //   stack:
    public virtual void InGuiIdle(IWorldAccessor world, ItemStack stack)
    {
    }

    //
    // Summary:
    //     Called when this item was collected by an entity
    //
    // Parameters:
    //   stack:
    //
    //   entity:
    public virtual void OnCollected(ItemStack stack, Entity entity)
    {
    }

    //
    // Summary:
    //     General begin use access. Override OnHeldAttackStart or OnHeldInteractStart to
    //     alter the behavior.
    //
    // Parameters:
    //   slot:
    //
    //   byEntity:
    //
    //   blockSel:
    //
    //   entitySel:
    //
    //   useType:
    //
    //   firstEvent:
    //     True on first mouse down
    //
    //   handling:
    //     Whether or not to do any subsequent actions. If not set or set to NotHandled,
    //     the action will not called on the server.
    public virtual void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
    {
        switch (useType)
        {
            case EnumHandInteract.HeldItemAttack:
                OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
                break;
            case EnumHandInteract.HeldItemInteract:
                OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                break;
        }
    }

    //
    // Summary:
    //     General cancel use access. Override OnHeldAttackCancel or OnHeldInteractCancel
    //     to alter the behavior.
    //
    // Parameters:
    //   secondsPassed:
    //
    //   slot:
    //
    //   byEntity:
    //
    //   blockSel:
    //
    //   entitySel:
    //
    //   cancelReason:
    public EnumHandInteract OnHeldUseCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
    {
        EnumHandInteract handUse = byEntity.Controls.HandUse;
        if (!((handUse == EnumHandInteract.HeldItemAttack) ? OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSel, entitySel, cancelReason) : OnHeldInteractCancel(secondsPassed, slot, byEntity, blockSel, entitySel, cancelReason)))
        {
            return handUse;
        }

        return EnumHandInteract.None;
    }

    //
    // Summary:
    //     General using access. Override OnHeldAttackStep or OnHeldInteractStep to alter
    //     the behavior.
    //
    // Parameters:
    //   secondsPassed:
    //
    //   slot:
    //
    //   byEntity:
    //
    //   blockSel:
    //
    //   entitySel:
    public EnumHandInteract OnHeldUseStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        EnumHandInteract handUse = byEntity.Controls.HandUse;
        if (!((handUse == EnumHandInteract.HeldItemAttack) ? OnHeldAttackStep(secondsPassed, slot, byEntity, blockSel, entitySel) : OnHeldInteractStep(secondsPassed, slot, byEntity, blockSel, entitySel)))
        {
            return EnumHandInteract.None;
        }

        return handUse;
    }

    //
    // Summary:
    //     General use over access. Override OnHeldAttackStop or OnHeldInteractStop to alter
    //     the behavior.
    //
    // Parameters:
    //   secondsPassed:
    //
    //   slot:
    //
    //   byEntity:
    //
    //   blockSel:
    //
    //   entitySel:
    //
    //   useType:
    public void OnHeldUseStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType)
    {
        if (useType == EnumHandInteract.HeldItemAttack)
        {
            OnHeldAttackStop(secondsPassed, slot, byEntity, blockSel, entitySel);
        }
        else
        {
            OnHeldInteractStop(secondsPassed, slot, byEntity, blockSel, entitySel);
        }
    }

    //
    // Summary:
    //     When the player has begun using this item for attacking (left mouse click). Return
    //     true to play a custom action.
    //
    // Parameters:
    //   slot:
    //
    //   byEntity:
    //
    //   blockSel:
    //
    //   entitySel:
    //
    //   handling:
    //     Whether or not to do any subsequent actions. If not set or set to NotHandled,
    //     the action will not called on the server.
    public virtual void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        EnumHandHandling bhHandHandling = EnumHandHandling.NotHandled;
        WalkBehaviors(delegate (CollectibleBehavior bh, ref EnumHandling hd)
        {
            bh.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref bhHandHandling, ref hd);
        }, delegate
        {
            if (HeldSounds?.Attack != null && api.World.Side == EnumAppSide.Client)
            {
                api.World.PlaySoundAt(HeldSounds.Attack, 0.0, 0.0, 0.0, null, 0.9f + (float)api.World.Rand.NextDouble() * 0.2f);
            }
        });
        handling = bhHandHandling;
    }

    //
    // Summary:
    //     When the player has canceled a custom attack action. Return false to deny action
    //     cancellation.
    //
    // Parameters:
    //   secondsPassed:
    //
    //   slot:
    //
    //   byEntity:
    //
    //   blockSelection:
    //
    //   entitySel:
    //
    //   cancelReason:
    public virtual bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
    {
        bool retval = false;
        WalkBehaviors(delegate (CollectibleBehavior bh, ref EnumHandling hd)
        {
            bool flag = bh.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason, ref hd);
            if (hd != 0)
            {
                retval = flag;
            }
        }, delegate
        {
        });
        return retval;
    }

    //
    // Summary:
    //     Called continously when a custom attack action is playing. Return false to stop
    //     the action.
    //
    // Parameters:
    //   secondsPassed:
    //
    //   slot:
    //
    //   byEntity:
    //
    //   blockSelection:
    //
    //   entitySel:
    public virtual bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
    {
        bool retval = false;
        WalkBehaviors(delegate (CollectibleBehavior bh, ref EnumHandling hd)
        {
            bool flag = bh.OnHeldAttackStep(secondsPassed, slot, byEntity, blockSelection, entitySel, ref hd);
            if (hd != 0)
            {
                retval = flag;
            }
        }, delegate
        {
        });
        return retval;
    }

    //
    // Summary:
    //     Called when a custom attack action is finished
    //
    // Parameters:
    //   secondsPassed:
    //
    //   slot:
    //
    //   byEntity:
    //
    //   blockSelection:
    //
    //   entitySel:
    public virtual void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
    {
        WalkBehaviors(delegate (CollectibleBehavior bh, ref EnumHandling hd)
        {
            bh.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel, ref hd);
        }, delegate
        {
        });
    }

    //
    // Summary:
    //     Called when the player right clicks while holding this block/item in his hands
    //
    //
    // Parameters:
    //   slot:
    //
    //   byEntity:
    //
    //   blockSel:
    //
    //   entitySel:
    //
    //   firstEvent:
    //     True when the player pressed the right mouse button on this block. Every subsequent
    //     call, while the player holds right mouse down will be false, it gets called every
    //     second while right mouse is down
    //
    //   handling:
    //     Whether or not to do any subsequent actions. If not set or set to NotHandled,
    //     the action will not called on the server.
    public virtual void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        EnumHandHandling handHandling = EnumHandHandling.NotHandled;
        bool flag = false;
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        foreach (CollectibleBehavior obj in collectibleBehaviors)
        {
            EnumHandling handling2 = EnumHandling.PassThrough;
            obj.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling2);
            if (handling2 != 0)
            {
                handling = handHandling;
                flag = true;
            }

            if (handling2 == EnumHandling.PreventSubsequent)
            {
                return;
            }
        }

        if (!flag)
        {
            tryEatBegin(slot, byEntity, ref handHandling);
            handling = handHandling;
        }
    }

    //
    // Summary:
    //     Called every frame while the player is using this collectible. Return false to
    //     stop the interaction.
    //
    // Parameters:
    //   secondsUsed:
    //
    //   slot:
    //
    //   byEntity:
    //
    //   blockSel:
    //
    //   entitySel:
    //
    // Returns:
    //     False if the interaction should be stopped. True if the interaction should continue
    public virtual bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        bool flag = true;
        bool flag2 = false;
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        foreach (CollectibleBehavior obj in collectibleBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
            if (handling != 0)
            {
                flag = flag && flag3;
                flag2 = true;
            }

            if (handling == EnumHandling.PreventSubsequent)
            {
                return flag;
            }
        }

        if (flag2)
        {
            return flag;
        }

        return tryEatStep(secondsUsed, slot, byEntity);
    }

    //
    // Summary:
    //     Called when the player successfully completed the using action, always called
    //     once an interaction is over
    //
    // Parameters:
    //   secondsUsed:
    //
    //   slot:
    //
    //   byEntity:
    //
    //   blockSel:
    //
    //   entitySel:
    public virtual void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        bool flag = false;
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        foreach (CollectibleBehavior obj in collectibleBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            obj.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
            if (handling != 0)
            {
                flag = true;
            }

            if (handling == EnumHandling.PreventSubsequent)
            {
                return;
            }
        }

        if (!flag)
        {
            tryEatStop(secondsUsed, slot, byEntity);
        }
    }

    //
    // Summary:
    //     When the player released the right mouse button. Return false to deny the cancellation
    //     (= will keep using the item until OnHeldInteractStep returns false).
    //
    // Parameters:
    //   secondsUsed:
    //
    //   slot:
    //
    //   byEntity:
    //
    //   blockSel:
    //
    //   entitySel:
    //
    //   cancelReason:
    public virtual bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
    {
        bool flag = true;
        bool flag2 = false;
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        foreach (CollectibleBehavior obj in collectibleBehaviors)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            bool flag3 = obj.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
            if (handled != 0)
            {
                flag = flag && flag3;
                flag2 = true;
            }

            if (handled == EnumHandling.PreventSubsequent)
            {
                return flag;
            }
        }

        if (flag2)
        {
            return flag;
        }

        return true;
    }

    //
    // Summary:
    //     Tries to eat the contents in the slot, first call
    //
    // Parameters:
    //   slot:
    //
    //   byEntity:
    //
    //   handling:
    //
    //   eatSound:
    //
    //   eatSoundRepeats:
    protected virtual void tryEatBegin(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling, string eatSound = "eat", int eatSoundRepeats = 1)
    {
        if (!slot.Empty && GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity) != null)
        {
            byEntity.World.RegisterCallback(delegate
            {
                playEatSound(byEntity, eatSound, eatSoundRepeats);
            }, 500);
            byEntity.AnimManager?.StartAnimation("eat");
            handling = EnumHandHandling.PreventDefault;
        }
    }

    protected void playEatSound(EntityAgent byEntity, string eatSound = "eat", int eatSoundRepeats = 1)
    {
        if (byEntity.Controls.HandUse != EnumHandInteract.HeldItemInteract)
        {
            return;
        }

        IPlayer dualCallByPlayer = null;
        if (byEntity is EntityPlayer)
        {
            dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
        }

        byEntity.PlayEntitySound(eatSound, dualCallByPlayer);
        eatSoundRepeats--;
        if (eatSoundRepeats > 0)
        {
            byEntity.World.RegisterCallback(delegate
            {
                playEatSound(byEntity, eatSound, eatSoundRepeats);
            }, 300);
        }
    }

    //
    // Summary:
    //     Tries to eat the contents in the slot, eat step call
    //
    // Parameters:
    //   secondsUsed:
    //
    //   slot:
    //
    //   byEntity:
    //
    //   spawnParticleStack:
    protected virtual bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, ItemStack spawnParticleStack = null)
    {
        if (GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity) == null)
        {
            return false;
        }

        Vec3d xYZ = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
        xYZ.X += byEntity.LocalEyePos.X;
        xYZ.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
        xYZ.Z += byEntity.LocalEyePos.Z;
        if (secondsUsed > 0.5f && (int)(30f * secondsUsed) % 7 == 1)
        {
            byEntity.World.SpawnCubeParticles(xYZ, spawnParticleStack ?? slot.Itemstack, 0.3f, 4, 0.5f, (byEntity as EntityPlayer)?.Player);
        }

        if (byEntity.World is IClientWorldAccessor)
        {
            return secondsUsed <= 1f;
        }

        return true;
    }

    //
    // Summary:
    //     Finished eating the contents in the slot, final call
    //
    // Parameters:
    //   secondsUsed:
    //
    //   slot:
    //
    //   byEntity:
    protected virtual void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        FoodNutritionProperties nutritionProperties = GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
        if (!(byEntity.World is IServerWorldAccessor) || nutritionProperties == null || !(secondsUsed >= 0.95f))
        {
            return;
        }

        float spoilState = UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
        float num = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
        float num2 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);
        byEntity.ReceiveSaturation(nutritionProperties.Satiety * num, nutritionProperties.FoodCategory);
        IPlayer player = null;
        if (byEntity is EntityPlayer)
        {
            player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
        }

        slot.TakeOut(1);
        if (nutritionProperties.EatenStack != null)
        {
            if (slot.Empty)
            {
                slot.Itemstack = nutritionProperties.EatenStack.ResolvedItemstack.Clone();
            }
            else if (player == null || !player.InventoryManager.TryGiveItemstack(nutritionProperties.EatenStack.ResolvedItemstack.Clone(), slotNotifyEffect: true))
            {
                byEntity.World.SpawnItemEntity(nutritionProperties.EatenStack.ResolvedItemstack.Clone(), byEntity.SidedPos.XYZ);
            }
        }

        float num3 = nutritionProperties.Health * num2;
        float @float = byEntity.WatchedAttributes.GetFloat("intoxication");
        byEntity.WatchedAttributes.SetFloat("intoxication", Math.Min(1.1f, @float + nutritionProperties.Intoxication));
        if (num3 != 0f)
        {
            byEntity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Internal,
                Type = ((num3 > 0f) ? EnumDamageType.Heal : EnumDamageType.Poison)
            }, Math.Abs(num3));
        }

        slot.MarkDirty();
        player.InventoryManager.BroadcastHotbarSlot();
    }

    //
    // Summary:
    //     Callback when the player dropped this item from his inventory. You can set handling
    //     to PreventDefault to prevent dropping this item. You can also check if the entityplayer
    //     of this player is dead to check if dropping of this item was due the players
    //     death
    //
    // Parameters:
    //   world:
    //
    //   byPlayer:
    //
    //   slot:
    //
    //   quantity:
    //     Amount of items the player wants to drop
    //
    //   handling:
    public virtual void OnHeldDropped(IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)
    {
    }

    //
    // Summary:
    //     Called by the inventory system when you hover over an item stack. This is the
    //     item stack name that is getting displayed.
    //
    // Parameters:
    //   itemStack:
    public virtual string GetHeldItemName(ItemStack itemStack)
    {
        if (Code == null)
        {
            return "Invalid block, id " + Id;
        }

        string text = ItemClass.Name();
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(Lang.GetMatching(Code?.Domain + ":" + text + "-" + Code?.Path));
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        for (int i = 0; i < collectibleBehaviors.Length; i++)
        {
            collectibleBehaviors[i].GetHeldItemName(stringBuilder, itemStack);
        }

        return stringBuilder.ToString();
    }

    //
    // Summary:
    //     Called by the inventory system when you hover over an item stack. This is the
    //     text that is getting displayed.
    //
    // Parameters:
    //   inSlot:
    //
    //   dsc:
    //
    //   world:
    //
    //   withDebugInfo:
    public virtual void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        ItemStack itemstack = inSlot.Itemstack;
        string itemDescText = GetItemDescText();
        if (withDebugInfo)
        {
            dsc.AppendLine("<font color=\"#bbbbbb\">Id:" + Id + "</font>");
            dsc.AppendLine(string.Concat("<font color=\"#bbbbbb\">Code: ", Code, "</font>"));
            ICoreAPI coreAPI = api;
            if (coreAPI != null && coreAPI.Side == EnumAppSide.Client && (api as ICoreClientAPI).Input.KeyboardKeyStateRaw[1])
            {
                dsc.AppendLine("<font color=\"#bbbbbb\">Attributes: " + inSlot.Itemstack.Attributes.ToJsonToken() + "</font>\n");
            }
        }

        int maxDurability = GetMaxDurability(itemstack);
        if (maxDurability > 1)
        {
            dsc.AppendLine(Lang.Get("Durability: {0} / {1}", itemstack.Collectible.GetRemainingDurability(itemstack), maxDurability));
        }

        if (MiningSpeed != null && MiningSpeed.Count > 0)
        {
            dsc.AppendLine(Lang.Get("Tool Tier: {0}", ToolTier));
            dsc.Append(Lang.Get("item-tooltip-miningspeed"));
            int num = 0;
            foreach (KeyValuePair<EnumBlockMaterial, float> item in MiningSpeed)
            {
                if (!((double)item.Value < 1.1))
                {
                    if (num > 0)
                    {
                        dsc.Append(", ");
                    }

                    dsc.Append(Lang.Get(item.Key.ToString()) + " " + item.Value.ToString("#.#") + "x");
                    num++;
                }
            }

            dsc.Append("\n");
        }

        IHeldBag collectibleInterface = GetCollectibleInterface<IHeldBag>();
        if (collectibleInterface != null)
        {
            dsc.AppendLine(Lang.Get("Storage Slots: {0}", collectibleInterface.GetQuantitySlots(itemstack)));
            bool flag = false;
            ItemStack[] contents = collectibleInterface.GetContents(itemstack, world);
            if (contents != null)
            {
                ItemStack[] array = contents;
                foreach (ItemStack itemStack in array)
                {
                    if (itemStack != null && itemStack.StackSize != 0)
                    {
                        if (!flag)
                        {
                            dsc.AppendLine(Lang.Get("Contents: "));
                            flag = true;
                        }

                        itemStack.ResolveBlockOrItem(world);
                        dsc.AppendLine("- " + itemStack.StackSize + "x " + itemStack.GetName());
                    }
                }

                if (!flag)
                {
                    dsc.AppendLine(Lang.Get("Empty"));
                }
            }
        }

        EntityPlayer entityPlayer = ((world.Side == EnumAppSide.Client) ? (world as IClientWorldAccessor).Player.Entity : null);
        float spoilState = AppendPerishableInfoText(inSlot, dsc, world);
        FoodNutritionProperties nutritionProperties = GetNutritionProperties(world, itemstack, entityPlayer);
        if (nutritionProperties != null)
        {
            float num2 = GlobalConstants.FoodSpoilageSatLossMul(spoilState, itemstack, entityPlayer);
            float num3 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, itemstack, entityPlayer);
            if (Math.Abs(nutritionProperties.Health * num3) > 0.001f)
            {
                dsc.AppendLine(Lang.Get((MatterState == EnumMatterState.Liquid) ? "liquid-when-drunk-saturation-hp" : "When eaten: {0} sat, {1} hp", Math.Round(nutritionProperties.Satiety * num2), Math.Round(nutritionProperties.Health * num3, 2)));
            }
            else
            {
                dsc.AppendLine(Lang.Get((MatterState == EnumMatterState.Liquid) ? "liquid-when-drunk-saturation" : "When eaten: {0} sat", Math.Round(nutritionProperties.Satiety * num2)));
            }

            dsc.AppendLine(Lang.Get("Food Category: {0}", Lang.Get("foodcategory-" + nutritionProperties.FoodCategory.ToString().ToLowerInvariant())));
        }

        if (GrindingProps?.GroundStack?.ResolvedItemstack != null)
        {
            dsc.AppendLine(Lang.Get("When ground: Turns into {0}x {1}", GrindingProps.GroundStack.ResolvedItemstack.StackSize, GrindingProps.GroundStack.ResolvedItemstack.GetName()));
        }

        if (CrushingProps != null)
        {
            float num4 = CrushingProps.Quantity.avg * (float)CrushingProps.CrushedStack.ResolvedItemstack.StackSize;
            dsc.AppendLine(Lang.Get("When pulverized: Turns into {0:0.#}x {1}", num4, CrushingProps.CrushedStack.ResolvedItemstack.GetName()));
            dsc.AppendLine(Lang.Get("Requires Pulverizer tier: {0}", CrushingProps.HardnessTier));
        }

        if (GetAttackPower(itemstack) > 0.5f)
        {
            dsc.AppendLine(Lang.Get("Attack power: -{0} hp", GetAttackPower(itemstack).ToString("0.#")));
            dsc.AppendLine(Lang.Get("Attack tier: {0}", ToolTier));
        }

        if (GetAttackRange(itemstack) > GlobalConstants.DefaultAttackRange)
        {
            dsc.AppendLine(Lang.Get("Attack range: {0} m", GetAttackRange(itemstack).ToString("0.#")));
        }

        if (CombustibleProps != null)
        {
            string text = CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
            if (text == "fire")
            {
                dsc.AppendLine(Lang.Get("itemdesc-fireinkiln"));
            }
            else
            {
                if (CombustibleProps.BurnTemperature > 0)
                {
                    dsc.AppendLine(Lang.Get("Burn temperature: {0}°C", CombustibleProps.BurnTemperature));
                    dsc.AppendLine(Lang.Get("Burn duration: {0}s", CombustibleProps.BurnDuration));
                }

                if (CombustibleProps.MeltingPoint > 0)
                {
                    dsc.AppendLine(Lang.Get("game:smeltpoint-" + text, CombustibleProps.MeltingPoint));
                }
            }

            if (CombustibleProps.SmeltedStack?.ResolvedItemstack != null)
            {
                int smeltedRatio = CombustibleProps.SmeltedRatio;
                int stackSize = CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize;
                string value = ((smeltedRatio == 1) ? Lang.Get("game:smeltdesc-" + text + "-singular", stackSize, CombustibleProps.SmeltedStack.ResolvedItemstack.GetName()) : Lang.Get("game:smeltdesc-" + text + "-plural", smeltedRatio, stackSize, CombustibleProps.SmeltedStack.ResolvedItemstack.GetName()));
                dsc.AppendLine(value);
            }
        }

        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        for (int i = 0; i < collectibleBehaviors.Length; i++)
        {
            collectibleBehaviors[i].GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        if (itemDescText.Length > 0 && dsc.Length > 0)
        {
            dsc.Append("\n");
        }

        dsc.Append(itemDescText);
        float temperature = GetTemperature(world, itemstack);
        if (temperature > 20f)
        {
            dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temperature));
        }

        if (Code != null && Code.Domain != "game")
        {
            Mod mod = api.ModLoader.GetMod(Code.Domain);
            dsc.AppendLine(Lang.Get("Mod: {0}", mod?.Info.Name ?? Code.Domain));
        }
    }

    public virtual string GetItemDescText()
    {
        string text = Code?.Domain + ":" + ItemClass.ToString().ToLowerInvariant() + "desc-" + Code?.Path;
        string matching = Lang.GetMatching(text);
        if (matching == text)
        {
            return "";
        }

        return matching + "\n";
    }

    //
    // Summary:
    //     Interaction help thats displayed above the hotbar, when the player puts this
    //     item/block in his active hand slot
    //
    // Parameters:
    //   inSlot:
    public virtual WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
    {
        WorldInteraction[] array = ((GetNutritionProperties(api.World, inSlot.Itemstack, null) == null) ? new WorldInteraction[0] : new WorldInteraction[1]
        {
            new WorldInteraction
            {
                ActionLangCode = "heldhelp-eat",
                MouseButton = EnumMouseButton.Right
            }
        });
        EnumHandling handling = EnumHandling.PassThrough;
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        for (int i = 0; i < collectibleBehaviors.Length; i++)
        {
            WorldInteraction[] heldInteractionHelp = collectibleBehaviors[i].GetHeldInteractionHelp(inSlot, ref handling);
            array = array.Append(heldInteractionHelp);
            if (handling == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }

        return array;
    }

    public virtual float AppendPerishableInfoText(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
    {
        float num = 0f;
        TransitionState[] array = UpdateAndGetTransitionStates(api.World, inSlot);
        bool flag = false;
        if (array == null)
        {
            return 0f;
        }

        for (int i = 0; i < array.Length; i++)
        {
            num = Math.Max(num, AppendPerishableInfoText(inSlot, dsc, world, array[i], flag));
            flag = flag || num > 0f;
        }

        return num;
    }

    protected virtual float AppendPerishableInfoText(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, TransitionState state, bool nowSpoiling)
    {
        TransitionableProperties props = state.Props;
        float num = GetTransitionRateMul(world, inSlot, props.Type);
        if (inSlot.Inventory is CreativeInventoryTab)
        {
            num = 1f;
        }

        float transitionLevel = state.TransitionLevel;
        float num2 = state.FreshHoursLeft / num;
        switch (props.Type)
        {
            case EnumTransitionType.Perish:
                {
                    if (transitionLevel > 0f)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-perishable-spoiling", (int)Math.Round(transitionLevel * 100f)));
                        return transitionLevel;
                    }

                    if (num <= 0f)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-perishable"));
                        break;
                    }

                    float hoursPerDay = api.World.Calendar.HoursPerDay;
                    float num7 = num2 / hoursPerDay / (float)api.World.Calendar.DaysPerYear;
                    if (num7 >= 1f)
                    {
                        if (num7 <= 1.05f)
                        {
                            dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-one-year"));
                            break;
                        }

                        dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-years", Math.Round(num7, 1)));
                    }
                    else if (num2 > hoursPerDay)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-days", Math.Round(num2 / hoursPerDay, 1)));
                    }
                    else
                    {
                        dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-hours", Math.Round(num2, 1)));
                    }

                    break;
                }
            case EnumTransitionType.Cure:
                {
                    if (nowSpoiling)
                    {
                        break;
                    }

                    if (transitionLevel > 0f || (num2 <= 0f && num > 0f))
                    {
                        dsc.AppendLine(Lang.Get("itemstack-curable-curing", (int)Math.Round(transitionLevel * 100f)));
                        break;
                    }

                    double num8 = api.World.Calendar.HoursPerDay;
                    if (num <= 0f)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-curable"));
                    }
                    else if ((double)num2 > num8)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-curable-duration-days", Math.Round((double)num2 / num8, 1)));
                    }
                    else
                    {
                        dsc.AppendLine(Lang.Get("itemstack-curable-duration-hours", Math.Round(num2, 1)));
                    }

                    break;
                }
            case EnumTransitionType.Ripen:
                {
                    if (nowSpoiling)
                    {
                        break;
                    }

                    if (transitionLevel > 0f || (num2 <= 0f && num > 0f))
                    {
                        dsc.AppendLine(Lang.Get("itemstack-ripenable-ripening", (int)Math.Round(transitionLevel * 100f)));
                        break;
                    }

                    double num4 = api.World.Calendar.HoursPerDay;
                    if (num <= 0f)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-ripenable"));
                    }
                    else if ((double)num2 > num4)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-ripenable-duration-days", Math.Round((double)num2 / num4, 1)));
                    }
                    else
                    {
                        dsc.AppendLine(Lang.Get("itemstack-ripenable-duration-hours", Math.Round(num2, 1)));
                    }

                    break;
                }
            case EnumTransitionType.Dry:
                {
                    if (nowSpoiling)
                    {
                        break;
                    }

                    if (transitionLevel > 0f)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-dryable-dried", (int)Math.Round(transitionLevel * 100f)));
                        dsc.AppendLine(Lang.Get("Drying rate in this container: {0:0.##}x", num));
                        break;
                    }

                    double num5 = api.World.Calendar.HoursPerDay;
                    if (num <= 0f)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-dryable"));
                    }
                    else if ((double)num2 > num5)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-dryable-duration-days", Math.Round((double)num2 / num5, 1)));
                    }
                    else
                    {
                        dsc.AppendLine(Lang.Get("itemstack-dryable-duration-hours", Math.Round(num2, 1)));
                    }

                    break;
                }
            case EnumTransitionType.Melt:
                {
                    if (nowSpoiling)
                    {
                        break;
                    }

                    if (transitionLevel > 0f || num2 <= 0f)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-meltable-melted", (int)Math.Round(transitionLevel * 100f)));
                        dsc.AppendLine(Lang.Get("Melting rate in this container: {0:0.##}x", num));
                        break;
                    }

                    double num6 = api.World.Calendar.HoursPerDay;
                    if (num <= 0f)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-meltable"));
                    }
                    else if ((double)num2 > num6)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-meltable-duration-days", Math.Round((double)num2 / num6, 1)));
                    }
                    else
                    {
                        dsc.AppendLine(Lang.Get("itemstack-meltable-duration-hours", Math.Round(num2, 1)));
                    }

                    break;
                }
            case EnumTransitionType.Harden:
                {
                    if (nowSpoiling)
                    {
                        break;
                    }

                    if (transitionLevel > 0f || num2 <= 0f)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-hardenable-hardened", (int)Math.Round(transitionLevel * 100f)));
                        break;
                    }

                    double num3 = api.World.Calendar.HoursPerDay;
                    if (num <= 0f)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-hardenable"));
                    }
                    else if ((double)num2 > num3)
                    {
                        dsc.AppendLine(Lang.Get("itemstack-hardenable-duration-days", Math.Round((double)num2 / num3, 1)));
                    }
                    else
                    {
                        dsc.AppendLine(Lang.Get("itemstack-hardenable-duration-hours", Math.Round(num2, 1)));
                    }

                    break;
                }
        }

        return 0f;
    }

    public virtual void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe recipe, ItemSlot slot, double x, double y, double z, double size)
    {
        capi.Render.RenderItemstackToGui(slot, x, y, z, (float)size * 0.58f, -1);
    }

    public virtual List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
    {
        if (Code == null)
        {
            return null;
        }

        JsonObject jsonObject = Attributes?["handbook"];
        if (jsonObject != null && jsonObject["exclude"].AsBool())
        {
            return null;
        }

        bool num = CreativeInventoryTabs != null && CreativeInventoryTabs.Length != 0;
        bool flag = CreativeInventoryStacks != null && CreativeInventoryStacks.Length != 0;
        if (!num && !flag && !(jsonObject?["include"].AsBool()).GetValueOrDefault())
        {
            return null;
        }

        List<ItemStack> list = new List<ItemStack>();
        if (flag && (jsonObject == null || !jsonObject["ignoreCreativeInvStacks"].AsBool()))
        {
            for (int i = 0; i < CreativeInventoryStacks.Length; i++)
            {
                JsonItemStack[] stacks = CreativeInventoryStacks[i].Stacks;
                for (int j = 0; j < stacks.Length; j++)
                {
                    ItemStack stack2 = stacks[j].ResolvedItemstack;
                    stack2.ResolveBlockOrItem(capi.World);
                    stack2 = stack2.Clone();
                    stack2.StackSize = stack2.Collectible.MaxStackSize;
                    if (!list.Any((ItemStack stack1) => stack1.Equals(stack2)))
                    {
                        list.Add(stack2);
                    }
                }
            }
        }
        else
        {
            ItemStack item = new ItemStack(this);
            list.Add(item);
        }

        return list;
    }

    //
    // Summary:
    //     Should return true if the stack can be placed into given slot
    //
    // Parameters:
    //   stack:
    //
    //   slot:
    public virtual bool CanBePlacedInto(ItemStack stack, ItemSlot slot)
    {
        if (slot.StorageType != 0)
        {
            return (slot.StorageType & GetStorageFlags(stack)) > (EnumItemStorageFlags)0;
        }

        return true;
    }

    //
    // Summary:
    //     Should return the max. number of items that can be placed from sourceStack into
    //     the sinkStack
    //
    // Parameters:
    //   sinkStack:
    //
    //   sourceStack:
    //
    //   priority:
    public virtual int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
    {
        if (Equals(sinkStack, sourceStack, GlobalConstants.IgnoredStackAttributes) && sinkStack.StackSize < MaxStackSize)
        {
            return Math.Min(MaxStackSize - sinkStack.StackSize, sourceStack.StackSize);
        }

        return 0;
    }

    //
    // Summary:
    //     Is always called on the sink slots item
    //
    // Parameters:
    //   op:
    public virtual void TryMergeStacks(ItemStackMergeOperation op)
    {
        op.MovableQuantity = GetMergableQuantity(op.SinkSlot.Itemstack, op.SourceSlot.Itemstack, op.CurrentPriority);
        if (op.MovableQuantity == 0 || !op.SinkSlot.CanTakeFrom(op.SourceSlot, op.CurrentPriority))
        {
            return;
        }

        bool flag = false;
        bool flag2 = false;
        op.MovedQuantity = GameMath.Min(op.SinkSlot.GetRemainingSlotSpace(op.SourceSlot.Itemstack), op.MovableQuantity, op.RequestedQuantity);
        if (HasTemperature(op.SinkSlot.Itemstack) || HasTemperature(op.SourceSlot.Itemstack))
        {
            if (op.CurrentPriority < EnumMergePriority.DirectMerge && Math.Abs(GetTemperature(op.World, op.SinkSlot.Itemstack) - GetTemperature(op.World, op.SourceSlot.Itemstack)) > 30f)
            {
                op.MovedQuantity = 0;
                op.MovableQuantity = 0;
                op.RequiredPriority = EnumMergePriority.DirectMerge;
                return;
            }

            flag = true;
        }

        TransitionState[] array = UpdateAndGetTransitionStates(op.World, op.SourceSlot);
        TransitionState[] array2 = UpdateAndGetTransitionStates(op.World, op.SinkSlot);
        Dictionary<EnumTransitionType, TransitionState> dictionary = null;
        if (array != null)
        {
            bool flag3 = true;
            bool flag4 = true;
            if (array2 == null)
            {
                op.MovedQuantity = 0;
                op.MovableQuantity = 0;
                return;
            }

            dictionary = new Dictionary<EnumTransitionType, TransitionState>();
            TransitionState[] array3 = array2;
            foreach (TransitionState transitionState in array3)
            {
                dictionary[transitionState.Props.Type] = transitionState;
            }

            array3 = array;
            foreach (TransitionState transitionState2 in array3)
            {
                TransitionState value = null;
                if (!dictionary.TryGetValue(transitionState2.Props.Type, out value))
                {
                    flag4 = false;
                    flag3 = false;
                    break;
                }

                if (Math.Abs(value.TransitionedHours - transitionState2.TransitionedHours) > 4f && Math.Abs(value.TransitionedHours - transitionState2.TransitionedHours) / transitionState2.FreshHours > 0.03f)
                {
                    flag4 = false;
                }
            }

            if (!flag4 && op.CurrentPriority < EnumMergePriority.DirectMerge)
            {
                op.MovedQuantity = 0;
                op.MovableQuantity = 0;
                op.RequiredPriority = EnumMergePriority.DirectMerge;
                return;
            }

            if (!flag3)
            {
                op.MovedQuantity = 0;
                op.MovableQuantity = 0;
                return;
            }

            flag2 = true;
        }

        if (op.SourceSlot.Itemstack == null)
        {
            op.MovedQuantity = 0;
        }
        else
        {
            if (op.MovedQuantity <= 0)
            {
                return;
            }

            if (op.SinkSlot.Itemstack == null)
            {
                op.SinkSlot.Itemstack = new ItemStack(op.SourceSlot.Itemstack.Collectible, 0);
            }

            if (flag)
            {
                SetTemperature(op.World, op.SinkSlot.Itemstack, ((float)op.SinkSlot.StackSize * GetTemperature(op.World, op.SinkSlot.Itemstack) + (float)op.MovedQuantity * GetTemperature(op.World, op.SourceSlot.Itemstack)) / (float)(op.SinkSlot.StackSize + op.MovedQuantity));
            }

            if (flag2)
            {
                float num = (float)op.MovedQuantity / (float)(op.MovedQuantity + op.SinkSlot.StackSize);
                TransitionState[] array3 = array;
                foreach (TransitionState transitionState3 in array3)
                {
                    TransitionState transitionState4 = dictionary[transitionState3.Props.Type];
                    SetTransitionState(op.SinkSlot.Itemstack, transitionState3.Props.Type, transitionState3.TransitionedHours * num + transitionState4.TransitionedHours * (1f - num));
                }
            }

            op.SinkSlot.Itemstack.StackSize += op.MovedQuantity;
            op.SourceSlot.Itemstack.StackSize -= op.MovedQuantity;
            if (op.SourceSlot.Itemstack.StackSize <= 0)
            {
                op.SourceSlot.Itemstack = null;
            }
        }
    }

    //
    // Summary:
    //     If the item is smeltable, this is the time it takes to smelt at smelting point
    //
    //
    // Parameters:
    //   world:
    //
    //   cookingSlotsProvider:
    //
    //   inputSlot:
    public virtual float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
    {
        if (CombustibleProps != null)
        {
            return CombustibleProps.MeltingDuration;
        }

        return 0f;
    }

    //
    // Summary:
    //     If the item is smeltable, this is its melting point
    //
    // Parameters:
    //   world:
    //
    //   cookingSlotsProvider:
    //
    //   inputSlot:
    public virtual float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
    {
        return (CombustibleProps != null) ? CombustibleProps.MeltingPoint : 0;
    }

    //
    // Summary:
    //     Should return true if this collectible is smeltable in an open fire
    //
    // Parameters:
    //   world:
    //
    //   cookingSlotsProvider:
    //
    //   inputStack:
    //
    //   outputStack:
    public virtual bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
    {
        ItemStack itemStack = CombustibleProps?.SmeltedStack?.ResolvedItemstack;
        if (itemStack != null && inputStack.StackSize >= CombustibleProps.SmeltedRatio && CombustibleProps.MeltingPoint > 0 && (CombustibleProps.SmeltingType != EnumSmeltType.Fire || world.Config.GetString("allowOpenFireFiring").ToBool()))
        {
            if (outputStack != null)
            {
                return outputStack.Collectible.GetMergableQuantity(outputStack, itemStack, EnumMergePriority.AutoMerge) >= itemStack.StackSize;
            }

            return true;
        }

        return false;
    }

    //
    // Summary:
    //     Transform the item to it's smelted variant
    //
    // Parameters:
    //   world:
    //
    //   cookingSlotsProvider:
    //
    //   inputSlot:
    //
    //   outputSlot:
    public virtual void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
    {
        if (CanSmelt(world, cookingSlotsProvider, inputSlot.Itemstack, outputSlot.Itemstack))
        {
            ItemStack itemStack = CombustibleProps.SmeltedStack.ResolvedItemstack.Clone();
            TransitionState transitionState = UpdateAndGetTransitionState(world, new DummySlot(inputSlot.Itemstack), EnumTransitionType.Perish);
            if (transitionState != null)
            {
                TransitionState transitionState2 = itemStack.Collectible.UpdateAndGetTransitionState(world, new DummySlot(itemStack), EnumTransitionType.Perish);
                float val = transitionState.TransitionedHours / (transitionState.TransitionHours + transitionState.FreshHours) * 0.8f * (transitionState2.TransitionHours + transitionState2.FreshHours) - 1f;
                itemStack.Collectible.SetTransitionState(itemStack, EnumTransitionType.Perish, Math.Max(0f, val));
            }

            int num = 1;
            if (outputSlot.Itemstack == null)
            {
                outputSlot.Itemstack = itemStack;
                outputSlot.Itemstack.StackSize = num * itemStack.StackSize;
            }
            else
            {
                itemStack.StackSize = num * itemStack.StackSize;
                ItemStackMergeOperation itemStackMergeOperation = new ItemStackMergeOperation(world, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.ConfirmedMerge, num * itemStack.StackSize);
                itemStackMergeOperation.SourceSlot = new DummySlot(itemStack);
                itemStackMergeOperation.SinkSlot = new DummySlot(outputSlot.Itemstack);
                outputSlot.Itemstack.Collectible.TryMergeStacks(itemStackMergeOperation);
                outputSlot.Itemstack = itemStackMergeOperation.SinkSlot.Itemstack;
            }

            inputSlot.Itemstack.StackSize -= num * CombustibleProps.SmeltedRatio;
            if (inputSlot.Itemstack.StackSize <= 0)
            {
                inputSlot.Itemstack = null;
            }

            outputSlot.MarkDirty();
        }
    }

    //
    // Summary:
    //     Returns true if the stack can spoil
    //
    // Parameters:
    //   itemstack:
    public virtual bool CanSpoil(ItemStack itemstack)
    {
        if (itemstack == null || itemstack.Attributes == null)
        {
            return false;
        }

        if (itemstack.Collectible.NutritionProps != null)
        {
            return itemstack.Attributes.HasAttribute("spoilstate");
        }

        return false;
    }

    //
    // Summary:
    //     Returns the transition state of given transition type
    //
    // Parameters:
    //   world:
    //
    //   inslot:
    //
    //   type:
    public virtual TransitionState UpdateAndGetTransitionState(IWorldAccessor world, ItemSlot inslot, EnumTransitionType type)
    {
        TransitionState[] array = UpdateAndGetTransitionStates(world, inslot);
        if (array == null)
        {
            return null;
        }

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].Props.Type == type)
            {
                return array[i];
            }
        }

        return null;
    }

    public virtual void SetTransitionState(ItemStack stack, EnumTransitionType type, float transitionedHours)
    {
        ITreeAttribute treeAttribute = (ITreeAttribute)stack.Attributes["transitionstate"];
        if (treeAttribute == null)
        {
            UpdateAndGetTransitionState(api.World, new DummySlot(stack), type);
            treeAttribute = (ITreeAttribute)stack.Attributes["transitionstate"];
        }

        TransitionableProperties[] transitionableProperties = GetTransitionableProperties(api.World, stack, null);
        for (int i = 0; i < transitionableProperties.Length; i++)
        {
            if (transitionableProperties[i].Type == type)
            {
                (treeAttribute["transitionedHours"] as FloatArrayAttribute).value[i] = transitionedHours;
                break;
            }
        }
    }

    public virtual float GetTransitionRateMul(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
    {
        float num = ((inSlot.Inventory == null) ? 1f : inSlot.Inventory.GetTransitionSpeedMul(transType, inSlot.Itemstack));
        if (transType == EnumTransitionType.Perish)
        {
            if (inSlot.Itemstack.Collectible.GetTemperature(world, inSlot.Itemstack) > 75f)
            {
                num = 0f;
            }

            num *= GlobalConstants.PerishSpeedModifier;
        }

        return num;
    }

    //
    // Summary:
    //     Returns a list of the current transition states of this item, redirects to UpdateAndGetTransitionStatesNative
    //
    //
    // Parameters:
    //   world:
    //
    //   inslot:
    public virtual TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
    {
        return UpdateAndGetTransitionStatesNative(world, inslot);
    }

    //
    // Summary:
    //     Returns a list of the current transition states of this item. Seperate from UpdateAndGetTransitionStates()
    //     so that you can call still call this methods several inheritances down, i.e.
    //     there is no base.base.Method() syntax in C#
    //
    // Parameters:
    //   world:
    //
    //   inslot:
    protected virtual TransitionState[] UpdateAndGetTransitionStatesNative(IWorldAccessor world, ItemSlot inslot)
    {
        if (inslot is ItemSlotCreative)
        {
            return null;
        }

        ItemStack itemstack = inslot.Itemstack;
        TransitionableProperties[] transitionableProperties = GetTransitionableProperties(world, inslot.Itemstack, null);
        if (itemstack == null || transitionableProperties == null || transitionableProperties.Length == 0)
        {
            return null;
        }

        if (itemstack.Attributes == null)
        {
            itemstack.Attributes = new TreeAttribute();
        }

        if (itemstack.Attributes.GetBool("timeFrozen"))
        {
            return null;
        }

        if (!(itemstack.Attributes["transitionstate"] is ITreeAttribute))
        {
            itemstack.Attributes["transitionstate"] = new TreeAttribute();
        }

        ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["transitionstate"];
        TransitionState[] array = new TransitionState[transitionableProperties.Length];
        float[] array2;
        float[] array3;
        float[] array4;
        if (!treeAttribute.HasAttribute("createdTotalHours"))
        {
            treeAttribute.SetDouble("createdTotalHours", world.Calendar.TotalHours);
            treeAttribute.SetDouble("lastUpdatedTotalHours", world.Calendar.TotalHours);
            array2 = new float[transitionableProperties.Length];
            array3 = new float[transitionableProperties.Length];
            array4 = new float[transitionableProperties.Length];
            for (int i = 0; i < transitionableProperties.Length; i++)
            {
                array4[i] = 0f;
                array2[i] = transitionableProperties[i].FreshHours.nextFloat(1f, world.Rand);
                array3[i] = transitionableProperties[i].TransitionHours.nextFloat(1f, world.Rand);
            }

            treeAttribute["freshHours"] = new FloatArrayAttribute(array2);
            treeAttribute["transitionHours"] = new FloatArrayAttribute(array3);
            treeAttribute["transitionedHours"] = new FloatArrayAttribute(array4);
        }
        else
        {
            array2 = (treeAttribute["freshHours"] as FloatArrayAttribute).value;
            array3 = (treeAttribute["transitionHours"] as FloatArrayAttribute).value;
            array4 = (treeAttribute["transitionedHours"] as FloatArrayAttribute).value;
            if (transitionableProperties.Length - array2.Length > 0)
            {
                for (int j = array2.Length; j < transitionableProperties.Length; j++)
                {
                    array2 = array2.Append(transitionableProperties[j].FreshHours.nextFloat(1f, world.Rand));
                    array3 = array3.Append(transitionableProperties[j].TransitionHours.nextFloat(1f, world.Rand));
                    array4 = array4.Append(0f);
                }

                (treeAttribute["freshHours"] as FloatArrayAttribute).value = array2;
                (treeAttribute["transitionHours"] as FloatArrayAttribute).value = array3;
                (treeAttribute["transitionedHours"] as FloatArrayAttribute).value = array4;
            }
        }

        double @double = treeAttribute.GetDouble("lastUpdatedTotalHours");
        double totalHours = world.Calendar.TotalHours;
        bool flag = false;
        float num = (float)(totalHours - @double);
        for (int k = 0; k < transitionableProperties.Length; k++)
        {
            TransitionableProperties transitionableProperties2 = transitionableProperties[k];
            if (transitionableProperties2 == null)
            {
                continue;
            }

            float transitionRateMul = GetTransitionRateMul(world, inslot, transitionableProperties2.Type);
            if (num > 0.05f)
            {
                float num2 = num * transitionRateMul;
                array4[k] += num2;
            }

            float freshHoursLeft = Math.Max(0f, array2[k] - array4[k]);
            float num3 = Math.Max(0f, array4[k] - array2[k]) / array3[k];
            if (num3 > 0f)
            {
                if (transitionableProperties2.Type == EnumTransitionType.Perish)
                {
                    flag = true;
                }
                else if (flag)
                {
                    continue;
                }
            }

            if (num3 >= 1f && world.Side == EnumAppSide.Server)
            {
                ItemStack itemStack = OnTransitionNow(inslot, itemstack.Collectible.TransitionableProps[k]);
                if (itemStack.StackSize <= 0)
                {
                    inslot.Itemstack = null;
                }
                else
                {
                    itemstack.SetFrom(itemStack);
                }

                inslot.MarkDirty();
                break;
            }

            array[k] = new TransitionState
            {
                FreshHoursLeft = freshHoursLeft,
                TransitionLevel = Math.Min(1f, num3),
                TransitionedHours = array4[k],
                TransitionHours = array3[k],
                FreshHours = array2[k],
                Props = transitionableProperties2
            };
        }

        if (num > 0.05f)
        {
            treeAttribute.SetDouble("lastUpdatedTotalHours", totalHours);
        }

        return (from s in array
                where s != null
                orderby (int)s.Props.Type
                select s).ToArray();
    }

    //
    // Summary:
    //     Called when any of its TransitionableProperties causes the stack to transition
    //     to another stack. Default behavior is to return props.TransitionedStack.ResolvedItemstack
    //     and set the stack size according to the transition rtio
    //
    // Parameters:
    //   slot:
    //
    //   props:
    //
    // Returns:
    //     The stack it should transition into
    public virtual ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props)
    {
        ItemStack itemStack = props.TransitionedStack.ResolvedItemstack.Clone();
        itemStack.StackSize = GameMath.RoundRandom(api.World.Rand, (float)slot.Itemstack.StackSize * props.TransitionRatio);
        return itemStack;
    }

    public static void CarryOverFreshness(ICoreAPI api, ItemSlot inputSlot, ItemStack outputStack, TransitionableProperties perishProps)
    {
        CarryOverFreshness(api, new ItemSlot[1] { inputSlot }, new ItemStack[1] { outputStack }, perishProps);
    }

    public static void CarryOverFreshness(ICoreAPI api, ItemSlot[] inputSlots, ItemStack[] outStacks, TransitionableProperties perishProps)
    {
        float num = 0f;
        float num2 = 0f;
        float num3 = 0f;
        int num4 = 0;
        foreach (ItemSlot itemSlot in inputSlots)
        {
            if (!itemSlot.Empty)
            {
                TransitionState transitionState = itemSlot.Itemstack?.Collectible?.UpdateAndGetTransitionState(api.World, itemSlot, EnumTransitionType.Perish);
                if (transitionState != null)
                {
                    num4++;
                    float num5 = transitionState.TransitionedHours / (transitionState.TransitionHours + transitionState.FreshHours);
                    float num6 = Math.Max(0f, (transitionState.TransitionedHours - transitionState.FreshHours) / transitionState.TransitionHours);
                    num2 = Math.Max(num6, num2);
                    num += num5;
                    num3 += num6;
                }
            }
        }

        num /= (float)Math.Max(1, num4);
        num3 /= (float)Math.Max(1, num4);
        for (int j = 0; j < outStacks.Length; j++)
        {
            if (outStacks[j] != null)
            {
                if (!(outStacks[j].Attributes["transitionstate"] is ITreeAttribute))
                {
                    outStacks[j].Attributes["transitionstate"] = new TreeAttribute();
                }

                float num7 = perishProps.TransitionHours.nextFloat(1f, api.World.Rand);
                float num8 = perishProps.FreshHours.nextFloat(1f, api.World.Rand);
                ITreeAttribute treeAttribute = (ITreeAttribute)outStacks[j].Attributes["transitionstate"];
                treeAttribute.SetDouble("createdTotalHours", api.World.Calendar.TotalHours);
                treeAttribute.SetDouble("lastUpdatedTotalHours", api.World.Calendar.TotalHours);
                treeAttribute["freshHours"] = new FloatArrayAttribute(new float[1] { num8 });
                treeAttribute["transitionHours"] = new FloatArrayAttribute(new float[1] { num7 });
                if (num3 > 0f)
                {
                    num3 *= 0.6f;
                    treeAttribute["transitionedHours"] = new FloatArrayAttribute(new float[1] { num8 + Math.Max(0f, num7 * num3 - 2f) });
                }
                else
                {
                    treeAttribute["transitionedHours"] = new FloatArrayAttribute(new float[1] { Math.Max(0f, num * (0.8f + (float)(2 + num4) * num2) * (num7 + num8)) });
                }
            }
        }
    }

    //
    // Summary:
    //     Test is failed for Perish-able items which have less than 50% of their fresh
    //     state remaining (or are already starting to spoil)
    //
    // Parameters:
    //   world:
    //
    //   itemstack:
    public virtual bool IsReasonablyFresh(IWorldAccessor world, ItemStack itemstack)
    {
        if (GetMaxDurability(itemstack) > 1 && (float)GetRemainingDurability(itemstack) / (float)GetMaxDurability(itemstack) < 0.95f)
        {
            return false;
        }

        if (itemstack == null)
        {
            return true;
        }

        TransitionableProperties[] transitionableProperties = GetTransitionableProperties(world, itemstack, null);
        if (transitionableProperties == null)
        {
            return true;
        }

        ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["transitionstate"];
        if (treeAttribute == null)
        {
            return true;
        }

        float[] value = (treeAttribute["freshHours"] as FloatArrayAttribute).value;
        float[] value2 = (treeAttribute["transitionedHours"] as FloatArrayAttribute).value;
        for (int i = 0; i < transitionableProperties.Length; i++)
        {
            TransitionableProperties obj = transitionableProperties[i];
            if (obj != null && obj.Type == EnumTransitionType.Perish && value2[i] > value[i] / 2f)
            {
                return false;
            }
        }

        return true;
    }

    //
    // Summary:
    //     Returns true if the stack has a temperature attribute
    //
    // Parameters:
    //   itemstack:
    public virtual bool HasTemperature(IItemStack itemstack)
    {
        if (itemstack == null || itemstack.Attributes == null)
        {
            return false;
        }

        return itemstack.Attributes.HasAttribute("temperature");
    }

    //
    // Summary:
    //     Returns the stacks item temperature in degree celsius
    //
    // Parameters:
    //   world:
    //
    //   itemstack:
    //
    //   didReceiveHeat:
    //     The amount of time it did receive heat since last update/call to this methode
    public virtual float GetTemperature(IWorldAccessor world, ItemStack itemstack, double didReceiveHeat)
    {
        if (!(itemstack?.Attributes?["temperature"] is ITreeAttribute))
        {
            return 20f;
        }

        ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["temperature"];
        double totalHours = world.Calendar.TotalHours;
        double @double = treeAttribute.GetDouble("temperatureLastUpdate");
        double num = totalHours - (@double + didReceiveHeat);
        float num2 = treeAttribute.GetFloat("temperature", 20f);
        if (num > 0.0117647061124444 && num2 > 0f)
        {
            num2 = Math.Max(0f, num2 - Math.Max(0f, (float)(totalHours - @double) * treeAttribute.GetFloat("cooldownSpeed", 90f)));
            treeAttribute.SetFloat("temperature", num2);
        }

        treeAttribute.SetDouble("temperatureLastUpdate", totalHours);
        return num2;
    }

    //
    // Summary:
    //     Returns the stacks item temperature in degree celsius
    //
    // Parameters:
    //   world:
    //
    //   itemstack:
    public virtual float GetTemperature(IWorldAccessor world, ItemStack itemstack)
    {
        if (!(itemstack?.Attributes?["temperature"] is ITreeAttribute))
        {
            return 20f;
        }

        ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["temperature"];
        double totalHours = world.Calendar.TotalHours;
        double @decimal = treeAttribute.GetDecimal("temperatureLastUpdate");
        double num = totalHours - @decimal;
        float num2 = (float)treeAttribute.GetDecimal("temperature", 20.0);
        if (itemstack.Attributes.GetBool("timeFrozen"))
        {
            return num2;
        }

        if (num > 0.0117647061124444 && num2 > 0f)
        {
            num2 = Math.Max(0f, num2 - Math.Max(0f, (float)(totalHours - @decimal) * treeAttribute.GetFloat("cooldownSpeed", 90f)));
            treeAttribute.SetFloat("temperature", num2);
            treeAttribute.SetDouble("temperatureLastUpdate", totalHours);
        }

        return num2;
    }

    //
    // Summary:
    //     Sets the stacks item temperature in degree celsius
    //
    // Parameters:
    //   world:
    //
    //   itemstack:
    //
    //   temperature:
    //
    //   delayCooldown:
    public virtual void SetTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown = true)
    {
        if (itemstack != null)
        {
            ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["temperature"];
            if (treeAttribute == null)
            {
                treeAttribute = (ITreeAttribute)(itemstack.Attributes["temperature"] = new TreeAttribute());
            }

            double num = world.Calendar.TotalHours;
            if (delayCooldown && treeAttribute.GetDecimal("temperature") < (double)temperature)
            {
                num += 0.5;
            }

            treeAttribute.SetDouble("temperatureLastUpdate", num);
            treeAttribute.SetFloat("temperature", temperature);
        }
    }

    //
    // Summary:
    //     Should return true if given stacks are equal, ignoring their stack size.
    //
    // Parameters:
    //   thisStack:
    //
    //   otherStack:
    //
    //   ignoreAttributeSubTrees:
    public virtual bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        if (thisStack.Class == otherStack.Class && thisStack.Id == otherStack.Id)
        {
            return thisStack.Attributes.Equals(api.World, otherStack.Attributes, ignoreAttributeSubTrees);
        }

        return false;
    }

    //
    // Summary:
    //     Should return true if thisStack is a satisfactory replacement of otherStack.
    //     It's bascially an Equals() test, but it ignores any additional attributes that
    //     exist in otherStack
    //
    // Parameters:
    //   thisStack:
    //
    //   otherStack:
    public virtual bool Satisfies(ItemStack thisStack, ItemStack otherStack)
    {
        if (thisStack.Class == otherStack.Class && thisStack.Id == otherStack.Id)
        {
            return thisStack.Attributes.IsSubSetOf(api.World, otherStack.Attributes);
        }

        return false;
    }

    //
    // Summary:
    //     This method is for example called by chests when they are being exported as part
    //     of a block schematic. Has to store all the currents block/item id mappings so
    //     it can be correctly imported again. By default it puts itself into the mapping
    //     and searches the itemstack attributes for attributes of type ItemStackAttribute
    //     and adds those to the mapping as well.
    //
    // Parameters:
    //   world:
    //
    //   inSlot:
    //
    //   blockIdMapping:
    //
    //   itemIdMapping:
    public virtual void OnStoreCollectibleMappings(IWorldAccessor world, ItemSlot inSlot, Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
        if (this is Item)
        {
            itemIdMapping[Id] = Code;
        }
        else
        {
            blockIdMapping[Id] = Code;
        }

        OnStoreCollectibleMappings(world, inSlot.Itemstack.Attributes, blockIdMapping, itemIdMapping);
        ITreeAttribute obj = inSlot.Itemstack.Attributes["temperature"] as ITreeAttribute;
        if (obj != null && obj.HasAttribute("temperatureLastUpdate"))
        {
            GetTemperature(world, inSlot.Itemstack);
        }
    }

    //
    // Summary:
    //     This method is called after a block/item like this has been imported as part
    //     of a block schematic. Has to restore fix the block/item id mappings as they are
    //     probably different compared to the world from where they were exported. By default
    //     iterates over all the itemstacks attributes and searches for attribute sof type
    //     ItenStackAttribute and calls .FixMapping() on them.
    //
    // Parameters:
    //   worldForResolve:
    //
    //   inSlot:
    //
    //   oldBlockIdMapping:
    //
    //   oldItemIdMapping:
    [Obsolete("Use the variant with resolveImports parameter")]
    public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ItemSlot inSlot, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping)
    {
        OnLoadCollectibleMappings(worldForResolve, inSlot, oldBlockIdMapping, oldItemIdMapping, resolveImports: true);
    }

    //
    // Summary:
    //     This method is called after a block/item like this has been imported as part
    //     of a block schematic. Has to restore fix the block/item id mappings as they are
    //     probably different compared to the world from where they were exported. By default
    //     iterates over all the itemstacks attributes and searches for attribute sof type
    //     ItenStackAttribute and calls .FixMapping() on them.
    //
    // Parameters:
    //   worldForResolve:
    //
    //   inSlot:
    //
    //   oldBlockIdMapping:
    //
    //   oldItemIdMapping:
    //
    //   resolveImports:
    //     Turn it off to spawn structures as they are. For example, in this mode, instead
    //     of traders, their meta spawners will spawn
    public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ItemSlot inSlot, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, bool resolveImports)
    {
        OnLoadCollectibleMappings(worldForResolve, inSlot.Itemstack.Attributes, oldBlockIdMapping, oldItemIdMapping);
    }

    private void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ITreeAttribute tree, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping)
    {
        foreach (KeyValuePair<string, IAttribute> item in tree)
        {
            if (item.Value is ITreeAttribute tree2)
            {
                OnLoadCollectibleMappings(worldForResolve, tree2, oldBlockIdMapping, oldItemIdMapping);
            }
            else if (item.Value is ItemstackAttribute { value: var value } itemstackAttribute)
            {
                if (value != null && !value.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
                {
                    itemstackAttribute.value = null;
                }
                else
                {
                    value?.Collectible.OnLoadCollectibleMappings(worldForResolve, value.Attributes, oldBlockIdMapping, oldItemIdMapping);
                }
            }
        }

        if (tree.HasAttribute("temperatureLastUpdate"))
        {
            tree.SetDouble("temperatureLastUpdate", worldForResolve.Calendar.TotalHours);
        }

        if (tree.HasAttribute("createdTotalHours"))
        {
            double @double = tree.GetDouble("createdTotalHours");
            double num = tree.GetDouble("lastUpdatedTotalHours") - @double;
            tree.SetDouble("lastUpdatedTotalHours", worldForResolve.Calendar.TotalHours);
            tree.SetDouble("createdTotalHours", worldForResolve.Calendar.TotalHours - num);
        }
    }

    private void OnStoreCollectibleMappings(IWorldAccessor world, ITreeAttribute tree, Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
        foreach (KeyValuePair<string, IAttribute> item in tree)
        {
            if (item.Value is ITreeAttribute tree2)
            {
                OnStoreCollectibleMappings(world, tree2, blockIdMapping, itemIdMapping);
            }
            else if (item.Value is ItemstackAttribute { value: { } value })
            {
                if (value.Collectible == null)
                {
                    value.ResolveBlockOrItem(world);
                }

                if (value.Class == EnumItemClass.Item)
                {
                    itemIdMapping[value.Id] = value.Collectible?.Code;
                }
                else
                {
                    blockIdMapping[value.Id] = value.Collectible?.Code;
                }
            }
        }
    }

    //
    // Summary:
    //     Should return a random pixel within the items/blocks texture
    //
    // Parameters:
    //   capi:
    //
    //   stack:
    public virtual int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
    {
        return 0;
    }

    //
    // Summary:
    //     Returns true if this blocks matterstate is liquid. (Liquid blocks should also
    //     implement IBlockFlowing)
    //     IMPORTANT: Calling code should have looked up the block using IBlockAccessor.GetBlock(pos,
    //     BlockLayersAccess.Fluid) !!!
    public virtual bool IsLiquid()
    {
        return MatterState == EnumMatterState.Liquid;
    }

    private void WalkBehaviors(CollectibleBehaviorDelegate onBehavior, Action defaultAction)
    {
        bool flag = true;
        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        foreach (CollectibleBehavior behavior in collectibleBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            onBehavior(behavior, ref handling);
            switch (handling)
            {
                case EnumHandling.PreventSubsequent:
                    return;
                case EnumHandling.PreventDefault:
                    flag = false;
                    break;
            }
        }

        if (flag)
        {
            defaultAction();
        }
    }

    //
    // Summary:
    //     Returns the blocks behavior of given type, if it has such behavior
    //
    // Parameters:
    //   type:
    //
    //   withInheritance:
    public CollectibleBehavior GetCollectibleBehavior(Type type, bool withInheritance)
    {
        return GetBehavior(CollectibleBehaviors, type, withInheritance);
    }

    public T GetCollectibleBehavior<T>(bool withInheritance) where T : CollectibleBehavior
    {
        return GetBehavior(CollectibleBehaviors, typeof(T), withInheritance) as T;
    }

    protected virtual CollectibleBehavior GetBehavior(CollectibleBehavior[] fromList, Type type, bool withInheritance)
    {
        if (withInheritance)
        {
            for (int i = 0; i < fromList.Length; i++)
            {
                Type type2 = fromList[i].GetType();
                if (type2 == type || type.IsAssignableFrom(type2))
                {
                    return fromList[i];
                }
            }

            return null;
        }

        for (int j = 0; j < fromList.Length; j++)
        {
            if (fromList[j].GetType() == type)
            {
                return fromList[j];
            }
        }

        return null;
    }

    //
    // Summary:
    //     Returns instance of class that implements this interface in the following order
    //
    //     1. Collectible (returns itself)
    //     2. CollectibleBlockBehavior (returns on of our own behavior)
    public virtual T GetCollectibleInterface<T>() where T : class
    {
        if (this is T result)
        {
            return result;
        }

        CollectibleBehavior collectibleBehavior = GetCollectibleBehavior(typeof(T), withInheritance: true);
        if (collectibleBehavior != null)
        {
            return collectibleBehavior as T;
        }

        return null;
    }

    //
    // Summary:
    //     Returns true if the block has given behavior
    //
    // Parameters:
    //   withInheritance:
    //
    // Type parameters:
    //   T:
    public virtual bool HasBehavior<T>(bool withInheritance = false) where T : CollectibleBehavior
    {
        return (T)GetCollectibleBehavior(typeof(T), withInheritance) != null;
    }

    //
    // Summary:
    //     Returns true if the block has given behavior
    //
    // Parameters:
    //   type:
    //
    //   withInheritance:
    public virtual bool HasBehavior(Type type, bool withInheritance = false)
    {
        return GetCollectibleBehavior(type, withInheritance) != null;
    }

    //
    // Summary:
    //     Returns true if the block has given behavior
    //
    // Parameters:
    //   type:
    //
    //   classRegistry:
    public virtual bool HasBehavior(string type, IClassRegistryAPI classRegistry)
    {
        return GetBehavior(classRegistry.GetBlockBehaviorClass(type)) != null;
    }

    //
    // Summary:
    //     Returns the blocks behavior of given type, if it has such behavior
    //
    // Parameters:
    //   type:
    public CollectibleBehavior GetBehavior(Type type)
    {
        return GetCollectibleBehavior(type, withInheritance: false);
    }

    //
    // Summary:
    //     Returns the blocks behavior of given type, if it has such behavior
    //
    // Type parameters:
    //   T:
    public T GetBehavior<T>() where T : CollectibleBehavior
    {
        return (T)GetCollectibleBehavior(typeof(T), withInheritance: false);
    }

    //
    // Summary:
    //     Called immediately prior to a firepit or similar testing whether this Collectible
    //     can be smelted
    //     Returns true if the caller should be marked dirty
    //
    // Parameters:
    //   inventorySmelting:
    public virtual bool OnSmeltAttempt(InventoryBase inventorySmelting)
    {
        return false;
    }

    [Obsolete]
    public static bool IsEmptyBackPack(IItemStack itemstack)
    {
        if (!IsBackPack(itemstack))
        {
            return false;
        }

        ITreeAttribute treeAttribute = itemstack.Attributes.GetTreeAttribute("backpack");
        if (treeAttribute == null)
        {
            return true;
        }

        foreach (KeyValuePair<string, IAttribute> item in treeAttribute.GetTreeAttribute("slots"))
        {
            IItemStack itemStack = (IItemStack)(item.Value?.GetValue());
            if (itemStack != null && itemStack.StackSize > 0)
            {
                return false;
            }
        }

        return true;
    }

    [Obsolete]
    public static bool IsBackPack(IItemStack itemstack)
    {
        if (itemstack == null || itemstack.Collectible.Attributes == null)
        {
            return false;
        }

        return itemstack.Collectible.Attributes["backpack"]["quantitySlots"].AsInt() > 0;
    }

    [Obsolete]
    public static int QuantityBackPackSlots(IItemStack itemstack)
    {
        if (itemstack == null || itemstack.Collectible.Attributes == null)
        {
            return 0;
        }

        return itemstack.Collectible.Attributes["backpack"]["quantitySlots"].AsInt();
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
