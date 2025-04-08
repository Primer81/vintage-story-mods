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

public class BlockRockTyped : BlockShapeFromAttributes
{
	public Dictionary<string, ClutterTypeProps> clutterByCode = new Dictionary<string, ClutterTypeProps>();

	public override string ClassType => "rocktyped-" + Variant["cover"];

	public override IEnumerable<IShapeTypeProps> AllTypes => clutterByCode.Values;

	public override IShapeTypeProps GetTypeProps(string code, ItemStack stack, BEBehaviorShapeFromAttributes be)
	{
		if (code == null)
		{
			return null;
		}
		clutterByCode.TryGetValue(code, out var cprops);
		return cprops;
	}

	public override void LoadTypes()
	{
		ClutterTypeProps[] array = Attributes["types"].AsObject<ClutterTypeProps[]>();
		StandardWorldProperty rocktypes = api.Assets.Get("worldproperties/block/rock.json").ToObject<StandardWorldProperty>();
		List<JsonItemStack> stacks = new List<JsonItemStack>();
		ModelTransform defaultGui = ModelTransform.BlockDefaultGui();
		ModelTransform defaultFp = ModelTransform.BlockDefaultFp();
		ModelTransform defaultTp = ModelTransform.BlockDefaultTp();
		ModelTransform defaultGround = ModelTransform.BlockDefaultGround();
		ClutterTypeProps[] array2 = array;
		foreach (ClutterTypeProps ct in array2)
		{
			if (ct.GuiTf != null)
			{
				ct.GuiTransform = new ModelTransform(ct.GuiTf, defaultGui);
			}
			if (ct.FpTf != null)
			{
				ct.FpTtransform = new ModelTransform(ct.FpTf, defaultFp);
			}
			if (ct.TpTf != null)
			{
				ct.TpTransform = new ModelTransform(ct.TpTf, defaultTp);
			}
			if (ct.GroundTf != null)
			{
				ct.GroundTransform = new ModelTransform(ct.GroundTf, defaultGround);
			}
			ct.ShapePath.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
			WorldPropertyVariant[] variants = rocktypes.Variants;
			foreach (WorldPropertyVariant rocktype in variants)
			{
				ClutterTypeProps rct = ct.Clone();
				rct.Code = rct.Code + "-" + rocktype.Code.Path;
				clutterByCode[rct.Code] = rct;
				foreach (CompositeTexture value in rct.Textures.Values)
				{
					value.FillPlaceholder("{rock}", rocktype.Code.Path);
				}
				if (rct.Drops != null)
				{
					BlockDropItemStack[] drops = rct.Drops;
					foreach (BlockDropItemStack drop in drops)
					{
						drop.Code.Path = drop.Code.Path.Replace("{rock}", rocktype.Code.Path);
						drop.Resolve(api.World, "rock typed block drop", Code);
					}
				}
				JsonItemStack jsonItemStack = new JsonItemStack();
				jsonItemStack.Code = Code;
				jsonItemStack.Type = EnumItemClass.Block;
				jsonItemStack.Attributes = new JsonObject(JToken.Parse("{ \"type\": \"" + rct.Code + "\", \"rock\": \"" + rocktype.Code.Path + "\" }"));
				JsonItemStack jstack = jsonItemStack;
				jstack.Resolve(api.World, ClassType + " type");
				stacks.Add(jstack);
			}
		}
		if (Variant["cover"] != "snow")
		{
			CreativeInventoryStacks = new CreativeTabAndStackList[1]
			{
				new CreativeTabAndStackList
				{
					Stacks = stacks.ToArray(),
					Tabs = new string[2] { "general", "terrain" }
				}
			};
		}
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		return GetTypeProps(bect?.Type, null, bect)?.Drops?.Select((BlockDropItemStack drop) => drop.GetNextItemStack(dropQuantityMultiplier)).ToArray() ?? new ItemStack[0];
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		BlockDropItemStack[] drops = base.GetDropsForHandbook(handbookStack, forPlayer);
		drops[0] = drops[0].Clone();
		drops[0].ResolvedItemstack.SetFrom(handbookStack);
		return drops;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string type = getRockType(inSlot.Itemstack.Attributes.GetString("type"));
		dsc.AppendLine(Lang.Get("rock-" + type));
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		BEBehaviorShapeFromAttributes bect = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		if (bect != null && bect.Type != null)
		{
			return Lang.Get("rock-" + getRockType(bect.Type));
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	private string getRockType(string type)
	{
		string[] parts = type.Split('-');
		if (parts.Length < 3)
		{
			return "unknown";
		}
		return parts[2];
	}
}
