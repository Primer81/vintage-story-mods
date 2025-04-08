using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCrate : BlockContainer, ITexPositionSource, IAttachableToEntity, IWearableShapeSupplier
{
	private string curType;

	private LabelProps nowTeselatingLabel;

	private ITexPositionSource tmpTextureSource;

	private TextureAtlasPosition labelTexturePos;

	public CrateProperties Props;

	private Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);

	private Cuboidf[] closedCollBoxes = new Cuboidf[1]
	{
		new Cuboidf(0.0625f, 0f, 0.0625f, 0.9375f, 0.9375f, 0.9375f)
	};

	public Size2i AtlasSize => tmpTextureSource.AtlasSize;

	public string Subtype
	{
		get
		{
			if (Props.VariantByGroup != null)
			{
				return Variant[Props.VariantByGroup];
			}
			return "";
		}
	}

	public string SubtypeInventory
	{
		get
		{
			if (Props?.VariantByGroupInventory != null)
			{
				return Variant[Props.VariantByGroupInventory];
			}
			return "";
		}
	}

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (nowTeselatingLabel != null)
			{
				return labelTexturePos;
			}
			TextureAtlasPosition pos = tmpTextureSource[curType + "-" + textureCode];
			if (pos == null)
			{
				pos = tmpTextureSource[textureCode];
			}
			if (pos == null)
			{
				pos = (api as ICoreClientAPI).BlockTextureAtlas.UnknownTexturePosition;
			}
			return pos;
		}
	}

	public virtual bool IsAttachable(Entity toEntity, ItemStack itemStack)
	{
		if (toEntity is EntityPlayer)
		{
			return false;
		}
		return true;
	}

	public void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
	{
		string type = stack.Attributes.GetString("type");
		foreach (string key in shape.Textures.Keys)
		{
			Textures.TryGetValue(type + "-" + key, out var ctex);
			if (ctex != null)
			{
				intoDict[texturePrefixCode + key] = ctex;
				continue;
			}
			Textures.TryGetValue(key, out var ctex2);
			intoDict[texturePrefixCode + key] = ctex2;
		}
	}

	public string GetCategoryCode(ItemStack stack)
	{
		return "crate";
	}

	public Shape GetShape(ItemStack stack, Entity forEntity, string texturePrefixCode)
	{
		string type = stack.Attributes.GetString("type", Props.DefaultType);
		stack.Attributes.GetString("label");
		string @string = stack.Attributes.GetString("lidState", "closed");
		CompositeShape cshape = Props[type].Shape;
		if (ShapeInventory != null)
		{
			new Vec3f(ShapeInventory.rotateX, ShapeInventory.rotateY, ShapeInventory.rotateZ);
		}
		ItemStack[] contentStacks = GetNonEmptyContents(api.World, stack);
		if (contentStacks != null && contentStacks.Length != 0)
		{
			_ = contentStacks[0];
		}
		if (@string == "opened")
		{
			cshape = cshape.Clone();
			cshape.Base.Path = cshape.Base.Path.Replace("closed", "opened");
		}
		AssetLocation shapeloc = cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		Shape shape = Vintagestory.API.Common.Shape.TryGet(api, shapeloc);
		shape.SubclassForStepParenting(texturePrefixCode);
		return shape;
	}

	public CompositeShape GetAttachedShape(ItemStack stack, string slotCode)
	{
		return null;
	}

	public string[] GetDisableElements(ItemStack stack)
	{
		return null;
	}

	public string[] GetKeepElements(ItemStack stack)
	{
		return null;
	}

	public string GetTexturePrefixCode(ItemStack stack)
	{
		return GetKey(stack);
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityCrate be)
		{
			return be.GetSelectionBoxes();
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityCrate { LidState: "closed" })
		{
			return closedCollBoxes;
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Props = Attributes.AsObject<CrateProperties>(null, Code.Domain);
		PlacedPriorityInteract = true;
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityCrate bect)
		{
			BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
			double dz = (double)(float)byPlayer.Entity.Pos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
			float angleHor = (float)Math.Atan2(y, dz);
			string type = bect.type;
			string rotatatableInterval = Props[type].RotatatableInterval;
			if (rotatatableInterval == "22.5degnot45deg")
			{
				float rounded90degRad = (float)(int)Math.Round(angleHor / ((float)Math.PI / 2f)) * ((float)Math.PI / 2f);
				float deg45rad = (float)Math.PI / 8f;
				if (Math.Abs(angleHor - rounded90degRad) >= deg45rad)
				{
					bect.MeshAngle = rounded90degRad + (float)Math.PI / 8f * (float)Math.Sign(angleHor - rounded90degRad);
				}
				else
				{
					bect.MeshAngle = rounded90degRad;
				}
			}
			if (rotatatableInterval == "22.5deg")
			{
				float deg22dot5rad = (float)Math.PI / 8f;
				float roundRad = (float)(int)Math.Round(angleHor / deg22dot5rad) * deg22dot5rad;
				bect.MeshAngle = roundRad;
			}
		}
		return num;
	}

	public string GetKey(ItemStack itemstack)
	{
		string type = itemstack.Attributes.GetString("type", Props.DefaultType);
		string label = itemstack.Attributes.GetString("label");
		string lidState = itemstack.Attributes.GetString("lidState", "closed");
		return type + "-" + label + "-" + lidState;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		string cacheKey = "crateMeshRefs" + FirstCodePart() + SubtypeInventory;
		Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate(capi, cacheKey, () => new Dictionary<string, MultiTextureMeshRef>());
		string key = GetKey(itemstack);
		if (!meshrefs.TryGetValue(key, out renderinfo.ModelRef))
		{
			string type = itemstack.Attributes.GetString("type", Props.DefaultType);
			string label = itemstack.Attributes.GetString("label");
			string lidState = itemstack.Attributes.GetString("lidState", "closed");
			CompositeShape cshape = Props[type].Shape;
			Vec3f rot = ((ShapeInventory == null) ? null : new Vec3f(ShapeInventory.rotateX, ShapeInventory.rotateY, ShapeInventory.rotateZ));
			ItemStack[] contentStacks = GetNonEmptyContents(capi.World, itemstack);
			ItemStack contentStack = ((contentStacks == null || contentStacks.Length == 0) ? null : contentStacks[0]);
			MeshData mesh = GenMesh(capi, contentStack, type, label, lidState, cshape, rot);
			meshrefs[key] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(mesh));
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI capi))
		{
			return;
		}
		string key = "crateMeshRefs" + FirstCodePart() + SubtypeInventory;
		Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, key);
		if (meshrefs == null)
		{
			return;
		}
		foreach (KeyValuePair<string, MultiTextureMeshRef> item in meshrefs)
		{
			item.Value.Dispose();
		}
		capi.ObjectCache.Remove(key);
	}

	public Shape GetShape(ICoreClientAPI capi, string type, CompositeShape cshape)
	{
		if (cshape?.Base == null)
		{
			return null;
		}
		ITesselatorAPI tesselator = capi.Tesselator;
		tmpTextureSource = tesselator.GetTextureSource(this, 0, returnNullWhenMissing: true);
		AssetLocation shapeloc = cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		Shape result = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc);
		curType = type;
		return result;
	}

	public MeshData GenMesh(ICoreClientAPI capi, ItemStack contentStack, string type, string label, string lidState, CompositeShape cshape, Vec3f rotation = null)
	{
		if (lidState == "opened")
		{
			cshape = cshape.Clone();
			cshape.Base.Path = cshape.Base.Path.Replace("closed", "opened");
		}
		Shape shape = GetShape(capi, type, cshape);
		ITesselatorAPI tesselator = capi.Tesselator;
		if (shape == null)
		{
			return new MeshData();
		}
		curType = type;
		tesselator.TesselateShape("crate", shape, out var mesh, this, (rotation == null) ? new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ) : rotation, 0, 0, 0);
		if (label != null && Props.Labels.TryGetValue(label, out var labelProps))
		{
			MeshData meshLabel = GenLabelMesh(capi, label, tmpTextureSource[labelProps.Texture], editableVariant: false, rotation);
			mesh.AddMeshData(meshLabel);
		}
		if (contentStack != null && lidState != "closed")
		{
			MeshData contentMesh = genContentMesh(capi, contentStack, rotation);
			if (contentMesh != null)
			{
				mesh.AddMeshData(contentMesh);
			}
		}
		return mesh;
	}

	public MeshData GenLabelMesh(ICoreClientAPI capi, string label, TextureAtlasPosition texPos, bool editableVariant, Vec3f rotation = null)
	{
		Props.Labels.TryGetValue(label, out var labelProps);
		if (labelProps == null)
		{
			throw new ArgumentException("No label props found for this label");
		}
		AssetLocation shapeloc = (editableVariant ? labelProps.EditableShape : labelProps.Shape).Base.Clone().WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc);
		Vec3f rot = ((rotation == null) ? new Vec3f(labelProps.Shape.rotateX, labelProps.Shape.rotateY, labelProps.Shape.rotateZ) : rotation);
		nowTeselatingLabel = labelProps;
		labelTexturePos = texPos;
		tmpTextureSource = capi.Tesselator.GetTextureSource(this, 0, returnNullWhenMissing: true);
		capi.Tesselator.TesselateShape("cratelabel", shape, out var meshLabel, this, rot, 0, 0, 0);
		nowTeselatingLabel = null;
		return meshLabel;
	}

	protected MeshData genContentMesh(ICoreClientAPI capi, ItemStack contentStack, Vec3f rotation = null)
	{
		float fillHeight;
		ITexPositionSource contentSource = BlockBarrel.getContentTexture(capi, contentStack, out fillHeight);
		if (contentSource != null)
		{
			Shape shape = Vintagestory.API.Common.Shape.TryGet(api, "shapes/block/wood/crate/contents.json");
			capi.Tesselator.TesselateShape("cratecontents", shape, out var contentMesh, contentSource, rotation, 0, 0, 0);
			contentMesh.Translate(0f, fillHeight * 1.1f, 0f);
			return contentMesh;
		}
		return null;
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrate be)
		{
			decalModelData.Rotate(origin, 0f, be.MeshAngle, 0f);
			decalModelData.Scale(origin, 0.9375f, 1f, 0.9375f);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = new ItemStack(this);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrate be)
		{
			stack.Attributes.SetString("type", be.type);
			if (be.label != null && be.label.Length > 0)
			{
				stack.Attributes.SetString("label", be.label);
			}
			stack.Attributes.SetString("lidState", be.preferredLidState);
		}
		else
		{
			stack.Attributes.SetString("type", Props.DefaultType);
		}
		return stack;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[1]
		{
			new BlockDropItemStack(handbookStack)
		};
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityCrate be)
		{
			return be.OnBlockInteractStart(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string type = itemStack.Attributes.GetString("type", Props.DefaultType);
		string lidState = itemStack.Attributes.GetString("lidState", "closed");
		if (lidState.Length == 0)
		{
			lidState = "closed";
		}
		return Lang.GetMatching(Code?.Domain + ":block-" + type + "-" + Code?.Path, Lang.Get("cratelidstate-" + lidState, "closed"));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string type = inSlot.Itemstack.Attributes.GetString("type", Props.DefaultType);
		if (type != null)
		{
			int qslots = Props[type].QuantitySlots;
			dsc.AppendLine("\n" + Lang.Get("Storage Slots: {0}", qslots));
		}
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer be)
		{
			if (!Textures.TryGetValue(be.type + "-lid", out var tex))
			{
				Textures.TryGetValue(be.type + "-top", out tex);
			}
			return capi.BlockTextureAtlas.GetRandomColor((tex?.Baked != null) ? tex.Baked.TextureSubId : 0, rndIndex);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public string GetType(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer be)
		{
			return be.type;
		}
		return Props.DefaultType;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction
		{
			ActionLangCode = "blockhelp-crate-add",
			MouseButton = EnumMouseButton.Right,
			HotKeyCode = "shift"
		}, new WorldInteraction
		{
			ActionLangCode = "blockhelp-crate-addall",
			MouseButton = EnumMouseButton.Right,
			HotKeyCodes = new string[2] { "shift", "ctrl" }
		}, new WorldInteraction
		{
			ActionLangCode = "blockhelp-crate-remove",
			MouseButton = EnumMouseButton.Right,
			HotKeyCode = null
		}, new WorldInteraction
		{
			ActionLangCode = "blockhelp-crate-removeall",
			MouseButton = EnumMouseButton.Right,
			HotKeyCode = "ctrl"
		});
	}
}
