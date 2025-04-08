using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockBookshelf : Block
{
	private string[] types;

	private string[] materials;

	private Dictionary<string, CompositeTexture> textures;

	private CompositeShape cshape;

	public Dictionary<string, int[]> UsableSlots;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		LoadTypes();
	}

	public void LoadTypes()
	{
		types = Attributes["types"].AsArray<string>();
		cshape = Attributes["shape"].AsObject<CompositeShape>();
		textures = Attributes["textures"].AsObject<Dictionary<string, CompositeTexture>>();
		RegistryObjectVariantGroup grp = Attributes["materials"].AsObject<RegistryObjectVariantGroup>();
		UsableSlots = Attributes["usableSlots"].AsObject<Dictionary<string, int[]>>();
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
		BlockEntityBookshelf beshelf = blockAccessor.GetBlockEntity(pos) as BlockEntityBookshelf;
		if (beshelf?.UsableSlots != null)
		{
			List<Cuboidf> cubs = new List<Cuboidf>
			{
				new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.1f),
				new Cuboidf(0f, 0f, 0f, 1f, 0.0625f, 0.5f),
				new Cuboidf(0f, 0.9375f, 0f, 1f, 1f, 0.5f),
				new Cuboidf(0f, 0f, 0f, 0.0625f, 1f, 0.5f),
				new Cuboidf(0.9375f, 0f, 0f, 1f, 1f, 0.5f)
			};
			for (int j = 0; j < 14; j++)
			{
				if (!beshelf.UsableSlots.Contains(j))
				{
					cubs.Add(new Cuboidf());
					continue;
				}
				float x = (float)(j % 7) * 2f / 16f + 11f / 160f;
				float y = (float)(j / 7) * 7.5f / 16f;
				float z = 13f / 32f;
				Cuboidf cub = new Cuboidf(x, y + 0.0625f, 0.0625f, x + 19f / 160f, y + 0.4375f, z);
				cubs.Add(cub);
			}
			for (int i = 0; i < cubs.Count; i++)
			{
				cubs[i] = cubs[i].RotatedCopy(0f, (beshelf?.MeshAngleRad ?? 0f) * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.5, 0.5));
			}
			return cubs.ToArray();
		}
		return new Cuboidf[1] { new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.5f).RotatedCopy(0f, (beshelf?.MeshAngleRad ?? 0f) * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.5, 0.5)) };
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntityBookshelf beshelf = blockAccessor.GetBlockEntity(pos) as BlockEntityBookshelf;
		return new Cuboidf[1] { new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.5f).RotatedCopy(0f, (beshelf?.MeshAngleRad ?? 0f) * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.5, 0.5)) };
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBookshelf bect)
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
		Dictionary<string, MeshData> cMeshes = ObjectCacheUtil.GetOrCreate(api, "BookshelfMeshes", () => new Dictionary<string, MeshData>());
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
			capi.Tesselator.TesselateShape("Bookshelf block", shape, out mesh, texSource, null, 0, 0, 0);
			if (overrideTexturesource == null)
			{
				cMeshes[key] = mesh;
			}
		}
		return mesh;
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BlockEntityBookshelf beb = GetBlockEntity<BlockEntityBookshelf>(pos);
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

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "BookshelfMeshesInventory", () => new Dictionary<string, MultiTextureMeshRef>());
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
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBookshelf beshelf)
		{
			return beshelf.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = base.OnPickBlock(world, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBookshelf beshelf)
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

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "BookshelfMeshesInventory");
		if (meshRefs == null)
		{
			return;
		}
		foreach (KeyValuePair<string, MultiTextureMeshRef> item in meshRefs)
		{
			item.Value?.Dispose();
		}
	}
}
