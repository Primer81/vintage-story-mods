using System;
using System.IO;
using Vintagestory.API.Common;

namespace Vintagestory.API.Datastructures;

public class ItemstackAttribute : IAttribute
{
	public ItemStack value;

	public ItemstackAttribute()
	{
	}

	public ItemstackAttribute(ItemStack value)
	{
		this.value = value;
	}

	public int GetAttributeId()
	{
		return 7;
	}

	public object GetValue()
	{
		return value;
	}

	public void SetValue(ItemStack newval)
	{
		value = newval;
	}

	public void FromBytes(BinaryReader stream)
	{
		if (!stream.ReadBoolean())
		{
			value = new ItemStack();
			value.FromBytes(stream);
		}
	}

	public void ToBytes(BinaryWriter stream)
	{
		stream.Write(value == null);
		if (value != null)
		{
			value.ToBytes(stream);
		}
	}

	public bool Equals(IWorldAccessor worldForResolve, IAttribute attr)
	{
		return Equals(worldForResolve, attr, null);
	}

	internal bool Equals(IWorldAccessor worldForResolve, IAttribute attr, string[] ignorePaths)
	{
		if (!(attr is ItemstackAttribute))
		{
			return false;
		}
		ItemstackAttribute stackAttr = (ItemstackAttribute)attr;
		if (stackAttr.value != null || value != null)
		{
			if (stackAttr.value != null)
			{
				return stackAttr.value.Equals(worldForResolve, value, ignorePaths);
			}
			return false;
		}
		return true;
	}

	public string ToJsonToken()
	{
		if (value?.Collectible == null)
		{
			return "";
		}
		if (value.Attributes == null || value.Attributes.Count == 0)
		{
			return $"{{ \"type\": \"{value.Collectible.ItemClass.ToString().ToLowerInvariant()}\", code: \"{value.Collectible.Code.ToShortString()}\"}}";
		}
		return $"{{ \"type\": \"{value.Collectible.ItemClass.ToString().ToLowerInvariant()}\", \"code\": \"{value.Collectible.Code.ToShortString()}\", \"attributes\": {value.Attributes.ToJsonToken()}}}";
	}

	public override int GetHashCode()
	{
		return value?.GetHashCode() ?? 0;
	}

	public IAttribute Clone()
	{
		return new ItemstackAttribute(value?.Clone());
	}

	Type IAttribute.GetType()
	{
		return GetType();
	}
}
