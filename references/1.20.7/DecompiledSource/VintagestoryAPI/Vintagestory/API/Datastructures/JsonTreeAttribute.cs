using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Datastructures;

public class JsonTreeAttribute
{
	public string value;

	public string[] values;

	public Dictionary<string, JsonTreeAttribute> elems = new Dictionary<string, JsonTreeAttribute>();

	public EnumAttributeType type;

	public IAttribute ToAttribute(IWorldAccessor resolver)
	{
		if (type == EnumAttributeType.Unknown)
		{
			if (elems != null)
			{
				type = EnumAttributeType.Tree;
			}
			else if (values != null)
			{
				type = EnumAttributeType.StringArray;
			}
			else
			{
				type = EnumAttributeType.String;
			}
		}
		switch (type)
		{
		case EnumAttributeType.Bool:
			return new BoolAttribute(value == "true");
		case EnumAttributeType.Int:
		{
			int val4 = 0;
			int.TryParse(value, out val4);
			return new IntAttribute(val4);
		}
		case EnumAttributeType.Double:
		{
			double val3 = 0.0;
			double.TryParse(value, out val3);
			return new DoubleAttribute(val3);
		}
		case EnumAttributeType.Float:
		{
			float val2 = 0f;
			float.TryParse(value, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out val2);
			return new FloatAttribute(val2);
		}
		case EnumAttributeType.String:
			return new StringAttribute(value);
		case EnumAttributeType.StringArray:
			return new StringArrayAttribute(values);
		case EnumAttributeType.Tree:
		{
			ITreeAttribute tree = new TreeAttribute();
			if (elems == null)
			{
				return tree;
			}
			{
				foreach (KeyValuePair<string, JsonTreeAttribute> val in elems)
				{
					IAttribute attribute = val.Value.ToAttribute(resolver);
					if (attribute != null)
					{
						tree[val.Key] = attribute;
					}
				}
				return tree;
			}
		}
		case EnumAttributeType.Itemstack:
		{
			if (elems == null)
			{
				return null;
			}
			JsonTreeAttribute elemClass;
			bool num = elems.TryGetValue("class", out elemClass) && elemClass.type == EnumAttributeType.String;
			JsonTreeAttribute elemCode;
			bool haveItemCode = elems.TryGetValue("code", out elemCode) && elemCode.type == EnumAttributeType.String;
			JsonTreeAttribute elemQuantity;
			bool haveStackSize = elems.TryGetValue("quantity", out elemQuantity) && elemQuantity.type == EnumAttributeType.Int;
			if (!num || !haveItemCode || !haveStackSize)
			{
				return null;
			}
			EnumItemClass itemclass;
			try
			{
				itemclass = (EnumItemClass)Enum.Parse(typeof(EnumItemClass), elems["class"].value);
			}
			catch (Exception)
			{
				return null;
			}
			int quantity = 0;
			if (!int.TryParse(elems["quantity"].value, out quantity))
			{
				return null;
			}
			ItemStack itemstack;
			if (itemclass == EnumItemClass.Block)
			{
				Block block = resolver.GetBlock(new AssetLocation(elems["code"].value));
				if (block == null)
				{
					return null;
				}
				itemstack = new ItemStack(block, quantity);
			}
			else
			{
				Item item = resolver.GetItem(new AssetLocation(elems["code"].value));
				if (item == null)
				{
					return null;
				}
				itemstack = new ItemStack(item, quantity);
			}
			if (elems.TryGetValue("attributes", out var jsonAttribs))
			{
				IAttribute attributes = jsonAttribs.ToAttribute(resolver);
				if (attributes is ITreeAttribute)
				{
					itemstack.Attributes = (ITreeAttribute)attributes;
				}
			}
			return new ItemstackAttribute(itemstack);
		}
		default:
			return null;
		}
	}

	public JsonTreeAttribute Clone()
	{
		JsonTreeAttribute attribute = new JsonTreeAttribute
		{
			type = type,
			value = value
		};
		if (elems != null)
		{
			attribute.elems = new Dictionary<string, JsonTreeAttribute>(elems);
		}
		return attribute;
	}
}
