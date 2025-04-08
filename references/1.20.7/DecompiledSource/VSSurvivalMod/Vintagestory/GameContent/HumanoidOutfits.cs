using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class HumanoidOutfits : ModSystem
{
	private Dictionary<string, HumanoidWearableProperties> propsByConfigFilename = new Dictionary<string, HumanoidWearableProperties>();

	private ICoreAPI api;

	public override double ExecuteOrder()
	{
		return 1.0;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
	}

	public HumanoidWearableProperties loadProps(string configFilename)
	{
		HumanoidWearableProperties props = api.Assets.TryGet(new AssetLocation("config/" + configFilename + ".json"))?.ToObject<HumanoidWearableProperties>();
		if (props == null)
		{
			throw new FileNotFoundException("config/" + configFilename + ".json is missing.");
		}
		for (int i = 0; i < props.BySlot.Length; i++)
		{
			string[] shapecodes = props.BySlot[i].Variants;
			for (int j = 0; j < shapecodes.Length; j++)
			{
				if (!props.Variants.TryGetValue(shapecodes[j], out var wcshape))
				{
					api.World.Logger.Error("Typo in " + configFilename + ".json Shape reference {0} defined for slot {1}, but not in list of shapes. Will remove.", shapecodes[j], props.BySlot[i].Code);
					shapecodes = shapecodes.Remove(shapecodes[j]);
					j--;
				}
				else
				{
					props.BySlot[i].WeightSum += wcshape.Weight;
				}
			}
		}
		return propsByConfigFilename[configFilename] = props;
	}

	public Dictionary<string, string> GetRandomOutfit(string configFilename, Dictionary<string, WeightedCode[]> partialRandomOutfits = null)
	{
		if (!propsByConfigFilename.TryGetValue(configFilename, out var props))
		{
			props = loadProps(configFilename);
		}
		Dictionary<string, string> outfit = new Dictionary<string, string>();
		for (int i = 0; i < props.BySlot.Length; i++)
		{
			SlotAlloc slotall = props.BySlot[i];
			if (partialRandomOutfits != null && partialRandomOutfits.TryGetValue(slotall.Code, out var wcodes))
			{
				float wsum = 0f;
				for (int k = 0; k < wcodes.Length; k++)
				{
					wsum += wcodes[k].Weight;
				}
				float rnd = (float)api.World.Rand.NextDouble() * wsum;
				foreach (WeightedCode wcode in wcodes)
				{
					rnd -= wcode.Weight;
					if (rnd <= 0f)
					{
						outfit[slotall.Code] = wcode.Code;
						break;
					}
				}
				continue;
			}
			float rnd2 = (float)api.World.Rand.NextDouble() * slotall.WeightSum;
			for (int l = 0; l < slotall.Variants.Length; l++)
			{
				TexturedWeightedCompositeShape wcshape = props.Variants[slotall.Variants[l]];
				rnd2 -= wcshape.Weight;
				if (rnd2 <= 0f)
				{
					outfit[slotall.Code] = slotall.Variants[l];
					break;
				}
			}
		}
		return outfit;
	}

	public HumanoidWearableProperties GetConfig(string configFilename)
	{
		if (!propsByConfigFilename.TryGetValue(configFilename, out var props))
		{
			return loadProps(configFilename);
		}
		return props;
	}

	public TexturedWeightedCompositeShape[] Outfit2Shapes(string configFilename, string[] outfit)
	{
		if (!propsByConfigFilename.TryGetValue(configFilename, out var props))
		{
			props = loadProps(configFilename);
		}
		TexturedWeightedCompositeShape[] cshapes = new TexturedWeightedCompositeShape[outfit.Length];
		for (int i = 0; i < outfit.Length; i++)
		{
			if (!props.Variants.TryGetValue(outfit[i], out cshapes[i]))
			{
				api.Logger.Warning("Outfit code {1} for config file {0} cannot be resolved into a variant - wrong code or missing entry?", configFilename, outfit[i]);
			}
		}
		return cshapes;
	}

	public void Reload()
	{
		propsByConfigFilename.Clear();
	}
}
