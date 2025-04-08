using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockClutter : BlockShapeFromAttributes, ISearchTextProvider
{
	public Dictionary<string, ClutterTypeProps> clutterByCode = new Dictionary<string, ClutterTypeProps>();

	private string basePath;

	public override string ClassType => "clutter";

	public override IEnumerable<IShapeTypeProps> AllTypes => clutterByCode.Values;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		api.Event.RegisterEventBusListener(onExpClang, 0.5, "expclang");
	}

	private void onExpClang(string eventName, ref EnumHandling handling, IAttribute data)
	{
		ITreeAttribute tree = data as ITreeAttribute;
		foreach (KeyValuePair<string, ClutterTypeProps> val in clutterByCode)
		{
			string langKey = ((Code.Domain == "game") ? "" : (Code.Domain + ":")) + ClassType + "-" + val.Key?.Replace("/", "-");
			if (!Lang.HasTranslation(langKey))
			{
				tree[langKey] = new StringAttribute("\t\"" + langKey + "\": \"" + Lang.GetNamePlaceHolder(new AssetLocation(val.Key)) + "\",");
			}
		}
	}

	public override void LoadTypes()
	{
		ClutterTypeProps[] array = Attributes["types"].AsObject<ClutterTypeProps[]>();
		basePath = "shapes/" + Attributes["shapeBasePath"].AsString() + "/";
		List<JsonItemStack> stacks = new List<JsonItemStack>();
		ModelTransform defaultGui = ModelTransform.BlockDefaultGui();
		ModelTransform defaultFp = ModelTransform.BlockDefaultFp();
		ModelTransform defaultTp = ModelTransform.BlockDefaultTp();
		ModelTransform defaultGround = ModelTransform.BlockDefaultGround();
		ClutterTypeProps[] array2 = array;
		foreach (ClutterTypeProps ct in array2)
		{
			clutterByCode[ct.Code] = ct;
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
			if (ct.ShapePath == null)
			{
				ct.ShapePath = AssetLocation.Create(basePath + ct.Code + ".json", Code.Domain);
			}
			else if (ct.ShapePath.Path.StartsWith('/'))
			{
				ct.ShapePath.WithPathPrefixOnce("shapes").WithPathAppendixOnce(".json");
			}
			else
			{
				ct.ShapePath.WithPathPrefixOnce(basePath).WithPathAppendixOnce(".json");
			}
			JsonItemStack jstack = new JsonItemStack
			{
				Code = Code,
				Type = EnumItemClass.Block,
				Attributes = new JsonObject(JToken.Parse("{ \"type\": \"" + ct.Code + "\" }"))
			};
			jstack.Resolve(api.World, ClassType + " type");
			stacks.Add(jstack);
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

	public static string Remap(IWorldAccessor worldAccessForResolve, string type)
	{
		if (type.StartsWithFast("pipes/"))
		{
			return "pipe-veryrusted-" + type.Substring(6);
		}
		return type;
	}

	public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
	{
		string type = activeHotbarSlot.Itemstack.Attributes.GetString("type", "");
		return GetTypeProps(type, activeHotbarSlot.Itemstack, null)?.HeldIdleAnim ?? base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);
	}

	public override string GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
	{
		string type = activeHotbarSlot.Itemstack.Attributes.GetString("type", "");
		return GetTypeProps(type, activeHotbarSlot.Itemstack, null)?.HeldReadyAnim ?? base.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand);
	}

	public override bool IsClimbable(BlockPos pos)
	{
		BEBehaviorShapeFromAttributes bec = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		if (bec != null && bec.Type != null && clutterByCode.TryGetValue(bec.Type, out var props))
		{
			return props.Climbable;
		}
		return Climbable;
	}

	public override IShapeTypeProps GetTypeProps(string code, ItemStack stack, BEBehaviorShapeFromAttributes be)
	{
		if (code == null)
		{
			return null;
		}
		clutterByCode.TryGetValue(code, out var cprops);
		return cprops;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[1]
		{
			new BlockDropItemStack(handbookStack)
		};
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		string type = baseInfo(inSlot, dsc, world, withDebugInfo);
		ICoreClientAPI obj = api as ICoreClientAPI;
		if (obj != null && obj.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			dsc.AppendLine(Lang.Get("Clutter type: {0}", type));
		}
	}

	private string baseInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine(Lang.Get("Unusable clutter"));
		string type = inSlot.Itemstack.Attributes.GetString("type", "");
		if (type.StartsWithFast("banner-"))
		{
			string[] parts = type.Split('-');
			dsc.AppendLine(Lang.Get("Pattern: {0}", Lang.Get("bannerpattern-" + parts[1])));
			dsc.AppendLine(Lang.Get("Segment: {0}", Lang.Get("bannersegment-" + parts[3])));
		}
		return type;
	}

	public string GetSearchText(IWorldAccessor world, ItemSlot inSlot)
	{
		StringBuilder dsc = new StringBuilder();
		baseInfo(inSlot, dsc, world, withDebugInfo: false);
		string type = inSlot.Itemstack.Attributes.GetString("type", "");
		dsc.AppendLine(Lang.Get("Clutter type: {0}", type));
		return dsc.ToString();
	}
}
