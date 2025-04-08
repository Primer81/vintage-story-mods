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

/// <summary>
/// Basic class for a placeable block
/// </summary>
public class Block : CollectibleObject
{
	public static readonly bool[] DefaultSideOpaque = new bool[6] { true, true, true, true, true, true };

	public static readonly bool[] DefaultSideAo = new bool[6] { true, true, true, true, true, true };

	public static readonly CompositeShape DefaultCubeShape = new CompositeShape
	{
		Base = new AssetLocation("block/basic/cube")
	};

	public static readonly string[] DefaultAllowAllSpawns = new string[1] { "*" };

	/// <summary>
	/// Default Full Block Collision Box
	/// </summary>
	public static Cuboidf DefaultCollisionBox = new Cuboidf(0f, 0f, 0f, 1f, 1f, 1f);

	/// <summary>
	/// Default Collision boxes (and also Selection boxes) array containing just the Default Collision Box
	/// This is standard for most solid blocks in the game. Since it is in practice immutable, all blocks can use a single copy of the same array
	/// This will help both RAM performance (avoids duplicate copies) and physics tick performance (this commonly accessed object can be well cached)
	/// </summary>
	public static readonly Cuboidf[] DefaultCollisionSelectionBoxes = new Cuboidf[1] { DefaultCollisionBox };

	/// <summary>
	/// Unique number of the block. Same as <see cref="P:Vintagestory.API.Common.Block.Id" />. This number depends on the order in which the blocks are order. The numbering is however always ensured to remain the same on a per world basis.
	/// </summary>
	public int BlockId;

	/// <summary>
	/// If not set to JSON it will use an efficient hardcoded model
	/// </summary>
	public EnumDrawType DrawType = EnumDrawType.JSON;

	/// <summary>
	/// During which render pass this block should be rendered
	/// </summary>
	public EnumChunkRenderPass RenderPass;

	/// <summary>
	/// Currently not used
	/// </summary>
	public bool Ambientocclusion = true;

	/// <summary>
	/// Walk speed when standing or inside this block
	/// </summary>
	public float WalkSpeedMultiplier = 1f;

	/// <summary>
	/// Drag multiplier applied to entities standing on it
	/// </summary>
	public float DragMultiplier = 1f;

	/// <summary>
	/// If true, players can target individual selection boxes of the block
	/// </summary>
	public bool PartialSelection;

	/// <summary>
	/// The sounds played for this block during step, break, build and walk. Use GetSounds() to query if not performance critical.
	/// </summary>
	public BlockSounds Sounds;

	/// <summary>
	/// Data thats passed on to the graphics card for every vertex of the blocks model
	/// </summary>
	public VertexFlags VertexFlags;

	/// <summary>
	/// A bit uploaded to the shader to add a frost overlay below freezing temperature
	/// </summary>
	public bool Frostable;

	/// <summary>
	/// For light blocking blocks. Any value above 32 will completely block all light.
	/// </summary>
	public int LightAbsorption;

	/// <summary>
	/// If true, when the player holds the sneak key and right clicks this block, calls the blocks OnBlockInteractStart first, the items OnHeldInteractStart second. Without it the order is reversed.
	/// </summary>
	public bool PlacedPriorityInteract;

	/// <summary>
	/// A value usually between 0-9999 that indicates which blocks may be replaced with others.
	/// - Any block with replaceable value above 5000 will be washed away by water
	/// - Any block with replaceable value above 6000 will replaced when the player tries to place a block
	/// Examples:
	/// 0 = Bedrock
	/// 6000 = Tallgrass
	/// 9000 = Lava
	/// 9500 = Water
	/// 9999 = Air
	/// </summary>
	public int Replaceable;

	/// <summary>
	/// 0 = nothing can grow, 10 = some tallgrass and small trees can be grow on it, 100 = all grass and trees can grow on it
	/// </summary>
	public int Fertility;

	/// <summary>
	/// The mining tier required to break this block
	/// </summary>
	public int RequiredMiningTier;

	/// <summary>
	/// How long it takes to break this block in seconds. Use GetResistance() to query if not performance critical.
	/// </summary>
	public float Resistance = 2f;

	/// <summary>
	/// A way to categorize blocks. Used for getting the mining speed for each tool type, amongst other things. Use GetBlockMaterial() to query if not performance critical.
	/// </summary>
	public EnumBlockMaterial BlockMaterial = EnumBlockMaterial.Stone;

	/// <summary>
	/// Random texture selection - whether or not to use the Y axis during randomization (for multiblock plants)
	/// </summary>
	public EnumRandomizeAxes RandomizeAxes;

	/// <summary>
	/// If true then the block will be randomly offseted by 1/3 of a block when placed
	/// </summary>
	public int RandomDrawOffset;

	public bool RandomizeRotations;

	public float RandomSizeAdjust;

	/// <summary>
	/// If true, the block will render with a UV offset enabling it to use the "other half" of a 64 x 64 texture on each alternate block position  (e.g. Redwood trunk)
	/// </summary>
	public bool alternatingVOffset;

	/// <summary>
	/// Bit flags for the direction in which the alternatingVOffset is to be applied e.g. 0x30 to apply alternatingVOffset as the y position moves up and down
	/// </summary>
	public int alternatingVOffsetFaces;

	/// <summary>
	/// The block shape to be used when displayed in the inventory GUI, held in hand or dropped on the ground
	/// <br />Note: from game version 1.20.4, this is <b>null on server-side</b> (except during asset loading start-up stage)
	/// </summary>
	public CompositeShape ShapeInventory;

	/// <summary>
	/// The default json block shape to be used when drawtype==JSON
	/// </summary>
	public CompositeShape Shape = DefaultCubeShape;

	/// <summary>
	/// The additional shape elements seen only at close distance ("LOD0"). For example, see leaves
	/// <br />Note: from game version 1.20.4, this is <b>null on server-side</b> (except during asset loading start-up stage)
	/// </summary>
	public CompositeShape Lod0Shape;

	/// <summary>
	/// The alternative simplified shape seen at far distance ("LOD2"). For example, see flowers
	/// <br />Note: from game version 1.20.4, this is <b>null on server-side</b> (except during asset loading start-up stage)
	/// </summary>
	public CompositeShape Lod2Shape;

	public MeshData Lod0Mesh;

	public MeshData Lod2Mesh;

	public bool DoNotRenderAtLod2;

	/// <summary>
	/// Default textures to be used for this block. The Dictionary keys are the texture short names, as referenced in this block's shape ShapeElementFaces
	/// <br />(may be null on clients, prior to receipt of server assets)
	/// <br />Note: from game version 1.20.4, this is <b>null on server-side</b> (except during asset loading start-up stage)
	/// </summary>
	public IDictionary<string, CompositeTexture> Textures;

	/// <summary>
	/// Fast array of texture variants, for use by cube (or similar) tesselators if the block has alternate shapes
	/// The outer array is indexed based on the 6 BlockFacing.Index numerals; the inner array is the variants
	/// </summary>
	public BakedCompositeTexture[][] FastTextureVariants;

	/// <summary>
	/// Textures to be used for this block in the inventory GUI, held in hand or dropped on the ground
	/// <br />(may be null on clients, prior to receipt of server assets)
	/// <br />Note: from game version 1.20.4, this is <b>null on server-side</b> (except during asset loading start-up stage)
	/// </summary>
	public IDictionary<string, CompositeTexture> TexturesInventory;

	/// <summary>
	/// Defines which of the 6 block sides are completely opaque. Used to determine which block faces can be culled during tesselation.
	/// </summary>
	public bool[] SideOpaque = DefaultSideOpaque;

	/// <summary>
	/// Defines which of the 6 block side are solid. Used to determine if attachable blocks can be attached to this block. Also used to determine if snow can rest on top of this block.
	/// </summary>
	public SmallBoolArray SideSolid = new SmallBoolArray(63);

	/// <summary>
	/// Defines which of the 6 block side should be shaded with ambient occlusion
	/// </summary>
	public bool[] SideAo = DefaultSideAo;

	/// <summary>
	/// Defines which of the 6 block neighbours should receive AO if this block is in front of them
	/// </summary>
	public byte EmitSideAo;

	/// <summary>
	/// Defines what creature groups may spawn on this block
	/// </summary>
	public string[] AllowSpawnCreatureGroups = DefaultAllowAllSpawns;

	public bool AllCreaturesAllowed;

	/// <summary>
	/// Determines which sides of the blocks should be rendered
	/// </summary>
	public EnumFaceCullMode FaceCullMode;

	/// <summary>
	/// The color map for climate color mapping. Leave null for no coloring by climate
	/// </summary>
	public string ClimateColorMap;

	public ColorMap ClimateColorMapResolved;

	/// <summary>
	/// The color map for season color mapping. Leave null for no coloring by season
	/// </summary>
	public string SeasonColorMap;

	public ColorMap SeasonColorMapResolved;

	/// <summary>
	/// Internal value that's set during if the block shape has any tint indexes for use in chunk tesselation and stuff O_O
	/// </summary>
	public bool ShapeUsesColormap;

	public bool LoadColorMapAnyway;

	/// <summary>
	/// Three extra color / season bits which may have meaning for specific blocks, such as leaves
	/// </summary>
	public int ExtraColorBits;

	/// <summary>
	/// Defines the area with which the player character collides with.
	/// </summary>
	public Cuboidf[] CollisionBoxes = DefaultCollisionSelectionBoxes;

	/// <summary>
	/// Defines the area which the players mouse pointer collides with for selection.
	/// </summary>
	public Cuboidf[] SelectionBoxes = DefaultCollisionSelectionBoxes;

	/// <summary>
	/// Defines the area with which particles collide with (if null, will be the same as CollisionBoxes).
	/// </summary>
	public Cuboidf[] ParticleCollisionBoxes;

	/// <summary>
	/// Used for ladders. If true, walking against this blocks collisionbox will make the player climb
	/// </summary>
	public bool Climbable;

	/// <summary>
	/// Will be used for not rendering rain below this block
	/// </summary>
	public bool RainPermeable;

	/// <summary>
	/// Value between 0..7 for Liquids to determine the height of the liquid
	/// </summary>
	public int LiquidLevel;

	/// <summary>
	/// If this block is or contains a liquid, this should be the code (or "identifier") of the liquid
	/// </summary>
	/// <returns></returns>
	public string LiquidCode;

	/// <summary>
	/// A flag set during texture block shape tesselation
	/// </summary>
	public bool HasAlternates;

	public bool HasTiles;

	/// <summary>
	/// Modifiers that can alter the behavior of a block, particularly when being placed or removed
	/// </summary>
	public BlockBehavior[] BlockBehaviors = new BlockBehavior[0];

	/// <summary>
	/// Modifiers that can alter the behavior of a block entity
	/// </summary>
	public BlockEntityBehaviorType[] BlockEntityBehaviors = new BlockEntityBehaviorType[0];

	/// <summary>
	/// The items that should drop from breaking this block
	/// </summary>
	public BlockDropItemStack[] Drops;

	/// <summary>
	/// If true, a blocks drops will be split into stacks of stacksize 1 for more game juice. This field is only used in OnBlockBroken() and OnBlockExploded()
	/// </summary>
	public bool SplitDropStacks = true;

	/// <summary>
	/// Information about the blocks as a crop
	/// </summary>
	public BlockCropProperties CropProps;

	/// <summary>
	/// If this block has a block entity attached to it, this will store it's code
	/// </summary>
	public string EntityClass;

	public bool CustomBlockLayerHandler;

	public bool CanStep = true;

	public bool AllowStepWhenStuck;

	/// <summary>
	/// To allow Decor Behavior settings to be accessed through the Block API.  See DecorFlags class for interpretation.
	/// </summary>
	public byte decorBehaviorFlags;

	/// <summary>
	/// Used to adjust selection box of parent block
	/// </summary>
	public float DecorThickness;

	public float InteractionHelpYOffset = 0.9f;

	public int TextureSubIdForBlockColor = -1;

	private float humanoidTraversalCost;

	/// <summary>
	/// To tell the JsonTesselator the offset to use when checking whether this is being rendered in/on ice
	/// (Currently only implemented by BlockWaterLily, compare seaweed and other water plants which check whether the block they are inside is ice, so their IceCheckOffset has the default value of 0)
	/// </summary>
	public int IceCheckOffset;

	protected static string[] miningTierNames = new string[7] { "tier_hands", "tier_stone", "tier_copper", "tier_bronze", "tier_iron", "tier_steel", "tier_titanium" };

	public Block notSnowCovered;

	public Block snowCovered1;

	public Block snowCovered2;

	public Block snowCovered3;

	public float snowLevel;

	protected float waveFlagMinY = 0.5625f;

	private float[] liquidBarrierHeightonSide;

	/// <summary>
	/// Returns the block id
	/// </summary>
	public override int Id => BlockId;

	/// <summary>
	/// Returns EnumItemClass.Block
	/// </summary>
	public override EnumItemClass ItemClass => EnumItemClass.Block;

	/// <summary>
	/// Return true if this block should be stored in the fluids layer in chunks instead of the solid blocks layer (e.g. water, flowing water, lake ice)
	/// </summary>
	public virtual bool ForFluidsLayer => false;

	/// <summary>
	/// Return non-null if this block should have water (or ice) placed in its position in the fluids layer when updating from 1.16 to 1.17
	/// </summary>
	public virtual string RemapToLiquidsLayer => null;

	/// <summary>
	/// Returns the first textures in the TexturesInventory dictionary
	/// </summary>
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

	/// <summary>
	/// Entity pushing while an entity is inside this block. Read from attributes because i'm lazy.
	/// </summary>
	public Vec3d PushVector { get; set; }

	public virtual string ClimateColorMapForMap => ClimateColorMap;

	public virtual string SeasonColorMapForMap => SeasonColorMap;

	/// <summary>
	/// Sets the whole SideOpaque array to true
	/// </summary>
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

	/// <summary>
	/// Creates a new instance of a block with null model transforms; BlockTypeNet will add default transforms client-side if they are null in the BlockType packet; transforms should not be needed on a server
	/// </summary>
	public Block()
	{
	}

	/// <summary>
	/// Called when this block was loaded by the server or the client
	/// </summary>
	/// <param name="api"></param>
	public override void OnLoaded(ICoreAPI api)
	{
		humanoidTraversalCost = (Attributes?["humanoidTraversalCost"]?.AsFloat(1f)).GetValueOrDefault(1f);
		PushVector = Attributes?["pushVector"]?.AsObject<Vec3d>();
		AllowStepWhenStuck = (Attributes?["allowStepWhenStuck"]?.AsBool()).GetValueOrDefault();
		CanStep = Attributes?["canStep"].AsBool(defaultValue: true) ?? true;
		base.OnLoaded(api);
		string coverVariant = Variant["cover"];
		if (coverVariant != null && (coverVariant == "free" || coverVariant.Contains("snow")))
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
		string code = Attributes?["textureCodeForBlockColor"].AsString();
		if (code != null && Textures.TryGetValue(code, out var compoTex))
		{
			TextureSubIdForBlockColor = compoTex.Baked.TextureSubId;
		}
		if (TextureSubIdForBlockColor < 0)
		{
			if (Textures.TryGetValue("up", out var upTexture))
			{
				TextureSubIdForBlockColor = upTexture.Baked.TextureSubId;
			}
			else if (Textures.Count > 0)
			{
				TextureSubIdForBlockColor = (Textures.First().Value?.Baked?.TextureSubId).GetValueOrDefault();
			}
		}
	}

	/// <summary>
	/// Called for example when the player places a block inside a liquid block. Needs to return true if the liquid should get removed.
	/// </summary>
	/// <param name="blockAccess"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual bool DisplacesLiquids(IBlockAccessor blockAccess, BlockPos pos)
	{
		return SideSolid.SidesAndBase;
	}

	/// <summary>
	/// Does the side APPEAR fully solid?  Called for example when deciding to render water edges at a position, or not
	/// Note: Worldgen code uses the blockAccessor-aware overload of this method
	/// </summary>
	public virtual bool SideIsSolid(BlockPos pos, int faceIndex)
	{
		return SideSolid[faceIndex];
	}

	/// <summary>
	/// Is the side solid or almost fully solid (in the case of chiselled blocks)?  Called for example when deciding to place loose stones or boulders above this during worldgen
	/// </summary>
	public virtual bool SideIsSolid(IBlockAccessor blockAccess, BlockPos pos, int faceIndex)
	{
		return SideIsSolid(pos, faceIndex);
	}

	/// <summary>
	/// This method gets called when facecull mode is set to 'Callback'. Curently used for custom behaviors when merging ice
	/// </summary>
	/// <param name="facingIndex">The index of the BlockFacing face of this block being tested</param>
	/// <param name="neighbourBlock">The neighbouring block</param>
	/// <param name="intraChunkIndex3d">The position index within the chunk (z * 32 * 32 + y * 32 + x): the BlockEntity can be obtained using this if necessary</param>
	/// <returns></returns>
	public virtual bool ShouldMergeFace(int facingIndex, Block neighbourBlock, int intraChunkIndex3d)
	{
		return false;
	}

	/// <summary>
	/// The cuboid used to determine where to spawn particles when breaking the block
	/// </summary>
	/// <param name="blockAccess"></param>
	/// <param name="pos"></param>
	/// <param name="facing"></param>
	/// <returns></returns>
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
		Cuboidf[] boxes = GetCollisionBoxes(blockAccess, pos);
		if (boxes == null || boxes.Length == 0)
		{
			boxes = GetSelectionBoxes(blockAccess, pos);
		}
		if (boxes == null || boxes.Length == 0)
		{
			return null;
		}
		return boxes[0];
	}

	/// <summary>
	/// Returns the blocks selection boxes at this position in the world.
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
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
		IWorldChunk chunk = blockAccessor.GetChunkAtBlockPos(pos);
		if (chunk == null)
		{
			return SelectionBoxes;
		}
		return chunk.AdjustSelectionBoxForDecor(blockAccessor, pos, SelectionBoxes);
	}

	/// <summary>
	/// Returns the blocks collision box. Warning: This method may get called by different threads, so it has to be thread safe.
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return CollisionBoxes;
	}

	/// <summary>
	/// Returns the blocks particle collision box. Warning: This method may get called by different threads, so it has to be thread safe.
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return ParticleCollisionBoxes ?? CollisionBoxes;
	}

	/// <summary>
	/// Should return the blocks material
	/// Warning: This method is may get called in a background thread. Please make sure your code in here is thread safe.
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="pos">May be null and therfore stack is non-null</param>
	/// <param name="stack"></param>
	/// <returns></returns>
	public virtual EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		return BlockMaterial;
	}

	/// <summary>
	/// Should return the blocks resistance to breaking
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Should returns the blocks sounds
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="pos">May be null and therfore stack is non-null</param>
	/// <param name="stack"></param>
	/// <returns></returns>
	public virtual BlockSounds GetSounds(IBlockAccessor blockAccessor, BlockSelection blockSel, ItemStack stack = null)
	{
		Block decorBlock = ((blockSel.Face == null) ? null : blockAccessor.GetDecor(blockSel.Position, new DecorBits(blockSel.Face)));
		if (decorBlock != null)
		{
			JsonObject attributes = decorBlock.Attributes;
			if (attributes == null || !attributes["ignoreSounds"].AsBool())
			{
				return decorBlock.Sounds;
			}
		}
		return Sounds;
	}

	/// <summary>
	/// Position-aware version of Attributes, for example can be used by BlockMultiblock
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
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

	/// <summary>
	/// If this block is or contains a liquid, it should return the code of it. Used for example by farmland to check if a nearby block is water
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual string GetLiquidCode(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return LiquidCode;
	}

	/// <summary>
	/// Called before a decal is created.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <param name="decalTexSource"></param>
	/// <param name="decalModelData">The block model which need UV values for the decal texture</param>
	/// <param name="blockModelData">The original block model</param>
	public virtual void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
	}

	/// <summary>
	/// Used by torches and other blocks to check if it can attach itself to that block
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="block"></param>
	/// <param name="pos"></param>
	/// <param name="blockFace"></param>
	/// <param name="attachmentArea">Area of attachment of given face in voxel dimensions (0..15)</param>
	/// <returns></returns>
	public virtual bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		bool result = true;
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.CanAttachBlockAt(blockAccessor, block, pos, blockFace, ref handled, attachmentArea);
			if (handled != 0)
			{
				result = result && behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		return SideSolid[blockFace.Index];
	}

	/// <summary>
	/// Should return if supplied entitytype is allowed to spawn on this block
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="pos"></param>
	/// <param name="type"></param>
	/// <param name="sc"></param>
	/// <returns></returns>
	public virtual bool CanCreatureSpawnOn(IBlockAccessor blockAccessor, BlockPos pos, EntityProperties type, BaseSpawnConditions sc)
	{
		bool result = true;
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.CanCreatureSpawnOn(blockAccessor, pos, type, sc, ref handled);
			if (handled != 0)
			{
				preventDefault = true;
				result = result && behaviorResult;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		bool allowedGroup = true;
		if (!AllCreaturesAllowed)
		{
			allowedGroup = AllowSpawnCreatureGroups != null && AllowSpawnCreatureGroups.Length != 0 && (AllowSpawnCreatureGroups.Contains("*") || AllowSpawnCreatureGroups.Contains(sc.Group));
		}
		if (allowedGroup)
		{
			if (sc.RequireSolidGround)
			{
				return SideSolid[BlockFacing.UP.Index];
			}
			return true;
		}
		return false;
	}

	/// <summary>
	/// Currently used for wildvines and saguaro cactus
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="pos"></param>
	/// <param name="onBlockFace"></param>
	/// <param name="worldgenRandom"></param>
	/// <param name="attributes"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Called when the player attempts to place this block
	/// </summary>
	/// <param name="world"></param>
	/// <param name="byPlayer"></param>
	/// <param name="itemstack"></param>
	/// <param name="blockSel"></param>
	/// <param name="failureCode">If you return false, set this value to a code why it cannot be placed. Its used for the ingame error overlay. Set to "__ignore__" to not trigger an error</param>
	/// <returns></returns>
	public virtual bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		bool result = true;
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref handled, ref failureCode);
			if (handled != 0)
			{
				result = result && behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return DoPlaceBlock(world, byPlayer, blockSel, itemstack);
		}
		return false;
	}

	/// <summary>
	/// Checks if this block does not intersect with something at given position
	/// </summary>
	/// <param name="world"></param>
	/// <param name="byPlayer"></param>
	/// <param name="blockSel"></param>
	/// <param name="failureCode"></param>
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
		bool result = true;
		if (byPlayer != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
			failureCode = "claimed";
			return false;
		}
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.CanPlaceBlock(world, byPlayer, blockSel, ref handled, ref failureCode);
			if (handled != 0)
			{
				result = result && behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		return true;
	}

	/// <summary>
	/// Called by TryPlaceBlock if placement is possible
	/// </summary>
	/// <param name="world"></param>
	/// <param name="byPlayer"></param>
	/// <param name="blockSel"></param>
	/// <param name="byItemStack">Might be null</param>
	public virtual bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool result = true;
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handled);
			if (handled != 0)
			{
				result = result && behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		world.BlockAccessor.SetBlock(BlockId, blockSel.Position, byItemStack);
		return true;
	}

	/// <summary>
	/// Called by the server and the client when the player currently looks at this block. Gets called continously every tick.
	/// </summary>
	/// <param name="byPlayer"></param>
	/// <param name="blockSel"></param>
	/// <param name="firstTick">True when previous tick the player looked at a different block. You can use it to make an efficient, single-event lookat trigger</param>
	public virtual void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick)
	{
	}

	/// <summary>
	/// Player is breaking this block. Has to reduce remainingResistance by the amount of time it should be broken. This method is called only client side, every 40ms during breaking.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="blockSel"></param>
	/// <param name="itemslot">The item the player currently has in his hands</param>
	/// <param name="remainingResistance">how many seconds was left until the block breaks fully</param>
	/// <param name="dt">seconds passed since last render frame</param>
	/// <param name="counter">Total count of hits (every 40ms)</param>
	/// <returns>how many seconds now left until the block breaks fully. If a value equal to or below 0 is returned, OnBlockBroken() will get called.</returns>
	public virtual float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		IItemStack stack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
		float resistance = remainingResistance;
		if (RequiredMiningTier == 0)
		{
			if (dt > 0f)
			{
				BlockBehavior[] blockBehaviors = BlockBehaviors;
				foreach (BlockBehavior behavior in blockBehaviors)
				{
					dt *= behavior.GetMiningSpeedModifier(api.World, blockSel.Position, player);
				}
			}
			resistance -= dt;
		}
		if (stack != null)
		{
			resistance = stack.Collectible.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
		}
		long totalMsBreaking = 0L;
		if (api.ObjectCache.TryGetValue("totalMsBlockBreaking", out var val))
		{
			totalMsBreaking = (long)val;
		}
		long nowMs = api.World.ElapsedMilliseconds;
		if (nowMs - totalMsBreaking > 225 || resistance <= 0f)
		{
			double posx = (double)blockSel.Position.X + blockSel.HitPosition.X;
			double posy = (double)blockSel.Position.InternalY + blockSel.HitPosition.Y;
			double posz = (double)blockSel.Position.Z + blockSel.HitPosition.Z;
			BlockSounds sounds = GetSounds(api.World.BlockAccessor, blockSel);
			player.Entity.World.PlaySoundAt((resistance > 0f) ? sounds.GetHitSound(player) : sounds.GetBreakSound(player), posx, posy, posz, player, RandomSoundPitch(api.World), 16f);
			api.ObjectCache["totalMsBlockBreaking"] = nowMs;
		}
		return resistance;
	}

	public virtual float RandomSoundPitch(IWorldAccessor world)
	{
		return (float)world.Rand.NextDouble() * 0.5f + 0.75f;
	}

	/// <summary>
	/// Called when a survival player has broken the block. This method needs to remove the block.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <param name="byPlayer"></param>
	/// <param name="dropQuantityMultiplier"></param>
	/// <returns></returns>
	public virtual void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			obj.OnBlockBroken(world, pos, byPlayer, ref handled);
			if (handled == EnumHandling.PreventDefault)
			{
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (preventDefault)
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
				for (int i = 0; i < drops.Length; i++)
				{
					if (SplitDropStacks)
					{
						for (int j = 0; j < drops[i].StackSize; j++)
						{
							ItemStack stack = drops[i].Clone();
							stack.StackSize = 1;
							world.SpawnItemEntity(stack, pos);
						}
					}
					else
					{
						world.SpawnItemEntity(drops[i].Clone(), pos);
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
		BlockBrokenParticleProps blockBrokenProps = new BlockBrokenParticleProps
		{
			blockdamage = new BlockDamage
			{
				Facing = BlockFacing.UP
			}
		};
		blockBrokenProps.Init(api);
		blockBrokenProps.blockdamage.Block = this;
		blockBrokenProps.blockdamage.Position = pos;
		blockBrokenProps.boyant = MaterialDensity < 1000;
		IClientPlayer plr = (api as ICoreClientAPI)?.World.Player;
		api.World.SpawnParticles(blockBrokenProps, plr);
		if (plr != null && (plr.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Creative)
		{
			api.World.SpawnParticles(blockBrokenProps, plr);
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
		Vec3d dropPos = new Vec3d((double)pos.X + 0.5 + (double)side.Normali.X * 0.75, (double)pos.Y + 0.5 + (double)side.Normali.Y * 0.75, (double)pos.Z + 0.5 + (double)side.Normali.Z * 0.75);
		for (int i = 0; i < drops.Length; i++)
		{
			if (SplitDropStacks)
			{
				for (int j = 0; j < drops[i].StackSize; j++)
				{
					ItemStack stack = drops[i].Clone();
					stack.StackSize = 1;
					world.SpawnItemEntity(stack, dropPos);
				}
			}
		}
	}

	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			obj.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe, ref handled);
			if (handled == EnumHandling.PreventDefault)
			{
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	/// <summary>
	/// Should return all of the blocks drops for display in the handbook
	/// </summary>
	/// <param name="handbookStack"></param>
	/// <param name="forPlayer"></param>
	/// <returns></returns>
	public virtual BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		if (Drops != null)
		{
			IEnumerable<BlockDropItemStack> drops = Array.Empty<BlockDropItemStack>();
			BlockDropItemStack[] drops2 = Drops;
			foreach (BlockDropItemStack drop in drops2)
			{
				if (drop.ResolvedItemstack.Collectible is IResolvableCollectible resolvable)
				{
					BlockDropItemStack[] resolvableStacks = resolvable.GetDropsForHandbook(handbookStack, forPlayer);
					drops = drops.Concat(resolvableStacks);
				}
				else
				{
					drops = drops.Append(drop);
				}
			}
			return drops.ToArray();
		}
		return Drops;
	}

	/// <summary>
	/// Helper method for a number of blocks
	/// </summary>
	/// <param name="handbookStack"></param>
	/// <param name="forPlayer"></param>
	/// <returns></returns>
	protected virtual BlockDropItemStack[] GetHandbookDropsFromBreakDrops(ItemStack handbookStack, IPlayer forPlayer)
	{
		ItemStack[] stacks = GetDrops(api.World, forPlayer.Entity.Pos.XYZ.AsBlockPos, forPlayer);
		if (stacks == null)
		{
			return new BlockDropItemStack[0];
		}
		BlockDropItemStack[] drops = new BlockDropItemStack[stacks.Length];
		for (int i = 0; i < stacks.Length; i++)
		{
			drops[i] = new BlockDropItemStack(stacks[i]);
		}
		return drops;
	}

	/// <summary>
	/// Is called before a block is broken, should return what items this block should drop. Return null or empty array for no drops.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <param name="byPlayer"></param>
	/// <param name="dropQuantityMultiplier"></param>
	/// <returns></returns>
	public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		bool preventDefault = false;
		List<ItemStack> dropStacks = new List<ItemStack>();
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			ItemStack[] stacks = obj.GetDrops(world, pos, byPlayer, ref dropQuantityMultiplier, ref handled);
			if (stacks != null)
			{
				dropStacks.AddRange(stacks);
			}
			switch (handled)
			{
			case EnumHandling.PreventSubsequent:
				return stacks;
			case EnumHandling.PreventDefault:
				preventDefault = true;
				break;
			}
		}
		if (preventDefault)
		{
			return dropStacks.ToArray();
		}
		if (Drops == null)
		{
			return null;
		}
		List<ItemStack> todrop = new List<ItemStack>();
		for (int i = 0; i < Drops.Length; i++)
		{
			BlockDropItemStack dstack = Drops[i];
			if (dstack.Tool.HasValue && (byPlayer == null || dstack.Tool != byPlayer.InventoryManager.ActiveTool))
			{
				continue;
			}
			float extraMul = 1f;
			if (dstack.DropModbyStat != null)
			{
				extraMul = byPlayer.Entity.Stats.GetBlended(dstack.DropModbyStat);
			}
			ItemStack stack = Drops[i].GetNextItemStack(dropQuantityMultiplier * extraMul);
			if (stack != null)
			{
				if (stack.Collectible is IResolvableCollectible irc)
				{
					DummySlot slot = new DummySlot(stack);
					irc.Resolve(slot, world);
					stack = slot.Itemstack;
				}
				todrop.Add(stack);
				if (Drops[i].LastDrop)
				{
					break;
				}
			}
		}
		todrop.AddRange(dropStacks);
		return todrop.ToArray();
	}

	/// <summary>
	/// When the player has presed the middle mouse click on the block
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		ItemStack stack = null;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling bhHandled = EnumHandling.PassThrough;
			ItemStack bhstack = obj.OnPickBlock(world, pos, ref bhHandled);
			if (bhHandled != 0)
			{
				stack = bhstack;
				handled = bhHandled;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return stack;
			}
		}
		if (handled == EnumHandling.PreventDefault)
		{
			return stack;
		}
		return new ItemStack(this);
	}

	/// <summary>
	/// Always called when a block has been removed through whatever method, except during worldgen or via ExchangeBlock()
	/// For Worldgen you might be able to use TryPlaceBlockForWorldGen() to attach custom behaviors during placement/removal
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
	{
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			obj.OnBlockRemoved(world, pos, ref handled);
			switch (handled)
			{
			case EnumHandling.PreventSubsequent:
				return;
			case EnumHandling.PreventDefault:
				preventDefault = true;
				break;
			}
		}
		if (!preventDefault && EntityClass != null)
		{
			world.BlockAccessor.RemoveBlockEntity(pos);
		}
	}

	/// <summary>
	/// Always called when a block has been placed through whatever method, except during worldgen or via ExchangeBlock()
	/// For Worldgen you might be able to use TryPlaceBlockForWorldGen() to attach custom behaviors during placement/removal
	/// </summary>
	/// <param name="world"></param>
	/// <param name="blockPos"></param>
	/// <param name="byItemStack">May be null!</param>
	public virtual void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
	{
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			obj.OnBlockPlaced(world, blockPos, ref handled);
			switch (handled)
			{
			case EnumHandling.PreventSubsequent:
				return;
			case EnumHandling.PreventDefault:
				preventDefault = true;
				break;
			}
		}
		if (!preventDefault && EntityClass != null)
		{
			world.BlockAccessor.SpawnBlockEntity(EntityClass, blockPos, byItemStack);
		}
	}

	/// <summary>
	/// Called when any of its 6 neighbour blocks has been changed
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <param name="neibpos"></param>
	public virtual void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].OnNeighbourBlockChange(world, pos, neibpos, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (handled == EnumHandling.PassThrough && (this == snowCovered1 || this == snowCovered2 || this == snowCovered3) && pos.X == neibpos.X && pos.Z == neibpos.Z && pos.Y + 1 == neibpos.Y && world.BlockAccessor.GetBlock(neibpos).Id != 0)
		{
			world.BlockAccessor.SetBlock(notSnowCovered.Id, pos);
		}
	}

	/// <summary>
	/// When a player does a right click while targeting this placed block. Should return true if the event is handled, so that other events can occur, e.g. eating a held item if the block is not interactable with.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="byPlayer"></param>
	/// <param name="blockSel"></param>
	/// <returns>False if the interaction should be stopped. True if the interaction should continue. If you return false, the interaction will not be synced to the server.</returns>
	public virtual bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		bool result = true;
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.OnBlockInteractStart(world, byPlayer, blockSel, ref handled);
			if (handled != 0)
			{
				result = result && behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		return false;
	}

	/// <summary>
	/// When a Command Block, console command or (perhaps in future) non-player entity wants to activate this placed block
	/// </summary>
	/// <param name="world"></param>
	/// <param name="caller"></param>
	/// <param name="blockSel"></param>
	/// <param name="activationArgs"></param>
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

	/// <summary>
	/// Called every frame while the player is using this block. Return false to stop the interaction.
	/// </summary>
	/// <param name="secondsUsed"></param>
	/// <param name="world"></param>
	/// <param name="byPlayer"></param>
	/// <param name="blockSel"></param>
	/// <returns></returns>
	public virtual bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		bool preventDefault = false;
		bool result = true;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel, ref handled);
			if (handled != 0)
			{
				result = result && behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		return false;
	}

	/// <summary>
	/// Called when the player successfully completed the using action, always called once an interaction is over
	/// </summary>
	/// <param name="secondsUsed"></param>
	/// <param name="world"></param>
	/// <param name="byPlayer"></param>
	/// <param name="blockSel"></param>
	public virtual void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			obj.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, ref handled);
			if (handled != 0)
			{
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	/// <summary>
	/// When the player released the right mouse button. Return false to deny the cancellation (= will keep using the block until OnBlockInteractStep returns false).
	/// </summary>
	/// <param name="secondsUsed"></param>
	/// <param name="world"></param>
	/// <param name="byPlayer"></param>
	/// <param name="blockSel"></param>
	/// <param name="cancelReason"></param>
	public virtual bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
	{
		bool preventDefault = false;
		bool result = true;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, ref handled);
			if (handled != 0)
			{
				result = result && behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		return true;
	}

	/// <summary>
	/// When an entity is inside a block 1x1x1 space, independent of of its selection box or collision box
	/// </summary>
	/// <param name="world"></param>
	/// <param name="entity"></param>
	/// <param name="pos"></param>
	public virtual void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
	{
	}

	/// <summary>
	/// Whenever an entity collides with the collision box of the block
	/// </summary>
	/// <param name="world"></param>
	/// <param name="entity"></param>
	/// <param name="pos"></param>
	/// <param name="facing"></param>
	/// <param name="collideSpeed"></param>
	/// <param name="isImpact"></param>
	public virtual void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		if (entity.Properties.CanClimb && (IsClimbable(pos) || entity.Properties.CanClimbAnywhere) && facing.IsHorizontal && entity is EntityAgent)
		{
			EntityAgent ea = entity as EntityAgent;
			if (!new bool?(ea.Controls.Sneak).GetValueOrDefault())
			{
				ea.SidedPos.Motion.Y = 0.04;
			}
		}
		float chanceReduction = Math.Max(0f, 0.75f - entity.CollisionBox.Height);
		float triggerChance = entity.WatchedAttributes.GetFloat("impactBlockUpdateChance", 0.2f - chanceReduction);
		if (isImpact && collideSpeed.Y < -0.05 && world.Rand.NextDouble() < (double)triggerChance)
		{
			OnNeighbourBlockChange(world, pos, pos.UpCopy());
		}
	}

	/// <summary>
	/// Called when a falling block falls onto this one. Return true to cancel default behavior.
	/// <br />Note: From game version 1.20.4, if overriding this you should also override <see cref="M:Vintagestory.API.Common.Block.CanAcceptFallOnto(Vintagestory.API.Common.IWorldAccessor,Vintagestory.API.MathTools.BlockPos,Vintagestory.API.Common.Block,Vintagestory.API.Datastructures.TreeAttribute)" />(). See BlockCoalPile for an example. If CanAcceptFallOnto() is not implemented, then this OnFallOnto() method will most likely never be called
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <param name="block"></param>
	/// <param name="blockEntityAttributes"></param>
	/// <returns></returns>
	public virtual bool OnFallOnto(IWorldAccessor world, BlockPos pos, Block block, TreeAttribute blockEntityAttributes)
	{
		return false;
	}

	/// <summary>
	/// Called on the main main thread or, potentially, on a separate thread if multiple physics threads is enabled. Return true to have <see cref="M:Vintagestory.API.Common.Block.OnFallOnto(Vintagestory.API.Common.IWorldAccessor,Vintagestory.API.MathTools.BlockPos,Vintagestory.API.Common.Block,Vintagestory.API.Datastructures.TreeAttribute)" />() called, which will always be on the main thread
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <param name="fallingBlock"></param>
	/// <param name="blockEntityAttributes"></param>
	/// <returns></returns>
	public virtual bool CanAcceptFallOnto(IWorldAccessor world, BlockPos pos, Block fallingBlock, TreeAttribute blockEntityAttributes)
	{
		return false;
	}

	/// <summary>
	/// Everytime the player moves by 8 blocks (or rather leaves the current 8-grid), a scan of all blocks 32x32x32 blocks around the player is initiated<br />
	/// and this method is called. If the method returns true, the block is registered to a client side game ticking for spawning particles and such.<br />
	/// This method will be called everytime the player left his current 8-grid area.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="player"></param>
	/// <param name="pos"></param>
	/// <param name="isWindAffected"></param>
	/// <returns></returns>
	public virtual bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		bool result = false;
		bool preventDefault = false;
		isWindAffected = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.ShouldReceiveClientParticleTicks(world, player, pos, ref handled);
			if (handled != 0)
			{
				result = result || behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		if (ParticleProperties != null && ParticleProperties.Length != 0)
		{
			for (int i = 0; i < ParticleProperties.Length; i++)
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

	/// <summary>
	/// If this block defines an ambient sounds, the intensity the ambient should be played at. Between 0 and 1. Return 0 to not play the ambient sound.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
	{
		return 1f;
	}

	/// <summary>
	/// Called evey 25ms if the block is in range (32 blocks) and block returned true on ShouldReceiveClientGameTicks(). Takes a few seconds for the game to register the block.
	/// </summary>
	/// <param name="manager"></param>
	/// <param name="pos"></param>
	/// <param name="windAffectednessAtPos"></param>
	/// <param name="secondsTicking"></param>
	public virtual void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (ParticleProperties != null && ParticleProperties.Length != 0)
		{
			for (int i = 0; i < ParticleProperties.Length; i++)
			{
				AdvancedParticleProperties bps = ParticleProperties[i];
				bps.WindAffectednesAtPos = windAffectednessAtPos;
				bps.basePos.X = (float)pos.X + TopMiddlePos.X;
				bps.basePos.Y = (float)pos.InternalY + TopMiddlePos.Y;
				bps.basePos.Z = (float)pos.Z + TopMiddlePos.Z;
				manager.Spawn(bps);
			}
		}
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int j = 0; j < blockBehaviors.Length; j++)
		{
			blockBehaviors[j].OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
		}
	}

	/// <summary>
	/// Called every interval specified in Server.Config.RandomTickInterval. Defaults to 50ms. This method
	/// is called on a separate server thread. This should be considered when deciding how to access blocks.
	/// If true is returned, the server will call OnServerGameTick on the main thread passing the BlockPos
	/// and the 'extra' object if specified. The 'extra' parameter is meant to prevent duplicating lookups
	/// and other calculations when OnServerGameTick is called.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos">The position of this block</param>
	/// <param name="offThreadRandom">If you do anything with random inside this method, don't use world.Rand because <see cref="T:System.Random" /> its not thread safe, use this or create your own instance</param>
	/// <param name="extra">Optional parameter to set if you need to pass additional data to the OnServerGameTick method</param>
	/// <returns></returns>
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

	/// <summary>
	/// Called by the main server thread if and only if this block returned true in ShouldReceiveServerGameTicks.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos">The position of this block</param>
	/// <param name="extra">The value set for the 'extra' parameter when ShouldReceiveGameTicks was called.</param>
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
			int cnt = decalMesh.VerticesCount;
			for (int vertexNum = 0; vertexNum < cnt; vertexNum++)
			{
				decalMesh.Flags[vertexNum] |= 100663296;
			}
		}
		else if (VertexFlags.WindMode == EnumWindBitMode.NormalWind)
		{
			decalMesh.SetWindFlag();
		}
	}

	/// <summary>
	/// If this block uses drawtype json, this method will be called everytime a chunk containing this block is tesselated.
	/// </summary>
	/// <param name="sourceMesh"></param>
	/// <param name="lightRgbsByCorner">Emitted light from this block</param>
	/// <param name="pos"></param>
	/// <param name="chunkExtBlocks">Optional, fast way to look up a direct neighbouring block. This is an array of the current chunk blocks, also including all direct neighbours, so it's a 34 x 34 x 34 block list. extIndex3d is the index of the current Block in this array. Use extIndex3d+TileSideEnum.MoveIndex[tileSide] to move around in the array.</param>
	/// <param name="extIndex3d">See description of chunkExtBlocks</param>
	public virtual void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (VertexFlags.WindMode == EnumWindBitMode.Leaves)
		{
			int verticesCount = sourceMesh.VerticesCount;
			for (int vertexNum = 0; vertexNum < verticesCount; vertexNum++)
			{
				sourceMesh.Flags[vertexNum] |= 100663296;
			}
		}
		else if (VertexFlags.WindMode == EnumWindBitMode.NormalWind)
		{
			sourceMesh.SetWindFlag(waveFlagMinY);
		}
	}

	/// <summary>
	/// Used as base position for particles.
	/// </summary>
	public virtual void DetermineTopMiddlePos()
	{
		if (CollisionBoxes != null && CollisionBoxes.Length != 0)
		{
			Cuboidf mainBox = CollisionBoxes[0];
			TopMiddlePos.X = (mainBox.X1 + mainBox.X2) / 2f;
			TopMiddlePos.Y = mainBox.Y2;
			TopMiddlePos.Z = (mainBox.Z1 + mainBox.Z2) / 2f;
			for (int i = 1; i < CollisionBoxes.Length; i++)
			{
				TopMiddlePos.Y = Math.Max(TopMiddlePos.Y, CollisionBoxes[i].Y2);
			}
		}
		else if (SelectionBoxes != null && SelectionBoxes.Length != 0)
		{
			Cuboidf mainBox2 = SelectionBoxes[0];
			TopMiddlePos.X = (mainBox2.X1 + mainBox2.X2) / 2f;
			TopMiddlePos.Y = mainBox2.Y2;
			TopMiddlePos.Z = (mainBox2.Z1 + mainBox2.Z2) / 2f;
			for (int j = 1; j < SelectionBoxes.Length; j++)
			{
				TopMiddlePos.Y = Math.Max(TopMiddlePos.Y, SelectionBoxes[j].Y2);
			}
		}
	}

	/// <summary>
	/// Used to determine if a block should be treated like air when placing blocks. (e.g. used for tallgrass)
	/// </summary>
	/// <param name="block"></param>
	/// <returns></returns>
	public virtual bool IsReplacableBy(Block block)
	{
		bool result = true;
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.IsReplacableBy(block, ref handled);
			if (handled != 0)
			{
				result = result && behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		if (IsLiquid() || Replaceable >= 6000)
		{
			return block.Replaceable < Replaceable;
		}
		return false;
	}

	/// <summary>
	/// Returns a horizontal and vertical orientation which should be used for oriented blocks like stairs during placement.
	/// </summary>
	/// <param name="byPlayer"></param>
	/// <param name="blockSel"></param>
	/// <returns></returns>
	public static BlockFacing[] SuggestedHVOrientation(IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
		double num = byPlayer.Entity.Pos.X + byPlayer.Entity.LocalEyePos.X - ((double)targetPos.X + blockSel.HitPosition.X);
		double dy = byPlayer.Entity.Pos.Y + byPlayer.Entity.LocalEyePos.Y - ((double)targetPos.Y + blockSel.HitPosition.Y);
		double dz = byPlayer.Entity.Pos.Z + byPlayer.Entity.LocalEyePos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
		float angleHor = (float)Math.Atan2(num, dz) + (float)Math.PI / 2f;
		double a = dy;
		float b = (float)Math.Sqrt(num * num + dz * dz);
		float angleVer = (float)Math.Atan2(a, b);
		BlockFacing verticalFace = (((double)angleVer < -Math.PI / 4.0) ? BlockFacing.DOWN : (((double)angleVer > Math.PI / 4.0) ? BlockFacing.UP : null));
		BlockFacing horizontalFace = BlockFacing.HorizontalFromAngle(angleHor);
		return new BlockFacing[2] { horizontalFace, verticalFace };
	}

	/// <summary>
	/// Called in the servers main thread
	/// </summary>
	/// <param name="ba"></param>
	/// <param name="pos"></param>
	/// <param name="newBlock">The block as returned by your GetSnowLevelUpdateBlock() method</param>
	/// <param name="snowLevel"></param>
	public virtual void PerformSnowLevelUpdate(IBulkBlockAccessor ba, BlockPos pos, Block newBlock, float snowLevel)
	{
		if (newBlock.Id != Id && (BlockMaterial == EnumBlockMaterial.Snow || BlockId == 0 || FirstCodePart() == newBlock.FirstCodePart()))
		{
			ba.ExchangeBlock(newBlock.Id, pos);
		}
	}

	/// <summary>
	/// Should return the snow covered block code for given snow level. Return null if snow cover is not supported for this block. If not overridden, it will check if Variant["cover"] exists and return its snow covered variant.
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="snowLevel"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Return a positive integer if the block retains heat (for warm rooms or greenhouses) or a negative integer if it preserves cool (for cellars)
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="facing"></param>
	/// <returns></returns>
	[Obsolete("Use GetRetention() instead")]
	public virtual int GetHeatRetention(BlockPos pos, BlockFacing facing)
	{
		return GetRetention(pos, facing, EnumRetentionType.Heat);
	}

	/// <summary>
	/// Return a positive integer if the block retains something, e.g. (for warm rooms or greenhouses) or a negative integer if something can pass through, e.g. cool for cellars
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="facing"></param>
	/// <param name="type"></param>
	/// <returns></returns>
	public virtual int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
	{
		bool preventDefault = false;
		int result = 0;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			int bhresult = obj.GetRetention(pos, facing, type, ref handled);
			if (handled != 0)
			{
				preventDefault = true;
				result = bhresult;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return bhresult;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		if (SideSolid[facing.Index])
		{
			if (type == EnumRetentionType.Sound)
			{
				return 10;
			}
			EnumBlockMaterial mat = GetBlockMaterial(api.World.BlockAccessor, pos);
			if (mat == EnumBlockMaterial.Ore || mat == EnumBlockMaterial.Stone || mat == EnumBlockMaterial.Soil || mat == EnumBlockMaterial.Ceramic)
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

	/// <summary>
	/// The cost of traversing this block as part of the AI pathfinding system.
	/// Return a negative value to prefer traversal of a block, return a positive value to avoid traversal of this block. A value over 10000f is considered impassable.
	/// Default value is 0
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="creatureType"></param>
	/// <returns></returns>
	public virtual float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.Humanoid)
		{
			return humanoidTraversalCost;
		}
		return (1f - WalkSpeedMultiplier) * (float)((creatureType == EnumAICreatureType.Humanoid) ? 5 : 2);
	}

	/// <summary>
	/// For any block that can be rotated, this method should be implemented to return the correct rotated block code. It is used by the world edit tool for allowing block data rotations
	/// </summary>
	/// <param name="angle"></param>
	/// <returns></returns>
	public virtual AssetLocation GetRotatedBlockCode(int angle)
	{
		bool preventDefault = false;
		AssetLocation result = Code;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			AssetLocation bhresult = obj.GetRotatedBlockCode(angle, ref handled);
			if (handled != 0)
			{
				preventDefault = true;
				result = bhresult;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return bhresult;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		return Code;
	}

	/// <summary>
	/// For any block that can be flipped upside down, this method should be implemented to return the correctly flipped block code. It is used by the world edit tool for allowing block data rotations
	/// </summary>
	/// <returns></returns>
	public virtual AssetLocation GetVerticallyFlippedBlockCode()
	{
		bool preventDefault = false;
		AssetLocation result = Code;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			AssetLocation bhresult = obj.GetVerticallyFlippedBlockCode(ref handled);
			switch (handled)
			{
			case EnumHandling.PreventSubsequent:
				return bhresult;
			case EnumHandling.PassThrough:
				continue;
			}
			preventDefault = true;
			result = bhresult;
		}
		if (preventDefault)
		{
			return result;
		}
		return Code;
	}

	/// <summary>
	/// For any block that can be flipped vertically, this method should be implemented to return the correctly flipped block code. It is used by the world edit tool for allowing block data rotations
	/// </summary>
	/// <param name="axis"></param>
	/// <returns></returns>
	public virtual AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		AssetLocation result = Code;
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			AssetLocation bhresult = obj.GetHorizontallyFlippedBlockCode(axis, ref handled);
			switch (handled)
			{
			case EnumHandling.PreventSubsequent:
				return bhresult;
			case EnumHandling.PassThrough:
				continue;
			}
			preventDefault = true;
			result = bhresult;
		}
		if (preventDefault)
		{
			return result;
		}
		return Code;
	}

	/// <summary>
	/// Returns the blocks behavior of given type, if it has such behavior
	/// </summary>
	/// <param name="type"></param>
	/// <param name="withInheritance"></param>
	/// <returns></returns>
	public BlockBehavior GetBehavior(Type type, bool withInheritance)
	{
		if (withInheritance)
		{
			for (int i = 0; i < BlockBehaviors.Length; i++)
			{
				Type testType = BlockBehaviors[i].GetType();
				if (testType == type || type.IsAssignableFrom(testType))
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

	/// <summary>
	/// Called by the block info HUD for display the interaction help besides the crosshair
	/// </summary>
	/// <param name="world"></param>
	/// <param name="selection"></param>
	/// <param name="forPlayer"></param>
	/// <returns></returns>
	public virtual WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		WorldInteraction[] interactions = new WorldInteraction[0];
		bool notProtected = true;
		if (world.Claims != null && world is IClientWorldAccessor clientWorld)
		{
			IClientPlayer player = clientWorld.Player;
			if (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Survival && world.Claims.TestAccess(clientWorld.Player, selection.Position, EnumBlockAccessFlags.BuildOrBreak) != 0)
			{
				notProtected = false;
			}
		}
		if (notProtected)
		{
			int i = 0;
			while (Drops != null && i < Drops.Length)
			{
				if (Drops[i].Tool.HasValue)
				{
					EnumTool tool = Drops[i].Tool.Value;
					interactions = interactions.Append(new WorldInteraction
					{
						ActionLangCode = "blockhelp-collect",
						MouseButton = EnumMouseButton.Left,
						Itemstacks = ObjectCacheUtil.GetOrCreate(api, "blockhelp-collect-withtool-" + tool, delegate
						{
							List<ItemStack> list = new List<ItemStack>();
							foreach (CollectibleObject current in api.World.Collectibles)
							{
								if (current.Tool == tool)
								{
									list.Add(new ItemStack(current));
								}
							}
							return list.ToArray();
						})
					});
				}
				i++;
			}
		}
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int j = 0; j < blockBehaviors.Length; j++)
		{
			WorldInteraction[] bhi = blockBehaviors[j].GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handled);
			interactions = interactions.Append(bhi);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		return interactions;
	}

	/// <summary>
	/// Called by the block info HUD for displaying the blocks name
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(OnPickBlock(world, pos)?.GetName());
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].GetPlacedBlockName(sb, world, pos);
		}
		return sb.ToString().TrimEnd();
	}

	/// <summary>
	/// Called by the block info HUD for displaying additional information
	/// </summary>
	/// <returns></returns>
	public virtual string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		StringBuilder sb = new StringBuilder();
		if (EntityClass != null)
		{
			BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
			if (be != null)
			{
				try
				{
					be.GetBlockInfo(forPlayer, sb);
				}
				catch (Exception e)
				{
					sb.AppendLine("(error in " + be.GetType().Name + ")");
					api.Logger.Error(e);
				}
			}
		}
		if (Code == null)
		{
			sb.AppendLine("Unknown Block with ID " + BlockId);
			return sb.ToString();
		}
		string descLangCode = Code.Domain + ":" + ItemClass.ToString().ToLowerInvariant() + "desc-" + Code.Path;
		string desc = Lang.GetMatching(descLangCode);
		desc = ((desc != descLangCode) ? desc : "");
		sb.Append(desc);
		Block[] decors = world.BlockAccessor.GetDecors(pos);
		List<string> decorLangLines = new List<string>();
		if (decors != null)
		{
			for (int i = 0; i < decors.Length; i++)
			{
				if (decors[i] != null)
				{
					AssetLocation decorCode = decors[i].Code;
					string decorBlockName = Lang.GetMatching(decorCode.Domain + ":" + ItemClass.ToString().ToLowerInvariant() + "-" + decorCode.Path);
					decorLangLines.Add(Lang.Get("block-with-decorname", decorBlockName));
				}
			}
		}
		sb.AppendLine(string.Join("\r\n", decorLangLines.Distinct()));
		if (RequiredMiningTier > 0 && api.World.Claims.TestAccess(forPlayer, pos, EnumBlockAccessFlags.BuildOrBreak) == EnumWorldAccessResponse.Granted)
		{
			AddMiningTierInfo(sb);
		}
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior bh in blockBehaviors)
		{
			sb.Append(bh.GetPlacedBlockInfo(world, pos, forPlayer));
		}
		return sb.ToString().TrimEnd();
	}

	public virtual void AddMiningTierInfo(StringBuilder sb)
	{
		string tierName = "?";
		if (RequiredMiningTier < miningTierNames.Length)
		{
			tierName = miningTierNames[RequiredMiningTier];
		}
		sb.AppendLine(Lang.Get("Requires tool tier {0} ({1}) to break", RequiredMiningTier, (tierName == "?") ? tierName : Lang.Get(tierName)));
	}

	/// <summary>
	/// Called by the inventory system when you hover over an item stack. This is the text that is getting displayed.
	/// </summary>
	/// <param name="inSlot"></param>
	/// <param name="dsc"></param>
	/// <param name="world"></param>
	/// <param name="withDebugInfo"></param>
	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		ItemStack stack = inSlot.Itemstack;
		if (DrawType == EnumDrawType.SurfaceLayer)
		{
			dsc.AppendLine(Lang.Get("Decor layer block"));
		}
		EnumBlockMaterial mat = GetBlockMaterial(world.BlockAccessor, null, stack);
		dsc.AppendLine(Lang.Get("Material: ") + Lang.Get("blockmaterial-" + mat));
		AddExtraHeldItemInfoPostMaterial(inSlot, dsc, world);
		byte[] lightHsv = GetLightHsv(world.BlockAccessor, null, stack);
		dsc.Append((!withDebugInfo) ? "" : ((lightHsv[2] > 0) ? (Lang.Get("light-hsv") + lightHsv[0] + ", " + lightHsv[1] + ", " + lightHsv[2] + "\n") : ""));
		dsc.Append(withDebugInfo ? "" : ((lightHsv[2] > 0) ? (Lang.Get("light-level") + lightHsv[2] + "\n") : ""));
		if (WalkSpeedMultiplier != 1f)
		{
			dsc.Append(Lang.Get("walk-multiplier") + WalkSpeedMultiplier + "\n");
		}
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior bh in blockBehaviors)
		{
			dsc.Append(bh.GetHeldBlockInfo(world, inSlot));
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}

	/// <summary>
	/// Opportunity for blocks to add additional lines to the Held Item info _prior to_ the behaviors output (such as nutrition properties or block reinforcement)
	/// </summary>
	/// <param name="inSlot"></param>
	/// <param name="dsc"></param>
	/// <param name="world"></param>
	public virtual void AddExtraHeldItemInfoPostMaterial(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
	{
	}

	/// <summary>
	/// If true, the player can select invdividual selection boxes of this block
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return PartialSelection;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="capi"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos)
	{
		return new Vec4f(0f, 0f, 0f, 0.5f);
	}

	/// <summary>
	/// Called by the texture atlas manager when building up the block atlas. Has to add all of the blocks texture
	/// </summary>
	/// <param name="api"></param>
	/// <param name="textureDict"></param>
	public virtual void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
	{
		(Textures as TextureDictionary).BakeAndCollect(api.Assets, textureDict, Code, "Baked variant of block ");
		(TexturesInventory as TextureDictionary).BakeAndCollect(api.Assets, textureDict, Code, "Baked inventory variant of block ");
		foreach (KeyValuePair<string, CompositeTexture> val in Textures)
		{
			AssetLocation f = val.Value.AnyWildCardNoFiles;
			if (f != null)
			{
				api.Logger.Warning("Block {0} defines a wildcard texture {1} (or one of its alternates), key {2}, but no matching texture found", Code, f, val.Key);
			}
		}
	}

	/// <summary>
	/// Should return the blocks blast resistance. Default behavior is to return BlockMaterialUtil.MaterialBlastResistance(blastType, BlockMaterial);
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <param name="blastDirectionVector"></param>
	/// <param name="blastType"></param>
	/// <returns></returns>
	public virtual double GetBlastResistance(IWorldAccessor world, BlockPos pos, Vec3f blastDirectionVector, EnumBlastType blastType)
	{
		if (blastType == EnumBlastType.RockBlast)
		{
			return Math.Min(BlockMaterialUtil.MaterialBlastResistance(EnumBlastType.RockBlast, GetBlockMaterial(world.BlockAccessor, pos)), BlockMaterialUtil.MaterialBlastResistance(EnumBlastType.OreBlast, GetBlockMaterial(world.BlockAccessor, pos)));
		}
		return BlockMaterialUtil.MaterialBlastResistance(blastType, GetBlockMaterial(world.BlockAccessor, pos));
	}

	/// <summary>
	/// Should return the chance of the block dropping its upon upon being exploded. Default behavior is to return BlockMaterialUtil.MaterialBlastDropChances(blastType, BlockMaterial);
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <param name="blastType"></param>
	/// <returns></returns>
	public virtual double ExplosionDropChance(IWorldAccessor world, BlockPos pos, EnumBlastType blastType)
	{
		return BlockMaterialUtil.MaterialBlastDropChances(blastType, GetBlockMaterial(world.BlockAccessor, pos));
	}

	/// <summary>
	/// Called when the block was blown up by explosives
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <param name="explosionCenter"></param>
	/// <param name="blastType"></param>
	public virtual void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int k = 0; k < blockBehaviors.Length; k++)
		{
			blockBehaviors[k].OnBlockExploded(world, pos, explosionCenter, blastType, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		if (handled == EnumHandling.PreventDefault)
		{
			return;
		}
		world.BulkBlockAccessor.SetBlock(0, pos);
		double dropChancce = ExplosionDropChance(world, pos, blastType);
		if (world.Rand.NextDouble() < dropChancce)
		{
			ItemStack[] drops = GetDrops(world, pos, null);
			int i = 0;
			while (drops != null && i < drops.Length)
			{
				if (SplitDropStacks)
				{
					for (int j = 0; j < drops[i].StackSize; j++)
					{
						ItemStack stack = drops[i].Clone();
						stack.StackSize = 1;
						world.SpawnItemEntity(stack, pos);
					}
				}
				else
				{
					world.SpawnItemEntity(drops[i].Clone(), pos);
				}
				i++;
			}
		}
		if (EntityClass != null)
		{
			world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken();
		}
	}

	/// <summary>
	/// Should return the color to be used for the block particle coloring
	/// </summary>
	/// <param name="capi"></param>
	/// <param name="pos"></param>
	/// <param name="facing"></param>
	/// <param name="rndIndex"></param>
	/// <returns></returns>
	public virtual int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (Textures == null || Textures.Count == 0)
		{
			return 0;
		}
		if (!Textures.TryGetValue(facing.Code, out var tex))
		{
			tex = Textures.First().Value;
		}
		if (tex?.Baked == null)
		{
			return 0;
		}
		int color = capi.BlockTextureAtlas.GetRandomColor(tex.Baked.TextureSubId, rndIndex);
		if (ClimateColorMapResolved != null || SeasonColorMapResolved != null)
		{
			color = capi.World.ApplyColorMapOnRgba(ClimateColorMapResolved, SeasonColorMapResolved, color, pos.X, pos.Y, pos.Z);
		}
		return color;
	}

	/// <summary>
	/// Should return a random pixel within the items/blocks texture
	/// </summary>
	/// <param name="capi"></param>
	/// <param name="stack"></param>
	/// <returns></returns>
	public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		if (TextureSubIdForBlockColor < 0)
		{
			return -1;
		}
		return capi.BlockTextureAtlas.GetRandomColor(TextureSubIdForBlockColor);
	}

	/// <summary>
	/// Should return an RGB color for this block. Current use: In the world map. Default behavior: The 2 averaged pixels at 40%/40% ad 60%/60% position
	/// </summary>
	/// <param name="capi"></param>
	/// <param name="pos"></param>
	public virtual int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		int color = GetColorWithoutTint(capi, pos);
		if (ClimateColorMapResolved != null || SeasonColorMapResolved != null)
		{
			color = capi.World.ApplyColorMapOnRgba(ClimateColorMapResolved, SeasonColorMapResolved, color, pos.X, pos.Y, pos.Z, flipRb: false);
		}
		return color;
	}

	/// <summary>
	/// Tint less version of GetColor. Used for map color export
	/// </summary>
	/// <param name="capi"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Alias of api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as T
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="blockSel"></param>
	/// <returns></returns>
	public virtual T GetBlockEntity<T>(BlockSelection blockSel) where T : BlockEntity
	{
		return api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as T;
	}

	/// <summary>
	/// Alias of api.World.BlockAccessor.GetBlockEntity(position) as T
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="position"></param>
	/// <returns></returns>
	public virtual T GetBlockEntity<T>(BlockPos position) where T : BlockEntity
	{
		return api.World.BlockAccessor.GetBlockEntity(position) as T;
	}

	/// <summary>
	/// Alias of api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior&lt;T&gt;()
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual T GetBEBehavior<T>(BlockPos pos) where T : BlockEntityBehavior
	{
		BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(pos);
		if (blockEntity == null)
		{
			return null;
		}
		return blockEntity.GetBehavior<T>();
	}

	/// <summary>
	/// Returns instance of class that implements this interface in the following order<br />
	/// 1. Block (returns itself)<br />
	/// 2. BlockBehavior (returns on of our own behavior)<br />
	/// 3. BlockEntity<br />
	/// 4. BlockEntityBehavior
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <returns></returns>
	public virtual T GetInterface<T>(IWorldAccessor world, BlockPos pos) where T : class
	{
		if (this is T blockt)
		{
			return blockt;
		}
		BlockBehavior bh = GetBehavior(typeof(T), withInheritance: true);
		if (bh != null)
		{
			return bh as T;
		}
		if (pos == null)
		{
			return null;
		}
		BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
		if (be is T bet)
		{
			return bet;
		}
		if (be != null)
		{
			T beh = be.GetBehavior<T>();
			if (beh != null)
			{
				return beh;
			}
		}
		return null;
	}

	/// <summary>
	/// Creates a deep copy of the block
	/// </summary>
	/// <returns></returns>
	public Block Clone()
	{
		Block cloned = (Block)MemberwiseClone();
		cloned.Code = Code.Clone();
		if (MiningSpeed != null)
		{
			cloned.MiningSpeed = new Dictionary<EnumBlockMaterial, float>(MiningSpeed);
		}
		if (Textures is FastSmallDictionary<string, CompositeTexture> fastTextures)
		{
			cloned.Textures = fastTextures.Clone();
		}
		else
		{
			cloned.Textures = new FastSmallDictionary<string, CompositeTexture>(Textures.Count);
			foreach (KeyValuePair<string, CompositeTexture> var2 in Textures)
			{
				cloned.Textures[var2.Key] = var2.Value.Clone();
			}
		}
		if (TexturesInventory is FastSmallDictionary<string, CompositeTexture> fastInvTextures)
		{
			cloned.TexturesInventory = fastInvTextures.Clone();
		}
		else
		{
			cloned.TexturesInventory = new Dictionary<string, CompositeTexture>();
			foreach (KeyValuePair<string, CompositeTexture> var in TexturesInventory)
			{
				cloned.TexturesInventory[var.Key] = var.Value.Clone();
			}
		}
		cloned.Shape = Shape.Clone();
		if (LightHsv != null)
		{
			cloned.LightHsv = (byte[])LightHsv.Clone();
		}
		if (ParticleProperties != null)
		{
			cloned.ParticleProperties = new AdvancedParticleProperties[ParticleProperties.Length];
			for (int j = 0; j < ParticleProperties.Length; j++)
			{
				cloned.ParticleProperties[j] = ParticleProperties[j].Clone();
			}
		}
		if (Drops != null)
		{
			cloned.Drops = new BlockDropItemStack[Drops.Length];
			for (int i = 0; i < Drops.Length; i++)
			{
				cloned.Drops[i] = Drops[i].Clone();
			}
		}
		if (SideOpaque != null)
		{
			cloned.SideOpaque = (bool[])SideOpaque.Clone();
		}
		cloned.SideSolid = SideSolid;
		if (SideAo != null)
		{
			cloned.SideAo = (bool[])SideAo.Clone();
		}
		if (CombustibleProps != null)
		{
			cloned.CombustibleProps = CombustibleProps.Clone();
		}
		if (NutritionProps != null)
		{
			cloned.NutritionProps = NutritionProps.Clone();
		}
		if (GrindingProps != null)
		{
			cloned.GrindingProps = GrindingProps.Clone();
		}
		if (Attributes != null)
		{
			cloned.Attributes = Attributes.Clone();
		}
		return cloned;
	}

	/// <summary>
	/// Returns true if the block has given block behavior
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="withInheritance"></param>
	/// <returns></returns>
	public bool HasBlockBehavior<T>(bool withInheritance = false) where T : BlockBehavior
	{
		return (T)GetCollectibleBehavior(typeof(T), withInheritance) != null;
	}

	/// <summary>
	/// Returns true if the block has given block behavior OR collectible behavior
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="withInheritance"></param>
	/// <returns></returns>
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
		List<string> toRemove = null;
		int i = 0;
		foreach (KeyValuePair<string, CompositeTexture> val2 in Textures)
		{
			if (val2.Value.Base == null)
			{
				logger.Error("The texture definition {0} for #{2} in block with code {1} is invalid. The base property is null. Will skip.", i, Code, val2.Key);
				if (toRemove == null)
				{
					toRemove = new List<string>();
				}
				toRemove.Add(val2.Key);
			}
			i++;
		}
		if (toRemove == null)
		{
			return;
		}
		foreach (string val in toRemove)
		{
			Textures.Remove(val);
		}
	}

	/// <summary>
	/// Return a decimal between 0.0 and 1.0 indicating - if this block is solid enough to block liquid flow on that side - how high the barrier is
	/// </summary>
	public virtual float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
	{
		bool preventDefault = false;
		float result = 0f;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			float bhresult = obj.GetLiquidBarrierHeightOnSide(face, pos, ref handled);
			if (handled != 0)
			{
				preventDefault = true;
				result = bhresult;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return bhresult;
			}
		}
		if (preventDefault)
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
			float[] lbos = Attributes?["liquidBarrierOnSides"].AsArray<float>();
			int i = 0;
			while (lbos != null && i < lbos.Length)
			{
				liquidBarrierHeightonSide[i] = lbos[i];
				i++;
			}
		}
		return liquidBarrierHeightonSide[face.Index];
	}

	/// <summary>
	/// Simple string representation for debugging
	/// </summary>
	/// <returns></returns>
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
