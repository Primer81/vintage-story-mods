using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public class EntityFloatStats
{
	public OrderedDictionary<string, EntityStat<float>> ValuesByKey = new OrderedDictionary<string, EntityStat<float>>();

	public EnumStatBlendType BlendType = EnumStatBlendType.WeightedSum;

	public EntityFloatStats()
	{
		ValuesByKey["base"] = new EntityStat<float>
		{
			Value = 1f,
			Persistent = true
		};
	}

	public float GetBlended()
	{
		float blended = 0f;
		bool first = true;
		switch (BlendType)
		{
		case EnumStatBlendType.FlatMultiply:
			foreach (EntityStat<float> stat3 in ValuesByKey.Values)
			{
				if (first)
				{
					blended = stat3.Value;
					first = false;
				}
				blended *= stat3.Value;
			}
			break;
		case EnumStatBlendType.FlatSum:
			foreach (EntityStat<float> stat4 in ValuesByKey.Values)
			{
				blended += stat4.Value;
			}
			break;
		case EnumStatBlendType.WeightedSum:
			foreach (EntityStat<float> stat2 in ValuesByKey.Values)
			{
				blended += stat2.Value * stat2.Weight;
			}
			break;
		case EnumStatBlendType.WeightedOverlay:
			foreach (EntityStat<float> stat in ValuesByKey.Values)
			{
				if (first)
				{
					blended = stat.Value;
					first = false;
				}
				blended = stat.Value * stat.Weight + blended * (1f - stat.Weight);
			}
			break;
		}
		return blended;
	}

	public void Set(string code, float value, bool persistent = false)
	{
		ValuesByKey[code] = new EntityStat<float>
		{
			Value = value,
			Persistent = persistent
		};
	}

	public void Remove(string code)
	{
		ValuesByKey.Remove(code);
	}

	public void ToTreeAttributes(ITreeAttribute tree, bool forClient)
	{
		foreach (KeyValuePair<string, EntityStat<float>> stat in ValuesByKey)
		{
			if (stat.Value.Persistent || forClient)
			{
				tree.SetFloat(stat.Key, stat.Value.Value);
			}
		}
	}

	public void FromTreeAttributes(ITreeAttribute tree)
	{
		foreach (KeyValuePair<string, IAttribute> val in tree)
		{
			ValuesByKey[val.Key] = new EntityStat<float>
			{
				Value = (val.Value as FloatAttribute).value,
				Persistent = true
			};
		}
	}
}
