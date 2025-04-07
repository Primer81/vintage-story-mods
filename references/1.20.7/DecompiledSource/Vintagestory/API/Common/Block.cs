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
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

//
// Summary:
//     Basic class for a placeable block
public class Block : CollectibleObject
{
    public static readonly bool[] DefaultSideOpaque = new bool[6] { true, true, true, true, true, true };

    public static readonly bool[] DefaultSideAo = new bool[6] { true, true, true, true, true, true };

    public static readonly CompositeShape DefaultCubeShape = new CompositeShape
    {
        Base = new AssetLocation("block/basic/cube")
    };

    public static readonly string[] DefaultAllowAllSpawns = new string[1] { "*" };

    //
    // Summary:
    //     Default Full Block Collision Box
    public static Cuboidf DefaultCollisionBox = new Cuboidf(0f, 0f, 0f, 1f, 1f, 1f);

    //
    // Summary:
    //     Default Collision boxes (and also Selection boxes) array containing just the
    //     Default Collision Box This is standard for most solid blocks in the game. Since
    //     it is in practice immutable, all blocks can use a single copy of the same array
    //     This will help both RAM performance (avoids duplicate copies) and physics tick
    //     performance (this commonly accessed object can be well cached)
    public static readonly Cuboidf[] DefaultCollisionSelectionBoxes = new Cuboidf[1] { DefaultCollisionBox };

    //
    // Summary:
    //     Unique number of the block. Same as Vintagestory.API.Common.Block.Id. This number
    //     depends on the order in which the blocks are order. The numbering is however
    //     always ensured to remain the same on a per world basis.
    public int BlockId;

    //
    // Summary:
    //     If not set to JSON it will use an efficient hardcoded model
    public EnumDrawType DrawType = EnumDrawType.JSON;

    //
    // Summary:
    //     During which render pass this block should be rendered
    public EnumChunkRenderPass RenderPass;

    //
    // Summary:
    //     Currently not used
    public bool Ambientocclusion = true;

    //
    // Summary:
    //     Walk speed when standing or inside this block
    public float WalkSpeedMultiplier = 1f;

    //
    // Summary:
    //     Drag multiplier applied to entities standing on it
    public float DragMultiplier = 1f;

    //
    // Summary:
    //     If true, players can target individual selection boxes of the block
    public bool PartialSelection;

    //
    // Summary:
    //     The sounds played for this block during step, break, build and walk. Use GetSounds()
    //     to query if not performance critical.
    public BlockSounds Sounds;

    //
    // Summary:
    //     Data thats passed on to the graphics card for every vertex of the blocks model
    public VertexFlags VertexFlags;

    //
    // Summary:
    //     A bit uploaded to the shader to add a frost overlay below freezing temperature
    public bool Frostable;

    //
    // Summary:
    //     For light blocking blocks. Any value above 32 will completely block all light.
    public int LightAbsorption;

    //
    // Summary:
    //     If true, when the player holds the sneak key and right clicks this block, calls
    //     the blocks OnBlockInteractStart first, the items OnHeldInteractStart second.
    //     Without it the order is reversed.
    public bool PlacedPriorityInteract;

    //
    // Summary:
    //     A value usually between 0-9999 that indicates which blocks may be replaced with
    //     others. - Any block with replaceable value above 5000 will be washed away by
    //     water - Any block with replaceable value above 6000 will replaced when the player
    //     tries to place a block Examples: 0 = Bedrock 6000 = Tallgrass 9000 = Lava 9500
    //     = Water 9999 = Air
    public int Replaceable;

    //
    // Summary:
    //     0 = nothing can grow, 10 = some tallgrass and small trees can be grow on it,
    //     100 = all grass and trees can grow on it
    public int Fertility;

    //
    // Summary:
    //     The mining tier required to break this block
    public int RequiredMiningTier;

    //
    // Summary:
    //     How long it takes to break this block in seconds. Use GetResistance() to query
    //     if not performance critical.
    public float Resistance = 2f;

    //
    // Summary:
    //     A way to categorize blocks. Used for getting the mining speed for each tool type,
    //     amongst other things. Use GetBlockMaterial() to query if not performance critical.
    public EnumBlockMaterial BlockMaterial = EnumBlockMaterial.Stone;

    //
    // Summary:
    //     Random texture selection - whether or not to use the Y axis during randomization
    //     (for multiblock plants)
    public EnumRandomizeAxes RandomizeAxes;

    //
    // Summary:
    //     If true then the block will be randomly offseted by 1/3 of a block when placed
    public int RandomDrawOffset;

    public bool RandomizeRotations;

    public float RandomSizeAdjust;

    //
    // Summary:
    //     If true, the block will render with a UV offset enabling it to use the "other
    //     half" of a 64 x 64 texture on each alternate block position (e.g. Redwood trunk)
    public bool alternatingVOffset;

    //
    // Summary:
    //     Bit flags for the direction in which the alternatingVOffset is to be applied
    //     e.g. 0x30 to apply alternatingVOffset as the y position moves up and down
    public int alternatingVOffsetFaces;

    //
    // Summary:
    //     The block shape to be used when displayed in the inventory GUI, held in hand
    //     or dropped on the ground
    //     Note: from game version 1.20.4, this is null on server-side (except during asset
    //     loading start-up stage)
    public CompositeShape ShapeInventory;

    //
    // Summary:
    //     The default json block shape to be used when drawtype==JSON
    public CompositeShape Shape = DefaultCubeShape;

    //
    // Summary:
    //     The additional shape elements seen only at close distance ("LOD0"). For example,
    //     see leaves
    //     Note: from game version 1.20.4, this is null on server-side (except during asset
    //     loading start-up stage)
    public CompositeShape Lod0Shape;

    //
    // Summary:
    //     The alternative simplified shape seen at far distance ("LOD2"). For example,
    //     see flowers
    //     Note: from game version 1.20.4, this is null on server-side (except during asset
    //     loading start-up stage)
    public CompositeShape Lod2Shape;

    public MeshData Lod0Mesh;

    public MeshData Lod2Mesh;

    public bool DoNotRenderAtLod2;

    //
    // Summary:
    //     Default textures to be used for this block. The Dictionary keys are the texture
    //     short names, as referenced in this block's shape ShapeElementFaces
    //     (may be null on clients, prior to receipt of server assets)
    //     Note: from game version 1.20.4, this is null on server-side (except during asset
    //     loading start-up stage)
    public IDictionary<string, CompositeTexture> Textures;

    //
    // Summary:
    //     Fast array of texture variants, for use by cube (or similar) tesselators if the
    //     block has alternate shapes The outer array is indexed based on the 6 BlockFacing.Index
    //     numerals; the inner array is the variants
    public BakedCompositeTexture[][] FastTextureVariants;

    //
    // Summary:
    //     Textures to be used for this block in the inventory GUI, held in hand or dropped
    //     on the ground
    //     (may be null on clients, prior to receipt of server assets)
    //     Note: from game version 1.20.4, this is null on server-side (except during asset
    //     loading start-up stage)
    public IDictionary<string, CompositeTexture> TexturesInventory;

    //
    // Summary:
    //     Defines which of the 6 block sides are completely opaque. Used to determine which
    //     block faces can be culled during tesselation.
    public bool[] SideOpaque = DefaultSideOpaque;

    //
    // Summary:
    //     Defines which of the 6 block side are solid. Used to determine if attachable
    //     blocks can be attached to this block. Also used to determine if snow can rest
    //     on top of this block.
    public SmallBoolArray SideSolid = new SmallBoolArray(63);

    //
    // Summary:
    //     Defines which of the 6 block side should be shaded with ambient occlusion
    public bool[] SideAo = DefaultSideAo;

    //
    // Summary:
    //     Defines which of the 6 block neighbours should receive AO if this block is in
    //     front of them
    public byte EmitSideAo;

    //
    // Summary:
    //     Defines what creature groups may spawn on this block
    public string[] AllowSpawnCreatureGroups = DefaultAllowAllSpawns;

    public bool AllCreaturesAllowed;

    //
    // Summary:
    //     Determines which sides of the blocks should be rendered
    public EnumFaceCullMode FaceCullMode;

    //
    // Summary:
    //     The color map for climate color mapping. Leave null for no coloring by climate
    public string ClimateColorMap;

    public ColorMap ClimateColorMapResolved;

    //
    // Summary:
    //     The color map for season color mapping. Leave null for no coloring by season
    public string SeasonColorMap;

    public ColorMap SeasonColorMapResolved;

    //
    // Summary:
    //     Internal value that's set during if the block shape has any tint indexes for
    //     use in chunk tesselation and stuff O_O
    public bool ShapeUsesColormap;

    public bool LoadColorMapAnyway;

    //
    // Summary:
    //     Three extra color / season bits which may have meaning for specific blocks, such
    //     as leaves
    public int ExtraColorBits;

    //
    // Summary:
    //     Defines the area with which the player character collides with.
    public Cuboidf[] CollisionBoxes = DefaultCollisionSelectionBoxes;

    //
    // Summary:
    //     Defines the area which the players mouse pointer collides with for selection.
    public Cuboidf[] SelectionBoxes = DefaultCollisionSelectionBoxes;

    //
    // Summary:
    //     Defines the area with which particles collide with (if null, will be the same
    //     as CollisionBoxes).
    public Cuboidf[] ParticleCollisionBoxes;

    //
    // Summary:
    //     Used for ladders. If true, walking against this blocks collisionbox will make
    //     the player climb
    public bool Climbable;

    //
    // Summary:
    //     Will be used for not rendering rain below this block
    public bool RainPermeable;

    //
    // Summary:
    //     Value between 0..7 for Liquids to determine the height of the liquid
    public int LiquidLevel;

    //
    // Summary:
    //     If this block is or contains a liquid, this should be the code (or "identifier")
    //     of the liquid
    public string LiquidCode;

    //
    // Summary:
    //     A flag set during texture block shape tesselation
    public bool HasAlternates;

    public bool HasTiles;

    //
    // Summary:
    //     Modifiers that can alter the behavior of a block, particularly when being placed
    //     or removed
    public BlockBehavior[] BlockBehaviors = new BlockBehavior[0];

    //
    // Summary:
    //     Modifiers that can alter the behavior of a block entity
    public BlockEntityBehaviorType[] BlockEntityBehaviors = new BlockEntityBehaviorType[0];

    //
    // Summary:
    //     The items that should drop from breaking this block
    public BlockDropItemStack[] Drops;

    //
    // Summary:
    //     If true, a blocks drops will be split into stacks of stacksize 1 for more game
    //     juice. This field is only used in OnBlockBroken() and OnBlockExploded()
    public bool SplitDropStacks = true;

    //
    // Summary:
    //     Information about the blocks as a crop
    public BlockCropProperties CropProps;

    //
    // Summary:
    //     If this block has a block entity attached to it, this will store it's code
    public string EntityClass;

    public bool CustomBlockLayerHandler;

    public bool CanStep = true;

    public bool AllowStepWhenStuck;

    //
    // Summary:
    //     To allow Decor Behavior settings to be accessed through the Block API. See DecorFlags
    //     class for interpretation.
    public byte decorBehaviorFlags;

    //
    // Summary:
    //     Used to adjust selection box of parent block
    public float DecorThickness;

    public float InteractionHelpYOffset = 0.9f;

    public int TextureSubIdForBlockColor = -1;

    private float humanoidTraversalCost;

    //
    // Summary:
    //     To tell the JsonTesselator the offset to use when checking whether this is being
    //     rendered in/on ice (Currently only implemented by BlockWaterLily, compare seaweed
    //     and other water plants which check whether the block they are inside is ice,
    //     so their IceCheckOffset has the default value of 0)
    public int IceCheckOffset;

    protected static string[] miningTierNames = new string[7] { "tier_hands", "tier_stone", "tier_copper", "tier_bronze", "tier_iron", "tier_steel", "tier_titanium" };

    public Block notSnowCovered;

    public Block snowCovered1;

    public Block snowCovered2;

    public Block snowCovered3;

    public float snowLevel;

    protected float waveFlagMinY = 0.5625f;

    private float[] liquidBarrierHeightonSide;

    //
    // Summary:
    //     Returns the block id
    public override int Id => BlockId;

    //
    // Summary:
    //     Returns EnumItemClass.Block
    public override EnumItemClass ItemClass => EnumItemClass.Block;

    //
    // Summary:
    //     Return true if this block should be stored in the fluids layer in chunks instead
    //     of the solid blocks layer (e.g. water, flowing water, lake ice)
    public virtual bool ForFluidsLayer => false;

    //
    // Summary:
    //     Return non-null if this block should have water (or ice) placed in its position
    //     in the fluids layer when updating from 1.16 to 1.17
    public virtual string RemapToLiquidsLayer => null;

    //
    // Summary:
    //     Returns the first textures in the TexturesInventory dictionary
    public CompositeTexture FirstTextureInventory
    {
        get
        {
            if (Textures != null && Textures.Count != 0)
            {
                return Textures.First().Value;
            }

            return null;
        }
    }

    //
    // Summary:
    //     Entity pushing while an entity is inside this block. Read from attributes because
    //     i'm lazy.
    public Vec3d PushVector { get; set; }

    public virtual string ClimateColorMapForMap => ClimateColorMap;

    public virtual string SeasonColorMapForMap => SeasonColorMap;

    //
    // Summary:
    //     Sets the whole SideOpaque array to true
    public bool AllSidesOpaque
    {
        get
        {
            if (SideOpaque[0] && SideOpaque[1] && SideOpaque[2] && SideOpaque[3] && SideOpaque[4])
            {
                return SideOpaque[5];
            }

            return false;
        }
        set
        {
            if (SideOpaque == DefaultSideOpaque)
            {
                if (!value)
                {
                    SideOpaque = new bool[6];
                }

                return;
            }

            SideOpaque[0] = value;
            SideOpaque[1] = value;
            SideOpaque[2] = value;
            SideOpaque[3] = value;
            SideOpaque[4] = value;
            SideOpaque[5] = value;
        }
    }

    //
    // Summary:
    //     Creates a new instance of a block with null model transforms; BlockTypeNet will
    //     add default transforms client-side if they are null in the BlockType packet;
    //     transforms should not be needed on a server
    public Block()
    {
    }

    //
    // Summary:
    //     Called when this block was loaded by the server or the client
    //
    // Parameters:
    //   api:
    public override void OnLoaded(ICoreAPI api)
    {
        humanoidTraversalCost = (Attributes?["humanoidTraversalCost"]?.AsFloat(1f)).GetValueOrDefault(1f);
        PushVector = Attributes?["pushVector"]?.AsObject<Vec3d>();
        AllowStepWhenStuck = (Attributes?["allowStepWhenStuck"]?.AsBool()).GetValueOrDefault();
        CanStep = Attributes?["canStep"].AsBool(defaultValue: true) ?? true;
        base.OnLoaded(api);
        string text = Variant["cover"];
        if (text != null && (text == "free" || text.Contains("snow")))
        {
            notSnowCovered = api.World.GetBlock(CodeWithVariant("cover", "free"));
            snowCovered1 = api.World.GetBlock(CodeWithVariant("cover", "snow"));
            snowCovered2 = api.World.GetBlock(CodeWithVariant("cover", "snow2"));
            snowCovered3 = api.World.GetBlock(CodeWithVariant("cover", "snow3"));
            if (this == snowCovered1)
            {
                snowLevel = 1f;
            }

            if (this == snowCovered2)
            {
                snowLevel = 2f;
            }

            if (this == snowCovered3)
            {
                snowLevel = 3f;
            }
        }

        if (api.Side == EnumAppSide.Client)
        {
            LoadTextureSubIdForBlockColor();
        }
    }

    public virtual void LoadTextureSubIdForBlockColor()
    {
        TextureSubIdForBlockColor = -1;
        if (Textures == null)
        {
            return;
        }

        string text = Attributes?["textureCodeForBlockColor"].AsString();
        if (text != null && Textures.TryGetValue(text, out var value))
        {
            TextureSubIdForBlockColor = value.Baked.TextureSubId;
        }

        if (TextureSubIdForBlockColor < 0)
        {
            if (Textures.TryGetValue("up", out var value2))
            {
                TextureSubIdForBlockColor = value2.Baked.TextureSubId;
            }
            else if (Textures.Count > 0)
            {
                TextureSubIdForBlockColor = (Textures.First().Value?.Baked?.TextureSubId).GetValueOrDefault();
            }
        }
    }

    //
    // Summary:
    //     Called for example when the player places a block inside a liquid block. Needs
    //     to return true if the liquid should get removed.
    //
    // Parameters:
    //   blockAccess:
    //
    //   pos:
    public virtual bool DisplacesLiquids(IBlockAccessor blockAccess, BlockPos pos)
    {
        return SideSolid.SidesAndBase;
    }

    //
    // Summary:
    //     Does the side APPEAR fully solid? Called for example when deciding to render
    //     water edges at a position, or not Note: Worldgen code uses the blockAccessor-aware
    //     overload of this method
    public virtual bool SideIsSolid(BlockPos pos, int faceIndex)
    {
        return SideSolid[faceIndex];
    }

    //
    // Summary:
    //     Is the side solid or almost fully solid (in the case of chiselled blocks)? Called
    //     for example when deciding to place loose stones or boulders above this during
    //     worldgen
    public virtual bool SideIsSolid(IBlockAccessor blockAccess, BlockPos pos, int faceIndex)
    {
        return SideIsSolid(pos, faceIndex);
    }

    //
    // Summary:
    //     This method gets called when facecull mode is set to 'Callback'. Curently used
    //     for custom behaviors when merging ice
    //
    // Parameters:
    //   facingIndex:
    //     The index of the BlockFacing face of this block being tested
    //
    //   neighbourBlock:
    //     The neighbouring block
    //
    //   intraChunkIndex3d:
    //     The position index within the chunk (z * 32 * 32 + y * 32 + x): the BlockEntity
    //     can be obtained using this if necessary
    public virtual bool ShouldMergeFace(int facingIndex, Block neighbourBlock, int intraChunkIndex3d)
    {
        return false;
    }

    //
    // Summary:
    //     The cuboid used to determine where to spawn particles when breaking the block
    //
    //
    // Parameters:
    //   blockAccess:
    //
    //   pos:
    //
    //   facing:
    public virtual Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
    {
        if (facing == null)
        {
            Cuboidf[] selectionBoxes = GetSelectionBoxes(blockAccess, pos);
            if (selectionBoxes == null || selectionBoxes.Length == 0)
            {
                return DefaultCollisionBox;
            }

            return selectionBoxes[0];
        }

        Cuboidf[] array = GetCollisionBoxes(blockAccess, pos);
        if (array == null || array.Length == 0)
        {
            array = GetSelectionBoxes(blockAccess, pos);
        }

        if (array == null || array.Length == 0)
        {
            return null;
        }

        return array[0];
    }

    //
    // Summary:
    //     Returns the blocks selection boxes at this position in the world.
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    public virtual Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        if (RandomDrawOffset != 0)
        {
            Cuboidf[] selectionBoxes = SelectionBoxes;
            if (selectionBoxes != null && selectionBoxes.Length >= 1)
            {
                float x = (float)(GameMath.oaatHash(pos.X, 0, pos.Z) % 12) / (24f + 12f * (float)RandomDrawOffset);
                float z = (float)(GameMath.oaatHash(pos.X, 1, pos.Z) % 12) / (24f + 12f * (float)RandomDrawOffset);
                return new Cuboidf[1] { SelectionBoxes[0].OffsetCopy(x, 0f, z) };
            }
        }

        Cuboidf[] selectionBoxes2 = SelectionBoxes;
        if (selectionBoxes2 == null || selectionBoxes2.Length != 1)
        {
            return SelectionBoxes;
        }

        IWorldChunk chunkAtBlockPos = blockAccessor.GetChunkAtBlockPos(pos);
        if (chunkAtBlockPos == null)
        {
            return SelectionBoxes;
        }

        return chunkAtBlockPos.AdjustSelectionBoxForDecor(blockAccessor, pos, SelectionBoxes);
    }

    //
    // Summary:
    //     Returns the blocks collision box. Warning: This method may get called by different
    //     threads, so it has to be thread safe.
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    public virtual Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return CollisionBoxes;
    }

    //
    // Summary:
    //     Returns the blocks particle collision box. Warning: This method may get called
    //     by different threads, so it has to be thread safe.
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    public virtual Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return ParticleCollisionBoxes ?? CollisionBoxes;
    }

    //
    // Summary:
    //     Should return the blocks material Warning: This method is may get called in a
    //     background thread. Please make sure your code in here is thread safe.
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    //     May be null and therfore stack is non-null
    //
    //   stack:
    public virtual EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
    {
        return BlockMaterial;
    }

    //
    // Summary:
    //     Should return the blocks resistance to breaking
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    public virtual float GetResistance(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return Resistance;
    }

    [Obsolete("Use GetSounds with BlockSelection instead")]
    public virtual BlockSounds GetSounds(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
    {
        BlockSelection blockSel = new BlockSelection
        {
            Position = pos
        };
        return GetSounds(blockAccessor, blockSel, stack);
    }

    //
    // Summary:
    //     Should returns the blocks sounds
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    //     May be null and therfore stack is non-null
    //
    //   stack:
    public virtual BlockSounds GetSounds(IBlockAccessor blockAccessor, BlockSelection blockSel, ItemStack stack = null)
    {
        Block block = ((blockSel.Face == null) ? null : blockAccessor.GetDecor(blockSel.Position, new DecorBits(blockSel.Face)));
        if (block != null)
        {
            JsonObject attributes = block.Attributes;
            if (attributes == null || !attributes["ignoreSounds"].AsBool())
            {
                return block.Sounds;
            }
        }

        return Sounds;
    }

    //
    // Summary:
    //     Position-aware version of Attributes, for example can be used by BlockMultiblock
    //
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    public virtual JsonObject GetAttributes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return Attributes;
    }

    public virtual bool DoEmitSideAo(IGeometryTester caller, BlockFacing facing)
    {
        return (EmitSideAo & facing.Flag) != 0;
    }

    public virtual bool DoEmitSideAoByFlag(IGeometryTester caller, Vec3iAndFacingFlags vec, int flags)
    {
        return (EmitSideAo & flags) != 0;
    }

    public virtual int GetLightAbsorption(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return LightAbsorption;
    }

    public virtual int GetLightAbsorption(IWorldChunk chunk, BlockPos pos)
    {
        return LightAbsorption;
    }

    //
    // Summary:
    //     If this block is or contains a liquid, it should return the code of it. Used
    //     for example by farmland to check if a nearby block is water
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    public virtual string GetLiquidCode(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return LiquidCode;
    }

    //
    // Summary:
    //     Called before a decal is created.
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   decalTexSource:
    //
    //   decalModelData:
    //     The block model which need UV values for the decal texture
    //
    //   blockModelData:
    //     The original block model
    public virtual void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
    {
    }

    //
    // Summary:
    //     Used by torches and other blocks to check if it can attach itself to that block
    //
    //
    // Parameters:
    //   blockAccessor:
    //
    //   block:
    //
    //   pos:
    //
    //   blockFace:
    //
    //   attachmentArea:
    //     Area of attachment of given face in voxel dimensions (0..15)
    public virtual bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        bool flag = true;
        bool flag2 = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.CanAttachBlockAt(blockAccessor, block, pos, blockFace, ref handling, attachmentArea);
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

        return SideSolid[blockFace.Index];
    }

    //
    // Summary:
    //     Should return if supplied entitytype is allowed to spawn on this block
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    //
    //   type:
    //
    //   sc:
    public virtual bool CanCreatureSpawnOn(IBlockAccessor blockAccessor, BlockPos pos, EntityProperties type, BaseSpawnConditions sc)
    {
        bool flag = true;
        bool flag2 = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.CanCreatureSpawnOn(blockAccessor, pos, type, sc, ref handling);
            if (handling != 0)
            {
                flag2 = true;
                flag = flag && flag3;
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

        bool flag4 = true;
        if (!AllCreaturesAllowed)
        {
            flag4 = AllowSpawnCreatureGroups != null && AllowSpawnCreatureGroups.Length != 0 && (AllowSpawnCreatureGroups.Contains("*") || AllowSpawnCreatureGroups.Contains(sc.Group));
        }

        if (flag4)
        {
            if (sc.RequireSolidGround)
            {
                return SideSolid[BlockFacing.UP.Index];
            }

            return true;
        }

        return false;
    }

    //
    // Summary:
    //     Currently used for wildvines and saguaro cactus
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    //
    //   onBlockFace:
    //
    //   worldgenRandom:
    //
    //   attributes:
    public virtual bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
    {
        Block block = blockAccessor.GetBlock(pos);
        if (block.IsReplacableBy(this))
        {
            if (block.EntityClass != null)
            {
                blockAccessor.RemoveBlockEntity(pos);
            }

            blockAccessor.SetBlock(BlockId, pos);
            if (EntityClass != null)
            {
                blockAccessor.SpawnBlockEntity(EntityClass, pos);
            }

            return true;
        }

        return false;
    }

    public virtual bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes attributes = null)
    {
        return TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom, attributes);
    }

    //
    // Summary:
    //     Called when the player attempts to place this block
    //
    // Parameters:
    //   world:
    //
    //   byPlayer:
    //
    //   itemstack:
    //
    //   blockSel:
    //
    //   failureCode:
    //     If you return false, set this value to a code why it cannot be placed. Its used
    //     for the ingame error overlay. Set to "__ignore__" to not trigger an error
    public virtual bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
        bool flag = true;
        bool flag2 = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref handling, ref failureCode);
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

        if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
        {
            return DoPlaceBlock(world, byPlayer, blockSel, itemstack);
        }

        return false;
    }

    //
    // Summary:
    //     Checks if this block does not intersect with something at given position
    //
    // Parameters:
    //   world:
    //
    //   byPlayer:
    //
    //   blockSel:
    //
    //   failureCode:
    public virtual bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
    {
        if (!world.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(this))
        {
            failureCode = "notreplaceable";
            return false;
        }

        if (CollisionBoxes != null && CollisionBoxes.Length != 0 && world.GetIntersectingEntities(blockSel.Position, GetCollisionBoxes(world.BlockAccessor, blockSel.Position), (Entity e) => e.IsInteractable).Length != 0)
        {
            failureCode = "entityintersecting";
            return false;
        }

        bool flag = true;
        if (byPlayer != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
        {
            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
            failureCode = "claimed";
            return false;
        }

        bool flag2 = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.CanPlaceBlock(world, byPlayer, blockSel, ref handling, ref failureCode);
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

        return true;
    }

    //
    // Summary:
    //     Called by TryPlaceBlock if placement is possible
    //
    // Parameters:
    //   world:
    //
    //   byPlayer:
    //
    //   blockSel:
    //
    //   byItemStack:
    //     Might be null
    public virtual bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
    {
        bool flag = true;
        bool flag2 = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handling);
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

        world.BlockAccessor.SetBlock(BlockId, blockSel.Position, byItemStack);
        return true;
    }

    //
    // Summary:
    //     Called by the server and the client when the player currently looks at this block.
    //     Gets called continously every tick.
    //
    // Parameters:
    //   byPlayer:
    //
    //   blockSel:
    //
    //   firstTick:
    //     True when previous tick the player looked at a different block. You can use it
    //     to make an efficient, single-event lookat trigger
    public virtual void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick)
    {
    }

    //
    // Summary:
    //     Player is breaking this block. Has to reduce remainingResistance by the amount
    //     of time it should be broken. This method is called only client side, every 40ms
    //     during breaking.
    //
    // Parameters:
    //   player:
    //
    //   blockSel:
    //
    //   itemslot:
    //     The item the player currently has in his hands
    //
    //   remainingResistance:
    //     how many seconds was left until the block breaks fully
    //
    //   dt:
    //     seconds passed since last render frame
    //
    //   counter:
    //     Total count of hits (every 40ms)
    //
    // Returns:
    //     how many seconds now left until the block breaks fully. If a value equal to or
    //     below 0 is returned, OnBlockBroken() will get called.
    public virtual float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
    {
        IItemStack itemstack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
        float num = remainingResistance;
        if (RequiredMiningTier == 0)
        {
            if (dt > 0f)
            {
                BlockBehavior[] blockBehaviors = BlockBehaviors;
                foreach (BlockBehavior blockBehavior in blockBehaviors)
                {
                    dt *= blockBehavior.GetMiningSpeedModifier(api.World, blockSel.Position, player);
                }
            }

            num -= dt;
        }

        if (itemstack != null)
        {
            num = itemstack.Collectible.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
        }

        long num2 = 0L;
        if (api.ObjectCache.TryGetValue("totalMsBlockBreaking", out var value))
        {
            num2 = (long)value;
        }

        long elapsedMilliseconds = api.World.ElapsedMilliseconds;
        if (elapsedMilliseconds - num2 > 225 || num <= 0f)
        {
            double posx = (double)blockSel.Position.X + blockSel.HitPosition.X;
            double posy = (double)blockSel.Position.InternalY + blockSel.HitPosition.Y;
            double posz = (double)blockSel.Position.Z + blockSel.HitPosition.Z;
            BlockSounds sounds = GetSounds(api.World.BlockAccessor, blockSel);
            player.Entity.World.PlaySoundAt((num > 0f) ? sounds.GetHitSound(player) : sounds.GetBreakSound(player), posx, posy, posz, player, RandomSoundPitch(api.World), 16f);
            api.ObjectCache["totalMsBlockBreaking"] = elapsedMilliseconds;
        }

        return num;
    }

    public virtual float RandomSoundPitch(IWorldAccessor world)
    {
        return (float)world.Rand.NextDouble() * 0.5f + 0.75f;
    }

    //
    // Summary:
    //     Called when a survival player has broken the block. This method needs to remove
    //     the block.
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   byPlayer:
    //
    //   dropQuantityMultiplier:
    public virtual void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
        bool flag = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            obj.OnBlockBroken(world, pos, byPlayer, ref handling);
            if (handling == EnumHandling.PreventDefault)
            {
                flag = true;
            }

            if (handling == EnumHandling.PreventSubsequent)
            {
                return;
            }
        }

        if (flag)
        {
            return;
        }

        if (EntityClass != null)
        {
            world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
        }

        if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
        {
            ItemStack[] drops = GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            if (drops != null)
            {
                for (int j = 0; j < drops.Length; j++)
                {
                    if (SplitDropStacks)
                    {
                        for (int k = 0; k < drops[j].StackSize; k++)
                        {
                            ItemStack itemStack = drops[j].Clone();
                            itemStack.StackSize = 1;
                            world.SpawnItemEntity(itemStack, pos);
                        }
                    }
                    else
                    {
                        world.SpawnItemEntity(drops[j].Clone(), pos);
                    }
                }
            }

            world.PlaySoundAt(Sounds?.GetBreakSound(byPlayer), pos, 0.0, byPlayer);
        }

        SpawnBlockBrokenParticles(pos);
        world.BlockAccessor.SetBlock(0, pos);
    }

    public void SpawnBlockBrokenParticles(BlockPos pos)
    {
        BlockBrokenParticleProps blockBrokenParticleProps = new BlockBrokenParticleProps
        {
            blockdamage = new BlockDamage
            {
                Facing = BlockFacing.UP
            }
        };
        blockBrokenParticleProps.Init(api);
        blockBrokenParticleProps.blockdamage.Block = this;
        blockBrokenParticleProps.blockdamage.Position = pos;
        blockBrokenParticleProps.boyant = MaterialDensity < 1000;
        IClientPlayer clientPlayer = (api as ICoreClientAPI)?.World.Player;
        api.World.SpawnParticles(blockBrokenParticleProps, clientPlayer);
        if (clientPlayer != null && (clientPlayer.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Creative)
        {
            api.World.SpawnParticles(blockBrokenParticleProps, clientPlayer);
        }
    }

    public virtual void OnBrokenAsDecor(IWorldAccessor world, BlockPos pos, BlockFacing side)
    {
        if (world.Side != EnumAppSide.Server)
        {
            return;
        }

        ItemStack[] drops = GetDrops(world, pos, null);
        if (drops == null)
        {
            return;
        }

        Vec3d position = new Vec3d((double)pos.X + 0.5 + (double)side.Normali.X * 0.75, (double)pos.Y + 0.5 + (double)side.Normali.Y * 0.75, (double)pos.Z + 0.5 + (double)side.Normali.Z * 0.75);
        for (int i = 0; i < drops.Length; i++)
        {
            if (SplitDropStacks)
            {
                for (int j = 0; j < drops[i].StackSize; j++)
                {
                    ItemStack itemStack = drops[i].Clone();
                    itemStack.StackSize = 1;
                    world.SpawnItemEntity(itemStack, position);
                }
            }
        }
    }

    public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
    {
        bool flag = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            obj.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe, ref handled);
            if (handled == EnumHandling.PreventDefault)
            {
                flag = true;
            }

            if (handled == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }
    }

    //
    // Summary:
    //     Should return all of the blocks drops for display in the handbook
    //
    // Parameters:
    //   handbookStack:
    //
    //   forPlayer:
    public virtual BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
    {
        if (Drops != null)
        {
            IEnumerable<BlockDropItemStack> enumerable = Array.Empty<BlockDropItemStack>();
            BlockDropItemStack[] drops = Drops;
            foreach (BlockDropItemStack blockDropItemStack in drops)
            {
                if (blockDropItemStack.ResolvedItemstack.Collectible is IResolvableCollectible resolvableCollectible)
                {
                    BlockDropItemStack[] dropsForHandbook = resolvableCollectible.GetDropsForHandbook(handbookStack, forPlayer);
                    enumerable = enumerable.Concat(dropsForHandbook);
                }
                else
                {
                    enumerable = enumerable.Append(blockDropItemStack);
                }
            }

            return enumerable.ToArray();
        }

        return Drops;
    }

    //
    // Summary:
    //     Helper method for a number of blocks
    //
    // Parameters:
    //   handbookStack:
    //
    //   forPlayer:
    protected virtual BlockDropItemStack[] GetHandbookDropsFromBreakDrops(ItemStack handbookStack, IPlayer forPlayer)
    {
        ItemStack[] drops = GetDrops(api.World, forPlayer.Entity.Pos.XYZ.AsBlockPos, forPlayer);
        if (drops == null)
        {
            return new BlockDropItemStack[0];
        }

        BlockDropItemStack[] array = new BlockDropItemStack[drops.Length];
        for (int i = 0; i < drops.Length; i++)
        {
            array[i] = new BlockDropItemStack(drops[i]);
        }

        return array;
    }

    //
    // Summary:
    //     Is called before a block is broken, should return what items this block should
    //     drop. Return null or empty array for no drops.
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   byPlayer:
    //
    //   dropQuantityMultiplier:
    public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
        bool flag = false;
        List<ItemStack> list = new List<ItemStack>();
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            ItemStack[] drops = obj.GetDrops(world, pos, byPlayer, ref dropQuantityMultiplier, ref handling);
            if (drops != null)
            {
                list.AddRange(drops);
            }

            switch (handling)
            {
                case EnumHandling.PreventSubsequent:
                    return drops;
                case EnumHandling.PreventDefault:
                    flag = true;
                    break;
            }
        }

        if (flag)
        {
            return list.ToArray();
        }

        if (Drops == null)
        {
            return null;
        }

        List<ItemStack> list2 = new List<ItemStack>();
        for (int j = 0; j < Drops.Length; j++)
        {
            BlockDropItemStack blockDropItemStack = Drops[j];
            if (blockDropItemStack.Tool.HasValue && (byPlayer == null || blockDropItemStack.Tool != byPlayer.InventoryManager.ActiveTool))
            {
                continue;
            }

            float num = 1f;
            if (blockDropItemStack.DropModbyStat != null)
            {
                num = byPlayer.Entity.Stats.GetBlended(blockDropItemStack.DropModbyStat);
            }

            ItemStack itemStack = Drops[j].GetNextItemStack(dropQuantityMultiplier * num);
            if (itemStack != null)
            {
                if (itemStack.Collectible is IResolvableCollectible resolvableCollectible)
                {
                    DummySlot dummySlot = new DummySlot(itemStack);
                    resolvableCollectible.Resolve(dummySlot, world);
                    itemStack = dummySlot.Itemstack;
                }

                list2.Add(itemStack);
                if (Drops[j].LastDrop)
                {
                    break;
                }
            }
        }

        list2.AddRange(list);
        return list2.ToArray();
    }

    //
    // Summary:
    //     When the player has presed the middle mouse click on the block
    //
    // Parameters:
    //   world:
    //
    //   pos:
    public virtual ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        EnumHandling enumHandling = EnumHandling.PassThrough;
        ItemStack result = null;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            ItemStack itemStack = obj.OnPickBlock(world, pos, ref handling);
            if (handling != 0)
            {
                result = itemStack;
                enumHandling = handling;
            }

            if (enumHandling == EnumHandling.PreventSubsequent)
            {
                return result;
            }
        }

        if (enumHandling == EnumHandling.PreventDefault)
        {
            return result;
        }

        return new ItemStack(this);
    }

    //
    // Summary:
    //     Always called when a block has been removed through whatever method, except during
    //     worldgen or via ExchangeBlock() For Worldgen you might be able to use TryPlaceBlockForWorldGen()
    //     to attach custom behaviors during placement/removal
    //
    // Parameters:
    //   world:
    //
    //   pos:
    public virtual void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
    {
        bool flag = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            obj.OnBlockRemoved(world, pos, ref handling);
            switch (handling)
            {
                case EnumHandling.PreventSubsequent:
                    return;
                case EnumHandling.PreventDefault:
                    flag = true;
                    break;
            }
        }

        if (!flag && EntityClass != null)
        {
            world.BlockAccessor.RemoveBlockEntity(pos);
        }
    }

    //
    // Summary:
    //     Always called when a block has been placed through whatever method, except during
    //     worldgen or via ExchangeBlock() For Worldgen you might be able to use TryPlaceBlockForWorldGen()
    //     to attach custom behaviors during placement/removal
    //
    // Parameters:
    //   world:
    //
    //   blockPos:
    //
    //   byItemStack:
    //     May be null!
    public virtual void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
    {
        bool flag = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            obj.OnBlockPlaced(world, blockPos, ref handling);
            switch (handling)
            {
                case EnumHandling.PreventSubsequent:
                    return;
                case EnumHandling.PreventDefault:
                    flag = true;
                    break;
            }
        }

        if (!flag && EntityClass != null)
        {
            world.BlockAccessor.SpawnBlockEntity(EntityClass, blockPos, byItemStack);
        }
    }

    //
    // Summary:
    //     Called when any of its 6 neighbour blocks has been changed
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   neibpos:
    public virtual void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
    {
        EnumHandling handling = EnumHandling.PassThrough;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        for (int i = 0; i < blockBehaviors.Length; i++)
        {
            blockBehaviors[i].OnNeighbourBlockChange(world, pos, neibpos, ref handling);
            if (handling == EnumHandling.PreventSubsequent)
            {
                return;
            }
        }

        if (handling == EnumHandling.PassThrough && (this == snowCovered1 || this == snowCovered2 || this == snowCovered3) && pos.X == neibpos.X && pos.Z == neibpos.Z && pos.Y + 1 == neibpos.Y && world.BlockAccessor.GetBlock(neibpos).Id != 0)
        {
            world.BlockAccessor.SetBlock(notSnowCovered.Id, pos);
        }
    }

    //
    // Summary:
    //     When a player does a right click while targeting this placed block. Should return
    //     true if the event is handled, so that other events can occur, e.g. eating a held
    //     item if the block is not interactable with.
    //
    // Parameters:
    //   world:
    //
    //   byPlayer:
    //
    //   blockSel:
    //
    // Returns:
    //     False if the interaction should be stopped. True if the interaction should continue.
    //     If you return false, the interaction will not be synced to the server.
    public virtual bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        bool flag = true;
        if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
        {
            return false;
        }

        bool flag2 = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
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

        return false;
    }

    //
    // Summary:
    //     When a Command Block, console command or (perhaps in future) non-player entity
    //     wants to activate this placed block
    //
    // Parameters:
    //   world:
    //
    //   caller:
    //
    //   blockSel:
    //
    //   activationArgs:
    public virtual void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs = null)
    {
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            obj.Activate(world, caller, blockSel, activationArgs, ref handled);
            if (handled == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }
    }

    //
    // Summary:
    //     Called every frame while the player is using this block. Return false to stop
    //     the interaction.
    //
    // Parameters:
    //   secondsUsed:
    //
    //   world:
    //
    //   byPlayer:
    //
    //   blockSel:
    public virtual bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        bool flag = false;
        bool flag2 = true;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel, ref handling);
            if (handling != 0)
            {
                flag2 = flag2 && flag3;
                flag = true;
            }

            if (handling == EnumHandling.PreventSubsequent)
            {
                return flag2;
            }
        }

        if (flag)
        {
            return flag2;
        }

        return false;
    }

    //
    // Summary:
    //     Called when the player successfully completed the using action, always called
    //     once an interaction is over
    //
    // Parameters:
    //   secondsUsed:
    //
    //   world:
    //
    //   byPlayer:
    //
    //   blockSel:
    public virtual void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        bool flag = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            obj.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, ref handling);
            if (handling != 0)
            {
                flag = true;
            }

            if (handling == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }
    }

    //
    // Summary:
    //     When the player released the right mouse button. Return false to deny the cancellation
    //     (= will keep using the block until OnBlockInteractStep returns false).
    //
    // Parameters:
    //   secondsUsed:
    //
    //   world:
    //
    //   byPlayer:
    //
    //   blockSel:
    //
    //   cancelReason:
    public virtual bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
    {
        bool flag = false;
        bool flag2 = true;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, ref handling);
            if (handling != 0)
            {
                flag2 = flag2 && flag3;
                flag = true;
            }

            if (handling == EnumHandling.PreventSubsequent)
            {
                return flag2;
            }
        }

        if (flag)
        {
            return flag2;
        }

        return true;
    }

    //
    // Summary:
    //     When an entity is inside a block 1x1x1 space, independent of of its selection
    //     box or collision box
    //
    // Parameters:
    //   world:
    //
    //   entity:
    //
    //   pos:
    public virtual void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
    {
    }

    //
    // Summary:
    //     Whenever an entity collides with the collision box of the block
    //
    // Parameters:
    //   world:
    //
    //   entity:
    //
    //   pos:
    //
    //   facing:
    //
    //   collideSpeed:
    //
    //   isImpact:
    public virtual void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
    {
        if (entity.Properties.CanClimb && (IsClimbable(pos) || entity.Properties.CanClimbAnywhere) && facing.IsHorizontal && entity is EntityAgent)
        {
            EntityAgent entityAgent = entity as EntityAgent;
            if (!new bool?(entityAgent.Controls.Sneak).GetValueOrDefault())
            {
                entityAgent.SidedPos.Motion.Y = 0.04;
            }
        }

        float num = Math.Max(0f, 0.75f - entity.CollisionBox.Height);
        float @float = entity.WatchedAttributes.GetFloat("impactBlockUpdateChance", 0.2f - num);
        if (isImpact && collideSpeed.Y < -0.05 && world.Rand.NextDouble() < (double)@float)
        {
            OnNeighbourBlockChange(world, pos, pos.UpCopy());
        }
    }

    //
    // Summary:
    //     Called when a falling block falls onto this one. Return true to cancel default
    //     behavior.
    //     Note: From game version 1.20.4, if overriding this you should also override Vintagestory.API.Common.Block.CanAcceptFallOnto(Vintagestory.API.Common.IWorldAccessor,Vintagestory.API.MathTools.BlockPos,Vintagestory.API.Common.Block,Vintagestory.API.Datastructures.TreeAttribute)().
    //     See BlockCoalPile for an example. If CanAcceptFallOnto() is not implemented,
    //     then this OnFallOnto() method will most likely never be called
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   block:
    //
    //   blockEntityAttributes:
    public virtual bool OnFallOnto(IWorldAccessor world, BlockPos pos, Block block, TreeAttribute blockEntityAttributes)
    {
        return false;
    }

    //
    // Summary:
    //     Called on the main main thread or, potentially, on a separate thread if multiple
    //     physics threads is enabled. Return true to have Vintagestory.API.Common.Block.OnFallOnto(Vintagestory.API.Common.IWorldAccessor,Vintagestory.API.MathTools.BlockPos,Vintagestory.API.Common.Block,Vintagestory.API.Datastructures.TreeAttribute)()
    //     called, which will always be on the main thread
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   fallingBlock:
    //
    //   blockEntityAttributes:
    public virtual bool CanAcceptFallOnto(IWorldAccessor world, BlockPos pos, Block fallingBlock, TreeAttribute blockEntityAttributes)
    {
        return false;
    }

    //
    // Summary:
    //     Everytime the player moves by 8 blocks (or rather leaves the current 8-grid),
    //     a scan of all blocks 32x32x32 blocks around the player is initiated
    //     and this method is called. If the method returns true, the block is registered
    //     to a client side game ticking for spawning particles and such.
    //     This method will be called everytime the player left his current 8-grid area.
    //
    //
    // Parameters:
    //   world:
    //
    //   player:
    //
    //   pos:
    //
    //   isWindAffected:
    public virtual bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
    {
        bool flag = false;
        bool flag2 = false;
        isWindAffected = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.ShouldReceiveClientParticleTicks(world, player, pos, ref handling);
            if (handling != 0)
            {
                flag = flag || flag3;
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

        if (ParticleProperties != null && ParticleProperties.Length != 0)
        {
            for (int j = 0; j < ParticleProperties.Length; j++)
            {
                isWindAffected |= ParticleProperties[0].WindAffectednes > 0f;
            }

            return true;
        }

        return false;
    }

    [Obsolete("Use GetAmbientsoundStrength() instead. Method will be removed in 1.21")]
    public virtual bool ShouldPlayAmbientSound(IWorldAccessor world, BlockPos pos)
    {
        return GetAmbientSoundStrength(world, pos) > 0f;
    }

    //
    // Summary:
    //     If this block defines an ambient sounds, the intensity the ambient should be
    //     played at. Between 0 and 1. Return 0 to not play the ambient sound.
    //
    // Parameters:
    //   world:
    //
    //   pos:
    public virtual float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
    {
        return 1f;
    }

    //
    // Summary:
    //     Called evey 25ms if the block is in range (32 blocks) and block returned true
    //     on ShouldReceiveClientGameTicks(). Takes a few seconds for the game to register
    //     the block.
    //
    // Parameters:
    //   manager:
    //
    //   pos:
    //
    //   windAffectednessAtPos:
    //
    //   secondsTicking:
    public virtual void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
    {
        if (ParticleProperties != null && ParticleProperties.Length != 0)
        {
            for (int i = 0; i < ParticleProperties.Length; i++)
            {
                AdvancedParticleProperties advancedParticleProperties = ParticleProperties[i];
                advancedParticleProperties.WindAffectednesAtPos = windAffectednessAtPos;
                advancedParticleProperties.basePos.X = (float)pos.X + TopMiddlePos.X;
                advancedParticleProperties.basePos.Y = (float)pos.InternalY + TopMiddlePos.Y;
                advancedParticleProperties.basePos.Z = (float)pos.Z + TopMiddlePos.Z;
                manager.Spawn(advancedParticleProperties);
            }
        }

        BlockBehavior[] blockBehaviors = BlockBehaviors;
        for (int j = 0; j < blockBehaviors.Length; j++)
        {
            blockBehaviors[j].OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
        }
    }

    //
    // Summary:
    //     Called every interval specified in Server.Config.RandomTickInterval. Defaults
    //     to 50ms. This method is called on a separate server thread. This should be considered
    //     when deciding how to access blocks. If true is returned, the server will call
    //     OnServerGameTick on the main thread passing the BlockPos and the 'extra' object
    //     if specified. The 'extra' parameter is meant to prevent duplicating lookups and
    //     other calculations when OnServerGameTick is called.
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //     The position of this block
    //
    //   offThreadRandom:
    //     If you do anything with random inside this method, don't use world.Rand because
    //     System.Random its not thread safe, use this or create your own instance
    //
    //   extra:
    //     Optional parameter to set if you need to pass additional data to the OnServerGameTick
    //     method
    public virtual bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
    {
        if (GlobalConstants.MeltingFreezingEnabled && (this == snowCovered1 || this == snowCovered2 || this == snowCovered3) && world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays).Temperature > 4f)
        {
            extra = "melt";
            return true;
        }

        extra = null;
        return false;
    }

    //
    // Summary:
    //     Called by the main server thread if and only if this block returned true in ShouldReceiveServerGameTicks.
    //
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //     The position of this block
    //
    //   extra:
    //     The value set for the 'extra' parameter when ShouldReceiveGameTicks was called.
    public virtual void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
    {
        if (extra is string && (string)extra == "melt")
        {
            if (this == snowCovered3)
            {
                world.BlockAccessor.SetBlock(snowCovered2.Id, pos);
            }
            else if (this == snowCovered2)
            {
                world.BlockAccessor.SetBlock(snowCovered1.Id, pos);
            }
            else if (this == snowCovered1)
            {
                world.BlockAccessor.SetBlock(notSnowCovered.Id, pos);
            }
        }
    }

    public virtual void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
    {
        if (VertexFlags.WindMode == EnumWindBitMode.Leaves)
        {
            int verticesCount = decalMesh.VerticesCount;
            for (int i = 0; i < verticesCount; i++)
            {
                decalMesh.Flags[i] |= 100663296;
            }
        }
        else if (VertexFlags.WindMode == EnumWindBitMode.NormalWind)
        {
            decalMesh.SetWindFlag();
        }
    }

    //
    // Summary:
    //     If this block uses drawtype json, this method will be called everytime a chunk
    //     containing this block is tesselated.
    //
    // Parameters:
    //   sourceMesh:
    //
    //   lightRgbsByCorner:
    //     Emitted light from this block
    //
    //   pos:
    //
    //   chunkExtBlocks:
    //     Optional, fast way to look up a direct neighbouring block. This is an array of
    //     the current chunk blocks, also including all direct neighbours, so it's a 34
    //     x 34 x 34 block list. extIndex3d is the index of the current Block in this array.
    //     Use extIndex3d+TileSideEnum.MoveIndex[tileSide] to move around in the array.
    //
    //
    //   extIndex3d:
    //     See description of chunkExtBlocks
    public virtual void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
    {
        if (VertexFlags.WindMode == EnumWindBitMode.Leaves)
        {
            int verticesCount = sourceMesh.VerticesCount;
            for (int i = 0; i < verticesCount; i++)
            {
                sourceMesh.Flags[i] |= 100663296;
            }
        }
        else if (VertexFlags.WindMode == EnumWindBitMode.NormalWind)
        {
            sourceMesh.SetWindFlag(waveFlagMinY);
        }
    }

    //
    // Summary:
    //     Used as base position for particles.
    public virtual void DetermineTopMiddlePos()
    {
        if (CollisionBoxes != null && CollisionBoxes.Length != 0)
        {
            Cuboidf cuboidf = CollisionBoxes[0];
            TopMiddlePos.X = (cuboidf.X1 + cuboidf.X2) / 2f;
            TopMiddlePos.Y = cuboidf.Y2;
            TopMiddlePos.Z = (cuboidf.Z1 + cuboidf.Z2) / 2f;
            for (int i = 1; i < CollisionBoxes.Length; i++)
            {
                TopMiddlePos.Y = Math.Max(TopMiddlePos.Y, CollisionBoxes[i].Y2);
            }
        }
        else if (SelectionBoxes != null && SelectionBoxes.Length != 0)
        {
            Cuboidf cuboidf2 = SelectionBoxes[0];
            TopMiddlePos.X = (cuboidf2.X1 + cuboidf2.X2) / 2f;
            TopMiddlePos.Y = cuboidf2.Y2;
            TopMiddlePos.Z = (cuboidf2.Z1 + cuboidf2.Z2) / 2f;
            for (int j = 1; j < SelectionBoxes.Length; j++)
            {
                TopMiddlePos.Y = Math.Max(TopMiddlePos.Y, SelectionBoxes[j].Y2);
            }
        }
    }

    //
    // Summary:
    //     Used to determine if a block should be treated like air when placing blocks.
    //     (e.g. used for tallgrass)
    //
    // Parameters:
    //   block:
    public virtual bool IsReplacableBy(Block block)
    {
        bool flag = true;
        bool flag2 = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag3 = obj.IsReplacableBy(block, ref handling);
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

        if (IsLiquid() || Replaceable >= 6000)
        {
            return block.Replaceable < Replaceable;
        }

        return false;
    }

    //
    // Summary:
    //     Returns a horizontal and vertical orientation which should be used for oriented
    //     blocks like stairs during placement.
    //
    // Parameters:
    //   byPlayer:
    //
    //   blockSel:
    public static BlockFacing[] SuggestedHVOrientation(IPlayer byPlayer, BlockSelection blockSel)
    {
        BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
        double num = byPlayer.Entity.Pos.X + byPlayer.Entity.LocalEyePos.X - ((double)blockPos.X + blockSel.HitPosition.X);
        double num2 = byPlayer.Entity.Pos.Y + byPlayer.Entity.LocalEyePos.Y - ((double)blockPos.Y + blockSel.HitPosition.Y);
        double num3 = byPlayer.Entity.Pos.Z + byPlayer.Entity.LocalEyePos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
        float radians = (float)Math.Atan2(num, num3) + MathF.PI / 2f;
        double y = num2;
        float num4 = (float)Math.Sqrt(num * num + num3 * num3);
        float num5 = (float)Math.Atan2(y, num4);
        BlockFacing blockFacing = (((double)num5 < -Math.PI / 4.0) ? BlockFacing.DOWN : (((double)num5 > Math.PI / 4.0) ? BlockFacing.UP : null));
        BlockFacing blockFacing2 = BlockFacing.HorizontalFromAngle(radians);
        return new BlockFacing[2] { blockFacing2, blockFacing };
    }

    //
    // Summary:
    //     Called in the servers main thread
    //
    // Parameters:
    //   ba:
    //
    //   pos:
    //
    //   newBlock:
    //     The block as returned by your GetSnowLevelUpdateBlock() method
    //
    //   snowLevel:
    public virtual void PerformSnowLevelUpdate(IBulkBlockAccessor ba, BlockPos pos, Block newBlock, float snowLevel)
    {
        if (newBlock.Id != Id && (BlockMaterial == EnumBlockMaterial.Snow || BlockId == 0 || FirstCodePart() == newBlock.FirstCodePart()))
        {
            ba.ExchangeBlock(newBlock.Id, pos);
        }
    }

    //
    // Summary:
    //     Should return the snow covered block code for given snow level. Return null if
    //     snow cover is not supported for this block. If not overridden, it will check
    //     if Variant["cover"] exists and return its snow covered variant.
    //
    // Parameters:
    //   pos:
    //
    //   snowLevel:
    public virtual Block GetSnowCoveredVariant(BlockPos pos, float snowLevel)
    {
        if (snowCovered1 == null)
        {
            return null;
        }

        if (snowLevel >= 1f)
        {
            if (snowLevel >= 3f && snowCovered3 != null)
            {
                return snowCovered3;
            }

            if (snowLevel >= 2f && snowCovered2 != null)
            {
                return snowCovered2;
            }

            return snowCovered1;
        }

        if ((double)snowLevel < 0.1)
        {
            return notSnowCovered;
        }

        return this;
    }

    public virtual float GetSnowLevel(BlockPos pos)
    {
        return snowLevel;
    }

    //
    // Summary:
    //     Return a positive integer if the block retains heat (for warm rooms or greenhouses)
    //     or a negative integer if it preserves cool (for cellars)
    //
    // Parameters:
    //   pos:
    //
    //   facing:
    [Obsolete("Use GetRetention() instead")]
    public virtual int GetHeatRetention(BlockPos pos, BlockFacing facing)
    {
        return GetRetention(pos, facing, EnumRetentionType.Heat);
    }

    //
    // Summary:
    //     Return a positive integer if the block retains something, e.g. (for warm rooms
    //     or greenhouses) or a negative integer if something can pass through, e.g. cool
    //     for cellars
    //
    // Parameters:
    //   pos:
    //
    //   facing:
    //
    //   type:
    public virtual int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
    {
        bool flag = false;
        int result = 0;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            int retention = obj.GetRetention(pos, facing, type, ref handled);
            if (handled != 0)
            {
                flag = true;
                result = retention;
            }

            if (handled == EnumHandling.PreventSubsequent)
            {
                return retention;
            }
        }

        if (flag)
        {
            return result;
        }

        if (SideSolid[facing.Index])
        {
            if (type == EnumRetentionType.Sound)
            {
                return 10;
            }

            EnumBlockMaterial blockMaterial = GetBlockMaterial(api.World.BlockAccessor, pos);
            if (blockMaterial == EnumBlockMaterial.Ore || blockMaterial == EnumBlockMaterial.Stone || blockMaterial == EnumBlockMaterial.Soil || blockMaterial == EnumBlockMaterial.Ceramic)
            {
                return -1;
            }

            return 1;
        }

        return 0;
    }

    public virtual bool IsClimbable(BlockPos pos)
    {
        return Climbable;
    }

    //
    // Summary:
    //     The cost of traversing this block as part of the AI pathfinding system. Return
    //     a negative value to prefer traversal of a block, return a positive value to avoid
    //     traversal of this block. A value over 10000f is considered impassable. Default
    //     value is 0
    //
    // Parameters:
    //   pos:
    //
    //   creatureType:
    public virtual float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
    {
        if (creatureType == EnumAICreatureType.Humanoid)
        {
            return humanoidTraversalCost;
        }

        return (1f - WalkSpeedMultiplier) * (float)((creatureType == EnumAICreatureType.Humanoid) ? 5 : 2);
    }

    //
    // Summary:
    //     For any block that can be rotated, this method should be implemented to return
    //     the correct rotated block code. It is used by the world edit tool for allowing
    //     block data rotations
    //
    // Parameters:
    //   angle:
    public virtual AssetLocation GetRotatedBlockCode(int angle)
    {
        bool flag = false;
        AssetLocation result = Code;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            AssetLocation rotatedBlockCode = obj.GetRotatedBlockCode(angle, ref handling);
            if (handling != 0)
            {
                flag = true;
                result = rotatedBlockCode;
            }

            if (handling == EnumHandling.PreventSubsequent)
            {
                return rotatedBlockCode;
            }
        }

        if (flag)
        {
            return result;
        }

        return Code;
    }

    //
    // Summary:
    //     For any block that can be flipped upside down, this method should be implemented
    //     to return the correctly flipped block code. It is used by the world edit tool
    //     for allowing block data rotations
    public virtual AssetLocation GetVerticallyFlippedBlockCode()
    {
        bool flag = false;
        AssetLocation result = Code;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            AssetLocation verticallyFlippedBlockCode = obj.GetVerticallyFlippedBlockCode(ref handling);
            switch (handling)
            {
                case EnumHandling.PreventSubsequent:
                    return verticallyFlippedBlockCode;
                case EnumHandling.PassThrough:
                    continue;
            }

            flag = true;
            result = verticallyFlippedBlockCode;
        }

        if (flag)
        {
            return result;
        }

        return Code;
    }

    //
    // Summary:
    //     For any block that can be flipped vertically, this method should be implemented
    //     to return the correctly flipped block code. It is used by the world edit tool
    //     for allowing block data rotations
    //
    // Parameters:
    //   axis:
    public virtual AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
    {
        AssetLocation result = Code;
        bool flag = false;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            AssetLocation horizontallyFlippedBlockCode = obj.GetHorizontallyFlippedBlockCode(axis, ref handling);
            switch (handling)
            {
                case EnumHandling.PreventSubsequent:
                    return horizontallyFlippedBlockCode;
                case EnumHandling.PassThrough:
                    continue;
            }

            flag = true;
            result = horizontallyFlippedBlockCode;
        }

        if (flag)
        {
            return result;
        }

        return Code;
    }

    //
    // Summary:
    //     Returns the blocks behavior of given type, if it has such behavior
    //
    // Parameters:
    //   type:
    //
    //   withInheritance:
    public BlockBehavior GetBehavior(Type type, bool withInheritance)
    {
        if (withInheritance)
        {
            for (int i = 0; i < BlockBehaviors.Length; i++)
            {
                Type type2 = BlockBehaviors[i].GetType();
                if (type2 == type || type.IsAssignableFrom(type2))
                {
                    return BlockBehaviors[i];
                }
            }

            return null;
        }

        for (int j = 0; j < BlockBehaviors.Length; j++)
        {
            if (BlockBehaviors[j].GetType() == type)
            {
                return BlockBehaviors[j];
            }
        }

        return null;
    }

    //
    // Summary:
    //     Called by the block info HUD for display the interaction help besides the crosshair
    //
    //
    // Parameters:
    //   world:
    //
    //   selection:
    //
    //   forPlayer:
    public virtual WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        EnumHandling handling = EnumHandling.PassThrough;
        WorldInteraction[] array = new WorldInteraction[0];
        bool flag = true;
        if (world.Claims != null && world is IClientWorldAccessor clientWorldAccessor)
        {
            IClientPlayer player = clientWorldAccessor.Player;
            if (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Survival && world.Claims.TestAccess(clientWorldAccessor.Player, selection.Position, EnumBlockAccessFlags.BuildOrBreak) != 0)
            {
                flag = false;
            }
        }

        if (flag)
        {
            int num = 0;
            while (Drops != null && num < Drops.Length)
            {
                if (Drops[num].Tool.HasValue)
                {
                    EnumTool tool = Drops[num].Tool.Value;
                    array = array.Append(new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-collect",
                        MouseButton = EnumMouseButton.Left,
                        Itemstacks = ObjectCacheUtil.GetOrCreate(api, "blockhelp-collect-withtool-" + tool, delegate
                        {
                            List<ItemStack> list = new List<ItemStack>();
                            foreach (CollectibleObject collectible in api.World.Collectibles)
                            {
                                if (collectible.Tool == tool)
                                {
                                    list.Add(new ItemStack(collectible));
                                }
                            }

                            return list.ToArray();
                        })
                    });
                }

                num++;
            }
        }

        BlockBehavior[] blockBehaviors = BlockBehaviors;
        for (int i = 0; i < blockBehaviors.Length; i++)
        {
            WorldInteraction[] placedBlockInteractionHelp = blockBehaviors[i].GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling);
            array = array.Append(placedBlockInteractionHelp);
            if (handling == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }

        return array;
    }

    //
    // Summary:
    //     Called by the block info HUD for displaying the blocks name
    //
    // Parameters:
    //   world:
    //
    //   pos:
    public virtual string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(OnPickBlock(world, pos)?.GetName());
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        for (int i = 0; i < blockBehaviors.Length; i++)
        {
            blockBehaviors[i].GetPlacedBlockName(stringBuilder, world, pos);
        }

        return stringBuilder.ToString().TrimEnd();
    }

    //
    // Summary:
    //     Called by the block info HUD for displaying additional information
    public virtual string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (EntityClass != null)
        {
            BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
            if (blockEntity != null)
            {
                try
                {
                    blockEntity.GetBlockInfo(forPlayer, stringBuilder);
                }
                catch (Exception e)
                {
                    stringBuilder.AppendLine("(error in " + blockEntity.GetType().Name + ")");
                    api.Logger.Error(e);
                }
            }
        }

        if (Code == null)
        {
            stringBuilder.AppendLine("Unknown Block with ID " + BlockId);
            return stringBuilder.ToString();
        }

        string text = Code.Domain + ":" + ItemClass.ToString().ToLowerInvariant() + "desc-" + Code.Path;
        string matching = Lang.GetMatching(text);
        matching = ((matching != text) ? matching : "");
        stringBuilder.Append(matching);
        Block[] decors = world.BlockAccessor.GetDecors(pos);
        List<string> list = new List<string>();
        if (decors != null)
        {
            for (int i = 0; i < decors.Length; i++)
            {
                if (decors[i] != null)
                {
                    AssetLocation code = decors[i].Code;
                    string matching2 = Lang.GetMatching(code.Domain + ":" + ItemClass.ToString().ToLowerInvariant() + "-" + code.Path);
                    list.Add(Lang.Get("block-with-decorname", matching2));
                }
            }
        }

        stringBuilder.AppendLine(string.Join("\r\n", list.Distinct()));
        if (RequiredMiningTier > 0 && api.World.Claims.TestAccess(forPlayer, pos, EnumBlockAccessFlags.BuildOrBreak) == EnumWorldAccessResponse.Granted)
        {
            AddMiningTierInfo(stringBuilder);
        }

        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior blockBehavior in blockBehaviors)
        {
            stringBuilder.Append(blockBehavior.GetPlacedBlockInfo(world, pos, forPlayer));
        }

        return stringBuilder.ToString().TrimEnd();
    }

    public virtual void AddMiningTierInfo(StringBuilder sb)
    {
        string text = "?";
        if (RequiredMiningTier < miningTierNames.Length)
        {
            text = miningTierNames[RequiredMiningTier];
        }

        sb.AppendLine(Lang.Get("Requires tool tier {0} ({1}) to break", RequiredMiningTier, (text == "?") ? text : Lang.Get(text)));
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
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        ItemStack itemstack = inSlot.Itemstack;
        if (DrawType == EnumDrawType.SurfaceLayer)
        {
            dsc.AppendLine(Lang.Get("Decor layer block"));
        }

        EnumBlockMaterial blockMaterial = GetBlockMaterial(world.BlockAccessor, null, itemstack);
        dsc.AppendLine(Lang.Get("Material: ") + Lang.Get("blockmaterial-" + blockMaterial));
        AddExtraHeldItemInfoPostMaterial(inSlot, dsc, world);
        byte[] lightHsv = GetLightHsv(world.BlockAccessor, null, itemstack);
        dsc.Append((!withDebugInfo) ? "" : ((lightHsv[2] > 0) ? (Lang.Get("light-hsv") + lightHsv[0] + ", " + lightHsv[1] + ", " + lightHsv[2] + "\n") : ""));
        dsc.Append(withDebugInfo ? "" : ((lightHsv[2] > 0) ? (Lang.Get("light-level") + lightHsv[2] + "\n") : ""));
        if (WalkSpeedMultiplier != 1f)
        {
            dsc.Append(Lang.Get("walk-multiplier") + WalkSpeedMultiplier + "\n");
        }

        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior blockBehavior in blockBehaviors)
        {
            dsc.Append(blockBehavior.GetHeldBlockInfo(world, inSlot));
        }

        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
    }

    //
    // Summary:
    //     Opportunity for blocks to add additional lines to the Held Item info _prior to_
    //     the behaviors output (such as nutrition properties or block reinforcement)
    //
    // Parameters:
    //   inSlot:
    //
    //   dsc:
    //
    //   world:
    public virtual void AddExtraHeldItemInfoPostMaterial(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
    {
    }

    //
    // Summary:
    //     If true, the player can select invdividual selection boxes of this block
    //
    // Parameters:
    //   world:
    //
    //   pos:
    public virtual bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
    {
        return PartialSelection;
    }

    //
    // Parameters:
    //   capi:
    //
    //   pos:
    public virtual Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos)
    {
        return new Vec4f(0f, 0f, 0f, 0.5f);
    }

    //
    // Summary:
    //     Called by the texture atlas manager when building up the block atlas. Has to
    //     add all of the blocks texture
    //
    // Parameters:
    //   api:
    //
    //   textureDict:
    public virtual void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
    {
        (Textures as TextureDictionary).BakeAndCollect(api.Assets, textureDict, Code, "Baked variant of block ");
        (TexturesInventory as TextureDictionary).BakeAndCollect(api.Assets, textureDict, Code, "Baked inventory variant of block ");
        foreach (KeyValuePair<string, CompositeTexture> texture in Textures)
        {
            AssetLocation anyWildCardNoFiles = texture.Value.AnyWildCardNoFiles;
            if (anyWildCardNoFiles != null)
            {
                api.Logger.Warning("Block {0} defines a wildcard texture {1} (or one of its alternates), key {2}, but no matching texture found", Code, anyWildCardNoFiles, texture.Key);
            }
        }
    }

    //
    // Summary:
    //     Should return the blocks blast resistance. Default behavior is to return BlockMaterialUtil.MaterialBlastResistance(blastType,
    //     BlockMaterial);
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   blastDirectionVector:
    //
    //   blastType:
    public virtual double GetBlastResistance(IWorldAccessor world, BlockPos pos, Vec3f blastDirectionVector, EnumBlastType blastType)
    {
        if (blastType == EnumBlastType.RockBlast)
        {
            return Math.Min(BlockMaterialUtil.MaterialBlastResistance(EnumBlastType.RockBlast, GetBlockMaterial(world.BlockAccessor, pos)), BlockMaterialUtil.MaterialBlastResistance(EnumBlastType.OreBlast, GetBlockMaterial(world.BlockAccessor, pos)));
        }

        return BlockMaterialUtil.MaterialBlastResistance(blastType, GetBlockMaterial(world.BlockAccessor, pos));
    }

    //
    // Summary:
    //     Should return the chance of the block dropping its upon upon being exploded.
    //     Default behavior is to return BlockMaterialUtil.MaterialBlastDropChances(blastType,
    //     BlockMaterial);
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   blastType:
    public virtual double ExplosionDropChance(IWorldAccessor world, BlockPos pos, EnumBlastType blastType)
    {
        return BlockMaterialUtil.MaterialBlastDropChances(blastType, GetBlockMaterial(world.BlockAccessor, pos));
    }

    //
    // Summary:
    //     Called when the block was blown up by explosives
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   explosionCenter:
    //
    //   blastType:
    public virtual void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
    {
        EnumHandling handling = EnumHandling.PassThrough;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        for (int i = 0; i < blockBehaviors.Length; i++)
        {
            blockBehaviors[i].OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);
            if (handling == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }

        if (handling == EnumHandling.PreventDefault)
        {
            return;
        }

        world.BulkBlockAccessor.SetBlock(0, pos);
        double num = ExplosionDropChance(world, pos, blastType);
        if (world.Rand.NextDouble() < num)
        {
            ItemStack[] drops = GetDrops(world, pos, null);
            int num2 = 0;
            while (drops != null && num2 < drops.Length)
            {
                if (SplitDropStacks)
                {
                    for (int j = 0; j < drops[num2].StackSize; j++)
                    {
                        ItemStack itemStack = drops[num2].Clone();
                        itemStack.StackSize = 1;
                        world.SpawnItemEntity(itemStack, pos);
                    }
                }
                else
                {
                    world.SpawnItemEntity(drops[num2].Clone(), pos);
                }

                num2++;
            }
        }

        if (EntityClass != null)
        {
            world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken();
        }
    }

    //
    // Summary:
    //     Should return the color to be used for the block particle coloring
    //
    // Parameters:
    //   capi:
    //
    //   pos:
    //
    //   facing:
    //
    //   rndIndex:
    public virtual int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
    {
        if (Textures == null || Textures.Count == 0)
        {
            return 0;
        }

        if (!Textures.TryGetValue(facing.Code, out var value))
        {
            value = Textures.First().Value;
        }

        if (value?.Baked == null)
        {
            return 0;
        }

        int num = capi.BlockTextureAtlas.GetRandomColor(value.Baked.TextureSubId, rndIndex);
        if (ClimateColorMapResolved != null || SeasonColorMapResolved != null)
        {
            num = capi.World.ApplyColorMapOnRgba(ClimateColorMapResolved, SeasonColorMapResolved, num, pos.X, pos.Y, pos.Z);
        }

        return num;
    }

    //
    // Summary:
    //     Should return a random pixel within the items/blocks texture
    //
    // Parameters:
    //   capi:
    //
    //   stack:
    public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
    {
        if (TextureSubIdForBlockColor < 0)
        {
            return -1;
        }

        return capi.BlockTextureAtlas.GetRandomColor(TextureSubIdForBlockColor);
    }

    //
    // Summary:
    //     Should return an RGB color for this block. Current use: In the world map. Default
    //     behavior: The 2 averaged pixels at 40%/40% ad 60%/60% position
    //
    // Parameters:
    //   capi:
    //
    //   pos:
    public virtual int GetColor(ICoreClientAPI capi, BlockPos pos)
    {
        int num = GetColorWithoutTint(capi, pos);
        if (ClimateColorMapResolved != null || SeasonColorMapResolved != null)
        {
            num = capi.World.ApplyColorMapOnRgba(ClimateColorMapResolved, SeasonColorMapResolved, num, pos.X, pos.Y, pos.Z, flipRb: false);
        }

        return num;
    }

    //
    // Summary:
    //     Tint less version of GetColor. Used for map color export
    //
    // Parameters:
    //   capi:
    //
    //   pos:
    public virtual int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
    {
        Block block = (HasBehavior("Decor", api.ClassRegistry) ? null : capi.World.BlockAccessor.GetDecor(pos, new DecorBits(BlockFacing.UP)));
        if (block != null && block != this)
        {
            return block.GetColorWithoutTint(capi, pos);
        }

        if (TextureSubIdForBlockColor < 0)
        {
            return -1;
        }

        return capi.BlockTextureAtlas.GetAverageColor(TextureSubIdForBlockColor);
    }

    public virtual bool AllowSnowCoverage(IWorldAccessor world, BlockPos blockPos)
    {
        return SideSolid[BlockFacing.UP.Index];
    }

    //
    // Summary:
    //     Alias of api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as T
    //
    // Parameters:
    //   blockSel:
    //
    // Type parameters:
    //   T:
    public virtual T GetBlockEntity<T>(BlockSelection blockSel) where T : BlockEntity
    {
        return api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as T;
    }

    //
    // Summary:
    //     Alias of api.World.BlockAccessor.GetBlockEntity(position) as T
    //
    // Parameters:
    //   position:
    //
    // Type parameters:
    //   T:
    public virtual T GetBlockEntity<T>(BlockPos position) where T : BlockEntity
    {
        return api.World.BlockAccessor.GetBlockEntity(position) as T;
    }

    //
    // Summary:
    //     Alias of api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<T>()
    //
    // Parameters:
    //   pos:
    //
    // Type parameters:
    //   T:
    public virtual T GetBEBehavior<T>(BlockPos pos) where T : BlockEntityBehavior
    {
        BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(pos);
        if (blockEntity == null)
        {
            return null;
        }

        return blockEntity.GetBehavior<T>();
    }

    //
    // Summary:
    //     Returns instance of class that implements this interface in the following order
    //
    //     1. Block (returns itself)
    //     2. BlockBehavior (returns on of our own behavior)
    //     3. BlockEntity
    //     4. BlockEntityBehavior
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    // Type parameters:
    //   T:
    public virtual T GetInterface<T>(IWorldAccessor world, BlockPos pos) where T : class
    {
        if (this is T result)
        {
            return result;
        }

        BlockBehavior behavior = GetBehavior(typeof(T), withInheritance: true);
        if (behavior != null)
        {
            return behavior as T;
        }

        if (pos == null)
        {
            return null;
        }

        BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
        if (blockEntity is T result2)
        {
            return result2;
        }

        if (blockEntity != null)
        {
            T behavior2 = blockEntity.GetBehavior<T>();
            if (behavior2 != null)
            {
                return behavior2;
            }
        }

        return null;
    }

    //
    // Summary:
    //     Creates a deep copy of the block
    public Block Clone()
    {
        Block block = (Block)MemberwiseClone();
        block.Code = Code.Clone();
        if (MiningSpeed != null)
        {
            block.MiningSpeed = new Dictionary<EnumBlockMaterial, float>(MiningSpeed);
        }

        if (Textures is FastSmallDictionary<string, CompositeTexture> fastSmallDictionary)
        {
            block.Textures = fastSmallDictionary.Clone();
        }
        else
        {
            block.Textures = new FastSmallDictionary<string, CompositeTexture>(Textures.Count);
            foreach (KeyValuePair<string, CompositeTexture> texture in Textures)
            {
                block.Textures[texture.Key] = texture.Value.Clone();
            }
        }

        if (TexturesInventory is FastSmallDictionary<string, CompositeTexture> fastSmallDictionary2)
        {
            block.TexturesInventory = fastSmallDictionary2.Clone();
        }
        else
        {
            block.TexturesInventory = new Dictionary<string, CompositeTexture>();
            foreach (KeyValuePair<string, CompositeTexture> item in TexturesInventory)
            {
                block.TexturesInventory[item.Key] = item.Value.Clone();
            }
        }

        block.Shape = Shape.Clone();
        if (LightHsv != null)
        {
            block.LightHsv = (byte[])LightHsv.Clone();
        }

        if (ParticleProperties != null)
        {
            block.ParticleProperties = new AdvancedParticleProperties[ParticleProperties.Length];
            for (int i = 0; i < ParticleProperties.Length; i++)
            {
                block.ParticleProperties[i] = ParticleProperties[i].Clone();
            }
        }

        if (Drops != null)
        {
            block.Drops = new BlockDropItemStack[Drops.Length];
            for (int j = 0; j < Drops.Length; j++)
            {
                block.Drops[j] = Drops[j].Clone();
            }
        }

        if (SideOpaque != null)
        {
            block.SideOpaque = (bool[])SideOpaque.Clone();
        }

        block.SideSolid = SideSolid;
        if (SideAo != null)
        {
            block.SideAo = (bool[])SideAo.Clone();
        }

        if (CombustibleProps != null)
        {
            block.CombustibleProps = CombustibleProps.Clone();
        }

        if (NutritionProps != null)
        {
            block.NutritionProps = NutritionProps.Clone();
        }

        if (GrindingProps != null)
        {
            block.GrindingProps = GrindingProps.Clone();
        }

        if (Attributes != null)
        {
            block.Attributes = Attributes.Clone();
        }

        return block;
    }

    //
    // Summary:
    //     Returns true if the block has given block behavior
    //
    // Parameters:
    //   withInheritance:
    //
    // Type parameters:
    //   T:
    public bool HasBlockBehavior<T>(bool withInheritance = false) where T : BlockBehavior
    {
        return (T)GetCollectibleBehavior(typeof(T), withInheritance) != null;
    }

    //
    // Summary:
    //     Returns true if the block has given block behavior OR collectible behavior
    //
    // Parameters:
    //   withInheritance:
    //
    // Type parameters:
    //   T:
    public override bool HasBehavior<T>(bool withInheritance = false)
    {
        return HasBehavior(typeof(T), withInheritance);
    }

    public override bool HasBehavior(string type, IClassRegistryAPI classRegistry)
    {
        if (GetBehavior(classRegistry.GetCollectibleBehaviorClass(type), withInheritance: false) == null)
        {
            return GetBehavior(classRegistry.GetBlockBehaviorClass(type)) != null;
        }

        return true;
    }

    public override bool HasBehavior(Type type, bool withInheritance = false)
    {
        if (GetBehavior(CollectibleBehaviors, type, withInheritance) == null)
        {
            CollectibleBehavior[] blockBehaviors = BlockBehaviors;
            return GetBehavior(blockBehaviors, type, withInheritance) != null;
        }

        return true;
    }

    internal void EnsureValidTextures(ILogger logger)
    {
        List<string> list = null;
        int num = 0;
        foreach (KeyValuePair<string, CompositeTexture> texture in Textures)
        {
            if (texture.Value.Base == null)
            {
                logger.Error("The texture definition {0} for #{2} in block with code {1} is invalid. The base property is null. Will skip.", num, Code, texture.Key);
                if (list == null)
                {
                    list = new List<string>();
                }

                list.Add(texture.Key);
            }

            num++;
        }

        if (list == null)
        {
            return;
        }

        foreach (string item in list)
        {
            Textures.Remove(item);
        }
    }

    //
    // Summary:
    //     Return a decimal between 0.0 and 1.0 indicating - if this block is solid enough
    //     to block liquid flow on that side - how high the barrier is
    public virtual float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
    {
        bool flag = false;
        float result = 0f;
        BlockBehavior[] blockBehaviors = BlockBehaviors;
        foreach (BlockBehavior obj in blockBehaviors)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            float liquidBarrierHeightOnSide = obj.GetLiquidBarrierHeightOnSide(face, pos, ref handled);
            if (handled != 0)
            {
                flag = true;
                result = liquidBarrierHeightOnSide;
            }

            if (handled == EnumHandling.PreventSubsequent)
            {
                return liquidBarrierHeightOnSide;
            }
        }

        if (flag)
        {
            return result;
        }

        if (liquidBarrierHeightonSide == null)
        {
            liquidBarrierHeightonSide = new float[6];
            for (int j = 0; j < 6; j++)
            {
                liquidBarrierHeightonSide[j] = (SideSolid.OnSide(BlockFacing.ALLFACES[j]) ? 1f : 0f);
            }

            float[] array = Attributes?["liquidBarrierOnSides"].AsArray<float>();
            int num = 0;
            while (array != null && num < array.Length)
            {
                liquidBarrierHeightonSide[num] = array[num];
                num++;
            }
        }

        return liquidBarrierHeightonSide[face.Index];
    }

    //
    // Summary:
    //     Simple string representation for debugging
    public override string ToString()
    {
        return Code.Domain + ":block " + Code.Path + "/" + BlockId;
    }

    public virtual void FreeRAMServer()
    {
        ShapeInventory = null;
        Lod0Shape = null;
        Lod2Shape = null;
        Textures = null;
        TexturesInventory = null;
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
