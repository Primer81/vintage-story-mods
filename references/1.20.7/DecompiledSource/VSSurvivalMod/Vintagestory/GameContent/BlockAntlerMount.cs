using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockAntlerMount : Block
{
	private string[] types;

	private string[] materials;

	private Dictionary<string, CompositeTexture> textures;

	private CompositeShape cshape;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		LoadTypes();
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		Dictionary<string, MeshData> antlerMeshes = ObjectCacheUtil.TryGet<Dictionary<string, MeshData>>(api, "AntlerMountMeshes");
		string key;
		if (antlerMeshes != null && antlerMeshes.Count > 0)
		{
			foreach (KeyValuePair<string, MeshData> item in antlerMeshes)
			{
				item.Deconstruct(out key, out var value);
				value.Dispose();
			}
			ObjectCacheUtil.Delete(api, "AntlerMountMeshes");
		}
		Dictionary<string, MultiTextureMeshRef> antlerInvMeshes = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "AntlerMountMeshesInventory");
		if (antlerInvMeshes != null && antlerInvMeshes.Count > 0)
		{
			foreach (KeyValuePair<string, MultiTextureMeshRef> item2 in antlerInvMeshes)
			{
				item2.Deconstruct(out key, out var value2);
				value2.Dispose();
			}
			ObjectCacheUtil.Delete(api, "AntlerMountMeshesInventory");
		}
		base.OnUnloaded(api);
	}

	public void LoadTypes()
	{
		types = Attributes["types"].AsArray<string>();
		cshape = Attributes["shape"].AsObject<CompositeShape>();
		textures = Attributes["textures"].AsObject<Dictionary<string, CompositeTexture>>();
		RegistryObjectVariantGroup grp = Attributes["materials"].AsObject<RegistryObjectVariantGroup>();
		materials = grp.States;
		if (grp.LoadFromProperties != null)
		{
			StandardWorldProperty prop = api.Assets.TryGet(grp.LoadFromProperties.WithPathPrefixOnce("worldproperties/").WithPathAppendixOnce(".json"))?.ToObject<StandardWorldProperty>();
			materials = prop.Variants.Select((WorldPropertyVariant p) => p.Code.Path).ToArray().Append(materials);
		}
		List<JsonItemStack> stacks = new List<JsonItemStack>();
		string[] array = types;
		foreach (string type in array)
		{
			string[] array2 = materials;
			foreach (string material in array2)
			{
				JsonItemStack jsonItemStack = new JsonItemStack();
				jsonItemStack.Code = Code;
				jsonItemStack.Type = EnumItemClass.Block;
				jsonItemStack.Attributes = new JsonObject(JToken.Parse("{ \"type\": \"" + type + "\", \"material\": \"" + material + "\" }"));
				JsonItemStack jstack = jsonItemStack;
				jstack.Resolve(api.World, string.Concat(Code, " type"));
				stacks.Add(jstack);
			}
		}
		CreativeInventoryStacks = new CreativeTabAndStackList[1]
		{
			new CreativeTabAndStackList
			{
				Stacks = stacks.ToArray(),
				Tabs = new string[2] { "general", "decorative" }
			}
		};
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		float degY = ((!(blockAccessor.GetBlockEntity(pos) is BlockEntityAntlerMount bect)) ? 0f : (bect.MeshAngleRad * (180f / (float)Math.PI)));
		return new Cuboidf[1] { SelectionBoxes[0].RotatedCopy(0f, degY, 0f, new Vec3d(0.5, 0.5, 0.5)) };
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return GetSelectionBoxes(blockAccessor, pos);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (blockSel.Face.IsHorizontal)
		{
			if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode))
			{
				return true;
			}
			if (failureCode == "entityintersecting")
			{
				return false;
			}
		}
		BlockFacing[] faces = BlockFacing.HORIZONTALS;
		blockSel = blockSel.Clone();
		for (int i = 0; i < faces.Length; i++)
		{
			blockSel.Face = faces[i];
			if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode))
			{
				return true;
			}
		}
		failureCode = "requirehorizontalattachable";
		return false;
	}

	private bool TryAttachTo(IWorldAccessor world, IPlayer player, BlockSelection blockSel, ItemStack itemstack, ref string failureCode)
	{
		BlockFacing oppositeFace = blockSel.Face.Opposite;
		BlockPos attachingBlockPos = blockSel.Position.AddCopy(oppositeFace);
		if (world.BlockAccessor.GetBlock(attachingBlockPos).CanAttachBlockAt(world.BlockAccessor, this, attachingBlockPos, blockSel.Face) && CanPlaceBlock(world, player, blockSel, ref failureCode))
		{
			DoPlaceBlock(world, player, blockSel, itemstack);
			return true;
		}
		return false;
	}

	private bool CanBlockStay(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing facing = BlockFacing.HorizontalFromAngle(((world.BlockAccessor.GetBlockEntity(pos) as BlockEntityAntlerMount)?.MeshAngleRad ?? 0f) + (float)Math.PI / 2f);
		return world.BlockAccessor.GetBlock(pos.AddCopy(facing)).CanAttachBlockAt(world.BlockAccessor, this, pos.AddCopy(facing), facing.Opposite);
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		return false;
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (val && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityAntlerMount bect)
		{
			for (int i = 0; i < 4; i++)
			{
				int faceIndex = (blockSel.Face.HorizontalAngleIndex + i) % 4;
				BlockPos attachingBlockPos = blockSel.Position.AddCopy(BlockFacing.HORIZONTALS_ANGLEORDER[faceIndex]);
				if (world.BlockAccessor.GetBlock(attachingBlockPos).CanAttachBlockAt(world.BlockAccessor, this, attachingBlockPos, blockSel.Face))
				{
					bect.MeshAngleRad = (float)(faceIndex * 90) * ((float)Math.PI / 180f) - (float)Math.PI / 2f;
					bect.OnBlockPlaced(byItemStack);
				}
			}
		}
		return val;
	}

	public Shape GetOrCreateShape(string type, string material)
	{
		ICoreClientAPI obj = api as ICoreClientAPI;
		CompositeShape rcshape = cshape.Clone();
		rcshape.Base.Path = rcshape.Base.Path.Replace("{type}", type).Replace("{material}", material);
		rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		return obj.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
	}

	public MeshData GetOrCreateMesh(string type, string material, string cachekeyextra = null, ITexPositionSource overrideTexturesource = null)
	{
		Dictionary<string, MeshData> cMeshes = ObjectCacheUtil.GetOrCreate(api, "AntlerMountMeshes", () => new Dictionary<string, MeshData>());
		ICoreClientAPI capi = api as ICoreClientAPI;
		string key = type + "-" + material + cachekeyextra;
		if (overrideTexturesource != null || !cMeshes.TryGetValue(key, out var mesh))
		{
			mesh = new MeshData(4, 3);
			CompositeShape rcshape = cshape.Clone();
			rcshape.Base.Path = rcshape.Base.Path.Replace("{type}", type).Replace("{material}", material);
			rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
			Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
			ITexPositionSource texSource = overrideTexturesource;
			if (texSource == null)
			{
				ShapeTextureSource stexSource = new ShapeTextureSource(capi, shape, rcshape.Base.ToString());
				texSource = stexSource;
				foreach (KeyValuePair<string, CompositeTexture> val in textures)
				{
					CompositeTexture ctex = val.Value.Clone();
					ctex.Base.Path = ctex.Base.Path.Replace("{type}", type).Replace("{material}", material);
					ctex.Bake(capi.Assets);
					stexSource.textures[val.Key] = ctex;
				}
			}
			if (shape == null)
			{
				return mesh;
			}
			capi.Tesselator.TesselateShape("AntlerMount block", shape, out mesh, texSource, null, 0, 0, 0);
			if (overrideTexturesource == null)
			{
				cMeshes[key] = mesh;
			}
		}
		return mesh;
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BlockEntityAntlerMount beb = GetBlockEntity<BlockEntityAntlerMount>(pos);
		if (beb != null)
		{
			float[] mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(beb.MeshAngleRad)
				.Translate(-0.5f, -0.5f, -0.5f)
				.Values;
			blockModelData = GetOrCreateMesh(beb.Type, beb.Material).Clone().MatrixTransform(mat);
			decalModelData = GetOrCreateMesh(beb.Type, beb.Material, null, decalTexSource).Clone().MatrixTransform(mat);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos);
		if (!CanBlockStay(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "AntlerMountMeshesInventory", () => new Dictionary<string, MultiTextureMeshRef>());
		string type = itemstack.Attributes.GetString("type", "");
		string material = itemstack.Attributes.GetString("material", "");
		string key = type + "-" + material;
		if (!meshRefs.TryGetValue(key, out var meshref))
		{
			MeshData mesh = GetOrCreateMesh(type, material);
			meshref = (meshRefs[key] = capi.Render.UploadMultiTextureMesh(mesh));
		}
		renderinfo.ModelRef = meshref;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityAntlerMount beshelf)
		{
			return beshelf.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = base.OnPickBlock(world, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityAntlerMount beshelf)
		{
			stack.Attributes.SetString("type", beshelf.Type);
			stack.Attributes.SetString("material", beshelf.Material);
		}
		return stack;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		BlockDropItemStack[] drops = base.GetDropsForHandbook(handbookStack, forPlayer);
		drops[0] = drops[0].Clone();
		drops[0].ResolvedItemstack.SetFrom(handbookStack);
		return drops;
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string type = itemStack.Attributes.GetString("type", "square");
		return Lang.Get("block-antlermount-" + type);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityAntlerMount bemount))
		{
			return base.GetPlacedBlockName(world, pos);
		}
		return Lang.Get("block-antlermount-" + bemount.Type);
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityAntlerMount bemount))
		{
			return base.GetPlacedBlockInfo(world, pos, forPlayer);
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer) + "\n" + Lang.Get("Material: {0}", Lang.Get("material-" + bemount.Material));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string wood = inSlot.Itemstack.Attributes.GetString("material", "oak");
		dsc.AppendLine(Lang.Get("Material: {0}", Lang.Get("material-" + wood)));
	}
}
