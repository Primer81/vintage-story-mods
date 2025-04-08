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

namespace Vintagestory.GameContent;

public class BlockClutterBookshelf : BlockShapeFromAttributes
{
	private string classtype;

	public OrderedDictionary<string, BookShelfVariantGroup> variantGroupsByCode = new OrderedDictionary<string, BookShelfVariantGroup>();

	public string basePath;

	private AssetLocation woodbackPanelShapePath;

	public override string ClassType => classtype;

	public override IEnumerable<IShapeTypeProps> AllTypes
	{
		get
		{
			List<IShapeTypeProps> result = new List<IShapeTypeProps>();
			foreach (BookShelfVariantGroup variant in variantGroupsByCode.Values)
			{
				result.AddRange(variant.typesByCode.Values);
			}
			return result;
		}
	}

	public override void LoadTypes()
	{
		variantGroupsByCode = Attributes["variantGroups"].AsObject<OrderedDictionary<string, BookShelfVariantGroup>>();
		basePath = Attributes["shapeBasePath"].AsString();
		classtype = Attributes["classtype"].AsString("bookshelf");
		List<JsonItemStack> stacks = new List<JsonItemStack>();
		woodbackPanelShapePath = AssetLocation.Create("shapes/" + basePath + "/" + Attributes["woodbackPanelShapePath"].AsString() + ".json", Code.Domain);
		foreach (KeyValuePair<string, BookShelfVariantGroup> variant in variantGroupsByCode)
		{
			variant.Value.block = this;
			BookShelfTypeProps[] types;
			if (variant.Value.DoubleSided)
			{
				JsonItemStack jstackd = new JsonItemStack
				{
					Code = Code,
					Type = EnumItemClass.Block,
					Attributes = new JsonObject(JToken.Parse("{ \"variant\": \"" + variant.Key + "\" }"))
				};
				jstackd.Resolve(api.World, ClassType + " type");
				stacks.Add(jstackd);
				types = variant.Value.types;
				foreach (BookShelfTypeProps btype in types)
				{
					variant.Value.typesByCode[btype.Code] = btype;
					btype.Variant = variant.Key;
					btype.group = variant.Value;
				}
				variant.Value.types = null;
				continue;
			}
			types = variant.Value.types;
			foreach (BookShelfTypeProps btype2 in types)
			{
				variant.Value.typesByCode[btype2.Code] = btype2;
				btype2.Variant = variant.Key;
				btype2.group = variant.Value;
				JsonItemStack jsonItemStack = new JsonItemStack();
				jsonItemStack.Code = Code;
				jsonItemStack.Type = EnumItemClass.Block;
				jsonItemStack.Attributes = new JsonObject(JToken.Parse("{ \"type\": \"" + btype2.Code + "\", \"variant\": \"" + btype2.Variant + "\" }"));
				JsonItemStack jstack = jsonItemStack;
				jstack.Resolve(api.World, ClassType + " type");
				stacks.Add(jstack);
			}
			variant.Value.types = null;
		}
		CreativeInventoryStacks = new CreativeTabAndStackList[1]
		{
			new CreativeTabAndStackList
			{
				Stacks = stacks.ToArray(),
				Tabs = new string[2] { "general", "clutter" }
			}
		};
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		BEBehaviorClutterBookshelf bec = GetBEBehavior<BEBehaviorClutterBookshelf>(pos);
		if (bec != null && bec.Variant != null)
		{
			variantGroupsByCode.TryGetValue(bec.Variant, out var grp);
			if (grp != null && grp.DoubleSided)
			{
				int angle = (int)(bec.rotateY * (180f / (float)Math.PI));
				if (angle < 0)
				{
					angle += 360;
				}
				switch (angle)
				{
				case 0:
				case 180:
					return blockFace.IsAxisWE;
				case 90:
				case 270:
					return blockFace.IsAxisNS;
				}
			}
		}
		return base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = base.OnPickBlock(world, pos);
		BEBehaviorClutterBookshelf bec = GetBEBehavior<BEBehaviorClutterBookshelf>(pos);
		if (bec != null)
		{
			stack.Attributes.SetString("type", bec.Type);
			stack.Attributes.SetString("variant", bec.Variant);
		}
		return stack;
	}

	public override IShapeTypeProps GetTypeProps(string code, ItemStack stack, BEBehaviorShapeFromAttributes be)
	{
		if (code == null)
		{
			return null;
		}
		string variant = ((stack != null) ? stack.Attributes.GetString("variant") : (be as BEBehaviorClutterBookshelf)?.Variant);
		if (variant == null)
		{
			return null;
		}
		if (variantGroupsByCode.TryGetValue(variant, out var vgroup))
		{
			if (vgroup.DoubleSided)
			{
				string type1;
				string type2;
				if (be != null)
				{
					type1 = (be as BEBehaviorClutterBookshelf).Type;
					type2 = (be as BEBehaviorClutterBookshelf).Type2;
				}
				else
				{
					if (!stack.Attributes.HasAttribute("type1"))
					{
						stack.Attributes.SetString("type1", RandomType(variant));
						stack.Attributes.SetString("type2", RandomType(variant));
					}
					type1 = stack.Attributes.GetString("type1");
					type2 = stack.Attributes.GetString("type2");
				}
				if (!vgroup.typesByCode.TryGetValue(type1, out var t1))
				{
					t1 = vgroup.typesByCode.First((KeyValuePair<string, BookShelfTypeProps> ele) => true).Value;
				}
				if (!vgroup.typesByCode.TryGetValue(type2, out var t2))
				{
					t2 = t1;
				}
				BookShelfTypeProps bookShelfTypeProps = new BookShelfTypeProps();
				bookShelfTypeProps.group = vgroup;
				bookShelfTypeProps.Code = variant + "-" + type1 + "-" + type2;
				bookShelfTypeProps.Type1 = type1;
				bookShelfTypeProps.Type2 = type2;
				bookShelfTypeProps.ShapeResolved = t1.ShapeResolved;
				bookShelfTypeProps.ShapeResolved2 = t2.ShapeResolved;
				bookShelfTypeProps.Variant = variant;
				bookShelfTypeProps.TexPos = vgroup.texPos;
				return bookShelfTypeProps;
			}
			vgroup.typesByCode.TryGetValue(code, out var bprops);
			return bprops;
		}
		return null;
	}

	public override MeshData GetOrCreateMesh(IShapeTypeProps cprops, ITexPositionSource overrideTexturesource = null, string overrideTextureCode = null)
	{
		Dictionary<string, MeshData> cMeshes = meshDictionary;
		ICoreClientAPI capi = api as ICoreClientAPI;
		BookShelfTypeProps bprops = cprops as BookShelfTypeProps;
		if (overrideTexturesource == null && cMeshes.TryGetValue(bprops.HashKey, out var mesh))
		{
			return mesh;
		}
		mesh = new MeshData(4, 3);
		Shape shape = cprops.ShapeResolved;
		if (shape == null)
		{
			return mesh;
		}
		ITexPositionSource texSource = overrideTexturesource;
		ShapeTextureSource stexSource = null;
		if (texSource == null)
		{
			stexSource = new ShapeTextureSource(capi, shape, cprops.ShapePath.ToString());
			texSource = stexSource;
			if (blockTextures != null)
			{
				foreach (KeyValuePair<string, CompositeTexture> val2 in blockTextures)
				{
					if (val2.Value.Baked == null)
					{
						val2.Value.Bake(capi.Assets);
					}
					stexSource.textures[val2.Key] = val2.Value;
				}
			}
		}
		capi.Tesselator.TesselateShape(blockForLogging, shape, out mesh, texSource, null, 0, 0, 0);
		if (bprops.Variant == "full" || bprops.group.DoubleSided)
		{
			mesh.Translate(0f, 0f, 0.5f);
			shape = ((!(bprops.Variant == "full")) ? bprops.ShapeResolved2 : capi.Assets.TryGet(woodbackPanelShapePath)?.ToObject<Shape>());
			texSource = new ShapeTextureSource(capi, shape, ((bprops.Variant == "full") ? woodbackPanelShapePath : bprops.ShapePath2).ToString());
			if (blockTextures != null && stexSource != null)
			{
				foreach (KeyValuePair<string, CompositeTexture> val in blockTextures)
				{
					if (val.Value.Baked == null)
					{
						val.Value.Bake(capi.Assets);
					}
					stexSource.textures[val.Key] = val.Value;
				}
			}
			capi.Tesselator.TesselateShape(blockForLogging, shape, out var mesh2, texSource, null, 0, 0, 0);
			mesh2.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, (float)Math.PI, 0f).Translate(0f, 0f, -0.5f);
			mesh.AddMeshData(mesh2);
		}
		if (cprops.TexPos == null)
		{
			cprops.TexPos = (texSource as ShapeTextureSource)?.firstTexPos;
			cprops.TexPos.RndColors = new int[30];
		}
		if (bprops.group.texPos == null)
		{
			bprops.group.texPos = cprops.TexPos;
		}
		if (overrideTexturesource == null)
		{
			cMeshes[bprops.HashKey] = mesh;
		}
		return mesh;
	}

	public string RandomType(string variant)
	{
		if (variantGroupsByCode == null)
		{
			return null;
		}
		BookShelfVariantGroup vgroup = variantGroupsByCode[variant];
		int rndindex = api.World.Rand.Next(vgroup.typesByCode.Count);
		return vgroup.typesByCode.GetKeyAtIndex(rndindex);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string type = itemStack.Attributes.GetString("type", "");
		string variant = itemStack.Attributes.GetString("variant", "");
		return Lang.GetMatching(Code.Domain + ":" + ((type.Length == 0) ? ("bookshelf-" + variant) : type.Replace("/", "-")));
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		BEBehaviorClutterBookshelf bec = GetBEBehavior<BEBehaviorClutterBookshelf>(pos);
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(Lang.GetMatching(Code.Domain + ":" + (bec?.Type?.Replace("/", "-") ?? "unknown")));
		bec?.GetBlockInfo(forPlayer, sb);
		sb.AppendLine();
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior bh in blockBehaviors)
		{
			sb.Append(bh.GetPlacedBlockInfo(world, pos, forPlayer));
		}
		return sb.ToString();
	}

	public override string BaseCodeForName()
	{
		return Code.Domain + ":";
	}
}
