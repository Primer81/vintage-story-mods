using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockScrollRack : Block
{
	private string[] types;

	private string[] materials;

	private Dictionary<string, CompositeTexture> textures;

	private CompositeShape cshape;

	public Cuboidf[] slotsHitBoxes;

	public string[] slotSide;

	public int[] oppositeSlotIndex;

	public Dictionary<string, int[]> slotsBySide = new Dictionary<string, int[]>();

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		LoadTypes();
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "ScrollrackMeshesInventory");
		if (meshRefs != null && meshRefs.Count > 0)
		{
			foreach (KeyValuePair<string, MultiTextureMeshRef> item in meshRefs)
			{
				item.Deconstruct(out var _, out var value);
				value.Dispose();
			}
			ObjectCacheUtil.Delete(api, "ScrollrackMeshesInventory");
		}
		base.OnUnloaded(api);
	}

	public void LoadTypes()
	{
		types = Attributes["types"].AsArray<string>();
		cshape = Attributes["shape"].AsObject<CompositeShape>();
		textures = Attributes["textures"].AsObject<Dictionary<string, CompositeTexture>>();
		slotsHitBoxes = Attributes["slotsHitBoxes"].AsObject<Cuboidf[]>();
		slotSide = Attributes["slotSide"].AsObject<string[]>();
		oppositeSlotIndex = Attributes["oppositeSlotIndex"].AsObject<int[]>();
		RegistryObjectVariantGroup grp = Attributes["materials"].AsObject<RegistryObjectVariantGroup>();
		materials = grp.States;
		if (grp.LoadFromProperties != null)
		{
			StandardWorldProperty prop = api.Assets.TryGet(grp.LoadFromProperties.WithPathPrefixOnce("worldproperties/").WithPathAppendixOnce(".json"))?.ToObject<StandardWorldProperty>();
			materials = prop.Variants.Select((WorldPropertyVariant p) => p.Code.Path).ToArray().Append(materials);
		}
		for (int i = 0; i < slotSide.Length; i++)
		{
			string side = slotSide[i];
			int[] slots = ((!slotsBySide.TryGetValue(side, out slots)) ? new int[1] { i } : slots.Append(i));
			slotsBySide[side] = slots;
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
		return GetBlockEntity<BlockEntityScrollRack>(pos)?.getOrCreateSelectionBoxes() ?? base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityScrollRack bect)
		{
			BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
			double dz = (double)(float)byPlayer.Entity.Pos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
			float num2 = (float)Math.Atan2(y, dz);
			float intervalRad = (float)Math.PI / 2f;
			float roundRad = (float)(int)Math.Round(num2 / intervalRad) * intervalRad;
			bect.MeshAngleRad = roundRad;
			bect.OnBlockPlaced(byItemStack);
		}
		return num;
	}

	public virtual MeshData GetOrCreateMesh(string type, string material, ITexPositionSource overrideTexturesource = null)
	{
		Dictionary<string, MeshData> cMeshes = ObjectCacheUtil.GetOrCreate(api, "ScrollrackMeshes", () => new Dictionary<string, MeshData>());
		ICoreClientAPI capi = api as ICoreClientAPI;
		string key = type + "-" + material;
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
			capi.Tesselator.TesselateShape("Scrollrack block", shape, out mesh, texSource, null, 0, 0, 0);
			if (overrideTexturesource == null)
			{
				cMeshes[key] = mesh;
			}
		}
		return mesh;
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BlockEntityScrollRack beb = GetBlockEntity<BlockEntityScrollRack>(pos);
		if (beb != null)
		{
			float[] mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(beb.MeshAngleRad)
				.Translate(-0.5f, -0.5f, -0.5f)
				.Values;
			blockModelData = GetOrCreateMesh(beb.Type, beb.Material).Clone().MatrixTransform(mat);
			decalModelData = GetOrCreateMesh(beb.Type, beb.Material, decalTexSource).Clone().MatrixTransform(mat);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		GetBlockEntity<BlockEntityScrollRack>(pos)?.clearUsableSlots();
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "ScrollrackMeshesInventory", () => new Dictionary<string, MultiTextureMeshRef>());
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

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityScrollRack beshelf)
		{
			return beshelf.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = base.OnPickBlock(world, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityScrollRack beshelf)
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
		return Lang.Get("block-scrollrack-" + itemStack.Attributes.GetString("material"));
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityScrollRack beshelf)
		{
			return Lang.Get("block-scrollrack-" + beshelf.Material);
		}
		return base.GetPlacedBlockName(world, pos);
	}
}
