using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityDressedHumanoid : EntityHumanoid
{
	private EntityBehaviorVillagerInv ebhv;

	private HumanoidOutfits humanoidOutfits;

	public Dictionary<string, WeightedCode[]> partialRandomOutfitsOverride;

	public override ItemSlot RightHandItemSlot => ebhv?.Inventory[0];

	public override ItemSlot LeftHandItemSlot => ebhv?.Inventory[1];

	public string OutfitConfigFileName => base.Properties.Attributes["outfitConfigFileName"].AsString("traderaccessories");

	public string[] OutfitSlots
	{
		get
		{
			return (WatchedAttributes["outfitslots"] as StringArrayAttribute)?.value;
		}
		set
		{
			if (value == null)
			{
				WatchedAttributes.RemoveAttribute("outfitslots");
			}
			else
			{
				WatchedAttributes["outfitslots"] = new StringArrayAttribute(value);
			}
			WatchedAttributes.MarkPathDirty("outfitslots");
		}
	}

	public string[] OutfitCodes
	{
		get
		{
			return (WatchedAttributes["outfitcodes"] as StringArrayAttribute)?.value;
		}
		set
		{
			if (value == null)
			{
				WatchedAttributes.RemoveAttribute("outfitcodes");
			}
			else
			{
				for (int i = 0; i < value.Length; i++)
				{
					if (value[i] == null)
					{
						value[i] = "";
					}
				}
				WatchedAttributes["outfitcodes"] = new StringArrayAttribute(value);
			}
			WatchedAttributes.MarkPathDirty("outfitcodes");
		}
	}

	public void LoadOutfitCodes()
	{
		if (Api.Side != EnumAppSide.Server)
		{
			return;
		}
		Dictionary<string, string> houtfit = base.Properties.Attributes["outfit"].AsObject<Dictionary<string, string>>();
		if (houtfit != null)
		{
			OutfitCodes = houtfit.Values.ToArray();
			OutfitSlots = houtfit.Keys.ToArray();
			return;
		}
		if (partialRandomOutfitsOverride == null)
		{
			partialRandomOutfitsOverride = base.Properties.Attributes["partialRandomOutfits"].AsObject<Dictionary<string, WeightedCode[]>>();
		}
		Dictionary<string, string> outfit = humanoidOutfits.GetRandomOutfit(OutfitConfigFileName, partialRandomOutfitsOverride);
		OutfitSlots = outfit.Keys.ToArray();
		OutfitCodes = outfit.Values.ToArray();
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		humanoidOutfits = Api.ModLoader.GetModSystem<HumanoidOutfits>();
		if (api.Side == EnumAppSide.Server)
		{
			if (OutfitCodes == null)
			{
				LoadOutfitCodes();
			}
		}
		else
		{
			WatchedAttributes.RegisterModifiedListener("outfitcodes", onOutfitsChanged);
		}
		ebhv = GetBehavior<EntityBehaviorVillagerInv>();
	}

	private void onOutfitsChanged()
	{
		MarkShapeModified();
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging)
	{
		ICoreClientAPI capi = Api as ICoreClientAPI;
		FastSmallDictionary<string, CompositeTexture> textDict = new FastSmallDictionary<string, CompositeTexture>(0);
		base.Properties.Client.Textures = textDict;
		foreach (KeyValuePair<string, CompositeTexture> val3 in Api.World.GetEntityType(Code).Client.Textures)
		{
			textDict[val3.Key] = val3.Value;
			val3.Value.Bake(capi.Assets);
		}
		Shape newShape = (entityShape = entityShape.Clone());
		string[] outfitCodes = OutfitCodes;
		TexturedWeightedCompositeShape[] cshapes = humanoidOutfits.Outfit2Shapes(OutfitConfigFileName, OutfitCodes);
		string[] slots = OutfitSlots;
		if (slots != null)
		{
			for (int j = 0; j < slots.Length && j < cshapes.Length; j++)
			{
				TexturedWeightedCompositeShape twcshape2 = cshapes[j];
				if (twcshape2 != null && !(twcshape2.Base == null))
				{
					addGearToShape(slots[j], twcshape2, newShape, shapePathForLogging, null, twcshape2.Textures);
				}
			}
			foreach (KeyValuePair<string, AssetLocation> val2 in entityShape.Textures)
			{
				if (!textDict.ContainsKey(val2.Key))
				{
					CompositeTexture texture = new CompositeTexture(val2.Value);
					texture.Bake(capi.Assets);
					textDict[val2.Key] = texture;
				}
			}
		}
		for (int i = 0; i < outfitCodes.Length; i++)
		{
			TexturedWeightedCompositeShape twcshape = cshapes[i];
			if (twcshape == null)
			{
				continue;
			}
			if (twcshape.DisableElements != null)
			{
				entityShape.RemoveElements(twcshape.DisableElements);
			}
			if (twcshape.OverrideTextures == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, AssetLocation> val in twcshape.OverrideTextures)
			{
				AssetLocation loc = val.Value;
				entityShape.Textures[val.Key] = loc;
				textDict[val.Key] = CreateCompositeTexture(loc, capi, new SourceStringComponents("Outfit config file {0}, Outfit slot {1}, Outfit type {2}, Override Texture {3}", OutfitConfigFileName, OutfitSlots[i], OutfitCodes[i], val.Key));
			}
		}
		bool cloned = true;
		base.OnTesselation(ref entityShape, shapePathForLogging, ref cloned);
	}

	private CompositeTexture CreateCompositeTexture(AssetLocation loc, ICoreClientAPI capi, SourceStringComponents sourceForLogging)
	{
		CompositeTexture cmpt = new CompositeTexture(loc);
		cmpt.Bake(capi.Assets);
		capi.EntityTextureAtlas.GetOrInsertTexture(new AssetLocationAndSource(cmpt.Baked.TextureFilenames[0], sourceForLogging), out var textureSubid, out var _);
		cmpt.Baked.TextureSubId = textureSubid;
		return cmpt;
	}

	protected void addGearToShape(string prefixcode, CompositeShape cshape, Shape entityShape, string shapePathForLogging, string[] disableElements = null, Dictionary<string, AssetLocation> textureOverrides = null)
	{
		if (disableElements != null)
		{
			entityShape.RemoveElements(disableElements);
		}
		AssetLocation shapePath = cshape.Base.CopyWithPath("shapes/" + cshape.Base.Path + ".json");
		Shape gearshape = Shape.TryGet(Api, shapePath);
		if (gearshape == null)
		{
			Api.World.Logger.Warning("Compositshape {0} (code: {2}) defined but not found or errored, was supposed to be at {1}. Part will be invisible.", cshape.Base, shapePath, prefixcode);
			return;
		}
		if (prefixcode != null && prefixcode.Length > 0)
		{
			prefixcode += "-";
		}
		if (textureOverrides != null)
		{
			foreach (KeyValuePair<string, AssetLocation> val2 in textureOverrides)
			{
				gearshape.Textures[prefixcode + val2.Key] = val2.Value;
			}
		}
		foreach (KeyValuePair<string, AssetLocation> val in gearshape.Textures)
		{
			entityShape.TextureSizes[prefixcode + val.Key] = new int[2] { gearshape.TextureWidth, gearshape.TextureHeight };
		}
		ICoreClientAPI capi = Api as ICoreClientAPI;
		IDictionary<string, CompositeTexture> clientTextures = base.Properties.Client.Textures;
		gearshape.SubclassForStepParenting(prefixcode);
		gearshape.ResolveReferences(Api.Logger, shapePath);
		entityShape.StepParentShape(gearshape, shapePath.ToShortString(), shapePathForLogging, Api.Logger, delegate(string texcode, AssetLocation loc)
		{
			string key = prefixcode + texcode;
			if (!clientTextures.ContainsKey(key))
			{
				clientTextures[key] = CreateCompositeTexture(loc, capi, new SourceStringComponents("Humanoid outfit", shapePath));
			}
		});
	}
}
