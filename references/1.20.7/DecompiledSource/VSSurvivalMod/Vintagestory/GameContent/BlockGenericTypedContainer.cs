using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockGenericTypedContainer : Block, IAttachableToEntity, IWearableShapeSupplier
{
	private string defaultType;

	private string variantByGroup;

	private string variantByGroupInventory;

	public string Subtype
	{
		get
		{
			if (variantByGroup != null)
			{
				return Variant[variantByGroup];
			}
			return "";
		}
	}

	public string SubtypeInventory
	{
		get
		{
			if (variantByGroupInventory != null)
			{
				return Variant[variantByGroupInventory];
			}
			return "";
		}
	}

	Shape IWearableShapeSupplier.GetShape(ItemStack stack, Entity forEntity, string texturePrefixCode)
	{
		string type = stack.Attributes.GetString("type");
		string shapename = Attributes["shape"][type].AsString();
		Shape shape = GetShape(forEntity.World.Api, shapename);
		shape.SubclassForStepParenting(texturePrefixCode);
		return shape;
	}

	public int GetProvideSlots(ItemStack stack)
	{
		string type = stack.Attributes.GetString("type");
		if (type != null)
		{
			return (stack.ItemAttributes?["quantitySlots"]?[type]?.AsInt()).GetValueOrDefault();
		}
		return 0;
	}

	public string GetCategoryCode(ItemStack stack)
	{
		string type = stack.Attributes?.GetString("type");
		return Attributes["attachableCategoryCode"][type].AsString("chest");
	}

	public CompositeShape GetAttachedShape(ItemStack stack, string slotCode)
	{
		return null;
	}

	public void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
	{
		string type = stack.Attributes.GetString("type");
		foreach (string key in shape.Textures.Keys)
		{
			intoDict[texturePrefixCode + key] = Textures[type + "-" + key];
		}
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
		return Code.ToShortString() + "-" + stack.Attributes.GetString("type") + "-";
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		defaultType = Attributes["defaultType"].AsString("normal-generic");
		variantByGroup = Attributes["variantByGroup"].AsString();
		variantByGroupInventory = Attributes["variantByGroupInventory"].AsString();
	}

	public string GetType(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer be)
		{
			return be.type;
		}
		return defaultType;
	}

	public override List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
	{
		return base.GetHandBookStacks(capi);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntityGenericTypedContainer bect = blockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
		if (bect?.collisionSelectionBoxes != null)
		{
			return bect.collisionSelectionBoxes;
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntityGenericTypedContainer bect = blockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
		if (bect?.collisionSelectionBoxes != null)
		{
			return bect.collisionSelectionBoxes;
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGenericTypedContainer bect)
		{
			BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
			double dz = (double)(float)byPlayer.Entity.Pos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
			float angleHor = (float)Math.Atan2(y, dz);
			string type = bect.type;
			string obj = Attributes?["rotatatableInterval"][type]?.AsString("22.5deg") ?? "22.5deg";
			if (obj == "22.5degnot45deg")
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
			if (obj == "22.5deg")
			{
				float deg22dot5rad = (float)Math.PI / 8f;
				float roundRad = (float)(int)Math.Round(angleHor / deg22dot5rad) * deg22dot5rad;
				bect.MeshAngle = roundRad;
			}
		}
		return num;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		Dictionary<string, MultiTextureMeshRef> meshrefs = new Dictionary<string, MultiTextureMeshRef>();
		string key = "genericTypedContainerMeshRefs" + FirstCodePart() + SubtypeInventory;
		meshrefs = ObjectCacheUtil.GetOrCreate(capi, key, delegate
		{
			foreach (KeyValuePair<string, MeshData> current in GenGuiMeshes(capi))
			{
				meshrefs[current.Key] = capi.Render.UploadMultiTextureMesh(current.Value);
			}
			return meshrefs;
		});
		string type = itemstack.Attributes.GetString("type", defaultType);
		if (!meshrefs.TryGetValue(type, out renderinfo.ModelRef))
		{
			MeshData mesh = GenGuiMesh(capi, type);
			meshrefs[type] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(mesh));
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		base.OnDecalTesselation(world, decalMesh, pos);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI capi))
		{
			return;
		}
		string key = "genericTypedContainerMeshRefs" + FirstCodePart() + SubtypeInventory;
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

	private MeshData GenGuiMesh(ICoreClientAPI capi, string type)
	{
		string shapename = Attributes["shape"][type].AsString();
		return GenMesh(capi, type, shapename);
	}

	public Dictionary<string, MeshData> GenGuiMeshes(ICoreClientAPI capi)
	{
		string[] array = Attributes["types"].AsArray<string>();
		Dictionary<string, MeshData> meshes = new Dictionary<string, MeshData>();
		string[] array2 = array;
		foreach (string type in array2)
		{
			string shapename = Attributes["shape"][type].AsString();
			meshes[type] = GenMesh(capi, type, shapename, null, (ShapeInventory == null) ? null : new Vec3f(ShapeInventory.rotateX, ShapeInventory.rotateY, ShapeInventory.rotateZ));
		}
		return meshes;
	}

	public Shape GetShape(ICoreAPI capi, string shapename)
	{
		if (shapename == null)
		{
			return null;
		}
		AssetLocation shapeloc = AssetLocation.Create(shapename, Code.Domain).WithPathPrefixOnce("shapes/");
		Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, string.Concat(shapeloc, ".json"));
		if (shape == null)
		{
			shape = Vintagestory.API.Common.Shape.TryGet(capi, string.Concat(shapeloc, "1.json"));
		}
		return shape;
	}

	public MeshData GenMesh(ICoreClientAPI capi, string type, string shapename, ITesselatorAPI tesselator = null, Vec3f rotation = null, int altTexNumber = 0)
	{
		Shape shape = GetShape(capi, shapename);
		if (tesselator == null)
		{
			tesselator = capi.Tesselator;
		}
		if (shape == null)
		{
			capi.Logger.Warning("Container block {0}, type: {1}: Shape file {2} not found!", Code, type, shapename);
			return new MeshData();
		}
		GenericContainerTextureSource texSource = new GenericContainerTextureSource
		{
			blockTextureSource = tesselator.GetTextureSource(this, altTexNumber),
			curType = type
		};
		TesselationMetaData meta = new TesselationMetaData
		{
			TexSource = texSource,
			WithJointIds = true,
			WithDamageEffect = true,
			TypeForLogging = "typedcontainer",
			Rotation = ((rotation == null) ? new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ) : rotation)
		};
		tesselator.TesselateShape(meta, shape, out var mesh);
		return mesh;
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer be)
		{
			ICoreClientAPI capi = api as ICoreClientAPI;
			string shapename = Attributes["shape"][be.type].AsString();
			if (shapename == null)
			{
				base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
				return;
			}
			blockModelData = GenMesh(capi, be.type, shapename);
			AssetLocation shapeloc = AssetLocation.Create(shapename, Code.Domain).WithPathPrefixOnce("shapes/");
			Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, string.Concat(shapeloc, ".json"));
			if (shape == null)
			{
				shape = Vintagestory.API.Common.Shape.TryGet(capi, string.Concat(shapeloc, "1.json"));
			}
			GenericContainerTextureSource texSource = new GenericContainerTextureSource
			{
				blockTextureSource = decalTexSource,
				curType = be.type
			};
			capi.Tesselator.TesselateShape("typedcontainer-decal", shape, out var md, texSource, null, 0, 0, 0);
			decalModelData = md;
			decalModelData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, be.MeshAngle, 0f);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")));
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer be)
		{
			stack.Attributes.SetString("type", be.type);
		}
		else
		{
			stack.Attributes.SetString("type", defaultType);
		}
		return stack;
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
			ItemStack[] drops = new ItemStack[1] { OnPickBlock(world, pos) };
			JsonObject jsonObject = Attributes["drop"];
			if (jsonObject != null && (jsonObject[GetType(world.BlockAccessor, pos)]?.AsBool()).GetValueOrDefault() && drops != null)
			{
				for (int i = 0; i < drops.Length; i++)
				{
					world.SpawnItemEntity(drops[i], pos);
				}
			}
			world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, -0.5, byPlayer);
		}
		if (EntityClass != null)
		{
			world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
		}
		world.BlockAccessor.SetBlock(0, pos);
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		string type = handbookStack.Attributes?.GetString("type");
		if (type == null)
		{
			api.World.Logger.Warning("BlockGenericTypedContainer.GetDropsForHandbook(): type not set for block " + handbookStack.Collectible?.Code);
			return new BlockDropItemStack[0];
		}
		JsonObject attributes = Attributes;
		if (attributes == null || attributes["drop"]?[type]?.AsBool() != false)
		{
			return new BlockDropItemStack[1]
			{
				new BlockDropItemStack(handbookStack)
			};
		}
		return new BlockDropItemStack[0];
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1]
		{
			new ItemStack(world.GetBlock(CodeWithVariant("side", "east")))
		};
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string type = itemStack.Attributes.GetString("type");
		return Lang.GetMatching(Code?.Domain + ":block-" + type + "-" + Code?.Path);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string type = inSlot.Itemstack.Attributes.GetString("type");
		if (type != null)
		{
			int? qslots = inSlot.Itemstack.ItemAttributes?["quantitySlots"]?[type]?.AsInt();
			dsc.AppendLine("\n" + Lang.Get("Storage Slots: {0}", qslots));
		}
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer be)
		{
			CompositeTexture tex = null;
			if (!Textures.TryGetValue(be.type + "-lid", out tex))
			{
				Textures.TryGetValue(be.type + "-top", out tex);
			}
			return capi.BlockTextureAtlas.GetRandomColor((tex?.Baked != null) ? tex.Baked.TextureSubId : 0, rndIndex);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-chest-open",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public virtual bool IsAttachable(Entity toEntity, ItemStack itemStack)
	{
		if (toEntity is EntityPlayer)
		{
			return false;
		}
		ITreeAttribute attributes = itemStack.Attributes;
		if (attributes != null && attributes.HasAttribute("animalSerialized"))
		{
			return false;
		}
		return true;
	}
}
