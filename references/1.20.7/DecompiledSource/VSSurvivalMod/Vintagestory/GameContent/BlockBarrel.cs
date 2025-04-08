using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBarrel : BlockLiquidContainerBase
{
	public override bool AllowHeldLiquidTransfer => false;

	public AssetLocation emptyShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/empty");


	public AssetLocation sealedShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/closed");


	public AssetLocation contentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/contents");


	public AssetLocation opaqueLiquidContentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/opaqueliquidcontents");


	public AssetLocation liquidContentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/liquidcontents");


	public override int GetContainerSlotId(BlockPos pos)
	{
		return 1;
	}

	public override int GetContainerSlotId(ItemStack containerStack)
	{
		return 1;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		object obj;
		Dictionary<string, MultiTextureMeshRef> meshrefs = (Dictionary<string, MultiTextureMeshRef>)(capi.ObjectCache.TryGetValue("barrelMeshRefs" + Code, out obj) ? (obj as Dictionary<string, MultiTextureMeshRef>) : (capi.ObjectCache["barrelMeshRefs" + Code] = new Dictionary<string, MultiTextureMeshRef>()));
		ItemStack[] contentStacks = GetContents(capi.World, itemstack);
		if (contentStacks != null && contentStacks.Length != 0)
		{
			bool issealed = itemstack.Attributes.GetBool("sealed");
			string meshkey = GetBarrelMeshkey(contentStacks[0], (contentStacks.Length > 1) ? contentStacks[1] : null);
			if (!meshrefs.TryGetValue(meshkey, out var meshRef))
			{
				MeshData meshdata = GenMesh(contentStacks[0], (contentStacks.Length > 1) ? contentStacks[1] : null, issealed);
				meshRef = (meshrefs[meshkey] = capi.Render.UploadMultiTextureMesh(meshdata));
			}
			renderinfo.ModelRef = meshRef;
		}
	}

	public string GetBarrelMeshkey(ItemStack contentStack, ItemStack liquidStack)
	{
		return string.Concat(contentStack?.StackSize + "x" + contentStack?.GetHashCode(), (liquidStack?.StackSize).ToString(), "x", (liquidStack?.GetHashCode()).ToString());
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI capi) || !capi.ObjectCache.TryGetValue("barrelMeshRefs", out var obj))
		{
			return;
		}
		foreach (KeyValuePair<int, MultiTextureMeshRef> item in obj as Dictionary<int, MultiTextureMeshRef>)
		{
			item.Value.Dispose();
		}
		capi.ObjectCache.Remove("barrelMeshRefs");
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
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
		if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			ItemStack[] drops = new ItemStack[1]
			{
				new ItemStack(this)
			};
			for (int i = 0; i < drops.Length; i++)
			{
				world.SpawnItemEntity(drops[i], pos);
			}
			world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, 0.0, byPlayer);
		}
		if (EntityClass != null)
		{
			world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
		}
		world.BlockAccessor.SetBlock(0, pos);
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
	}

	public override int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres)
	{
		return base.TryPutLiquid(pos, liquidStack, desiredLitres);
	}

	public override int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
	{
		return base.TryPutLiquid(containerStack, liquidStack, desiredLitres);
	}

	public MeshData GenMesh(ItemStack contentStack, ItemStack liquidContentStack, bool issealed, BlockPos forBlockPos = null)
	{
		ICoreClientAPI obj = api as ICoreClientAPI;
		Shape shape = Vintagestory.API.Common.Shape.TryGet(obj, issealed ? sealedShape : emptyShape);
		obj.Tesselator.TesselateShape(this, shape, out var barrelMesh);
		if (!issealed)
		{
			JsonObject containerProps = liquidContentStack?.ItemAttributes?["waterTightContainerProps"];
			MeshData contentMesh = getContentMeshFromAttributes(contentStack, liquidContentStack, forBlockPos) ?? getContentMeshLiquids(contentStack, liquidContentStack, forBlockPos, containerProps) ?? getContentMesh(contentStack, forBlockPos, contentsShape);
			if (contentMesh != null)
			{
				barrelMesh.AddMeshData(contentMesh);
			}
			if (forBlockPos != null)
			{
				barrelMesh.CustomInts = new CustomMeshDataPartInt(barrelMesh.FlagsCount);
				barrelMesh.CustomInts.Values.Fill(67108864);
				barrelMesh.CustomInts.Count = barrelMesh.FlagsCount;
				barrelMesh.CustomFloats = new CustomMeshDataPartFloat(barrelMesh.FlagsCount * 2);
				barrelMesh.CustomFloats.Count = barrelMesh.FlagsCount * 2;
			}
		}
		return barrelMesh;
	}

	private MeshData getContentMeshLiquids(ItemStack contentStack, ItemStack liquidContentStack, BlockPos forBlockPos, JsonObject containerProps)
	{
		bool isopaque = containerProps?["isopaque"].AsBool() ?? false;
		bool isliquid = containerProps?.Exists ?? false;
		if (liquidContentStack != null && (isliquid || contentStack == null))
		{
			AssetLocation shapefilepath = contentsShape;
			if (isliquid)
			{
				shapefilepath = (isopaque ? opaqueLiquidContentsShape : liquidContentsShape);
			}
			return getContentMesh(liquidContentStack, forBlockPos, shapefilepath);
		}
		return null;
	}

	private MeshData getContentMeshFromAttributes(ItemStack contentStack, ItemStack liquidContentStack, BlockPos forBlockPos)
	{
		if (liquidContentStack != null && (liquidContentStack.ItemAttributes?["inBarrelShape"].Exists).GetValueOrDefault())
		{
			AssetLocation loc = AssetLocation.Create(liquidContentStack.ItemAttributes?["inBarrelShape"].AsString(), contentStack.Collectible.Code.Domain).WithPathPrefixOnce("shapes").WithPathAppendixOnce(".json");
			return getContentMesh(contentStack, forBlockPos, loc);
		}
		return null;
	}

	protected MeshData getContentMesh(ItemStack stack, BlockPos forBlockPos, AssetLocation shapefilepath)
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(stack);
		ITexPositionSource contentSource;
		float fillHeight;
		if (props != null)
		{
			if (props.Texture == null)
			{
				return null;
			}
			contentSource = new ContainerTextureSource(capi, stack, props.Texture);
			fillHeight = GameMath.Min(1f, (float)stack.StackSize / props.ItemsPerLitre / (float)Math.Max(50, props.MaxStackSize)) * 10f / 16f;
		}
		else
		{
			contentSource = getContentTexture(capi, stack, out fillHeight);
		}
		if (stack != null && contentSource != null)
		{
			Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapefilepath);
			if (shape == null)
			{
				api.Logger.Warning($"Barrel block '{Code}': Content shape {shapefilepath} not found. Will try to default to another one.");
				return null;
			}
			capi.Tesselator.TesselateShape("barrel", shape, out var contentMesh, contentSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), props?.GlowLevel ?? 0, 0, 0);
			contentMesh.Translate(0f, fillHeight, 0f);
			if (props != null && props.ClimateColorMap != null)
			{
				int col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, -1, 196, 128, flipRb: false);
				if (forBlockPos != null)
				{
					col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, -1, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, flipRb: false);
				}
				byte[] rgba = ColorUtil.ToBGRABytes(col);
				for (int i = 0; i < contentMesh.Rgba.Length; i++)
				{
					contentMesh.Rgba[i] = (byte)(contentMesh.Rgba[i] * rgba[i % 4] / 255);
				}
			}
			return contentMesh;
		}
		return null;
	}

	public static ITexPositionSource getContentTexture(ICoreClientAPI capi, ItemStack stack, out float fillHeight)
	{
		ITexPositionSource contentSource = null;
		fillHeight = 0f;
		JsonObject obj = stack?.ItemAttributes?["inContainerTexture"];
		if (obj != null && obj.Exists)
		{
			contentSource = new ContainerTextureSource(capi, stack, obj.AsObject<CompositeTexture>());
			fillHeight = GameMath.Min(0.75f, 0.7f * (float)stack.StackSize / (float)stack.Collectible.MaxStackSize);
		}
		else if (stack?.Block != null && (stack.Block.DrawType == EnumDrawType.Cube || stack.Block.Shape.Base.Path.Contains("basic/cube")) && capi.BlockTextureAtlas.GetPosition(stack.Block, "up", returnNullWhenMissing: true) != null)
		{
			contentSource = new BlockTopTextureSource(capi, stack.Block);
			fillHeight = GameMath.Min(0.75f, 0.7f * (float)stack.StackSize / (float)stack.Collectible.MaxStackSize);
		}
		else if (stack != null)
		{
			if (stack.Class == EnumItemClass.Block)
			{
				if (stack.Block.Textures.Count > 1)
				{
					return null;
				}
				contentSource = new ContainerTextureSource(capi, stack, stack.Block.Textures.FirstOrDefault().Value);
			}
			else
			{
				if (stack.Item.Textures.Count > 1)
				{
					return null;
				}
				contentSource = new ContainerTextureSource(capi, stack, stack.Item.FirstTexture);
			}
			fillHeight = GameMath.Min(0.75f, 0.7f * (float)stack.StackSize / (float)stack.Collectible.MaxStackSize);
		}
		return contentSource;
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-place",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => true
			}
		};
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (Attributes != null)
		{
			capacityLitresFromAttributes = Attributes["capacityLitres"].AsInt(50);
			emptyShape = AssetLocation.Create(Attributes["emptyShape"].AsString(emptyShape), Code.Domain);
			sealedShape = AssetLocation.Create(Attributes["sealedShape"].AsString(sealedShape), Code.Domain);
			contentsShape = AssetLocation.Create(Attributes["contentsShape"].AsString(contentsShape), Code.Domain);
			opaqueLiquidContentsShape = AssetLocation.Create(Attributes["opaqueLiquidContentsShape"].AsString(opaqueLiquidContentsShape), Code.Domain);
			liquidContentsShape = AssetLocation.Create(Attributes["liquidContentsShape"].AsString(liquidContentsShape), Code.Domain);
		}
		emptyShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		sealedShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		contentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		opaqueLiquidContentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		liquidContentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		ICoreClientAPI capi = api as ICoreClientAPI;
		interactions = ObjectCacheUtil.GetOrCreate(api, "liquidContainerBase", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (current is ILiquidSource || current is ILiquidSink || current is BlockWateringCan)
				{
					List<ItemStack> handBookStacks = current.GetHandBookStacks(capi);
					if (handBookStacks != null)
					{
						list.AddRange(handBookStacks);
					}
				}
			}
			ItemStack[] lstacks = list.ToArray();
			ItemStack[] linenStack = new ItemStack[1]
			{
				new ItemStack(api.World.GetBlock(new AssetLocation("linen-normal-down")))
			};
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bucket-rightclick",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = lstacks,
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection ws)
					{
						BlockEntityBarrel obj = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityBarrel;
						return (obj == null || obj.Sealed) ? null : lstacks;
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-barrel-takecottagecheese",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = linenStack,
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection ws) => ((api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityBarrel)?.Inventory[1].Itemstack?.Item?.Code?.Path == "cottagecheeseportion") ? linenStack : null
				}
			};
		});
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer)
	{
		BlockEntityBarrel bebarrel = null;
		if (blockSel.Position != null)
		{
			bebarrel = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBarrel;
		}
		if (bebarrel != null && bebarrel.Sealed)
		{
			return new WorldInteraction[0];
		}
		return base.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer);
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		BlockEntityBarrel bebarrel = null;
		if (blockSel.Position != null)
		{
			bebarrel = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBarrel;
		}
		if (bebarrel != null && bebarrel.Sealed)
		{
			return true;
		}
		bool handled = base.OnBlockInteractStart(world, byPlayer, blockSel);
		if (!handled && !byPlayer.WorldData.EntityControls.ShiftKey && blockSel.Position != null)
		{
			bebarrel?.OnPlayerRightClick(byPlayer);
			return true;
		}
		return handled;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		ItemStack[] contentStacks = GetContents(world, inSlot.Itemstack);
		if (contentStacks != null && contentStacks.Length != 0)
		{
			ItemStack itemstack = ((contentStacks[0] == null) ? contentStacks[1] : contentStacks[0]);
			if (itemstack != null)
			{
				dsc.Append(", " + Lang.Get("{0}x {1}", itemstack.StackSize, itemstack.GetName()));
			}
		}
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		string text = base.GetPlacedBlockInfo(world, pos, forPlayer);
		string aftertext = "";
		int i = text.IndexOfOrdinal(Environment.NewLine + Environment.NewLine);
		if (i > 0)
		{
			aftertext = text.Substring(i);
			text = text.Substring(0, i);
		}
		if (GetCurrentLitres(pos) <= 0f)
		{
			text = "";
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBarrel bebarrel)
		{
			ItemSlot slot = bebarrel.Inventory[0];
			if (!slot.Empty)
			{
				text = ((text.Length <= 0) ? (text + Lang.Get("Contents:") + "\n ") : (text + " "));
				text += Lang.Get("{0}x {1}", slot.Itemstack.StackSize, slot.Itemstack.GetName());
				text += BlockLiquidContainerBase.PerishableInfoCompact(api, slot, 0f, withStackName: false);
			}
			if (bebarrel.Sealed && bebarrel.CurrentRecipe != null)
			{
				double hoursPassed = world.Calendar.TotalHours - bebarrel.SealedSinceTotalHours;
				if (hoursPassed < 3.0)
				{
					hoursPassed = Math.Max(0.0, hoursPassed + 0.2);
				}
				string timePassedText = ((hoursPassed > 24.0) ? Lang.Get("{0} days", Math.Floor(hoursPassed / (double)api.World.Calendar.HoursPerDay * 10.0) / 10.0) : Lang.Get("{0} hours", Math.Floor(hoursPassed)));
				string timeTotalText = ((bebarrel.CurrentRecipe.SealHours > 24.0) ? Lang.Get("{0} days", Math.Round(bebarrel.CurrentRecipe.SealHours / (double)api.World.Calendar.HoursPerDay, 1)) : Lang.Get("{0} hours", Math.Round(bebarrel.CurrentRecipe.SealHours)));
				text = text + "\n" + Lang.Get("Sealed for {0} / {1}", timePassedText, timeTotalText);
			}
		}
		return text + aftertext;
	}

	public override void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos)
	{
	}
}
