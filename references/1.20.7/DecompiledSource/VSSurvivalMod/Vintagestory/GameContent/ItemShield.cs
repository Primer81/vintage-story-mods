using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemShield : Item, IContainedMeshSource
{
	private float offY;

	private float curOffY;

	private ICoreClientAPI capi;

	private Dictionary<string, Dictionary<string, int>> durabilityGains;

	private Dictionary<int, MultiTextureMeshRef> meshrefs => ObjectCacheUtil.GetOrCreate(api, "shieldmeshrefs", () => new Dictionary<int, MultiTextureMeshRef>());

	public string Construction => Variant["construction"];

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		curOffY = (offY = FpHandTransform.Translation.Y);
		capi = api as ICoreClientAPI;
		durabilityGains = Attributes["durabilityGains"].AsObject<Dictionary<string, Dictionary<string, int>>>();
		AddAllTypesToCreativeInventory();
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (api.ObjectCache.ContainsKey("shieldmeshrefs") && meshrefs.Count > 0)
		{
			foreach (KeyValuePair<int, MultiTextureMeshRef> meshref in meshrefs)
			{
				meshref.Deconstruct(out var _, out var value);
				value.Dispose();
			}
			ObjectCacheUtil.Delete(api, "shieldmeshrefs");
		}
		base.OnUnloaded(api);
	}

	public override int GetMaxDurability(ItemStack itemstack)
	{
		int gain = 0;
		foreach (KeyValuePair<string, Dictionary<string, int>> val in durabilityGains)
		{
			string mat = itemstack.Attributes.GetString(val.Key);
			if (mat != null)
			{
				val.Value.TryGetValue(mat, out var matgain);
				gain += matgain;
			}
		}
		return base.GetMaxDurability(itemstack) + gain;
	}

	public void AddAllTypesToCreativeInventory()
	{
		if (Construction == "crude" || Construction == "blackguard")
		{
			return;
		}
		List<JsonItemStack> stacks = new List<JsonItemStack>();
		Dictionary<string, string[]> vg = Attributes["variantGroups"].AsObject<Dictionary<string, string[]>>();
		string[] array = vg["metal"];
		foreach (string metal in array)
		{
			switch (Construction)
			{
			case "woodmetal":
			{
				string[] array2 = vg["wood"];
				foreach (string wood in array2)
				{
					stacks.Add(genJstack($"{{ wood: \"{wood}\", metal: \"{metal}\", deco: \"none\" }}"));
				}
				break;
			}
			case "woodmetalleather":
			{
				string[] array2 = vg["color"];
				foreach (string color2 in array2)
				{
					stacks.Add(genJstack(string.Format("{{ wood: \"{0}\", metal: \"{1}\", color: \"{2}\", deco: \"none\" }}", "generic", metal, color2)));
					if (color2 != "redblack")
					{
						stacks.Add(genJstack(string.Format("{{ wood: \"{0}\", metal: \"{1}\", color: \"{2}\", deco: \"ornate\" }}", "generic", metal, color2)));
					}
				}
				break;
			}
			case "metal":
			{
				stacks.Add(genJstack(string.Format("{{ wood: \"{0}\", metal: \"{1}\", deco: \"none\" }}", "generic", metal)));
				string[] array2 = vg["color"];
				foreach (string color in array2)
				{
					if (color != "redblack")
					{
						stacks.Add(genJstack(string.Format("{{ wood: \"{0}\", metal: \"{1}\", color: \"{2}\", deco: \"ornate\" }}", "generic", metal, color)));
					}
				}
				break;
			}
			}
		}
		CreativeInventoryStacks = new CreativeTabAndStackList[1]
		{
			new CreativeTabAndStackList
			{
				Stacks = stacks.ToArray(),
				Tabs = new string[3] { "general", "items", "tools" }
			}
		};
	}

	private JsonItemStack genJstack(string json)
	{
		JsonItemStack jsonItemStack = new JsonItemStack();
		jsonItemStack.Code = Code;
		jsonItemStack.Type = EnumItemClass.Item;
		jsonItemStack.Attributes = new JsonObject(JToken.Parse(json));
		jsonItemStack.Resolve(api.World, "shield type");
		return jsonItemStack;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		int meshrefid = itemstack.TempAttributes.GetInt("meshRefId");
		if (meshrefid == 0 || !meshrefs.TryGetValue(meshrefid, out renderinfo.ModelRef))
		{
			int id = meshrefs.Count + 1;
			MultiTextureMeshRef modelref = capi.Render.UploadMultiTextureMesh(GenMesh(itemstack, capi.ItemTextureAtlas));
			ItemRenderInfo obj = renderinfo;
			MultiTextureMeshRef modelRef = (meshrefs[id] = modelref);
			obj.ModelRef = modelRef;
			itemstack.TempAttributes.SetInt("meshRefId", id);
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
	{
		string onhand = ((byEntity.LeftHandItemSlot == slot) ? "left" : "right");
		string notonhand = ((byEntity.LeftHandItemSlot == slot) ? "right" : "left");
		if (byEntity.Controls.Sneak && !byEntity.Controls.RightMouseDown)
		{
			if (!byEntity.AnimManager.IsAnimationActive("raiseshield-" + onhand))
			{
				byEntity.AnimManager.StartAnimation("raiseshield-" + onhand);
			}
		}
		else if (byEntity.AnimManager.IsAnimationActive("raiseshield-" + onhand))
		{
			byEntity.AnimManager.StopAnimation("raiseshield-" + onhand);
		}
		if (byEntity.AnimManager.IsAnimationActive("raiseshield-" + notonhand))
		{
			byEntity.AnimManager.StopAnimation("raiseshield-" + notonhand);
		}
		base.OnHeldIdle(slot, byEntity);
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
	{
		ContainedTextureSource cnts = new ContainedTextureSource(api as ICoreClientAPI, targetAtlas, new Dictionary<string, AssetLocation>(), $"For render in shield {Code}");
		cnts.Textures.Clear();
		string wood = itemstack.Attributes.GetString("wood");
		string metal = itemstack.Attributes.GetString("metal");
		string color = itemstack.Attributes.GetString("color");
		string deco = itemstack.Attributes.GetString("deco");
		if (wood == null && metal == null && Construction != "crude" && Construction != "blackguard")
		{
			return new MeshData();
		}
		if (wood == null || wood == "")
		{
			wood = "generic";
		}
		Dictionary<string, AssetLocation> textures = cnts.Textures;
		Dictionary<string, AssetLocation> textures2 = cnts.Textures;
		AssetLocation assetLocation2 = (cnts.Textures["handle"] = new AssetLocation("block/wood/planks/generic.png"));
		AssetLocation value = (textures2["back"] = assetLocation2);
		textures["front"] = value;
		foreach (KeyValuePair<string, AssetLocation> ctex in capi.TesselatorManager.GetCachedShape(Shape.Base).Textures)
		{
			cnts.Textures[ctex.Key] = ctex.Value;
		}
		switch (Construction)
		{
		case "woodmetal":
			if (wood != "generic")
			{
				Dictionary<string, AssetLocation> textures5 = cnts.Textures;
				Dictionary<string, AssetLocation> textures6 = cnts.Textures;
				assetLocation2 = (cnts.Textures["front"] = new AssetLocation("block/wood/debarked/" + wood + ".png"));
				value = (textures6["back"] = assetLocation2);
				textures5["handle"] = value;
			}
			cnts.Textures["rim"] = new AssetLocation("block/metal/sheet/" + metal + "1.png");
			if (deco == "ornate")
			{
				cnts.Textures["front"] = new AssetLocation("item/tool/shield/ornate/" + color + ".png");
			}
			break;
		case "woodmetalleather":
			if (wood != "generic")
			{
				Dictionary<string, AssetLocation> textures7 = cnts.Textures;
				Dictionary<string, AssetLocation> textures8 = cnts.Textures;
				assetLocation2 = (cnts.Textures["front"] = new AssetLocation("block/wood/debarked/" + wood + ".png"));
				value = (textures8["back"] = assetLocation2);
				textures7["handle"] = value;
			}
			cnts.Textures["front"] = new AssetLocation("item/tool/shield/leather/" + color + ".png");
			cnts.Textures["rim"] = new AssetLocation("block/metal/sheet/" + metal + "1.png");
			if (deco == "ornate")
			{
				cnts.Textures["front"] = new AssetLocation("item/tool/shield/ornate/" + color + ".png");
			}
			break;
		case "metal":
		{
			Dictionary<string, AssetLocation> textures3 = cnts.Textures;
			value = (cnts.Textures["handle"] = new AssetLocation("block/metal/sheet/" + metal + "1.png"));
			textures3["rim"] = value;
			Dictionary<string, AssetLocation> textures4 = cnts.Textures;
			value = (cnts.Textures["back"] = new AssetLocation("block/metal/plate/" + metal + ".png"));
			textures4["front"] = value;
			if (deco == "ornate")
			{
				cnts.Textures["front"] = new AssetLocation("item/tool/shield/ornate/" + color + ".png");
			}
			break;
		}
		}
		capi.Tesselator.TesselateItem(this, out var mesh, cnts);
		return mesh;
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		bool ornate = itemStack.Attributes.GetString("deco") == "ornate";
		string metal = itemStack.Attributes.GetString("metal");
		string wood = itemStack.Attributes.GetString("wood");
		string color = itemStack.Attributes.GetString("color");
		switch (Construction)
		{
		case "crude":
			return Lang.Get("Crude shield");
		case "woodmetal":
			if (wood == "generic")
			{
				if (!ornate)
				{
					return Lang.Get("Wooden shield");
				}
				return Lang.Get("Ornate wooden shield");
			}
			if (wood == "aged")
			{
				if (!ornate)
				{
					return Lang.Get("Aged wooden shield");
				}
				return Lang.Get("Aged ornate shield");
			}
			if (!ornate)
			{
				return Lang.Get("{0} shield", Lang.Get("material-" + wood));
			}
			return Lang.Get("Ornate {0} shield", Lang.Get("material-" + wood));
		case "woodmetalleather":
			if (!ornate)
			{
				return Lang.Get("Leather reinforced wooden shield");
			}
			return Lang.Get("Ornate leather reinforced wooden shield");
		case "metal":
			if (!ornate)
			{
				return Lang.Get("shield-withmaterial", Lang.Get("material-" + metal));
			}
			return Lang.Get("shield-ornatemetal", Lang.Get("color-" + color), Lang.Get("material-" + metal));
		case "blackguard":
			return Lang.Get("Blackguard shield");
		default:
			return base.GetHeldItemName(itemStack);
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		JsonObject attr = inSlot.Itemstack?.ItemAttributes?["shield"];
		if (attr == null || !attr.Exists)
		{
			return;
		}
		if (attr["protectionChance"]["active-projectile"].Exists)
		{
			float pacchance = attr["protectionChance"]["active-projectile"].AsFloat();
			float ppachance = attr["protectionChance"]["passive-projectile"].AsFloat();
			float pflatdmgabsorb = attr["projectileDamageAbsorption"].AsFloat();
			dsc.AppendLine("<strong>" + Lang.Get("Projectile protection") + "</strong>");
			dsc.AppendLine(Lang.Get("shield-stats", (int)(100f * pacchance), (int)(100f * ppachance), pflatdmgabsorb));
			dsc.AppendLine();
		}
		float flatdmgabsorb = attr["damageAbsorption"].AsFloat();
		float acchance = attr["protectionChance"]["active"].AsFloat();
		float pachance = attr["protectionChance"]["passive"].AsFloat();
		dsc.AppendLine("<strong>" + Lang.Get("Melee attack protection") + "</strong>");
		dsc.AppendLine(Lang.Get("shield-stats", (int)(100f * acchance), (int)(100f * pachance), flatdmgabsorb));
		dsc.AppendLine();
		string construction = Construction;
		if (!(construction == "woodmetal"))
		{
			if (construction == "woodmetalleather")
			{
				dsc.AppendLine(Lang.Get("shield-metaltype", Lang.Get("material-" + inSlot.Itemstack.Attributes.GetString("metal"))));
			}
		}
		else
		{
			dsc.AppendLine(Lang.Get("shield-woodtype", Lang.Get("material-" + inSlot.Itemstack.Attributes.GetString("wood"))));
			dsc.AppendLine(Lang.Get("shield-metaltype", Lang.Get("material-" + inSlot.Itemstack.Attributes.GetString("metal"))));
		}
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
	{
		return GenMesh(itemstack, targetAtlas);
	}

	public string GetMeshCacheKey(ItemStack itemstack)
	{
		string wood = itemstack.Attributes.GetString("wood");
		string metal = itemstack.Attributes.GetString("metal");
		string color = itemstack.Attributes.GetString("color");
		string deco = itemstack.Attributes.GetString("deco");
		return Code.ToShortString() + "-" + wood + "-" + metal + "-" + color + "-" + deco;
	}
}
